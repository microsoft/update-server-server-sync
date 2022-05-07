// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.PackageGraph.ObjectModel
{
    /// <summary>
    /// Interface that represents the unique identity of a package (update) in the object model.
    /// </summary>
    public interface IPackageIdentity : IComparable
    {
        /// <summary>
        /// The partition to which the update belongs. Possible values are Linux, Microsoft, Nuget, etc.
        /// </summary>
        string Partition { get; }

        /// <summary>
        /// A unique ID for a package across all partitions.
        /// Implementations of this interface expose a partition specific ID as well.
        /// </summary>
        byte[] OpenId { get; }

        /// <summary>
        /// HEX representation of the unique ID.
        /// </summary>
        string OpenIdHex { get; }
    }
}
