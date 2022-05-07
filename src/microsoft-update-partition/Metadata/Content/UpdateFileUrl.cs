// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Content
{
    /// <summary>
    /// Represents source information for an update file.
    /// </summary>
    public class UpdateFileUrl
    {
        /// <summary>
        /// Gets the digest of the file content
        /// </summary>
        /// <value>SHA256 digest, base64 encoded string.</value>
        [JsonProperty]
        public string DigestBase64 { get; private set; }

        /// <summary>
        /// Gets the Microsoft Update URL to the file.
        /// <para>This property is set if the update containing this file was queries from the official
        /// Microsoft upstream server.</para>
        /// </summary>
        /// <value>URL string</value>
        [JsonProperty]
        public string MuUrl { get; private set; }

        /// <summary>
        /// Gets the upstream server URL to the file.
        /// <para>This property is set if the update containing his file was queries from a WSUS upstream server.</para>
        /// </summary>
        /// <value>URL string</value>
        [JsonProperty]
        public string UssUrl { get; private set; }

        /// <summary>
        /// Private constructor for deserialization
        /// </summary>
        [JsonConstructor]
        private UpdateFileUrl() { }

        /// <summary>
        /// Instantiate a new update file URL using the specified attributes
        /// </summary>
        /// <param name="digest">File digest in base64 format</param>
        /// <param name="muUrl">The MU URL.</param>
        /// <param name="ussUrl">The USS URL.</param>
        public UpdateFileUrl(string digest, string muUrl, string ussUrl)
        {
            DigestBase64 = digest;
            MuUrl = muUrl;
            UssUrl = ussUrl;
        }

        /// <summary>
        /// Override equality comparison. Two UpdateFileUrl are equal if they have the same content hash.
        /// </summary>
        /// <param name="obj">The other UpdateFileUrl</param>
        /// <returns>True if the two UpdateFileUrl have the same content hash, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (obj is UpdateFileUrl other)
            {
                return this.DigestBase64.Equals(other.DigestBase64);
            }
            else if (obj is string otherString)
            {
                return this.DigestBase64.Equals(otherString);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns a hash code based on the content hash.
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            return DigestBase64.GetHashCode();
        }

        /// <summary>
        /// Checks if two file URLs point to the same content.
        /// </summary>
        /// <param name="obj">The other update content URL</param>
        /// <returns>True if both URLs point to the same content (by hash), false otherwise</returns>
        public int CompareTo(object obj)
        {
            if (obj is null)
            {
                return -1;
            }

            if (obj is UpdateFileUrl other)
            {
                return this.DigestBase64.CompareTo(other.DigestBase64);
            }
            else if (obj is string otherString)
            {
                return this.DigestBase64.CompareTo(otherString);
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Override equality operator to match Equals method.
        /// </summary>
        /// <param name="lhs">Left UpdateFileUrl</param>
        /// <param name="rhs">Right UpdateFileUrl</param>
        /// <returns>True if the two UpdateFileUrl have the same content hash, false otherwise</returns>
        public static bool operator ==(UpdateFileUrl lhs, UpdateFileUrl rhs) => lhs is null ? rhs is null : lhs.Equals(rhs);

        /// <summary>
        /// Override inequality operator to match Equals method
        /// </summary>
        /// <param name="lhs">Left UpdateFileUrl</param>
        /// <param name="rhs">Right UpdateFileUrl</param>
        /// <returns>False if the two UpdateFileUrl have the same content hash, true otherwise</returns>
        public static bool operator !=(UpdateFileUrl lhs, UpdateFileUrl rhs)
        {
            return !(lhs == rhs);
        }
    }
}
