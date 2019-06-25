// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Xml.Linq;


namespace Microsoft.UpdateServices.Metadata.Content
{
    /// <summary>
    /// Parses file information from update XML blob
    /// </summary>
    abstract class UpdateFileParser
    {
        /// <summary>
        /// Create an UpdateFile object with metadata from the XML blob and URLs from the url data array
        /// </summary>
        /// <param name="xdoc">The XML element that holds file metadata</param>
        /// <param name="urlData">array of known file URLs</param>
        /// <returns></returns>
        public static List<UpdateFile> Parse(XDocument xdoc, List<UpdateFileUrl> urlData)
        {
            var parsedFiles = new List<UpdateFile>();

            // Grab all File elements from the XML
            var fileElements = xdoc.Descendants(XName.Get("File", "http://schemas.microsoft.com/msus/2002/12/Update"));
            foreach (var fileElement in fileElements)
            {
                // Create a new UpdateFile from the File element
                parsedFiles.Add(new UpdateFile(fileElement));
            }

            // Find URLs for the parsed files. Urls are matched by file digest.
            foreach(var file in parsedFiles)
            {
                foreach(var hash in file.Digests)
                {
                    var fileUrl = urlData.Find(url => url.DigestBase64.Equals(hash.DigestBase64));
                    if (fileUrl != null)
                    {
                        file.Urls.Add(fileUrl);
                        break;
                    }
                }
            }

            return parsedFiles;
        }
    }
}
