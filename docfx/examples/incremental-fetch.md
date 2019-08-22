Fetch incremental changes from an upstream server.

Assumes some updates have been fetched to baseline.zip
```
var server = new UpstreamServerClient(Endpoint.Default);

var baselineSource = CompressedMetadataStore.Open("baseline.zip");
var latestSource = new CompressedMetadataStore(baselineSource);

var queryFilter = baselineSource.Filters.FirstOrDefault();
if (queryFilter == null)
{
    // The baseline does not have any filteres; create a filter that matches all updates
    queryFilter = new QueryFilter(
        baselineSource.ProductsIndex.Values,
        baselineSource.ClassificationsIndex.Values);
}

// This call performs an incremental fetch from the baseline
// The result is saved to baseline-1.zip and contains changed updates.
// To open baseline-1.zip in the future, baseline.zip must be present in the same directory.
await server.GetUpdates(queryFilter, latestSource);

// Finalize changes; this enables read access to the metadata source
latestSource.Commit();

// One next fetch, use baseline-1.zip as the baseline:
var newBaseline = CompressedMetadataStore.Open("baseline-1.zip");
var newLatest = new CompressedMetadataStore(newBaseline);

// Fetch updates that changed; results are written to baseline-2.zip.
// Please note that baseline.zip, baseline-1.zip and baseline-2.zip
// are required when opening baseline-2.zip
await server.GetUpdates(newBaseline.Filters.First(), newLatest);

// Finalize
newLatest.Commit();
```
