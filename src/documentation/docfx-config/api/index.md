This library provides a high level abstraction over the underlying WSUS server-sync SOAP-based protocol. It handles authentication, server configuration, batched queries, and cross-linking between updates and update content.

Update XML metadata is parsed into an [object model](Microsoft.PackageGraph.MicrosoftUpdate.Metadata.html) that exposes prerequisites, bundled updates, files, categories, and other update metadata.

#### [Upstream categories source](Microsoft.PackageGraph.MicrosoftUpdate.Source.UpstreamCategoriesSource.html)
[UpstreamCategoriesSource](Microsoft.PackageGraph.MicrosoftUpdate.Source.UpstreamCategoriesSource.html) retrieves categories from the Microsoft Update Server (or 3rd party upstream update server): [ProductCategory](Microsoft.PackageGraph.MicrosoftUpdate.Metadata.ProductCategory.html), [ClassificationCategory](Microsoft.PackageGraph.MicrosoftUpdate.Metadata.ClassificationCategory.html) and [DetectoidCategory](Microsoft.PackageGraph.MicrosoftUpdate.Metadata.DetectoidCategory.html)

An update has 1 or more classifications and 1 or more products. 

Products are hierarchical. For example the "Windows 10, 1903 and later" product has "Windows" as parent product. However, the server does not automatically return updates for child products when only the root product name was used in a filter. Subsequently, the leaf product names must be used when querying for updates using a product filter.

#### [Upstream update metadata source](Microsoft.PackageGraph.MicrosoftUpdate.Source.UpstreamUpdatesSource.html)
[UpstreamUpdatesSource](Microsoft.PackageGraph.MicrosoftUpdate.Source.UpstreamUpdatesSource.html) retrieves ([SoftwareUpdate](Microsoft.PackageGraph.MicrosoftUpdate.Metadata.SoftwareUpdate.html) or [DriverUpdate](Microsoft.PackageGraph.MicrosoftUpdate.Metadata.DriverUpdate.html)) from the Microsoft Update Server (or 3rd party upstream update server).

Use [IMetadataFilter](Microsoft.PackageGraph.Storage.IMetadataFilter.html) when querying the upstream update source; otherwise, the whole Microsoft Update Catalog will be retrieved. First query for categories from the catalog, then build a [UpstreamSourceFilter](Microsoft.PackageGraph.MicrosoftUpdate.Source.UpstreamSourceFilter.html) for the desired product and classification combination. The filter is then applied to [UpstreamUpdatesSource](Microsoft.PackageGraph.MicrosoftUpdate.Source.UpstreamUpdatesSource.html) to fetch only the desired updates.

#### [Metadata store](Microsoft.PackageGraph.Storage.IMetadataStore.html)
Use [IMetadataStore](Microsoft.PackageGraph.Storage.IMetadataStore.html) to store update metadata locally. The indexed metadata store allows for fast queries on update metadata and can be used as a source for serving updates to downstream servers or to Windows Update clients.

Two implemenentations are provided:
* [Azure Blob metadata store](Microsoft.PackageGraph.Storage.Azure.PackageStore.html) that stores update metadata in Azure blob storage
* [Local filesystem metadata store](Microsoft.PackageGraph.Storage.Local.PackageStore.html) that stores update metadata on the local file system

#### The [object model](Microsoft.PackageGraph.MicrosoftUpdate.Metadata.html)

Update metadata is parsed and linked into an object model, documented in the [Microsoft.PackageGraph.MicrosoftUpdate.Metadata](Microsoft.PackageGraph.MicrosoftUpdate.Metadata.html) namespace.

The object model allows querying for title, description, KB article, supersedence chain, applicability rules, hardware ID, and other update metadata fields.


#### [Content store](Microsoft.PackageGraph.Storage.IContentStore.html)

Use [IcontentStore](Microsoft.PackageGraph.Storage.IContentStore.html) to replicate update content from the Microsoft Update Catalog (or upstream server).

Note: replicating update content is not required in order to inspect update metadata

Two implementations are provided.
* [Azure Blob content store](Microsoft.PackageGraph.Storage.Azure.BlobContentStore.html) that stores update content in Azure blob storage
* [Local filesystem content store](Microsoft.PackageGraph.Storage.Local.FileSystemContentStore.html) that stores update content on the local file system

#### [Upstream ASP.NETCore server](api/Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ServerSync.UpstreamServerStartup.html)
Use [UpstreamServerStartup](api/Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ServerSync.UpstreamServerStartup.html) to run an upstream server that serves updates to downstream update servers (like WSUS).

First sync updates from the Microsoft Update Catalog (or other upstream server) to a metadata store, then configure the upstream server startup to distribute updates from the metadata store to downstream WSUS servers.

#### [Client sync ASP.NETCore server](Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ClientSync.UpdateServerStartup.html)
Use [UpdateServerStartup](Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ClientSync.UpdateServerStartup.html) to run an update server that serves updates to Windows Update clients over the MUv6 protocol.

#### [Microsoft Update content controller for ASP.NETCore](Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.Content.MicrosoftUpdateContentController.html)
Use [MicrosoftUpdateContentController](Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.Content.MicrosoftUpdateContentController.html) in a ASP.NETCore application to handle requests for Microsoft Update content coming from downstream WSUS servers or Windows Update clients.