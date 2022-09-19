// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CommandLine;
using System.Collections.Generic;

namespace Microsoft.PackageGraph.Utilitites.Upsync
{
    enum PackageType
    {
        MicrosoftUpdateClassification,
        MicrosoftUpdateProduct,
        MicrosoftUpdateDetectoid,
        MicrosoftUpdateUpdate,
        MicrosoftUpdateDriver,
        AnyPackage
    }
    public interface IMetadataSourceOptions
    {
        string UpstreamEndpoint { get; }
        string EndpointType { get; }
    }

    public interface IMetadataStoreOptions
    {
        string Alias { get; }

        string Path { get; }

        string Type { get; }

        string StoreConnectionString { get; }
    }

    public interface IMetadataFilterOptions
    {
        IEnumerable<string> ProductsFilter { get; }

        IEnumerable<string> ClassificationsFilter { get; }

        IEnumerable<string> IdFilter { get; }

        string HardwareIdFilter { get; }

        string ComputerHardwareIdFilter { get; set; }

        string TitleFilter { get; }

        bool SkipSuperseded { get; }

        IEnumerable<string> KbArticleFilter { get; }

        int FirstX { get; }
    }

    public interface ISyncQueryFilter
    {
        IEnumerable<string> ProductsFilter { get; }

        IEnumerable<string> ClassificationsFilter { get; }
    }

    [Verb("fetch-config", HelpText = "Retrieves upstream server configuration")]
    public class FetchConfigurationOptions
    {
        [Option("endpoint", Required = false, HelpText = "The endpoint from which to fetch updates", SetName = "custom")]
        public string UpstreamEndpoint { get; set; }

        [Option("master", Required = false, Default = false, HelpText = "Only fetch categories", SetName = "official")]
        public bool MasterEndpoint { get; set; }

        [Option("destination", Required = true, HelpText = "Destination JSON file.")]
        public string OutFile { get; set; }
    }

    [Verb("pre-fetch", HelpText = "Retrieves metadata from an upstream server")]
    public class FetchCategoriesOptions : IMetadataStoreOptions
    {
        [Option("endpoint", Required = false, HelpText = "The endpoint from which to fetch categories.", SetName = "custom")]
        public string UpstreamEndpoint { get; set; }

        [Option("master", Required = false, Default = false, HelpText = "Fetch categories from the official Microsoft upstream server.", SetName = "official")]
        public bool MasterEndpoint { get; set; }

        [Option("account-name", Required = false, HelpText = "Account name; if not set, a random GUID is used.")]
        public string AccountName { get; set; }

        [Option("account-guid", Required = false, HelpText = "Account GUID. If not set, a random GUID is used.")]
        public string AccountGuid { get; set; }

        [Option("store-alias", Required = false, HelpText = "Destination store alias")]
        public string Alias { get; set; }

        [Option("store-path", Required = false, HelpText = "Destination store")]
        public string Path { get; set; }

        [Option("store-type", Required = false, Default = "local", HelpText = "Store type; local (default) or azure")]
        public string Type { get; set; }

        [Option("connection-string", Required = false, HelpText = "Azure connection string; required when the store type is azure")]
        public string StoreConnectionString { get; set; }
    }

    [Verb("index", HelpText = "Indexes a package store")]
    public class ReindexStoreOptions : IMetadataStoreOptions
    {
        [Option("store-alias", Required = false, HelpText = "Destination store alias")]
        public string Alias { get; set; }

        [Option("store-path", Required = false, HelpText = "Store to index")]
        public string Path { get; set; }

        [Option("store-type", Required = false, Default = "local", HelpText = "Store type; local (default) or azure")]
        public string Type { get; set; }

        [Option("connection-string", Required = false, HelpText = "Azure connection string; required when the store type is azure")]
        public string StoreConnectionString { get; set; }

        [Option("force", Required = false, Default = false, HelpText = "Force indexing even when not required")]
        public bool ForceReindex { get; set; }
    }

    [Verb("fetch", HelpText = "Retrieves metadata from an upstream server")]
    public class FetchPackagesOptions : IMetadataStoreOptions, IMetadataSourceOptions
    {
        public const string MicrosoftUpdateEndpoint = "microsoft-update";
        public const string NuGetV3Endpoint = "nuget";
        public const string LinuxEndpoint = "linux";
        public const string WebEndpoint = "web";

