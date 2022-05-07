// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Content;
using Microsoft.PackageGraph.ObjectModel;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Parsers
{
    /// <summary>
    /// Parses file information from update XML blob
    /// </summary>
    abstract class UpdateFileParser
    {
        public static List<UpdateFile> ParseFiles(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            var parsedFiles = new List<UpdateFile>();

            // Grab all File elements from the XML
            XPathExpression filesQuery = metadataNavigator.Compile("upd:Update/upd:Files/upd:File");
            filesQuery.SetContext(namespaceManager);

            var result = metadataNavigator.Evaluate(filesQuery) as XPathNodeIterator;

            if (result.Count > 0)
            {
                while (result.MoveNext())
                {
                    var currentFileNode = result.Current;
                    var newFile = new UpdateFile
                    {
                        Digests = new List<ContentFileDigest>()
                    };

                    newFile.Digests.Add(new ContentFileDigest(
                        currentFileNode.GetAttribute("DigestAlgorithm", ""),
                        currentFileNode.GetAttribute("Digest", "")));

                    var additionalDigests = currentFileNode.Select("upd:AdditionalDigest", namespaceManager);
                    while (additionalDigests.MoveNext())
                    {
                        newFile.Digests.Add(new ContentFileDigest(
                        additionalDigests.Current.GetAttribute("Algorithm", ""),
                        additionalDigests.Current.Value));
                    }

                    newFile.FileName = currentFileNode.GetAttribute("FileName", "");
                    newFile.ModifiedDate = DateTime.Parse(currentFileNode.GetAttribute("Modified", ""));
                    newFile.Size = UInt64.Parse(currentFileNode.GetAttribute("Size", ""));
                    newFile.PatchingType = currentFileNode.GetAttribute("PatchingType", "");

                    parsedFiles.Add(newFile);
                }
            }

            return parsedFiles;
        }
    }
}
