// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Content;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Handlers;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Prerequisites;
using Microsoft.PackageGraph.Storage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PackageGraph.Utilitites.Upsync
{
    class MicrosoftUpdateMetadata
    {
        /// <summary>
        /// Print updates from the store
        /// </summary>
        /// <param name="options">Print options, including filters</param>
        public static void PrintMicrosoftUpdatePackages(QueryMetadataOptions options, IMetadataStore metadataStore, PackageType packageType)
        {
            var filter = FilterBuilder.MicrosoftUpdateFilterFromCommandLine(options as IMetadataFilterOptions);
            if (filter == null)
            {
                return;
            }

            // Apply filters specified on the command line
            IEnumerable<MicrosoftUpdatePackage> packagesList;
            var allCategories = new List<MicrosoftUpdatePackage>();
            allCategories.AddRange(metadataStore.OfType<ClassificationCategory>());
            allCategories.AddRange(metadataStore.OfType<ProductCategory>());
            allCategories.AddRange(metadataStore.OfType<DetectoidCategory>());

            if (packageType == PackageType.MicrosoftUpdateClassification)
            {
                packagesList = filter.Apply<ClassificationCategory>(metadataStore);
            }
            else if (packageType == PackageType.MicrosoftUpdateProduct)
            {
                packagesList = filter.Apply<ProductCategory>(metadataStore);
            }
            else if (packageType == PackageType.MicrosoftUpdateDetectoid)
            {
                packagesList = filter.Apply<DetectoidCategory>(metadataStore);
            }
            else if (packageType == PackageType.MicrosoftUpdateUpdate)
            {
                packagesList = filter.Apply<SoftwareUpdate>(metadataStore);
            }
            else if (packageType == PackageType.MicrosoftUpdateDriver)
            {
                packagesList = filter.Apply<DriverUpdate>(metadataStore);
            }
            else
            {
                packagesList = filter.Apply<MicrosoftUpdatePackage>(metadataStore);
            }

            var categoriesLookup = allCategories.ToLookup(package => package.Id.ID);

            Console.Write("\r\nQuery results:\r\n-----------------------------");
            int counter = 0;

            var allUpdatesLookup = metadataStore.OfType<MicrosoftUpdatePackage>().ToLookup(package => package.Id.ID);
            foreach (var update in packagesList)
            {
                counter++;

                if (!options.CountOnly)
                {
                    PrintMicrosoftUpdateMetadata(update, metadataStore, categoriesLookup, allUpdatesLookup);
                }
            }

            Console.WriteLine("-----------------------------");
            Console.WriteLine($"Returned {counter} entries");
        }

        /// <summary>
        /// Print update metadata
        /// </summary>
        /// <param name="update">The update to print metadata for</param>
        static void PrintMicrosoftUpdateMetadata(MicrosoftUpdatePackage update, IMetadataStore source, ILookup<Guid, MicrosoftUpdatePackage> categoriesLookup, ILookup<Guid, MicrosoftUpdatePackage> updatesLookup)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            ConsoleOutput.WriteGreen($"ID      : {update.Id}");
            ConsoleOutput.WriteGreen($"Open ID : {update.Id.OpenIdHex}");
            Console.ResetColor();
            Console.WriteLine("    Title          : {0}", update.Title);
            Console.WriteLine("    Description    : {0}", update.Description);

            var categories = update.GetCategories(categoriesLookup);
            if (categories != null)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("    Categories:");
                Console.ResetColor();
                foreach (var category in categories.OfType<ClassificationCategory>())
                {
                    Console.WriteLine("        Classification          : {0}", category.Id);
                    Console.WriteLine("                                  {0}", category.Title);
                }

                foreach (var product in categories.OfType<ProductCategory>())
                {
                    Console.WriteLine("        Product                 : {0}", product.Id);
                    Console.WriteLine("                                  {0}", product.Title);
                }
            }

            if (update.Handler != null)
            {
                PrintHandlerMetadata(update.Handler);
            }

            if (update is DriverUpdate driverUpdate)
            {
                PrintDriverMetadata(driverUpdate);

            }
            else if (update is SoftwareUpdate softwareUpdate)
            {
                PrintSoftwareUpdateMetadata(softwareUpdate, source, updatesLookup);
            }

            if (update.Files != null)
            {
                PrintFileDetails(update.Files.Cast<UpdateFile>());
            }

            PrintPrerequisites(update, updatesLookup);
        }

        static void PrintHandlerMetadata(HandlerMetadata handler)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine( "    Handler :");
            Console.ResetColor();

            Type handlerType = handler.GetType();
            var handlerProperties = handlerType.GetProperties();
            foreach(var property in handlerProperties)
            {
                Console.WriteLine("{0,8}{1,-20} : {2}", " ", property.Name, property.GetValue(handler, null));
            }

            if (handler is CommandLineHandler commandLineHandler)
            {
                foreach (var returnCode in commandLineHandler.ReturnCodes)
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine("{0,8}{1,-20} :", " ", "ReturnCode");
                    Console.ResetColor();
                    Type returnCodeType = returnCode.GetType();
                    var returnCodeProperties = returnCodeType.GetProperties();
                    foreach (var property in returnCodeProperties)
                    {
                        Console.WriteLine("{0,12}{1,-20} : {2}", " ", property.Name, property.GetValue(returnCode, null));
                    }
                }
            }
        }

        static void PrintBundleChainRecursive(SoftwareUpdate softwareUpdate, IMetadataStore source, int recursionIndex)
        {
            const int indentSize = 4;

            if (softwareUpdate.BundledWithUpdates != null)
            {
                foreach (var parentBundleID in softwareUpdate.BundledWithUpdates)
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.CursorLeft = indentSize * recursionIndex + indentSize;
                    Console.WriteLine($"    Bundled with   :");
                    Console.ResetColor();
                    Console.CursorLeft = indentSize * recursionIndex + indentSize + 8;
                    Console.WriteLine(parentBundleID);
                    Console.CursorLeft = indentSize * recursionIndex + indentSize + 8;

                    if (source.ContainsPackage(parentBundleID))
                    {
                        var parentBundle = source.GetPackage(parentBundleID);
                        Console.WriteLine(parentBundle.Title);
                        PrintBundleChainRecursive(parentBundle as SoftwareUpdate, source, recursionIndex + 1);

                        Console.WriteLine();
                    }
                }
            }
        }

        static void PrintDriverMetadata(DriverUpdate driverUpdate)
        {
            var driverMetadataList = driverUpdate.GetDriverMetadata();
            if (driverMetadataList != null)
            {
                foreach (var driverMetadata in driverMetadataList)
                {
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.WriteLine("    Driver metadata:");
                    Console.ResetColor();
                    Console.WriteLine("        HardwareId : {0}", driverMetadata.HardwareID);
                    Console.WriteLine("        Date       : {0}", driverMetadata.Versioning.Date);
                    Console.WriteLine("        Version    : {0}", driverMetadata.Versioning.VersionString);
                    Console.WriteLine("        Class      : {0}", driverMetadata.Class);

                    if (driverMetadata.FeatureScores.Count > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("        Feature score:");
                        Console.ResetColor();
                        foreach (var featureScore in driverMetadata.FeatureScores)
                        {

                            Console.WriteLine("            Operating System : {0}", featureScore.OperatingSystem);
                            Console.WriteLine("            Score       : {0}", featureScore.Score);
                        }
                    }

                    if (driverMetadata.TargetComputerHardwareId.Count > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("        Target computer hardware id:");
                        Console.ResetColor();
                        foreach (var targetComputerHwId in driverMetadata.TargetComputerHardwareId)
                        {
                            Console.WriteLine("            {0}", targetComputerHwId);
                        }
                    }

                    if (driverMetadata.DistributionComputerHardwareId.Count > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("        Distribution computer hardware id:");
                        Console.ResetColor();
                        foreach (var distributionComputerHardwareId in driverMetadata.DistributionComputerHardwareId)
                        {

                            Console.WriteLine("            {0}", distributionComputerHardwareId);
                        }
                    }
                }
            }
        }

        static void PrintSoftwareUpdateMetadata(SoftwareUpdate softwareUpdate, IMetadataStore source, ILookup<Guid, MicrosoftUpdatePackage> updatesLookup)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("    Software update metadata:");
            Console.ResetColor();
            Console.WriteLine("        Support URL    : {0}", softwareUpdate.SupportUrl);
            Console.WriteLine("        KB Article     : {0}", softwareUpdate.KBArticleId);

            if (softwareUpdate.IsSupersededBy != null)
            {
                Console.WriteLine("        Superseded by");
                foreach (var supersedingUpdate in softwareUpdate.IsSupersededBy.OfType<MicrosoftUpdatePackageIdentity>())
                {
                    Console.WriteLine($"            {supersedingUpdate}");
                    Console.WriteLine($"              {updatesLookup[supersedingUpdate.ID].First().Title}");
                    Console.WriteLine();
                }
            }

            if (softwareUpdate.SupersededUpdates != null)
            {
                Console.WriteLine("        Superseds");
                foreach (var supersededGuid in softwareUpdate.SupersededUpdates)
                {
                    Console.WriteLine($"            {supersededGuid}");
                    if (updatesLookup.Contains(supersededGuid))
                    {
                        Console.WriteLine($"              {updatesLookup[supersededGuid].First().Title}");
                    }
                    else
                    {
                        Console.WriteLine("              No data available!");
                    }

                    Console.WriteLine();
                }
            }

            if (!string.IsNullOrEmpty(softwareUpdate.OsUpgrade))
            {
                Console.WriteLine("        OsUpgrade   : {0}", softwareUpdate.OsUpgrade);
            }

            PrintBundledUpdates(softwareUpdate, source);
        }

        static void PrintFileDetails(IEnumerable<UpdateFile> files)
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

        static void PrintBundledUpdates(SoftwareUpdate softwareUpdate, IMetadataStore source)
        {
            if (softwareUpdate.BundledUpdates != null)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("    Bundled updates:");
                Console.ResetColor();

                foreach (var id in softwareUpdate.BundledUpdates)
                {
                    Console.WriteLine($"        ID        : {id}");
                    if (source.ContainsPackage(id))
                    {
                        Console.WriteLine($"        Title     : {source.GetPackage(id).Title}");
                    }
                }
            }

            PrintBundleChainRecursive(softwareUpdate, source, 0);
        }

        static void PrintPrerequisites(MicrosoftUpdatePackage update, ILookup<Guid, MicrosoftUpdatePackage> updatesLookup)
        {
            if (update.Prerequisites != null && update.Prerequisites.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine("    Prerequisites:");
                Console.ResetColor();

                foreach (var prereq in update.Prerequisites)
                {
                    if (prereq is AtLeastOne atLeastOneOf)
                    {
                        Console.WriteLine("        At least one of:");
                        Console.WriteLine("            ******************************");
                        foreach (var subPrereq in atLeastOneOf.Simple)
                        {
                            if (atLeastOneOf.IsCategory)
                            {
                                Console.WriteLine("            *ID          : {0} (is category)", subPrereq.UpdateId);
                            }
                            else
                            {
                                Console.WriteLine("            *ID          : {0}", subPrereq.UpdateId);
                            }

                            if (updatesLookup.Contains(subPrereq.UpdateId))
                            {
                                Console.WriteLine("            *              {0}", updatesLookup[subPrereq.UpdateId].First().Title);
                            }
                        }

                        Console.WriteLine("            ******************************");
                    }
                    else if (prereq is Simple simple)
                    {
                        Console.WriteLine("            ID          : {0}", simple.UpdateId);
                        if (updatesLookup.Contains(simple.UpdateId))
                        {
                            Console.WriteLine("                          {0}", updatesLookup[simple.UpdateId].First().Title);
                        }
                    }
                }
            }
        }
    }
}
