// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.Metadata.Prerequisites;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Represents a driver update.
    /// </summary>
    /// <example>
    /// <code>
    /// var server = new UpstreamServerClient(Endpoint.Default);
    /// 
    /// // Query categories
    /// var categories = await server.GetCategories();
    /// 
    /// // Create a filter for quering drivers
    /// var filter = new QueryFilter(
    ///     categories.Updates.OfType&lt;Product&gt;(),
    ///     categories.Updates.OfType&lt;Classification&gt;().Where(c => c.Title.Equals("Driver")));
    ///     
    /// // Get drivers
    /// var driversQueryResult = await server.GetUpdates(filter);
    /// var drivers = driversQueryResult.Updates.OfType&lt;DriverUpdate&gt;();
    /// </code>
    /// </example>
    public class DriverUpdate :
        Update,
        IUpdateWithPrerequisites,
        IUpdateWithFiles,
        IUpdateWithProduct,
        IUpdateWithProductInternal,
        IUpdateWithClassification,
        IUpdateWithClassificationInternal,
        IUpdateWithSupersededUpdates
    {
        /// <summary>
        /// Gets the list of product IDs for the driver update
        /// </summary>
        /// <value>List of product IDs. The GUIDs map to a <see cref="Product"/></value>
        [JsonProperty]
        public List<Guid> ProductIds { get; private set; }

        /// <summary>
        /// Gets the list of classifications for the driver update
        /// </summary>
        /// <value>List of classification IDs. The GUIDs map to a <see cref="Classification"/></value>
        [JsonProperty]
        public List<Guid> ClassificationIds { get; private set; }

        /// <summary>
        /// Gets the list of driver update extended metadata.
        /// </summary>
        /// <value>
        /// List of driver metadata (hardware ID, version, etc.)
        /// </value>
        [JsonIgnore]
        public List<DriverMetadata> Metadata { get; private set; }

        /// <summary>
        /// Gets the list of files (content) for the driver update
        /// </summary>
        /// <value>
        /// List of content files
        /// </value>
        [JsonIgnore]
        public List<UpdateFile> Files { get; private set; }

        /// <summary>
        /// Get the list of prerequisites for the driver update.
        /// </summary>
        /// <value>
        /// List of prerequisites
        /// </value>
        [JsonIgnore]
        public List<Prerequisites.Prerequisite> Prerequisites { get; private set; }

        /// <summary>
        /// Get the list of updates that this driver update superseds
        /// </summary>
        /// <value>
        /// List of update IDs that this driver update replaced.
        /// </value>
        [JsonIgnore]
        public List<Identity> SupersededUpdates { get; private set; }

        [JsonConstructor]
        private DriverUpdate()
        {

        }

        /// <summary>
        /// Create a DriverUpdate from an update XML and raw update data
        /// </summary>
        /// <param name="serverSyncUpdateData"></param>
        /// <param name="xdoc">Update XML document</param>
        internal DriverUpdate(ServerSyncUpdateData serverSyncUpdateData, XDocument xdoc) : base(serverSyncUpdateData)
        {
            GetTitleAndDescriptionFromXml(xdoc);
            UpdateType = UpdateType.Driver;

            Prerequisites = Prerequisite.FromXml(xdoc);

            // Parse superseded updates
            SupersededUpdates = SupersededUpdatesParser.Parse(xdoc);
        }

        /// <summary>
        /// Sets extended attributes from the XML metadata.
        /// </summary>
        /// <param name="xmlReader">XML stream</param>
        /// <param name="contentFiles">All known content files. Used to resolve the hash from XML metadata to an actual file</param>
        internal override void LoadExtendedAttributesFromXml(StreamReader xmlReader, Dictionary<string, UpdateFileUrl> contentFiles)
        {
            if (!ExtendedAttributesLoaded)
            {
                var xdoc = XDocument.Parse(xmlReader.ReadToEnd(), LoadOptions.None);
                Metadata = new List<DriverMetadata>();
                ParseDriverMetadata(xdoc);

                Files = UpdateFileParser.Parse(xdoc, contentFiles);

                Prerequisites = Prerequisite.FromXml(xdoc);

                SupersededUpdates = SupersededUpdatesParser.Parse(xdoc);

                ExtendedAttributesLoaded = true;
            }
        }

        /// <summary>
        ///  Parse the inner metadata from XML
        /// </summary>
        /// <param name="xdoc">Update XML document</param>
        private void ParseDriverMetadata(XDocument xdoc)
        {
            // Get the driver metadata nodes
            var metadataElements = xdoc.Descendants(XName.Get("WindowsDriverMetaData", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsDriver"));
            foreach (var metadataElement in metadataElements)
            {
                Metadata.Add(new DriverMetadata(metadataElement));
            }
        }

        /// <summary>
        /// Resolves the parent product of this driver.
        /// This is done by finding the "AtleastOne" prerequisite with IsCategory attribute that matches a product ID
        /// </summary>
        /// <param name="allProducts">All known products</param>
        void IUpdateWithProductInternal.ResolveProduct(List<Product> allProducts)
        {
            if (ProductIds == null && Prerequisites != null)
            {
                ProductIds = CategoryResolver.ResolveProductFromPrerequisites(Prerequisites, allProducts);
                if (ProductIds == null)
                {
                    throw new Exception($"Cannot resolve product for update {Identity}");
                }
            }
        }

        /// <summary>
        /// Resolves the classification of this driver.
        /// This is done by finding the "AtleastOne" prerequisite with IsCategory attribute that matches a classification ID
        /// </summary>
        /// <param name="allClassifications">All known classifications</param>
        void IUpdateWithClassificationInternal.ResolveClassification(List<Classification> allClassifications)
        {
            if (ClassificationIds == null && Prerequisites != null)
            {
                ClassificationIds = CategoryResolver.ResolveClassificationFromPrerequisites(Prerequisites, allClassifications);

                if (ClassificationIds == null)
                {
                    throw new Exception($"Cannot resolve classification for update {Identity}");
                }
            }
        }
    }
}
