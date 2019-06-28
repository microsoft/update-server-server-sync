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
    class MetadataSync
    {
        /// <summary>
        /// Initialize a new local updates repository by sync'ing the server configuration
        /// </summary>
        /// <param name="options">Initialization options (path)</param>
        public static void SyncConfiguration(InitRepositoryOptions options)
        {
            var localRepo = Program.LoadRepositoryFromOptions(options as IRepositoryPathOption, Repository.RepositoryOpenMode.CreateIfDoesNotExist);

            var server = new UpstreamServerClient(Endpoint.Default);
            server.MetadataQueryProgress += Server_MetadataQueryProgress;
            server.RefreshAccessToken().GetAwaiter().GetResult();
            server.RefreshServerConfigData().GetAwaiter().GetResult();

            localRepo.CacheAccessToken(server.AccessToken);
            localRepo.CacheServiceConfiguration(server.ConfigData);

            ConsoleOutput.WriteGreen("Done!");
        }

        /// <summary>
        /// Syncs categories in a local updates repository
        /// </summary>
        /// <param name="options">Sync options</param>
        public static void SyncCategories(CategoriesSyncOptions options)
        {
            var localRepo = Program.LoadRepositoryFromOptions(options as IRepositoryPathOption, Repository.RepositoryOpenMode.CreateIfDoesNotExist);

            var cachedToken = localRepo.GetAccessToken();
            if (cachedToken != null)
            {
                Console.WriteLine("Loaded cached access token.");
            }

            var serviceConfig = localRepo.GetServiceConfiguration();
            if (serviceConfig != null)
            {
                Console.WriteLine("Loaded cached service configuration.");
            }

            var server = new UpstreamServerClient(Endpoint.Default, serviceConfig, cachedToken);
            server.MetadataQueryProgress += Server_MetadataQueryProgress;
            using (var newCategories = server.GetCategories(localRepo.Categories).GetAwaiter().GetResult())
            {
                Console.Write("Merging the query result and commiting the changes...");
                localRepo.MergeQueryResult(newCategories);
                ConsoleOutput.WriteGreen("Done!");
            }

            localRepo.CacheAccessToken(server.AccessToken);
            localRepo.CacheServiceConfiguration(server.ConfigData);
        }

        /// <summary>
        /// Runs the sync command
        /// </summary>
        /// <param name="options">Sync command options</param>
        public static void SyncUpdates(UpdatesSyncOptions options)
        {
            var localRepo = Program.LoadRepositoryFromOptions(options as IRepositoryPathOption, Repository.RepositoryOpenMode.OpenExisting);

            if (localRepo == null)
            {
                ConsoleOutput.WriteRed("Initialize the repo and sync categories first!");
                return;
            }

            if (localRepo.Categories.Categories.Count == 0)
            {
                ConsoleOutput.WriteRed("Categories must be sync'ed before updates. Please run \"sync-categories\" first");
                return;
            }

            Query.QueryFilter filter;
            try
            {
                filter = MetadataSync.CreateValidFilterFromOptions(options, localRepo.Categories);
            }
            catch (Exception ex)
            {
                ConsoleOutput.WriteRed(ex.Message);
                return;
            }

            var cachedToken = localRepo.GetAccessToken();
            if (cachedToken != null)
            {
                Console.WriteLine("Loaded cached access token.");
            }

            var serviceConfig = localRepo.GetServiceConfiguration();
            if (serviceConfig != null)
            {
                Console.WriteLine("Loaded cached service configuration.");
            }

            var server = new UpstreamServerClient(Endpoint.Default, serviceConfig, cachedToken);
            server.MetadataQueryProgress += Server_MetadataQueryProgress;

            using (var newUpdates = server.GetUpdates(filter, localRepo.Updates).GetAwaiter().GetResult())
            {
                Console.Write("Merging the query result and commiting the changes...");
                localRepo.MergeQueryResult(newUpdates);
                ConsoleOutput.WriteGreen("Done!");
            }

            localRepo.CacheAccessToken(server.AccessToken);
            localRepo.CacheServiceConfiguration(server.ConfigData);
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
                case QuerySubTaskTypes.AuthenticateStart:
                    Console.Write("Acquiring new access token...");
                    break;

                case QuerySubTaskTypes.GetServerConfigStart:
                    Console.Write("Retrieving service configuration data...");
                    break;

                case QuerySubTaskTypes.AuthenticateEnd:
                case QuerySubTaskTypes.GetServerConfigEnd:
                case QuerySubTaskTypes.GetRevisionIdsEnd:
                    ConsoleOutput.WriteGreen("Done!");
                    break;

                case QuerySubTaskTypes.GetRevisionIdsStart:
                    Console.Write("Retrieving revision IDs...");
                    break;

                case QuerySubTaskTypes.GetUpdateMetadataStart:
                    Console.Write("Retrieving {0} updates metadata: 0%", e.Maximum);
                    break;

                case QuerySubTaskTypes.GetUpdateMetadataEnd:
                    Console.CursorLeft = 0;
                    Console.WriteLine("Retrieving {0} updates metadata: 100% Done", e.Maximum);
                    break;

                case QuerySubTaskTypes.GetUpdateMetadataProgress:
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
        private static Query.QueryFilter CreateValidFilterFromOptions(UpdatesSyncOptions options, CategoriesCache categories)
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
