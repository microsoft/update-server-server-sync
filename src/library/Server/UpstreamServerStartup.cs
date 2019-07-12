// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using System.ServiceModel;
using SoapCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Hosting;
using System.Reflection;

namespace Microsoft.UpdateServices.Server
{
    /// <summary>
    /// Startup class for a ASP.NET Core web service that implements the Server-Server sync protocol.
    /// <para>A web service started with UpstreamServerStartup can act as an upstream server for WSUS.</para>
    /// <para><see cref="Client.UpstreamServerClient"/> can be used to query updates from a web service started with UpstreamServerStartup.</para>
    /// </summary>
    /// <example>
    /// <code>
    /// // Open an existing local repository.
    /// // This sample assumes updates were sync'ed from an upstream server and merged
    /// // into this local repository
    /// var localRepo = FileSystemRepository.Open(Environment.CurrentDirectory);
    /// 
    /// // Create an empty filter; serves all updates in repository
    /// var filter = new RepositoryFilter();
    /// 
    /// // Create and initialize an ASP.NET web host builder
    /// var host = new WebHostBuilder()
    ///    .UseUrls($"http://localhost:24222")
    ///    .UseStartup&lt;Microsoft.UpdateServices.Server.UpstreamServerStartup&gt;()
    ///    .UseKestrel()
    ///    .ConfigureAppConfiguration((hostingContext, config) =>
    ///    {
    ///        config.AddInMemoryCollection(
    ///        new Dictionary&lt;string, string&gt;()
    ///        {
    ///            { "repo-path", Environment.CurrentDirectory },
    ///            { "updates-filter", filter.ToJson() }
    ///        });
    ///    })
    ///    .Build();
    /// 
    /// // Run the ASP.NET service
    /// host.Run();
    /// </code>
    /// </example>
    public class UpstreamServerStartup
    {
        IRepository LocalRepository;

        RepositoryFilter Filter;

        bool MetadataOnly;

        /// <summary>
        /// Initialize a new instance of UpstreamServerStartup.
        /// </summary>
        /// <param name="config">
        /// <para>ASP.NET configuration.</para>
        /// 
        /// <para>Must contain a string entry "repo-path" with the path to the repository to use.</para>
        /// 
        /// <para>Must contain a string entry "updates-filter" with a JSON serialized filter for the repository.</para>
        /// </param>
        public UpstreamServerStartup(IConfiguration config)
        {
            // Get the repository path from the configuration
            var repoPath = config.GetValue<string>("repo-path");

            // Get the filteres to apply to updates; restricts which updates are shared with downstream servers
            Filter = RepositoryFilter.FromJson(config.GetValue<string>("updates-filter"));

            // Load the repository. It must exist
            LocalRepository = FileSystemRepository.Open(repoPath);
            if (LocalRepository == null)
            {
                throw new System.Exception("Cannot find local repository; a local updates repository is required to run an upstream server.");
            }

            // Get the repository path from the configuration
            var metadataOnly = config.GetValue<string>("metadata-only");
            if (!string.IsNullOrEmpty(metadataOnly))
            {
                MetadataOnly = true;
            }
        }

        /// <summary>
        /// Called by ASP.NET to configure services
        /// </summary>
        /// <param name="services">Service collection.
        /// <para>The server-server sync and authentication services are added to this list.</para>
        /// </param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Enable SoapCore; this middleware provides translation services from WCF/SOAP to Asp.net
            services.AddSoapCore();

            // Enable the upstream WCF services
            services.TryAddSingleton<ServerSyncWebService>(new Server.ServerSyncWebService(LocalRepository, Filter, MetadataOnly));
            services.TryAddSingleton<AuthenticationWebService>();

            if (!MetadataOnly)
            {
                // Enable the content controller if serving content
                services.TryAddSingleton<ContentController>(new ContentController(LocalRepository, Filter));

                // Add ContentController from this assembly
                services.AddMvc().AddApplicationPart(Assembly.GetExecutingAssembly()).AddControllersAsServices();
            }
        }

        /// <summary>
        /// Called by ASP.NET to configure a web app's application pipeline
        /// </summary>
        /// <param name="app">Applicatin to configure.
        /// <para>A SOAP endpoint is configured for this app.</para>
        /// </param>
        /// <param name="env">Hosting environment.</param>
        /// <param name="loggerFactory">Logging factory.</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (!MetadataOnly)
            {
                // Create routes for the content controller
                app.UseMvc(routes =>
                {
                    routes.MapRoute(
                        name: "getContent",
                        template: "Content/{directory}/{name}", defaults: new { controller = "Content", action = "GetUpdateContent" });
                });
            }
            

            // Wire the upstream WCF services
            app.UseSoapEndpoint<ServerSyncWebService>("/ServerSyncWebService/ServerSyncWebService.asmx", new BasicHttpBinding(), SoapSerializer.XmlSerializer);
            app.UseSoapEndpoint<AuthenticationWebService>("/DssAuthWebService/DssAuthWebService.asmx", new BasicHttpBinding(), SoapSerializer.XmlSerializer);

            // This entry is for backwards compat with WSUS, which seems to add an extra '/' that does not get routed properly by ASP
            app.UseSoapEndpoint<AuthenticationWebService>("//DssAuthWebService/DssAuthWebService.asmx", new BasicHttpBinding(), SoapSerializer.XmlSerializer);
        }
    }
}
