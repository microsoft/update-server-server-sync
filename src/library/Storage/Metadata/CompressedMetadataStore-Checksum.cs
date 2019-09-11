// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ICSharpCode.SharpZipLib.Zip;
using Microsoft.UpdateServices.Client;
using Microsoft.UpdateServices.Compression;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.Metadata.Prerequisites;
using Microsoft.UpdateServices.Storage;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.UpdateServices.Storage
{
    public partial class CompressedMetadataStore
    {
        /// <summary>
        /// The checksum of the updates in the metadata source.
        /// </summary>
        /// <value>SHA512 checksum of metadata source in base64 string format</value>
        [JsonProperty]
        public string Checksum { get; private set; }

        /// <summary>
        /// Computes the checksum of this medata source.
        /// The checksum is computed from the list of triples [update index, update guid, update revision], sorted by update index
        /// </summary>
        private void ComputeChecksum()
        {
            CommitProgress?.Invoke(this, new OperationProgress() { CurrentOperation = OperationType.HashMetadataStart });

            using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA512))
            {
                foreach (var entry in IndexToIdentity)
                {
                    hash.AppendData(BitConverter.GetBytes(entry.Key));
                    hash.AppendData(BitConverter.GetBytes(entry.Value.Revision));
                    hash.AppendData(entry.Value.Raw.UpdateID.ToByteArray());
                }

                Checksum = Convert.ToBase64String(hash.GetHashAndReset());
            }

            CommitProgress?.Invoke(this, new OperationProgress() { CurrentOperation = OperationType.HashMetadataEnd });
        }
    }
}
