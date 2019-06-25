// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System;

namespace Microsoft.UpdateServices.Metadata.Content
{
    /// <summary>
    /// Stores digest information for a file
    /// </summary>
    public class UpdateFileDigest
    {
        /// <summary>
        /// The digest algorithm used
        /// </summary>
        public string Algorithm { get; set; }

        /// <summary>
        /// Base64 encoded digest
        /// </summary>
        public string DigestBase64 { get; set; }

        [JsonConstructor]
        private UpdateFileDigest() { }

        public UpdateFileDigest(string algorithm, string digestBase64)
        {
            Algorithm = algorithm;
            DigestBase64 = digestBase64;
        }
    }
}
