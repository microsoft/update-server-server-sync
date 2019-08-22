The following example shows how to run an upstream server that serves updates to a downstream WSUS server from a local repository.

```
// Create an empty filter; serves all updates from the source
var filter = new MetadataFilter();

var host = new WebHostBuilder()
    .UseUrls($"http://localhost:32150")
    .UseStartup<Server.UpstreamServerStartup>()
    .UseKestrel()
    .ConfigureKestrel((context, opts) => { })
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        var configDictionary = new Dictionary<string, string>()
        {
            // Assumes updates were fetched from an upstream server into master.zip
            { "metadata-path", "master.zip" },
            { "content-path", @".\" },
            { "updates-filter", filter.ToJson() },
            // Use UpstreamServerClient.GetServerConfigData to get the official
            // server's configuration as a starting point
            { "service-config-path", "service-config.json" }
        };
        config.AddInMemoryCollection(configDictionary);
    })
    .Build();

// Run the ASP.NET service
host.Run();
```
