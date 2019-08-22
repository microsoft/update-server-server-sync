// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Storage;
using Microsoft.UpdateServices.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UpdateServices.Client;
using System.IO;
using Newtonsoft.Json;

namespace Microsoft.UpdateServices.Tools.UpdateRepo
{
    /// <summary>
    /// Implements operations to fetch update metadata from an upstream update server
    /// </summary>
    class MetadataSync
    {
        public static void MergeQueryResult(MergeQueryResultOptions opts)
        {
            
        }

        /// <summary>
        /// Fetches categories from an upstream server
        /// </summary>
        /// <param name="options">Pre-Fetch command options</param>
        public static void FetchConfiguration(FetchConfigurationOptions options)
        {
            Endpoint upstreamEndpoint;
            if (!string.IsNullOrEmpty(options.UpstreamEndpoint))
            {
                upstreamEndpoint = new Endpoint(options.UpstreamEndpoint);
            }
            else
            {
                upstreamEndpoint = Endpoint.Default;
            }

            var server = new UpstreamServerClient(upstreamEndpoint);
            server.MetadataQueryProgress += Server_MetadataQueryProgress;
            var configData = server.GetServerConfigData().GetAwaiter().GetResult();

            File.WriteAllText(options.OutFile, JsonConvert.SerializeObject(configData));
        }


        /// <summary>
        /// Fetches categories from an upstream server
        /// </summary>
        /// <param name="options">Pre-Fetch command options</param>
        public static void FetchCategories(FetchCategoriesOptions options)
        {
            Endpoint upstreamEndpoint;
            if (!string.IsNullOrEmpty(options.UpstreamEndpoint))
            {
                upstreamEndpoint = new Endpoint(options.UpstreamEndpoint);
            }
            else
            {
                upstreamEndpoint = Endpoint.Default;
            }

            CompressedMetadataStore syncResult;

            string destinationFile;
            if (!string.IsNullOrEmpty(options.OutFile))
            {
                if (Path.GetExtension(options.OutFile).ToLower() != ".zip")
                {
                    ConsoleOutput.WriteRed("The out file must have .zip extension");
                    return;
                }

                destinationFile = options.OutFile;
            }
            else
            {
                destinationFile = $"QueryResult-{DateTime.Now.ToFileTime()}.zip";
            }

            Console.WriteLine($"Creating compressed metadata store {destinationFile}");
            syncResult = new CompressedMetadataStore(destinationFile, upstreamEndpoint);

            if (!string.IsNullOrEmpty(options.AccountName) &&
                !string.IsNullOrEmpty(options.AccountGuid))
            {
                if (!Guid.TryParse(options.AccountGuid, out Guid accountGuid))
                {
                    ConsoleOutput.WriteRed("The account GUID must be a valid GUID string");
                    return;
                }

                syncResult.SetUpstreamCredentials(options.AccountName, accountGuid);
            }

            FetchUpdates(null, syncResult);
        }
        /// <summary>
        /// Runs the sync command
        /// </summary>
        /// <param name="options">Fetch command options</param>
        public static void FetchUpdates(FetchUpdatesOptions options)
        {
            Endpoint upstreamEndpoint;
            CompressedMetadataStore baselineSource;

            Console.WriteLine("Opening query results file");
            baselineSource = CompressedMetadataStore.Open(options.MetadataSourcePath);
            if (baselineSource == null)
            {
                ConsoleOutput.WriteRed("Cannot open the query result file!");
                return;
            }

            ConsoleOutput.WriteGreen("Done!");
            upstreamEndpoint = baselineSource.UpstreamSource;

            // The filter to apply when fetching updates
            QueryFilter queryToRun = null;

            queryToRun = baselineSource.Filters.FirstOrDefault();
            if (queryToRun != null)
            {
                ConsoleOutput.WriteCyan("Using the filter from the baseline source. Command line filters are ignored!");
            }

            if (queryToRun == null)
            {
                // A new query will be created based on command line options
                try
                {
                    queryToRun = MetadataSync.CreateValidFilterFromOptions(options, baselineSource);
                }
                catch (Exception ex)
                {
                    ConsoleOutput.WriteRed(ex.Message);
                    return;
                }
            }

            using (var syncResult = new CompressedMetadataStore(baselineSource))
            {
                if (!string.IsNullOrEmpty(options.AccountName) && !string.IsNullOrEmpty(options.AccountGuid))
                {
                    if (!Guid.TryParse(options.AccountGuid, out Guid accountGuid))
                    {
                        ConsoleOutput.WriteRed("The account GUID must be a valid GUID string");
                        return;
                    }

                    syncResult.SetUpstreamCredentials(options.AccountName, accountGuid);
                }

                FetchUpdates(queryToRun, syncResult);
            }
        }

