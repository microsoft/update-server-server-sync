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
                CategoriesSyncOptions>(args)
                .WithParsed<DeleteRepositoryOptions>(opts => DeleteRepository(opts))
                .WithParsed<InitRepositoryOptions>(opts => InitRepository(opts))
                .WithParsed<UpdatesSyncOptions>(opts => SyncUpdates(opts))
                .WithParsed<QueryRepositoryOptions>(opts => QueryLocalRepo(opts))
                .WithParsed<RepositoryExportOptions>(opts => ExportUpdates(opts))
                .WithParsed<CategoriesSyncOptions>(opts => SyncCategories(opts))
                .WithNotParsed(failed => Console.WriteLine("Error"));
        }

        /// <summary>
        /// Syncs categories in a local updates repository
        /// </summary>
        /// <param name="options">Sync options</param>
        static void SyncCategories(CategoriesSyncOptions options)
        {
            var localRepo = LoadRepositoryFromOptions(options as IRepositoryPathOption, Repository.RepositoryOpenMode.CreateIfDoesNotExist);

            var cachedToken = localRepo.GetAccessToken();
            if (cachedToken != null)
            {
                Console.WriteLine("Loaded cached access token.");
            }

            var serviceConfig = localRepo.GetServiceConfiguration();
            if (serviceConfig != null)
            {
                Console.WriteLine("Loaded cached service configuration.");
            }

            var server = new UpstreamServerClient(Endpoint.Default, serviceConfig, cachedToken);
            server.MetadataQueryProgress += Server_MetadataQueryProgress;
            var newCategories = server.GetCategories(localRepo.Categories).GetAwaiter().GetResult();

            Console.Write("Merging the query result and commiting the changes...");
            localRepo.MergeQueryResult(newCategories);
            ConsoleOutput.WriteGreen("Done!");

            localRepo.CacheAccessToken(server.AccessToken);
            localRepo.CacheServiceConfiguration(server.ConfigData);
        }

        /// <summary>
        /// Export update metadata from a repository
        /// </summary>
        /// <param name="options">Export options</param>
        static void ExportUpdates(RepositoryExportOptions options)
        {
            var localRepo = LoadRepositoryFromOptions(options as IRepositoryPathOption, Repository.RepositoryOpenMode.OpenExisting);
            if (localRepo == null)
            {
                return;
            }

            // Collect updates that pass the filter
            var filteredData = new List<MicrosoftUpdate>();

            if (options.Drivers)
            {
                filteredData.AddRange(localRepo.Updates.Drivers);
            }
            else
            {
                filteredData.AddRange(localRepo.Updates.Updates.Values);
            }

            if (!string.IsNullOrEmpty(options.TitleFilter))
            {
                var filterTokens = options.TitleFilter.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                filteredData.RemoveAll(category => !category.MatchTitle(filterTokens));
            }

            if (!string.IsNullOrEmpty(options.IdFilter))
            {
                if (!Guid.TryParse(options.IdFilter, out Guid guidFilter))
                {
                    ConsoleOutput.WriteRed("The ID filter must be a GUID string!");
                    return;
                }

                filteredData.RemoveAll(category => category.Identity.Raw.UpdateID != guidFilter);
            }

            if (filteredData.Count == 0)
            {
                ConsoleOutput.WriteRed("No data found that matches the filters");
                return;
            }

            localRepo.RepositoryOperationProgress += LocalRepo_RepositoryOperationProgress;
            localRepo.Export(filteredData, options.ExportFile, Repository.ExportFormat.WSUS_2016);
        }

        /// <summary>
        /// Initialize a new local updates repository
        /// </summary>
        /// <param name="options">Initialization options (path)</param>
        static void InitRepository(InitRepositoryOptions options)
        {
            var localRepo = LoadRepositoryFromOptions(options as IRepositoryPathOption, Repository.RepositoryOpenMode.CreateIfDoesNotExist);

            var server = new UpstreamServerClient(Endpoint.Default);
            server.MetadataQueryProgress += Server_MetadataQueryProgress;
            server.RefreshAccessToken().GetAwaiter().GetResult();
            server.RefreshServerConfigData().GetAwaiter().GetResult();

            localRepo.CacheAccessToken(server.AccessToken);
            localRepo.CacheServiceConfiguration(server.ConfigData);
            
            ConsoleOutput.WriteGreen("Done!");
        }

        /// <summary>
        /// Deletes the repo specified in the options
        /// </summary>
        /// <param name="options">Options containing the path to the repo to delete</param>
        static void DeleteRepository(DeleteRepositoryOptions options)
        {
            var localRepo = LoadRepositoryFromOptions(options as IRepositoryPathOption, Repository.RepositoryOpenMode.OpenExisting);
            if(localRepo == null)
            {
                return;
            }

            Console.Write("Deleting the repository...");
            localRepo.Delete();
            ConsoleOutput.WriteGreen("Done!");
        }

        /// <summary>
        ///  Runs a local repo query command
        /// </summary>
        /// <param name="options">Query options (filters)</param>
        static void QueryLocalRepo(QueryRepositoryOptions options)
        {
            var localRepo = LoadRepositoryFromOptions(options as IRepositoryPathOption, Repository.RepositoryOpenMode.OpenExisting);
            if (localRepo == null)
            {
                return;
            }

            var localStoreQuery = new LocalStoreQuery(localRepo);
            localStoreQuery.Run(options);
        }

        /// <summary>
        /// Loads a repository from the path specified on the command line
        /// </summary>
        /// <param name="repositoryOption">The command line switch that contains the path</param>
        /// <param name="openMode">Open mode: if existing of create if not existing</param>
        /// <returns>A repository if one was opened or created, null if a repo does not exist at the path and create was not requested</returns>
        static Repository LoadRepositoryFromOptions(IRepositoryPathOption repositoryOption, Repository.RepositoryOpenMode openMode)
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

        /// <summary>
        /// Runs the sync command
        /// </summary>
        /// <param name="options">Sync command options</param>
        static void SyncUpdates(UpdatesSyncOptions options)
        {
            var localRepo = LoadRepositoryFromOptions(options as IRepositoryPathOption, Repository.RepositoryOpenMode.OpenExisting);

            if (localRepo == null)
            {
                ConsoleOutput.WriteRed("Initialize the repo and sync categories first!");
                return;
            }

            if (localRepo.Categories.Categories.Count == 0)
            {
                ConsoleOutput.WriteRed("Categories must be sync'ed before updates. Please run \"sync-categories\" first");
                return;
            }

            Query.QueryFilter filter;
            try
            {
                filter = RepoSync.CreateValidFilterFromOptions(options, localRepo.Categories);
            }
            catch(Exception ex)
            {
                ConsoleOutput.WriteRed(ex.Message);
                return;
            }

            var cachedToken = localRepo.GetAccessToken();
            if (cachedToken != null)
            {
                Console.WriteLine("Loaded cached access token.");
            }

            var serviceConfig = localRepo.GetServiceConfiguration();
            if (serviceConfig != null)
            {
                Console.WriteLine("Loaded cached service configuration.");
            }

            var server = new UpstreamServerClient(Endpoint.Default, serviceConfig, cachedToken);
            server.MetadataQueryProgress += Server_MetadataQueryProgress;

            var newUpdates = server.GetUpdates(filter, localRepo.Updates).GetAwaiter().GetResult();

            Console.Write("Merging the query result and commiting the changes...");
            localRepo.MergeQueryResult(newUpdates);
            ConsoleOutput.WriteGreen("Done!");

            localRepo.CacheAccessToken(server.AccessToken);
            localRepo.CacheServiceConfiguration(server.ConfigData);
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
                case QuerySubTaskTypes.AuthenticateStart:
                    Console.Write("Acquiring new access token...");
                    break;

                case QuerySubTaskTypes.GetServerConfigStart:
                    Console.Write("Retrieving service configuration data...");
                    break;

                case QuerySubTaskTypes.AuthenticateEnd:
                case QuerySubTaskTypes.GetServerConfigEnd:
                case QuerySubTaskTypes.GetRevisionIdsEnd:
                    ConsoleOutput.WriteGreen("Done!");
                    break;

                case QuerySubTaskTypes.GetRevisionIdsStart:
                    Console.Write("Retrieving revision IDs...");
                    break;

                case QuerySubTaskTypes.GetUpdateMetadataStart:
                    Console.Write("Retrieving {0} updates metadata: 0%", e.Maximum);
                    break;

                case QuerySubTaskTypes.GetUpdateMetadataEnd:
                    Console.CursorLeft = 0;
                    Console.WriteLine("Retrieving {0} updates metadata: 100% Done", e.Maximum);
                    break;

                case QuerySubTaskTypes.GetUpdateMetadataProgress:
                    Console.CursorLeft = 0;
                    Console.Write("Retrieving {0} updates metadata: {1:000.00}%", e.Maximum, e.PercentDone);
                    break;
            }
        }

        /// <summary>
        /// Handles progress notifications from a local repository
        /// Prints progress information to the console
        /// </summary>
        /// <param name="sender">The local repository that is executing a long running operation</param>
        /// <param name="e">Progress information</param>
        private static void LocalRepo_RepositoryOperationProgress(object sender, RepoOperationProgress e)
        {
            switch(e.CurrentOperation)
            {
                case RepoOperationTypes.ExportUpdateXmlBlobStart:
                    Console.Write("Exporting update XML data: 000.00%");
                    break;

                case RepoOperationTypes.ExportUpdateXmlBlobProgress:
                    Console.CursorLeft = 0;
                    Console.Write("Exporting {0} update(s) and categories XML data: {1:000.00}%", e.Maximum, e.PercentDone);
                    break;

                case RepoOperationTypes.ExportMetadataStart:
                    Console.Write("Packing metadata...");
                    break;

                case RepoOperationTypes.CompressExportFileStart:
                    Console.WriteLine("Compressing output export file...\r\n");
                    break;

                case RepoOperationTypes.ExportMetadataEnd:
                case RepoOperationTypes.ExportUpdateXmlBlobEnd:
                case RepoOperationTypes.CompressExportFileEnd:
                    ConsoleOutput.WriteGreen("Done!");
                    break;

            }
        }
    }
}
