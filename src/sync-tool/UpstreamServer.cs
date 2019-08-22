// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.UpdateServices.Storage;
using System;
using System.Collections.Generic;

namespace Microsoft.UpdateServices.Tools.UpdateRepo
{
    /// <summary>
    /// Runs a service hat provides updates to downstream updates servers (WSUS)
    /// Requires a local source of update metadata. All or a subset of updates from the local source can be served.
    /// </summary>
    class UpstreamServer
    {
        public static void Run(RunUpstreamServerOptions options)
        {
            // Create the updates filter configuration
            var filter = FilterBuilder.MetadataFilterFromCommandLine(options as IMetadataFilterOptions);

            var host = new WebHostBuilder()
                .UseUrls($"http://{options.Endpoint}:{options.Port}")
                .UseStartup<Server.UpstreamServerStartup>()
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
                        { "metadata-path", options.MetadataSourcePath },
                        { "content-path", options.ContentSourcePath },
                        { "updates-filter", filter.ToJson() },
                        { "service-config-path", options.ServiceConfigurationPath }
                    };

                    config.AddInMemoryCollection(configDictionary);
                })
                .Build();

            host.Run();
        }   
    }
}
