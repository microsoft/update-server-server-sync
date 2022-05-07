// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Partitions;
using Microsoft.PackageGraph.Storage.Index;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microsoft.PackageGraph.Storage.Local
{
    class DirectoryPackageStore : IMetadataSink, IMetadataStore, IMetadataLookup
    {
        readonly string TargetPath;

        private const string TableOfContentsFileName = ".toc.json";
        private const string IdentitiesFileName = ".identities.json";
        private const string TypesFileName = ".types.json";
        private const string IdentitiesDirectoryName = "identities";
        private const string IndexesContainerFileName = ".indexes.zip";

        private Dictionary<IPackageIdentity, int> _IdentityToIndexMap;
        private Dictionary<int, IPackageIdentity> _IndexToIdentityMap;

        private Dictionary<int, int> _PackageTypeIndex;

        List<CompressedMetadataStore> DeltaMetadataStores;

        TableOfContent TOC;

        private readonly object WriteLock = new();

        private bool IsDirty = false;
        private bool IsIndexDirty = false;
        private bool IsDisposed = false;

        public int PackageCount => _IdentityToIndexMap.Count;

        /// <inheritdoc cref="IMetadataStore.IsReindexingRequired"/>
        public bool IsReindexingRequired => _IsReindexingRequired;

        /// <inheritdoc cref="IMetadataStore.IsMetadataIndexingSupported"/>
        public bool IsMetadataIndexingSupported { get; private set; } = true;

        private bool NewDeltaSubdirectoryCreated = false;

        private bool _IsReindexingRequired = false;

        readonly private ZipStreamIndexContainer Indexes;

#pragma warning disable 0067
        public event EventHandler<PackageStoreEventArgs> MetadataCopyProgress;
        public event EventHandler<PackageStoreEventArgs> OpenProgress;
        public event EventHandler<PackageStoreEventArgs> PackagesAddProgress;
#pragma warning restore 0067

        public event EventHandler<PackageStoreEventArgs> PackageIndexingProgress;
        

        private readonly List<IPackage> PendingPackages = new();

        public DirectoryPackageStore(string path, FileMode mode)
        {
            TargetPath = path;

            if (Directory.Exists(path) && IsValidDirectory(path))
            {
                ReadToc();
                ReadIdentities();

                var indexContainerPath = Path.Combine(path, IndexesContainerFileName);
                if (File.Exists(indexContainerPath))
                {
                    Indexes = ZipStreamIndexContainer.Open(File.OpenRead(indexContainerPath));
                    if (Indexes.GetStatus() != ZipStreamIndexContainer.IndexContainerStatus.Valid)
                    {
                        _IsReindexingRequired = true;
                    }
                }
                else
                {
                    Indexes = ZipStreamIndexContainer.Create();
                    if (_IdentityToIndexMap.Count > 0)
                    {
                        _IsReindexingRequired = true;
                    }
                }
            }
            else
            {
                if (mode == FileMode.Open)
                {
                    throw new Exception("The store does not exist or is corrupt");
                }

                Directory.CreateDirectory(path);
                Indexes = ZipStreamIndexContainer.Create();
                TOC = new TableOfContent
                {
                    TocVersion = TableOfContent.CurrentVersion
                };
                DeltaMetadataStores = new List<CompressedMetadataStore>();
                _IdentityToIndexMap = new Dictionary<IPackageIdentity, int>();
                _IndexToIdentityMap = new Dictionary<int, IPackageIdentity>();
                _PackageTypeIndex = new Dictionary<int, int>();
            }
        }

        public static bool Exists(string path)
        {
            return Directory.Exists(path) && IsValidDirectory(path);
        }

        private static bool IsValidDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                return false;
            }

            var tocFile = Path.Combine(path, TableOfContentsFileName);
            if (!File.Exists(tocFile))
            {
                return false;
            }

            var identitiesDirectory = Path.Combine(path, IdentitiesDirectoryName);
            if (!Directory.Exists(identitiesDirectory))
            {
                return false;
            }

            var partitions = Directory.GetDirectories(identitiesDirectory);
            foreach (var partition in partitions)
            {
                var identitiesFile = Path.Combine(partition, IdentitiesFileName);
                if (!File.Exists(identitiesFile))
                {
                    return false;
                }
            }

            return true;
        }

        private void WriteIndexes()
        {
            var indexContainerPath = Path.Combine(TargetPath, IndexesContainerFileName);
            var tempIndexContainerPath = indexContainerPath + ".tmp";
            Indexes.Save(File.Create(tempIndexContainerPath));

            Indexes.CloseInput();

            if (File.Exists(indexContainerPath))
            {
                File.Delete(indexContainerPath);
            }

            File.Move(tempIndexContainerPath, indexContainerPath);
        }

        private void ReadToc()
        {
            using (var tocFileStream = File.OpenText(Path.Combine(TargetPath, TableOfContentsFileName)))
            {
                var deserializer = new JsonSerializer();
                TOC = deserializer.Deserialize(tocFileStream, typeof(TableOfContent)) as TableOfContent;
            }

            if (TOC.TocVersion != TableOfContent.CurrentVersion)
            {
                throw new InvalidDataException();
            }

            DeltaMetadataStores = new List<CompressedMetadataStore>();
            for (int i = 0; i < TOC.DeltaSectionCount; i++)
            {
                DeltaMetadataStores.Add(CompressedMetadataStore.OpenExisting(Path.Combine(TargetPath, $"{i}.zip")));
            }
        }

        private void WriteToc()
        {
            using var tocFileStream = File.CreateText(Path.Combine(TargetPath, TableOfContentsFileName));
            var serializer = new JsonSerializer();
            serializer.Serialize(tocFileStream, TOC);
        }

        private void ReadIdentities()
        {
            _IndexToIdentityMap = new Dictionary<int, IPackageIdentity>();
            _PackageTypeIndex = new Dictionary<int, int>();

            var partitionDirectories = Directory.GetDirectories(Path.Combine(TargetPath, IdentitiesDirectoryName));
            foreach (var partitionDirectory in partitionDirectories)
            {
                var identitiesFilePath = Path.Combine(partitionDirectory, IdentitiesFileName);
                var partitionName = Path.GetFileName(partitionDirectory);

                if (PartitionRegistration.TryGetPartition(partitionName, out var partitionDefinition))
                {
                    using var identitiesFileReader = File.OpenText(identitiesFilePath);
                    var partitionIdentities = partitionDefinition.Factory.IdentitiesFromJson(identitiesFileReader);
                    foreach (var identityEntry in partitionIdentities)
                    {
                        _IndexToIdentityMap.Add(identityEntry.Key, identityEntry.Value);
                    }
                }
            }

            var typesFile = Path.Combine(TargetPath, TypesFileName);
            using (var typesFileReader = File.OpenText(typesFile))
            {
                var deserializer = new JsonSerializer();
                _PackageTypeIndex = deserializer.Deserialize(typesFileReader, typeof(Dictionary<int, int>)) as Dictionary<int, int>;
            }

            _IdentityToIndexMap = _IndexToIdentityMap.ToDictionary(pair => pair.Value, pair => pair.Key);
        }

        public bool ContainsPackage(IPackageIdentity packageIdentity)
        {
            return _IdentityToIndexMap.ContainsKey(packageIdentity);
        }

        public void CopyTo(IMetadataSink destination, CancellationToken cancelToken)
        {
            var packagesIdsToCopy = _IdentityToIndexMap.Keys.ToList();
            
            if (destination is IMetadataStore destinationPackageStore)
            {
                packagesIdsToCopy = packagesIdsToCopy.Except(destinationPackageStore.GetPackageIdentities()).ToList();
            }

            var packagesToAdd = packagesIdsToCopy.Select(id => GetPackage(id));
            destination.AddPackages(packagesToAdd);
        }

        public void Dispose()
        {
            lock(WriteLock)
            {
                if (!IsDisposed)
                {
                    Flush();
                }

                DeltaMetadataStores.ForEach(s => s.Dispose());
                DeltaMetadataStores.Clear();
                _IndexToIdentityMap.Clear();
                _IdentityToIndexMap.Clear();
                _PackageTypeIndex.Clear();

                IsDisposed = true;
            }
        }

        public void Flush()
        {
            if (IsDirty)
            {
                DeltaMetadataStores.Last().Flush();

                WriteToc();

                foreach(var partitionEntry in PartitionRegistration.GetAllPartitions())
                {
                    if (!partitionEntry.HandlesIdentities)
                    {
                        continue;
                    }

                    var partitionIdentites = partitionEntry.Factory.FilterPartitionIdentities(_IndexToIdentityMap);

                    var partitionDirectoryPath = Path.Combine(TargetPath, IdentitiesDirectoryName, partitionEntry.Name);
                    if (!Directory.Exists(partitionDirectoryPath))
                    {
                        Directory.CreateDirectory(partitionDirectoryPath);
                    }

                    var partitionIdentitiesFile = Path.Combine(partitionDirectoryPath, IdentitiesFileName);
                    using var identitiesWriter = File.CreateText(partitionIdentitiesFile);
                    var serializer = new JsonSerializer();
                    serializer.Serialize(identitiesWriter, partitionIdentites);
                }

                var packageTypesFile = Path.Combine(TargetPath, TypesFileName);
                using (var typesWriter = File.CreateText(packageTypesFile))
                {
                    var serializer = new JsonSerializer();
                    serializer.Serialize(typesWriter, _PackageTypeIndex);
                }

                WriteIndexes();

                IsDirty = false;

                PendingPackages.Clear();
            }
            else if (IsIndexDirty)
            {
                WriteIndexes();
                IsIndexDirty = false;
            }
        }

        private void CheckIndex(bool forceReindex = false)
        {
            lock(WriteLock)
            {
                if (!_IsReindexingRequired && !forceReindex)
                {
                    return;
                }

                Indexes.ResetIndex();

                PackageStoreEventArgs progressEvent = new() 
                { 
                    Total = _IdentityToIndexMap.Count, 
                    Current = 0 
                };

                foreach(var deltaStore in DeltaMetadataStores)
                {
                    foreach(var parsedPackage in deltaStore)
                    {
                        Indexes.IndexPackage(parsedPackage, _IdentityToIndexMap[parsedPackage.Id]);

                        if (progressEvent.Current % 100 == 0)
                        {
                            PackageIndexingProgress?.Invoke(this, progressEvent);
                        }
                        progressEvent.Current++;
                    }
                }

                _IsReindexingRequired = false;
                IsIndexDirty = true;
            }
        }

        public void AddPackage(IPackage package, out int packageIndex)
        {
            CheckIndex();

            if (_IdentityToIndexMap.TryGetValue(package.Id, out packageIndex))
            {
                return;
            }

            lock(WriteLock)
            {
                if (!NewDeltaSubdirectoryCreated)
                {
                    DeltaMetadataStores.Add(CompressedMetadataStore.CreateNew(Path.Combine(TargetPath, $"{TOC.DeltaSectionCount}.zip")));
                    TOC.DeltaSectionCount++;

                    if (TOC.DeltaSectionPackageCount == null)
                    {
                        TOC.DeltaSectionPackageCount = new List<long> { 0 };
                    }
                    else
                    {
                        TOC.DeltaSectionPackageCount.Add(TOC.DeltaSectionPackageCount.Last());
                    }

                    NewDeltaSubdirectoryCreated = true;
                }

                TOC.DeltaSectionPackageCount[^1] = TOC.DeltaSectionPackageCount[^1] + 1;

                packageIndex = PackageCount;

                _IdentityToIndexMap.Add(package.Id, packageIndex);
                _IndexToIdentityMap.Add(packageIndex, package.Id);

                AddPackageType(packageIndex, package);

                Indexes.IndexPackage(package, packageIndex);
                IsIndexDirty = true;

                DeltaMetadataStores.Last().AddPackage(package);

                PendingPackages.Add(package);

                IsDirty = true;
            }
        }

        public void AddPackageType(int packageIndex, IPackage package)
        {
            if (PartitionRegistration.TryGetPartitionFromPackage(package, out var partitionDefinition))
            {
                _PackageTypeIndex.Add(packageIndex, partitionDefinition.Factory.GetPackageType(package));
            }
        }

        public int IndexOf(IPackageIdentity packageIdentity)
        {
            return _IdentityToIndexMap.TryGetValue(packageIdentity, out int packageIndex) ? packageIndex : -1;
        }

        public IEnumerator<IPackageIdentity> GetEnumerator()
        {
            return _IndexToIdentityMap.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new MetadataEnumerator(this);
        }

        public void AddPackage(IPackage package)
        {
            AddPackage(package, out var _);
        }

        public void AddPackages(IEnumerable<IPackage> packages)
        {
            foreach (var package in packages)
            {
                AddPackage(package);
            }
        }

        public List<IPackageIdentity> GetPackageIdentities()
        {
            return _IdentityToIndexMap.Keys.ToList();
        }

        public IPackage GetPackage(IPackageIdentity packageIdentity)
        {
            if (!_IdentityToIndexMap.TryGetValue(packageIdentity, out int packageIndex))
            {
                throw new KeyNotFoundException();
            }

            if (PartitionRegistration.TryGetPartitionFromPackageId(packageIdentity, out var partitionDefinition))
            {
                return partitionDefinition.Factory.FromStore(_PackageTypeIndex[packageIndex], packageIdentity, this, this);
            }
            else
            {
                throw new NotImplementedException($"The package belongs to a partition that was not registered: {packageIdentity.Partition}");
            }
        }

        private int GetDeltaIndexFromPackageIndex(int packageIndex)
        {
            var deltaIndex = TOC.DeltaSectionPackageCount.BinarySearch(packageIndex);
            if (deltaIndex < 0)
            {
                deltaIndex = ~deltaIndex;
            }
            else
            {
                deltaIndex++;
            }

            if (deltaIndex == TOC.DeltaSectionPackageCount.Count)
            {
                throw new KeyNotFoundException();
            }

            return deltaIndex;
        }

        IEnumerator<IPackage> IEnumerable<IPackage>.GetEnumerator()
        {
            return new MetadataEnumerator(this);
        }

        public bool TrySimpleKeyLookup<T>(IPackageIdentity packageIdentity, string indexName, out T value)
        {
            if (!_IdentityToIndexMap.TryGetValue(packageIdentity, out int packageIndex))
            {
                throw new KeyNotFoundException();
            }

            return Indexes.TrySimpleKeyLookup(packageIndex, indexName, out value);
        }

        public bool TryPackageLookupByCustomKey<T>(T key, string indexName, out IPackageIdentity value)
        {
            if (Indexes.TryPackageLookupByCustomKey(key, indexName, out int packageIndex))
            {
                return _IndexToIdentityMap.TryGetValue(packageIndex, out value);
            }
            else
            {
                value = null;
                return false;
            }
        }

        public bool TryPackageListLookupByCustomKey<T>(T key, string indexName, out List<IPackageIdentity> value)
        {
            if (Indexes.TryPackageListLookupByCustomKey(key, indexName, out List<int> packageIndex))
            {
                value = packageIndex.Select(index => _IndexToIdentityMap[index]).ToList();
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public List<IndexDefinition> GetAvailableIndexes()
        {
            return Indexes.GetLoadedIndexes();
        }

        /// <inheritdoc cref="IMetadataStore.ReIndex"/>
        public void ReIndex()
        {
            CheckIndex(true);
        }

        public bool TryListKeyLookup<T>(IPackageIdentity packageIdentity, string indexName, out List<T> value)
        {
            if (!_IdentityToIndexMap.TryGetValue(packageIdentity, out int packageIndex))
            {
                throw new KeyNotFoundException();
            }

            return Indexes.TryListKeyLookup<T>(packageIndex, indexName, out value);
        }

        public bool ContainsMetadata(IPackageIdentity packageIdentity)
        {
            return _IdentityToIndexMap.TryGetValue(packageIdentity, out var _);
        }

        public Stream GetMetadata(IPackageIdentity packageIdentity)
        {
            if (!_IdentityToIndexMap.TryGetValue(packageIdentity, out int packageIndex))
            {
                throw new KeyNotFoundException();
            }

            var deltaIndex = GetDeltaIndexFromPackageIndex(packageIndex);
            return DeltaMetadataStores[deltaIndex].GetMetadata(packageIdentity);
        }

        public List<T> GetFiles<T>(IPackageIdentity packageIdentity)
        {
            if (!_IdentityToIndexMap.TryGetValue(packageIdentity, out int packageIndex))
            {
                throw new KeyNotFoundException();
            }

            var deltaIndex = GetDeltaIndexFromPackageIndex(packageIndex);
            return DeltaMetadataStores[deltaIndex].GetFiles<T>(packageIdentity);
        }

        public int GetPackageIndex(IPackageIdentity packageIdentity)
        {
            if (_IdentityToIndexMap.TryGetValue(packageIdentity, out int packageIndex))
            {
                return packageIndex;
            }
            else
            {
                return -1;
            }
        }

        public IPackage GetPackage(int packageIndex)
        {
            if (_IndexToIdentityMap.TryGetValue(packageIndex, out var packageIdentity))
            {
                if (PartitionRegistration.TryGetPartitionFromPackageId(packageIdentity, out var partitionDefinition))
                {
                    return partitionDefinition.Factory.FromStore(_PackageTypeIndex[packageIndex], packageIdentity, this, this);
                }
                else
                {
                    throw new NotImplementedException($"The package belongs to a partition that was not registered: {packageIdentity.Partition}");
                }
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }

        public void CopyTo(IMetadataSink destination, IMetadataFilter filter, CancellationToken cancelToken)
        {
            var packagesMatchingFilter = filter.Apply(this);

            var packagesIdsToCopy = packagesMatchingFilter.Select(p => p.Id);
            if (destination is IMetadataStore destinationPackageStore)
            {
                packagesIdsToCopy = packagesIdsToCopy.Except(destinationPackageStore.GetPackageIdentities()).ToList();
            }

            var packagesToAdd = packagesIdsToCopy.Select(id => GetPackage(id));
            destination.AddPackages(packagesToAdd);
        }

        public IReadOnlyList<IPackage> GetPendingPackages()
        {
            return PendingPackages.AsReadOnly();
        }

        class MetadataEnumerator : IEnumerator<IPackage>
        {
            readonly DirectoryPackageStore _Source;
            readonly IEnumerator<IPackageIdentity> IdentitiesEnumerator;

            public MetadataEnumerator(DirectoryPackageStore metadataSource)
            {
                _Source = metadataSource;
                IdentitiesEnumerator = _Source.GetEnumerator();
            }

            public object Current => GetCurrent();

            IPackage IEnumerator<IPackage>.Current => GetCurrent();

            private IPackage GetCurrent() => _Source.GetPackage(IdentitiesEnumerator.Current);

            public void Dispose() => IdentitiesEnumerator.Dispose();

            public bool MoveNext() => IdentitiesEnumerator.MoveNext();

            public void Reset() => IdentitiesEnumerator.Reset();
        }
    }
}