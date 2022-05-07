using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.PackageGraph.ObjectModel
{
    /// <summary>
    /// Generic interface for inspecting metadata associated with updates originating from Microsoft Update.
    /// Objects that implement this interfaces should be cast to their specialized types to obtain type specific metadata.
    /// </summary>
    public interface IPackage
    {
        /// <summary>
        /// Get the package's identity
        /// </summary>
        IPackageIdentity Id { get; }

        /// <summary>
        /// Get the package title
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Get the package description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the list of files (content) for a package
        /// </summary>
        /// <value>
        /// List of content files
        /// </value>
        IEnumerable<IContentFile> Files { get; }

        /// <summary>
        /// Package extended metadata
        /// </summary>
        Stream GetMetadataStream();
    }
}
