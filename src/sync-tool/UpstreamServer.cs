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
    /// Requires a local repository. All or a subset of updates from the local repository can be served.
    /// </summary>
    class UpstreamServer
    {
        public static void Run(RunUpstreamServerOptions options)
        {
            // Check that the repository exists
            var repoPath = string.IsNullOrEmpty(options.RepositoryPath) ? Environment.CurrentDirectory : options.RepositoryPath;
            if (!FileSystemRepository.RepoExists(repoPath))
            {
                ConsoleOutput.WriteRed($"There is no repository at path {repoPath}");
                return;
            }

            // Create the updates filter configuration
            var filter = MetadataFilter.RepositoryFilterFromCommandLineFilter(options as IUpdatesFilter);

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
                    config.AddInMemoryCollection(
                        new Dictionary<string, string>()
                        {
                            { "repo-path", repoPath },
                            { "updates-filter", filter.ToJson() }
                        });
                })
                .Build();

            host.Run();
        }   
    }
}
