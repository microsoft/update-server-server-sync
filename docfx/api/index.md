While this library implements the server-server sync protocol, it provides a higher level of abstraction over the underlying SOAP-based protocol for interacting with a Microsoft upstream update server. Authentication, server configuration, batched queries, metadata and content cross-linking are handled internally. Update XML metadata data is parsed behind the scenes and exposed as native C# properties: prerequisites, bundled updates, files, categories, extended metadata.

#### The [ClientAuthenticator](Microsoft.UpdateServices.Client.ClientAuthenticator.html)
The ClientAuthenticator retrieves an access token to an upstream update server. It is not necessary to use the authenticator on its own, as UpstreamServerClient will perform authentication automatically if an access token is not provided.

#### The [UpstreamServerClient](Microsoft.UpdateServices.Client.UpstreamServerClient.html)
Use UpstreamServerClient to retrieve categories and updates from an upstream update server.

While not required, it is recommended to use a local store - like [FileSystemRepository](Microsoft.UpdateServices.Storage.FileSystemRepository.html) - with UpstreamServerClient to enable caching and delta update sync'ing.

##### Retrieve update categories:
```
var server = new UpstreamServerClient(Endpoint.Default);
var categoriesQueryResult = await server.GetCategories();
categoriesQueryResult.Updates.ForEach(cat => Console.WriteLine(cat.Title));
```

##### Retrieve updates:
```
// Create a filter for first product and all classifications for it
var filter = new QueryFilter(
                categoriesQueryResult.Updates.OfType<Product>().Take(1),
                categoriesQueryResult.Updates.OfType<Classification>());

// Get updates
var updatesQueryResult = await server.GetUpdates(filter);
updatesQueryResult.Updates.ForEach(update => Console.WriteLine(update.Title));
```

#### The [update repository](Microsoft.UpdateServices.Storage.IRepository.html)
An update repository caches updates locally for running queries on update metadata, filtering, exporting updates to WSUS or running your own Upstream Update Server.

A [FileSystemRepository](Microsoft.UpdateServices.Storage.FileSystemRepository.html) caches updates on the local file system. Used together with [UpstreamServerClient](Microsoft.UpdateServices.Client.UpstreamServerClient.html) it enables delta syncs - retrieving only changed or new updates from a baseline.

An update repository syncs updates from a single upstream update server. Multiple repositories can be created to sync from multiple upstream servers.

##### Initialize a repository
```
// Create a repository for updates in the current directory, tracking the 
// official Microsoft upstream update server
var localRepo = FileSystemRepository.Init(Environment.CurrentDirectory, Endpoint.Default.URI);

// Create a client from the repository and query categories
var server = new UpstreamServerClient(localRepo);
var categoriesQueryResult = await server.GetCategories();

// Save retrieved categories
localRepo.MergeQueryResult(categoriesQueryResult);
```

##### Query a repository
```
// Open an existing repository
var localRepo = FileSystemRepository.Open(Environment.CurrentDirectory);

// Get categories from the repository
var categories = localRepo.GetCategories();

// Print category ID and title
categories.ForEach(cat => Console.WriteLine($"{cat.Identity}:{cat.Title}"));
```

#### The [upsteam server](Microsoft.UpdateServices.Server.UpstreamServerStartup.html)
Use [UpstreamServerStartup](Microsoft.UpdateServices.Server.UpstreamServerStartup.html) to run an upstream server in your ASP.NET web app.

First sync some updates to a local repository, then configure the upstream server startup to distribute updates from your local repository to downstream WSUS servers.

See [this example](../examples/upstream_server.html) for running an upstream server in a ASP.NET Core web app.