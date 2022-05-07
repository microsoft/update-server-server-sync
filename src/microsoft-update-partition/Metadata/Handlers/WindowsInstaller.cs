// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Handlers
{
    /// <summary>
    /// Metadata fo the windows installer handler
    /// </summary>
    public class WindowsInstallerHandler : HandlerMetadata
    {
        /// <summary>
        /// Install command line
        /// </summary>
        [JsonProperty]
        public string CommandLine { get; private set; }

        /// <summary>
        /// Uninstall command line
        /// </summary>
        [JsonProperty]
        public string UninstallCommandLine { get; private set; }

        /// <summary>
        /// Full file patch code
        /// </summary>
        [JsonProperty]
        public string FullFilePatchCode { get; private set; }

        /// <summary>
        /// Patch code
        /// </summary>
        [JsonProperty]
        public string PatchCode { get; private set; }

        [JsonConstructor]
        private WindowsInstallerHandler()
        {

        }

        internal static new WindowsInstallerHandler FromXml(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            var msiHandler = new WindowsInstallerHandler()
            {
                HandlerType = UpdateHandlerType.WindowsInstaller
            };

            msiHandler.ExtractAttributesFromXml(
                new string[] { "CommandLine", "UninstallCommandLine", "FullFilePatchCode", "PatchCode" },
                "msp:MspData/@*",
                metadataNavigator,
                namespaceManager);

            return msiHandler;
        }
    }
}
