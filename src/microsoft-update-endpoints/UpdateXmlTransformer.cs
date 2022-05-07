// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ClientSync
{
    /// <summary>
    /// Convert the update XML into various fragments to be sent to clients.
    /// See https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-wusp/7b42ccc2-770b-4452-a0f8-e731474ad619
    /// </summary>
    class UpdateXmlTransformer
    {
        private static readonly string[] AttributesToRemoveInExtendedFragment = {
            "UpdateType",
            "ExplicitlyDeployable",
            "AutoSelectOnWebSites",
            "EulaID",
            "PublicationState",
            "PublisherID",
            "CreationDate",
            "IsPublic",
            "LegacyName",
            "DetectoidType",
            "OSUpgrade",
            "PerUser"
        };

        private static readonly string[] AttributesToKeepInCoreFragment =
        {
            "UpdateType", "AutoSelectOnWebSites", "EulaID", "ExplicitlyDeployable", "OSUpgrade"
        };

        /// <summary>
        /// Gets a core XML fragment from full update metadata XML.
        /// See: https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-wusp/7b42ccc2-770b-4452-a0f8-e731474ad619
        /// </summary>
        /// <param name="metadataXml">Complete update XML</param>
        /// <returns>Core fragment</returns>
        public static string GetCoreFragmentFromMetadataXml(string metadataXml)
        {
            XDocument xml = XDocument.Parse(metadataXml);
            StripNamespacesFromXml(xml);

            var identity = xml.Root.XPathSelectElements("/Update/UpdateIdentity").First();

            var properties = xml.Root.XPathSelectElements("/Update/Properties").First();
            var filteredProperties = FilterElementAttributes(properties, AttributesToKeepInCoreFragment);

            var relationships = xml.Root.XPathSelectElements("/Update/Relationships").FirstOrDefault();
            var applicabilityRules = xml.Root.XPathSelectElements("/Update/ApplicabilityRules").FirstOrDefault();

            if (applicabilityRules != null)
            {
                RemoveDriverMetadataNodes(applicabilityRules);
            }

            return identity.ToString(SaveOptions.DisableFormatting) + filteredProperties.ToString(SaveOptions.DisableFormatting) + relationships?.ToString(SaveOptions.DisableFormatting) + applicabilityRules?.ToString(SaveOptions.DisableFormatting);
        }

        private static void RemoveDriverMetadataNodes(XElement element)
        {
            foreach(var driverMetadataElement in element.XPathSelectElements("/Update/ApplicabilityRules/Metadata/d.WindowsDriverMetaData"))
            {
                driverMetadataElement.RemoveNodes();
            }
        }

        private static XElement FilterElementAttributes(XElement element, string[] attributesToKeep)
        {
            var attributesList = element.Attributes().ToList();
            element.RemoveAttributes();

            foreach (var attribute in attributesList)
            {
                if (attributesToKeep.Contains(attribute.Name.LocalName))
                {
                    element.Add(attribute);
                }
            }

            return element;
        }

        private static XElement RemoveElementAttributes(XElement element, string[] attributesToRemove)
        {
            var attributesList = element.Attributes().ToList();
            element.RemoveAttributes();

            foreach (var attribute in attributesList)
            {
                if (!attributesToRemove.Contains(attribute.Name.LocalName))
                {
                    element.Add(attribute);
                }
            }

            return element;
        }

        private static void StripNamespacesFromXml(XDocument xml)
        {
            foreach (XElement XE in xml.Root.DescendantsAndSelf())
            {
                // Stripping the namespace by setting the name of the element to it's localname only

                if (XE.Name.Namespace.NamespaceName.Equals("http://schemas.microsoft.com/msus/2002/12/BaseApplicabilityRules"))
                {
                    XE.Name = $"b.{XE.Name.LocalName}";
                }
                else if (XE.Name.Namespace.NamespaceName.Equals("http://schemas.microsoft.com/msus/2002/12/MsiApplicabilityRules"))
                {
                    XE.Name = $"m.{XE.Name.LocalName}";
                }
                else if (XE.Name.Namespace.NamespaceName.Equals("http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsDriver"))
                {
                    XE.Name = $"d.{XE.Name.LocalName}";
                }
                else
                {
                    XE.Name = XE.Name.LocalName;
                }
                // replacing all attributes with attributes that are not namespaces and their names are set to only the localname
                XE.ReplaceAttributes((from xattrib in XE.Attributes().Where(xa => !xa.IsNamespaceDeclaration) select new XAttribute(xattrib.Name.LocalName, xattrib.Value)));
            }
        }

        /// <summary>
        /// Get an extended fragment from full update metadata
        /// See https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-wusp/7b42ccc2-770b-4452-a0f8-e731474ad619
        /// </summary>
        /// <param name="metadataXml">Update metadata XML</param>
        /// <returns>Extended fragment</returns>
        public static string GetExtendedFragmentFromMetadataXml(string metadataXml)
        {
            XDocument xml = XDocument.Parse(metadataXml);
            StripNamespacesFromXml(xml);

            var properties = xml.Root.XPathSelectElements("/Update/Properties").First();
            var filteredProperties = RemoveElementAttributes(properties, AttributesToRemoveInExtendedFragment);
            filteredProperties.Name = XName.Get("ExtendedProperties");

            var files = xml.Root.XPathSelectElements("/Update/Files").FirstOrDefault();
            var handlerSpecificData = xml.Root.XPathSelectElements("/Update/HandlerSpecificData").FirstOrDefault();

            return filteredProperties.ToString(SaveOptions.DisableFormatting) + files?.ToString(SaveOptions.DisableFormatting) + handlerSpecificData?.ToString(SaveOptions.DisableFormatting);
        }

        /// <summary>
        /// Gets localized properties fragment from full update metadata
        /// </summary>
        /// <param name="metadataXml">Update metadata XML</param>
        /// <param name="languages">Language to get the localized properties for</param>
        /// <returns>Localized properties fragment</returns>
        public static string GetLocalizedPropertiesFromMetadataXml(string metadataXml, string[] languages)
        {
            XDocument xml = XDocument.Parse(metadataXml);
            StripNamespacesFromXml(xml);

            var localizedProperties = xml.Root.XPathSelectElements("/Update/LocalizedPropertiesCollection/LocalizedProperties");

            foreach(var localizedProperty in localizedProperties)
            {
                var languageElement = localizedProperty.XPathSelectElement("Language");
                if (languageElement != null && languages.Contains(languageElement.Value))
                {
                    return localizedProperty.ToString(SaveOptions.DisableFormatting);
                }
                else
                {
                    continue;
                }
            }

            return null;
        }
    }
}
