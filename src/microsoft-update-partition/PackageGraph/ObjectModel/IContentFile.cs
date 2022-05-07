// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.PackageGraph.ObjectModel
{
    /// <summary>
    /// Represents a content file for an update.
    /// </summary>
    public interface IContentFile
    {
        /// <summary>
        /// Gets the name of the file
        /// </summary>
        /// <value>
        /// File name
        /// </value>
        string FileName { get; }

        /// <summary>
        /// Ges the file size, in bytes.
        /// </summary>
        /// <value>File size</value>
        UInt64 Size { get; }

        /// <summary>
        /// Gets the primary digest of a content file. 
        /// </summary>
        /// <value>Content file digest.</value>
        IContentFileDigest Digest { get; }

        /// <summary>
        /// Gets the default download URL for a file.
        /// </summary>
        string Source { get; }
    }
}