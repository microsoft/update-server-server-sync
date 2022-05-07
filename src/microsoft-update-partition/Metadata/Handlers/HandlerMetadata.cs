// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Handlers
{
    /// <summary>
    /// Possible types of update handlers used by Microsoft Update
    /// </summary>
    public enum UpdateHandlerType
    {
        /// <summary>
        /// Command line handler; installs updates by executing local scripts or programs
        /// </summary>
        CommandLine,

        /// <summary>
        /// OS update handler; installs cumulative updates through CBS
        /// </summary>
        OS,

        /// <summary>
        /// OS update handler; installs individual packages through CBS
        /// </summary>
        CBS,

        /// <summary>
        /// MSI update handler; installs updates through MSI
        /// </summary>
        MSI,

        /// <summary>
        /// No information available.
        /// </summary>
        Category,

        /// <summary>
        /// No information available.
        /// </summary>
        WindowsPatch,

        /// <summary>
        /// OS update handler; installs Windows feature updates
        /// </summary>
        WindowsSetup,

        /// <summary>
        /// No information available
        /// </summary>
        WindowsInstaller,
    }

    /// <summary>
    /// Base class for update handler metadata classes.
    /// </summary>
    public class HandlerMetadata
    {
        /// <summary>
        /// The handler type.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public UpdateHandlerType HandlerType { get; set; }

        [JsonConstructor]
        internal HandlerMetadata()
        {

        }

        internal static HandlerMetadata FromXml(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            XPathExpression handlerSpecificDataQuery = metadataNavigator.Compile("upd:Update/upd:HandlerSpecificData");
            handlerSpecificDataQuery.SetContext(namespaceManager);

            var handlerSpecificDataResult = metadataNavigator.Evaluate(handlerSpecificDataQuery) as XPathNodeIterator;
            while (handlerSpecificDataResult.MoveNext())
            {
                var type = handlerSpecificDataResult.Current.GetAttribute("type", "http://www.w3.org/2001/XMLSchema-instance");
                return type switch
                {
                    "cmd:CommandLineInstallation" => CommandLineHandler.FromXml(handlerSpecificDataResult.Current, namespaceManager),
                    "cbs:Cbs" => CbsHandler.FromXml(handlerSpecificDataResult.Current, namespaceManager),
                    "cat:Category" => CategoryHandler.FromXml(handlerSpecificDataResult.Current, namespaceManager),
                    "msp:WindowsInstallerApp" => MsiHandler.FromXml(handlerSpecificDataResult.Current, namespaceManager),
                    "msp:WindowsInstaller" => WindowsInstallerHandler.FromXml(handlerSpecificDataResult.Current, namespaceManager),
                    "OSInstallerMetadata" => OsInstallerHandler.FromXml(handlerSpecificDataResult.Current, namespaceManager),
                    "psf:WindowsPatch" => WindowsPatchHandler.FromXml(handlerSpecificDataResult.Current, namespaceManager),
                    "wsi:WindowsSetup" => WindowsSetupHandler.FromXml(handlerSpecificDataResult.Current, namespaceManager),
                    _ => throw new NotImplementedException($"Handler type `{handlerSpecificDataResult.Current.Value}` not implemented."),
                };
            }

            return null;
        }

        internal void ExtractAttributesFromXml(string[] attributeNames, string xpath, XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            XPathExpression attributesQuery = metadataNavigator.Compile(xpath);
            attributesQuery.SetContext(namespaceManager);

            var attributesQueryResult = metadataNavigator.Evaluate(attributesQuery) as XPathNodeIterator;

            while (attributesQueryResult.MoveNext())
            {
                if (attributeNames.Contains(attributesQueryResult.Current.Name))
                {
                    Type type = this.GetType();

                    PropertyInfo prop = type.GetProperty(attributesQueryResult.Current.Name);

                    if (prop.PropertyType == typeof(string))
                    {
                        prop.SetValue(this, attributesQueryResult.Current.Value, null);
                    }
                    else if (prop.PropertyType == typeof(bool?))
                    {
                        prop.SetValue(this, attributesQueryResult.Current.ValueAsBoolean, null);
                    }
                    else if (prop.PropertyType == typeof(int?))
                    {
                        prop.SetValue(this, attributesQueryResult.Current.ValueAsInt, null);
                    }
                    else
                    {
                        throw new NotImplementedException($"Type {prop.PropertyType} not implemented");
                    }
                }
                else
                {
                    throw new NotImplementedException($"Attribute {attributesQueryResult.Current.Name} not implemented.");
                }
            }
        }
    }
}
