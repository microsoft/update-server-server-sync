using Microsoft.UpdateServices.LocalCache;
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
            var localRepo = Program.LoadRepositoryFromOptions(options as IRepositoryPathOption, Repository.RepositoryOpenMode.OpenExisting);
            if (localRepo == null)
            {
                return;
            }

            // Collect updates that pass the filter
            var updatesToDownload = new List<MicrosoftUpdate>();

            // Only updates with files and those with a product and classifications are considered
            updatesToDownload.AddRange(localRepo.Updates.Index.Values.Where(u => u is IUpdateWithFiles && u is IUpdateWithProduct && u is IUpdateWithClassification));

            // Apply the drivers filter
            if (options.Drivers)
            {
                // Sync only drivers
                updatesToDownload.RemoveAll(u => !(u is DriverUpdate));
            }

            // Apply other filters specified on the command line
            MetadataFilter.Apply(updatesToDownload, options as IUpdatesFilter);

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

        private static void LocalRepo_RepositoryOperationProgress(object sender, RepoOperationProgress e)
        {
            switch (e.CurrentOperation)
            {
                case RepoOperationTypes.DownloadFileStart:
                    Console.Write("Downloading {0,60} [000.00%]", (e as RepoContentOperationProgress).File.FileName);
                    break;

                case RepoOperationTypes.HashFileStart:
                    Console.Write("Hashing     {0,60} [000.00%]", (e as RepoContentOperationProgress).File.FileName);
                    break;

                case RepoOperationTypes.DownloadFileEnd:
                    Console.CursorLeft = 0;
                    Console.Write("Downloading {0,60} [100.00%] ", (e as RepoContentOperationProgress).File.FileName);
                    ConsoleOutput.WriteGreen("Done!");
                    break;

                case RepoOperationTypes.HashFileEnd:
                    Console.CursorLeft = 0;
                    Console.Write("Hashing     {0,60} [100.00%] ", (e as RepoContentOperationProgress).File.FileName);
                    ConsoleOutput.WriteGreen("Done!");
                    break;

                case RepoOperationTypes.DownloadFileProgress:
                    Console.CursorLeft = 0;
                    Console.Write("Downloading {0,60} [{1:000.00}%]", (e as RepoContentOperationProgress).File.FileName, e.PercentDone);
                    break;

                case RepoOperationTypes.HashFileProgress:
                    Console.CursorLeft = 0;
                    Console.Write("Hashing     {0,60} [{1:000.00}%]", (e as RepoContentOperationProgress).File.FileName, e.PercentDone);
                    break;
            }
        }
    }
}
