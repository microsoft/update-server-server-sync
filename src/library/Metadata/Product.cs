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
    /// Interface implemeted by updates that have a product category
    /// </summary>
    public interface IUpdateWithProduct
    {
        List<Guid> ProductIds { get; }

        void ResolveProduct(List<MicrosoftProduct> allProducts);
    }

    /// <summary>
    /// Metadata for a product category.
    /// </summary>
    public class MicrosoftProduct : MicrosoftUpdate, IUpdateWithProduct
    {
        /// <summary>
        /// The parent product of this product, if any
        /// </summary>
        public List<Guid> ProductIds { get; set; }

        [JsonIgnore]
        private List<Prerequisite> TemporaryPrerequisites;

        [JsonConstructor]
        private MicrosoftProduct()
        {

        }

        public MicrosoftProduct(ServerSyncUpdateData serverSyncUpdateData, XDocument xdoc) : base(serverSyncUpdateData)
        {
            var titleAndDescription = GetTitleAndDescriptionFromXml(xdoc);
            Title = titleAndDescription.Key;
            Description = titleAndDescription.Value;
            UpdateType = MicrosoftUpdateType.Product;

            // The parent product ID is embedded amongst the prerequisites
            // The parent product is a prerequisite of type AtLeastOne having the IsCategory attribute set to true
            // All the metadata is required to identify a product; for now stash all prerequisites and later
            // we'll have another pass and identify the product
            TemporaryPrerequisites = PrerequisitesParser.Parse(xdoc);
        }

        /// <summary>
        /// Resolves the parent product of this update.
        /// This is done by finding the "AtleastOne" prerequisite with IsCategory attribute that matches a product ID
        /// </summary>
        /// <param name="allProducts">All known products</param>
        public void ResolveProduct(List<MicrosoftProduct> allProducts)
        {
            ProductIds = CategoryResolver.ResolveProductFromPrerequisites(TemporaryPrerequisites, allProducts);
        }
    }
}
