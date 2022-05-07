
##### Query update metadata

This code sample uses the [IMetadataStore](/api/Microsoft.PackageGraph.Storage.IMetadataStore.html) to retrieve updates by type and to filter updates by various metadata fields

Run the [windows updates fetch sample](windows-updates-fetch.html) first to populate the store with some updates.

```
// Open the local updates store
using var packageStore = PackageStore.Open("./store");

// Grab the first cumulative update that is superseded by another update
var firstUpdateAvailable = packageStore
    .OfType<SoftwareUpdate>()
    .FirstOrDefault(update => update.IsSupersededBy?.Count > 0 && 
    update.Title.Contains("cumulative", StringComparison.OrdinalIgnoreCase));

if (firstUpdateAvailable is not null)
{
    Console.WriteLine($"Software update in the store: {firstUpdateAvailable.Title}");
    Console.WriteLine($"Superseded by:");
    foreach (var supersededUpdateId in firstUpdateAvailable.IsSupersededBy)
    {
        var supersededByUpdate = packageStore
            .FirstOrDefault(update => update.Id == supersededUpdateId);
        if (supersededByUpdate is not null)
        {
            Console.WriteLine($"    {supersededByUpdate.Title}");
        }
    }    
}
```
