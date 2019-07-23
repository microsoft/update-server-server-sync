// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Storage;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.Metadata.Prerequisites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UpdateServices.Client;

namespace Microsoft.UpdateServices.Tools.UpdateRepo
{
    /// <summary>
    /// Implements query and management operations on a local updates repository
    /// </summary>
    class RepositoryAccess
    {
        private readonly IRepository TargetRepo;
        private readonly QueryRepositoryOptions Options;

        /// <summary>
        ///  Runs a local repo query command
        /// </summary>
        /// <param name="options">Query options (filters)</param>
        public static void Query(QueryRepositoryOptions options)
        {
            var localRepo = Program.LoadRepositoryFromOptions(options as IRepositoryPathOption);
            if (localRepo == null)
            {
                return;
            }

            var repoQuery = new RepositoryAccess(localRepo, options);
            repoQuery.Query();
        }

        public static void Status(RepositoryStatusOptions options)
        {
            var localRepo = Program.LoadRepositoryFromOptions(options as IRepositoryPathOption);
            if (localRepo == null)
            {
                return;
            }

            Console.WriteLine($"Upstream server: {localRepo.Configuration.UpstreamServerEndpoint.URI}");
            Console.WriteLine($"Account name: {localRepo.Configuration.AccountName}");
            Console.WriteLine($"Account GUID: {localRepo.Configuration.AccountGuid.ToString()}");
        }

        /// <summary>
        /// Initialize a new repo or updates repo configuration
        /// </summary>
        /// <param name="options">Options containing the path to the repo to delete</param>
        public static void Init(InitRepositoryOptions options)
        {
            var repoPath = string.IsNullOrEmpty(options.RepositoryPath) ? Environment.CurrentDirectory : options.RepositoryPath;
            var upstreamServer = string.IsNullOrEmpty(options.UpstreamServerAddress) ? Endpoint.Default.URI : options.UpstreamServerAddress;

            FileSystemRepository repo;

            if (FileSystemRepository.RepoExists(repoPath))
            {
                repo = FileSystemRepository.Open(repoPath);
            }
            else
            {
                repo = FileSystemRepository.Init(repoPath, upstreamServer);
                ConsoleOutput.WriteGreen($"Repository created. Upstream server: {repo.Configuration.UpstreamServerEndpoint.URI}");
            }

            if (!string.IsNullOrEmpty(options.AccountName) &&
                !string.IsNullOrEmpty(options.AccountGuid))
            {
                if (!Guid.TryParse(options.AccountGuid, out Guid accountGuid))
                {
                    ConsoleOutput.WriteRed("The account GUID must be a valid GUID string");
                    return;
                }

                repo.SetRemoteEndpointCredentials(options.AccountName, accountGuid);
                ConsoleOutput.WriteGreen("Credentials updated.");
            }
        }

        /// <summary>
        /// Deletes the repo specified in the options
        /// </summary>
        /// <param name="options">Options containing the path to the repo to delete</param>
        public static void Delete(DeleteRepositoryOptions options)
        {
            var localRepo = Program.LoadRepositoryFromOptions(options as IRepositoryPathOption);
            if (localRepo == null)
            {
                return;
            }

            Console.Write("Deleting the repository...");
            localRepo.Delete();
            ConsoleOutput.WriteGreen("Done!");
        }

        private RepositoryAccess(IRepository localRepo, QueryRepositoryOptions options)
        {
            TargetRepo = localRepo;
            Options = options;
        }

        private void Query()
        {
            if (Options.Products ||
                Options.Classifications ||
                Options.Updates ||
                Options.Drivers ||
                Options.Detectoids)
            {
                PrintUpdates(Options);
            }
        }

