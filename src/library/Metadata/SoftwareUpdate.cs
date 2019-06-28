// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.Metadata.Prerequisites;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Stores metadata for a software update (non-driver)
    /// </summary>
    public class SoftwareUpdate :
        MicrosoftUpdate,
        IUpdateWithPrerequisites,
        IUpdateWithFiles,
        IUpdateWithSupersededUpdates,
        IUpdateWithBundledUpdates,
        IUpdateWithProduct,
        IUpdateWithClassification
    {
        /// <summary>
        /// Files contained in the update
        /// </summary>
        public List<UpdateFile> Files { get; set; }

        /// <summary>
        /// SoftwareUpdate prerequisites
        /// </summary>
        public List<Prerequisite> Prerequisites { get; set; }

        /// <summary>
        /// Updates that this SoftwareUpdate superseeds
        /// </summary>
        public List<MicrosoftUpdateIdentity> SupersededUpdates { get; set; }

        /// <summary>
        /// Other bundled updates in this SoftwareUpdate
        /// </summary>
        public List<MicrosoftUpdateIdentity> BundledUpdates { get; set; }

        /// <summary>
        /// Support url
        /// </summary>
        public string SupportUrl { get; set; }

        /// <summary>
        /// KB article ID
        /// </summary>
        public string KBArticleId { get; set; }

        /// <summary>
        /// If the SoftareUpdate is OsUpgrade, contains the type (e.g. "swap")
        /// </summary>
        public string OsUpgrade { get; set; }

        /// <summary>
        /// The product this update belongs to
        /// </summary>
        public List<Guid> ProductIds { get; set; }

        /// <summary>
        /// The classification of this software update
        /// </summary>
        public List<Guid> ClassificationIds { get; set; }

        /// <summary>
        /// Private constructor used by the deserializer
        /// </summary>
        [JsonConstructor]
        private SoftwareUpdate()
        {

        }

        /// <summary>
        /// Create a SoftwareUpdate by parsing it's properties from the specified XML
        /// </summary>
        /// <param name="serverSyncUpdateData"></param>
        /// <param name="xdoc"></param>
        /// <param name="urlData">URL data for the files referenced in the update (if any)</param>
        public SoftwareUpdate(ServerSyncUpdateData serverSyncUpdateData, XDocument xdoc, List<UpdateFileUrl> urlData) : base(serverSyncUpdateData)
        {
            var titleAndDescription = GetTitleAndDescriptionFromXml(xdoc);
            Title = titleAndDescription.Key;
            Description = titleAndDescription.Value;
            UpdateType = MicrosoftUpdateType.Software;

            // Parse filese
            Files = UpdateFileParser.Parse(xdoc, urlData);

            // Parse prerequisites
            Prerequisites = PrerequisitesParser.Parse(xdoc);

            // Parse superseded updates
            SupersededUpdates = SupersededUpdatesParser.Parse(xdoc);

            // Parse bundled updates
            BundledUpdates = BundlesUpdatesParser.Parse(xdoc);

            // Parse software update specific properties
            GetPropertiesFromXml(xdoc);
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
        public void ResolveProduct(List<MicrosoftProduct> allProducts)
        {
            ProductIds = CategoryResolver.ResolveProductFromPrerequisites(Prerequisites, allProducts);
        }

        /// <summary>
        /// Resolves the classification of this software update.
        /// This is done by finding the "AtleastOne" prerequisite with IsCategory attribute that matches a classification ID
        /// </summary>
        /// <param name="allProducts">All known products</param>
        public void ResolveClassification(List<Classification> allClassifications)
        {
            ClassificationIds = CategoryResolver.ResolveClassificationFromPrerequisites(Prerequisites, allClassifications);
        }
    }
}
