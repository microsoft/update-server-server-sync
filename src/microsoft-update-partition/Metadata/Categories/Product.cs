// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.Storage;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata
{
    /// <summary>
    /// Represents a product category in the Microsoft Update catalog. 
    /// Software or driver updates have one or more corresponding categories: "SQL Server [x]", "Visual Studio [x]", "Windows 1903 and later", etc.
    /// </summary>
    public class ProductCategory : MicrosoftUpdatePackage
    {
        internal ProductCategory(
            MicrosoftUpdatePackageIdentity id, 
            XPathNavigator metadataNavigator, 
            XmlNamespaceManager namespaceManager) : base(id, metadataNavigator, namespaceManager)
        {
        }

        internal ProductCategory(MicrosoftUpdatePackageIdentity id, IMetadataLookup metadataLookup, IMetadataSource metadataSource) : base(id, metadataLookup, metadataSource)
        {
        }

        internal override void LoadNonIndexedMetadata(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
        }
    }
}
