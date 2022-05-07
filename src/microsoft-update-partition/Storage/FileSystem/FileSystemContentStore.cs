// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PackageGraph.Storage.Local
{
    /// <summary>
    /// Downloads and stores update content as files on the local file system.
    /// </summary>
    public class FileSystemContentStore : IContentStore
    {
        /// <inheritdoc cref="IContentStore.Progress"/>
        public event EventHandler<ContentOperationProgress> Progress;

        /// <summary>
        /// Directory under which the store structure is created
        /// </summary>
        readonly string LocalPath;

        /// <summary>
        /// Root content directory name
        /// </summary>
        const string ContentDirectoryName = "content";

        string ContentDirectoryPath => Path.Combine(LocalPath, ContentDirectoryName);

        /// <inheritdoc cref="IContentStore.QueuedSize"/>
        public long QueuedSize => throw new NotImplementedException();

        /// <inheritdoc cref="IContentStore.DownloadedSize"/>
        public long DownloadedSize => throw new NotImplementedException();

        /// <inheritdoc cref="IContentStore.QueuedCount"/>
        public int QueuedCount => throw new NotImplementedException();

        /// <summary>
        /// Opens or creates a new file system based content store.
        /// If the specified directory does not exist, it will be created.
        /// </summary>
        /// <param name="path">Path where to create the store</param>
        public FileSystemContentStore(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            LocalPath = path;
        }

        /// <inheritdoc cref="IContentStore.Download(IEnumerable{IContentFile}, CancellationToken)"/>
        public void Download(IEnumerable<IContentFile> files, CancellationToken cancelToken)
        {
            var contentDownloader = new ContentDownloader();
            contentDownloader.OnDownloadProgress += ContentDownloader_OnDownloadProgress;

            var hashChecker = new ContentHash();
            hashChecker.OnHashingProgress += HashChecker_OnHashingProgress;

            var progressData = new ContentOperationProgress();

            foreach (var file in files)
            {
                progressData.CurrentOperation = PackagesOperationType.DownloadFileStart;
                progressData.File = file;
                Progress?.Invoke(this, progressData);

                if (Contains(file))
                {
                    progressData.CurrentOperation = PackagesOperationType.DownloadFileEnd;
                    Progress?.Invoke(this, progressData);
                }
                else
                {
                    // Create the directory structure where the file will be downloaded
                    var contentFilePath = GetUri(file);
                    var contentFileDirectory = Path.GetDirectoryName(contentFilePath);
                    if (!Directory.Exists(contentFileDirectory))
                    {
                        Directory.CreateDirectory(contentFileDirectory);
                    }

                    // Download the file (or resume and interrupted download)
                    contentDownloader.DownloadToFile(GetUri(file), file, cancelToken);

                    progressData.CurrentOperation = PackagesOperationType.DownloadFileEnd;
                    Progress?.Invoke(this, progressData);

                    progressData.CurrentOperation = PackagesOperationType.HashFileStart;
                    Progress?.Invoke(this, progressData);

                    // Check the hash; must match the strongest hash specified in the update metadata
                    if (hashChecker.Check(file, contentFilePath))
                    {
                        File.WriteAllText(GetUpdateFileMarkerPath(file.Digest), file.FileName);
                    }

                    progressData.CurrentOperation = PackagesOperationType.HashFileEnd;
                    Progress?.Invoke(this, progressData);
                }
            }
        }

        private void HashChecker_OnHashingProgress(object sender, ContentOperationProgress e)
        {
            Progress?.Invoke(this, e);
        }

        private void ContentDownloader_OnDownloadProgress(object sender, ContentOperationProgress e)
        {
            Progress?.Invoke(this, e);
        }

        /// <inheritdoc cref="IContentStore.Contains(IContentFile)"/>
        public bool Contains(IContentFile file)
        {
            return File.Exists(GetUpdateFileMarkerPath(file.Digest));
        }

        /// <inheritdoc cref="IContentStore.Get(IContentFile)"/>
        public Stream Get(IContentFile updateFile)
        {
            if (!Contains(updateFile))
            {
                throw new Exception("The requested file is not downloaded");
            }

            return File.OpenRead(GetUri(updateFile));
        }

        /// <summary>
        /// Returns the path to the file that marks whether an update content file was successfully downloaded.
        /// The marker file is written after the update content file is downloaded and its hash verified
        /// </summary>
        /// <param name="fileDigest">Update content digest for which to retrieve the marker file path</param>
        /// <returns>The marker file path. This file might not exist.</returns>
        private string GetUpdateFileMarkerPath(IContentFileDigest fileDigest)
        {
            return GetUri(fileDigest) + ".done";
        }

        /// <inheritdoc cref="IContentStore.GetUri(IContentFile)"/>
        public string GetUri(IContentFile updateFile)
        {
            if (updateFile.Digest == null)
            {
                throw new Exception("Cannot determine file path for update with no digest");
            }

            var contentSubDirectory = GetContentDirectoryName(updateFile.Digest);

            return Path.Combine(ContentDirectoryPath, contentSubDirectory, updateFile.Digest.HexString, updateFile.Digest.HexString);
        }

        /// <inheritdoc cref="IContentStore.GetUri(IContentFileDigest)"/>
        public string GetUri(IContentFileDigest fileDigest)
        {
            var contentSubDirectory = GetContentDirectoryName(fileDigest);

            return Path.Combine(ContentDirectoryPath, contentSubDirectory, fileDigest.HexString, fileDigest.HexString);
        }

        /// <summary>
        /// Returns the directory name under which an update file would be stored on disk.
        /// </summary>
        /// <param name="fileDigest">The update content digest.</param>
        /// <returns>Content parent directory name</returns>
        public static string GetContentDirectoryName(IContentFileDigest fileDigest)
        {
            byte[] hashBytes = Convert.FromBase64String(fileDigest.DigestBase64);
            return string.Format("{0:X}", hashBytes.Last());
        }

        /// <inheritdoc cref="IContentStore.DownloadAsync(IContentFile, CancellationToken)"/>
        public Task DownloadAsync(IContentFile file, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IContentStore.Contains(IContentFileDigest, out string)"/>
        public bool Contains(IContentFileDigest fileDigest, out string fileName)
        {
            var markerFilePath = GetUpdateFileMarkerPath(fileDigest);
            var exists = File.Exists(markerFilePath);

            if (exists)
            {
                fileName = File.ReadAllText(markerFilePath);
            }
            else
            {
                fileName = null;
            }

            return exists;
        }

        /// <inheritdoc cref="IContentStore.Get(IContentFileDigest)"/>
        public Stream Get(IContentFileDigest fileDigest)
        {
            if (!Contains(fileDigest, out var _))
            {
                throw new Exception("The requested file is not downloaded");
            }

            return File.OpenRead(GetUri(fileDigest));
        }
    }
}
