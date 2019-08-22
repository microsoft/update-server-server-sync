This library provides a high level abstraction over the underlying server-sync SOAP-based protocol. Authentication, server configuration, batched queries, metadata and content cross-linking are handled internally. Update XML metadata data is parsed and indexed internally and exposed as native C# properties: prerequisites, bundled updates, files, categories, extended metadata.

#### The [UpstreamServerClient](/api/Microsoft.UpdateServices.Client.UpstreamServerClient.html)
Use UpstreamServerClient to retrieve categories and updates from an upstream update server or download update content.

##### Retrieve update categories:
```
var server = new UpstreamServerClient(Endpoint.Default);
var categoriesSource = await server.GetCategories();
categoriesSource
    .CategoriesIndex
    .Values
    .ToList()
    .ForEach(cat => Console.WriteLine(cat.Title));

// The categories source is saved on the file system; the path is available in categoriesSource.FilePath
// Delete the categories source file
categoriesSource.Delete();
```

##### Retrieve update metadata:
```
var server = new UpstreamServerClient(Endpoint.Default);
var categoriesSource = await server.GetCategories();

// Create a filter for first product and all classifications for it
var filter = new QueryFilter(
    categoriesSource.ProductsIndex.Values.Take(1),
    categoriesSource.ClassificationsIndex.Values);

// Get updates
var updatesSource = await server.GetUpdates(filter);
updatesSource
    .UpdatesIndex
    .Values
    .ToList()
    .ForEach(update => Console.WriteLine(update.Title));

updatesSource.Delete();
categoriesSource.Delete();
```

#### The [update metadata source](/api/Microsoft.UpdateServices.Storage.IMetadataSource.html)
A metadata source caches update metadata locally and is used for filtering, quering and serving updates to Windows PCs or downstream servers. A metadata source stores update metadata from a single upstream update server using at most one filter. Multiple sources can be created to sync from multiple upstream servers or use different filters.

A [compressed metadata store](/api/Microsoft.UpdateServices.Storage.CompressedMetadataStore.html) is a metadata source implementation that stores update metadata and indexes within a compressed archive. CompressedMetadataStore supports storing incremental changes from a baseline and can be used with [UpstreamServerClient](/api/Microsoft.UpdateServices.Client.UpstreamServerClient.html) to execute incremental fetching of metadata:

```
var server = new UpstreamServerClient(Endpoint.Default);

// Open the baseline metadata source
var baselineSource = CompressedMetadataStore.Open("baseline.zip");

// Create a metadata sink, with a baseline
var latestSource = new CompressedMetadataStore(baselineSource);

// Open the baseline's filter if it exists, or create a new one
var queryFilter = baselineSource.Filters.FirstOrDefault();
if (queryFilter == null)
{
    // The baseline does not have any filteres; create a filter that matches all updates
    queryFilter = new QueryFilter(
        baselineSource.ProductsIndex.Values,
        baselineSource.ClassificationsIndex.Values);
}

// This call performs an incremental fetch from the baseline
// The result is saved to baseline-1.zip and contains changed updates. To open baseline-1.zip in the future, baseline.zip must be present in the same directory.
// latestSource can be used to query metadata and queries operate on both baseline and changed metadata.
await server.GetUpdates(queryFilter, latestSource);

// Finalize changes in the sink
latestSource.Commit();
```

A compressed metadata source file is portable, as long as all incremental files are copied together.

To open a metadata source:
```
var metadataSource = CompressedMetadataStore.Open("baseline-1.zip");
```

#### The update content store
A content store manages update content received from an upstream server. 
The [FileSystemContentStore](/api/Microsoft.UpdateServices.Storage.FileSystemContentStore.html) class is an implementation of a content store sink and source. It can be used to both download update content from an upstream server and read content from it with the intent of serving it to Windows PCs or downstream servers.

To download an update file:
```
// Open the metadata store to find an update
var metadataSource = CompressedMetadataStore.Open("baseline-1.zip");

// Take the first update that has content
var updateWithFile = metadataSource.UpdatesIndex.Values.Where(u => u.HasFiles).FirstOrDefault();
if (updateWithFile != null)
{
    // Open or create a content store in the current directory
    var contentStore = new FileSystemContentStore(@".\");
    contentStore.Add(updateWithFile.Files);
}
```

To read an update file from the store
```
// Open the metadata store to find an update
var metadataSource = CompressedMetadataStore.Open("baseline-1.zip");

// Take the first update that has content
var updateWithFile = metadataSource.UpdatesIndex.Values.Where(u => u.HasFiles).FirstOrDefault();
if (updateWithFile != null)
{
    // Open or create a content store in the current directory
    var contentStore = new FileSystemContentStore(@".\");
    if (contentStore.Contains(updateWithFile.Files.First()))
    {
        using (var updateContentStream = contentStore.Get(updateWithFile.Files.First()))
        {
            Console.WriteLine($"Update content length: {updateContentStream.Length}");
        }
    }
}
```

#### The [upsteam server](/api/Microsoft.UpdateServices.Server.UpstreamServerStartup.html)
Use [UpstreamServerStartup](/api/Microsoft.UpdateServices.Server.UpstreamServerStartup.html) to run an upstream server in your ASP.NET web app.

First sync some updates to a metadata source, then configure the upstream server startup to distribute updates from the metadata source to downstream WSUS servers.

See [this example](/examples/upstream_server.html) for running an upstream server in a ASP.NET Core web app.