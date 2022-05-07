##### Fetch update content from the Microsoft Update Catalog

This code sample shows how to use the [IMetadataStore](/api/Microsoft.PackageGraph.Storage.IMetadataStore.html) to query for updates that have content and [IContentStore](/api/Microsoft.PackageGraph.Storage.IContentStore.html) to download and store update content locally.

Run the [windows updates fetch sample](windows-updates-fetch.html) first to populate the metadata store with some updates.

```
// Open the local updates store
// Make sure some updates have been fetched prior
using var packageStore = PackageStore.Open("./store");

// Grab the first update that has some content
var updateWithContent = packageStore
    .OfType<SoftwareUpdate>()
    .FirstOrDefault(update => update.Files?.Count() > 0);

if (updateWithContent is null)
{
    Console.WriteLine("No update in the store has content");
    return;
}

var contentFileToDownload = updateWithContent.Files.First();
Console.WriteLine(
    $"Downloading {contentFileToDownload.FileName}, size {contentFileToDownload.Size}");

var contentStore = new FileSystemContentStore("./content");
contentStore.Download(
    new List<IContentFile> { contentFileToDownload }, 
    CancellationToken.None);
```
