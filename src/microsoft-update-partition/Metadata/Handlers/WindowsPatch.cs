// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Handlers
{
    /// <summary>
    /// Metadata for a Windows patch handler
    /// </summary>
    public class WindowsPatchHandler : HandlerMetadata
    {
        /// <summary>
        /// Install parameters for the patch
        /// </summary>
        [JsonProperty]
        public string InstallParameters {get; private set; }

        /// <summary>
        /// Unpacking parameters for the patch
        /// </summary>
        [JsonProperty]
        public string UnpackParameters { get; private set; }

        [JsonConstructor]
        private WindowsPatchHandler()
        {

        }

        internal static new WindowsPatchHandler FromXml(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            var windowsPatchHandler = new WindowsPatchHandler()
            {
                HandlerType = UpdateHandlerType.WindowsPatch
            };

            windowsPatchHandler.ExtractAttributesFromXml(
                new string[] { "InstallParameters", "UnpackParameters" },
                "psf:WindowsPatchData/@*",
                metadataNavigator,
                namespaceManager);

            return windowsPatchHandler;
        }
    }
}
