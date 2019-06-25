// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.UpdateServices.Query
{
    /// <summary>
    /// Filter used when quering for updates. Combines a categories and classification filters
    /// </summary>
    public class QueryFilter
    {
        /// <summary>
        /// The categories filter
        /// </summary>
        public List<MicrosoftUpdateIdentity> ProductsFilter { get; set; }

        /// <summary>
        /// The classifications query
        /// </summary>
        public List<MicrosoftUpdateIdentity> ClassificationsFilter { get; set; }

        /// <summary>
        /// Server returned anchor for this query. Save it to use in the future when using this filter
        /// </summary>
        public string Anchor { get; set; }

        /// <summary>
        /// True if this filter was used to query categories; false otherwise.
        /// </summary>
        public bool IsCategoriesQuery { get; set; }

        [JsonConstructor]
        internal QueryFilter()
        {
            ProductsFilter = new List<MicrosoftUpdateIdentity>();
            ClassificationsFilter = new List<MicrosoftUpdateIdentity>();
            Anchor = null;
        }

        /// <summary>
        /// Create a filter with an anchor and no categories or products
        /// </summary>
        /// <param name="anchor"></param>
        internal QueryFilter(string anchor)
        {
            ProductsFilter = new List<MicrosoftUpdateIdentity>();
            ClassificationsFilter = new List<MicrosoftUpdateIdentity>();
            Anchor = anchor;
        }

        /// <summary>
        /// Create a filter that contains categories or classifications
        /// </summary>
        /// <param name="categories">The categories filter used</param>
        /// <param name="classifications">The classifications filter used</param>
        /// <param name="anchor">The anchor received from the service after the query</param>
        internal QueryFilter(List<Metadata.MicrosoftUpdate> categories, List<Metadata.MicrosoftUpdate> classifications, string anchor)
        {
            ProductsFilter = categories.Select(cat => cat.Identity).ToList();
            ClassificationsFilter = classifications.Select(classification => classification.Identity).ToList();
            Anchor = anchor;
        }

        /// <summary>
        /// Create a filter that contains categories or classifications
        /// </summary>
        /// <param name="categories">The products filter</param>
        /// <param name="classifications">The classifications filter</param>
        /// <param name="anchor">The anchor received from the service after the query</param>
        public QueryFilter(IEnumerable<MicrosoftProduct> products, IEnumerable<Classification> classifications)
        {
            ProductsFilter = products.Select(p => p.Identity).ToList();
            ClassificationsFilter = classifications.Select(classification => classification.Identity).ToList();
            Anchor = null;
            IsCategoriesQuery = false;
        }

        /// <summary>
        /// Creates a ServerSyncFilter object to be used with GetRevisionIdListAsync
        /// </summary>
        /// <returns>A ServerSyncFilter instance</returns>
        internal ServerSyncFilter ToServerSyncFilter()
        {
            ServerSyncFilter filter = new ServerSyncFilter();

            if (ProductsFilter.Count > 0)
            {
                filter.Categories = new IdAndDelta[ProductsFilter.Count];
                for (int i = 0; i < ProductsFilter.Count; i++)
                {
                    filter.Categories[i] = new IdAndDelta();

                    // Request deltas if we have an anchor from a previous query
                    filter.Categories[i].Delta = !string.IsNullOrEmpty(Anchor);

                    filter.Categories[i].Id = ProductsFilter[i].Raw.UpdateID;
                }
            }

            if (ClassificationsFilter.Count > 0)
            {
                filter.Classifications = new IdAndDelta[ClassificationsFilter.Count];
                for (int i = 0; i < ClassificationsFilter.Count; i++)
                {
                    filter.Classifications[i] = new IdAndDelta();

                    // Request deltas if we have an anchor from a previous query
                    filter.Classifications[i].Delta = !string.IsNullOrEmpty(Anchor);

                    filter.Classifications[i].Id = ClassificationsFilter[i].Raw.UpdateID;
                }
            }

            filter.Anchor = Anchor;

            return filter;
        }

        /// <summary>
        /// Equality comparer for 2 queries
        /// </summary>
        /// <param name="obj">Other query</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (!(obj is QueryFilter))
            {
                return false;
            }

            var other = obj as QueryFilter;
            if (this.ProductsFilter.Count != other.ProductsFilter.Count ||
                this.ClassificationsFilter.Count != other.ClassificationsFilter.Count ||
                this.IsCategoriesQuery != other.IsCategoriesQuery)
            {
                return false;
            }

            return this.ProductsFilter.All(cat => other.ProductsFilter.Contains(cat))
                && this.ClassificationsFilter.All(cat => other.ClassificationsFilter.Contains(cat));
        }

        public static bool operator ==(QueryFilter lhs, QueryFilter rhs)
        {
            if (object.ReferenceEquals(lhs, null))
            {
                return object.ReferenceEquals(rhs, null);
            }
            else
            {
                return lhs.Equals(rhs);
            }
        }

        public static bool operator !=(QueryFilter lhs, QueryFilter rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            int hash = 0;
            this.ProductsFilter.ForEach(cat => hash |= cat.GetHashCode());
            this.ClassificationsFilter.ForEach(cat => hash |= cat.GetHashCode());

            return hash;
        }
    }
}
