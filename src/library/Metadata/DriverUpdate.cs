// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.Metadata.Prerequisites;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Metadata for a driver update
    /// </summary>
    public class DriverUpdate : MicrosoftUpdate, IUpdateWithPrerequisites, IUpdateWithFiles, IUpdateWithProduct, IUpdateWithClassification
    {
        /// <summary>
        /// Inner metadata extracted from the update XML
        /// </summary>
        public List<DriverMetadata> Metadata;

        /// <summary>
        /// Files in the driver update
        /// </summary>
        public List<UpdateFile> Files { get; set; }

        /// <summary>
        /// Prerequisites
        /// </summary>
        public List<Prerequisites.Prerequisite> Prerequisites { get; set; }

        /// <summary>
        /// The product this driver belongs to
        /// </summary>
        public List<Guid> ProductIds { get; set; }

        /// <summary>
        /// The classifications of this driver update
        /// </summary>
        public List<Guid> ClassificationIds { get; set; }

        [JsonConstructor]
        private DriverUpdate()
        {

        }

        /// <summary>
        /// Create a DriverUpdate from an update XML
        /// </summary>
        /// <param name="serverSyncUpdateData"></param>
        /// <param name="xdoc">Update XML document</param>
        /// <param name="urlData">URL data for the files referenced in the update (if any)</param>
        public DriverUpdate(ServerSyncUpdateData serverSyncUpdateData, XDocument xdoc, List<UpdateFileUrl> urlData) : base(serverSyncUpdateData)
        {
            var titleAndDescription = GetTitleAndDescriptionFromXml(xdoc);
            Title = titleAndDescription.Key;
            Description = titleAndDescription.Value;
            UpdateType = MicrosoftUpdateType.Driver;

            Metadata = new List<DriverMetadata>();
            ParseDriverMetadata(xdoc);

            Files = UpdateFileParser.Parse(xdoc, urlData);

            Prerequisites = PrerequisitesParser.Parse(xdoc);
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
        public void ResolveProduct(List<MicrosoftProduct> allProducts)
        {
            ProductIds = CategoryResolver.ResolveProductFromPrerequisites(Prerequisites, allProducts);
        }

        /// <summary>
        /// Resolves the classification of this driver.
        /// This is done by finding the "AtleastOne" prerequisite with IsCategory attribute that matches a classification ID
        /// </summary>
        /// <param name="allProducts">All known products</param>
        public void ResolveClassification(List<Classification> allClassifications)
        {
            ClassificationIds = CategoryResolver.ResolveClassificationFromPrerequisites(Prerequisites, allClassifications);
        }
    }

    /// <summary>
    /// Stores driver metadata extracted from the update XML blob
    /// </summary>
    public class DriverMetadata
    {
        public string HardwareID { get; set; }
        public string WhqlDriverID { get; set; }

        public string Manufacturer { get; set; }

        public string Company { get; set; }

        public string Provider { get; set; }

        public string DriverVerDate { get; set; }
        public string DriverVerVersion { get; set; }

        public string Class { get; set; }

        [JsonConstructor]
        private DriverMetadata() { }

        /// <summary>
        /// Constructor; parses the passed WindowsDriverMetaData XML element
        /// </summary>
        /// <param name="xmlMetadata">The WindowsDriverMetaData element to parse data from</param>
        public DriverMetadata(XElement xmlMetadata)
        {
            string[] propertyNames = new string[] {
                "HardwareID", "WhqlDriverID", "Manufacturer", "Company", "Provider", "DriverVerDate", "DriverVerVersion", "Class" };

            // Go through all property names, extract them from the XML node and set this objects properties
            foreach(var propertyName in propertyNames)
            {
                var propertyAttributes = xmlMetadata.Attributes(propertyName);
                if (propertyAttributes.Count() > 0)
                {
                    this.GetType().GetProperty(propertyName).SetValue(this, propertyAttributes.First().Value);
                }
            }
        }
    }
}
