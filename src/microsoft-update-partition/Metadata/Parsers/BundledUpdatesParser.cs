// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Parsers
{
    /// <summary>
    /// Parses bundled updates information from update XML blob
    /// </summary>
    abstract class BundlesUpdatesParser
    {
        public static List<MicrosoftUpdatePackageIdentity> Parse(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            var bundledUpdates = new List<MicrosoftUpdatePackageIdentity>();

            XPathExpression supersededUpdatesQuery = metadataNavigator.Compile("upd:Update/upd:Relationships/upd:BundledUpdates/upd:UpdateIdentity");
            supersededUpdatesQuery.SetContext(namespaceManager);

            var result = metadataNavigator.Evaluate(supersededUpdatesQuery) as XPathNodeIterator;
            while (result.MoveNext())
            {
                var guid = Guid.Parse(result.Current.GetAttribute("UpdateID", ""));
                var revision = Int32.Parse(result.Current.GetAttribute("RevisionNumber", ""));

                bundledUpdates.Add(new MicrosoftUpdatePackageIdentity(guid, revision));
            }

            XPathExpression supersededUpdatesQueryAtLeast = metadataNavigator.Compile("upd:Update/upd:Relationships/upd:BundledUpdates/upd:AtLeastOne/upd:UpdateIdentity");
            supersededUpdatesQueryAtLeast.SetContext(namespaceManager);

            var atLeastOneResult = metadataNavigator.Evaluate(supersededUpdatesQueryAtLeast) as XPathNodeIterator;
            while (atLeastOneResult.MoveNext())
            {
                var guid = Guid.Parse(atLeastOneResult.Current.GetAttribute("UpdateID", ""));
                var revision = Int32.Parse(atLeastOneResult.Current.GetAttribute("RevisionNumber", ""));

                bundledUpdates.Add(new MicrosoftUpdatePackageIdentity(guid, revision));
            }

            return bundledUpdates;
        }
    }
}
