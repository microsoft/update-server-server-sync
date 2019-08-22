// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Metadata.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;

namespace Microsoft.UpdateServices.Storage
{
    /// <summary>
    /// Downloads and stores update content as files on the local file system.
    /// </summary>
    public class FileSystemContentStore : IUpdateContentSource, IUpdateContentSink
    {
        /// <summary>
        /// Notifications for download progress to the content store
        /// </summary>
        public event EventHandler<OperationProgress> Progress;

        /// <summary>
        /// Directory under which the store structure is created
        /// </summary>
        private string LocalPath;

        /// <summary>
        /// Root content directory name
        /// </summary>
        private const string ContentDirectoryName = "content";
        private string ContentDirectoryPath => Path.Combine(LocalPath, ContentDirectoryName);

        /// <summary>
        /// Opens or creates a new file system based content store.
        /// If the specified directory does not exist, it will be created.
        /// </summary>
        /// <param name="path"></param>
        public FileSystemContentStore(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            LocalPath = path;
        }

        /// <summary>
        /// Download content
        /// </summary>
        /// <param name="files">The files to download</param>
        public void Add(IEnumerable<UpdateFile> files)
        {
            var contentDownloader = new ContentDownloader();
            contentDownloader.OnDownloadProgress += ContentDownloader_OnDownloadProgress;

            var hashChecker = new ContentHash();
            hashChecker.OnHashingProgress += HashChecker_OnHashingProgress;

            var cancellationSource = new CancellationTokenSource();

            var progressData = new ContentOperationProgress();

            foreach (var file in files)
            {
                progressData.CurrentOperation = OperationType.DownloadFileStart;
                progressData.File = file;
                Progress?.Invoke(this, progressData);

                if (Contains(file))
                {
                    progressData.CurrentOperation = OperationType.DownloadFileEnd;
                    Progress?.Invoke(this, progressData);
                }
                else
                {
                    // Create the directory structure where the file will be downloaded
                    var contentFilePath = GetUpdateFilePath(file);
                    var contentFileDirectory = Path.GetDirectoryName(contentFilePath);
                    if (!Directory.Exists(contentFileDirectory))
                    {
                        Directory.CreateDirectory(contentFileDirectory);
                    }

                    // Download the file (or resume and interrupted download)
                    contentDownloader.DownloadToFile(GetUpdateFilePath(file), file, cancellationSource.Token);

                    progressData.CurrentOperation = OperationType.DownloadFileEnd;
                    Progress?.Invoke(this, progressData);

                    progressData.CurrentOperation = OperationType.HashFileStart;
                    Progress?.Invoke(this, progressData);

                    // Check the hash; must match the strongest hash specified in the update metadata
                    if (hashChecker.Check(file, contentFilePath))
                    {
                        var markerFile = File.Create(GetUpdateFileMarkerPath(file));
                        markerFile.Dispose();
                    }

                    progressData.CurrentOperation = OperationType.HashFileEnd;
                    Progress?.Invoke(this, progressData);
                }
            }
        }

        private void HashChecker_OnHashingProgress(object sender, OperationProgress e)
        {
            Progress?.Invoke(this, e);
        }

        private void ContentDownloader_OnDownloadProgress(object sender, OperationProgress e)
        {
            Progress?.Invoke(this, e);
        }

        /// <summary>
        /// Checks if an update file has been downloaded
        /// </summary>
        /// <param name="file">File to check if it was downloaded</param>
        /// <returns>True if the file was downloaded, false otherwise</returns>
        public bool Contains(UpdateFile file)
        {
            return File.Exists(GetUpdateFileMarkerPath(file));
        }

        /// <summary>
        /// Gets a read only stream for an update content file
        /// </summary>
        /// <param name="updateFile">The update file to open</param>
        /// <returns>Read only stream for the requested update content file</returns>
        public Stream Get(UpdateFile updateFile)
        {
            if (!Contains(updateFile))
            {
                throw new Exception("The requested file is not downloaded");
            }

            return File.OpenRead(GetUpdateFilePath(updateFile));
        }

        /// <summary>
        /// Returns the path to the file that marks whether an update content file was successfully downloaded.
        /// The marker file is written after the update content file is downloaded and its hash verified
        /// </summary>
        /// <param name="updateFile">Update content file for which to retrieve the marker file path</param>
        /// <returns>The marker file path. This file might not exist.</returns>
        private string GetUpdateFileMarkerPath(UpdateFile updateFile)
        {
            return GetUpdateFilePath(updateFile) + ".done";
        }

        /// <summary>
        /// Given an update file, returns the path to the file in local store
        /// </summary>
        /// <param name="updateFile">The file to get the path for</param>
        /// <returns>Fully qualified path to the file. The path might not exist.</returns>
        private string GetUpdateFilePath(UpdateFile updateFile)
        {
            if (updateFile.Digests.Count == 0)
            {
                throw new Exception("Cannot determine file path for update with no digest");
            }

            var contentSubDirectory = updateFile.GetContentDirectoryName();

            return Path.Combine(LocalPath, ContentDirectoryName, contentSubDirectory, updateFile.Digests[0].HexString, updateFile.Digests[0].HexString + Path.GetExtension(updateFile.FileName));
        }
    }
}
