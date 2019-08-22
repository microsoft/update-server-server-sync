// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Represents a filter that can be applied to update metadata
    /// </summary>
    public class MetadataFilter
    {
        /// <summary>
        /// Get or set the Classification filter. 
        /// </summary>
        /// <value>List of classification IDs (ID only, no revision)</value>
        public List<Guid> ClassificationFilter;

        /// <summary>
        /// Get or set the Product filter.
        /// </summary>
        /// <value>List of product IDs (ID only, no revision)</value>
        public List<Guid> ProductFilter;

        /// <summary>
        /// Get of set the ID filter.
        /// </summary>
        /// <value>List of update IDs (ID only, no revision)</value>
        public List<Guid> IdFilter;

        /// <summary>
        /// Get or set the title filter.
        /// </summary>
        /// <value>Title filter string</value>
        public string TitleFilter;

        /// <summary>
        /// Get or set whether to filter or not superseded updates
        /// </summary>
        /// <value>True to skip superseded updates, false otherwise</value>
        public bool SkipSuperseded;

        /// <summary>
        /// Returns the first X results only
        /// </summary>
        /// <value>0 to include all updates, greater than 0 value to limit output.</value>
        public int FirstX;

        /// <summary>
        /// Initialize a new filter. A newly initialized filter matches all updates or categories.
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

        internal static List<Update> FilterUpdatesList(IEnumerable<Update> updates, MetadataFilter filter)
        {
            var filteredUpdates = new List<Update>(updates);

            if (filter.ClassificationFilter.Count > 0)
            {
                filteredUpdates.RemoveAll(u => !u.HasClassification);

                // Apply the classification filter
                foreach (var classificationId in filter.ClassificationFilter)
                {
                    filteredUpdates.RemoveAll(u => !u.ClassificationIds.Contains(classificationId));
                }
            }

            if (filter.ProductFilter.Count > 0)
            {
                filteredUpdates.RemoveAll(u => !u.HasProduct);

                // Apply the product filter
                foreach (var productId in filter.ProductFilter)
                {
                    filteredUpdates.RemoveAll(u => !u.ProductIds.Contains(productId));
                }
            }

            // Apply the title filter
            if (!string.IsNullOrEmpty(filter.TitleFilter))
            {
                var filterTokens = filter.TitleFilter.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                filteredUpdates.RemoveAll(category => !category.MatchTitle(filterTokens));
            }

            // Apply the id filter
            if (filter.IdFilter.Count > 0)
            {
                // Remove all updates that don't match the ID filter
                filteredUpdates.RemoveAll(u => !filter.IdFilter.Contains(u.Identity.Raw.UpdateID));
            }

            if (filter.SkipSuperseded)
            {
                filteredUpdates.RemoveAll(u => u.IsSuperseded);
            }

            // Return first X matches, if requested
            if (filter.FirstX > 0)
            {
                return filteredUpdates.Take(Math.Min(filter.FirstX, filteredUpdates.Count)).ToList();
            }
            else
            {
                return filteredUpdates;
            }
        }
    }
}
