# Windows Update Services ServerServer Sync Protocol

Provide a C# implementation (.NET Core) of the Microsoft Update Server-Server sync protocol, both client and server.

Use this library to
* programmatically browse the Microsoft Update catalog
* sync updates locally and run advanced queries on update metadata
* export updates to WSUS
* run your upstream update server in an ASP.NET Core web app and serve updates to downstream WSUS servers

## Reference the library in your project

The easiest way is to use the published NuGet package. In your .NET Core project, add a reference to the [UpdateServices.ServerServerSync NuGet package](https://www.nuget.org/packages/UpdateServices.ServerServerSync). Make sure to check "Include prerelease" if searching for the NuGet package in Visual Studio.

Alternatively, you can compile the code yourself. Visual Studio 2017 with .Net Core development tools is required to build the solution provided at build\server-server-update-sync.sln

## Use the library

Please refer to the [API documentation](https://microsoft.github.io/update-server-server-sync/) for help on using the library.

[Get started with using the update sync client](https://microsoft.github.io/update-server-server-sync/api/index.html#the-upstreamserverclient)

[Get started with using the updates sync server](https://microsoft.github.io/update-server-server-sync/api/index.html#the-upsteam-server)


## Use the upsync utility
The upsync command line utility is provided as a sample for using the library. This tool can be used to browse Microsoft's update catalog, sync updates and export updates to a WSUS server.

You can build the tool in Visual Studio. It builds from the same solution as the library.

Or download and unzip upsync from [https://github.com/microsoft/update-server-server-sync/releases](https://github.com/microsoft/update-server-server-sync/releases)

See the wiki for [command line options reference](https://github.com/microsoft/update-server-server-sync/wiki/UpSync-tool-command-line-options)

See the wiki for [samples on running the upsync tool](https://github.com/microsoft/update-server-server-sync/wiki/UpSync-tool-examples)

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
