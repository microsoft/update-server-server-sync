// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ICSharpCode.SharpZipLib.Zip;
using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Partitions;
using Microsoft.PackageGraph.Storage.Index;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.PackageGraph.Storage.Local
{
    class ZipStreamIndexContainer : IIndexStreamContainer
    {
        Dictionary<string, IIndex> Indexes;
        List<IndexDefinition> UnknownIndexes;
        List<IndexDefinition> MissingIndexes;

        private ZipFile InputFile;

        private const string TocFileName = ".toc";

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

        private IndexContainerStatus Status;

        private ZipStreamIndexContainer()
        {

        }

        public static ZipStreamIndexContainer Open(Stream source)
        {
            var indexContainer = new ZipStreamIndexContainer();
            try
            {
                indexContainer.InputFile = new ZipFile(source, false);
                indexContainer.ReadTableOfContents();
            }
            catch(Exception)
            {
                indexContainer.Status = IndexContainerStatus.Corrupt;
                indexContainer.ResetIndex();
                indexContainer.InputFile = null;
            }

            return indexContainer;
        }

        public static ZipStreamIndexContainer Create()
        {
            var indexContainer = new ZipStreamIndexContainer();
            indexContainer.ResetIndex();
            return indexContainer;
        }

        public void ResetIndex()
        {
            if (InputFile != null)
            {
                InputFile.Close();
                InputFile = null;
            }

            CreateTableOfContents();
            CreateAllKnownIndexes();
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

        public void CloseInput()
        {
            if (InputFile != null)
            {
                InputFile.Close();
                InputFile = null;
            }
        }

        private static string GetIndexPathFromDefinition(IndexDefinition definition)
        {
            string indexEntry = "";
            if (!string.IsNullOrEmpty(definition.PartitionName))
            {
                indexEntry = definition.PartitionName + "/";
            }

            indexEntry += definition.Name;

            return indexEntry;
        }

        public void Save(Stream destination)
        {
            using var compressor = new ZipOutputStream(destination);
            foreach (var index in Indexes.Values)
            {
                compressor.PutNextEntry(new ZipEntry(GetIndexPathFromDefinition(index.Definition)));
                index.Save(compressor);
                compressor.CloseEntry();
            }

            TOC.ContainedIndexes = Indexes.Select(index => index.Value.Definition).ToList();

            compressor.PutNextEntry(new ZipEntry(TocFileName));
            using (var tocWriter = new StreamWriter(compressor, Encoding.UTF8, 4096, true))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(tocWriter, TOC);
            }
            compressor.CloseEntry();
        }

        private void CreateTableOfContents()
        {
            Indexes = new Dictionary<string, IIndex>();
            UnknownIndexes = new List<IndexDefinition>();
            MissingIndexes = new List<IndexDefinition>();
            TOC = new IndexTableOfContents
            {
                Version = IndexTableOfContents.CurrentVersion
            };
        }

        private void ReadTableOfContents()
        {
            var entryIndex = InputFile.FindEntry(TocFileName, true);
            if (entryIndex < 0)
            {
                Status = IndexContainerStatus.MissingToc;
                throw new KeyNotFoundException();
            }

            var tocEntry = InputFile.GetInputStream(entryIndex);
            using var tocReader = new StreamReader(tocEntry);
            var jsonSerializer = new JsonSerializer();
            var toc = jsonSerializer.Deserialize(tocReader, typeof(IndexTableOfContents)) as IndexTableOfContents;
            if (toc.Version == IndexTableOfContents.CurrentVersion)
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
                TOC = new IndexTableOfContents();
                Status = IndexContainerStatus.BadTocVersion;
            }
        }

        public bool TryGetIndex(string name, out IIndex index)
        {
            return Indexes.TryGetValue(name, out index);
        }

        public bool TryGetIndexReadStream(IndexDefinition index, out Stream indexStream)
        {
            if (InputFile == null)
            {
                indexStream = null;
                return false;
            }

            string indexPath = GetIndexPathFromDefinition(index);
            var entryIndex = InputFile.FindEntry(indexPath, true);
            if (entryIndex < 0)
            {
                indexStream = null;
                return false;
            }

            indexStream = InputFile.GetInputStream(entryIndex);
            return true;
        }

        public IndexContainerStatus GetStatus()
        {
            if (Status != IndexContainerStatus.Valid)
            {
                return Status;
            }

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
                    (PartitionRegistration.TryGetPartition(index.Definition.PartitionName, out _) &&
                    package.Id.Partition.Equals(index.Definition.PartitionName)))
                {
                    index.IndexPackage(package, packageIndex);
                }
            }
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
                .Where(index => index.Tag.Contains("stream"))
                .ToList();
        }
    }
}
