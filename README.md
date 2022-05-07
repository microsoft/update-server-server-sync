# Windows Update Services ServerServer Sync Protocol

Provide a C# implementation (.NET Core) of the Microsoft Update Server-Server sync protocol, both client and server.

Use this library to
* programmatically browse the Microsoft Update catalog
* sync updates locally and run advanced queries on update metadata
* export updates to WSUS
* run an upstream update server in ASP.NET Core and serve updates to downstream WSUS servers
* run an update server in ASP.NET Core and serve updates Windows Update clients

## Reference the library in your project

Visual Studio 2022 with .Net Core development tools is required to build the solution provided at build\microsoft-update.sln

## Use the library

Please refer to the [API documentation](https://microsoft.github.io/update-server-server-sync/) for help on using the library.

[Code samples](https://microsoft.github.io/update-server-server-sync/examples/categories-fetch.html)


## Use the upsync utility
The upsync command line utility is provided as a sample for using the library. Upsync can be used to browse Microsoft's update catalog, sync updates locally and serve them to Windows Update clients or downstream WSUS servers.

You can build upsync in Visual Studio; it builds from the same solution as the library.

Or download and unzip upsync from [https://github.com/microsoft/update-server-server-sync/releases](https://github.com/microsoft/update-server-server-sync/releases)

See [upsync examples](https://github.com/microsoft/update-server-server-sync/wiki/UpSync-V3-examples)

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
