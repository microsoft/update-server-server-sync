// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Parsers
{
    abstract class UpdateParser
    {
        public static string GetDescription(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            XPathExpression descriptionQuery = metadataNavigator.Compile("upd:Update/upd:LocalizedPropertiesCollection/upd:LocalizedProperties[upd:Language='en']/upd:Description");
            descriptionQuery.SetContext(namespaceManager);

            var result = metadataNavigator.Evaluate(descriptionQuery) as XPathNodeIterator;
            if (result.Count > 0)
            {
                result.MoveNext();
                return result.Current.Value;
            }
            else
            {
                return null;
            }
        }

		public static string GetCreationDate(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
		{
			XPathExpression updateTypeQuery = metadataNavigator.Compile("upd:Update/upd:Properties/@CreationDate");
			updateTypeQuery.SetContext(namespaceManager);

			var result = metadataNavigator.Evaluate(updateTypeQuery) as XPathNodeIterator;

			if (result.Count == 0)
			{
				throw new Exception("Invalid XML");
			}

			result.MoveNext();
			return result.Current.Value;
		}

		public static string GetTitle(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            XPathExpression titleQuery = metadataNavigator.Compile("upd:Update/upd:LocalizedPropertiesCollection/upd:LocalizedProperties[upd:Language='en']/upd:Title");
            titleQuery.SetContext(namespaceManager);

            var result = metadataNavigator.Evaluate(titleQuery) as XPathNodeIterator;

            if (result.Count == 0)
            {
                throw new Exception("Invalid XML");
            }

            result.MoveNext();
            return result.Current.Value;
        }

        public static string GetUpdateType(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            XPathExpression updateTypeQuery = metadataNavigator.Compile("upd:Update/upd:Properties/@UpdateType");
            updateTypeQuery.SetContext(namespaceManager);

            var result = metadataNavigator.Evaluate(updateTypeQuery) as XPathNodeIterator;

            if (result.Count == 0)
            {
                throw new Exception("Invalid XML");
            }

            result.MoveNext();
            return result.Current.Value;
        }

        public static MicrosoftUpdatePackageIdentity GetUpdateId(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            XPathExpression updateIdQuery = metadataNavigator.Compile("upd:Update/upd:UpdateIdentity/@UpdateID");
            XPathExpression revisionQuery = metadataNavigator.Compile("upd:Update/upd:UpdateIdentity/@RevisionNumber");
            updateIdQuery.SetContext(namespaceManager);
            revisionQuery.SetContext(namespaceManager);

            var idResult = metadataNavigator.Evaluate(updateIdQuery) as XPathNodeIterator;
            var revisionResult = metadataNavigator.Evaluate(revisionQuery) as XPathNodeIterator;

            if (idResult.Count == 0 || revisionResult.Count == 0)
            {
                throw new Exception("Invalid XML");
            }

            revisionResult.MoveNext();
            idResult.MoveNext();

            return new MicrosoftUpdatePackageIdentity(Guid.Parse(idResult.Current.Value), Int32.Parse(revisionResult.Current.Value));
        }

        public static string GetCategory(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            XPathExpression categoryQuery = metadataNavigator.Compile("upd:Update/upd:HandlerSpecificData/cat:CategoryInformation/@CategoryType");
            categoryQuery.SetContext(namespaceManager);

            var result = metadataNavigator.Evaluate(categoryQuery) as XPathNodeIterator;

            if (result.Count == 0)
            {
                throw new Exception("Invalid XML");
            }

            result.MoveNext();
            return result.Current.Value;
        }
    }
}
