// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UpdateServices.Metadata.Content
{
    /// <summary>
    /// Represents source information for an update file.
    /// </summary>
    public class UpdateFileUrl
    {
        /// <summary>
        /// Gets the SHA256 digest of the file content
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
        /// Construct object from raw ServerSyncUrlData
        /// </summary>
        /// <param name="serverSyncUrlData"></param>
        internal UpdateFileUrl(ServerSyncUrlData serverSyncUrlData)
        {
            DigestBase64 = Convert.ToBase64String(serverSyncUrlData.FileDigest);
            MuUrl = serverSyncUrlData.MUUrl;
            UssUrl = serverSyncUrlData.UssUrl;
        }

        /// <summary>
        /// Override equality comparison. Two UpdateFileUrl are equal if they have the same content hash.
        /// </summary>
        /// <param name="obj">The other UpdateFileUrl</param>
        /// <returns>True if the two UpdateFileUrl have the same content hash, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            return ReferenceEquals(obj, null) || !(obj is UpdateFileUrl)
                ? false
                : this.DigestBase64.Equals((obj as UpdateFileUrl).DigestBase64);
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
        /// Override equality operator to match Equals method.
        /// </summary>
        /// <param name="lhs">Left UpdateFileUrl</param>
        /// <param name="rhs">Right UpdateFileUrl</param>
        /// <returns>True if the two UpdateFileUrl have the same content hash, false otherwise</returns>
        public static bool operator ==(UpdateFileUrl lhs, UpdateFileUrl rhs) => ReferenceEquals(lhs, null) ? ReferenceEquals(rhs, null) : lhs.Equals(rhs);

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
