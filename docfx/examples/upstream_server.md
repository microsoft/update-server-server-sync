The following example shows how to run an upstream server that serves updates to a downstream WSUS server from a local repository.

```
// Open an existing local repository.
// This sample assumes updates were sync'ed from an upstream server and merged
// into this local repository
var localRepo = FileSystemRepository.Open(Environment.CurrentDirectory);

// Create an empty filter; serves all updates in repository
var filter = new RepositoryFilter();

// Create and initialize an ASP.NET web host builder
var host = new WebHostBuilder()
   .UseUrls($"http://localhost:24222")
   .UseStartup<Microsoft.UpdateServices.Server.UpstreamServerStartup>()
   .UseKestrel()
   .ConfigureAppConfiguration((hostingContext, config) =>
   {
       config.AddInMemoryCollection(
       new Dictionary<string, string>()
       {
           { "repo-path", Environment.CurrentDirectory },
           { "updates-filter", filter.ToJson() }
       });
   })
   .Build();

// Run the ASP.NET service
host.Run();
```
