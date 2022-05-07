// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using SoapCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.UpdateServices.WebServices.ClientSync;
using System.Reflection;
using Microsoft.PackageGraph.Storage;
using Microsoft.PackageGraph.Storage.Local;
using Newtonsoft.Json;
using System.ServiceModel;
using System.Text;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ClientSync
{
    /// <summary>
    /// Startup class for a ASP.NET Core web service that implements the Client-Server sync protocol.
    /// This startup runs a SOAP web service that serves updates to Windows Update clients
    /// <para>This startup configures the required SOAP adapter required for the SOAP based Client-Server sync protocol.</para>
    /// </summary>
    public class UpdateServerStartup
    {
        readonly IMetadataStore MetadataSource;

        readonly Config UpdateServiceConfiguration;

        readonly IContentStore ContentSource = null;

        readonly string ContentRoot;

        /// <summary>
        /// Creates the update server startup using the specified configuration an update metadata store
        /// </summary>
        /// <param name="config">Startup configuration
        /// <para>ASP.NET configuration.</para>
        /// 
        /// <para>Must contain a string entry "metadata-path" with the path to the metadata source to use</para>
        /// 
        /// <para>Must contain a string entry "service-config-json" with the service configuration JSON</para>
        /// 
        /// <para>Can contain a string entry "content-path" with the path to the content store to use if serving update content</para>
        /// </param>
        /// <exception cref="System.Exception">If the content store specified in the configuration cannot be opened</exception>
        public UpdateServerStartup(IConfiguration config)
        {
            var metadataPath = config.GetValue<string>("metadata-path");
            MetadataSource = PackageStore.Open(metadataPath);

            UpdateServiceConfiguration = JsonConvert.DeserializeObject<Config>(config.GetValue<string>("service-config-json"));

            // A file that contains mapping of update identity to a 32 bit, locally assigned revision ID.
            var contentPath = config.GetValue<string>("content-path");
            if (!string.IsNullOrEmpty(contentPath))
            {
                ContentSource = new FileSystemContentStore(contentPath);
                if (ContentSource == null)
                {
                    throw new System.Exception($"Cannot open updates content source from path {contentPath}");
                }

                ContentRoot = config.GetValue<string>("content-http-root");
            }
        }

        /// <summary>
        /// Called by ASP.NET to configure services
        /// </summary>
        /// <param name="services">Service collection.
        /// <para>The client-server sync and simple authentication services are added to this list.</para>
        /// </param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Enable SoapCore; this middleware provides translation services from WCF/SOAP to Asp.net
            services.AddSoapCore();

            // Enable the upstream WCF services
            var clientSyncService = new ClientSyncWebService();
            clientSyncService.SetContentURLBase(ContentSource == null ? null : ContentRoot);
            clientSyncService.SetServiceConfiguration(UpdateServiceConfiguration);
            clientSyncService.SetPackageStore(MetadataSource);

            services.TryAddSingleton<ClientSyncWebService>(clientSyncService);
            services.TryAddSingleton<SimpleAuthenticationWebService>();
            services.TryAddSingleton<ReportingWebService>();

            // Enable the content controller if serving content
            if (ContentSource != null)
            {
                services.AddSingleton(ContentSource as IContentStore);
                // Add ContentController from this assembly
                services.AddMvc().AddApplicationPart(Assembly.GetExecutingAssembly()).AddControllersAsServices();
            }
        }

        /// <summary>
        /// Called by ASP.NET to configure a web app's application pipeline
        /// </summary>
        /// <param name="app">App builder to configure</param>
        /// <param name="env">Hosting environment</param>
        /// <param name="loggerFactory">Logging factory</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            if (ContentSource != null)
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllerRoute(
                       name: "getContent",
                       pattern: "microsoftupdate/content/{contentHash}",
                       defaults: new { controller = "MicrosoftUpdateContent", action = "GetMicrosoftUpdateContent" });
                });
            }

            // Wire the upstream WCF services
            app.UseSoapEndpoint<ClientSyncWebService>(
                "/ClientWebService/client.asmx", 
                new SoapEncoderOptions() { WriteEncoding = new UTF8Encoding(false) },
                SoapSerializer.XmlSerializer);

            app.UseSoapEndpoint<SimpleAuthenticationWebService>(
                "/SimpleAuthWebService/SimpleAuth.asmx",
                new SoapEncoderOptions() { WriteEncoding = new UTF8Encoding(false) },
                SoapSerializer.XmlSerializer);
        }
    }
}
