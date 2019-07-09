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
    /// Represents a filter used for quering updates. Combines categories and classifications filters
    /// <para>To create a QueryFilter, query the categories on the upstream server first.</para>
    /// </summary>
    /// <example>
    /// <code>
    /// var server = new UpstreamServerClient(Endpoint.Default);
    /// 
    /// var categories = await server.GetCategories();
    /// 
    /// // Create a filter for first product and all classifications
    /// var filter = new QueryFilter(
    ///     categories.Updates.OfType&lt;Product&gt;().Take(1),
    ///     categories.Updates.OfType&lt;Classification&gt;());
    ///
    /// // Get updates
    /// var updatesQueryResult = await server.GetUpdates(filter);
    /// </code>
    /// </example>
    public class QueryFilter
    {
        /// <summary>
        /// Gets the list of products in the filter.
        /// </summary>
        /// <value>List of product identities.</value>
        public List<Identity> ProductsFilter { get; internal set; }

        /// <summary>
        /// Gets the list of classifications in the filter.
        /// </summary>
        /// <value>List of classification identities.</value>
        public List<Identity> ClassificationsFilter { get; internal set; }

        /// <summary>
        /// Server returned anchor for this query. Save it to use in the future when using this filter
        /// </summary>
        internal string Anchor { get; set; }

        /// <summary>
        /// True if this filter was used to query categories; false otherwise.
        /// </summary>
        internal bool IsCategoriesQuery { get; set; }

        [JsonConstructor]
        internal QueryFilter()
        {
            ProductsFilter = new List<Identity>();
            ClassificationsFilter = new List<Identity>();
            Anchor = null;
        }

        /// <summary>
        /// Create a filter with an anchor and no categories or products
        /// </summary>
        /// <param name="anchor"></param>
        internal QueryFilter(string anchor)
        {
            ProductsFilter = new List<Identity>();
            ClassificationsFilter = new List<Identity>();
            Anchor = anchor;
        }

        /// <summary>
        /// Create a filter that contains categories or classifications
        /// </summary>
        /// <param name="categories">The categories filter used</param>
        /// <param name="classifications">The classifications filter used</param>
        /// <param name="anchor">The anchor received from the service after the query</param>
        internal QueryFilter(List<Metadata.Update> categories, List<Metadata.Update> classifications, string anchor)
        {
            ProductsFilter = categories.Select(cat => cat.Identity).ToList();
            ClassificationsFilter = classifications.Select(classification => classification.Identity).ToList();
            Anchor = anchor;
        }

        /// <summary>
        /// Initialize a new QueryFilter from the specified products and categories.
        /// </summary>
        /// <param name="products">The products filter</param>
        /// <param name="classifications">The classifications filter</param>
        public QueryFilter(IEnumerable<Product> products, IEnumerable<Classification> classifications)
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
        /// Override Equals for 2 QueryFilter objects
        /// </summary>
        /// <param name="obj">Other QueryFilter</param>
        /// <returns>
        /// <para>True if the two QueryFilter are identical (same product and classification filters).</para>
        /// <para>False otherwise</para>
        /// </returns>
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

        /// <summary>
        /// Override equality operator QueryFilter objects
        /// </summary>
        /// <param name="lhs">Left QueryFilter</param>
        /// <param name="rhs">Right QueryFilter</param>
        /// <returns>
        /// <para>True if both lhs and rhs are QueryFilter and they contain the same classification and product filters</para>
        /// <para>False otherwise</para>
        /// </returns>
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

        /// <summary>
        /// Override inequality operator QueryFilter objects
        /// </summary>
        /// <param name="lhs">Left QueryFilter</param>
        /// <param name="rhs">Right QueryFilter</param>
        /// <returns>
        /// <para>True if both lhs and rhs are not QueryFilter or they contain different classification and product filters</para>
        /// <para>False otherwise</para>
        /// </returns>
        public static bool operator !=(QueryFilter lhs, QueryFilter rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Returns a hash code based on the hash codes of the contained classification and products
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            int hash = 0;
            this.ProductsFilter.ForEach(cat => hash |= cat.GetHashCode());
            this.ClassificationsFilter.ForEach(cat => hash |= cat.GetHashCode());

            return hash;
        }
    }
}