        private static void FetchUpdates(QueryFilter queryToRun, CompressedMetadataStore destinationResult)
        {
            var server = new UpstreamServerClient(destinationResult.UpstreamSource);
            server.MetadataQueryProgress += Server_MetadataQueryProgress;

            Console.WriteLine("Fetching categories ...");

            server.GetCategories(destinationResult).GetAwaiter().GetResult();

            if (queryToRun != null)
            {
                Console.WriteLine("Running query with filters:");

                MetadataQuery.PrintFilter(queryToRun, destinationResult);

                server.GetUpdates(queryToRun, destinationResult).GetAwaiter().GetResult();
            }

            Console.WriteLine();
            destinationResult.CommitProgress += Program.MetadataSourceOperationProgressHandler;
            destinationResult.Commit();

            Console.WriteLine();
            ConsoleOutput.WriteGreen($"Query result saved to file {destinationResult.FilePath}");
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
                    Console.Write("Retrieving updates metadata [{0}]: 0%", e.Maximum);
                    break;

                case MetadataQueryStage.GetUpdateMetadataEnd:
                    Console.CursorLeft = 0;
                    Console.Write("Retrieving updates metadata [{0}]: 100.00%", e.Maximum);
                    ConsoleOutput.WriteGreen(" Done!");
                    break;

                case MetadataQueryStage.GetUpdateMetadataProgress:
                    Console.CursorLeft = 0;
                    Console.Write("Retrieving updates metadata [{0}]: {1:000.00}%", e.Maximum, e.PercentDone);
                    break;
            }
        }

        /// <summary>
        /// Create a valid filter for retrieving updates
        /// </summary>
        /// <param name="options">The user's commandline options with intended filter</param>
        /// <param name="metadataSource">Metadata source that contains the list of known categories and classifications</param>
        /// <returns>A query filter that can be used to selectively retrieve updates from the upstream server</returns>
        private static QueryFilter CreateValidFilterFromOptions(FetchUpdatesOptions options, IMetadataSource metadataSource)
        {
            var productFilter = new List<Product>();
            var classificationFilter = new List<Classification>();

            // If a classification is specified then categories is also required, regardless of user option. Add all categories in this case.
            bool allProductsRequired = options.ProductsFilter.Count() == 0  || options.ProductsFilter.Contains("all");

            // If category is specified then classification is also required, regardless of user option. Add all classifications in this case.
            bool allClassificationsRequired = options.ClassificationsFilter.Count() == 0 || options.ClassificationsFilter.Contains("all");

            if (allProductsRequired)
            {
                productFilter.AddRange(metadataSource.ProductsIndex.Values);
            }
            else
            {
                foreach (var categoryGuidString in options.ProductsFilter)
                {
                    var categoryGuid = new Guid(categoryGuidString);
                    var matchingProduct = metadataSource.ProductsIndex.Values.Where(category => category.Identity.ID == categoryGuid);

                    if (matchingProduct.Count() == 0)
                    {
                        throw new Exception($"Could not find a match for product filter {categoryGuidString}");
                    }

                    productFilter.Add(matchingProduct.First());
                }
            }

            if (allClassificationsRequired)
            {
                classificationFilter.AddRange(metadataSource.ClassificationsIndex.Values);
            }
            else
            {
                foreach (var classificationGuidString in options.ClassificationsFilter)
                {
                    var classificationGuid = new Guid(classificationGuidString);
                    var matchingClassification = metadataSource.ClassificationsIndex.Values.Where(classification => classification.Identity.ID == classificationGuid);

                    if (matchingClassification.Count() == 0)
                    {
                        throw new Exception($"Could not find a match for classification filter {classificationGuidString}");
                    }

                    classificationFilter.Add(matchingClassification.First());
                }
            }

            return new QueryFilter(productFilter, classificationFilter);
        }
    }
}
