// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Handlers
{
    /// <summary>
    /// Represents metadata for the Windows installer handler. This handler is responsible for installing cumulative updates.
    /// </summary>
    public class OsInstallerHandler : HandlerMetadata
    {
        /// <summary>
        /// The initial module to load in order to start the update process
        /// </summary>
        [JsonProperty]
        public string InitialModule { get; private set; }

        [JsonConstructor]
        private OsInstallerHandler()
        {

        }

        internal static new OsInstallerHandler FromXml(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            var osHandler = new OsInstallerHandler()
            {
                HandlerType = UpdateHandlerType.OS
            };

            osHandler.ExtractAttributesFromXml(new string[] { "InitialModule" }, "msp:OSInstallData/@*", metadataNavigator, namespaceManager);

            return osHandler;
        }
    }
}
