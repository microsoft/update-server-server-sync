using Microsoft.UpdateServices.LocalCache;
using Microsoft.UpdateServices.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.UpdateServices.Tools.UpdateRepo
{
    /// <summary>
    /// Implements operations to sync a local updates repository with an upstream update server
    /// </summary>
    class RepoSync
    {
        /// <summary>
        /// Create a valid filter for retrieving updates
        /// </summary>
        /// <param name="options">The user's commandline options with intended filter</param>
        /// <param name="categories">List of known categories and classifications</param>
        /// <returns>A query filter that can be used to selectively retrieve updates from the upstream server</returns>
        public static Query.QueryFilter CreateValidFilterFromOptions(UpdatesSyncOptions options, CategoriesCache categories)
        {
            var productFilter = new List<MicrosoftProduct>();
            var classificationFilter = new List<Classification>();

            // If a classification is specified then categories is also required, regardless of user option. Add all categories in this case.
            bool allProductsRequired = options.ProductsFilter.Count() == 0  || options.ProductsFilter.Contains("all");

            // If category is specified then classification is also required, regardless of user option. Add all classifications in this case.
            bool allClassificationsRequired = options.ClassificationsFilter.Count() == 0 || options.ClassificationsFilter.Contains("all");

            if (allProductsRequired && allClassificationsRequired)
            {
                throw new Exception("At least one classification or product filter must be set.");
            }

            if (allProductsRequired)
            {
                productFilter.AddRange(categories.Products);
            }
            else
            {
                foreach (var categoryGuidString in options.ProductsFilter)
                {
                    var categoryGuid = new Guid(categoryGuidString);
                    var matchingProduct = categories.Products.Where(category => category.Identity.Raw.UpdateID == categoryGuid);

                    if (matchingProduct.Count() != 1)
                    {
                        throw new Exception($"Could not find a match for product filter {categoryGuidString}");
                    }

                    productFilter.Add(matchingProduct.First());
                }
            }

            if (allClassificationsRequired)
            {
                classificationFilter.AddRange(categories.Classifications);
            }
            else
            {
                foreach (var classificationGuidString in options.ClassificationsFilter)
                {
                    var classificationGuid = new Guid(classificationGuidString);
                    var matchingClassification = categories.Classifications.Where(classification => classification.Identity.Raw.UpdateID == classificationGuid);

                    if (matchingClassification.Count() != 1)
                    {
                        throw new Exception($"Could not find a match for classification filter {classificationGuidString}");
                    }

                    classificationFilter.Add(matchingClassification.First());
                }
            }

            return new Query.QueryFilter(productFilter, classificationFilter);
        }
    }
}
