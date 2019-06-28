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
    /// <summary>
    /// Implements query and management operations on a local updates repository
    /// </summary>
    class RepositoryAccess
    {
        private readonly Repository TargetRepo;
        private readonly QueryRepositoryOptions Options;

        /// <summary>
        ///  Runs a local repo query command
        /// </summary>
        /// <param name="options">Query options (filters)</param>
        public static void Query(QueryRepositoryOptions options)
        {
            var localRepo = Program.LoadRepositoryFromOptions(options as IRepositoryPathOption, Repository.RepositoryOpenMode.OpenExisting);
            if (localRepo == null)
            {
                return;
            }

            var repoQuery = new RepositoryAccess(localRepo, options);
            repoQuery.Query();
        }

        /// <summary>
        /// Deletes the repo specified in the options
        /// </summary>
        /// <param name="options">Options containing the path to the repo to delete</param>
        public static void Delete(DeleteRepositoryOptions options)
        {
            var localRepo = Program.LoadRepositoryFromOptions(options as IRepositoryPathOption, Repository.RepositoryOpenMode.OpenExisting);
            if (localRepo == null)
            {
                return;
            }

            Console.Write("Deleting the repository...");
            localRepo.Delete();
            ConsoleOutput.WriteGreen("Done!");
        }

        private RepositoryAccess(Repository localRepo, QueryRepositoryOptions options)
        {
            TargetRepo = localRepo;
            Options = options;
        }

        private void Query()
        {
            if (Options.Configuration)
            {
                PrintConfiguration();
            }
            else if (Options.Products ||
                Options.Classifications ||
                Options.Updates ||
                Options.Drivers ||
                Options.Detectoids)
            {
                PrintUpdates(Options);
            }
        }

        /// <summary>
        /// Print the service configuration from the store
        /// </summary>
        public void PrintConfiguration()
        {
            var configuration = TargetRepo.GetServiceConfiguration();
            if (configuration == null)
            {
                ConsoleOutput.WriteRed("Cannot read configuration data from the store!");
                return;
            }

            Console.WriteLine("Anchor config     |  {0}", configuration.NewConfigAnchor);
            Console.WriteLine("Protocol version  |  {0}", configuration.ProtocolVersion);
            Console.WriteLine("Hosts psf files   |  {0}", configuration.ServerHostsPsfFiles);
            Console.WriteLine("Catalog only sync |  {0}", configuration.CatalogOnlySync);
            Console.WriteLine("Lazy sync         |  {0}", configuration.LazySync);
            Console.WriteLine("Max computer ids  |  {0}", configuration.MaxNumberOfComputerIdsInRequest);
            Console.WriteLine("Max driver sets   |  {0}", configuration.MaxNumberOfDriverSetsPerRequest);
            Console.WriteLine("Max hardware ids  |  {0}", configuration.MaxNumberOfPnpHardwareIdsInRequest);
            Console.WriteLine("Max updates       |  {0}", configuration.MaxNumberOfUpdatesPerRequest);
        }

        /// <summary>
        /// Print updates from the store
        /// </summary>
        /// <param name="options">Print options, including filters</param>
        public void PrintUpdates(QueryRepositoryOptions options)
        {
            // Collect updates that pass the filter
            var filteredData = new List<MicrosoftUpdate>();

            if (options.Classifications || options.Products || options.Detectoids)
            {
                if (options.Classifications)
                {
                    filteredData.AddRange(TargetRepo.Categories.Classifications);
                }

                if (options.Products)
                {
                    filteredData.AddRange(TargetRepo.Categories.Products);
                }

                if (options.Detectoids)
                {
                    filteredData.AddRange(TargetRepo.Categories.Detectoids);
                }
            }

            if (options.Updates || options.Drivers)
            {
                if (options.Updates)
                {
                    filteredData.AddRange(TargetRepo.Updates.Updates.Values);
                }
                else if (options.Drivers)
                {
                    filteredData.AddRange(TargetRepo.Updates.Drivers);
                }
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
                Console.WriteLine("No data found");
            }
            else
            {
                Console.Write("\r\nQuery results:\r\n-----------------------------");

                foreach (var update in filteredData)
                {
                    PrintUpdateMetadata(update);
                }

                Console.WriteLine("-----------------------------\r\nMatched {0} entries", filteredData.Count);
            }
        }

        /// <summary>
        /// Print update metadata
        /// </summary>
        /// <param name="update">The update to print metadata for</param>
        void PrintUpdateMetadata(MicrosoftUpdate update)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\r\nID: {0}", update.Identity.Raw.UpdateID);
            Console.ResetColor();
            Console.WriteLine("    Title          : {0}", update.Title);
            Console.WriteLine("    Description    : {0}", update.Description);

            if (update is DriverUpdate)
            {
                PrintDriverMetadata(update as DriverUpdate);

            }
            else if (update is SoftwareUpdate)
            {
                PrintSoftwareUpdateMetadata(update as SoftwareUpdate);
            }

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
                        var matchindProducts = TargetRepo.Categories.Products.Where(p => p.Identity.Raw.UpdateID == updateParentId).ToList();
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
                        var matchingClassification = TargetRepo.Categories.Classifications.Where(p => p.Identity.Raw.UpdateID == classificationId).ToList();
                        if (matchingClassification.Count > 0)
                        {
                            Console.WriteLine("        Classification name : {0}", matchingClassification[0].Title);
                        }
                    }
                }
            }

            if (update is IUpdateWithFiles)
            {
                PrintFileDetails(update as IUpdateWithFiles);
            }

            if (update is IUpdateWithSuperseededUpdates)
            {
                PrintSuperseededUpdates(update as IUpdateWithSuperseededUpdates);
            }

            if (update is IUpdateWithBundledUpdates)
            {
                PrintBundledUpdates(update as IUpdateWithBundledUpdates);
            }

            if (update is IUpdateWithPrerequisites)
            {
                PrintPrerequisites(update as IUpdateWithPrerequisites);
            }

            //Console.WriteLine("{0}", update.XmlData);
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

        void PrintSuperseededUpdates(IUpdateWithSuperseededUpdates updateWithSuperseeds)
        {
            if (updateWithSuperseeds.SuperseededUpdates.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("    Superseeds:");
                Console.ResetColor();

                foreach (var id in updateWithSuperseeds.SuperseededUpdates)
                {
                    Console.WriteLine("        ID  : {0}", id.Raw.UpdateID);
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
                    Console.WriteLine("        ID        : {0}", id.Raw.UpdateID);
                    Console.WriteLine("        Revision  : {0}", id.Raw.RevisionNumber);
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
                    else if (prereq is SimplePrerequisite)
                    {
                        Console.WriteLine("            ID          : {0}", (prereq as SimplePrerequisite).UpdateId);
                    }
                }
            }
        }
    }
}
