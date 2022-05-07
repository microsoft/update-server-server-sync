// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Microsoft.PackageGraph.Storage
{
    /// <summary>
    /// Progress base class for various operations in a metadata store
    /// </summary>
    public class PackageStoreEventArgs : EventArgs
    {
        /// <summary>
        /// Operation total value
        /// </summary>
        public long Total { get; set; }

        /// <summary>
        /// Operation current value
        /// </summary>
        public long Current { get; set; }

        /// <summary>
        /// Constructor for a package store event
        /// </summary>
        public PackageStoreEventArgs() 
        { 
        }
    }

    /// <summary>
    /// <para>
    /// Interface implemented by update metadata sources. Metadata sources retrieve
    /// raw metadata to be ingested into the package graph object model and subsequently 
    /// stored locally and queries.
    /// </para>
    /// </summary>
    public interface IMetadataSource
    {
        /// <summary>
        /// Get raw metadata for a package identity
        /// </summary>
        /// <param name="packageIdentity">Package identity</param>
        /// <returns>Package raw metadata.</returns>
        Stream GetMetadata(IPackageIdentity packageIdentity);

        /// <summary>
        /// Checks if the source has metadata for a specific package ID
        /// </summary>
        /// <param name="packageIdentity">Package identity</param>
        /// <returns>True if the source has raw metadata</returns>
        bool ContainsMetadata(IPackageIdentity packageIdentity);


        /// <summary>
        /// Retrieves content files associated with a package
        /// </summary>
        /// <typeparam name="T">The type of content file applicable to the package, depending on the partition</typeparam>
        /// <param name="packageIdentity">The identity of the package</param>
        /// <returns>List of content files associated with the package</returns>
        List<T> GetFiles<T>(IPackageIdentity packageIdentity);

        /// <summary>
        /// Copies all package metadata from this source to the target metadata sink
        /// </summary>
        /// <param name="destination">Metadata destination</param>
        /// <param name="cancelToken">Cancellation token</param>
        void CopyTo(IMetadataSink destination, CancellationToken cancelToken);

        /// <summary>
        /// Copies select package metadata from this source to the target metadata sink
        /// </summary>
        /// <param name="destination">Metadata destination</param>
        /// <param name="filter">Filter to apply during copy</param>
        /// <param name="cancelToken">Cancellation token</param>
        void CopyTo(IMetadataSink destination, IMetadataFilter filter, CancellationToken cancelToken);

        /// <summary>
        /// Progress notification during metadata copy operations
        /// </summary>
        event EventHandler<PackageStoreEventArgs> MetadataCopyProgress;

        /// <summary>
        /// Progress notifications during opening the metadata source
        /// </summary>
        event EventHandler<PackageStoreEventArgs> OpenProgress;
    }
}