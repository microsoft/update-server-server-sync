// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.ObjectModel;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Microsoft.PackageGraph.Storage.Local
{
    /// <summary>
    /// Hashes file contents and checks hashes with value expected from update file metadata
    /// </summary>
    class ContentHash
    {
        public event EventHandler<ContentOperationProgress> OnHashingProgress;

        public bool Check(IContentFile updateFile, string filePath)
        {
            byte[] readAheadBuffer, buffer;
            int readAheadBytesRead, bytesRead;
            int bufferSize = 512 * 1024;

            // Pick the stronges hash algorithm available
            HashAlgorithm hashAlgorithm;
            if (updateFile.Digest.Algorithm.Equals("SHA512"))
            {
                hashAlgorithm = SHA512.Create();
            }
            else if (updateFile.Digest.Algorithm.Equals("SHA256"))
            {
                hashAlgorithm = SHA256.Create();
            }
            else if (updateFile.Digest.Algorithm.Equals("SHA1"))
            {
                hashAlgorithm = SHA1.Create();
            }
            else
            {
                throw new Exception($"No supported hashing algorithms found for update file {updateFile.FileName}");
            }

            readAheadBuffer = new byte[bufferSize];
            buffer = new byte[bufferSize];

            // Hash the file contents
            using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            var progress = new ContentOperationProgress()
            {
                File = updateFile,
                Current = 0,
                Maximum = (long)updateFile.Size,
                CurrentOperation = PackagesOperationType.HashFileProgress
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

                OnHashingProgress?.Invoke(this, progress);
            } while (readAheadBytesRead != 0);

            // Check that actual hash matches the expected value
            var actualHash = hashAlgorithm.Hash;
            var expectedHash = Convert.FromBase64String(updateFile.Digest.DigestBase64);

            return actualHash.SequenceEqual(expectedHash);
        }
    }
}
