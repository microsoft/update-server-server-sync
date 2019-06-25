// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Compression;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Types of updates
    /// </summary>
    public enum MicrosoftUpdateType
    {
        Detectoid,
        Classification,
        Product,
        Driver,
        Software
    }

    /// <summary>
    /// Stores update data in a JSON friendly format. The data is parsed from the XML contained in ServerSyncUpdateData.
    /// MicrosoftUpdate is abstract and is actually a factory for other concrete update types
    /// </summary>
    public abstract class MicrosoftUpdate
    {
        /// <summary>
        /// The update's identity, consisting of a GUID and revision number
        /// </summary>
        public MicrosoftUpdateIdentity Identity { get; set; }

        /// <summary>
        /// The type of update
        /// </summary>
        public MicrosoftUpdateType UpdateType { get; set; }

        /// <summary>
        /// XML data received from the server. It is not serialized with this object but rather
        /// saved independently to an XML file on disk
        /// </summary>
        [JsonIgnore]
        public string XmlData { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public bool MatchTitle(string[] keywords)
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
        protected MicrosoftUpdate() { }

        protected MicrosoftUpdate(ServerSyncUpdateData serverSyncUpdateData)
        {
            Identity = new MicrosoftUpdateIdentity(serverSyncUpdateData.Id);
        }

        /// <summary>
        /// Construct an update by decoding the contained XML
        /// </summary>
        /// <param name="serverSyncUpdateData"></param>
        /// /// <param name="urlData">URL data for the files referenced in the update (if any)</param>
        public static MicrosoftUpdate FromServerSyncUpdateData(ServerSyncUpdateData serverSyncUpdateData, List<UpdateFileUrl> urlData)
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

            // Get the title and optional description
            var titleAndDescription = GetTitleAndDescriptionFromXml(xdoc);

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
                        return new MicrosoftProduct(serverSyncUpdateData, xdoc)
                        {
                            XmlData = updateXml
                        };
                    }
                    else
                    {
                        throw new Exception($"Unexpected category type {categoryType}");
                    }

                case "driver":
                    return new DriverUpdate(serverSyncUpdateData, xdoc, urlData)
                    {
                        XmlData = updateXml
                    };

                case "software":
                    return new SoftwareUpdate(serverSyncUpdateData, xdoc, urlData)
                    {
                        XmlData = updateXml
                    };

                default:
                    throw new Exception($"Unexpected update type: {updateType}");
            }
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
        protected static KeyValuePair<string, string> GetTitleAndDescriptionFromXml(XDocument xdoc)
        {
            // Get the title and description (if available)
            var localizedProperties = xdoc.Descendants(XName.Get("LocalizedProperties", "http://schemas.microsoft.com/msus/2002/12/Update"));
            foreach (var localizedProperty in localizedProperties)
            {
                var language = localizedProperty.Descendants(XName.Get("Language", "http://schemas.microsoft.com/msus/2002/12/Update")).First();
                if (language.Value == "en")
                {
                    var title = localizedProperty.Descendants(XName.Get("Title", "http://schemas.microsoft.com/msus/2002/12/Update")).First().Value;

                    var descriptions = localizedProperty.Descendants(XName.Get("Description", "http://schemas.microsoft.com/msus/2002/12/Update"));
                    if (descriptions.Count() > 0)
                    {
                        return new KeyValuePair<string, string>(title, descriptions.First().Value);
                    }
                    else
                    {
                        return new KeyValuePair<string, string>(title, null);
                    }
                }
            }

            throw new Exception("Cannot find update title");
        }
    }

    /// <summary>
    /// Deserialization converter that instantiates the correct update object based on the type encoded in the JSON
    /// </summary>
    public class MicrosoftUpdateConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(MicrosoftUpdate).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader,
            Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject item = JObject.Load(reader);
            var updateType = (MicrosoftUpdateType)item["UpdateType"].Value<int>();

            JsonSerializer prerequisiteDeserializer = new JsonSerializer();
            prerequisiteDeserializer.Converters.Add(new Prerequisites.PrerequisiteConverter());

            if (updateType == MicrosoftUpdateType.Product)
            {
                return item.ToObject<MicrosoftProduct>(prerequisiteDeserializer);
            }
            else if (updateType == MicrosoftUpdateType.Detectoid)
            {
                return item.ToObject<Detectoid>(prerequisiteDeserializer);
            }
            else if (updateType == MicrosoftUpdateType.Classification)
            {
                return item.ToObject<Classification>(prerequisiteDeserializer);
            }
            else if (updateType == MicrosoftUpdateType.Driver)
            {
                return item.ToObject<DriverUpdate>(prerequisiteDeserializer);
            }
            else if (updateType == MicrosoftUpdateType.Software)
            {
                return item.ToObject<SoftwareUpdate>(prerequisiteDeserializer);
            }
            else
            {
                throw new Exception("Unexpected update type");
            }
        }

        public override void WriteJson(JsonWriter writer,
            object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
