// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Parsers
{
    class SoftwareUpdateParser
    {
        public static SoftwareUpdateMetadata GetSoftwareUpdateProperties(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            var propertiesNode = metadataNavigator.SelectSingleNode("upd:Update/upd:Properties", namespaceManager);
            var osUpgradeAttribute = metadataNavigator.Select("upd:Update/upd:Properties/@OSUpgrade", namespaceManager);

            var metadata = new SoftwareUpdateMetadata();

            if (osUpgradeAttribute.Count > 0)
            {
                osUpgradeAttribute.MoveNext();
                metadata.OSUpgrade = osUpgradeAttribute.Current.Value;
            }

            if (propertiesNode.MoveToFirstChild())
            {
                do
                {
                    if (propertiesNode.Name == "upd:MoreInfoUrl")
                    {
                        metadata.MoreInfoUrl = propertiesNode.Value;
                    }
                    else if (propertiesNode.Name == "upd:SupportUrl")
                    {
                        metadata.SupportUrl = propertiesNode.Value;
                    }
                    else if (propertiesNode.Name == "upd:KBArticleID")
                    {
                        metadata.KBArticleID = propertiesNode.Value;
                    }
                } while (propertiesNode.MoveToNext());
            }

            return metadata;
        }
    }

    struct SoftwareUpdateMetadata
    {
        public string MoreInfoUrl;
        public string SupportUrl;
        public string KBArticleID;
        public string OSUpgrade;
    }
}
