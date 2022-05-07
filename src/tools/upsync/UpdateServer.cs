// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ClientSync;
using Microsoft.PackageGraph.Storage.Local;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.PackageGraph.Utilitites.Upsync
{
    /// <summary>
    /// Runs a service that provides updates to downstream updates servers (WSUS)
    /// Requires a local source of update metadata. All or a subset of updates from the local source can be served.
    /// </summary>
    class UpdateServer
    {
        public static void Run(RunUpdateServerOptions options)
        {
            // Load the default configuration for the MUv6 server
            var serviceConfigurationJson = File.ReadAllText("./update-server-config.json");
            var metadataPath = options.Path;
            var contentPath = options.ContentSourcePath;
            var bindEndpoint = options.Endpoint;
            var bindPort = options.Port;

            var host = new WebHostBuilder()
                // Bind to an IP address of HOST NAME
                // Use the same endpoint information when configuring update group policy on the devices
                // that should get updates from this server
                .UseUrls($"http://{bindEndpoint}:{bindPort}")
                // Use the sample MUv6 server startup. Use the sample startup code as a starting point for customization
                .UseStartup<UpdateServerStartup>()
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
                    var configDictionary = new Dictionary<string, string>()
                    {
                        // Local path of update metadata
                        { "metadata-path", metadataPath },
                        // Local path of update content
                        { "content-path", contentPath },
                        // The MUv6 service configuration to use. Windows Update clients download this configuration
                        { "service-config-json", serviceConfigurationJson },
                        // The URL where update content will be served from.
                        // This path match with the path used in ASP.NETCore to serve content
                        // In this case, we use the sample MicrosoftUpdateContentController, which serves content from /microsoftupdate/content
                        { "content-http-root", $"http://{bindEndpoint}:{bindPort}/microsoftupdate/content" },
                    };

                    config.AddInMemoryCollection(configDictionary);
                })
                .Build();

            host.Run();
        }   
    }
}
