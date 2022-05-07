// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Drivers;
using Microsoft.PackageGraph.MicrosoftUpdate.Source;
using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Storage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PackageGraph.Utilitites.Upsync
{
    /// <summary>
    /// Implements query and management operations on a local updates metadata source
    /// </summary>
    class MetadataQuery
    {
        private readonly IMetadataStore MetadataStore;
        private readonly QueryMetadataOptions Options;

        /// <summary>
        ///  Runs a query command against a metadata source
        /// </summary>
        /// <param name="options">Query options (filters)</param>
        public static void Query(QueryMetadataOptions options)
        {
            var source = MetadataStoreCreator.OpenFromOptions(options as IMetadataStoreOptions);
            if (source == null)
            {
                return;
            }

            var repoQuery = new MetadataQuery(source, options);
            repoQuery.Query();
        }

        public static void MatchDrivers(MatchDriverOptions options)
        {
            var source = MetadataStoreCreator.OpenFromOptions(options as IMetadataStoreOptions);
            if (source == null)
            {
                return;
            }

            using (source)
            {
                List<Guid> computerHardwareIds = FilterBuilder.StringGuidsToGuids(options.ComputerHardwareIds);
                if (computerHardwareIds == null)
                {
                    ConsoleOutput.WriteRed($"The computer hardware ID must be a GUID");
                    return;
                }

                var prerequisites = FilterBuilder.StringGuidsToGuids(options.InstalledPrerequisites);
                if (prerequisites == null)
                {
                    ConsoleOutput.WriteRed($"Prerequisites must be a list of GUIDs separated by '+'");
                    return;
                }

                DriverUpdateMatching driverMatching = DriverUpdateMatching.FromPackageSource(source);

                var driverMatch = driverMatching.MatchDriver(options.HardwareIds, computerHardwareIds, prerequisites);

                if (driverMatch != null)
                {
                    Console.WriteLine("Matched driver update:");
                    ConsoleOutput.WriteGreen($"    ID      : {driverMatch.Driver.Id.ID}");
                    ConsoleOutput.WriteGreen($"    Open ID : {driverMatch.Driver.Id.OpenIdHex}");
                    Console.WriteLine($"    Matched hardware id          : {driverMatch.MatchedHardwareId}");
                    Console.WriteLine($"    Driver version               : [Date {driverMatch.MatchedVersion.Date} Version {driverMatch.MatchedVersion.VersionString}]");

                    if (driverMatch.MatchedComputerHardwareId.HasValue)
                    {
                        Console.WriteLine($"    Matched computer hardware ID : {driverMatch.MatchedComputerHardwareId.Value}");
                    }

                    if (driverMatch.MatchedFeatureScore != null)
                    {
                        Console.WriteLine($"    Driver feature score         : [OS {driverMatch.MatchedFeatureScore.OperatingSystem}, Score  {driverMatch.MatchedFeatureScore.Score}]");
                    }
                }
                else
                {
                    ConsoleOutput.WriteRed("No match found");
                }
            }
        }

        public static void Status(MetadataSourceStatusOptions options)
        {
            var source = MetadataStoreCreator.OpenFromOptions(options as IMetadataStoreOptions);
            if (source == null)
            {
                return;
            }

            using (source)
            {
                var packageCount = source.Cast<IPackage>().Count();
                Console.WriteLine($"Package count            : {packageCount}");
                Console.WriteLine($"Package ID indexed       : {source is IMetadataStore}");
                Console.WriteLine($"    Reindexing required  : {source.IsReindexingRequired}");
            }
        }

        public static void PrintFilter(UpstreamSourceFilter filter, IMetadataStore metadataSource)
        {
            Console.WriteLine("Filter:");
            
            var allClassifications = metadataSource.OfType<ClassificationCategory>();
            var allProducts = metadataSource.OfType<ProductCategory>();

            bool allClassificationsIncluded = false;
            if (allClassifications.Count() == filter.ClassificationsFilter.Count)
            {
                allClassificationsIncluded = true;
                foreach (var classificationId in filter.ClassificationsFilter)
                {
                    if (!allClassifications.Any(c => c.Id.ID == classificationId))
                    {
                        allClassificationsIncluded = false;
                        break;
                    }
                }
            }

            ConsoleOutput.WriteGreen("    Classifications:");
            if (allClassificationsIncluded)
            {
                ConsoleOutput.WriteGreen($"        all");
            }
            else
            {
                foreach (var classificationId in filter.ClassificationsFilter)
                {
                    ConsoleOutput.WriteGreen($"        {classificationId}");
                    ConsoleOutput.WriteGreen($"            {allClassifications.FirstOrDefault(c => c.Id.ID == classificationId).Title}");
                }
            }

            ConsoleOutput.WriteCyan("    Products:");
            bool allProductsIncluded = false;
            if (allProducts.Count() == filter.ProductsFilter.Count)
            {
                allProductsIncluded = true;
                foreach (var productId in filter.ProductsFilter)
                {
                    if (!allProducts.Any(c => c.Id.ID == productId))
                    {
                        allProductsIncluded = false;
                        break;
                    }
                }
            }

            if (allProductsIncluded)
            {
                Console.WriteLine("        all");
            }
            else
            {
                foreach (var productId in filter.ProductsFilter)
                {
                    ConsoleOutput.WriteCyan($"        {productId}");
                    ConsoleOutput.WriteCyan($"            {allProducts.FirstOrDefault(c => c.Id.ID == productId).Title}");
                }
            }
        }

        private MetadataQuery(IMetadataStore metadataSource, QueryMetadataOptions options)
        {
            MetadataStore = metadataSource;
            Options = options;
        }

        private static readonly Dictionary<string, PackageType> SupportedPackages = new()
        {
            { "microsoft-classification", PackageType.MicrosoftUpdateClassification },
            { "microsoft-product", PackageType.MicrosoftUpdateProduct },
            { "microsoft-update", PackageType.MicrosoftUpdateUpdate },
            { "microsoft-driver", PackageType.MicrosoftUpdateDriver },
            { "microsoft-detectoid", PackageType.MicrosoftUpdateDetectoid }
        };

        private void Query()
        {
            if (SupportedPackages.ContainsKey(Options.PackageType))
            {
                var packageType = SupportedPackages[Options.PackageType];
                switch (packageType)
                {
                    case PackageType.MicrosoftUpdateClassification:
                    case PackageType.MicrosoftUpdateProduct:
                    case PackageType.MicrosoftUpdateUpdate:
                    case PackageType.MicrosoftUpdateDriver:
                    case PackageType.MicrosoftUpdateDetectoid:
                        MicrosoftUpdateMetadata.PrintMicrosoftUpdatePackages(Options, MetadataStore, packageType);
                        break;

                    default:
                        ConsoleOutput.WriteRed("Unknown package type");
                        break;
                }
            }
            else
            {
                ConsoleOutput.WriteRed("Unsupported package type. Supported package types are:");
                SupportedPackages.Keys.ToList().ForEach(packageTypeName => Console.WriteLine(packageTypeName));
                return;
            }
        }
    }
}