        [Option("store-alias", Required = false, HelpText = "Destination store alias")]
        public string Alias { get; set; }

        [Option("store-path", Required = false, HelpText = "Destination store")]
        public string Path { get; set; }

        [Option("store-type", Required = false, Default = "local", HelpText = "Store type; local (default) or azure")]
        public string Type { get; set; }

        [Option("connection-string", Required = false, HelpText = "Azure connection string; required when the store type is azure")]
        public string StoreConnectionString { get; set; }

        [Option("endpoint", Required = false, HelpText = "The endpoint from which to fetch updates.")]
        public string UpstreamEndpoint { get; set; }

        [Option("endpoint-type", Required = false, Default = MicrosoftUpdateEndpoint, HelpText = "The endpoint from which to fetch updates.")]
        public string EndpointType { get; set; }

        [Option("product-filter", Required = false, Separator = '+', HelpText = "Product filter for sync'ing updates")]
        public IEnumerable<string> ProductsFilter { get; set; }

        [Option("classification-filter", Required = false, Separator = '+', HelpText = "Classification filter for sync'ing updates")]
        public IEnumerable<string> ClassificationsFilter { get; set; }

        [Option("account-name", Required = false, HelpText = "Account name; if not set, a random GUID is used.")]
        public string AccountName { get; set; }

        [Option("account-guid", Required = false, HelpText = "Account GUID. If not set, a random GUID is used.")]
        public string AccountGuid { get; set; }

        [Option("ids", Required = false, Separator = '+', HelpText = "Try fetch metadata for this list of ids (GUIDs)")]
        public IEnumerable<string> Ids { get; set; }
    }

    [Verb("fetch-content", HelpText = "Downloads update content from an upstream server")]
    public class ContentSyncOptions : IMetadataStoreOptions, IMetadataFilterOptions
    {
        [Option("metadata-store-alias", Required = false, HelpText = "Destination store alias")]
        public string Alias { get; set; }

        [Option("metadata-store-path", Required = false, HelpText = "Destination store")]
        public string Path { get; set; }

        [Option("metadata-store-type", Required = false, Default = "local", HelpText = "Store type; local (default) or azure")]
        public string Type { get; set; }

        [Option("connection-string", Required = false, HelpText = "Azure connection string; required when the store type is azure")]
        public string StoreConnectionString { get; set; }

        [Option("content-store-path", Required = true, HelpText = "Destination content store")]
        public string ContentPath { get; set; }

        [Option("content-store-type", Required = false, Default = "local", HelpText = "Content store type; default is local")]
        public string ContentStoreType { get; set; }

        [Option("content-connection-string", Required = false, HelpText = "Azure connection string; required when the store type is azure")]
        public string ContentStoreConnectionString { get; set; }

        [Option("product-filter", Required = false, Separator = '+', HelpText = "Product filter for sync'ing updates")]
        public IEnumerable<string> ProductsFilter { get; set; }

        [Option("classification-filter", Required = false, Separator = '+', HelpText = "Classification filter for sync'ing updates")]
        public IEnumerable<string> ClassificationsFilter { get; set; }

        [Option("id-filter", Required = false, Separator = '+', HelpText = "ID filter")]
        public IEnumerable<string> IdFilter { get; set; }

        [Option("title-filter", Required = false, HelpText = "Title filter")]
        public string TitleFilter { get; set; }

        [Option("hwid-filter", Required = false, HelpText = "Hardware ID filter")]
        public string HardwareIdFilter { get; set; }

        [Option("computer-hwid-filter", Required = false, HelpText = "Computer hardware ID filter")]
        public string ComputerHardwareIdFilter { get; set; }

        [Option("kbarticle-filter", Required = false, Separator = '+', HelpText = "KB article filter (numbers only)")]
        public IEnumerable<string> KbArticleFilter { get; set; }

        [Option("skip-superseded", Required = false, Default = false, HelpText = "Do not consider superseded updates for download")]
        public bool SkipSuperseded { get; set; }

        [Option("first", Required = false, Default = 0, HelpText = "Content sync only the first x packages")]
        public int FirstX { get; set; }
    }

