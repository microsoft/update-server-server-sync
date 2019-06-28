using System;
using System.Collections.Generic;
using CommandLine;
using Microsoft.UpdateServices.LocalCache;
using Microsoft.UpdateServices.Metadata;

namespace Microsoft.UpdateServices.Tools.UpdateRepo
{
    class Program
    {
        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<
                UpdatesSyncOptions,
                QueryRepositoryOptions,
                InitRepositoryOptions,
                DeleteRepositoryOptions,
                RepositoryExportOptions,
                CategoriesSyncOptions,
                ContentSyncOptions>(args)
                .WithParsed<DeleteRepositoryOptions>(opts => RepositoryAccess.Delete(opts))
                .WithParsed<InitRepositoryOptions>(opts => MetadataSync.SyncConfiguration(opts))
                .WithParsed<UpdatesSyncOptions>(opts => MetadataSync.SyncUpdates(opts))
                .WithParsed<QueryRepositoryOptions>(opts => RepositoryAccess.Query(opts))
                .WithParsed<RepositoryExportOptions>(opts => RepositoryExport.ExportUpdates(opts))
                .WithParsed<CategoriesSyncOptions>(opts => MetadataSync.SyncCategories(opts))
                .WithParsed<ContentSyncOptions>(opts => ContentSync.Run(opts))
                .WithNotParsed(failed => Console.WriteLine("Error"));
        }

        /// <summary>
        /// Loads a repository from the path specified on the command line
        /// </summary>
        /// <param name="repositoryOption">The command line switch that contains the path</param>
        /// <param name="openMode">Open mode: if existing of create if not existing</param>
        /// <returns>A repository if one was opened or created, null if a repo does not exist at the path and create was not requested</returns>
        public static Repository LoadRepositoryFromOptions(IRepositoryPathOption repositoryOption, Repository.RepositoryOpenMode openMode)
        {
            Console.Write("Opening repository...");
            var repoPath = string.IsNullOrEmpty(repositoryOption.RepositoryPath) ? Environment.CurrentDirectory : repositoryOption.RepositoryPath;
            var localRepo = Repository.FromDirectory(repoPath, openMode);
            if (localRepo == null)
            {
                ConsoleOutput.WriteRed($"There is no repository at path {repoPath}");
            }
            else
            {
                ConsoleOutput.WriteGreen("Done!");
            }

            return localRepo;
        }
    }
}
