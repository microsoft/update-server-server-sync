// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Handlers
{
    /// <summary>
    /// Microsoft Update handler for updating individual Windows CBS packages
    /// </summary>
    public class CbsHandler : HandlerMetadata
    {
        /// <summary>
        /// The identity of the package being updated
        /// </summary>
        [JsonProperty]
        public string PackageIdentity { get; private set; }

        [JsonConstructor]
        private CbsHandler()
        {

        }

        internal static new CbsHandler FromXml(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            var cbsHandler = new CbsHandler()
            {
                HandlerType = UpdateHandlerType.CBS
            };

            cbsHandler.ExtractAttributesFromXml(
                new string[] { "PackageIdentity" },
                "cbs:CbsData/@*",
                metadataNavigator,
                namespaceManager);

            return cbsHandler;
        }
    }
}
