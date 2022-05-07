// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;

namespace Microsoft.PackageGraph.Storage.Local
{
    /// <summary>
    /// Creates an instance of <see cref="IMetadataStore"/> that stores update metadata locally in a specified directory.
    /// </summary>
    public abstract class PackageStore
    {
        /// <summary>
        /// Opens an exiting IMetadataStore from the specified directory.
        /// </summary>
        /// <param name="path">Path to the directory containing the IMetadataStore to open.</param>
        /// <returns>An instance of IMetadataStore</returns>
        /// <exception cref="DirectoryNotFoundException">If the directory does not exist or does not contain a valid IMetadataStore.</exception>
        public static IMetadataStore Open(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException(path);
            }

            return new DirectoryPackageStore(path, FileMode.Open);
        }

        /// <summary>
        /// Opens an existing IMetadataStore from the specified directory. If a store does not exist,
        /// or the directory does not exist, a new store is created.
        /// </summary>
        /// <param name="path">Path to the directory to open or create</param>
        /// <returns>An instance of IMetadataStore</returns>
        public static IMetadataStore OpenOrCreate(string path)
        {
            return new DirectoryPackageStore(path, FileMode.OpenOrCreate);
        }

        /// <summary>
        /// Checks if a IMetadataStore exists in the specified directory
        /// </summary>
        /// <param name="path">Path to the directory to check.</param>
        /// <returns>True if a store exists under the directory, false otherwise</returns>
        public static bool Exists(string path)
        {
            return DirectoryPackageStore.Exists(path);
        }
    }
}