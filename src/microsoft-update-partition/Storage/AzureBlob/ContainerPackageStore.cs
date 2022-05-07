// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Storage.Blob;
using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Partitions;
using Microsoft.PackageGraph.Storage.Index;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microsoft.PackageGraph.Storage.Azure
{
    enum AzurePackageStoreInitializeMode
    {
        ResetOnIndexCorruption,
        FailOnIndexCorruption
    }

    class ContainerPackageStore : IMetadataStore, IMetadataLookup
    {
        public event EventHandler<PackageStoreEventArgs> MetadataCopyProgress;
        public event EventHandler<PackageStoreEventArgs> PackageIndexingProgress;

#pragma warning disable 0067
        public event EventHandler<PackageStoreEventArgs> OpenProgress;
#pragma warning restore 0067

        public event EventHandler<PackageStoreEventArgs> PackagesAddProgress;

        readonly CloudBlobContainer ParentContainer;

        readonly MetadataStore Metadata;

        const string MetadataBlobName = "metadata";
        const string TocBlobName = "toc";

        readonly IndexContainer IndexContainer;

        readonly IdentitiesIndex Identities;

        bool IsDisposed = false;

        readonly List<IPackage> PendingPackages = new();

        /// <inheritdoc cref="IMetadataStore.IsReindexingRequired"/>
        public bool IsReindexingRequired { get; private set; } = false;

        /// <inheritdoc cref="IMetadataStore.IsMetadataIndexingSupported"/>
        public bool IsMetadataIndexingSupported { get; private set; } = true;

        private ContainerPackageStore(CloudBlobContainer container, AzurePackageStoreInitializeMode mode)
        {
            if (!container.Exists())
            {
                throw new Exception("Container does not exist");
            }

            ParentContainer = container;

            Identities = new IdentitiesIndex(container, mode);
            IndexContainer = new IndexContainer(container);

            Metadata = new MetadataStore(ParentContainer);

            var indexedIdentities = Identities.Identities.Select(identity => identity.OpenIdHex).ToList();

            var metadataIndexedIdentities = IndexContainer.GetListOfMetadataIndexedPackages().Select(index => Identities.GetPackageIdentity(index)).Select(identity => identity.OpenIdHex);
            var notMetadataIndexedIdentities = indexedIdentities.Except(metadataIndexedIdentities).ToList();
            if (notMetadataIndexedIdentities.Count > 0)
            {
                IsReindexingRequired = true;
            }

            IsReindexingRequired |= this.IndexContainer.ReIndexingRequired;

            var missingMetadata = indexedIdentities.Except(indexedIdentities).ToList();
            if (missingMetadata.Count > 0)
            {
                if (mode == AzurePackageStoreInitializeMode.FailOnIndexCorruption)
                {
                    throw new Exception($"The underlying metadata store does not contain all indexed packages from the store. Missing: {missingMetadata.Count}");
                }
                else if (mode == AzurePackageStoreInitializeMode.ResetOnIndexCorruption)
                {
                    Identities.Reset();
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        public static ContainerPackageStore OpenExisting(CloudBlobContainer container)
        {
            return new ContainerPackageStore(container, AzurePackageStoreInitializeMode.FailOnIndexCorruption);
        }

        public static ContainerPackageStore OpenExisting(CloudBlobClient client, string containerName)
        {
            var container = client.GetContainerReference(containerName);

            return new ContainerPackageStore(container, AzurePackageStoreInitializeMode.FailOnIndexCorruption);
        }

        public static void Erase(CloudBlobClient client, string containerName)
        {
            var containerRef = client.GetContainerReference(containerName);
            if (containerRef.Exists())
            {
                var tocReference = containerRef.GetBlockBlobReference(TocBlobName);
                tocReference.DeleteIfExists();

                for (int i = 0; i < int.MaxValue; i++)
                {
                    var metadataRef = containerRef.GetPageBlobReference(MetadataBlobName + i.ToString());
                    if (metadataRef.Exists())
                    {
                        metadataRef.Delete();
                    }
                    else
                    {
                        break;
                    }
                }

                IndexContainer.Erase(containerRef);
                IdentitiesIndex.Erase(containerRef);
            }
        }

        public static ContainerPackageStore OpenOrCreate(CloudBlobClient client, string containerName)
        {
            var container = client.GetContainerReference(containerName);
            container.CreateIfNotExists();

            return new ContainerPackageStore(container, AzurePackageStoreInitializeMode.ResetOnIndexCorruption);
        }

        public static bool Exists(CloudBlobClient client, string containerName)
        {
            var container = client.GetContainerReference(containerName);
            return container.Exists();
        }

        public bool ContainsMetadata(IPackageIdentity packageIdentity)
        {
            return Identities.TryGetPackageIndex(packageIdentity, out var _);
        }

        public Stream GetMetadata(IPackageIdentity packageIdentity)
        {
            if (Identities.TryGetPackageIndex(packageIdentity, out var packageIndex) &&
                Identities.TryGetStoreEntry(packageIndex, out var storeEntry))
            {
                return Metadata.GetMetadata(storeEntry);
            }
            else
            {
                throw new Exception($"Package {packageIdentity} not found");
            }
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("package store");
            }

            Flush();
            IsDisposed = true;
        }

        public void AddPackage(IPackage package)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("package store");
            }

            if (Identities.TryGetPackageIndex(package.Id, out var _))
            {
                return;
            }
            
            lock(Identities)
            {
                var entry = Metadata.AddPackage(package);

                var packageIndex = Identities.AddPackage(package, entry);
                IndexContainer.IndexPackage(package, packageIndex);

                PendingPackages.Add(package);
            }
        }

        public List<T> GetFiles<T>(IPackageIdentity packageIdentity)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("package store");
            }

            if (Identities.TryGetPackageIndex(packageIdentity, out var packageIndex) &&
                Identities.TryGetStoreEntry(packageIndex, out var storeEntry))
            {
                return Metadata.GetFiles<T>(storeEntry);
            }
            else
            {
                throw new Exception($"Package {packageIdentity} not found");
            }
        }

        public void AddPackages(IEnumerable<IPackage> packages)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("package store");
            }

            var progressArgs = new PackageStoreEventArgs() { Current = 0, Total = packages.Count() };
            PackagesAddProgress?.Invoke(this, progressArgs);
            foreach (var package in packages)
            {
                AddPackage(package);

                progressArgs.Current++;
                PackagesAddProgress?.Invoke(this, progressArgs);
            }
        }

        public void Flush()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("package store");
            }

            Identities.Save();
            IndexContainer.Save();
            Metadata.Flush();

            PendingPackages.Clear();
        }

        public IEnumerator<IPackage> GetEnumerator()
        {
            return new AzurePackageEnumerator(GetPackagesList(), this);
        }

        public List<IPackageIdentity> GetPackageIdentities()
        {
            return Identities.Identities.ToList();
        }

        public void CopyTo(IMetadataSink destination, CancellationToken cancelToken)
        {
            var identitiesToCopy = Identities.Identities.ToDictionary(id => id.ToString());
            var copyCount = identitiesToCopy.Count;

            HashSet<string> matchingPackagesInDestination = new();
            if (destination is IMetadataStore destinationPackageStore)
            {
                matchingPackagesInDestination = destinationPackageStore.GetPackageIdentities().Select(i => i.ToString()).ToHashSet();
            }

            copyCount -= matchingPackagesInDestination.Count;

            var progressArgs = new PackageStoreEventArgs() { Total = copyCount, Current = 0 };
            MetadataCopyProgress?.Invoke(copyCount, progressArgs);

            var sortedEntries = Identities.Entries.ToList().OrderBy(e => e.MetadataOffset);

            foreach (var entry in sortedEntries)
            {
                if (!matchingPackagesInDestination.Contains(entry.PackageId))
                {
                    var metadataStream = Metadata.GetMetadata(entry);

                    if (!PartitionRegistration.TryGetPartition(entry.PartitionName, out var partition))
                    {
                        throw new Exception($"There is no registered partition {entry.PartitionName}");
                    }

                    var package = partition.Factory.FromStream(metadataStream, this);
                    destination.AddPackage(package);

                    progressArgs.Current++;
                    MetadataCopyProgress?.Invoke(copyCount, progressArgs);
                }
            }
        }

        public bool ContainsPackage(IPackageIdentity packageIdentity)
        {
            return Identities.TryGetPackageIndex(packageIdentity, out var _);
        }

        public int GetPackageIndex(IPackageIdentity packageIdentity)
        {
            if (Identities.TryGetPackageIndex(packageIdentity, out var index))
            {
                return index;
            }
            else
            {
                return -1;
            }
        }

        public IPackage GetPackage(IPackageIdentity packageIdentity)
        {
            if (!Identities.TryGetPackageType(packageIdentity, out int packageType))
            {
                throw new Exception($"Package type is not available for package {packageIdentity}");
            }

            if (!PartitionRegistration.TryGetPartition(packageIdentity.Partition, out var partition))
            {
                throw new Exception($"There is no registered partition {packageIdentity.Partition}");
            }

            return partition.Factory.FromStore(packageType, packageIdentity, this, this);
        }

        public IPackage GetPackage(int packageIndex)
        {
            if (Identities.TryGetPackageIdentity(packageIndex, out var packageIdentity))
            {
                return GetPackage(packageIdentity);
            }
            else
            {
                throw new Exception($"Index {packageIndex} not found");
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private List<KeyValuePair<IPackageIdentity, PartitionDefinition>> GetPackagesList()
        {
            var packagePaths = new List<KeyValuePair<IPackageIdentity, PartitionDefinition>>();
            var allRegisteredPartitions = PartitionRegistration
                .GetAllPartitions()
                .Where(partition => partition.HandlesIdentities)
                .ToDictionary(partition => partition.Name);

            foreach (var identity in Identities.Identities)
            {
                if (!allRegisteredPartitions.TryGetValue(identity.Partition, out var partitionDefinition))
                {
                    throw new Exception($"There is no registered partition {identity.Partition}");
                }

                packagePaths.Add(new KeyValuePair<IPackageIdentity, PartitionDefinition>(identity, partitionDefinition));
            }

            return packagePaths;
        }

        public bool TrySimpleKeyLookup<T>(IPackageIdentity packageIdentity, string indexName, out T value)
        {
            if (!Identities.TryGetPackageIndex(packageIdentity, out int packageIndex))
            {
                throw new KeyNotFoundException();
            }

            return IndexContainer.TrySimpleKeyLookup(packageIndex, indexName, out value);
        }

        public bool TryPackageLookupByCustomKey<T>(T key, string indexName, out IPackageIdentity value)
        {
            if (IndexContainer.TryPackageLookupByCustomKey(key, indexName, out int packageIndex))
            {
                return Identities.TryGetPackageIdentity(packageIndex, out value);
            }
            else
            {
                value = null;
                return false;
            }
        }

        public bool TryPackageListLookupByCustomKey<T>(T key, string indexName, out List<IPackageIdentity> value)
        {
            if (IndexContainer.TryPackageListLookupByCustomKey(key, indexName, out List<int> packageIndex))
            {
                value = packageIndex.Select(index => Identities.GetPackageIdentity(index)).ToList();
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public bool TryListKeyLookup<T>(IPackageIdentity packageIdentity, string indexName, out List<T> value)
        {
            if (!Identities.TryGetPackageIndex(packageIdentity, out int packageIndex))
            {
                throw new KeyNotFoundException();
            }

            return IndexContainer.TryListKeyLookup<T>(packageIndex, indexName, out value);
        }

        public List<IndexDefinition> GetAvailableIndexes()
        {
            return IndexContainer.GetLoadedIndexes();
        }

        private void CheckIndex(bool forceReindex)
        {
            lock (Identities)
            {
                if (!IsReindexingRequired && !forceReindex)
                {
                    return;
                }

                IndexContainer.Erase(ParentContainer);
                IndexContainer.ResetIndex();

                PackageStoreEventArgs progressEvent = new() { Total = Identities.Identities.Count, Current = 0 };

                for (int packageIndex = 0; packageIndex <= Identities.Entries.Max(e => e.PackageIndex); packageIndex++)
                {
                    var packageIdentity = Identities.GetPackageIdentity(packageIndex);
                    var packageStream = Metadata.GetMetadata(Identities.GetStoreEntry(packageIndex));
                    if (PartitionRegistration.TryGetPartition(packageIdentity.Partition, out var partitionDefinition))
                    {
                        var parsedPackage = partitionDefinition.Factory.FromStream(packageStream, this);
                        IndexContainer.IndexPackage(parsedPackage, Identities.GetPackageIndex(packageIdentity));
                    }
                    else
                    {
                        throw new Exception($"Partition not found {packageIdentity.Partition}");
                    }

                    progressEvent.Current++;
                    PackageIndexingProgress?.Invoke(this, progressEvent);
                }

                IsReindexingRequired = false;
            }
        }

        /// <inheritdoc cref="IMetadataStore.ReIndex"/>
        public void ReIndex()
        {
            CheckIndex(true);
        }

        public void CopyTo(IMetadataSink destination, IMetadataFilter filter, CancellationToken cancelToken)
        {
            var identitiesToCopy = filter.Apply(this).Select(p => p.Id).ToDictionary(i => i.ToString());
            var copyCount = identitiesToCopy.Count;

            HashSet<string> matchingPackagesInDestination = new();
            if (destination is IMetadataStore destinationPackageStore)
            {
                matchingPackagesInDestination = destinationPackageStore.GetPackageIdentities().Select(i => i.ToString()).Intersect(identitiesToCopy.Keys).ToHashSet();
            }

            copyCount -= matchingPackagesInDestination.Count;

            var progressArgs = new PackageStoreEventArgs() { Total = copyCount, Current = 0 };
            MetadataCopyProgress?.Invoke(copyCount, progressArgs);

            if (copyCount == 0)
            {
                return;
            }

            foreach (var entry in Identities.Entries)
            {
                if (identitiesToCopy.ContainsKey(entry.PackageId) && !matchingPackagesInDestination.Contains(entry.PackageId))
                {
                    var metadataStream = Metadata.GetMetadata(entry);

                    if (!PartitionRegistration.TryGetPartition(entry.PartitionName, out var partition))
                    {
                        throw new Exception($"There is no registered partition {entry.PartitionName}");
                    }

                    var package = partition.Factory.FromStream(metadataStream, this);
                    destination.AddPackage(package);

                    progressArgs.Current++;
                    MetadataCopyProgress?.Invoke(copyCount, progressArgs);
                }
            }
        }

        public IReadOnlyList<IPackage> GetPendingPackages()
        {
            return PendingPackages.AsReadOnly();
        }

        class AzurePackageEnumerator : IEnumerator<IPackage>
        {
            readonly ContainerPackageStore _Source;
            readonly IEnumerator<KeyValuePair<IPackageIdentity, PartitionDefinition>> IdentitiesEnumerator;

            public AzurePackageEnumerator(List<KeyValuePair<IPackageIdentity, PartitionDefinition>> paths, ContainerPackageStore metadataSource)
            {
                _Source = metadataSource;
                IdentitiesEnumerator = paths.GetEnumerator();
            }

            public object Current => GetCurrent();

            IPackage IEnumerator<IPackage>.Current => GetCurrent();

            private IPackage GetCurrent()
            {
                return _Source.GetPackage(IdentitiesEnumerator.Current.Key);
            }

            public void Dispose()
            {
                IdentitiesEnumerator.Dispose();
            }

            public bool MoveNext()
            {
                return IdentitiesEnumerator.MoveNext();
            }

            public void Reset()
            {
                IdentitiesEnumerator.Reset();
            }
        }
    }
}