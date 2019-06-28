# Windows Update Services ServerServer Sync Protocol

Provide a C# implementation (.NET Core) of the Microsoft Update Server-Server sync protocol (client side).

Use this library to programmatically browse the Microsoft Update catalog, sync updates locally and run advanced queries on update metadata.

### See [MS-WSUSSS](https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-wsusss/f49f0c3e-a426-4b4b-b401-9aeb2892815c) for the complete technical documentation of the protocol.

## Referencing the library in your project

The easiest way is to use the published NuGet package. In your .NET Core project, add a reference to the [UpdateServices.ServerServerSync NuGet package](https://www.nuget.org/packages/UpdateServices.ServerServerSync). Make sure to check "Include prerelease" if searching for the NuGet package in Visual Studio.

Alternatively, you can compile the code yourself. Visual Studio 2017 with .Net Core development tools is required to build the solution provided at build\server-server-update-sync.sln

## Using the library

The library provides a higher level of abstraction for interacting with an update server than the underlying SOAP. Authentication, server configuration, batched queries, metadata and content cross-linking are handled internally. Update XML metadata is parsed into C# objects for easy access to update properties: prerequisites, bundled updates, files, categories, etc.

#### The UpstreamServerClient object:

`var server = new UpstreamServerClient(Endpoint.Default);`

#### Retrieve update categories:
```
var categories = await server.GetCategories();
categories.Updates.ForEach(cat => Console.WriteLine(cat.Title));
```

#### Retrieve updates:
```
// Create a filter for first product and all classifications for it
var filter = new QueryFilter(
                categories.Updates.OfType<MicrosoftProduct>().Take(1),
                categories.Updates.OfType<Classification>());

// Get updates
var updates = await server.GetUpdates(filter);
updates.Updates.ForEach(update => Console.WriteLine(update.Title));
```

#### Cache data locally
A reference local updates repository implemementation is provided to persist updates locally, run advanced queries against local updates, and export select updates to WSUS. Using a cache also enables delta syncs with the upstream server, which are much faster than baseline syncs.

```
// Create a repository for updates in the current directory
var localRepo = Repository.FromDirectory(Environment.CurrentDirectory);

// Save categories and updates
localRepo.MergeQueryResult(categories);
localRepo.MergeQueryResult(updates);
```

More examples are available [in the wiki pages](https://github.com/microsoft/update-server-server-sync/wiki/Library-examples)

## Using the upsync utility
A command line utility (upsync in src\sync-tool) is provided as a sample for using the library. This tool can be used to browse Microsoft's update catalog, selectively sync updates and export updates to a WSUS server. 

See this wiki entry for information on how to [run the .NET Core upsync tool](https://github.com/microsoft/update-server-server-sync/wiki/Running-the-upsync-tool)

See the wiki for [command line options reference](https://github.com/microsoft/update-server-server-sync/wiki/UpSync-tool-command-line-options)

See the wiki for [samples on running the upsync tool](https://github.com/microsoft/update-server-server-sync/wiki/UpSync-tool-examples)

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
