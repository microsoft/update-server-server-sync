// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;
using System.IO;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;

namespace Microsoft.PackageGraph.Utilitites.Upsync
{
    class UpdateMetadataExport
    {
        /// <summary>
        /// Export filtered or all update metadata from a source
        /// </summary>
        /// <param name="options">Export options</param>
        public static void ExportUpdates(MetadataSourceExportOptions options)
        {
            var source = MetadataStoreCreator.OpenFromOptions(options as IMetadataStoreOptions);
            if (source == null)
            {
                return;
            }

            var filter = FilterBuilder.MicrosoftUpdateFilterFromCommandLine(options as IMetadataFilterOptions);
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

            var exporter = new WsusExporter(source, serverConfig);
            exporter.Export(filter, options.ExportFile);
        }
    }
}
