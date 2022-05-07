// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;

namespace Microsoft.PackageGraph.Utilitites.Upsync
{
    /// <summary>
    /// Implements copying of metadata from a source repository to a destination
    /// </summary>
    class MetadataCopy
    {
        public static void Run(MetadataCopyOptions options)
        {
            using var source = MetadataStoreCreator.OpenFromOptions(
                new MetadataStoreOptions()
                {
                    Alias = options.SourceAlias,
                    StoreConnectionString = options.SourceConnectionString,
                    Path = options.SourcePath,
                    Type = options.SourceType
                });

            if (source == null)
            {
                ConsoleOutput.WriteRed("Failed to open source repository");
                return;
            }

            using var destination = MetadataStoreCreator.CreateFromOptions(
                new MetadataStoreOptions()
                {
                    Alias = options.DestinationAlias,
                    StoreConnectionString = options.DestinationConnectionString,
                    Path = options.DestionationPath,
                    Type = options.DestinationType
                });

            if (destination == null)
            {
                ConsoleOutput.WriteRed("Failed to open or create destination repository");
                return;
            }

            var filter = FilterBuilder.MicrosoftUpdateFilterFromCommandLine(options as IMetadataFilterOptions);
            if (filter == null)
            {
                return;
            }

            Console.WriteLine("Copying packages ...");
            source.MetadataCopyProgress += Program.OnPackageCopyProgress;
            destination.PackagesAddProgress += Program.OnPackageCopyProgress;
            source.CopyTo(destination, filter, new CancellationTokenSource().Token);
        }
    }
}
