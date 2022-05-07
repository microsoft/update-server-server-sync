// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.ObjectModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Content

{
    /// <summary>
    /// Represents a content file for an update.
    /// </summary>
    public class UpdateFile : IContentFile
    {
        /// <summary>
        /// Gets the name of the file
        /// </summary>
        /// <value>
        /// File name
        /// </value>
        [JsonProperty]
        public string FileName { get; set; }

        /// <summary>
        /// Ges the file size, in bytes.
        /// </summary>
        /// <value>File size</value>
        [JsonProperty]
        public UInt64 Size { get; set; }

        /// <summary>
        /// Gets the last modified timestamp for the file
        /// </summary>
        /// <value>Last modified DateTime</value>
        [JsonProperty]
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// Gets the list of file digests. Multiple hashing algorithms might be used.
        /// </summary>
        /// <value>List of file digests, one per algorithm.</value>
        [JsonProperty]
        public List<ContentFileDigest> Digests { get; set; }

        /// <summary>
        /// Gets the type of patching this file provides
        /// </summary>
        /// <value>Patchin type string</value>
        [JsonProperty]
        public string PatchingType { get; set; }

        /// <summary>
        /// Gets the list of URLs for the file.
        /// </summary>
        /// <value>List of URLs.</value>
        [JsonProperty]
        public List<UpdateFileUrl> Urls { get; set; }

        /// <summary>
        /// Gets the default download URL for a file.
        /// </summary>
        [JsonIgnore]
        public string Source => string.IsNullOrEmpty(Urls[0].MuUrl) ? Urls[0].UssUrl : Urls[0].MuUrl;

        /// <summary>
        /// Gets the primary digest of a content file. 
        /// </summary>
        /// <value>Content file digest.</value>
        [JsonIgnore]
        public IContentFileDigest Digest => Digests.First();

        /// <summary>
        /// Create a new UpdateFile
        /// </summary>
        [JsonConstructor]
        public UpdateFile() { }

        /// <summary>
        /// Override equality method; two UpdateFile are equal if they have the same content hash.
        /// </summary>
        /// <param name="obj">Other UpdateFile</param>
        /// <returns>True if the two objects are equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            if (obj is not UpdateFile)
            {
                return false;
            }

            var otherDigest = (obj as UpdateFile).Digests[0];

            return otherDigest.DigestBase64.Equals(Digests[0].DigestBase64) && otherDigest.Algorithm.Equals(Digests[0].Algorithm);
        }

        /// <summary>
        /// Return a hash code based on the hash of the file content.
        /// </summary>
        /// <returns>UpdateFile hash code</returns>
        public override int GetHashCode()
        {
            return Digests[0].DigestBase64.GetHashCode();
        }
    }
}