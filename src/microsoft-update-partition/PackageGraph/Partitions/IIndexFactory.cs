// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.Storage;
using Microsoft.PackageGraph.Storage.Index;

namespace Microsoft.PackageGraph.Partitions
{
    /// <summary>
    /// Interface for creation of indexes for a <see cref="IMetadataSource"/>
    /// This interface must be implemented by a partition to enable indexing of updates (packages) originating
    /// from the partition
    /// </summary>
    interface IIndexFactory
    {
        /// <summary>
        /// Creates an index that will be attached to a <see cref="IMetadataSource"/>
        /// </summary>
        /// <param name="definition">The index definition</param>
        /// <param name="container">The container that will contain the newly created index</param>
        /// <returns></returns>
        IIndex CreateIndex(IndexDefinition definition, IIndexContainer container);
    }
}
