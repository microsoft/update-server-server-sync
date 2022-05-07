// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System;

namespace Microsoft.PackageGraph.ObjectModel
{
    /// <summary>
    /// An implementation for <see cref="IContentFileDigest"/> for an update content file.
    /// </summary>
    public class ContentFileDigest : IContentFileDigest
    {
        /// <inheritdoc cref="IContentFileDigest.Algorithm"/>
        [JsonProperty]
        public string Algorithm { get; private set; }

        /// <inheritdoc cref="IContentFileDigest.DigestBase64"/>
        [JsonProperty]
        public string DigestBase64 { get; private set; }

        /// <inheritdoc cref="IContentFileDigest.HexString"/>
        public string HexString => BitConverter.ToString(Convert.FromBase64String(DigestBase64)).Replace("-", "");

        [JsonConstructor]
        private ContentFileDigest() { }

        /// <summary>
        /// Creates update content digest from a hash algorithm and value
        /// </summary>
        /// <param name="algorithm">Algorithm used to generate the digest</param>
        /// <param name="digestBase64">Digest value as base64 string</param>
        public ContentFileDigest(string algorithm, string digestBase64)
        {
            Algorithm = algorithm;
            DigestBase64 = digestBase64;
        }

        /// <summary>
        /// Equality override between two content digest objects.
        /// </summary>
        /// <param name="obj">Other digest</param>
        /// <returns>True if algorigthms and values match for the two digests</returns>
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }
            else if (obj is ContentFileDigest other)
            {
                return this.Algorithm == other.Algorithm && this.DigestBase64 == other.DigestBase64;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// String formatting override.
        /// </summary>
        /// <returns>String formatted object as hash_algorithm:hex_value</returns>
        public override string ToString()
        {
            return $"{Algorithm}:{HexString}";
        }

        /// <summary>
        /// Override for getting the object's hash code
        /// </summary>
        /// <returns>Int hash code</returns>
        public override int GetHashCode()
        {
            return ($"{Algorithm}:{DigestBase64}").GetHashCode();
        }
    }
}