        /// <summary>
        /// Print updates from the store
        /// </summary>
        /// <param name="options">Print options, including filters</param>
        public void PrintUpdates(QueryRepositoryOptions options)
        {
            var filter = MetadataFilter.RepositoryFilterFromCommandLineFilter(options as IUpdatesFilter);
            if (filter == null)
            {
                return;
            }

            filter.SkipSuperseded = options.SkipSuperseded;
            filter.FirstX = options.FirstX;

            var metadataMode = options.ExtendedMetadata ? UpdateRetrievalMode.Extended : UpdateRetrievalMode.Basic;

            // Apply filters specified on the command line
            List<Update> filteredUpdates;

            if (options.Classifications || options.Products || options.Detectoids)
            {
                filteredUpdates = TargetRepo.GetCategories(filter);
                if (!options.Classifications)
                {
                    filteredUpdates.RemoveAll(u => u is Classification);
                }

                if (!options.Products)
                {
                    filteredUpdates.RemoveAll(u => u is Product);
                }

                if (!options.Detectoids)
                {
                    filteredUpdates.RemoveAll(u => u is Detectoid);
                }
            }
            else if (options.Updates || options.Drivers)
            {
                filteredUpdates = TargetRepo.GetUpdates(filter, metadataMode);

                if (options.Drivers)
                {
                    filteredUpdates.RemoveAll(u => !(u is DriverUpdate));
                }
            }
            else
            {
                filteredUpdates = new List<Update>();
            }

            if (filteredUpdates.Count == 0)
            {
                Console.WriteLine("No data found");
            }
            else
            {
                Console.Write("\r\nQuery results:\r\n-----------------------------");

                if (!options.CountOnly)
                {
                    foreach (var update in filteredUpdates)
                    {
                        PrintUpdateMetadata(update, metadataMode);
                    }
                }

                Console.WriteLine("-----------------------------\r\nMatched {0} entries", filteredUpdates.Count);
            }
        }

        /// <summary>
        /// Print update metadata
        /// </summary>
        /// <param name="update">The update to print metadata for</param>
        void PrintUpdateMetadata(Update update, UpdateRetrievalMode metadataMode)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\r\nID: {0}", update.Identity.ID);
            Console.ResetColor();
            Console.WriteLine("    Title          : {0}", update.Title);
            Console.WriteLine("    Description    : {0}", update.Description);

            var productInfo = update as Metadata.IUpdateWithProduct;
            var classificationInfo = update as IUpdateWithClassification;

            if (productInfo != null || classificationInfo != null)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("    Categories:");
                Console.ResetColor();

                if (productInfo != null)
                {
                    var updateParentIds = productInfo.ProductIds;
                    foreach (var updateParentId in updateParentIds)
                    {
                        Console.WriteLine("        Product ID          : {0}", updateParentId);
                        var matchindProducts = TargetRepo.ProductsIndex.Values.Where(p => p.Identity.ID == updateParentId).ToList();
                        if (matchindProducts.Count > 0)
                        {
                            Console.WriteLine("        Product name        : {0}", matchindProducts[0].Title);
                        }
                    }
                }

                if (classificationInfo != null)
                {
                    var classificationIds = classificationInfo.ClassificationIds;
                    foreach (var classificationId in classificationIds)
                    {
                        Console.WriteLine("        Classification ID   : {0}", classificationId);
                        var matchingClassification = TargetRepo.ClassificationsIndex.Values.Where(p => p.Identity.ID == classificationId).ToList();
                        if (matchingClassification.Count > 0)
                        {
                            Console.WriteLine("        Classification name : {0}", matchingClassification[0].Title);
                        }
                    }
                }
            }

            if (metadataMode == UpdateRetrievalMode.Basic)
            {
                return;
            }

            if (update is DriverUpdate)
            {
                PrintDriverMetadata(update as DriverUpdate);

            }
            else if (update is SoftwareUpdate)
            {
                PrintSoftwareUpdateMetadata(update as SoftwareUpdate);
            }

            if (update is IUpdateWithFiles)
            {
                PrintFileDetails(update as IUpdateWithFiles);
            }

            if (update is IUpdateWithSupersededUpdates)
            {
                PrintSupersededUpdates(update as IUpdateWithSupersededUpdates);
            }

            if (update is IUpdateWithBundledUpdates)
            {
                PrintBundledUpdates(update as IUpdateWithBundledUpdates);
            }