    [Verb("status", HelpText = "Displays status information about and updates metadata source")]
    public class MetadataSourceStatusOptions : IMetadataStoreOptions
    {
        [Option("store-alias", Required = false, HelpText = "Destination store alias")]
        public string Alias { get; set; }

        [Option("store-path", Required = false, HelpText = "Store to get status for")]
        public string Path { get; set; }

        [Option("store-type", Required = false, Default = "local", HelpText = "Store type; local (default) or azure")]
        public string Type { get; set; }

        [Option("connection-string", Required = false, HelpText = "Azure connection string; required when the store type is azure")]
        public string StoreConnectionString { get; set; }
    }

    [Verb("query", HelpText = "Query package metadata from a package store")]
    public class QueryMetadataOptions : IMetadataStoreOptions, IMetadataFilterOptions
    {
        [Option("store-alias", Required = false, HelpText = "Destination store alias")]
        public string Alias { get; set; }

        [Option("store-path", Required = false, HelpText = "Store to query")]
        public string Path { get; set; }

        [Option("store-type", Required = false, Default = "local", HelpText = "Store type; local (default) or azure")]
        public string Type { get; set; }

        [Option("connection-string", Required = false, HelpText = "Azure connection string; required when the store type is azure")]
        public string StoreConnectionString { get; set; }

        [Option("package-type", Required = true, HelpText = "Type of package to query")]
        public string PackageType { get; set; }

        [Option("id-filter", Required = false, Separator = '+', HelpText = "ID filter")]
        public IEnumerable<string> IdFilter { get; set; }

        [Option("file-hash", Required = false, HelpText = "File hash", SetName = "files")]
        public string FileHash { get; set; }

        [Option("title-filter", Required = false, HelpText = "Title filter")]
        public string TitleFilter { get; set; }

        [Option("hwid-filter", Required = false, HelpText = "Hardware ID filter")]
        public string HardwareIdFilter { get; set; }

        [Option("computer-hwid-filter", Required = false, HelpText = "Computer hardware ID filter")]
        public string ComputerHardwareIdFilter { get; set; }

        [Option("product-filter", Required = false, Separator = '+', HelpText = "Product filter")]
        public IEnumerable<string> ProductsFilter { get; set; }

        [Option("classification-filter", Required = false, Separator = '+', HelpText = "Classification filter")]
        public IEnumerable<string> ClassificationsFilter { get; set; }

        [Option("kbarticle-filter", Required = false, Separator = '+', HelpText = "KB article filter (numbers only)")]
        public IEnumerable<string> KbArticleFilter { get; set; }

        [Option("skip-superseded", Required = false, Default = false, HelpText = "Ignore superseded updates")]
        public bool SkipSuperseded { get; set; }

        [Option("count-only", Required = false, Default = false, HelpText = "Count updates, do not display update information")]
        public bool CountOnly { get; set; }

        [Option("first", Required = false, Default = 0, HelpText = "Display first x updates only")]
        public int FirstX { get; set; }

        [Option("json-out-path", Required = false, HelpText = "Save results as JSON to the specified path")]
        public string JsonOutPath { get; set; }
    }

    [Verb("match-driver", HelpText = "Find drivers")]
    public class MatchDriverOptions : IMetadataStoreOptions
    {
        [Option("store-alias", Required = false, HelpText = "Destination store alias")]
        public string Alias { get; set; }

        [Option("store-path", Required = false, HelpText = "Store to match drivers from")]
        public string Path { get; set; }

        [Option("store-type", Required = false, Default = "local", HelpText = "Store type; local (default) or azure")]
        public string Type { get; set; }

        [Option("connection-string", Required = false, HelpText = "Azure connection string; required when the store type is azure")]
        public string StoreConnectionString { get; set; }

        [Option("hwid", Required = true, Separator = '+', HelpText = "Match drivers for this list of hardware ids; Add HwIds from specific to generic")]
        public IEnumerable<string> HardwareIds { get; set; }

        [Option("computer-hwid", Required = false, Separator = '+', HelpText = "Match drivers that target these computer hardware ids.")]
        public IEnumerable<string> ComputerHardwareIds { get; set; }

        [Option("installed-prerequisites", Required = true, Separator = '+', HelpText = "Prerequisites installed on the target computer. Used for driver applicability checks")]
        public IEnumerable<string> InstalledPrerequisites { get; set; }
    }

