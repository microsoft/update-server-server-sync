The following example shows how to:
* fetch categories
* create a category based filter
* fetch updates using a filter

```
var server = new UpstreamServerClient(Endpoint.Default);
var categoriesSource = await server.GetCategories();

// Create a filter for first product and all classifications for it
var filter = new QueryFilter(
    categoriesSource.ProductsIndex.Values.Take(1),
    categoriesSource.ClassificationsIndex.Values);

// Get updates; the source is saved locally at the path updatesSource.FilePath
var updatesSource = await server.GetUpdates(filter);
updatesSource
    .UpdatesIndex
    .Values
    .ToList()
    .ForEach(update => Console.WriteLine(update.Title));

// The categories source is not needed anymore
categoriesSource.Delete();
```
