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
    /// Interface implemented by updates that can have files
    /// </summary>
    public interface IUpdateWithFiles
    {
        List<UpdateFile> Files { get; }
    }

    /// <summary>
    /// Stores information about a file associated with an update
    /// </summary>
    public class UpdateFile
    {
        /// <summary>
        /// The name of the file
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// File size
        /// </summary>
        public UInt64 Size { get; set; }

        /// <summary>
        /// Last file modified data
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// File digests. Multiple algorithms might be used
        /// </summary>
        public List<UpdateFileDigest> Digests { get; set; }

        /// <summary>
        /// The type of patching this file provides
        /// </summary>
        public string PatchingType { get; set; }

        /// <summary>
        /// Soruce URLs for the file
        /// </summary>
        public List<UpdateFileUrl> Urls { get; set; }

        public bool HasDownloadUrl => Urls.Count > 0;

        public string DownloadUrl => string.IsNullOrEmpty(Urls[0].MuUrl) ? Urls[0].UssUrl : Urls[0].MuUrl;

        [JsonConstructor]
        private UpdateFile() { }

        /// <summary>
        /// Create a new update file with data parsed from the XML element specified
        /// </summary>
        /// <param name="xmlFileElement"></param>
        public UpdateFile(XElement xmlFileElement)
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

        public override bool Equals(object obj)
        {
            if (!(obj is UpdateFile))
            {
                return false;
            }

            var otherDigest = (obj as UpdateFile).Digests[0];

            return otherDigest.DigestBase64.Equals(Digests[0].DigestBase64) && otherDigest.Algorithm.Equals(Digests[0].Algorithm);
        }

        public override int GetHashCode()
        {
            return Digests[0].DigestBase64.GetHashCode();
        }
    }
}