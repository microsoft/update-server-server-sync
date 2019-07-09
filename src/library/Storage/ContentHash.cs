using Microsoft.UpdateServices.Metadata.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.UpdateServices.Storage
{
    /// <summary>
    /// Hashes file contents and checks hashes with value expected from update file metadata
    /// </summary>
    class ContentHash
    {
        public event EventHandler<OperationProgress> OnHashingProgress;

        /// <summary>
        /// Checks that the hash of a file matches the value specified in the update file metadata
        /// </summary>
        /// <param name="updateFile">The update file object that contains the expected checksums</param>
        /// <param name="filePath">The path to the file to checksum</param>
        /// <returns>The string representatin of the hash</returns>
        public bool Check(UpdateFile updateFile, string filePath)
        {
            byte[] readAheadBuffer, buffer;
            int readAheadBytesRead, bytesRead;
            int bufferSize = 512 * 1024;

            // Pick the stronges hash algorithm available
            HashAlgorithm hashAlgorithm;
            UpdateFileDigest targetDigest;
            if ((targetDigest = updateFile.Digests.Find(d => d.Algorithm.Equals("SHA512"))) != null)
            {
                hashAlgorithm = new SHA512Managed();
            }
            else if ((targetDigest = updateFile.Digests.Find(d => d.Algorithm.Equals("SHA256"))) != null)
            {
                hashAlgorithm = new SHA256Managed();
            }
            else if ((targetDigest = updateFile.Digests.Find(d => d.Algorithm.Equals("SHA1"))) != null)
            {
                hashAlgorithm = new SHA1Managed();
            }
            else
            {
                throw new Exception($"No supported hashing algorithms found for update file {updateFile.FileName}");
            }

            readAheadBuffer = new byte[bufferSize];
            buffer = new byte[bufferSize];

            // Hash the file contents
            using (var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                var progress = new ContentOperationProgress()
                {
                    File = updateFile,
                    Current = 0,
                    Maximum = (long)updateFile.Size,
                    CurrentOperation = OperationType.HashFileProgress,
                    PercentDone = 0
                };

                readAheadBytesRead = fileStream.Read(readAheadBuffer, 0, readAheadBuffer.Length);

                do
                {
                    byte[] tempBuffer;
                    bytesRead = readAheadBytesRead;
                    tempBuffer = buffer;
                    buffer = readAheadBuffer;
                    readAheadBuffer = tempBuffer;

                    readAheadBytesRead = fileStream.Read(readAheadBuffer, 0, readAheadBuffer.Length);

                    if (readAheadBytesRead == 0)
                    {
                        hashAlgorithm.TransformFinalBlock(buffer, 0, bytesRead);
                        progress.Current += bytesRead;
                        
                    }
                    else
                    {
                        hashAlgorithm.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                        progress.Current += bytesRead;
                    }

                    progress.PercentDone = (progress.Current * 100) / progress.Maximum;

                    OnHashingProgress?.Invoke(this, progress);
                } while (readAheadBytesRead != 0);

                // Check that actual hash matches the expected value
                var actualHash = hashAlgorithm.Hash;
                var expectedHash = Convert.FromBase64String(targetDigest.DigestBase64);

                return actualHash.SequenceEqual(expectedHash);
            }
        }
    }
}
