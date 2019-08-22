// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.UpdateServices.Storage;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Represents a Classification. Used to clasify updates on an upstream server.
    /// <para>
    /// Example classifications: drivers, security updates, feature packs etc.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// // Query categories
    /// var categoriesSource = await server.GetCategories();
    /// 
    /// // Get classifications
    /// var classifications = categoriesSource.ClassificationsIndex.Values;
    ///
    /// // Delete the query result from disk when done with it.
    /// categoriesSource.Delete();
    /// </code>
    /// </example>
    public class Classification : Update
    {
        internal Classification(Identity id, IMetadataSource source) : base(id, source) { }

        internal Classification(Identity id, XDocument xdoc) : base(id, null) { }
    }
}
