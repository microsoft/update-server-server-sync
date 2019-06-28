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
            updatesToDownload.AddRange(localRepo.Updates.Updates.Values.Where(u => u is IUpdateWithFiles && u is IUpdateWithProduct && u is IUpdateWithClassification));

            // Apply the classification filter
            foreach(var classificationFilter in options.ClassificationsFilter)
            {
                if (!Guid.TryParse(classificationFilter, out Guid classificationId))
                {
                    ConsoleOutput.WriteRed("The classification filter must contain only GUIDs!");
                    return;
                }

                updatesToDownload.RemoveAll(u => !(u as IUpdateWithClassification).ClassificationIds.Contains(classificationId));
            }

            // Apply the product filter
            foreach(var productFilter in options.ProductsFilter)
            {
                if (!Guid.TryParse(productFilter, out Guid productId))
                {
                    ConsoleOutput.WriteRed("The product ID filter must contain only GUIDs!");
                    return;
                }

                updatesToDownload.RemoveAll(u => !(u as IUpdateWithProduct).ProductIds.Contains(productId));
            }

            // Apply the drivers filter
            if (options.Drivers)
            {
                // Sync only drivers
                updatesToDownload.RemoveAll(u => !(u is DriverUpdate));
            }

            if (!string.IsNullOrEmpty(options.TitleFilter))
            {
                var filterTokens = options.TitleFilter.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                updatesToDownload.RemoveAll(category => !category.MatchTitle(filterTokens));
            }

            if (options.IdFilter.Count() > 0)
            {
                var idFilter = new List<Guid>();
                foreach(var stringId in options.IdFilter)
                {
                    if (!Guid.TryParse(stringId, out Guid guidId))
                    {
                        ConsoleOutput.WriteRed("The ID filter must be a GUID string!");
                        return;
                    }

                    idFilter.Add(guidId);
                }

                // Remove all updates that don't match the ID filter
                updatesToDownload.RemoveAll(u  => !idFilter.Contains(u.Identity.Raw.UpdateID));
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
