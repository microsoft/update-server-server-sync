// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ICSharpCode.SharpZipLib.Zip;
using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Partitions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Microsoft.PackageGraph.Storage.Local
{
    class CompressedMetadataStore : IMetadataSink, IMetadataSource
    {
        private ZipFile InputFile;
        ZipOutputStream OutputFile;

        Dictionary<string, long> ZipEntriesIndex;
        
        private bool IsDisposed = false;

        readonly object WriteLock = new();

        public event EventHandler<PackageStoreEventArgs> MetadataCopyProgress;

#pragma warning disable 0067
        public event EventHandler<PackageStoreEventArgs> OpenProgress;
        public event EventHandler<PackageStoreEventArgs> PackagesAddProgress;
#pragma warning restore 0067

        private CompressedMetadataStore()
        {
        }

        public static CompressedMetadataStore OpenExisting(string path)
        {
            var newZipStorage = new CompressedMetadataStore
            {
                InputFile = new ZipFile(path)
            };
            newZipStorage.ZipEntriesIndex = newZipStorage.InputFile.OfType<ZipEntry>().ToDictionary(entry => entry.Name, entry => entry.ZipFileIndex);

            return newZipStorage;
        }

        public static CompressedMetadataStore CreateNew(string path)
        {
            var newZipStorage = new CompressedMetadataStore
            {
                OutputFile = new ZipOutputStream(File.Create(path))
            };
            newZipStorage.OutputFile.SetLevel(1);
            return newZipStorage;
        }

        private static string GetPackageMetadataPath(IPackageIdentity identity)
        {
            return $"metadata/partitions/{identity.Partition}/{GetPackageIndex(identity)}/{identity.OpenIdHex}.xml";
        }

        private static string GetPackageFilesPath(IPackageIdentity identity)
        {
            return $"filemetadata/partitions/{identity.Partition}/{GetPackageIndex(identity)}/{identity.OpenIdHex}.files.json";
        }

        private static string GetPackageIndex(IPackageIdentity identity)
        {
            // The index is the last 8 bits of the update ID.
            return identity.OpenId.Last().ToString();
        }

        public bool ContainsMetadata(IPackageIdentity packageIdentity)
        {
            if (InputFile != null)
            {
                var metadataPath = GetPackageMetadataPath(packageIdentity);
                return ZipEntriesIndex.TryGetValue(metadataPath, out var _);
            }
            else
            {
                throw new Exception("Read not supported");
            }
        }

        public Stream GetMetadata(IPackageIdentity packageIdentity)
        {
            if (InputFile != null)
            {
                var metadataPath = GetPackageMetadataPath(packageIdentity);
                return GetEntryStream(metadataPath);
            }
            else
            {
                throw new Exception("Read not supported");
            }
        }

        private Stream GetEntryStream(string path)
        {
            if (ZipEntriesIndex.TryGetValue(path, out long entryIndex))
            {
                return InputFile.GetInputStream(entryIndex);
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                if (OutputFile != null)
                {
                    OutputFile.Close();
                    OutputFile.Dispose();
                    OutputFile = null;
                }
                else if (InputFile == null)
                {
                    InputFile.Close();
                    InputFile = null;
                }

                IsDisposed = true;
            }
        }

        public void AddPackage(IPackage package)
        {
            if (OutputFile == null)
            {
                throw new Exception("Write not supported");
            }

            lock(WriteLock)
            {
                WritePackageMetadata(package);

                if (PartitionRegistration.TryGetPartitionFromPackage(package, out var partitionDefinition) &&
                    partitionDefinition.HasExternalContentFileMetadata &&
                    package.Files != null &&
                    package.Files.Any())
                {
                    WritePackageFiles(package);
                }
            }
        }

        private void WritePackageMetadata(IPackage package)
        {
            var metadataPath = GetPackageMetadataPath(package.Id);
            OutputFile.PutNextEntry(new ZipEntry(metadataPath));
            var packageMetadataStream = package.GetMetadataStream();
            packageMetadataStream.CopyTo(OutputFile);
            OutputFile.CloseEntry();
        }

        private void WritePackageFiles(IPackage package)
        {
            var filesFilePath = GetPackageFilesPath(package.Id);
            OutputFile.PutNextEntry(new ZipEntry(filesFilePath));

            var serializer = new JsonSerializer();
            using (var textWriter = new StreamWriter(OutputFile, Encoding.UTF8, 4096, true))
            {
                serializer.Serialize(textWriter, package.Files);
            }

            OutputFile.CloseEntry();
        }

        public List<T> GetFiles<T>(IPackageIdentity packageIdentity)
        {
            if (InputFile == null)
            {
                throw new Exception("Read not supported");
            }

            if (PartitionRegistration.TryGetPartitionFromPackageId(packageIdentity, out var partitionDefinition) &&
                    partitionDefinition.HasExternalContentFileMetadata)
            {
                var filesPath = GetPackageFilesPath(packageIdentity);
                if (ZipEntriesIndex.TryGetValue(filesPath, out long entryIndex))
                {
                    using var filesStream = InputFile.GetInputStream(entryIndex);
                    using var filesReader = new StreamReader(filesStream);
                    var serializer = new JsonSerializer();
                    return (serializer.Deserialize(filesReader, typeof(List<T>)) as List<T>);
                }
            }

            return new List<T>();
        }

        public void AddPackages(IEnumerable<IPackage> packages)
        {
            foreach(var package in packages)
            {
                AddPackage(package);
            }
        }

        public void Flush()
        {
            if (OutputFile != null)
            {
                lock(WriteLock)
                {
                    OutputFile.Flush();
                }
            }
        }

        private List<KeyValuePair<string, PartitionDefinition>> GetPackagesList()
        {
            var packagePaths = new List<KeyValuePair<string, PartitionDefinition>>();

            foreach (var entry in InputFile)
            {
                if (entry is ZipEntry zipEntry)
                {
                    foreach(var partitionDefinition in PartitionRegistration.GetAllPartitions())
                    {
                        if (zipEntry.Name.StartsWith($"metadata/partitions/{partitionDefinition.Name}/"))
                        {
                            packagePaths.Add(new KeyValuePair<string, PartitionDefinition>(zipEntry.Name, partitionDefinition));
                            break;
                        }
                    }   
                }
            }

            return packagePaths;
        }

        private IPackage GetPackage(string path, string partitionName)
        {
            var packageStream = GetEntryStream(path);
            if (PartitionRegistration.TryGetPartition(partitionName, out var partitionDefinition))
            {
                return partitionDefinition.Factory.FromStream(packageStream, this);
            }

            throw new NotImplementedException();
        }

        public IEnumerator<IPackage> GetEnumerator()
        {
            if (InputFile == null)
            {
                throw new Exception("Read not supported");
            }

            var packagePaths = GetPackagesList();
            return new MetadataEnumerator(packagePaths, this);
        }

        public void CopyTo(IMetadataSink destination, CancellationToken cancelToken)
        {
            var packageEntries = GetPackagesList();

            var progressArgs = new PackageStoreEventArgs() { Total = packageEntries.Count, Current = 0 };
            MetadataCopyProgress?.Invoke(this, progressArgs);
            packageEntries.AsParallel().ForAll(packageEntry =>
            {
                if (cancelToken.IsCancellationRequested)
                {
                    return;
                }

                destination.AddPackage(GetPackage(packageEntry.Key, packageEntry.Value.Name));

                lock(progressArgs)
                {
                    progressArgs.Current++;
                }
                if (progressArgs.Current % 100 == 0)
                {
                    MetadataCopyProgress?.Invoke(this, progressArgs);
                }
            });
        }

        public void CopyTo(IMetadataSink destination, IMetadataFilter filter, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }

        class MetadataEnumerator : IEnumerator<IPackage>
        {
            readonly CompressedMetadataStore _Source;
            readonly IEnumerator<KeyValuePair<string, PartitionDefinition>> PathsEnumerator;

            public MetadataEnumerator(List<KeyValuePair<string, PartitionDefinition>> paths, CompressedMetadataStore metadataSource)
            {
                _Source = metadataSource;
                PathsEnumerator = paths.GetEnumerator();
            }

            public object Current => GetCurrent();

            IPackage IEnumerator<IPackage>.Current => GetCurrent();

            private IPackage GetCurrent()
            {
                return _Source.GetPackage(PathsEnumerator.Current.Key, PathsEnumerator.Current.Value.Name);
            }

            public void Dispose()
            {
                PathsEnumerator.Dispose();
            }

            public bool MoveNext()
            {
                return PathsEnumerator.MoveNext();
            }

            public void Reset()
            {
                PathsEnumerator.Reset();
            }
        }
    }
}