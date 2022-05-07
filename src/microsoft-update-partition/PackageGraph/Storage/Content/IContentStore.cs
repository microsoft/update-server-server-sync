// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PackageGraph.Storage
{
    /// <summary>
    /// Download, storage and retrieval for package content (update content). Stores <see cref="IContentFile"/>
    /// </summary>
    public interface IContentStore
    {
        /// <summary>
        /// Raised on progress for long running content store operations
        /// </summary>
        /// <value>
        /// Progress data.
        /// </value>
        event EventHandler<ContentOperationProgress> Progress;

        /// <summary>
        /// Checks if a content file has been downloaded
        /// </summary>
        /// <param name="file">File to check if it was downloaded</param>
        /// <returns>True if the file was downloaded, false otherwise</returns>
        bool Contains(IContentFile file);

        /// <summary>
        /// Checks if an update file (by hash) has been downloaded
        /// </summary>
        /// <param name="fileDigest">File hash to check if it was downloaded</param>
        /// <param name="fileName">If the store contains the file by hash, this parameter receives the original file name</param>
        /// <returns>True if the file was downloaded, false otherwise</returns>
        bool Contains(IContentFileDigest fileDigest, out string fileName);

        /// <summary>
        /// Gets a read only stream for an update content file
        /// </summary>
        /// <param name="updateFile">The update file to open</param>
        /// <returns>Read only stream for the requested update content file</returns>
        Stream Get(IContentFile updateFile);

        /// <summary>
        /// Gets a read only stream for an update content file (by hash)
        /// </summary>
        /// <param name="fileDigest">The update file (by hash) to open</param>
        /// <returns>Read only stream for the requested update content file</returns>
        Stream Get(IContentFileDigest fileDigest);

        /// <summary>
        /// Gets an URI for the content file (by hash)
        /// </summary>
        /// <param name="fileDigest">The update file hash</param>
        /// <returns>Uri to content file</returns>
        string GetUri(IContentFileDigest fileDigest);

        /// <summary>
        /// Gets the source URI for the update content file
        /// </summary>
        /// <param name="updateFile">The update file</param>
        /// <returns>Source Uri for the content file</returns>
        string GetUri(IContentFile updateFile);

        /// <summary>
        /// Downloads the specified update content files.
        /// </summary>
        /// <param name="files">List of update content files to download</param>
        /// <param name="cancelToken">Cancellation token for aborting the operation</param>
        void Download(IEnumerable<IContentFile> files, CancellationToken cancelToken);

        /// <summary>
        /// Downloads the specified update content file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        Task DownloadAsync(IContentFile file, CancellationToken cancelToken);

        /// <summary>
        /// The size in bytes of content left to be downloaded in the current download operation
        /// </summary>
        long QueuedSize { get; }

        /// <summary>
        /// The size in bytes of content downloaded during the current download operation
        /// </summary>
        long DownloadedSize { get;  }

        /// <summary>
        /// The count of content files left to be downloaded in the current download operation
        /// </summary>
        int QueuedCount { get; }
    }
}
