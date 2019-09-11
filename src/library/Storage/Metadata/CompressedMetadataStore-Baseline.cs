// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ICSharpCode.SharpZipLib.Zip;
using Microsoft.UpdateServices.Client;
using Microsoft.UpdateServices.Compression;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.Metadata.Prerequisites;
using Microsoft.UpdateServices.Storage;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.UpdateServices.Storage
{
    public partial class CompressedMetadataStore
    {
        /// <summary>
        /// The checksum of the baseline metadata source. A delta metadata source can only be merged into a baseline
        /// that matches this checksum
        /// </summary>
        [JsonProperty]
        private string BaselineChecksum { get; set; }

        /// <summary>
        /// The metadata source that is the baseline for this delta metadata source
        /// </summary>
        CompressedMetadataStore BaselineSource;

        private bool IsDeltaSource => !string.IsNullOrEmpty(BaselineChecksum);

        /// <summary>
        /// The largest update index present in the baseline; indexes are continuous and increasing
        /// </summary>
        [JsonProperty]
        private int BaselineIndexesEnd { get; set; }

        /// <summary>
        /// This metadata source's index in a chain of delta sources
        /// </summary>
        [JsonProperty]
        private ulong DeltaIndex { get; set; }

        /// <summary>
        /// The list of updates identites that are expected to exist in the baseline
        /// </summary>
        private SortedSet<Identity> BaselineIdentities { get; set; }

        // The baseline file path is build dynamically from the path of a delta file. The naming scheme is:
        // Base file: base_result.zip
        // Delta 1: base_result-1.zip
        // Delta 2: base_result-2.zip
        // The delta chain remains valid across file renames as long as the root of the file name stays the same.
        private string BaselineFilePath;

        /// <summary>
        /// Check if an index fall withing the range of indexes in the baseline file
        /// </summary>
        /// <param name="index">The index to check</param>
        /// <returns>True if the update index is present in the baseline, false otherwise</returns>
        private bool IsInBaseline(int index) => (index <= BaselineIndexesEnd);

        private void ValidateBaseline()
        {
            if (!TryGetDeltaIndexFromFilePath(FilePath, out ulong parsedIndex, out string baseFileName))
            {
                throw new Exception("Source file naming scheme is corrupt. Expected [version]-[rest of file name].");
            }

            if (parsedIndex != DeltaIndex)
            {
                throw new Exception($"Source file naming scheme is corrupt. Expected index {DeltaIndex} but file name indicates index {parsedIndex}");
            }

            BaselineFilePath =  baseFileName + (DeltaIndex == 1 ? "" : "-" + (DeltaIndex - 1).ToString()) + ".zip";

            if (!File.Exists(BaselineFilePath))
            {
                throw new Exception($"Delta file {BaselineFilePath} missing");
            }

            BaselineSource = Open(BaselineFilePath);

            if (BaselineSource == null)
            {
                throw new Exception($"Cannot open delta file {BaselineFilePath}");
            }

            if (BaselineChecksum != BaselineSource.Checksum)
            {
                throw new Exception($"Unexpected checksum in delta file {BaselineFilePath}. Expected {BaselineChecksum}, found {BaselineSource.Checksum}");
            }
        }

        private bool TryGetDeltaIndexFromFilePath(string filePath, out ulong deltaIndex, out string basename)
        {
            string indexPattern = @"^(?'base'.+)-(?'index'[0-9]+)\.zip$"; ;

            var match = Regex.Match(Path.GetFileName(filePath), indexPattern);
            if (match.Success)
            {
                basename = match.Groups["base"].Value;
                return ulong.TryParse(match.Groups["index"].Value, out deltaIndex);
            }
            else
            {
                deltaIndex = 0;
                basename = null;
                return false;
            }
        }

        /// <summary>
        /// Creates a new metadata source as a delta from a baseline metadata source
        /// </summary>
        /// <param name="baseline"></param>
        public CompressedMetadataStore(CompressedMetadataStore baseline)
        {
            Version = CurrentVersion;

            DeltaIndex = baseline.DeltaIndex + 1;

            // Generate a file name that follows the indexed naming scheme.
            if (DeltaIndex == 1)
            {
                // If this is the first delta from a baseline, take the file name and add -1 to the end (before the extension)
                FilePath = $"{Path.GetFileNameWithoutExtension(baseline.FilePath)}-{DeltaIndex}{Path.GetExtension(baseline.FilePath)}";
            }
            else
            {
                // If this is a higher delta from a baseline, make sure the current file matches the naming scheme
                if (!TryGetDeltaIndexFromFilePath(baseline.FilePath, out ulong parsedIndex, out string baseFileName))
                {
                    throw new Exception("Baseline file naming scheme is corrupt. Expected [file name]-[delta index].zip.");
                }

                // If it does, add the version to the base file name, before the extension
                FilePath = $"{baseFileName}-{DeltaIndex.ToString()}.zip";
            }

            OutputFile = new ZipOutputStream(File.Create(FilePath));

            BaselineSource = baseline;
            BaselineChecksum = baseline.Checksum;

            // Record the end of the index range for the baseline
            BaselineIndexesEnd = baseline.IndexToIdentity.Keys.Count - 1;

            BaselineIdentities = new SortedSet<Identity>(baseline.IndexToIdentity.Values);

            Identities = new SortedSet<Identity>(baseline.Identities);
            IndexToIdentity = new Dictionary<int, Identity>(baseline.IndexToIdentity);
            IdentityToIndex = new Dictionary<Identity, int>(baseline.IdentityToIndex);
            ProductsTree = new Dictionary<int, List<int>>(baseline.ProductsTree);
            UpdateTypeMap = new Dictionary<int, uint>(baseline.UpdateTypeMap);

            Updates = new ConcurrentDictionary<Identity, Update>(baseline.Updates);
            Categories = new ConcurrentDictionary<Identity, Update>(baseline.Categories);

            // Create update placeholders
            InstantiateUpdatePlaceholders();

            UpstreamSource = baseline.UpstreamSource;

            // Populate indexes
            UpdatesIndex = Updates;
            CategoriesIndex = Categories;
            ProductsIndex = Categories.Values.OfType<Product>().ToDictionary(p => p.Identity);
            ClassificationsIndex = Categories.Values.OfType<Classification>().ToDictionary(c => c.Identity);
            DetectoidsIndex = Categories.Values.OfType<Detectoid>().ToDictionary(d => d.Identity);

            Filter =  BaselineSource.Filter;
            CategoriesAnchor = BaselineSource.CategoriesAnchor;

            UpstreamAccountGuid = BaselineSource.UpstreamAccountGuid;
            UpstreamAccountName = BaselineSource.UpstreamAccountName;

            UpdateTitlesIndex = new Dictionary<int, string>();

            // Initialize bundles information
            OnDeltaStore_InitializeBundes();

            // Initialize prerequisites information
            OnDeltaStore_InitializePrerequisites();

            // Initialize update classification and product information
            OnDeltaStore_InitializeProductClassification();

            // Initialize files index
            OnDeltaStore_InitializeFilesIndex();

            // Initialize superseding index
            OnDeltaStore_InitializeSupersededIndex();

            // Initialize driver indexes
            OnDeltaStore_InitializeDriversIndex();
        }
    }
}
