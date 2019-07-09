// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.UpdateServices.Storage
{
    /// <summary>
    /// Represents a filter that can be applied to an updates repository to filter updates
    /// </summary>
    public class RepositoryFilter
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
        /// Initialize a new filter. A newly initialized filter matches all updates or categories in a repository.
        /// </summary>
        public RepositoryFilter()
        {

        }

        /// <summary>
        /// Create a repository filter from JSON
        /// </summary>
        /// <param name="source">The JSON string</param>
        /// <returns>A repository filter</returns>
        public static RepositoryFilter FromJson(string source)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<RepositoryFilter>(source);
        }

        /// <summary>
        /// Serializes this filter to JSON
        /// </summary>
        /// <returns>The JSON string</returns>
        public string ToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
    }
}
