// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ServerSync;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.PackageGraph.Utilitites.Upsync
{
    /// <summary>
    /// Runs a service that provides updates to downstream updates servers (WSUS)
    /// Requires a local source of update metadata. All or a subset of updates from the local source can be served.
    /// </summary>
    class UpstreamServer
    {
        public static void Run(RunUpstreamServerOptions options)
        {
            var serviceConfigurationJson = File.ReadAllText("./upstream-server-config.json");
            var bindEndpoint = "localhost";
            var bindPort = 40080;
            var metadataPath = "./store";
            var contentPath = "./content";

var host = new WebHostBuilder()
    // Bind to a specific IP address or HOST NAME
    .UseUrls($"http://{bindEndpoint}:{bindPort}")
    // Use the sample startup provided. Use the sample startup as a starting point for a custom startup
    .UseStartup<UpstreamServerStartup>()
    .UseKestrel()
    .ConfigureKestrel((context, opts) => { })
    .ConfigureLogging((hostingContext, logging) =>
    {
        logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
        logging.AddConsole();
        logging.AddDebug();
        logging.AddEventSourceLogger();
    })
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        // Pass along configuration to the startup.
        var configDictionary = new Dictionary<string, string>()
        {
            // Path to local metadata store
            { "metadata-path", metadataPath },
            // Path to local update content store
            { "content-path", contentPath },
            // Path to the WSUS configuration file
            { "service-config-json", serviceConfigurationJson }
        };

        config.AddInMemoryCollection(configDictionary);
    })
    .Build();

            host.Run();

        }
    }
}