    [Verb("export", HelpText = "Export select update metadata from a metadata source.")]
    public class MetadataSourceExportOptions : IMetadataStoreOptions, IMetadataFilterOptions
    {
        [Option("store-alias", Required = false, HelpText = "Destination store alias")]
        public string Alias { get; set; }

        [Option("store-path", Required = false, HelpText = "Store to export from")]
        public string Path { get; set; }

        [Option("store-type", Required = false, Default = "local", HelpText = "Store type; local (default) or azure")]
        public string Type { get; set; }

        [Option("connection-string", Required = false, HelpText = "Azure connection string; required when the store type is azure")]
        public string StoreConnectionString { get; set; }

        [Option("export-file", Required = true, HelpText = "File where to export updates. If the file exists, it will be overwritten.")]
        public string ExportFile { get; set; }

        [Option("server-config", Required = true, HelpText = "JSON file containing server configuration./")]
        public string ServerConfigFile { get; set; }

        [Option("product-filter", Required = false, Separator = '+', HelpText = "Product filter")]
        public IEnumerable<string> ProductsFilter { get; set; }

        [Option("classification-filter", Required = false, Separator = '+', HelpText = "Classification filter")]
        public IEnumerable<string> ClassificationsFilter { get; set; }

        [Option("id-filter", Required = false, Separator = '+', HelpText = "ID filter")]
        public IEnumerable<string> IdFilter { get; set; }

        [Option("title-filter", Required = false, HelpText = "Title filter")]
        public string TitleFilter { get; set; }

        [Option("hwid-filter", Required = false, HelpText = "Hardware ID filter")]
        public string HardwareIdFilter { get; set; }

        [Option("computer-hwid-filter", Required = false, HelpText = "Computer hardware ID filter")]
        public string ComputerHardwareIdFilter { get; set; }

        [Option("kbarticle-filter", Required = false, Separator = '+', HelpText = "KB article filter (numbers only)")]
        public IEnumerable<string> KbArticleFilter { get; set; }

        [Option("skip-superseded", Required = false, Default = false, HelpText = "Do not export superseded updates")]
        public bool SkipSuperseded { get; set; }

        [Option("first", Required = false, Default = 0, HelpText = "Export only the first x updates")]
        public int FirstX { get; set; }
    }

    [Verb("run-upstream-server", HelpText = "Serve updates to downstream servers")]
    public class RunUpstreamServerOptions : IMetadataStoreOptions
    {
        [Option("metadata-store-alias", Required = false, HelpText = "Destination store alias")]
        public string Alias { get; set; }

        [Option("metadata-store-path", Required = false, HelpText = "Package metadata store to server packages from")]
        public string Path { get; set; }

        [Option("store-type", Required = false, Default = "local", HelpText = "Store type; local (default) or azure")]
        public string Type { get; set; }

        [Option("connection-string", Required = false, HelpText = "Azure connection string; required when the store type is azure")]
        public string StoreConnectionString { get; set; }

        [Option("content-source", Required = false, HelpText = "Path to content source")]
        public string ContentSourcePath { get; set; }

        [Option("service-config", Required = false, HelpText = "Path to service configuration JSON file")]
        public string ServiceConfigurationPath { get; set; }

        [Option("port", Required = false, Default = 32150, HelpText = "The port to bind the server to.")]
        public int Port { get; set; }

        [Option("endpoint", Required = false, Default = "*", HelpText = "The port to bind the server to.")]
        public string Endpoint { get; set; }
    }

    [Verb("run-update-server", HelpText = "Serve updates to Windows Update clients")]
    public class RunUpdateServerOptions : IMetadataStoreOptions
    {
        [Option("metadata-store-alias", Required = false, HelpText = "Destination store alias")]
        public string Alias { get; set; }

        [Option("metadata-store-path", Required = false, HelpText = "Package metadata store to server packages from")]
        public string Path { get; set; }

        [Option("store-type", Required = false, Default = "local", HelpText = "Store type; local (default) or azure")]
        public string Type { get; set; }

        [Option("connection-string", Required = false, HelpText = "Azure connection string; required when the store type is azure")]
        public string StoreConnectionString { get; set; }

        [Option("content-source", Required = false, HelpText = "Path to content source")]
        public string ContentSourcePath { get; set; }

