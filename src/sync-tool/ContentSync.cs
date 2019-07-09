using Microsoft.UpdateServices.Storage;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.Metadata.Prerequisites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.UpdateServices.Tools.UpdateRepo
{
    class ContentSync
    {
        public static void Run(ContentSyncOptions options)
        {
            var localRepo = Program.LoadRepositoryFromOptions(options as IRepositoryPathOption);
            if (localRepo == null)
            {
                return;
            }

            var filter = MetadataFilter.RepositoryFilterFromCommandLineFilter(options as IUpdatesFilter);
            if (filter == null)
            {
                return;
            }

            // Apply filters specified on the command line
            var updatesToDownload = localRepo.GetUpdates(filter, UpdateRetrievalMode.Extended);

            // Only updates with files are considered
            updatesToDownload.RemoveAll(u => !(u is IUpdateWithFiles));

            // Apply the drivers filter
            if (options.Drivers)
            {
                // Sync only drivers
                updatesToDownload.RemoveAll(u => !(u is DriverUpdate));
            }

            if (updatesToDownload.Count == 0)
            {
                Console.WriteLine("No updates matched the filter");
                return;
            }

            localRepo.RepositoryOperationProgress += LocalRepo_RepositoryOperationProgress;

            var uniqueFiles = updatesToDownload.SelectMany(u => (u as IUpdateWithFiles).Files).GroupBy(f => f.DownloadUrl);

            var totalDownloadSize = uniqueFiles.Sum(f => (long)f.First().Size);
            var totalFilesToDownload = uniqueFiles.Count();
            Console.Write($"Downloading {totalDownloadSize} bytes in {totalFilesToDownload} files. Continue? (y/n)");
            var response = Console.ReadKey();
            if (response.Key == ConsoleKey.Y)
            {
                Console.WriteLine();
                foreach (var update in updatesToDownload)
                {
                    localRepo.DownloadUpdateContent(update as IUpdateWithFiles);
                }
            }
        }

        private static void LocalRepo_RepositoryOperationProgress(object sender, OperationProgress e)
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
