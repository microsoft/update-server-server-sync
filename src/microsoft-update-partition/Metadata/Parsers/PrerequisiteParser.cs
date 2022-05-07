// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Prerequisites;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Parsers
{
    abstract class PrerequisiteParser
    {
        internal static List<IPrerequisite> FromXml(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            var parsedPrerequisites = new List<IPrerequisite>();

            // Grab all File elements from the XML
            XPathExpression simplePrerequisiteQuery = metadataNavigator.Compile("upd:Update/upd:Relationships/upd:Prerequisites/upd:UpdateIdentity/@UpdateID");
            simplePrerequisiteQuery.SetContext(namespaceManager);

            var simplePrereqResult = metadataNavigator.Evaluate(simplePrerequisiteQuery) as XPathNodeIterator;
            while (simplePrereqResult.MoveNext())
            {
                parsedPrerequisites.Add(new Simple(Guid.Parse(simplePrereqResult.Current.Value)));
            }

            XPathExpression atLeastOnePrereq = metadataNavigator.Compile("upd:Update/upd:Relationships/upd:Prerequisites/upd:AtLeastOne");
            atLeastOnePrereq.SetContext(namespaceManager);
            var atLeastOneResult = metadataNavigator.Evaluate(atLeastOnePrereq) as XPathNodeIterator;
            while (atLeastOneResult.MoveNext())
            {
                var isCategoryString = atLeastOneResult.Current.GetAttribute("IsCategory", "");
                bool isCategory = !string.IsNullOrEmpty(isCategoryString) && isCategoryString == "true";

                var atLeastUpdatesList = atLeastOneResult.Current.Select("upd:UpdateIdentity/@UpdateID", namespaceManager);
                List<Guid> atLeastUpdates = new();
                while (atLeastUpdatesList.MoveNext())
                {
                    atLeastUpdates.Add(Guid.Parse(atLeastUpdatesList.Current.Value));
                }

                if (atLeastUpdates.Count == 0)
                {
                    throw new Exception("The list of prerequisites was empty");
                }

                parsedPrerequisites.Add(new AtLeastOne(atLeastUpdates, isCategory));
            }

            return parsedPrerequisites;
        }
    }
}
