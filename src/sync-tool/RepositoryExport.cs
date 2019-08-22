// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Storage;
using Microsoft.UpdateServices.Metadata;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using Microsoft.UpdateServices.WebServices.ServerSync;

namespace Microsoft.UpdateServices.Tools.UpdateRepo
{
    class UpdateMetadataExport
    {
        /// <summary>
        /// Export filtered or all update metadata from a source
        /// </summary>
        /// <param name="options">Export options</param>
        public static void ExportUpdates(MetadataSourceExportOptions options)
        {
            var source = Program.LoadMetadataSourceFromOptions(options as IMetadataSourceOptions);
            if (source == null)
            {
                return;
            }

            var filter = FilterBuilder.MetadataFilterFromCommandLine(options as IMetadataFilterOptions);
            if (filter == null)
            {
                return;
            }

            ServerSyncConfigData serverConfig;
            try
            {
                serverConfig = JsonConvert.DeserializeObject<ServerSyncConfigData>(File.ReadAllText(options.ServerConfigFile));
            }
            catch(Exception)
            {
                ConsoleOutput.WriteRed($"Failed to read server configuration file from {options.ServerConfigFile}");
                return;
            }

            (source as CompressedMetadataStore).ExportProgress += LocalSource_ExportOperationProgress;
            source.Export(filter, options.ExportFile, RepoExportFormat.WSUS_2016, serverConfig);
        }

        /// <summary>
        /// Handles progress notifications from a the local update metadata source
        /// Prints progress information to the console
        /// </summary>
        /// <param name="sender">The metadata source that is executing a long running operation</param>
        /// <param name="e">Progress information</param>
        private static void LocalSource_ExportOperationProgress(object sender, OperationProgress e)
        {
            switch (e.CurrentOperation)
            {
                case OperationType.ExportUpdateXmlBlobStart:
                    Console.Write("Exporting update XML data: 000.00%");
                    break;

                case OperationType.ExportUpdateXmlBlobProgress:
                    Console.CursorLeft = 0;
                    Console.Write("Exporting {0} update(s) and categories XML data: {1:000.00}%", e.Maximum, e.PercentDone);
                    break;

                case OperationType.ExportMetadataStart:
                    Console.Write("Packing metadata...");
                    break;

                case OperationType.CompressExportFileStart:
                    Console.WriteLine("Compressing output export file...\r\n");
                    break;

                case OperationType.ExportMetadataEnd:
                case OperationType.ExportUpdateXmlBlobEnd:
                case OperationType.CompressExportFileEnd:
                    ConsoleOutput.WriteGreen("Done!");
                    break;

            }
        }
    }
}
