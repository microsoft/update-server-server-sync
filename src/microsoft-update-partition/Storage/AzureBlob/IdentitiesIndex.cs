// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ICSharpCode.SharpZipLib.GZip;
using Microsoft.Azure.Storage.Blob;
using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Partitions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.PackageGraph.Storage.Azure
{
    class IdentitiesIndex
    {
        private readonly CloudBlobContainer ParentContainer;

        private Dictionary<IPackageIdentity, int> _IdentityToIndexMap;
        private Dictionary<int, IPackageIdentity> _IndexToIdentityMap;
        private Dictionary<int, int> PackageTypeIndex;
        private Dictionary<int, PackageStoreEntry> StoreEntries;

        private string ConcurrencyEtag;

        private const string IdentitiesIndexBlobName = "identities-index";

        public IReadOnlyCollection<PackageStoreEntry> Entries => StoreEntries.Values;

        private List<PackageStoreEntry> PendingIdentities;

        public List<IPackageIdentity> Identities => _IdentityToIndexMap.Keys.ToList();

        public IdentitiesIndex(CloudBlobContainer container, AzurePackageStoreInitializeMode mode)
        {
            ParentContainer = container;
            PendingIdentities = new List<PackageStoreEntry>();
            Read(mode);
        }

        public static void Erase(CloudBlobContainer container)
        {
            var indexBlob = container.GetBlockBlobReference(IdentitiesIndexBlobName);
            indexBlob.DeleteIfExists();
        }

        public void Reset()
        {
            _IndexToIdentityMap = new Dictionary<int, IPackageIdentity>();
            _IdentityToIndexMap = new Dictionary<IPackageIdentity, int>();
            PackageTypeIndex = new Dictionary<int, int>();
            PendingIdentities = new List<PackageStoreEntry>();
        }

        private void ReadIdentityEntries()
        {
            _IndexToIdentityMap = new Dictionary<int, IPackageIdentity>();
            _IdentityToIndexMap = new Dictionary<IPackageIdentity, int>();
            PackageTypeIndex = new Dictionary<int, int>();
            StoreEntries = new Dictionary<int, PackageStoreEntry>();

            var indexBlob = ParentContainer.GetBlockBlobReference(IdentitiesIndexBlobName);
            if (indexBlob.Exists())
            {
                ConcurrencyEtag = indexBlob.Properties.ETag;
                using var indexStream = new MemoryStream();
                indexBlob.DownloadRangeToStream(indexStream, 0, indexBlob.Properties.Length);

                var blockList = indexBlob.DownloadBlockList(BlockListingFilter.Committed);
                long currentOffset = 0;

                foreach (var block in blockList)
                {
                    indexStream.Seek(currentOffset, SeekOrigin.Begin);

                    using (var zipStream = new GZipInputStream(indexStream))
                    {
                        zipStream.IsStreamOwner = false;
                        using var jsonReader = new StreamReader(zipStream, Encoding.UTF8);
                        var jsonDeserializer = new JsonSerializer();
                        var deserializedIntries = jsonDeserializer.Deserialize(jsonReader, typeof(List<PackageStoreEntry>)) as List<PackageStoreEntry>;
                        deserializedIntries.ForEach(entry =>
                        {
                            if (!PartitionRegistration.TryGetPartition(entry.PartitionName, out var partitionDefinition))
                            {
                                throw new Exception("Unknown package partition");
                            }

                            var packageIdentity = partitionDefinition.Factory.IdentityFromString(entry.PackageId);
                            _IndexToIdentityMap.Add((int)entry.PackageIndex, packageIdentity);
                            _IdentityToIndexMap.Add(packageIdentity, (int)entry.PackageIndex);
                            PackageTypeIndex.Add((int)entry.PackageIndex, entry.PackageType);
                            StoreEntries.Add((int)entry.PackageIndex, entry);
                        });
                    }

                    currentOffset += block.Length;
                }
            }
            else
            {
                ConcurrencyEtag = null;
            }
        }

        private void Read(AzurePackageStoreInitializeMode mode)
        {
            ReadIdentityEntries();

            if (_IndexToIdentityMap.Keys.Except(PackageTypeIndex.Keys).Any())
            {
                if (mode == AzurePackageStoreInitializeMode.FailOnIndexCorruption)
                {
                    throw new Exception("Mismatch between package type and identity indexes");
                }
                else if (mode == AzurePackageStoreInitializeMode.ResetOnIndexCorruption)
                {
                    Reset();
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        public bool TryGetStoreEntry(int packageIndex, out PackageStoreEntry storeEntry)
        {
            return StoreEntries.TryGetValue(packageIndex, out storeEntry);
        }

        public PackageStoreEntry GetStoreEntry(int packageIndex)
        {
            return StoreEntries[packageIndex];
        }

        public bool TryGetPackageType(IPackageIdentity packageIdentity, out int packageType)
        {
            if (_IdentityToIndexMap.TryGetValue(packageIdentity, out var packageIndex))
            {
                return PackageTypeIndex.TryGetValue(packageIndex, out packageType);
            }
            else
            {
                packageType = -1;
                return false;
            }
        }

        public bool TryGetPackageIndex(IPackageIdentity packageIdentity, out int index)
        {
            return _IdentityToIndexMap.TryGetValue(packageIdentity, out index);
        }

        public int GetPackageIndex(IPackageIdentity packageIdentity)
        {
            return _IdentityToIndexMap[packageIdentity];
        }

        public IPackageIdentity GetPackageIdentity(int index)
        {
            return _IndexToIdentityMap[index];
        }

        public bool TryGetPackageIdentity(int index, out IPackageIdentity packageIdentity)
        {
            return _IndexToIdentityMap.TryGetValue(index, out packageIdentity);
        }

        public int AddPackage(IPackage package, PackageStoreEntry packageEntry)
        {
            lock(_IdentityToIndexMap)
            {
                var insertIndex = _IdentityToIndexMap.Count;
                if (!_IdentityToIndexMap.TryAdd(package.Id, insertIndex))
                {
                    throw new Exception("package already exists");
                }

                _IndexToIdentityMap.Add(insertIndex, package.Id);

                if (!PartitionRegistration.TryGetPartitionFromPackage(package, out var partitionDefinition))
                {
                    throw new Exception($"Cannot find partition {package.Id.Partition}");
                }

                var packageType = partitionDefinition.Factory.GetPackageType(package);
                PackageTypeIndex.Add(insertIndex, packageType);

                packageEntry.PackageId = package.Id.ToString();
                packageEntry.PackageIndex = insertIndex;
                packageEntry.PackageType = packageType;
                packageEntry.PartitionName = package.Id.Partition;

                PendingIdentities.Add(packageEntry);

                return insertIndex;
            }
        }

        private static string GetCommitIdForPackages(List<string> packageIdentities)
        {
            MemoryStream identitiesBuffer = new();
            packageIdentities.ForEach(id => identitiesBuffer.Write(Encoding.UTF8.GetBytes(id)));
            identitiesBuffer.Seek(0, SeekOrigin.Begin);

            using HashAlgorithm hashAlgorithm = SHA256.Create();
            return BitConverter.ToString(hashAlgorithm.ComputeHash(identitiesBuffer)).Replace("-", "");
        }

        public void Save()
        {
            lock(_IdentityToIndexMap)
            {
                if (PendingIdentities.Count > 0)
                {
                    var pendingIdentitiesJson = JsonConvert.SerializeObject(PendingIdentities);
                    using var pendingIdentitiesStream = new MemoryStream();
                    using (var compressor = new GZipOutputStream(pendingIdentitiesStream))
                    {
                        compressor.IsStreamOwner = false;
                        compressor.Write(Encoding.UTF8.GetBytes(pendingIdentitiesJson));
                    }

                    pendingIdentitiesStream.Seek(0, SeekOrigin.Begin);
                    var indexBlob = ParentContainer.GetBlockBlobReference(IdentitiesIndexBlobName);
                    List<string> blocksList = new();

                    string currentEtag = null;
                    if (indexBlob.Exists())
                    {
                        currentEtag = indexBlob.Properties.ETag;
                        blocksList.AddRange(indexBlob.DownloadBlockList(BlockListingFilter.Committed).Select(block => block.Name));
                    }

                    if (currentEtag != ConcurrencyEtag)
                    {
                        throw new Exception("Package store index changed unexpectedly.");
                    }

                    var commitId = GetCommitIdForPackages(PendingIdentities.Select(p => p.PackageId).ToList());
                    indexBlob.PutBlock(commitId, pendingIdentitiesStream, null);

                    blocksList.Add(commitId);
                    indexBlob.PutBlockList(blocksList);

                    PendingIdentities.Clear();
                }
            }
        }
    }
}
