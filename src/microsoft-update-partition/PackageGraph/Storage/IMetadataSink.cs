// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.ObjectModel;
using System;
using System.Collections.Generic;

namespace Microsoft.PackageGraph.Storage
{
    /// <summary>
    /// Interface for an object that can store packages metadata
    /// </summary>
    public interface IMetadataSink : IDisposable
    {
        /// <summary>
        /// Adds a list of packages to the packages collection
        /// </summary>
        /// <param name="packages">The packages to add to this sink</param>
        void AddPackages(IEnumerable<IPackage> packages);

        /// <summary>
        /// Adds a package to the packages collection
        /// </summary>
        /// <param name="package">The package to add</param>
        void AddPackage(IPackage package);

        /// <summary>
        /// Provides progress notifications for the AddPackages operation
        /// </summary>
        event EventHandler<PackageStoreEventArgs> PackagesAddProgress;
    }
}
