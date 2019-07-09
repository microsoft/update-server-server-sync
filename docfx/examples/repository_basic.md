The following example shows how to:
* create a local repository
* sync categories
* create a category based filter
* sync updates using a filter
* save the results in the local repository

```
// Create a repository for updates in the current directory, tracking the official
// Microsoft upstream server
var localRepository = FileSystemRepository.Init(
    Environment.CurrentDirectory,
    Endpoint.Default.URI);

// Create a client that caches to the local repository
var server = new UpstreamServerClient(localRepository);

// Get categories
var categories = await server.GetCategories();

// Save the categories retrieved
localRepository.MergeQueryResult(categories);

// Create a filter for "Windows 10, version 1903 and later" and all classifications
var filter = new QueryFilter(
    localRepository.ProductsIndex.Values.Where(
        p => p.Title.Contains("Windows 10, version 1903 and later")),
    localRepository.ClassificationsIndex.Values);

// Query updates, using filter
var updates = await server.GetUpdates(filter);

// Save the updates retrieved
localRepository.MergeQueryResult(updates);
```
