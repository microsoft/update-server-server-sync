// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Storage.Index;
using System;
using System.Collections.Generic;

namespace Microsoft.PackageGraph.Storage
{
    /// <summary>
    /// Interface implemented by metadata stores that support metadata indexed lookups.
    /// </summary>
    interface IMetadataLookup
    {
        /// <summary>
        /// Query an index by package identity for simple typed data.
        /// </summary>
        /// <typeparam name="T">Simple type</typeparam>
        /// <param name="packageIdentity">The package identity to query for</param>
        /// <param name="indexName">The name of the index</param>
        /// <param name="value">Retrieved value, if found</param>
        /// <returns>True if the package was found in the index, false otherwise</returns>
        bool TrySimpleKeyLookup<T>(IPackageIdentity packageIdentity, string indexName, out T value);

        /// <summary>
        /// Query an index by package identity. Returns list data 
        /// </summary>
        /// <typeparam name="T">Element type for the list</typeparam>
        /// <param name="packageIdentity">The package identity to query for</param>
        /// <param name="indexName">The name of the index</param>
        /// <param name="value">Retrieved value, if found</param>
        /// <returns>True if the package was found in the index, false otherwise</returns>
        bool TryListKeyLookup<T>(IPackageIdentity packageIdentity, string indexName, out List<T> value);

        /// <summary>
        /// Query a package identity from an index by a custom key.
        /// </summary>
        /// <typeparam name="T">Key type</typeparam>
        /// <param name="key">Query key</param>
        /// <param name="indexName">The name of the index</param>
        /// <param name="value">The package identity associated with the key</param>
        /// <returns>True if the key was found in the index, false otherwise.</returns>
        bool TryPackageLookupByCustomKey<T>(T key, string indexName, out IPackageIdentity value);

        /// <summary>
        /// Queries a list of package identities from an index by a custom key
        /// </summary>
        /// <typeparam name="T">Key type</typeparam>
        /// <param name="key">Query key</param>
        /// <param name="indexName">The name of the index</param>
        /// <param name="value">List of packages found for the specified query key.</param>
        /// <returns>True if the key was found, false otherwise.</returns>
        bool TryPackageListLookupByCustomKey<T>(T key, string indexName, out List<IPackageIdentity> value);

        /// <summary>
        /// True if re-indexing is required in a metadata source, false otherwise
        /// </summary>
        bool IsReindexingRequired { get; }

        /// <summary>
        /// Re-indexes the metadata source
        /// </summary>
        void ReIndex();

        /// <summary>
        /// Progress reporting for a long-running reindexing operation.
        /// </summary>
        event EventHandler<PackageStoreEventArgs> PackageIndexingProgress;
    }
}
