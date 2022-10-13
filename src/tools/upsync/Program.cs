// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using CommandLine;
using Microsoft.PackageGraph.MicrosoftUpdate;
using Microsoft.PackageGraph.Storage;

namespace Microsoft.PackageGraph.Utilitites.Upsync
{
    class Program
    {
        private static readonly object ProgressLock = new();
        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<
                MetadataSourceStatusOptions,
                FetchPackagesOptions,
                QueryMetadataOptions,
                MetadataSourceExportOptions,
                ContentSyncOptions,
                RunUpstreamServerOptions,
                RunUpdateServerOptions,
                FetchCategoriesOptions,
                FetchConfigurationOptions,
                ReindexStoreOptions,
                MatchDriverOptions,
                MetadataCopyOptions,
                StoreAliasListOptions,
                StoreAliasDeleteOptions,
                StoreAliasCreateOptions>(args)
                .WithParsed<FetchPackagesOptions>(opts => MetadataSync.FetchPackagesUpdates(opts))
                .WithParsed<FetchConfigurationOptions>(opts => MetadataSync.FetchConfiguration(opts))
                .WithParsed<FetchCategoriesOptions>(opts => MetadataSync.FetchCategories(opts))
                .WithParsed<ReindexStoreOptions>(opts => MetadataSync.ReIndex(opts))
                .WithParsed<QueryMetadataOptions>(opts => MetadataQuery.Query(opts))
                .WithParsed<MatchDriverOptions>(opts => MetadataQuery.MatchDrivers(opts))
                .WithParsed<MetadataSourceExportOptions>(opts => UpdateMetadataExport.ExportUpdates(opts))
                .WithParsed<ContentSyncOptions>(opts => ContentSync.SyncContent(opts))
                .WithParsed<MetadataSourceStatusOptions>(opts => MetadataQuery.Status(opts))
                .WithParsed<RunUpstreamServerOptions>(opts => UpstreamServer.Run(opts))
                .WithParsed<RunUpdateServerOptions>(opts => UpdateServer.Run(opts))
                .WithParsed<MetadataCopyOptions>(opts => MetadataCopy.Run(opts))
                .WithParsed<StoreAliasListOptions>(opts => MetadataStoreCreator.ListAliases(opts))
                .WithParsed<StoreAliasDeleteOptions>(opts => MetadataStoreCreator.DeleteAlias(opts))
                .WithParsed<StoreAliasCreateOptions>(opts => MetadataStoreCreator.CreateAlias(opts))
                .WithNotParsed(failed => Console.WriteLine("Error"));
        }

        private static readonly object ConsoleWriteLock = new();

        private static void UpdateConsoleForMessageRefresh()
        {
            if (!Console.IsOutputRedirected)
            {
                Console.CursorLeft = 0;
            }
            else
            {
                Console.WriteLine();
            }
        }

        public static void OnPackageCopyProgress(object sender, PackageStoreEventArgs e)
        {
            lock (ConsoleWriteLock)
            {
                UpdateConsoleForMessageRefresh();

                if (e.Total == 0)
                {
                    Console.Write($"Copying {e.Total} package(s)");
                }
                else
                {
                    Console.Write($"Copying {e.Total} package(s). {e.Current} {Math.Truncate(((double)e.Current * 100) / e.Total)}%");
                }
            }
        }

        public static void OnOpenProgress(object sender, PackageStoreEventArgs e)
        {
            lock (ConsoleWriteLock)
            {
                UpdateConsoleForMessageRefresh();

                if (e.Total == 0)
                {
                    Console.Write(e.Current);
                }
                else
                {
                    Console.Write($"{e.Current}, {Math.Truncate(((double)e.Current * 100) / e.Total)}%");
                }
            }
        }

        public static void OnPackageIndexingProgress(object sender, PackageStoreEventArgs e)
        {
            lock(ProgressLock)
            {
                UpdateConsoleForMessageRefresh();


                if (e.Total == 0)
                {
                    Console.Write($"Indexing {e.Total} package(s)");
                }
                else
                {
                    Console.Write($"Indexing {e.Total} package(s). {e.Current} {Math.Truncate(((double)e.Current * 100) / e.Total)}%");
                }
            }
        }
    }
}
