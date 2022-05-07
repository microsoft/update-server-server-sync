// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.Storage.Index;
using System.Collections.Generic;

namespace Microsoft.PackageGraph.Partitions
{
    /// <summary>
    /// Definition for a partition in package graph. 
    /// Each partition in package graph handles packages (updates) from specific sources: Microsoft Update, Linux, Nuget, etc.
    /// </summary>
    class PartitionDefinition
    {
        /// <summary>
        /// The name of the partition
        /// </summary>
        public string Name;

        /// <summary>
        /// Partition factory for creating packages from raw metadata and other sources
        /// </summary>

        public IPackageFactory Factory;

        /// <summary>
        /// True if packages from this partition have side-car metadata files that is not captured in IPackage
        /// </summary>
        public bool HasExternalContentFileMetadata;

        /// <summary>
        /// List of indexes that this partition can have over contained packages
        /// </summary>
        public List<IndexDefinition> Indexes;

        /// <summary>
        /// True if the partition can map package identities to an integer index for quick lookup
        /// </summary>
        public bool HandlesIdentities = true;
    }
}
