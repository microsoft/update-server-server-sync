This code sample shows runs an upstream server. The upstream server is used to seed updates from a local repository to downstream WSUS servers.

Fetch some updates to a local repository before running this sample.

```
// Read the default WSUS configuration
// The default configuration JSON file is stored with the upsync tool code
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
```
