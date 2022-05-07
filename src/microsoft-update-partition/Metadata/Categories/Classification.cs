// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.Storage;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata
{
    /// <summary>
    /// Represents a classification in the Microsoft Update catalog. 
    /// Software or driver updates have a classification: "update", "critical update", etc.
    /// </summary>
    public class ClassificationCategory : MicrosoftUpdatePackage
    {
        internal ClassificationCategory(
            MicrosoftUpdatePackageIdentity id, 
            XPathNavigator metadataNavigator, 
            XmlNamespaceManager namespaceManager) : base(id, metadataNavigator, namespaceManager)
        {
        }

        internal ClassificationCategory(MicrosoftUpdatePackageIdentity id, IMetadataLookup metadataLookup, IMetadataSource metadataSource) : base(id, metadataLookup, metadataSource)
        {
        }

        internal override void LoadNonIndexedMetadata(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
        }
    }
}
