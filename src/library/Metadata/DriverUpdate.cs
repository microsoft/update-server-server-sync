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
    public class DriverUpdate : Update
    {
        /// <summary>
        /// Gets the list of driver update extended metadata.
        /// </summary>
        /// <value>
        /// List of driver metadata (hardware ID, version, etc.)
        /// </value>
        [JsonIgnore]
        public List<DriverMetadata> Metadata { get; private set; }

        internal DriverUpdate(Identity id, IMetadataSource source) : base(id, source)
        {
        }

        /// <summary>
        /// Create a DriverUpdate from an update XML and raw update data
        /// </summary>
        /// <param name="id">Update ID</param>
        /// <param name="xdoc">Update XML document</param>
        internal DriverUpdate(Identity id, XDocument xdoc) : base(id, null)
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

                        Metadata = new List<DriverMetadata>();
                        ParseDriverMetadata(xdoc);
                    }

                    AttributesLoaded = true;
                }
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
    }
}
