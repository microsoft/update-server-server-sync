// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Xml.Linq;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Microsoft.UpdateServices.Metadata.Prerequisites;
using Microsoft.UpdateServices.Metadata.Content;
using System.IO;
using Microsoft.UpdateServices.Storage;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Metadata for a product category.
    /// </summary>
    /// <example>
    /// <code>
    /// // Query categories
    /// var categoriesSource = await server.GetCategories();
    /// 
    /// // Get products
    /// var products = categoriesSource.ProductsIndex.Values;
    ///
    /// // Delete the query result from disk when done with it.
    /// categoriesSource.Delete();
    /// </code>
    /// </example>
    public class Product : Update
    {
        internal Product(Identity id, IMetadataSource source) : base(id, source)
        {
            
        }

        internal Product(Identity id, XDocument xdoc) : base(id, null)
        {

        }
    }
}
