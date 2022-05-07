// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Handlers
{
    /// <summary>
    /// Metadata for the command line update handler. This handler applies updates by running various scripts on a device.
    /// </summary>
    public class CommandLineHandler : HandlerMetadata
    {
        /// <summary>
        /// The program to invoke in order to perform the updates
        /// </summary>
        [JsonProperty]
        public string Program {get; private set; }

        /// <summary>
        /// The arguments to pass to the update program
        /// </summary>
        [JsonProperty]
        public string Arguments { get; private set; }

        /// <summary>
        /// Whether to reboot the device, regardless of the return code
        /// </summary>
        [JsonProperty]
        public bool? RebootByDefault { get; private set; }

        /// <summary>
        /// The default result code expected from the update program
        /// </summary>
        [JsonProperty]
        public string DefaultResult { get; private set; }

        /// <summary>
        /// Expected result codes
        /// </summary>
        [JsonProperty]
        public List<ReturnCode> ReturnCodes { get; private set; }

        [JsonConstructor]
        private CommandLineHandler()
        {

        }

        internal static new CommandLineHandler FromXml(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            var commandLineHandler = new CommandLineHandler()
            {
                HandlerType = UpdateHandlerType.CommandLine
            };

            commandLineHandler.ParseCommandLineMetadata(metadataNavigator, namespaceManager);

            return commandLineHandler;
        }

        private void ParseCommandLineMetadata(XPathNavigator handlerMetadataNavigator, XmlNamespaceManager namespaceManager)
        {
            ExtractAttributesFromXml(
                new string[] { "Program", "Arguments", "RebootByDefault", "DefaultResult"},
                "cmd:InstallCommand/@*",
                handlerMetadataNavigator,
                namespaceManager);

            XPathExpression commandLineRetCodeQuery = handlerMetadataNavigator.Compile("cmd:InstallCommand/cmd:ReturnCode");
            commandLineRetCodeQuery.SetContext(namespaceManager);
            var retCodeQueryResult = handlerMetadataNavigator.Evaluate(commandLineRetCodeQuery) as XPathNodeIterator;

            ReturnCodes = new List<ReturnCode>();
            while (retCodeQueryResult.MoveNext())
            {
                ReturnCodes.Add(ReturnCode.FromXml(retCodeQueryResult.Current, namespaceManager));
            }
        }
    }
}
