// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Prerequisites;
using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Storage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata
{
    /// <summary>
    /// <para>
    /// A filter that can be applied to Microsoft updates based on their metadata: title, hardware id, KB article, etc.
    /// </para>
    /// <para>
    /// Use this filter to filter updates when copying updates between <see cref="IMetadataSource"/> and <see cref="IMetadataStore"/>
    /// </para>
    /// </summary>
    public class MetadataFilter : IMetadataFilter
    {
        /// <summary>
        /// Get or set the Classification or Product filter. 
        /// </summary>
        /// <value>List of classification or product IDs (ID only, no revision)</value>
        public List<Guid> CategoryFilter;

        /// <summary>
        /// Get or set the ID filter.
        /// </summary>
        /// <value>List of update IDs (ID only, no revision)</value>
        public List<Guid> IdFilter;

        /// <summary>
        /// Get or set the title filter.
        /// </summary>
        /// <value>Title filter string</value>
        public string TitleFilter;

        /// <summary>
        /// Get or set whether to filter out superseded updates
        /// </summary>
        /// <value>True to skip superseded updates, false otherwise</value>
        public bool SkipSuperseded;

        /// <summary>
        /// Returns the first X results only
        /// </summary>
        /// <value>0 to include all updates, greater than 0 value to limit output.</value>
        public int FirstX;

        /// <summary>
        /// Returns only driver updates that match this hardware ID.
        /// </summary>
        /// <value>Hardware id string</value>
        public string HardwareIdFilter;

        /// <summary>
        /// Returns only driver updates that target this computer hardware ID
        /// </summary>
        /// <value>Computer hardware ID (GUID)</value>
        public Guid ComputerHardwareIdFilter;

        /// <summary>
        /// Get or set the KB article filter
        /// </summary>
        /// <value>List of KB article ids - numbers only</value>
        public List<string> KbArticleFilter;

        /// <summary>
        /// Initialize a new filter. An empty filter matches all updates or categories.
        /// </summary>
        public MetadataFilter()
        {

        }

        /// <summary>
        /// Create a filter from JSON
        /// </summary>
        /// <param name="source">The JSON string</param>
        /// <returns>A filter for metadata in a updates metadata source</returns>
        public static MetadataFilter FromJson(string source)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<MetadataFilter>(source);
        }

        /// <summary>
        /// Serializes this filter to JSON
        /// </summary>
        /// <returns>The JSON string</returns>
        public string ToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }

        /// <summary>
        /// Apply the filter to a <see cref="IMetadataSource"/> and returns the matching packages of the specified type.
        /// </summary>
        /// <typeparam name="T">Package type to query. The type must inherit <see cref="MicrosoftUpdatePackage"/></typeparam>
        /// <param name="source">The metadata store to filter</param>
        /// <returns>Matching packages</returns>
        public IEnumerable<T> Apply<T>(IMetadataStore source)  where T : MicrosoftUpdatePackage
        {
            IEnumerable<T> filteredUpdates;
            var updates = source.OfType<T>();

            if (!string.IsNullOrEmpty(HardwareIdFilter) || (Guid.Empty != ComputerHardwareIdFilter))
            {
                filteredUpdates = updates.Where(u => u is DriverUpdate);
            }
            else if (KbArticleFilter?.Count > 0)
            {
                filteredUpdates = updates.Where(u => u is SoftwareUpdate);
            }
            else
            {
                filteredUpdates = updates;
            }

            if (!string.IsNullOrEmpty(HardwareIdFilter))
            {
                filteredUpdates = filteredUpdates.Where(
                    u => (u as DriverUpdate)
                    .GetDriverMetadata()
                    .Any(metadata => metadata.HardwareID.Equals(HardwareIdFilter, StringComparison.OrdinalIgnoreCase)));
            }

            if (ComputerHardwareIdFilter != Guid.Empty)
            {
                filteredUpdates = filteredUpdates.Where(
                    u => (u as DriverUpdate)
                    .GetDriverMetadata()
                    .Any(metadata => metadata.DistributionComputerHardwareId.Contains(ComputerHardwareIdFilter)));
            }

            if (CategoryFilter?.Count > 0)
            {
                filteredUpdates = filteredUpdates.Where(u => u.Prerequisites != null);

                filteredUpdates = filteredUpdates
                    .Where(u =>
                    u.Prerequisites.OfType<AtLeastOne>()
                    .SelectMany(p => p.Simple)
                    .Select(s => s.UpdateId)
                    .Intersect(CategoryFilter).Any());
            }

            if (KbArticleFilter?.Count > 0)
            {
                var kbLookup = KbArticleFilter.ToHashSet();
                filteredUpdates = filteredUpdates.Where(u => kbLookup.Contains((u as SoftwareUpdate).KBArticleId));
            }

            // Apply the title filter
            if (!string.IsNullOrEmpty(TitleFilter))
            {
                var filterTokens = TitleFilter.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                filteredUpdates = filteredUpdates.Where(category => category.MatchTitle(filterTokens));
            }

            // Apply the id filter
            if (IdFilter?.Count > 0)
            {
                // Remove all updates that don't match the ID filter
                filteredUpdates = filteredUpdates.Where(u => IdFilter.Contains((u.Id as MicrosoftUpdatePackageIdentity).ID));
            }

            if (SkipSuperseded)
            {
                filteredUpdates = filteredUpdates
                    .Where(u => u is not SoftwareUpdate || 
                    (u is SoftwareUpdate softwareUpdate && (softwareUpdate.IsSupersededBy == null || softwareUpdate.IsSupersededBy.Count == 0)));
            }

            // Return first X matches, if requested
            if (FirstX > 0)
            {
                return filteredUpdates.Take(FirstX);
            }
            else
            {
                return filteredUpdates;
            }
        }

        /// <summary>
        /// Apply the filter to a <see cref="IMetadataSource"/> and returns matching packages of type <see cref="MicrosoftUpdatePackage"/>
        /// </summary>
        /// <param name="source">The metadata store to filter</param>
        /// <returns>Matching packages</returns>
        public IEnumerable<IPackage> Apply(IMetadataStore source)
        {
            return Apply<MicrosoftUpdatePackage>(source);
        }
    }
}
