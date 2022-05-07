##### Fetch Windows Cumulative updates from the Microsoft Update Catalog

Run the [categories fetch](categories-fetch.html) sample first. This sample needs products information in order to selectively retrieve updates.

```
// Open the local store
// Assumes that categories have been retrieved and saved to this store
// We'll save fetched updates to this store as well
using var packageStore = PackageStore.Open("./store");

// Create a filter to retrieve selected updates by product name
var updatesFilter = new SourceFilter();

// Set a "windows 11" product filter.
// First find the "Windows" product
var windowsProduct = packageStore
    .OfType<ProductCategory>()
    .First(category => category.Title.Equals("Windows"));

// Find the "Windows 11" product that is a child of "Windows"
var windows11Product = packageStore
    .OfType<ProductCategory>()
    .First(category => category.Categories.Contains(windowsProduct.Id.ID) && 
    category.Title.Equals("Windows 11"));
updatesFilter.ProductsFilter.Add(windows11Product.Id.ID);

// Request all available update classifications for the product selected
updatesFilter
    .ClassificationsFilter
    .AddRange(packageStore.OfType<ClassificationCategory>().Select(classification => classification.Id.ID));

// Create an upstream updates source from the Microsoft Update Catalog
var updatesSource = new UpstreamUpdatesSource(Endpoint.Default, updatesFilter);

// Copy updates from the upstream to the local store
updatesSource.CopyTo(packageStore, CancellationToken.None);
Console.WriteLine($"Copied {packageStore.GetPendingPackages().Count} new updates");
```
