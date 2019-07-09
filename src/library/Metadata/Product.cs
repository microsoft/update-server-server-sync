// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Xml.Linq;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Microsoft.UpdateServices.Metadata.Prerequisites;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Interface implemeted by updates that belong to a <see cref="Product"/>
    /// </summary>
    public interface IUpdateWithProduct
    {
        /// <summary>
        /// Gets the list of parent Products Ids of this update.
        /// </summary>
        /// <value>
        /// List of GUIDs
        /// </value>
        List<Guid> ProductIds { get; }
    }

    /// <summary>
    /// Internal interface that exposes the ResolveProduct operation on an update that has a parent Product
    /// </summary>
    interface IUpdateWithProductInternal
    {
        void ResolveProduct(List<Product> allProducts);
    }

    /// <summary>
    /// Metadata for a product category.
    /// </summary>
    /// <example>
    /// <code>
    /// var server = new UpstreamServerClient(Endpoint.Default);
    /// 
    /// // Query categories
    /// var categoriesQueryResult = await server.GetCategories();
    /// 
    /// // Get Products
    /// var products = categoriesQueryResult.Updates.OfType&lt;Product&gt;();
    /// </code>
    /// </example>
    public class Product : Update, IUpdateWithProduct, IUpdateWithProductInternal
    {
        /// <summary>
        /// Gets the list of parent Products of this Product.
        /// <para>For example, Microsoft is the parent product of the "Windows" product, which is the parent product of the "Windows 8.1" product.</para>
        /// </summary>
        /// <value>
        /// List of GUIDs
        /// </value>
        public List<Guid> ProductIds { get; set; }

        /// <summary>
        /// Temporary list of prerequisites; Used to resolve the parent product, after which the list is released.
        /// </summary>
        [JsonIgnore]
        private List<Prerequisite> TemporaryPrerequisites;

        [JsonConstructor]
        private Product()
        {

        }

        internal Product(ServerSyncUpdateData serverSyncUpdateData, XDocument xdoc) : base(serverSyncUpdateData)
        {
            GetTitleAndDescriptionFromXml(xdoc);
            UpdateType = UpdateType.Product;

            // The parent product ID is embedded amongst the prerequisites
            // The parent product is a prerequisite of type AtLeastOne having the IsCategory attribute set to true
            // All the metadata is required to identify a product; for now stash all prerequisites and later
            // we'll have another pass and identify the product
            TemporaryPrerequisites = Prerequisite.FromXml(xdoc);
        }

        /// <summary>
        /// Resolves the parent product of this update.
        /// This is done by finding the "AtleastOne" prerequisite with IsCategory attribute that matches a product ID
        /// </summary>
        /// <param name="allProducts">All known products</param>
        void IUpdateWithProductInternal.ResolveProduct(List<Product> allProducts)
        {
            ProductIds = CategoryResolver.ResolveProductFromPrerequisites(TemporaryPrerequisites, allProducts);
        }
    }
}
