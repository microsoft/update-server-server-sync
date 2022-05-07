This sample runs a a MUv6 update server. 

The update server delivers updates to Windows Update clients that have been configured through group policy to connect to it. For more information, see [Specify intranet Microsoft update service location](https://docs.microsoft.com/en-us/windows/deployment/update/waas-wu-settings#specify-intranet-microsoft-update-service-location)

Fetch some Windows updates before running this sample. A good starting point is to sync classification "Security Updates" for the "Windows 11" product.

```
// Load the default configuration for the MUv6 server
// The default configuration JSON file is stored with the upsync tool code
var serviceConfigurationJson = File.ReadAllText("./update-server-config.json");
var metadataPath = "./store";
var contentPath = "./content";
// Do not use localhost; bind to an endpoint accessible
// to the devices you want to update.
var bindEndpoint = "localhost;
var bindPort = 40080;

var host = new WebHostBuilder()
    // Bind to an IP address of HOST NAME
    // Use the same endpoint information when configuring update group policy on the devices
    // that should get updates from this server
    .UseUrls($"http://{bindEndpoint}:{bindPort}")
    // Use the sample MUv6 server startup. Use the sample startup code as a starting 
    // point for customization
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
            { 
                "metadata-path", 
                metadataPath 
            },

            // Local path of update content
            { 
                "content-path", 
                contentPath 
            },

            // The MUv6 service configuration to use. 
            // Windows Update clients download this configuration.
            { 
                "service-config-json", 
                serviceConfigurationJson 
            },

            // The URL where update content will be served from.
            // This path match with the path used in ASP.NETCore to serve content
            // In this case, we use the sample MicrosoftUpdateContentController, 
            // which serves content from /microsoftupdate/content
            { 
                "content-http-root", $"http://{bindEndpoint}:{bindPort}/microsoftupdate/content" 
            },
        };

        config.AddInMemoryCollection(configDictionary);
    })
    .Build();

host.Run();
```
