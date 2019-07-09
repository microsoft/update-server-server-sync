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
using System.Text;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Reprerents a software update.
    /// </summary>
    /// <example>
    /// <code>
    /// var server = new UpstreamServerClient(Endpoint.Default);
    /// 
    /// // Query categories
    /// var categories = await server.GetCategories();
    /// 
    /// // Create a filter for Windows 10 1803 updates
    /// var filter = new QueryFilter(
    ///     categories.Updates.OfType&lt;Product&gt;().Where(p => p.Title.Contains("Windows 10 version 1803 and Later")),
    ///     categories.Updates.OfType&lt;Classification&gt;());
    ///     
    /// // Get updates
    /// var updatesQueryResult = await server.GetUpdates(filter);
    /// var softwareUpdates = updatesQueryResult.Updates.OfType&lt;SoftwareUpdate&gt;();
    /// </code>
    /// </example>
    public class SoftwareUpdate :
        Update,
        IUpdateWithPrerequisites,
        IUpdateWithFiles,
        IUpdateWithSupersededUpdates,
        IUpdateWithBundledUpdates,
        IUpdateWithProduct,
        IUpdateWithProductInternal,
        IUpdateWithClassification,
        IUpdateWithClassificationInternal
    {
        /// <summary>
        /// Gets the list of product IDs for the software update
        /// </summary>
        /// <value>List of product IDs. The GUIDs map to a <see cref="Product"/></value>
        [JsonProperty]
        public List<Guid> ProductIds { get; private set; }

        /// <summary>
        /// Gets the list of classifications for the software update
        /// </summary>
        /// <value>List of classification IDs. The GUIDs map to a <see cref="Classification"/></value>
        [JsonProperty]
        public List<Guid> ClassificationIds { get; private set; }

        /// <summary>
        /// Gets the list of files (content) for the software update
        /// </summary>
        /// <value>
        /// List of content files
        /// </value>
        [JsonIgnore]
        public List<UpdateFile> Files { get; private set; }

        /// <summary>
        /// Get the list of prerequisites for the software update.
        /// </summary>
        /// <value>
        /// List of prerequisites
        /// </value>
        [JsonIgnore]
        public List<Prerequisites.Prerequisite> Prerequisites { get; private set; }

        /// <summary>
        /// Get the list of updates that this software update superseds
        /// </summary>
        /// <value>
        /// List of update IDs that this software update replaced.
        /// </value>
        [JsonIgnore]
        public List<Identity> SupersededUpdates { get; private set; }

        /// <summary>
        /// Get the list of other updates bundled with this software update. 
        /// </summary>
        /// <value>
        /// List of update IDs that this software update contains.
        /// </value>
        [JsonIgnore]
        public List<Identity> BundledUpdates { get; private set; }

        /// <summary>
        /// Gets the support url
        /// </summary>
        /// <value>
        /// Support URL string
        /// </value>
        [JsonIgnore]
        public string SupportUrl { get; private set; }

        /// <summary>
        /// Knowledge base (KB) article ID
        /// </summary>
        /// <value>
        /// KB article ID string
        /// </value>
        [JsonIgnore]
        public string KBArticleId { get; private set; }

        /// <summary>
        /// Gets the OsUpgrade type ("swap" etc.)
        /// <para>
        /// The property is set only for operating system upgrades.
        /// </para>
        /// </summary>
        /// <value>
        /// OS upgrade type string
        /// </value>
        [JsonIgnore]
        public string OsUpgrade { get; private set; }

        /// <summary>
        /// Private constructor used by the deserializer
        /// </summary>
        [JsonConstructor]
        private SoftwareUpdate()
        {

        }

        /// <summary>
        /// Create a SoftwareUpdate by parsing it's properties from the specified XML and raw update data
        /// </summary>
        /// <param name="serverSyncUpdateData">The raw update metadata received from the upstream server</param>
        /// <param name="xdoc">XML document with update metadata</param>
        internal SoftwareUpdate(ServerSyncUpdateData serverSyncUpdateData, XDocument xdoc) : base(serverSyncUpdateData)
        {
            GetTitleAndDescriptionFromXml(xdoc);
            UpdateType = UpdateType.Software;

            // Parse prerequisites
            Prerequisites = Prerequisite.FromXml(xdoc);

            // Parse superseded updates
            SupersededUpdates = SupersededUpdatesParser.Parse(xdoc);
        }

        /// <summary>
        /// Sets extended attributes from the XML metadata.
        /// </summary>
        /// <param name="xmlReader">XML stream containing metadata</param>
        /// <param name="contentFiles">All known content files. Used to resolve the hash from XML metadata to an actual file</param>
        internal override void LoadExtendedAttributesFromXml(StreamReader xmlReader, Dictionary<string, UpdateFileUrl> contentFiles)
        {
            if (!ExtendedAttributesLoaded)
            {
                var xdoc = XDocument.Parse(xmlReader.ReadToEnd(), LoadOptions.None);

                // Parse files
                Files = UpdateFileParser.Parse(xdoc, contentFiles);

                // Parse prerequisites
                Prerequisites = Prerequisite.FromXml(xdoc);

                // Parse superseded updates
                SupersededUpdates = SupersededUpdatesParser.Parse(xdoc);

                // Parse bundled updates
                BundledUpdates = BundlesUpdatesParser.Parse(xdoc);

                // Parse software update specific properties
                GetPropertiesFromXml(xdoc);

                ExtendedAttributesLoaded = true;
            }
        }

        /// <summary>
        /// Decode SoftwareUpdate specific properties
        /// </summary>
        /// <param name="xdoc"></param>
        private void GetPropertiesFromXml(XDocument xdoc)
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

            try
            {
                var osUpgradeAttribute = propertyNode.Attribute("OSUpgrade");
                OsUpgrade = osUpgradeAttribute.Value;
            }
            catch (Exception) { }

            var supportUrlNodes = propertyNode.Descendants(XName.Get("SupportUrl", "http://schemas.microsoft.com/msus/2002/12/Update"));
            if (supportUrlNodes.Count() > 0)
            {
                SupportUrl = supportUrlNodes.First().Value;
            }

            var KBArticleIDNodes = propertyNode.Descendants(XName.Get("KBArticleID", "http://schemas.microsoft.com/msus/2002/12/Update"));
            if (KBArticleIDNodes.Count() > 0)
            {
                KBArticleId = KBArticleIDNodes.First().Value;
            }
        }

        /// <summary>
        /// Resolves the parent product of this software update.
        /// This is done by finding the "AtleastOne" prerequisite with IsCategory attribute that matches a product ID
        /// </summary>
        /// <param name="allProducts">All known products</param>
        void IUpdateWithProductInternal.ResolveProduct(List<Product> allProducts)
        {
            if (ProductIds == null)
            {
                ProductIds = CategoryResolver.ResolveProductFromPrerequisites(Prerequisites, allProducts);
            }
        }

        /// <summary>
        /// Resolves the classification of this software update.
        /// This is done by finding the "AtleastOne" prerequisite with IsCategory attribute that matches a classification ID
        /// </summary>
        /// <param name="allClassifications">All known products</param>
        void IUpdateWithClassificationInternal.ResolveClassification(List<Classification> allClassifications)
        {
            if (ClassificationIds == null)
            {
                ClassificationIds = CategoryResolver.ResolveClassificationFromPrerequisites(Prerequisites, allClassifications);
            }
        }
    }
}
