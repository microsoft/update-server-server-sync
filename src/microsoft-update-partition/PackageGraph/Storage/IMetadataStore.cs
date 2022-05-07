// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.ObjectModel;
using System;
using System.Collections.Generic;

namespace Microsoft.PackageGraph.Storage
{
    /// <summary>
    /// <para>
    /// Store containing metadata for <see cref="IPackage"/> originating from any partition of the package graph.
    /// </para>
    /// </summary>
    public interface IMetadataStore : IEnumerable<IPackage>, IDisposable, IMetadataSource, IMetadataSink
    {
        /// <summary>
        /// Checks if the store contains a package by identity
        /// </summary>
        /// <param name="packageIdentity">Package identity</param>
        /// <returns>True if the store contains the package, false otherwise</returns>
        bool ContainsPackage(IPackageIdentity packageIdentity);

        /// <summary>
        /// Retrieves a list of all package identities in the store
        /// </summary>
        /// <returns>List of package identities</returns>
        List<IPackageIdentity> GetPackageIdentities();

        /// <summary>
        /// Gets a integer index for a package. This index is unique only in the context of the metadata source that retrieved it.
        /// </summary>
        /// <param name="packageIdentity"></param>
        /// <returns></returns>
        int GetPackageIndex(IPackageIdentity packageIdentity);

        /// <summary>
        /// Retrieves a package by package identity
        /// </summary>
        /// <param name="packageIdentity">Package identity</param>
        /// <returns></returns>
        IPackage GetPackage(IPackageIdentity packageIdentity);

        /// <summary>
        /// Retrieves a package by package index. The package index must originate from this store instance.
        /// </summary>
        /// <param name="packageIndex"></param>
        /// <returns></returns>
        IPackage GetPackage(int packageIndex);

        /// <summary>
        /// Flush the store to its underlying storage
        /// </summary>
        void Flush();

        /// <summary>
        /// Gets a list of packages pending indexing
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<IPackage> GetPendingPackages();

        /// <summary>
        /// True if re-indexing is required in a metadata store, false otherwise
        /// </summary>
        bool IsReindexingRequired { get; }

        /// <summary>
        /// True if the metadata in this store is indexed for fast queries; false otherwise
        /// </summary>
        bool IsMetadataIndexingSupported { get; }

        /// <summary>
        /// Re-indexes the metadata store, if supported.
        /// </summary>
        void ReIndex();

        /// <summary>
        /// Progress reporting for a long-running reindexing operation.
        /// </summary>
        event EventHandler<PackageStoreEventArgs> PackageIndexingProgress;
    }
}
