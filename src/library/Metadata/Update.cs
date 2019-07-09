// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Compression;
using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// The UpdateType enumeration defines various types of updates
    /// </summary>
    internal enum UpdateType
    {
        /// <summary>
        /// <see cref="Metadata.Detectoid"/>
        /// </summary>
        Detectoid,
        /// <summary>
        /// <see cref="Metadata.Classification"/>
        /// </summary>
        Classification,
        /// <summary>
        /// <see cref="Metadata.Product"/>
        /// </summary>
        Product,
        /// <summary>
        /// <see cref="Metadata.DriverUpdate"/>
        /// </summary>
        Driver,
        /// <summary>
        /// <see cref="Metadata.SoftwareUpdate"/>
        /// </summary>
        Software
    }

    /// <summary>
    /// A base class for all updates stored on an upstream update server. Stores generic update metadata applicable to both categories and updates.
    /// </summary>
    public abstract class Update
    {
        /// <summary>
        /// Gets the update or category identity, consisting of a GUID and revision number
        /// </summary>
        /// <value>
        /// Update identity.
        /// </value>
        [JsonProperty]
        public Identity Identity { get; private set; }

        /// <summary>
        /// The type of update
        /// </summary>
        [JsonProperty]
        internal UpdateType UpdateType;

        /// <summary>
        /// Get the category or update title
        /// </summary>
        [JsonProperty]
        public string Title { get; private set; }

        /// <summary>
        /// Get the category or update description
        /// </summary>
        [JsonProperty]
        public string Description { get; private set; }

        /// <summary>
        /// Time when the update was added to the repository
        /// </summary>
        [JsonProperty]
        internal DateTime LastChanged;

        /// <summary>
        /// Check if the update is superseded by another update
        /// </summary>
        [JsonProperty]
        public bool IsSuperseded { get; internal set; }

        /// <summary>
        /// XML data received from the server. It is not serialized with this object but rather
        /// saved independently to an XML file on disk
        /// </summary>
        [JsonIgnore]
        internal string XmlData;

        /// <summary>
        /// True if extended attributes have been loaded
        /// </summary>
        [JsonIgnore]
        internal bool ExtendedAttributesLoaded = false;

        internal bool MatchTitle(string[] keywords)
        {
            foreach(var keyword in keywords)
            {
                if (!Title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        [JsonConstructor]
        internal Update() { }

        internal Update(ServerSyncUpdateData serverSyncUpdateData)
        {
            Identity = new Identity(serverSyncUpdateData.Id);
        }

        /// <summary>
        /// Construct an update by decoding the contained XML
        /// </summary>
        /// <param name="serverSyncUpdateData"></param>
        internal static Update FromServerSyncUpdateData(ServerSyncUpdateData serverSyncUpdateData)
        {
            // We need to parse the XML update blob
            string updateXml = serverSyncUpdateData.XmlUpdateBlob;
            if (string.IsNullOrEmpty(updateXml))
            {
                // If the plain text blob is not availabe, use the compressed XML blob
                if (serverSyncUpdateData.XmlUpdateBlobCompressed == null || serverSyncUpdateData.XmlUpdateBlobCompressed.Length == 0)
                {
                    throw new Exception("Missing XmlUpdateBlobCompressed");
                }

                // Note: This only works on Windows.
                updateXml = CabinetUtility.DecompressData(serverSyncUpdateData.XmlUpdateBlobCompressed);
            }

            var xdoc = XDocument.Parse(updateXml, LoadOptions.None);

            // Get the update type
            var updateType = GetUpdateTypeFromXml(xdoc).ToLowerInvariant();
            switch (updateType)
            {
                case "detectoid":
                    return new Detectoid(serverSyncUpdateData, xdoc)
                    {
                        XmlData = updateXml
                    };

                case "category":
                    var categoryType = GetCategoryFromXml(xdoc).ToLowerInvariant();
                    if (categoryType == "updateclassification")
                    {
                        return new Classification(serverSyncUpdateData, xdoc)
                        {
                            XmlData = updateXml
                        };
                    }
                    else if (categoryType == "product" || categoryType == "company" || categoryType == "productfamily")
                    {
                        return new Product(serverSyncUpdateData, xdoc)
                        {
                            XmlData = updateXml
                        };
                    }
                    else
                    {
                        throw new Exception($"Unexpected category type {categoryType}");
                    }

                case "driver":
                    return new DriverUpdate(serverSyncUpdateData, xdoc)
                    {
                        XmlData = updateXml
                    };

                case "software":
                    return new SoftwareUpdate(serverSyncUpdateData, xdoc)
                    {
                        XmlData = updateXml
                    };

                default:
                    throw new Exception($"Unexpected update type: {updateType}");
            }
        }

        /// <summary>
        /// Loads extended attributes from XML. Classes that inherit should provide an implementation.
        /// </summary>
        /// <param name="xmlReader">The XML stream</param>
        /// <param name="contentFiles">Dictionary of known update files. Used to resolve file hashes to URLs</param>
        internal virtual void LoadExtendedAttributesFromXml(StreamReader xmlReader, Dictionary<string, UpdateFileUrl> contentFiles)
        {
            ExtendedAttributesLoaded = true;
        }

        /// <summary>
        /// Parses the XML and determines the actual type of the update encoded in the XML
        /// </summary>
        /// <param name="xdoc"></param>
        /// <returns></returns>
        private static string GetUpdateTypeFromXml(XDocument xdoc)
        {
            // Parse the Properties node from the XML
            var propertiesNodes = xdoc.Descendants(XName.Get("Properties", "http://schemas.microsoft.com/msus/2002/12/Update"));
            if (propertiesNodes == null || propertiesNodes.Count() == 0)
            {
                throw new Exception("Unexpected XmlUpdateBlob content");
            }

            var propertyNode = propertiesNodes.First();
            if (!propertyNode.HasAttributes)
            {
                throw new Exception("Missing attributes in Properties node");
            }

            // Get the update type
            try
            {
                return propertyNode.Attribute("UpdateType").Value;
            }
            catch (Exception)
            {
                throw new Exception("Cannot find UpdateType attribute");
            }
        }

        /// <summary>
        /// For a "category" update, parses the actual category type
        /// </summary>
        /// <param name="xdoc"></param>
        /// <returns></returns>
        private static string GetCategoryFromXml(XDocument xdoc)
        {
            try
            {
                var handlerData = xdoc.Descendants(XName.Get("HandlerSpecificData", "http://schemas.microsoft.com/msus/2002/12/Update")).First();
                var categoryInformation = handlerData.Descendants(XName.Get("CategoryInformation", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/Category")).First();
                return categoryInformation.Attribute("CategoryType").Value;
            }
            catch (Exception)
            {
                throw new Exception("Cannot find CategoryType attribute");
            }
        }

        /// <summary>
        /// Parse update title and description
        /// </summary>
        /// <param name="xdoc"></param>
        /// <returns></returns>
        internal void GetTitleAndDescriptionFromXml(XDocument xdoc)
        {
            // Get the title and description (if available)
            var localizedProperties = xdoc.Descendants(XName.Get("LocalizedProperties", "http://schemas.microsoft.com/msus/2002/12/Update"));
            foreach (var localizedProperty in localizedProperties)
            {
                var language = localizedProperty.Descendants(XName.Get("Language", "http://schemas.microsoft.com/msus/2002/12/Update")).First();
                if (language.Value == "en")
                {
                    Title = localizedProperty.Descendants(XName.Get("Title", "http://schemas.microsoft.com/msus/2002/12/Update")).First().Value;

                    var descriptions = localizedProperty.Descendants(XName.Get("Description", "http://schemas.microsoft.com/msus/2002/12/Update"));
                    if (descriptions.Count() > 0)
                    {
                        Description = descriptions.First().Value;
                    }

                    return;
                }
            }

            throw new Exception("Cannot find update title");
        }
    }
}
