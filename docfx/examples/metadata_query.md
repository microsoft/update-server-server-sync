The following example shows how to filter updates from a metadata source:

Assumes the repository has been initialized and sync'ed with an upstream server.
```
// Open a metadata source
var metadataSource = CompressedMetadataStore.Open("metadata.zip");

// Get all drivers in the repository
var drivers = metadataSource.UpdatesIndex.OfType<DriverUpdate>().ToList();
           
// Print the ID and title for each driver update
drivers .ForEach(d => Console.WriteLine($"{d.Identity} : {d.Title}"));

// Get all superseded updates
var supersededUpdates = metadataSource.UpdatesIndex.Values.Where(u => u.IsSuperseded);
```
