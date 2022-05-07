// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Handlers
{
    /// <summary>
    /// Metadata for the handler responsible with Windows feature updates.
    /// </summary>
    public class WindowsSetupHandler : HandlerMetadata
    {
        /// <summary>
        /// The entry point for starting the windows feature update process
        /// </summary>
        [JsonProperty]
        public string Program {get; private set; }

        /// <summary>
        /// Whether this feature update is setup360 or not
        /// </summary>
        [JsonProperty]
        public bool? IsSetup360 { get; private set; }

        [JsonConstructor]
        private WindowsSetupHandler()
        {

        }

        internal static new WindowsSetupHandler FromXml(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            var windowsSetupHandler = new WindowsSetupHandler()
            {
                HandlerType = UpdateHandlerType.WindowsPatch
            };

            windowsSetupHandler.ExtractAttributesFromXml(
                new string[] { "IsSetup360", "Program" },
                "wsi:InstallCommand/@*",
                metadataNavigator,
                namespaceManager);

            return windowsSetupHandler;
        }
    }
}
