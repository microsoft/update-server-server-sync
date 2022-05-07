// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Partitions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microsoft.PackageGraph.Storage.Local
{
    class DirectoryMetadataStore : IMetadataSink, IMetadataSource
    {
        readonly string TargetPath;
        
        private bool IsDisposed = false;

        readonly object WriteLock = new();

        public event EventHandler<PackageStoreEventArgs> MetadataCopyProgress;

#pragma warning disable 0067
        public event EventHandler<PackageStoreEventArgs> OpenProgress;
        public event EventHandler<PackageStoreEventArgs> PackagesAddProgress;
#pragma warning restore 0067

        public DirectoryMetadataStore(string path)
        {
            TargetPath = path;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private string GetPackageMetadataPath(IPackageIdentity identity)
        {
            return Path.Combine(TargetPath, "metadata", "partitions", identity.Partition, GetPackageIndex(identity), $"{identity.OpenIdHex}.xml");
        }

        private static string GetPackageFilesPath(IPackageIdentity identity)
        {
            return Path.Combine("filemetadata", "partitions", identity.Partition, GetPackageIndex(identity), $"{identity.OpenIdHex}.files.json");
        }

        private static string GetPackageIndex(IPackageIdentity identity)
        {
            // The index is the last 8 bits of the update ID.
            return identity.OpenId.Last().ToString();
        }

        public bool ContainsMetadata(IPackageIdentity packageIdentity)
        {
            var metadataPath = GetPackageMetadataPath(packageIdentity);
            return File.Exists(metadataPath);
        }

        public Stream GetMetadata(IPackageIdentity packageIdentity)
        {
            var metadataPath = GetPackageMetadataPath(packageIdentity);
            return GetEntryStream(metadataPath);
        }

        private static Stream GetEntryStream(string path)
        {
            if (File.Exists(path))
            {
                return File.OpenRead(path);
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
                IsDisposed = true;
            }
        }

        public void AddPackage(IPackage package)
        {
            lock(WriteLock)
            {
                WritePackageMetadata(package);

                if (PartitionRegistration.TryGetPartitionFromPackage(package, out var partitionDefinition) &&
                    partitionDefinition.HasExternalContentFileMetadata &&
                    package.Files.Any())
                {
                    WritePackageFiles(package);
                }
            }
        }

        private void WritePackageMetadata(IPackage package)
        {
            var metadataPath = GetPackageMetadataPath(package.Id);
            if (!Directory.Exists(Path.GetDirectoryName(metadataPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(metadataPath));
            }

            using var packageMetadata = File.Create(metadataPath);
            package.GetMetadataStream().CopyTo(packageMetadata);
        }

        private static void WritePackageFiles(IPackage package)
        {
            var filesFilePath = GetPackageFilesPath(package.Id);
            if (!Directory.Exists(Path.GetDirectoryName(filesFilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filesFilePath));
            }

            using var filesFile = File.CreateText(filesFilePath);
            var serializer = new JsonSerializer();
            serializer.Serialize(filesFile, package.Files);
        }

        public List<T> GetFiles<T>(IPackageIdentity packageIdentity)
        {
            if (PartitionRegistration.TryGetPartitionFromPackageId(packageIdentity, out var partitionDefinition) &&
                partitionDefinition.HasExternalContentFileMetadata)
            {
                var filesPath = GetPackageFilesPath(packageIdentity);

                if (File.Exists(filesPath))
                {
                    using var filesStream = File.OpenText(filesPath);
                    var serializer = new JsonSerializer();
                    return (serializer.Deserialize(filesStream, typeof(List<T>)) as List<T>);
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

        private List<KeyValuePair<string, PartitionDefinition>> GetPackagesList()
        {
            List<KeyValuePair<string, PartitionDefinition>> packagePaths =  new();

            var partitions = Directory.GetDirectories(Path.Combine(TargetPath, "metadata", "partitions"));
            foreach (var partition in partitions)
            {
                var partitionName = Path.GetFileName(partition);

                if (PartitionRegistration.TryGetPartition(partitionName, out var partitionDefinition))
                {
                    var packagesInPartition = Directory.GetFiles(partition);
                    foreach (var package in packagesInPartition)
                    {
                        packagePaths.Add(new KeyValuePair<string, PartitionDefinition>(package, partitionDefinition));
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
            return new MetadataEnumerator(GetPackagesList(), this);
        }

        public void CopyTo(IMetadataSink destination, CancellationToken cancelToken)
        {
            var packages = GetPackagesList();

            var progressArgs = new PackageStoreEventArgs() { Total = packages.Count, Current = 0 };
            MetadataCopyProgress?.Invoke(this, progressArgs);
            packages.AsParallel().ForAll(package =>
            {
                if (cancelToken.IsCancellationRequested)
                {
                    return;
                }

                destination.AddPackage(GetPackage(package.Key, package.Value.Name));

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
            readonly DirectoryMetadataStore _Source;
            readonly IEnumerator<KeyValuePair<string, PartitionDefinition>> PathsEnumerator;

            public MetadataEnumerator(List<KeyValuePair<string, PartitionDefinition>> paths, DirectoryMetadataStore metadataSource)
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