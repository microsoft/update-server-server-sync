// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.Metadata.Prerequisites;
using Microsoft.UpdateServices.Storage;
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
    /// 
    /// // Query categories
    /// var categoriesSource = await server.GetCategories();
    /// 
    /// // Create a filter for Windows 10 1803 updates
    /// var filter = new QueryFilter(
    ///     categoriesSource.ProductsIndex.Values.Where(p => p.Title.Contains("Windows 10 version 1803 and Later")),
    ///     categoriesSource.ClassificationsIndex.Values);
    /// 
    /// // Get updates
    /// var metadataSource = await server.GetUpdates(filter);
    /// var softwareUpdates = metadataSource.UpdatesIndex.Values.OfType&lt;SoftwareUpdate&gt;();
    /// 
    /// metadataSource.Delete();
    /// categoriesSource.Delete();
    /// </code>
    /// </example>
    public class SoftwareUpdate : Update
    {
        /// <summary>
        /// Gets the support url
        /// </summary>
        /// <value>
        /// Support URL string
        /// </value>
        [JsonIgnore]
        public string SupportUrl
        {
            get
            {
                LoadAttributesFromMetadataSource();
                return _SupportUrl;
            }
        }

        private string _SupportUrl;

        /// <summary>
        /// Knowledge base (KB) article ID
        /// </summary>
        /// <value>
        /// KB article ID string
        /// </value>
        [JsonIgnore]
        public string KBArticleId
        {
            get
            {
                LoadAttributesFromMetadataSource();
                return _KBArticleId;
            }
        }

        private string _KBArticleId;

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

        internal SoftwareUpdate(Identity id, IMetadataSource source) : base(id, source)
        {
        }

        /// <summary>
        /// Create a SoftwareUpdate by parsing it's properties from the specified XML and raw update data
        /// </summary>
        /// <param name="id">Update ID</param>
        /// <param name="xdoc">XML document with update metadata</param>
        internal SoftwareUpdate(Identity id, XDocument xdoc) : base(id, null)
        {
        }

        /// <summary>
        /// Sets extended attributes from the XML metadata.
        /// </summary>
        internal override void LoadAttributesFromMetadataSource()
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

                        GetPropertiesFromXml(xdoc);
                    }

                    AttributesLoaded = true;
                }
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
                _SupportUrl = supportUrlNodes.First().Value;
            }

            var KBArticleIDNodes = propertyNode.Descendants(XName.Get("KBArticleID", "http://schemas.microsoft.com/msus/2002/12/Update"));
            if (KBArticleIDNodes.Count() > 0)
            {
                _KBArticleId = KBArticleIDNodes.First().Value;
            }
        }
    }
}
