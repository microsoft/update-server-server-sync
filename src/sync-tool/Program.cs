// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using CommandLine;
using Microsoft.UpdateServices.Storage;

namespace Microsoft.UpdateServices.Tools.UpdateRepo
{
    class Program
    {
        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<
                RepositoryStatusOptions,
                UpdatesSyncOptions,
                QueryRepositoryOptions,
                InitRepositoryOptions,
                DeleteRepositoryOptions,
                RepositoryExportOptions,
                CategoriesSyncOptions,
                ContentSyncOptions,
                RunUpstreamServerOptions>(args)
                .WithParsed<DeleteRepositoryOptions>(opts => RepositoryAccess.Delete(opts))
                .WithParsed<InitRepositoryOptions>(opts => RepositoryAccess.Init(opts))
                .WithParsed<UpdatesSyncOptions>(opts => MetadataSync.SyncUpdates(opts))
                .WithParsed<QueryRepositoryOptions>(opts => RepositoryAccess.Query(opts))
                .WithParsed<RepositoryExportOptions>(opts => RepositoryExport.ExportUpdates(opts))
                .WithParsed<CategoriesSyncOptions>(opts => MetadataSync.SyncCategories(opts))
                .WithParsed<ContentSyncOptions>(opts => ContentSync.Run(opts))
                .WithParsed<RepositoryStatusOptions>(opts => RepositoryAccess.Status(opts))
                .WithParsed<RunUpstreamServerOptions>(opts => UpstreamServer.Run(opts))
                .WithNotParsed(failed => Console.WriteLine("Error"));
        }

        /// <summary>
        /// Loads a repository from the path specified on the command line
        /// </summary>
        /// <param name="repositoryOption">The command line switch that contains the path</param>
        /// <param name="openMode">Open mode: if existing of create if not existing</param>
        /// <returns>A repository if one was opened or created, null if a repo does not exist at the path and create was not requested</returns>
        public static IRepository LoadRepositoryFromOptions(IRepositoryPathOption repositoryOption)
        {
            Console.Write("Opening repository...");
            var repoPath = string.IsNullOrEmpty(repositoryOption.RepositoryPath) ? Environment.CurrentDirectory : repositoryOption.RepositoryPath;
            var localRepo = FileSystemRepository.Open(repoPath);
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
