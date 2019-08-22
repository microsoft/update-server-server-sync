// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Compression;
using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.Metadata.Prerequisites;
using Microsoft.UpdateServices.Storage;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// The UpdateType enumeration defines various types of updates
    /// </summary>
    internal enum UpdateType : uint
    {
        /// <summary>
        /// <see cref="Metadata.Detectoid"/>
        /// </summary>
        Detectoid = 0,
        /// <summary>
        /// <see cref="Metadata.Classification"/>
        /// </summary>
        Classification = 1,
        /// <summary>
        /// <see cref="Metadata.Product"/>
        /// </summary>
        Product = 2,
        /// <summary>
        /// <see cref="Metadata.DriverUpdate"/>
        /// </summary>
        Driver = 3,
        /// <summary>
        /// <see cref="Metadata.SoftwareUpdate"/>
        /// </summary>
        Software = 4
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
        public Identity Identity { get; private set; }

        private string _Title;
        /// <summary>
        /// Get the category or update title
        /// </summary>
        public string Title
        {
            get
            {
                if (_Title == null)
                {
                    _Title = MetadataSource.GetUpdateTitle(this.Identity);
                }

                return _Title;
            }
        }

        /// <summary>
        /// Set for updates that bundle other updates
        /// </summary>
        public bool IsBundle =>MetadataSource.IsBundle(this.Identity);

        /// <summary>
        /// Set for updates that are bundled together with other updates
        /// </summary>
        public bool IsBundled => MetadataSource.IsBundled(this.Identity);

        /// <summary>
        /// List of bundled updates
        /// </summary>
        /// <value>
        /// List of update identities.
        /// </value>
        public IEnumerable<Identity> BundledUpdates => MetadataSource.GetBundledUpdates(this.Identity);

        /// <summary>
        /// Gets the update within which this update is bundled
        /// </summary>
        public IEnumerable<Identity> BundleParent => MetadataSource.GetBundle(this.Identity);

        /// <summary>
        /// Determine if the update has prerequisites
        /// </summary>
        public bool HasPrerequisites => MetadataSource.HasPrerequisites(this.Identity);

        /// <summary>
        /// Check if this update has a parent product
        /// </summary>
        public bool HasProduct => MetadataSource.HasProduct(this.Identity);

        /// <summary>
        /// Check if this update has a classification
        /// </summary>
        public bool HasClassification => MetadataSource.HasClassification(this.Identity);

        /// <summary>
        /// Gets the list of product IDs for the update
        /// </summary>
        /// <value>List of product IDs. The GUIDs map to a <see cref="Product"/></value>
        [JsonProperty]
        public List<Guid> ProductIds => MetadataSource.GetUpdateProductIds(this.Identity);

        /// <summary>
        /// Gets the list of classifications for the driver update
        /// </summary>
        /// <value>List of classification IDs. The GUIDs map to a <see cref="Classification"/></value>
        [JsonProperty]
        public List<Guid> ClassificationIds => MetadataSource.GetUpdateClassificationIds(this.Identity);

        /// <summary>
        /// Get the list of prerequisites
        /// </summary>
        /// <value>
        /// List of prerequisites
        /// </value>
        public List<Prerequisites.Prerequisite> Prerequisites => MetadataSource.GetPrerequisites(this.Identity);

        /// <summary>
        /// Check if an update superseds other updates
        /// </summary>
        public bool IsSupersedingUpdates => MetadataSource.IsSuperseding(this.Identity);

        /// <summary>
        /// Check if an update superseds other updates
        /// </summary>
        public bool IsSuperseded => MetadataSource.IsSuperseded(this.Identity);

        /// <summary>
        /// List of Update Ids superseded by an update.
        /// </summary>
        /// <value>List of update <see cref="Identity"/></value>
        public IReadOnlyList<Guid> SupersededUpdates => MetadataSource.GetSupersededUpdates(this.Identity);

        /// <summary>
        /// Get the update that superseded this update
        /// </summary>
        /// <value>The identity of the update that superseds this update</value>
        public Identity SupersedingUpdate => MetadataSource.GetSupersedingUpdate(this.Identity);

        /// <summary>
        /// Determines if the update is applicable based on its list of prerequisites and the list of installed updates (prerequisites) on a computer
        /// </summary>
        /// <param name="installedPrerequisites">List of installed updates on a computer</param>
        /// <returns>True if all prerequisites are met, false otherwise</returns>
        public bool IsApplicable(List<Guid> installedPrerequisites)
        {
            return PrerequisitesAnalyzer.IsApplicable(this, installedPrerequisites);
        }

        internal string _Description;

        /// <summary>
        /// Get the category or update description
        /// </summary>
        [JsonIgnore]
        public string Description
        {
            get
            {
                LoadAttributesFromMetadataSource();
                return _Description;
            }
        }

        /// <summary>
        /// Check if an update contains content files
        /// </summary>
        public bool HasFiles => MetadataSource.HasFiles(this.Identity);

        /// <summary>
        /// Gets the list of files (content) for update
        /// </summary>
        /// <value>
        /// List of content files
        /// </value>
        public List<UpdateFile> Files => MetadataSource.GetFiles(this.Identity);

        /// <summary>
        /// True if extended attributes have been loaded
        /// </summary>
        [JsonIgnore]
        internal bool AttributesLoaded = false;

        internal readonly IMetadataSource MetadataSource;

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

        internal Update(Identity id, IMetadataSource source)
        {
            Identity = id;
            MetadataSource = source;
        }

        /// <summary>
        /// Construct an update from update metadata (XML)
        /// </summary>
        /// <param name="id">Update ID</param>
        /// <param name="xdoc">Update XML metadata</param>
        internal static Update FromUpdateXml(Identity id, XDocument xdoc)
        {
            // Get the update type
            var updateType = GetUpdateTypeFromXml(xdoc).ToLowerInvariant();
            switch (updateType)
            {
                case "detectoid":
                    return new Detectoid(id, xdoc);

                case "category":
                    var categoryType = GetCategoryFromXml(xdoc).ToLowerInvariant();
                    if (categoryType == "updateclassification")
                    {
                        return new Classification(id, xdoc);
                    }
                    else if (categoryType == "product" || categoryType == "company" || categoryType == "productfamily")
                    {
                        return new Product(id, xdoc);
                    }
                    else
                    {
                        throw new Exception($"Unexpected category type {categoryType}");
                    }

                case "driver":
                    return new DriverUpdate(id, xdoc);

                case "software":
                    return new SoftwareUpdate(id, xdoc);

                default:
                    throw new Exception($"Unexpected update type: {updateType}");
            }
        }

        /// <summary>
        /// Loads extended attributes from XML. Classes that inherit should provide an implementation.
        /// </summary>
        internal virtual void LoadAttributesFromMetadataSource()
        {
            lock (this)
            {
                if (!AttributesLoaded)
                {
                    using (var metadataStream = MetadataSource.GetUpdateMetadataStream(Identity))
                    using (var metadataReader = new StreamReader(metadataStream))
                    {
                        var xdoc = XDocument.Parse(metadataReader.ReadToEnd(), LoadOptions.None);
                        GetDescriptionFromXml(xdoc);
                    }

                    AttributesLoaded = true;
                }
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
        internal void GetDescriptionFromXml(XDocument xdoc)
        {
            // Get the title and description (if available)
            var localizedProperties = xdoc.Descendants(XName.Get("LocalizedProperties", "http://schemas.microsoft.com/msus/2002/12/Update"));
            foreach (var localizedProperty in localizedProperties)
            {
                var language = localizedProperty.Descendants(XName.Get("Language", "http://schemas.microsoft.com/msus/2002/12/Update")).First();
                if (language.Value == "en")
                {
                    var descriptions = localizedProperty.Descendants(XName.Get("Description", "http://schemas.microsoft.com/msus/2002/12/Update"));
                    if (descriptions.Count() > 0)
                    {
                        _Description = descriptions.First().Value;
                    }

                    return;
                }
            }

            throw new Exception("Cannot find update title");
        }

        /// <summary>
        /// Parse update title and description
        /// </summary>
        /// <param name="xdoc"></param>
        /// <returns></returns>
        internal static string GetTitleFromXml(XDocument xdoc)
        {
            // Get the title and description (if available)
            var localizedProperties = xdoc.Descendants(XName.Get("LocalizedProperties", "http://schemas.microsoft.com/msus/2002/12/Update"));
            foreach (var localizedProperty in localizedProperties)
            {
                var language = localizedProperty.Descendants(XName.Get("Language", "http://schemas.microsoft.com/msus/2002/12/Update")).First();
                if (language.Value == "en")
                {
                    return localizedProperty.Descendants(XName.Get("Title", "http://schemas.microsoft.com/msus/2002/12/Update")).First().Value;
                }
            }

            throw new Exception("Cannot find update title");
        }
    }
}
