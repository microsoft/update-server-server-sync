// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using SoapCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Hosting;
using System.Reflection;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using System.IO;
using Microsoft.PackageGraph.Storage;
using Microsoft.PackageGraph.Storage.Local;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ServerSync
{
    /// <summary>
    /// Startup class for a ASP.NET Core web service that implements the Server-Server sync protocol.
    /// <para>A web service started with UpstreamServerStartup can act as an upstream server for WSUS.</para>
    /// <para>This startup configures the required SOAP adapter required for the SOAP based Server-Server sync protocol.</para>
    /// </summary>
    public class UpstreamServerStartup
    {
        readonly IMetadataStore PackageSource;
        readonly IContentStore LocalContentSource;
        readonly ServerSyncConfigData ServiceConfiguration;

        /// <summary>
        /// Initialize a new instance of UpstreamServerStartup.
        /// </summary>
        /// <param name="config">
        /// <para>ASP.NET configuration.</para>
        /// 
        /// <para>Must contain a string entry "metadata-path" with the path to the metadata source to use</para>
        /// 
        /// <para>Must contain a string entry "service-config" with the service configuration JSON</para>
        /// 
        /// <para>Can contain a string entry "content-path" with the path to the content store to use if serving update content</para>
        /// </param>
        public UpstreamServerStartup(IConfiguration config)
        {
            var contentPath = config.GetValue<string>("content-path");
            var metadataPath = config.GetValue<string>("metadata-path");

            PackageSource = PackageStore.Open(metadataPath);

            var serviceConfigJson = config.GetValue<string>("service-config-json");
            ServiceConfiguration = JsonConvert.DeserializeObject<ServerSyncConfigData>(serviceConfigJson);

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
            var serverSyncService = new ServerSyncWebService();
            serverSyncService.SetServerConfiguration(ServiceConfiguration);
            serverSyncService.SetPackageStore(PackageSource);
            services.TryAddSingleton<ServerSyncWebService>(serverSyncService);
            services.TryAddSingleton<AuthenticationWebService>();
            services.TryAddSingleton<ReportingWebService>();

            if (LocalContentSource != null)
            {
                // Enable the content controller if serving content
                // Add your content controller here

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
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                   name: "getContent",
                   pattern: "microsoftupdate/content/{contentHash}",
                   defaults: new { controller = "MicrosoftUpdateContent", action = "GetMicrosoftUpdateContent" });
            });

            // Wire the upstream WCF services
            app.UseSoapEndpoint<ServerSyncWebService>("/ServerSyncWebService/ServerSyncWebService.asmx", new SoapEncoderOptions(), SoapSerializer.XmlSerializer);
            app.UseSoapEndpoint<AuthenticationWebService>("/DssAuthWebService/DssAuthWebService.asmx", new SoapEncoderOptions(), SoapSerializer.XmlSerializer);
            app.UseSoapEndpoint<ReportingWebService>("/ReportingWebService/ReportingWebService.asmx", new SoapEncoderOptions(), SoapSerializer.XmlSerializer);


            // This entry is for backwards compat with WSUS, which seems to add an extra '/' that does not get routed properly by ASP
            app.UseSoapEndpoint<AuthenticationWebService>("//DssAuthWebService/DssAuthWebService.asmx", new SoapEncoderOptions(), SoapSerializer.XmlSerializer);
        }
    }
}
