// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using CommandLine;
using Microsoft.UpdateServices.Storage;

namespace Microsoft.UpdateServices.Tools.UpdateRepo
{
    class Program
    {
        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<
                MetadataSourceStatusOptions,
                FetchUpdatesOptions,
                QueryMetadataOptions,
                MetadataSourceExportOptions,
                ContentSyncOptions,
                RunUpstreamServerOptions,
                MergeQueryResultOptions,
                FetchCategoriesOptions,
                FetchConfigurationOptions>(args)
                .WithParsed<FetchUpdatesOptions>(opts => MetadataSync.FetchUpdates(opts))
                .WithParsed<FetchConfigurationOptions>(opts => MetadataSync.FetchConfiguration(opts))
                .WithParsed<FetchCategoriesOptions>(opts => MetadataSync.FetchCategories(opts))
                .WithParsed<QueryMetadataOptions>(opts => MetadataQuery.Query(opts))
                .WithParsed<MetadataSourceExportOptions>(opts => UpdateMetadataExport.ExportUpdates(opts))
                .WithParsed<ContentSyncOptions>(opts => ContentSync.Run(opts))
                .WithParsed<MetadataSourceStatusOptions>(opts => MetadataQuery.Status(opts))
                .WithParsed<RunUpstreamServerOptions>(opts => UpstreamServer.Run(opts))
                .WithParsed<MergeQueryResultOptions>(opts => MetadataSync.MergeQueryResult(opts))
                .WithNotParsed(failed => Console.WriteLine("Error"));
        }

        /// <summary>
        /// Opens an updates metadata source the path specified on the command line
        /// </summary>
        /// <param name="sourceOptions">The command line switch that contains the path</param>
        /// <returns>A open updates metadata source</returns>
        public static IMetadataSource LoadMetadataSourceFromOptions(IMetadataSourceOptions sourceOptions)
        {
            IMetadataSource source = null;
            Console.Write("Opening update metadata source file... ");
            try
            {
                source = CompressedMetadataStore.Open(sourceOptions.MetadataSourcePath);
                ConsoleOutput.WriteGreen("Done!");
            }
            catch(Exception ex)
            {
                Console.WriteLine();
                ConsoleOutput.WriteRed("Cannot open the query result file:");
                ConsoleOutput.WriteRed(ex.Message);
            }
            

            return source;
        }

        static Dictionary<OperationType, string> OperationsMessages = new Dictionary<OperationType, string>()
        {
            { OperationType.ProcessSupersedeDataStart, "Processing superseding data"},
            { OperationType.ProcessSupersedeDataEnd, "Processing superseding data"},
            { OperationType.PrerequisiteGraphUpdateEnd, "Updating prerequisites graph"},
            { OperationType.PrerequisiteGraphUpdateProgress, "Updating prerequisites graph"},
            { OperationType.PrerequisiteGraphUpdateStart, "Updating prerequisites graph"},
            { OperationType.IndexingTitlesEnd, "Indexing update titles"},
            { OperationType.IndexingTitlesStart, "Indexing update titles"},
            { OperationType.IndexingCategoriesStart, "Indexing categories"},
            { OperationType.IndexingCategoriesProgress, "Indexing categories"},
            { OperationType.IndexingCategoriesEnd, "Indexing categories"},
            { OperationType.HashMetadataStart, "Creating checksum"},
            { OperationType.HashMetadataEnd, "Creating checksum"},
            { OperationType.IndexingBundlesStart, "Indexing bundles"},
            { OperationType.IndexingBundlesEnd, "Indexing bundles"},
            { OperationType.IndexingFilesStart, "Indexing files"},
            { OperationType.IndexingFilesEnd, "Indexing files"}
        };

        public static void MetadataSourceOperationProgressHandler(object sender, OperationProgress e)
        {
            if (!OperationsMessages.TryGetValue(e.CurrentOperation, out string operationMessage))
            {
                return;
            }

            switch (e.CurrentOperation)
            {
                case OperationType.IndexingFilesStart:
                case OperationType.IndexingBundlesStart:
                case OperationType.IndexingCategoriesStart:
                case OperationType.HashMetadataStart:
                case OperationType.PrerequisiteGraphUpdateStart:
                case OperationType.ProcessSupersedeDataStart:
                case OperationType.IndexingTitlesStart:
                case OperationType.IndexingPrerequisitesStart:
                    Console.CursorLeft = 0;
                    Console.Write($"{operationMessage} [000.0%]");
                    break;

                case OperationType.IndexingFilesEnd:
                case OperationType.IndexingBundlesEnd:
                case OperationType.IndexingCategoriesEnd:
                case OperationType.PrerequisiteGraphUpdateEnd:
                case OperationType.ProcessSupersedeDataEnd:
                case OperationType.HashMetadataEnd:
                case OperationType.IndexingTitlesEnd:
                case OperationType.IndexingPrerequisitesEnd:
                    Console.CursorLeft = 0;
                    Console.Write($"{operationMessage}  [100.00%] ");
                    ConsoleOutput.WriteGreen(" Done!");
                    break;

                case OperationType.IndexingCategoriesProgress:
                case OperationType.PrerequisiteGraphUpdateProgress:
                    Console.CursorLeft = 0;
                    Console.Write("{1} [{0:000.00}%]", e.PercentDone, operationMessage);
                    break;
            }
        }
    }
}