        [Option("service-config", Required = false, HelpText = "Path to service configuration JSON file")]
        public string ServiceConfigurationPath { get; set; }

        [Option("port", Required = false, Default = 32150, HelpText = "The port to bind the server to.")]
        public int Port { get; set; }

        [Option("endpoint", Required = false, Default = "*", HelpText = "The port to bind the server to.")]
        public string Endpoint { get; set; }
    }

    [Verb("copy-metadata", HelpText = "Copy packages from one repository to another")]
    public class MetadataCopyOptions : IMetadataFilterOptions
    {
        [Option("source-alias", Required = false, HelpText = "Destination store alias")]
        public string SourceAlias { get; set; }

        [Option("source-path", Required = false, HelpText = "Package metadata source")]
        public string SourcePath { get; set; }

        [Option("source-type", Required = false, Default = "local", HelpText = "Source store type; local (default), azure-blob, azure-table etc.")]
        public string SourceType { get; set; }

        [Option("source-connection-string", Required = false, HelpText = "Source connection string; required for non-local sources")]
        public string SourceConnectionString { get; set; }

        [Option("destination-alias", Required = false, HelpText = "Destination store alias")]
        public string DestinationAlias { get; set; }

        [Option("destination-path", Required = false, HelpText = "Package metadata destination")]
        public string DestionationPath { get; set; }

        [Option("destination-type", Required = false, Default = "local", HelpText = "Destination store type; local (default), azure-blob, azure-table etc.")]
        public string DestinationType { get; set; }

        [Option("destination-connection-string", Required = false, HelpText = "Destination connection string; required for non-local destinations")]
        public string DestinationConnectionString { get; set; }

        [Option("id-filter", Required = false, Separator = '+', HelpText = "ID filter")]
        public IEnumerable<string> IdFilter { get; set; }

        [Option("title-filter", Required = false, HelpText = "Title filter")]
        public string TitleFilter { get; set; }

        [Option("hwid-filter", Required = false, HelpText = "Hardware ID filter")]
        public string HardwareIdFilter { get; set; }

        [Option("computer-hwid-filter", Required = false, HelpText = "Computer hardware ID filter")]
        public string ComputerHardwareIdFilter { get; set; }

        [Option("product-filter", Required = false, Separator = '+', HelpText = "Product filter")]
        public IEnumerable<string> ProductsFilter { get; set; }

        [Option("kbarticle-filter", Required = false, Separator = '+', HelpText = "KB article filter (numbers only)")]
        public IEnumerable<string> KbArticleFilter { get; set; }

        [Option("classification-filter", Required = false, Separator = '+', HelpText = "Classification filter")]
        public IEnumerable<string> ClassificationsFilter { get; set; }

        [Option("skip-superseded", Required = false, Default = false, HelpText = "Do not serve superseded updates")]
        public bool SkipSuperseded { get; set; }

        [Option("first", Required = false, Default = 0, HelpText = "Copy only the first x updates")]
        public int FirstX { get; set; }
    }

    [Verb("create-store-alias", HelpText = "Saves store information and create an alias for it")]
    public class StoreAliasCreateOptions : IMetadataStoreOptions
    {
        [Option("alias", Required = true, HelpText = "Alias for this store configuration")]
        public string Alias { get; set; }

        [Option("path", Required = true, HelpText = "Store path. Local path for a local path; store name for a cloud store")]
        public string Path { get; set; }

        [Option("type", Required = false, Default = "local", HelpText = "Store type; local (default) or azure")]
        public string Type { get; set; }

        [Option("connection-string", Required = false, HelpText = "Azure connection string; required when the store type is azure")]
        public string StoreConnectionString { get; set; }
    }

    [Verb("delete-store-alias", HelpText = "Deletes a store configuration by alias")]
    public class StoreAliasDeleteOptions
    {
        [Option("alias", Required = true, HelpText = "Delete only the specified alias", SetName ="specific")]
        public string Alias { get; set; }

        [Option("all", Required = true, HelpText = "Delete all aliases", SetName = "all")]
        public bool All{ get; set; }
    }

    [Verb("list-store-aliases", HelpText = "Lists stored store aliases")]
    public class StoreAliasListOptions
    {
        [Option("alias", Required = false, HelpText = "List only the specified alias")]
        public string Alias { get; set; }
    }
}
