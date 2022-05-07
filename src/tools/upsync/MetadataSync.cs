// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using Microsoft.PackageGraph.MicrosoftUpdate.Source;
using Microsoft.PackageGraph.Storage;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;

namespace Microsoft.PackageGraph.Utilitites.Upsync
{
    /// <summary>
    /// Implements operations to fetch update metadata from an upstream update server
    /// </summary>
    class MetadataSync
    {
        public static void FetchConfiguration(FetchConfigurationOptions options)
        {
            MicrosoftUpdate.Source.Endpoint upstreamEndpoint;
            if (!string.IsNullOrEmpty(options.UpstreamEndpoint))
            {
                upstreamEndpoint = new MicrosoftUpdate.Source.Endpoint(options.UpstreamEndpoint);
            }
            else
            {
                upstreamEndpoint = MicrosoftUpdate.Source.Endpoint.Default;
            }

            var server = new UpstreamServerClient(upstreamEndpoint);
            server.MetadataQueryProgress += Server_MetadataQueryProgress;
            var configData = server.GetServerConfigData().GetAwaiter().GetResult();

            File.WriteAllText(options.OutFile, JsonConvert.SerializeObject(configData));
        }

        public static void ReIndex(ReindexStoreOptions options)
        {
            var sourceToUpdate = MetadataStoreCreator.OpenFromOptions(options as IMetadataStoreOptions);
            if (sourceToUpdate == null)
            {
                return;
            }

            using(sourceToUpdate)
            {
                if (sourceToUpdate.IsMetadataIndexingSupported)
                {
                    sourceToUpdate.PackageIndexingProgress += Program.OnPackageIndexingProgress;
                    if (sourceToUpdate.IsReindexingRequired || options.ForceReindex)
                    {
                        Console.WriteLine("ReIndexing ...");
                        sourceToUpdate.ReIndex();
                        ConsoleOutput.WriteGreen("Done!");
                    }
                    else
                    {
                        ConsoleOutput.WriteGreen("Indexing not required!");
                    }
                }
                else
                {
                    ConsoleOutput.WriteRed("Package store does not support indexing!");
                }
            }
        }

        public static void FetchCategories(FetchCategoriesOptions options)
        {
            MicrosoftUpdate.Source.Endpoint upstreamEndpoint;
            if (!string.IsNullOrEmpty(options.UpstreamEndpoint))
            {
                upstreamEndpoint = new MicrosoftUpdate.Source.Endpoint(options.UpstreamEndpoint);
            }
            else
            {
                upstreamEndpoint = MicrosoftUpdate.Source.Endpoint.Default;
            }

            if (!string.IsNullOrEmpty(options.AccountName) &&
                !string.IsNullOrEmpty(options.AccountGuid))
            {
                throw new NotImplementedException();
            }

            var destinationStore = MetadataStoreCreator.CreateFromOptions(options as IMetadataStoreOptions);
            if (destinationStore == null)
            {
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"Getting list of categories. This might take up to 1 minute ...");
            using (destinationStore)
            {
                var microsoftUpdateCategoriesSource = new UpstreamCategoriesSource(upstreamEndpoint);
                microsoftUpdateCategoriesSource.MetadataCopyProgress += Program.OnPackageCopyProgress;
                var cancellationToken = new CancellationTokenSource();
                microsoftUpdateCategoriesSource.CopyTo(destinationStore, cancellationToken.Token);
            }

            Console.WriteLine();
            ConsoleOutput.WriteGreen("Done!");
        }

