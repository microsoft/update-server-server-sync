using Microsoft.UpdateServices.Storage;
using Microsoft.UpdateServices.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UpdateServices.Client;

namespace Microsoft.UpdateServices.Tools.UpdateRepo
{
    /// <summary>
    /// Implements operations to sync a local updates repository with an upstream update server
    /// </summary>
    class MetadataSync
    {
        /// <summary>
        /// Syncs categories in a local updates repository
        /// </summary>
        /// <param name="options">Sync options</param>
        public static void SyncCategories(CategoriesSyncOptions options)
        {
            var localRepo = Program.LoadRepositoryFromOptions(options as IRepositoryPathOption);
            if (localRepo == null)
            {
                return;
            }

            var server = new UpstreamServerClient(localRepo);
            server.MetadataQueryProgress += Server_MetadataQueryProgress;
            using (var newCategories = server.GetCategories().GetAwaiter().GetResult())
            {
                Console.Write("Merging the query result and commiting the changes...");
                localRepo.MergeQueryResult(newCategories);
                ConsoleOutput.WriteGreen("Done!");
            }
        }

        /// <summary>
        /// Runs the sync command
        /// </summary>
        /// <param name="options">Sync command options</param>
        public static void SyncUpdates(UpdatesSyncOptions options)
        {
            var localRepo = Program.LoadRepositoryFromOptions(options as IRepositoryPathOption);
            if (localRepo == null)
            {
                ConsoleOutput.WriteRed("Initialize the repo and sync categories first!");
                return;
            }

            if (localRepo.GetCategories().Count == 0)
            {
                ConsoleOutput.WriteRed("Categories must be sync'ed before updates. Please run \"sync-categories\" first");
                return;
            }

            Query.QueryFilter filter;
            try
            {
                filter = MetadataSync.CreateValidFilterFromOptions(options, localRepo);
            }
            catch (Exception ex)
            {
                ConsoleOutput.WriteRed(ex.Message);
                return;
            }

            var server = new UpstreamServerClient(localRepo);
            server.MetadataQueryProgress += Server_MetadataQueryProgress;

            using (var newUpdates = server.GetUpdates(filter).GetAwaiter().GetResult())
            {
                Console.Write("Merging the query result and commiting the changes...");
                localRepo.MergeQueryResult(newUpdates);
                ConsoleOutput.WriteGreen("Done!");
            }
        }


        /// <summary>
        /// Handles progress notifications from a metadata query on an upstream server.
        /// Prints progress information to the console
        /// </summary>
        /// <param name="sender">The upstream server client that raised the event</param>
        /// <param name="e">Progress information</param>
        private static void Server_MetadataQueryProgress(object sender, MetadataQueryProgress e)
        {
            switch (e.CurrentTask)
            {
                case MetadataQueryStage.AuthenticateStart:
                    Console.Write("Acquiring new access token...");
                    break;

                case MetadataQueryStage.GetServerConfigStart:
                    Console.Write("Retrieving service configuration data...");
                    break;

                case MetadataQueryStage.AuthenticateEnd:
                case MetadataQueryStage.GetServerConfigEnd:
                case MetadataQueryStage.GetRevisionIdsEnd:
                    ConsoleOutput.WriteGreen("Done!");
                    break;

                case MetadataQueryStage.GetRevisionIdsStart:
                    Console.Write("Retrieving revision IDs...");
                    break;

                case MetadataQueryStage.GetUpdateMetadataStart:
                    Console.Write("Retrieving {0} updates metadata: 0%", e.Maximum);
                    break;

                case MetadataQueryStage.GetUpdateMetadataEnd:
                    Console.CursorLeft = 0;
                    Console.WriteLine("Retrieving {0} updates metadata: 100% Done", e.Maximum);
                    break;

                case MetadataQueryStage.GetUpdateMetadataProgress:
                    Console.CursorLeft = 0;
                    Console.Write("Retrieving {0} updates metadata: {1:000.00}%", e.Maximum, e.PercentDone);
                    break;
            }
        }

        /// <summary>
        /// Create a valid filter for retrieving updates
        /// </summary>
        /// <param name="options">The user's commandline options with intended filter</param>
        /// <param name="categories">List of known categories and classifications</param>
        /// <returns>A query filter that can be used to selectively retrieve updates from the upstream server</returns>
        private static Query.QueryFilter CreateValidFilterFromOptions(UpdatesSyncOptions options, IRepository categories)
        {
            var productFilter = new List<Product>();
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
                productFilter.AddRange(categories.ProductsIndex.Values);
            }
            else
            {
                foreach (var categoryGuidString in options.ProductsFilter)
                {
                    var categoryGuid = new Guid(categoryGuidString);
                    var matchingProduct = categories.ProductsIndex.Values.Where(category => category.Identity.ID == categoryGuid);

                    if (matchingProduct.Count() == 0)
                    {
                        throw new Exception($"Could not find a match for product filter {categoryGuidString}");
                    }

                    productFilter.Add(matchingProduct.First());
                }
            }

            if (allClassificationsRequired)
            {
                classificationFilter.AddRange(categories.ClassificationsIndex.Values);
            }
            else
            {
                foreach (var classificationGuidString in options.ClassificationsFilter)
                {
                    var classificationGuid = new Guid(classificationGuidString);
                    var matchingClassification = categories.ClassificationsIndex.Values.Where(classification => classification.Identity.ID == classificationGuid);

                    if (matchingClassification.Count() == 0)
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
