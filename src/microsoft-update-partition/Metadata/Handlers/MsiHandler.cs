// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Handlers
{
    /// <summary>
    /// Metadata for the MSI install handler
    /// </summary>
    public class MsiHandler : HandlerMetadata
    {
        /// <summary>
        /// Product code being installed
        /// </summary>
        [JsonProperty]
        public string ProductCode { get; private set; }

        /// <summary>
        /// Main MSI file to be installed
        /// </summary>
        [JsonProperty]
        public string MsiFile { get; private set; }

        /// <summary>
        /// Command line for launching MSI installation
        /// </summary>
        [JsonProperty]
        public string CommandLine { get; private set; }

        [JsonConstructor]
        private MsiHandler()
        {

        }

        internal static new MsiHandler FromXml(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            var msiHandler = new MsiHandler()
            {
                HandlerType = UpdateHandlerType.MSI
            };

            msiHandler.ExtractAttributesFromXml(new string[] { "ProductCode", "MsiFile", "CommandLine" }, "msp:MsiData/@*", metadataNavigator, namespaceManager);

            return msiHandler;
        }
    }
}