            if (update is IUpdateWithPrerequisites)
            {
                PrintPrerequisites(update as IUpdateWithPrerequisites);
            }
        }

        void PrintDriverMetadata(DriverUpdate driverUpdate)
        {
            foreach (var driverMetadata in driverUpdate.Metadata)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("    Metadata:");
                Console.ResetColor();
                Console.WriteLine("        HardwareId : {0}", driverMetadata.HardwareID);
                Console.WriteLine("        Date       : {0}", driverMetadata.DriverVerDate);
                Console.WriteLine("        Version    : {0}", driverMetadata.DriverVerVersion);
                Console.WriteLine("        Class      : {0}", driverMetadata.Class);
            }
        }

        void PrintSoftwareUpdateMetadata(SoftwareUpdate softwareUpdate)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("    Metadata:");
            Console.ResetColor();
            Console.WriteLine("        Support URL : {0}", softwareUpdate.SupportUrl);
            Console.WriteLine("        KB Article  : {0}", softwareUpdate.KBArticleId);

            if (!string.IsNullOrEmpty(softwareUpdate.OsUpgrade))
            {
                Console.WriteLine("        OsUpgrade   : {0}", softwareUpdate.OsUpgrade);
            }
        }

        void PrintFileDetails(IUpdateWithFiles updateWithFiles)
        {
            foreach (var file in updateWithFiles.Files)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("    File:");
                Console.ResetColor();

                Console.WriteLine("        Name           : {0}", file.FileName);
                Console.WriteLine("        Size           : {0}", file.Size);

                foreach (var hash in file.Digests)
                {
                    Console.WriteLine("        Digest         : {0} {1}", hash.Algorithm, hash.DigestBase64);
                }

                foreach (var url in file.Urls)
                {
                    if (!string.IsNullOrEmpty(url.MuUrl))
                    {
                        Console.WriteLine("        MU URL         : {0}", url.MuUrl);
                    }

                    if (!string.IsNullOrEmpty(url.UssUrl))
                    {
                        Console.WriteLine("        USS URL        : {0}", url.UssUrl);
                    }
                }

                if (!string.IsNullOrEmpty(file.PatchingType))
                {
                    Console.WriteLine("        Patching type  : {0}", file.PatchingType);
                }
            }
        }

        void PrintSupersededUpdates(IUpdateWithSupersededUpdates updateWithSuperseeds)
        {
            if (updateWithSuperseeds.SupersededUpdates.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("    Superseeds:");
                Console.ResetColor();

                foreach (var id in updateWithSuperseeds.SupersededUpdates)
                {
                    Console.WriteLine("        ID  : {0}", id.ID);
                }
            }
        }

        void PrintBundledUpdates(IUpdateWithBundledUpdates updateWithBundledUpdates)
        {
            if (updateWithBundledUpdates.BundledUpdates.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("    Bundled updates:");
                Console.ResetColor();

                foreach (var id in updateWithBundledUpdates.BundledUpdates)
                {
                    Console.WriteLine("        ID        : {0}", id.ID);
                    Console.WriteLine("        Revision  : {0}", id.Revision);
                }
            }
        }

        void PrintPrerequisites(IUpdateWithPrerequisites updateWithPrereqs)
        {
            if (updateWithPrereqs.Prerequisites.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("    Prerequisites:");
                Console.ResetColor();

                foreach (var prereq in updateWithPrereqs.Prerequisites)
                {
                    var atLeastOneOf = prereq as AtLeastOne;
                    if (atLeastOneOf != null && !atLeastOneOf.IsCategory)
                    {
                        Console.WriteLine("        At least one of:");

                        foreach (var subPrereq in atLeastOneOf.Simple)
                        {
                            Console.WriteLine("            ID          : {0}", subPrereq.UpdateId);
                        }
                    }
                    else if (prereq is Simple)
                    {
                        Console.WriteLine("            ID          : {0}", (prereq as Simple).UpdateId);
                    }
                }
            }
        }
    }
}
