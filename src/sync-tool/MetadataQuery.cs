// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Storage;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.Metadata.Prerequisites;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.UpdateServices.Tools.UpdateRepo
{
    /// <summary>
    /// Implements query and management operations on a local updates metadata source
    /// </summary>
    class MetadataQuery
    {
        private readonly IMetadataSource MetadataSource;
        private readonly QueryMetadataOptions Options;

        /// <summary>
        ///  Runs a query command against a metadata source
        /// </summary>
        /// <param name="options">Query options (filters)</param>
        public static void Query(QueryMetadataOptions options)
        {
            var source = Program.LoadMetadataSourceFromOptions(options as IMetadataSourceOptions);
            if (source == null)
            {
                return;
            }

            var repoQuery = new MetadataQuery(source, options);
            repoQuery.Query();
        }

        public static void Status(MetadataSourceStatusOptions options)
        {
            var source = Program.LoadMetadataSourceFromOptions(options as IMetadataSourceOptions);
            if (source == null)
            {
                return;
            }

            Console.WriteLine($"Upstream server   : {source.UpstreamSource.URI}");
            Console.WriteLine($"Checksum          : {source.Checksum}");
            Console.WriteLine($"Categories anchor : {source.CategoriesAnchor}");

            Console.WriteLine($"User name         : {source.UpstreamAccountName}");
            Console.WriteLine($"User guid         : {source.UpstreamAccountGuid}");

            foreach (var filter in source.Filters)
            {
                PrintFilter(filter, source);
            }
        }

        public static void PrintFilter(QueryFilter filter, IMetadataSource metadataSource)
        {
            Console.WriteLine("Filter:");
            Console.WriteLine("    Anchor: {0}", filter.Anchor);
            ConsoleOutput.WriteGreen("    Classifications:");
            if (metadataSource.ClassificationsIndex.Count == filter.ClassificationsFilter.Count)
            {
                ConsoleOutput.WriteGreen("        all");
            }
            else
            {
                foreach (var classificationId in filter.ClassificationsFilter)
                {
                    ConsoleOutput.WriteGreen($"        {classificationId}");
                    ConsoleOutput.WriteGreen($"            {metadataSource.ClassificationsIndex.Values.First(c => c.Identity == classificationId).Title}");
                }
            }

            ConsoleOutput.WriteCyan("    Products:");
            if (metadataSource.ProductsIndex.Count == filter.ProductsFilter.Count)
            {
                Console.WriteLine("        all");
            }
            else
            {
                foreach (var productId in filter.ProductsFilter)
                {
                    ConsoleOutput.WriteCyan($"        {productId}");
                    ConsoleOutput.WriteCyan($"            {metadataSource.ProductsIndex.Values.First(c => c.Identity == productId).Title}");
                }
            }
        }

        private MetadataQuery(IMetadataSource metadataSource, QueryMetadataOptions options)
        {
            MetadataSource = metadataSource;
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
            else if (Options.Files)
            {
                PrintFiles(Options);
            }
        }

        /// <summary>
        /// Print files from the store
        /// </summary>
        /// <param name="options">Print options, including filters</param>
        public void PrintFiles(QueryMetadataOptions options)
        {
            if (!string.IsNullOrEmpty(options.FileHash))
            {
                var hashBytes = new byte[options.FileHash.Length / 2];
                for (var i = 0; i < hashBytes.Length; i++)
                {
                    hashBytes[i] = System.Convert.ToByte(options.FileHash.Substring(i * 2, 2), 16);
                }

                var hashBase64 = Convert.ToBase64String(hashBytes);

                if (!MetadataSource.HasFile(hashBase64))
                {
                    ConsoleOutput.WriteRed($"Cannot find file hash {options.FileHash}");
                }

                var file = MetadataSource.GetFile(hashBase64);

                foreach (var update in MetadataSource.UpdatesIndex.Values)
                {
                    if (update.HasFiles)
                    {
                        foreach (var updateFile in update.Files)
                        {
                            if (updateFile.Digests.Any(d => d.HexString.Equals(options.FileHash, StringComparison.OrdinalIgnoreCase)))
                            {
                                PrintUpdateMetadata(update);
                                break;
                            }
                        }
                    }
                }
            }
            else if (options.IdFilter.Count() > 0)
            {
                var idFilter = FilterBuilder.StringGuidsToGuids(options.IdFilter);
                if (idFilter == null)
                {
                    ConsoleOutput.WriteRed("The update ID filter must contain only GUIDs!");
                    return;
                }

                var update = MetadataSource.UpdatesIndex.Values.FirstOrDefault(u => u.Identity.ID.Equals(idFilter[0]));
                if (update == null)
                {
                    ConsoleOutput.WriteRed($"Cannot find update with GUID {idFilter[0]}");
                    return;
                }

                var files = GetAllUpdateFiles(MetadataSource, update);
                PrintFileDetails(files);
            }
        }

        /// <summary>
        /// Gets all files for an update, including files in bundled updates (recursive)
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        public static List<UpdateFile> GetAllUpdateFiles(IMetadataSource source, Update update)
        {
            var filesList = new List<UpdateFile>();
            if (update.HasFiles)
            {
                filesList.AddRange(update.Files);
            }

            if (update.IsBundle)
            {
                foreach(var bundledUpdate in update.BundledUpdates)
                {
                    filesList.AddRange(GetAllUpdateFiles(source, source.UpdatesIndex[bundledUpdate]));
                }
            }

            return filesList;
        }

        /// <summary>
        /// Print updates from the store
        /// </summary>
        /// <param name="options">Print options, including filters</param>
        public void PrintUpdates(QueryMetadataOptions options)
        {
            var filter = FilterBuilder.MetadataFilterFromCommandLine(options as IMetadataFilterOptions);
            if (filter == null)
            {
                return;
            }

            filter.FirstX = options.FirstX;

            // Apply filters specified on the command line
            List<Update> filteredUpdates;

            if (options.Classifications || options.Products || options.Detectoids)
            {
                filteredUpdates = MetadataSource.GetCategories(filter);
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
                filteredUpdates = MetadataSource.GetUpdates(filter);

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
                        PrintUpdateMetadata(update);
                    }
                }

                Console.WriteLine("-----------------------------\r\nMatched {0} entries", filteredUpdates.Count);
            }
        }

        /// <summary>
        /// Print update metadata
        /// </summary>
        /// <param name="update">The update to print metadata for</param>
        void PrintUpdateMetadata(Update update)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\r\nID: {0}", update.Identity.ID);
            Console.ResetColor();
            Console.WriteLine("    Title          : {0}", update.Title);
            Console.WriteLine("    Description    : {0}", update.Description);
            Console.WriteLine("    Is superseded  : {0}", update.IsSuperseded ? $"By {update.SupersedingUpdate.ToString()}" : "no");

            PrintBundleChainRecursive(update, 0);

            if (update.HasProduct || update.HasClassification)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("    Categories:");
                Console.ResetColor();

                if (update.HasProduct)
                {
                    var updateParentIds = update.ProductIds;
                    foreach (var updateParentId in updateParentIds)
                    {
                        Console.WriteLine("        Product ID          : {0}", updateParentId);
                        var matchindProducts = MetadataSource.ProductsIndex.Values.Where(p => p.Identity.ID == updateParentId).ToList();
                        if (matchindProducts.Count > 0)
                        {
                            Console.WriteLine("        Product name        : {0}", matchindProducts[0].Title);
                        }
                    }
                }

                if (update.HasClassification)
                {
                    var classificationIds = update.ClassificationIds;
                    foreach (var classificationId in classificationIds)
                    {
                        Console.WriteLine("        Classification ID   : {0}", classificationId);
                        var matchingClassification = MetadataSource.ClassificationsIndex.Values.Where(p => p.Identity.ID == classificationId).ToList();
                        if (matchingClassification.Count > 0)
                        {
                            Console.WriteLine("        Classification name : {0}", matchingClassification[0].Title);
                        }
                    }
                }
            }

           /* if (update is DriverUpdate)
            {
                PrintDriverMetadata(update as DriverUpdate);

            }
            else if (update is SoftwareUpdate)
            {
                PrintSoftwareUpdateMetadata(update as SoftwareUpdate);
            }*/

            if (update.HasFiles)
            {
                PrintFileDetails(update.Files);
            }

            if (update.IsSupersedingUpdates)
            {
                PrintSupersededUpdates(update);
            }

            if (update.IsBundle)
            {
                PrintBundledUpdates(update);
            }

            if (update.HasPrerequisites)
            {
                PrintPrerequisites(update);
            }
        }

        void PrintBundleChainRecursive(Update update, int recursionIndex)
        {
            const int indentSize = 4;

            if (update.IsBundled)
            {
                foreach(var parentBundleID in update.BundleParent)
                {
                    Console.CursorLeft = indentSize * recursionIndex + indentSize;
                    Console.WriteLine("Bundled in     : {0}", parentBundleID);
                    Console.CursorLeft = indentSize * recursionIndex + indentSize;
                    Console.WriteLine("               : {0}", MetadataSource.GetUpdateTitle(parentBundleID));

                    PrintBundleChainRecursive(MetadataSource.GetUpdate(parentBundleID), recursionIndex + 1);
                }
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

        void PrintFileDetails(List<UpdateFile> files)
        {
            foreach (var file in files)
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

        void PrintSupersededUpdates(Update updateWithSuperseeds)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("    Superseeds:");
            Console.ResetColor();

            foreach (var id in updateWithSuperseeds.SupersededUpdates)
            {
                Console.WriteLine("        ID  : {0}", id);
            }
        }

        void PrintBundledUpdates(Update updateWithBundledUpdates)
        {
            if (updateWithBundledUpdates.BundledUpdates.Count() > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("    Bundled updates:");
                Console.ResetColor();

                foreach (var id in updateWithBundledUpdates.BundledUpdates)
                {
                    Console.WriteLine("        ID        : {0}", id);
                    Console.WriteLine("        Title     : {0}", MetadataSource.GetUpdateTitle(id));
                }
            }
        }

        void PrintPrerequisites(Update updateWithPrereqs)
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

                        Console.WriteLine();
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
