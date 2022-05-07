// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.ObjectModel;
using System.Collections.Generic;

namespace Microsoft.PackageGraph.Storage
{
    /// <summary>
    /// Interface for filtering packages from a <see cref="IMetadataSource"/> or for selectively quering packages from a <see cref="IMetadataSource"/>
    /// </summary>
    public interface IMetadataFilter
    {
        /// <summary>
        /// Apply the filter to a <see cref="IMetadataSource"/> and returns the matching packages
        /// </summary>
        /// <param name="source">The metadata store to filter</param>
        /// <returns>Matching packages</returns>
        IEnumerable<IPackage> Apply(IMetadataStore source);
    }
}
