// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Storage;
using Microsoft.UpdateServices.Metadata;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UpdateServices.Tools.UpdateRepo
{
    class RepositoryExport
    {
        /// <summary>
        /// Export update metadata from a repository
        /// </summary>
        /// <param name="options">Export options</param>
        public static void ExportUpdates(RepositoryExportOptions options)
        {
            var localRepo = Program.LoadRepositoryFromOptions(options as IRepositoryPathOption);
            if (localRepo == null)
            {
                return;
            }

            var filter = MetadataFilter.RepositoryFilterFromCommandLineFilter(options as IUpdatesFilter);
            if (filter == null)
            {
                return;
            }

            localRepo.RepositoryOperationProgress += LocalRepo_RepositoryOperationProgress;
            localRepo.Export(filter, options.ExportFile, RepoExportFormat.WSUS_2016);
        }

        /// <summary>
        /// Handles progress notifications from a local repository
        /// Prints progress information to the console
        /// </summary>
        /// <param name="sender">The local repository that is executing a long running operation</param>
        /// <param name="e">Progress information</param>
        private static void LocalRepo_RepositoryOperationProgress(object sender, OperationProgress e)
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
