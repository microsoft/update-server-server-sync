// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Parsers
{
    /// <summary>
    /// Parses superseded updates information from update XML blob
    /// </summary>
    abstract class SupersededUpdatesParser
    {
        public static List<Guid> Parse(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            var supersededIds = new List<Guid>();

            XPathExpression supersededUpdatesQuery = metadataNavigator.Compile("upd:Update/upd:Relationships/upd:SupersededUpdates/upd:UpdateIdentity/@UpdateID");
            supersededUpdatesQuery.SetContext(namespaceManager);

            var result = metadataNavigator.Evaluate(supersededUpdatesQuery) as XPathNodeIterator;

            while (result.MoveNext())
            {
                supersededIds.Add(Guid.Parse(result.Current.Value));
            }

            return supersededIds;
        }
    }
}
