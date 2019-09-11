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
        /// Gets the driver metadata for a driver update
        /// </summary>
        /// <returns>Driver specific metadata</returns>
        public IEnumerable<DriverMetadata> GetDriverMetadata() => MetadataSource.GetDriverMetadata(this.Identity);

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
                    }

                    AttributesLoaded = true;
                }
            }
        }
    }
}
