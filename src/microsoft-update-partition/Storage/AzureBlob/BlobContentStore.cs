// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Storage.Blob;
using Microsoft.PackageGraph.ObjectModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PackageGraph.Storage.Azure
{
    /// <summary>
    /// Implementation of <see cref="IContentStore"/> that downloads and stores update content in Azure Blob Storage
    /// </summary>
    public class BlobContentStore : IContentStore
    {
        /// <inheritdoc cref="IContentStore.Progress"/>
        public event EventHandler<ContentOperationProgress> Progress;

        private const long BlockSize = 64 * 1024 * 1024;

        readonly CloudBlobContainer ParentContainer;

        /// <summary>
        /// List of pending downloads
        /// </summary>
        public ConcurrentDictionary<string, IContentFile> PendingFileDownloads = new();

        /// <inheritdoc cref="IContentStore.QueuedSize"/>
        public long QueuedSize => _QueuedSize;

        /// <inheritdoc cref="IContentStore.DownloadedSize"/>
        public long DownloadedSize => _DownloadedSize;

        /// <inheritdoc cref="IContentStore.QueuedCount"/>
        public int QueuedCount => _QueuedCount;

        long _QueuedSize;
        long _DownloadedSize;
        int _QueuedCount;

        private BlobContentStore(CloudBlobContainer contentContainer)
        {
            ParentContainer = contentContainer;
        }

        /// <summary>
        /// Opens an exiting or creates a new <see cref="IContentStore"/> with storage in the specified Azure Blob account and container
        /// </summary>
        /// <param name="client">The Azure Blob client to use</param>
        /// <param name="containerName">The container name where to store update content</param>
        /// <returns></returns>
        public static BlobContentStore OpenOrCreate(CloudBlobClient client, string containerName)
        {
            var container = client.GetContainerReference(containerName);
            container.CreateIfNotExists();

            return new BlobContentStore(container);
        }

        /// <inheritdoc cref="IContentStore.Download(IEnumerable{IContentFile}, CancellationToken)"/>
        public void Download(IEnumerable<IContentFile> files, CancellationToken cancelToken)
        {
            var queuedFiles = new List<IContentFile>();
            foreach(var file in files)
            {
                if (PendingFileDownloads.TryAdd(file.Source, file))
                {
                    queuedFiles.Add(file);
                }
            }

            Interlocked.Add(ref _QueuedCount, queuedFiles.Count);

            Interlocked.Add(ref _QueuedSize, queuedFiles.Sum(f => (long)f.Size));

            var cancellationSource = new CancellationTokenSource();
            var progress = new ContentOperationProgress();
            

            Progress?.Invoke(this, progress);

            
            foreach (var file in queuedFiles)
            {
                progress.Maximum = (long)file.Size;
                progress.CurrentOperation = PackagesOperationType.DownloadFileStart;
                Progress?.Invoke(this, progress);

                if (Contains(file))
                {
                    Interlocked.Add(ref _DownloadedSize, (long)file.Size);
                    Interlocked.Decrement(ref _QueuedCount);

                    progress.Current = (long)file.Size;
                    progress.CurrentOperation = PackagesOperationType.DownloadFileEnd;
                    Progress?.Invoke(this, progress);

                    PendingFileDownloads.TryRemove(file.Source, out var completeFileRemoved);

                    continue;
                }

                progress.CurrentOperation = PackagesOperationType.DownloadFileProgress;
                var fileBlob = GetBlobForFile(file);

                using (var client = new HttpClient())
                {
                    var fileSizeOnServer = GetFileSizeOnSourceServer(client, file.Source, cancelToken);
                    if (cancelToken.IsCancellationRequested)
                    {
                        break;
                    }

                    if ((ulong)fileSizeOnServer != file.Size)
                    {
                        throw new Exception($"Mismatch in file size. Expected {file.Size}, server has {fileSizeOnServer}");
                    }

                    int startBlock = 0;
                    var blockCount = fileSizeOnServer / BlockSize + (fileSizeOnServer % BlockSize == 0 ? 0 : 1);
                    List<string> blockIdList;
                    if (fileBlob.Exists())
                    {
                        var fileBlocks = fileBlob.DownloadBlockList(BlockListingFilter.Uncommitted).ToList();

                        if (fileBlocks.Count <= blockCount)
                        {
                            startBlock = fileBlocks.Count;
                        }

                        blockIdList = fileBlocks.Select(b => b.Name).ToList();
                    }
                    else
                    {
                        blockIdList = new List<string>();
                    }

                    for (int i = startBlock; i < blockCount; i++)
                    {
                        var startOffset = i * BlockSize;
                        var blockSize = (fileSizeOnServer % BlockSize  != 0 && i == (blockCount  -1 )? fileSizeOnServer % BlockSize : BlockSize);

                        fileBlob.PutBlock(Convert.ToBase64String(BitConverter.GetBytes(i)), new Uri(file.Source), startOffset, blockSize, null);
                        blockIdList.Add(Convert.ToBase64String(BitConverter.GetBytes(i)));

                        if (cancellationSource.IsCancellationRequested)
                        {
                            break;
                        }

                        Interlocked.Add(ref _DownloadedSize, blockSize);
                        progress.Current += blockSize;
                        Progress?.Invoke(this, progress);
                    }

                    fileBlob.PutBlockList(blockIdList);
                    using var markerFile = GetBlobMarkerForFile(file).OpenWrite();
                    markerFile.Write(Convert.FromBase64String(file.Digest.DigestBase64));


                }

                Interlocked.Add(ref _DownloadedSize, (long)file.Size * -1);
                Interlocked.Add(ref _QueuedSize, (long)file.Size * -1);
                Interlocked.Decrement(ref _QueuedCount);
                progress.CurrentOperation = PackagesOperationType.DownloadFileEnd;
                Progress?.Invoke(this, progress);

                PendingFileDownloads.TryRemove(file.Source, out var downloadedFileRemoved);
            }
        }

        private static long GetFileSizeOnSourceServer(HttpClient client, string url, CancellationToken cancellationToken)
        {
            // First get the HEAD to check the server's size for the file
            long fileSizeOnServer;
            using (var request = new HttpRequestMessage { RequestUri = new Uri(url), Method = HttpMethod.Head })
            {
                using var headResponse = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).GetAwaiter().GetResult();
                if (!headResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to get HEAD of update from {url}: {headResponse.ReasonPhrase}");
                }

                fileSizeOnServer = headResponse.Content.Headers.ContentLength.Value;
            }

            return fileSizeOnServer;
        }

        /// <inheritdoc cref="IContentStore.Contains(IContentFile)"/>
        public bool Contains(IContentFile file)
        {
            return GetBlobMarkerForFile(file).Exists();
        }

        /// <inheritdoc cref="IContentStore.Get(IContentFile)"/>
        public Stream Get(IContentFile contentFile)
        {
            var doneMarker = GetBlobMarkerForFile(contentFile);
            if (doneMarker.Exists())
            {
                return GetBlobForFile(contentFile).OpenRead();
            }
            else
            {
                throw new Exception("The requested file is not available");
            }
        }

        private CloudBlockBlob GetBlobMarkerForFile(IContentFile updateFile)
        {
            return ParentContainer.GetBlockBlobReference(updateFile.Digest.HexString.ToLower() + ".complete");
        }

        private CloudBlockBlob GetBlobForFile(IContentFile updateFile)
        {
            return ParentContainer.GetBlockBlobReference(updateFile.Digest.HexString.ToLower());
        }

        /// <inheritdoc cref="IContentStore.GetUri(IContentFile)"/>
        public string GetUri(IContentFile updateFile)
        {
            var fileBlob = GetBlobForFile(updateFile);

            SharedAccessBlobPolicy sharedPolicy =
                new()
                {
                    SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(10), // 2 minutes expired
                    Permissions = SharedAccessBlobPermissions.Read
                };

            string sasBlobToken = fileBlob.GetSharedAccessSignature(sharedPolicy, new SharedAccessBlobHeaders()
            {
                ContentDisposition = "attachment; filename=" + updateFile.FileName
            });

            return fileBlob.Uri.ToString() + sasBlobToken;
        }

        /// <inheritdoc cref="IContentStore.DownloadAsync(IContentFile, CancellationToken)"/>
        public Task DownloadAsync(IContentFile file, CancellationToken cancelToken)
        {
            var downloadTask = new Task(() =>
            {
                Download(new List<IContentFile>() { file}, cancelToken);
            });

            downloadTask.Start();

            return downloadTask;
        }

        /// <inheritdoc cref="IContentStore.Contains(IContentFileDigest, out string)"/>
        public bool Contains(IContentFileDigest fileDigest, out string fileName)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IContentStore.Get(IContentFileDigest)"/>
        public Stream Get(IContentFileDigest fileDigest)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IContentStore.GetUri(IContentFileDigest)"/>
        public string GetUri(IContentFileDigest fileDigest)
        {
            throw new NotImplementedException();
        }
    }
}
