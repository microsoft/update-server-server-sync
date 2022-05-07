# Microsoft.UpdateServices

#### Overview
This library provides:
* C# implementation (.NET Core) of the Microsoft Update Server-Server sync protocol, for both the client and server roles.
* C# implementation (.NET Core) of the server side of the Microsoft Update Client-Server sync protocol (MUv6)
* An object model for updates in the Microsoft Update Catalog

#### Use cases for this library

##### Retrieve and inspect update metadata from the Microsoft Update Catalog

Use the update source interfaces, [UpstreamCategoriesSource](api/Microsoft.PackageGraph.MicrosoftUpdate.Source.UpstreamCategoriesSource.html) and [UpstreamUpdatesSource](api/Microsoft.PackageGraph.MicrosoftUpdate.Source.UpstreamUpdatesSource.html), to retrieve updates from Microsoft Update Catalog (or upstream WSUS server), then use the provided [object model](api/Microsoft.PackageGraph.MicrosoftUpdate.Metadata.html) to inspect the metadata.

Optionally, you can use a [IMetadataStore](api/Microsoft.PackageGraph.Storage.IMetadataStore.html) to store update metadata locally. This enables incremental update synchronization with the upstream server and indexed fast queries on update metadata.


##### Serve updates to downstream WSUS servers or Windows Update clients

Use [UpstreamServerStartup](api/Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ServerSync.UpstreamServerStartup.html) in a ASP.NETCore web app to serve updates to other downstream WSUS servers.

Use [UpdateServerStartup](api/Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ClientSync.UpdateServerStartup.html) in a ASP.NETCore web app to serve Windows Update clients over the MUv6 protocol.

First retrieve updates from the Microsoft Update Catalog (or other upstream servers) and store them in a [IMetadataStore](api/Microsoft.PackageGraph.Storage.IMetadataStore.html), 

##### Customize the update metadata storage

Implement custom [IMetadataStore](api/Microsoft.PackageGraph.Storage.IMetadataStore.html), [IMetadataSink](api/Microsoft.PackageGraph.Storage.IMetadataSink.html)  or [IMetadataSource](api/Microsoft.PackageGraph.Storage.IMetadataSource.html) for specialized storage of update metadata.

#### Additional resources

See [MS-WSUSSS](https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-wsusss/f49f0c3e-a426-4b4b-b401-9aeb2892815c) for the complete technical documentation of the protocol.

See [Windows Server Update Server](https://docs.microsoft.com/en-us/windows-server/administration/windows-server-update-services/get-started/windows-server-update-services-wsus) for an introduction to WSUS.
