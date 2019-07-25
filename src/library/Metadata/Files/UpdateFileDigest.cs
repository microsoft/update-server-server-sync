// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System;

namespace Microsoft.UpdateServices.Metadata.Content
{
    /// <summary>
    /// Represents digest information for an update content file
    /// </summary>
    public class UpdateFileDigest
    {
        /// <summary>
        /// Gets the digest algorithm used
        /// </summary>
        /// <value>Digest algorithm name</value>
        public string Algorithm { get; private set; }

        /// <summary>
        /// Gets the base64 encoded digest
        /// </summary>
        /// <value>Base64 encoded string</value>
        public string DigestBase64 { get; private set; }

        /// <summary>
        /// Gets the HEX string representation of the digest 
        /// </summary>
        public string HexString => BitConverter.ToString(Convert.FromBase64String(DigestBase64)).Replace("-", "");

        [JsonConstructor]
        private UpdateFileDigest() { }

        internal UpdateFileDigest(string algorithm, string digestBase64)
        {
            Algorithm = algorithm;
            DigestBase64 = digestBase64;
        }
    }
}