        public static void FetchPackagesUpdates(FetchPackagesOptions options)
        {
            var store = MetadataStoreCreator.CreateFromOptions(options as IMetadataStoreOptions);
            if (store == null)
            {
                return;
            }

            switch (options.EndpointType)
            {
                case FetchPackagesOptions.MicrosoftUpdateEndpoint:
                    FetchMicrosoftUpdatePackages(options, store);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private static void FetchMicrosoftUpdatePackages(FetchPackagesOptions options, IMetadataStore store)
        {
            var upstreamEndpoint = string.IsNullOrEmpty(options.UpstreamEndpoint) ? MicrosoftUpdate.Source.Endpoint.Default : new MicrosoftUpdate.Source.Endpoint(options.UpstreamEndpoint);

            if (!string.IsNullOrEmpty(options.AccountName) &&
                !string.IsNullOrEmpty(options.AccountGuid))
            {
                throw new NotImplementedException();
            }

            using (store)
            {
                var microsoftUpdateCategoriesSource = new UpstreamCategoriesSource(upstreamEndpoint);

                Console.WriteLine($"Getting list of categories. This might take up to 1 minute ...");

                microsoftUpdateCategoriesSource.MetadataCopyProgress += Program.OnPackageCopyProgress;
                var cancellationToken = new CancellationTokenSource();
                microsoftUpdateCategoriesSource.CopyTo(store, cancellationToken.Token);

                if (options.Ids.Any())
                {
                    var server = new UpstreamServerClient(upstreamEndpoint);

                    foreach (var updateId in options.Ids)
                    {
                        if (Guid.TryParse(updateId, out var updateIdGuid))
                        {
                            Console.WriteLine();
                            Console.Write($"Searching for package {updateId}");
                            var foundPackage = server.TryGetExpiredUpdate(updateIdGuid, 300, 100).GetAwaiter().GetResult();
                            if (foundPackage == null)
                            {
                                ConsoleOutput.WriteRed($" Not found!");
                            }
                            else
                            {
                                ConsoleOutput.WriteGreen($" Found!");
                                store.AddPackage(foundPackage);
                            }
                        }
                        else
                        {
                            ConsoleOutput.WriteRed($"Update id must be in GUID format: {updateId}");
                            return;
                        }
                    }
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("Creating the query ...");
                    UpstreamSourceFilter sourceFilter;
                    try
                    {
                        sourceFilter = MetadataSync.CreateValidFilterFromOptions(options, store);
                    }
                    catch (Exception ex)
                    {
                        ConsoleOutput.WriteRed(ex.Message);
                        return;
                    }

                    MetadataQuery.PrintFilter(sourceFilter, store);

                    Console.WriteLine($"Getting list of updates. This might take up to 1 minute ...");
                    var microsoftUpdateSource = new UpstreamUpdatesSource(upstreamEndpoint, sourceFilter);
                    microsoftUpdateSource.MetadataCopyProgress += Program.OnPackageCopyProgress;
                    microsoftUpdateSource.CopyTo(store, cancellationToken.Token);

                    Console.WriteLine();
                    Console.WriteLine("Done!");
                }
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

        private static List<Guid> CreateFilterListForCategory<T>(IEnumerable<string> userFilterList, IMetadataStore metadataSource)
        {
            List<Guid> filterList;
            if (userFilterList.Any())
            {
                filterList = new List<Guid>();
                foreach (var guidString in userFilterList)
                {
                    if (Guid.TryParse(guidString, out Guid guid))
                    {
                        filterList.Add(guid);
                    }
                }
            }
            else
            {
                filterList = metadataSource.OfType<T>()
                    .Select(update => (update as MicrosoftUpdatePackage).Id.ID)
                    .ToList();

                if (filterList.Count == 0)
                {
                    throw new Exception("No products information available to create a filter");
                }
            }

            return filterList;
        }

        private static UpstreamSourceFilter CreateValidFilterFromOptions(FetchPackagesOptions options, IMetadataStore metadataSource)
        {
            List<Guid> productFilter = CreateFilterListForCategory<ProductCategory>(
                options.ProductsFilter, 
                metadataSource);

            List<Guid> classificationFilter = CreateFilterListForCategory<ClassificationCategory>(
                options.ClassificationsFilter,
                metadataSource);

            return new UpstreamSourceFilter(productFilter, classificationFilter);
        }
    }
}
