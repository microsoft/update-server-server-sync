// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Handlers
{
    /// <summary>
    /// Metadata for representing the expected return codes of various update handlers
    /// </summary>
    public class ReturnCode
    {
        /// <summary>
        /// Whether a return code indicates reboot required
        /// </summary>
        [JsonProperty]
        public bool? Reboot { get; private set; }

        /// <summary>
        /// The numerical return code
        /// </summary>
        [JsonProperty]
        public int? Code { get; private set; }

        /// <summary>
        /// Corresponding result string
        /// </summary>
        [JsonProperty]
        public string Result { get; private set; }

        /// <summary>
        /// Localized result string
        /// </summary>
        [JsonProperty]
        public string DefaultLocalizedDescription { get; private set; }

        [JsonConstructor]
        private ReturnCode()
        {

        }

        internal static ReturnCode FromXml(XPathNavigator returnCodeNavigator, XmlNamespaceManager namespaceManager)
        {
            var returnCode = new ReturnCode();

            XPathExpression attributesQuery = returnCodeNavigator.Compile("@*");
            attributesQuery.SetContext(namespaceManager);
            var attributesQueryResult = returnCodeNavigator.Evaluate(attributesQuery) as XPathNodeIterator;

            while (attributesQueryResult.MoveNext())
            {
                switch (attributesQueryResult.Current.Name)
                {
                    case "Reboot":
                        returnCode.Reboot = attributesQueryResult.Current.ValueAsBoolean;
                        break;

                    case "Result":
                        returnCode.Result = attributesQueryResult.Current.Value;
                        break;

                    case "Code":
                        returnCode.Code = attributesQueryResult.Current.ValueAsInt;
                        break;

                    case "DefaultLocalizedDescription":
                        returnCode.DefaultLocalizedDescription = attributesQueryResult.Current.Value;
                        break;

                    default:
                        throw new NotImplementedException($"Attribute {attributesQueryResult.Current.Name} not implemented for ReturnCode in handler");
                }
            }

            if (!returnCode.Code.HasValue || string.IsNullOrEmpty(returnCode.Result))
            {
                throw new NotSupportedException("Missing return code metadata in ReturnCode handler");
            }
            
            return returnCode;
        }
    }
}
