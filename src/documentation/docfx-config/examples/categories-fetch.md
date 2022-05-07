##### Working with categories in the Microsoft Update Catalog

Retrieve categories from the catalog:
```
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;
using Microsoft.PackageGraph.MicrosoftUpdate.Source;
using Microsoft.PackageGraph.Storage.Local;

...

// Create a categories source from the Microsoft Update Catalog
UpstreamCategoriesSource categoriesSource = new(Endpoint.Default);

// Create a local store to save categories locally
using var packageStore = PackageStore.OpenOrCreate("./store");

// Copy categories from the upstream source to the local store
categoriesSource.CopyTo(packageStore, CancellationToken.None);            
Console.WriteLine($"Copied {packageStore.GetPendingPackages().Count} new categories");
```

After the code executes, "./store" contains a local copy of all categories available in the Microsoft Update catalog. Use categories to filter updates in the catalog.

Finding categories by name:
```
// Find the "Windows" product category.
var windowsProduct = packageStore
    .OfType<ProductCategory>()
    .First(category => category.Title.Equals("Windows"));
```

Find classifications by name:
```
var securityUpdateClassification = packageStore
    .OfType<ClassificationCategory>()
    .Where(classification => classification.Title.Equals("Security Updates"))
    .FirstOrDefault();
```


