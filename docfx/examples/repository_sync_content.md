Sync update content to a local repository.

Assumes the repository has been initialized and some updates sync'ed with an upstream server.
```
// Open a repository that was previously sync'ed with the desired updates
var localRepo = FileSystemRepository.Open(Environment.CurrentDirectory);

// Get all the updates that have downloadable content
var updatesWithContent = localRepo
    .GetUpdates(UpdateRetrievalMode.Basic)
    .OfType<IUpdateWithFiles>();
if (updatesWithContent.Count() > 0)
{
    // Get update extended metadata for the first update
    var updateToDownload = localRepo.GetUpdate(
        (updatesWithContent.First() as Update).Identity,
        UpdateRetrievalMode.Extended);

    // Download the first update that has content
    localRepo.DownloadUpdateContent(updateToDownload as IUpdateWithFiles);
}
```
