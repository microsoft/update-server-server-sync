// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.LocalCache;
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
            var localRepo = Program.LoadRepositoryFromOptions(options as IRepositoryPathOption, Repository.RepositoryOpenMode.OpenExisting);
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
                filteredData.AddRange(localRepo.Updates.Index.Values);
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
        /// Handles progress notifications from a local repository
        /// Prints progress information to the console
        /// </summary>
        /// <param name="sender">The local repository that is executing a long running operation</param>
        /// <param name="e">Progress information</param>
        private static void LocalRepo_RepositoryOperationProgress(object sender, RepoOperationProgress e)
        {
            switch (e.CurrentOperation)
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
