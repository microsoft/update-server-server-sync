// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Handlers
{
    /// <summary>
    /// Stores metadata specific to categories (products and classifications) in the Microsoft Update catalog
    /// </summary>
    public class CategoryHandler : HandlerMetadata
    {
        /// <summary>
        /// Category type
        /// </summary>
        [JsonProperty]
        public string CategoryType { get; private set; }

        /// <summary>
        /// Sub-categories are prohibited for this category
        /// </summary>
        [JsonProperty]
        public bool? ProhibitsSubcategories { get; private set; }

        /// <summary>
        /// Does not have carry updates
        /// </summary>
        [JsonProperty]
        public bool? ProhibitsUpdates { get; private set; }

        /// <summary>
        /// Display order
        /// </summary>
        [JsonProperty]
        public int? DisplayOrder { get; private set; }

        /// <summary>
        /// Excluded by default
        /// </summary>
        [JsonProperty]
        public bool? ExcludedByDefault { get; private set; }

        [JsonConstructor]
        private CategoryHandler()
        {

        }

        internal static new CategoryHandler FromXml(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            var categoryHandler = new CategoryHandler()
            {
                HandlerType = UpdateHandlerType.Category
            };

            categoryHandler.ExtractAttributesFromXml(
                new string[] { "CategoryType",  "ProhibitsSubcategories", "ProhibitsUpdates", "DisplayOrder", "ExcludedByDefault"},
                "cat:CategoryInformation/@*",
                metadataNavigator,
                namespaceManager);

            return categoryHandler;
        }
    }
}
