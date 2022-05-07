// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Prerequisites
{
    /// <summary>
    /// Resolves "IsCategory" prerequisites to a category.
    /// This is needed because the category and classification for an update is encoded as a prerequisite
    /// </summary>
    abstract class CategoryResolver
    {
        /// <summary>
        /// Resolve product from prerequisites and list of all known products
        /// </summary>
        /// <param name="prerequisites">Update prerequisites</param>
        /// <param name="allProducts">All known products</param>
        /// <returns>All products that were found in the prerequisites list</returns>
        public static List<Guid> ResolveProductFromPrerequisites(List<IPrerequisite> prerequisites, IReadOnlyList<MicrosoftUpdatePackageIdentity> allProducts)
        {
            var returnList = new List<Guid>();
            // Find all "AtLeastOne" prerequisites
            var categoryPrereqs = prerequisites.OfType<AtLeastOne>().Where(p => p.IsCategory).ToList();

            foreach (var category in categoryPrereqs)
            {
                foreach (var subCategory in category.Simple)
                {
                    var matchingProduct = allProducts.FirstOrDefault(p => p.ID == subCategory.UpdateId);
                    if (matchingProduct != null)
                    {
                        returnList.Add(matchingProduct.ID);
                    }
                }
            }

            return returnList;
        }

        /// <summary>
        /// Resolve classification from prerequisites and list of all known classifications
        /// </summary>
        /// <param name="prerequisites">Update prerequisites</param>
        /// <param name="allClassifications">All known classifications</param>
        /// <returns>On success, the GUID of the classification, empty guid otherwise</returns>
        public static List<Guid> ResolveClassificationFromPrerequisites(List<IPrerequisite> prerequisites, HashSet<MicrosoftUpdatePackageIdentity> allClassifications)
        {
            var returnList = new List<Guid>();
            // Find all "AtLeastOne" prerequisites
            var categoryPrereqs = prerequisites.OfType<AtLeastOne>().ToList();

            foreach (var category in categoryPrereqs)
            {
                foreach (var subCategory in category.Simple)
                {
                    var matchingProduct = allClassifications.FirstOrDefault(p => p.ID == subCategory.UpdateId);
                    if (matchingProduct != null)
                    {
                        returnList.Add(matchingProduct.ID);
                    }
                }

            }

            return returnList;
        }
    }
}
