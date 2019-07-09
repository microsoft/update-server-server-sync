using Microsoft.UpdateServices.Metadata.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;

namespace Microsoft.UpdateServices.Storage
{
    internal class ContentDownloader
    {
        public event EventHandler<OperationProgress> OnDownloadProgress;

        /// <summary>
        /// Downloads a single file belonging to an update package. Supports resuming a partial download
        /// </summary>
        /// <param name="destinationFilePath">Download destination file.</param>
        /// <param name="updateFile">The update file to download.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public void DownloadToFile(
            string destinationFilePath,
            UpdateFile updateFile,
            CancellationToken cancellationToken)
        {
            if (!File.Exists(destinationFilePath))
            {
                // Destination file does not exist; create it and then download it
                using (var fileStream = File.Create(destinationFilePath))
                {
                    DownloadToStream(fileStream, updateFile, 0, cancellationToken);
                }
            }
            else
            {
                // Destination file exists; if only partially downloaded, seek to the end and resume download
                // from where we left off
                using (var fileStream = File.Open(destinationFilePath, FileMode.Open, FileAccess.Write))
                {
                    if (fileStream.Length != (long)updateFile.Size)
                    {
                        fileStream.Seek(0, SeekOrigin.End);
                        DownloadToStream(fileStream, updateFile, fileStream.Length, cancellationToken);
                    }
                }
            }
        }

        /// <summary>
        /// Downloads the specified URL to the destination file stream
        /// </summary>
        /// <param name="destination">The file stream to write content to</param>
        /// <param name="updateFile">The update to download</param>
        /// <param name="startOffset">Offset to resume download at</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public void DownloadToStream(
            Stream destination,
            UpdateFile updateFile,
            long startOffset,
            CancellationToken cancellationToken)
        {
            var progress = new ContentOperationProgress()
            {
                File = updateFile,
                Current = startOffset,
                Maximum = (long)updateFile.Size,
                CurrentOperation = OperationType.DownloadFileProgress
            };
            progress.PercentDone = (progress.Current * 100) / progress.Maximum;

            // Validate starting offset
            if (startOffset >= (long)updateFile.Size)
            {
                throw new Exception($"Start offset {startOffset} cannot be greater than expected file size {updateFile.Size}");
            }

            var url = updateFile.DownloadUrl;

            using (var client = new HttpClient())
            {
                // First get the HEAD to check the server's size for the file
                HttpResponseMessage headResponse;
                var request = new HttpRequestMessage { RequestUri = new Uri(url), Method = HttpMethod.Head };
                headResponse = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).GetAwaiter().GetResult();

                if (!headResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to get HEAD of update from {url}: {headResponse.ReasonPhrase}");
                }

                // Make sure our size matches the server's size
                var fileSizeOnServer = headResponse.Content.Headers.ContentLength.Value;
                if (fileSizeOnServer != (long)updateFile.Size)
                {
                    throw new Exception($"File size mismatch. Expected {updateFile.Size}, server advertised {fileSizeOnServer}");
                }

                // Build the range request for the download
                var updateRequest = new HttpRequestMessage { RequestUri = new Uri(url), Method = HttpMethod.Get };
                updateRequest.Headers.Range = new RangeHeaderValue((long)startOffset, (long)fileSizeOnServer - 1);

                // Stream the file to disk
                using (HttpResponseMessage response = client
                    .SendAsync(updateRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .GetAwaiter()
                    .GetResult())
                {
                    if (response.IsSuccessStatusCode)
                    {
                        using (Stream streamToReadFrom = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                        {
                            // Read in 2MB chunks while not at the end and cancellation was not requested
                            byte[] readBuffer = new byte[2097152 * 5];
                            var readBytesCount = streamToReadFrom.Read(readBuffer, 0, readBuffer.Length);
                            while (!cancellationToken.IsCancellationRequested && readBytesCount > 0)
                            {
                                destination.Write(readBuffer, 0, readBytesCount);

                                progress.Current += readBytesCount;
                                progress.PercentDone = (progress.Current * 100) / progress.Maximum;
                                OnDownloadProgress?.Invoke(this, progress);

                                readBytesCount = streamToReadFrom.Read(readBuffer, 0, readBuffer.Length);
                            }
                        }
                    }
                    else
                    {
                        throw new Exception($"Failed to get content of update from {url}: {response.ReasonPhrase}");
                    }
                }
            }
        }
    }
}
