// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.UpdateServices.Query
{
    /// <summary>
    /// Stores the result of a query for updates, in a JSON friendly format
    /// </summary>
    public class QueryResult : IDisposable
    {
        /// <summary>
        /// Filter used for the query
        /// </summary>
        public QueryFilter Filter { get; set; }

        /// <summary>
        /// The updates returned by the query
        /// </summary>
        public List<Metadata.MicrosoftUpdate> Updates { get; set; }

        /// <summary>
        /// Temporary directory that contains XML metadata for updates.
        /// XML data is written to disk to avoid running out of memory when sync'ing a large number
        /// of updates
        /// </summary>
        private readonly string TempDirectory;

        private QueryResult()
        {
            TempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(TempDirectory);
        }

        /// <summary>
        /// Create a query result for a categories query
        /// </summary>
        /// <param name="anchor">The anchor returned by the service</param>
        internal static QueryResult CreateCategoriesQueryResult(string anchor)
        {
            return new QueryResult()
            {
                Filter = new QueryFilter()
                {
                    Anchor = anchor,
                    IsCategoriesQuery = true
                },
                Updates = new List<Metadata.MicrosoftUpdate>()
            };
        }

        /// <summary>
        /// Create a query result for an updates query
        /// </summary>
        /// <param name="filter">The filter used for the query</param>
        /// <param name="anchor">The anchor returned by the service</param>
        internal static QueryResult CreateUpdatesQueryResult(Query.QueryFilter filter, string anchor)
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
                Updates = new List<Metadata.MicrosoftUpdate>()
            };
        }

        /// <summary>
        /// Adds an update to the query result. The XML metadata is written to disk to avoid running out of memory
        /// </summary>
        /// <param name="update">The update to add. The in-memory XML string is released after writing it to disk</param>
        public void AddUpdate(Metadata.MicrosoftUpdate update)
        {
            Updates.Add(update);

            // To balance the directory structure, spread XML metadata files across multiple subdirectories
            // based on the update index
            var xmlParentDirectory = Path.Combine(TempDirectory, GetUpdateIndex(update));
            if (!Directory.Exists(xmlParentDirectory))
            {
                Directory.CreateDirectory(xmlParentDirectory);
            }

            File.WriteAllText(GetUpdateXmlPath(update), update.XmlData);
            update.XmlData = null;
        }

        /// <summary>
        /// Returns the path to the update XML in the temporary directory
        /// </summary>
        /// <param name="update">The update to get the path for.</param>
        /// <returns>A fully qualified path to the XML file belonging to the specified update</returns>
        public string GetUpdateXmlPath(Metadata.MicrosoftUpdate update)
        {
            return Path.Combine(TempDirectory, GetUpdateIndex(update), update.Identity.ToString() + ".xml");
        }

        /// <summary>
        /// Returns an index for an update (number between 0 and 255) based on the update\s ID.
        /// </summary>
        /// <param name="update">The update to get the index for</param>
        /// <returns>String representation of the index</returns>
        private static string GetUpdateIndex(Metadata.MicrosoftUpdate update)
        {
            // The index is the last 8 bits of the update ID.
            return update.Identity.Raw.UpdateID.ToByteArray().Last().ToString();
        }

        /// <summary>
        /// Deletes the temporary directory that contains XML metadata
        /// </summary>
        public void Dispose()
        {
            if (Directory.Exists(TempDirectory))
            {
                Directory.Delete(TempDirectory, true);
            }
        }
    }
}
