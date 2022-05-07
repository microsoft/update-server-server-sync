// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Azure.Storage.Blob;
using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Partitions;
using Microsoft.PackageGraph.Storage.Index;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Microsoft.PackageGraph.Storage.Azure
{
    class IndexContainer : IIndexStreamContainer
    {
        Dictionary<string, IIndex> Indexes;
        List<IndexDefinition> UnknownIndexes;
        List<IndexDefinition> MissingIndexes;

        private const string IndexVirtualDirectoryName = "index";
        private const string TocBlobName = IndexVirtualDirectoryName + "/toc.json";

        public bool IsDirty = false;

        public bool ReIndexingRequired => MissingIndexes.Count > 0;

        IndexTableOfContents TOC;

        public enum IndexContainerStatus
        {
            Valid,
            Corrupt,
            MissingToc,
            BadTocVersion,
            UnknownIndexes,
            BadIndexVersion,
            MissingIndexes,
        }

        readonly CloudBlobContainer ParentContainer;

        public IndexContainer(CloudBlobContainer container)
        {
            ParentContainer = container;
            ReadTableOfContents();
        }

        public void ResetIndex()
        {
            CreateTableOfContents();
            CreateAllKnownIndexes();
        }

        public static void Erase(CloudBlobContainer container)
        {
            var registeredIndexes = GetRegisteredIndexes();
            foreach(var registeredIndex in registeredIndexes)
            {
                var indexBlob = container.GetBlockBlobReference(GetIndexBlobNameFromDefinition(registeredIndex));
                indexBlob.DeleteIfExists();
            }

            var tocBlob = container.GetBlockBlobReference(TocBlobName);
            tocBlob.DeleteIfExists();
        }

        private void CreateAllKnownIndexes()
        {
            foreach(var partition in PartitionRegistration.GetAllPartitions())
            {
                foreach (var knownIndex in partition.Indexes)
                {
                    Indexes.Add(knownIndex.Name, knownIndex.Factory.CreateIndex(knownIndex, this));
                }
            }
        }

        private static string GetIndexBlobNameFromDefinition(IndexDefinition definition)
        {
            string indexEntry = IndexVirtualDirectoryName + "/";
            if (!string.IsNullOrEmpty(definition.PartitionName))
            {
                indexEntry += definition.PartitionName + "/";
            }

            indexEntry += definition.Name;

            return indexEntry;
        }

        public void Save()
        {
            bool indexesChanged = false;
            foreach (var index in Indexes.Values)
            {
                if (index.IsDirty)
                {
                    var indexBlob = ParentContainer.GetBlockBlobReference(GetIndexBlobNameFromDefinition(index.Definition));
                    using var indexStream = indexBlob.OpenWrite();
                    using var compressor = new GZipStream(indexStream, CompressionLevel.Optimal, true);
                    index.Save(compressor);
                    indexesChanged = true;
                }
            }

            if (indexesChanged || IsDirty)
            {
                TOC.ContainedIndexes = Indexes.Select(index => index.Value.Definition).ToList();

                var tocBlob = ParentContainer.GetBlockBlobReference(TocBlobName);
                using var tocStream = tocBlob.OpenWrite();
                using var tocWriter = new StreamWriter(tocStream, Encoding.UTF8, 4096, true);
                var serializer = new JsonSerializer();
                serializer.Serialize(tocWriter, TOC);
            }
        }

        private void CreateTableOfContents()
        {
            Indexes = new Dictionary<string, IIndex>();
            UnknownIndexes = new List<IndexDefinition>();
            MissingIndexes = new List<IndexDefinition>();
            TOC = new IndexTableOfContents
            {
                Version = IndexTableOfContents.CurrentVersion,
                IndexedPackages = new List<int>()
            };
        }

        public List<int> GetListOfMetadataIndexedPackages()
        {
            return TOC.IndexedPackages;
        }

        private void ReadTableOfContents()
        {
            var tocBlob = ParentContainer.GetBlockBlobReference(TocBlobName);
            if (!tocBlob.Exists())
            {
                ResetIndex();
                return;
            }

            using var blobReadStream = tocBlob.OpenRead();
            using var blobReader = new StreamReader(blobReadStream);
            var jsonSerializer = new JsonSerializer();
            IndexTableOfContents toc;

            try
            {
                toc = jsonSerializer.Deserialize(blobReader, typeof(IndexTableOfContents)) as IndexTableOfContents;
                if (toc.Version != IndexTableOfContents.CurrentVersion)
                {
                    toc = null;
                }
            }
            catch (Exception) { toc = null; }

            if (toc != null)
            {
                var registeredIndexes = GetRegisteredIndexes();

                UnknownIndexes = toc
                    .ContainedIndexes
                    .Where(index => registeredIndexes.Any(knownIndex => knownIndex == index))
                    .ToList();

                toc.ContainedIndexes.RemoveAll(index => !registeredIndexes.Contains(index));

                MissingIndexes = registeredIndexes.Where(index => !toc.ContainedIndexes.Contains(index)).ToList();

                Indexes = new Dictionary<string, IIndex>();
                foreach (var index in toc.ContainedIndexes)
                {
                    var registeredIndex = registeredIndexes.Find(ri => ri.Equals(index));
                    Indexes.Add(index.Name, registeredIndex.Factory.CreateIndex(index, this));
                }

                foreach (var missingIndex in MissingIndexes)
                {
                    Indexes.Add(missingIndex.Name, missingIndex.Factory.CreateIndex(missingIndex, this));
                }

                TOC = toc;
            }
            else
            {
                ResetIndex();
                IsDirty = true;
            }
        }

        public bool TryGetIndex(string name, out IIndex index)
        {
            return Indexes.TryGetValue(name, out index);
        }

        public bool TryGetIndexReadStream(IndexDefinition index, out Stream indexStream)
        {
            var indexBlob = ParentContainer.GetBlockBlobReference(GetIndexBlobNameFromDefinition(index));
            if (indexBlob.Exists())
            {
                indexStream = new GZipStream(indexBlob.OpenRead(), CompressionMode.Decompress);
                return true;
            }
            else
            {
                indexStream = null;
                return false;
            }
        }

        public IndexContainerStatus GetStatus()
        {
            if (UnknownIndexes.Count > 0)
            {
                return IndexContainerStatus.UnknownIndexes;
            }

            if (MissingIndexes.Count > 0)
            {
                return IndexContainerStatus.MissingIndexes;
            }

            return IndexContainerStatus.Valid;
        }

        public void IndexPackage(IPackage package, int packageIndex)
        {
            foreach(var index in Indexes.Values)
            {
                if (string.IsNullOrEmpty(index.Definition.PartitionName) ||
                    PartitionRegistration.TryGetPartition(index.Definition.PartitionName, out var _))
                {
                    index.IndexPackage(package, packageIndex);
                }
            }

            TOC.IndexedPackages.Add(packageIndex);
            IsDirty = true;
        }

        public bool TrySimpleKeyLookup<T>(int packageIndex, string indexName, out T value)
        {
            if (Indexes.TryGetValue(indexName, out IIndex index))
            {
                return (index as ISimpleMetadataIndex<int, T>).TryGet(packageIndex, out value);
            }
            else
            {
                value = default;
                return false;
            }
        }

        public bool TryPackageLookupByCustomKey<T>(T key, string indexName, out int packageIndex)
        {
            if (Indexes.TryGetValue(indexName, out IIndex index))
            {
                return (index as ISimpleMetadataIndex<T, int>).TryGet(key, out packageIndex);
            }
            else
            {
                packageIndex = -1;
                return false;
            }
        }

        public bool TryPackageListLookupByCustomKey<T>(T key, string indexName, out List<int> packageIndexs)
        {
            if (Indexes.TryGetValue(indexName, out IIndex index))
            {
                return (index as ISimpleMetadataIndex<T, List<int>>).TryGet(key, out packageIndexs);
            }
            else
            {
                packageIndexs = null;
                return false;
            }
        }

        public bool TryListKeyLookup<T>(int packageIndex, string indexName, out List<T> value)
        {
            if (Indexes.TryGetValue(indexName, out IIndex index))
            {
                return (index as ISimpleMetadataIndex<int, List<T>>).TryGet(packageIndex, out value);
            }
            else
            {
                value = null;
                return false;
            }
        }

        public List<IndexDefinition> GetLoadedIndexes()
        {
            return Indexes.Select(i => i.Value.Definition).ToList();
        }

        private static List<IndexDefinition> GetRegisteredIndexes()
        {
            return PartitionRegistration
                .GetAllPartitions()
                .SelectMany(partition => partition.Indexes)
                .Where(index => index.Tag.Equals("stream"))
                .ToList();
        }
    }
}
