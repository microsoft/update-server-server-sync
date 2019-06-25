// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.UpdateServices.Query
{
    /// <summary>
    /// Stores the result of a query for updates, in a JSON friendly format
    /// </summary>
    public class QueryResult
    {
        /// <summary>
        /// Filter used for the query
        /// </summary>
        public QueryFilter Filter { get; set; }

        /// <summary>
        /// The updates returned by the query
        /// </summary>
        public List<Metadata.MicrosoftUpdate> Updates { get; set; }

        [JsonConstructor]
        private QueryResult()
        {

        }

        /// <summary>
        /// Create a query result for a categories query
        /// </summary>
        /// <param name="categories">The categories returned by the query</param>
        /// <param name="anchor">The anchor returned by the service</param>
        internal static QueryResult CreateCategoriesQueryResult(List<Metadata.MicrosoftUpdate> categories, string anchor)
        {
            return new QueryResult()
            {
                Filter = new QueryFilter()
                {
                    Anchor = anchor,
                    IsCategoriesQuery = true
                },
                Updates = categories
            };
        }

        /// <summary>
        /// Create a query result for an updates query
        /// </summary>
        /// <param name="filter">The filter used for the query</param>
        /// <param name="updates">The updates returned by the query</param>
        /// <param name="anchor">The anchor returned by the service</param>
        internal static QueryResult CreateUpdatesQueryResult(Query.QueryFilter filter, List<Metadata.MicrosoftUpdate> updates, string anchor)
        {
            return new QueryResult()
            {
                Filter = new QueryFilter()
                {
                    Anchor = anchor,
                    IsCategoriesQuery = false,
                    ProductsFilter = filter.ProductsFilter,
                    ClassificationsFilter = filter.ClassificationsFilter
                },
                Updates = updates
            };
        }

        /// <summary>
        /// Constructor for update metadata queries
        /// </summary>
        /// <param name="categoriesFilter">The categories filter used</param>
        /// <param name="classificationsFilter">The classifications filter used</param>
        /// <param name="updates">The query results</param>
        /// <param name="anchor">The anchor returned by the service</param>
        private QueryResult(List<Metadata.MicrosoftUpdate> categoriesFilter, List<Metadata.MicrosoftUpdate> classificationsFilter, List<Metadata.MicrosoftUpdate> updates, string anchor)
        {
            Filter = new QueryFilter(categoriesFilter, classificationsFilter, anchor);
            Updates = updates;
        }
    }
}
