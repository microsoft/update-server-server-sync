// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Metadata.Content
{
    /// <summary>
    /// Interface implemented by updates that have content (<see cref="IUpdateWithFiles"/>)
    /// </summary>
    public interface IUpdateWithFiles
    {
        /// <summary>
        /// Gets the list of <see cref="UpdateFile"/> for an update
        /// </summary>
        /// <value>
        /// List of files
        /// </value>
        List<UpdateFile> Files { get; }
    }

    /// <summary>
    /// Represents a content file for an update.
    /// </summary>
    public class UpdateFile
    {
        /// <summary>
        /// Gets the name of the file
        /// </summary>
        /// <value>
        /// File name
        /// </value>
        [JsonProperty]
        public string FileName { get; private set; }

        /// <summary>
        /// Ges the file size, in bytes.
        /// </summary>
        /// <value>File size</value>
        [JsonProperty]
        public UInt64 Size { get; private set; }

        /// <summary>
        /// Gets the last modified timestamp for the file
        /// </summary>
        /// <value>Last modified DateTime</value>
        [JsonProperty]
        public DateTime ModifiedDate { get; private set; }

        /// <summary>
        /// Gets the list of file digests. Multiple hashing algorithms might be used.
        /// </summary>
        /// <value>List of file digests, one per algorithm.</value>
        [JsonProperty]
        public List<UpdateFileDigest> Digests { get; private set; }

        /// <summary>
        /// Gets the type of patching this file provides
        /// </summary>
        /// <value>Patchin type string</value>
        [JsonProperty]
        public string PatchingType { get; private set; }

        /// <summary>
        /// Gets the list of URLs for the file.
        /// </summary>
        /// <value>List of URLs.</value>
        [JsonProperty]
        public List<UpdateFileUrl> Urls { get; private set; }

        /// <summary>
        /// Gets the default download URL for a file.
        /// </summary>
        public string DownloadUrl => string.IsNullOrEmpty(Urls[0].MuUrl) ? Urls[0].UssUrl : Urls[0].MuUrl;

        [JsonConstructor]
        private UpdateFile() { }

        /// <summary>
        /// Create a new update file with data parsed from the XML element specified
        /// </summary>
        /// <param name="xmlFileElement"></param>
        internal UpdateFile(XElement xmlFileElement)
        {
            Digests = new List<UpdateFileDigest>();
            Urls = new List<UpdateFileUrl>();

            Digests.Add(new UpdateFileDigest(xmlFileElement.Attributes("DigestAlgorithm").First().Value, xmlFileElement.Attributes("Digest").First().Value));

            FileName = xmlFileElement.Attributes("FileName").First().Value;
            Size = UInt64.Parse(xmlFileElement.Attributes("Size").First().Value);
            ModifiedDate = DateTime.Parse(xmlFileElement.Attributes("Modified").First().Value);

            var patchingTypeAttributes = xmlFileElement.Attributes("PatchingType");
            if (patchingTypeAttributes.Count() > 0)
            {
                PatchingType = patchingTypeAttributes.First().Value;
            }

            var additionalDigestsElements = xmlFileElement.Descendants(XName.Get("AdditionalDigest", "http://schemas.microsoft.com/msus/2002/12/Update"));
            foreach(var additionalDigest in additionalDigestsElements)
            {
                Digests.Add(new UpdateFileDigest(additionalDigest.Attributes("Algorithm").First().Value, additionalDigest.Value));
            }
        }

        /// <summary>
        /// Override equality method; two UpdateFile are equal if they have the same content hash.
        /// </summary>
        /// <param name="obj">Other UpdateFile</param>
        /// <returns>True if the two objects are equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is UpdateFile))
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