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
using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using System.IO;
using Microsoft.UpdateServices.Metadata;

namespace Microsoft.UpdateServices.Server
{
    /// <summary>
    /// Startup class for a ASP.NET Core web service that implements the Server-Server sync protocol.
    /// <para>A web service started with UpstreamServerStartup can act as an upstream server for WSUS.</para>
    /// <para><see cref="Client.UpstreamServerClient"/> can be used to query updates from a web service started with UpstreamServerStartup.</para>
    /// </summary>
    public class UpstreamServerStartup
    {
        IMetadataSource LocalMetadataSource;
        IUpdateContentSource LocalContentSource;
        ServerSyncConfigData ServiceConfiguration;

        MetadataFilter Filter;

        /// <summary>
        /// Initialize a new instance of UpstreamServerStartup.
        /// </summary>
        /// <param name="config">
        /// <para>ASP.NET configuration.</para>
        /// 
        /// <para>Must contain a string entry "metadata-path" with the path to the metadata source to use</para>
        /// 
        /// <para>Must contain a string entry "updates-filter" with a JSON serialized updates metadata filter</para>
        /// 
        /// <para>Must contain a string entry "service-config-path" with the path to the service configuration JSON</para>
        /// 
        /// <para>Can contain a string entry "content-path" with the path to the content store to use if serving update content</para>
        /// </param>
        public UpstreamServerStartup(IConfiguration config)
        {
            // Get the metadata source path from the configuration
            var sourcePath = config.GetValue<string>("metadata-path");

            var contentPath = config.GetValue<string>("content-path");

            // Get the filteres to apply to updates; restricts which updates are shared with downstream servers
            Filter = MetadataFilter.FromJson(config.GetValue<string>("updates-filter"));

            // Open the updates metadata source. It must exist
            LocalMetadataSource = CompressedMetadataStore.Open(sourcePath);
            if (LocalMetadataSource == null)
            {
                throw new System.Exception($"Cannot open the specified metadata source at {sourcePath}");
            }

            var serviceConfigPath = config.GetValue<string>("service-config-path");
            ServiceConfiguration = JsonConvert.DeserializeObject<ServerSyncConfigData>(
                File.OpenText(serviceConfigPath).ReadToEnd());

            if (!string.IsNullOrEmpty(contentPath))
            {
                LocalContentSource = new FileSystemContentStore(contentPath);
                ServiceConfiguration.CatalogOnlySync = false;
            }
            else
            {
                ServiceConfiguration.CatalogOnlySync = true;
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
            services.TryAddSingleton<ServerSyncWebService>(new Server.ServerSyncWebService(LocalMetadataSource, Filter, ServiceConfiguration));
            services.TryAddSingleton<AuthenticationWebService>();
            services.TryAddSingleton<ReportingWebService>();

            if (LocalContentSource != null)
            {
                // Enable the content controller if serving content
                services.TryAddSingleton<ContentController>(new ContentController(LocalMetadataSource, LocalContentSource, Filter));

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
            if (LocalContentSource != null)
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
            app.UseSoapEndpoint<ReportingWebService>("/ReportingWebService/ReportingWebService.asmx", new BasicHttpBinding(), SoapSerializer.XmlSerializer);


            // This entry is for backwards compat with WSUS, which seems to add an extra '/' that does not get routed properly by ASP
            app.UseSoapEndpoint<AuthenticationWebService>("//DssAuthWebService/DssAuthWebService.asmx", new BasicHttpBinding(), SoapSerializer.XmlSerializer);
        }
    }
}
