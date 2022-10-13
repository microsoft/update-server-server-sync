// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System.Linq;
using System.Threading;
using System;
using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Storage;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;
using System.Collections.Generic;
using Microsoft.PackageGraph.Storage.Local;

namespace Microsoft.PackageGraph.Utilitites.Upsync
{
    class ContentSync
    {
        public static void SyncContent(ContentSyncOptions options)
        {
            var metadataSource = MetadataStoreCreator.OpenFromOptions(options as IMetadataStoreOptions);
            if (metadataSource == null)
            {
                return;
            }

            var contentStore = GetContentStoreFromOptions(options);
            if (contentStore == null)
            {
                return;
            }

            var filter = FilterBuilder.MicrosoftUpdateFilterFromCommandLine(options as IMetadataFilterOptions);
            if (filter == null)
            {
                return;
            }

            var filteredPackages = filter.Apply(metadataSource);

            var filesToDownload = filteredPackages.Where(p => p.Files != null).SelectMany(p => p.Files).ToList();

            foreach(var microsoftUpdatePackage in filteredPackages.OfType<MicrosoftUpdatePackage>())
            {
                filesToDownload.AddRange(GetAllUpdateFiles(metadataSource, microsoftUpdatePackage));
            }

            filesToDownload = filesToDownload.Distinct().ToList();

            Console.WriteLine($"Sync {filesToDownload.Count} files, {filesToDownload.Sum(f => (long)f.Size)} bytes. Continue? (y/n)");
            if (Console.ReadKey().Key != ConsoleKey.Y)
            {
                return;
            }

            CancellationTokenSource cancelTokenSource = new();
            contentStore.Progress += ContentStore_Progress;
            contentStore.Download(filesToDownload, cancelTokenSource.Token);
        }

        /// <summary>
        /// Gets all files for an update, including files in bundled updates (recursive)
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        private static List<IContentFile> GetAllUpdateFiles(IMetadataStore metadataSource, MicrosoftUpdatePackage update)
        {
            var filesList = new List<IContentFile>();
            if (update.Files != null)
            {
                filesList.AddRange(update.Files);
            }

            if (update is SoftwareUpdate softwareUpdate && softwareUpdate.BundledUpdates != null)
            {
                foreach (var bundledUpdate in softwareUpdate.BundledUpdates)
                {
                    filesList.AddRange(
                        GetAllUpdateFiles(
                            metadataSource,
                            metadataSource.GetPackage(bundledUpdate) as MicrosoftUpdatePackage));
                }
            }

            return filesList;
        }

        static string ContentSyncLastFileDigest = "";

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

        private static void ContentStore_Progress(object sender, ObjectModel.ContentOperationProgress e)
        {
            if (e.File.Digest.DigestBase64 != ContentSyncLastFileDigest)
            {
                Console.WriteLine();
                ContentSyncLastFileDigest = e.File.Digest.DigestBase64;
            }

            switch(e.CurrentOperation)
            {
                case ObjectModel.PackagesOperationType.DownloadFileProgress:
                    UpdateConsoleForMessageRefresh();
                    Console.Write("Sync'ing update content [{0}]: {1:000.00}%", e.Maximum, e.PercentDone);
                    break;
            }
        }

        private static IContentStore GetContentStoreFromOptions(ContentSyncOptions options)
        {
            switch(options.ContentStoreType)
            {
                case "local":
                    return new FileSystemContentStore(options.ContentPath);

                case "azure":
                    if (string.IsNullOrEmpty(options.ContentStoreConnectionString))
                    {
                        ConsoleOutput.WriteRed("Connection string required for Azure stores");
                        return null;
                    }

                    if (!CloudStorageAccount.TryParse(options.ContentStoreConnectionString, out var storageAccount))
                    {
                        ConsoleOutput.WriteRed("Invalid connection string");
                        return null;
                    }

                    var blobClient = storageAccount.CreateCloudBlobClient();
                    return Storage.Azure.BlobContentStore.OpenOrCreate(blobClient, options.ContentPath);

                default:
                    ConsoleOutput.WriteRed("Content store type not supported.");
                    return null;

            }
        }
    }
}
