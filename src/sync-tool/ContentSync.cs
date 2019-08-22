// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Storage;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Metadata.Content;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Microsoft.UpdateServices.Tools.UpdateRepo
{
    class ContentSync
    {
        public static void Run(ContentSyncOptions options)
        {
            var metadataSource = Program.LoadMetadataSourceFromOptions(options as IMetadataSourceOptions);
            if (metadataSource == null)
            {
                return;
            }

            var filter = FilterBuilder.MetadataFilterFromCommandLine(options as IMetadataFilterOptions);
            if (filter == null)
            {
                return;
            }

            // Apply filters specified on the command line
            var updatesToDownload = metadataSource.GetUpdates(filter);
            if (updatesToDownload.Count == 0)
            {
                Console.WriteLine("No updates matched the filter");
                return;
            }

            var filesToDownload = new List<UpdateFile>();
            foreach(var update in updatesToDownload)
            {
                filesToDownload.AddRange(MetadataQuery.GetAllUpdateFiles(metadataSource, update));
            }

            var contentDestination = new FileSystemContentStore(options.ContentDestination);
            contentDestination.Progress += LocalSource_OperationProgress;

            var uniqueFiles = filesToDownload.GroupBy(f => f.DownloadUrl).Select(g => g.First()).ToList();

            uniqueFiles.RemoveAll(f => contentDestination.Contains(f));

            if (uniqueFiles.Count == 0)
            {
                ConsoleOutput.WriteGreen("The content matching the filter is up-to-date");
                return;
            }

            var totalDownloadSize = uniqueFiles.Sum(f => (long)f.Size);
            Console.Write($"Downloading {totalDownloadSize} bytes in {uniqueFiles.Count} files. Continue? (y/n)");
            var response = Console.ReadKey();
            if (response.Key == ConsoleKey.Y)
            {
                Console.WriteLine();
                contentDestination.Add(uniqueFiles);
            }
        }

        private static void LocalSource_OperationProgress(object sender, OperationProgress e)
        {
            switch (e.CurrentOperation)
            {
                case OperationType.DownloadFileStart:
                    Console.Write("Downloading {0,60} [000.00%]", (e as ContentOperationProgress).File.FileName);
                    break;

                case OperationType.HashFileStart:
                    Console.Write("Hashing     {0,60} [000.00%]", (e as ContentOperationProgress).File.FileName);
                    break;

                case OperationType.DownloadFileEnd:
                    Console.CursorLeft = 0;
                    Console.Write("Downloading {0,60} [100.00%] ", (e as ContentOperationProgress).File.FileName);
                    ConsoleOutput.WriteGreen("Done!");
                    break;

                case OperationType.HashFileEnd:
                    Console.CursorLeft = 0;
                    Console.Write("Hashing     {0,60} [100.00%] ", (e as ContentOperationProgress).File.FileName);
                    ConsoleOutput.WriteGreen("Done!");
                    break;

                case OperationType.DownloadFileProgress:
                    Console.CursorLeft = 0;
                    Console.Write("Downloading {0,60} [{1:000.00}%]", (e as ContentOperationProgress).File.FileName, e.PercentDone);
                    break;

                case OperationType.HashFileProgress:
                    Console.CursorLeft = 0;
                    Console.Write("Hashing     {0,60} [{1:000.00}%]", (e as ContentOperationProgress).File.FileName, e.PercentDone);
                    break;
            }
        }
    }
}
