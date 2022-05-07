// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Storage;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.PackageGraph.Partitions
{
    /// <summary>
    /// <para>
    /// Interface that allows creation of <see cref="IPackage"/> from raw metadata or from metadata stores.
    /// </para>
    /// <para>
    /// This interface allows extending the package graph object model to any package source: Microsoft Update, Linux Repo, NuGet, NPM, etc.
    /// Each such "partition" will implement its own package factory, capable of creating IPackage from metadata.
    /// If a partition implements this interface, other interfaces that handle query and storage of packages will automatically be able to handle packages from the partition.
    /// </para>
    /// <para>
    /// Package graph partitions implement this interface. Implement this partition when extending the package graph to
    /// other partitions (nuget, npm, etc.)
    /// </para>
    /// </summary>
    interface IPackageFactory
    {
        /// <summary>
        /// Creates an <see cref="IPackage"/> from the raw stream. The package will be associated with a specified metadata store
        /// </summary>
        /// <param name="metadataStream">The raw metadata.</param>
        /// <param name="backingMetadataStore">The store that contains the metadata.</param>
        /// <returns><see cref="IPackage"/></returns>
        IPackage FromStream(Stream metadataStream, IMetadataSource backingMetadataStore);

        /// <summary>
        /// Creates an <see cref="IPackage"/> by rehydrating it from a backing metadata store
        /// </summary>
        /// <param name="updateType">The update type. The type is opaque and partition specific.</param>
        /// <param name="id">The package identity.</param>
        /// <param name="store">Store where to lookup the package</param>
        /// <param name="metadataSource">Store that contains the raw metadata for the package</param>
        /// <returns><see cref="IPackage"/></returns>
        IPackage FromStore(int updateType, IPackageIdentity id, IMetadataLookup store, IMetadataSource metadataSource);

        /// <summary>
        /// Creates a <see cref="IPackage"/> from a URI.
        /// </summary>
        /// <param name="sourceUri">URI to the update raw metadata</param>
        /// <returns><see cref="IPackage"/></returns>
        IPackage FromSource(string sourceUri);

        /// <summary>
        /// Checks if a package can be created from the specified URI
        /// </summary>
        /// <param name="sourceUri">URI to the update raw metadata</param>
        /// <returns>True if a package graph partition can create a package from the URI, false otherwise</returns>
        bool CanCreatePackageFromSource(string sourceUri);

        /// <summary>
        /// Rehydrates a mapping of package identity to int index that was serialized to JSON.
        /// </summary>
        /// <param name="jsonStream">JSON stream</param>
        /// <returns>List of package identity to index mappings</returns>
        IEnumerable<KeyValuePair<int, IPackageIdentity>> IdentitiesFromJson(StreamReader jsonStream);

        /// <summary>
        /// Rehydrates a package identity from string.
        /// </summary>
        /// <param name="packageIdentityString">The string representation of the package id.</param>
        /// <returns>Rehydrated package identity</returns>
        IPackageIdentity IdentityFromString(string packageIdentityString);

        /// <summary>
        /// Filters the list of package identities to only those identities that belong to a partition.
        /// </summary>
        /// <param name="packageIdentities">List of package identities</param>
        /// <returns>List of package identities that are handled by a package graph partition.</returns>
        IEnumerable<KeyValuePair<int, IPackageIdentity>> FilterPartitionIdentities(Dictionary<int, IPackageIdentity> packageIdentities);

        /// <summary>
        /// Retrieves the partition-specific package type.
        /// </summary>
        /// <param name="package">IPackage</param>
        /// <returns>Package type; int; partition specific.</returns>
        int GetPackageType(IPackage package);
    }
}
