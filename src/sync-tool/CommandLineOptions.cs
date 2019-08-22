// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UpdateServices.Tools.UpdateRepo
{
    public interface IMetadataSourceOptions
    {
        string MetadataSourcePath { get; }
    }

    public interface IMetadataFilterOptions
    {
        IEnumerable<string> ProductsFilter { get; }

        IEnumerable<string> ClassificationsFilter { get; }

        IEnumerable<string> IdFilter { get; }
        
        string TitleFilter { get; }

        bool SkipSuperseded { get; }
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
    public class FetchCategoriesOptions
    {
        [Option("endpoint", Required = false, HelpText = "The endpoint from which to fetch categories.", SetName = "custom")]
        public string UpstreamEndpoint { get; set; }

        [Option("master", Required = false, Default = false, HelpText = "Fetch categories from the official Microsoft upstream server.", SetName = "official")]
        public bool MasterEndpoint { get; set; }

        [Option("account-name", Required = false, HelpText = "Account name; if not set, a random GUID is used.")]
        public string AccountName { get; set; }

        [Option("account-guid", Required = false, HelpText = "Account GUID. If not set, a random GUID is used.")]
        public string AccountGuid { get; set; }

        [Option("destination", Required = false, HelpText = "Destination file for fetch results. Must have .zip extension")]
        public string OutFile { get; set; }
    }

    [Verb("fetch", HelpText = "Retrieves metadata from an upstream server")]
    public class FetchUpdatesOptions : IMetadataSourceOptions
    {
        [Option("baseline", Required = true, HelpText = "Baseline for fetch; only updates not present in baseline are fetched")]
        public string MetadataSourcePath { get; set; }

        [Option("product-filter", Required = false, Separator = '+', HelpText = "Product filter for sync'ing updates")]
        public IEnumerable<string> ProductsFilter { get; set; }

        [Option("classification-filter", Required = false, Separator = '+', HelpText = "Classification filter for sync'ing updates")]
        public IEnumerable<string> ClassificationsFilter { get; set; }

        [Option("account-name", Required = false, HelpText = "Account name; if not set, a random GUID is used.")]
        public string AccountName { get; set; }

        [Option("account-guid", Required = false, HelpText = "Account GUID. If not set, a random GUID is used.")]
        public string AccountGuid { get; set; }
    }

    [Verb("fetch-content", HelpText = "Downloads update content from an upstream server")]
    public class ContentSyncOptions : IMetadataSourceOptions, IMetadataFilterOptions
    {
        [Option("metadata-source", Required = true, HelpText = "Path to metadata source file")]
        public string MetadataSourcePath { get; set; }

        [Option("destination", Required = true, HelpText = "Path to a local content store")]
        public string ContentDestination { get; set; }

        [Option("product-filter", Required = false, Separator = '+', HelpText = "Product filter for sync'ing updates")]
        public IEnumerable<string> ProductsFilter { get; set; }

        [Option("classification-filter", Required = false, Separator = '+', HelpText = "Classification filter for sync'ing updates")]
        public IEnumerable<string> ClassificationsFilter { get; set; }

        [Option("id-filter", Required = false, Separator = '+', HelpText = "ID filter")]
        public IEnumerable<string> IdFilter { get; set; }

        [Option("title-filter", Required = false, HelpText = "Title filter")]
        public string TitleFilter { get; set; }

        [Option("skip-superseded", Required = false, Default = false, HelpText = "Do not consider superseded updates for download")]
        public bool SkipSuperseded { get; set; }
    }

    [Verb("status", HelpText = "Displays status information about and updates metadata source")]
    public class MetadataSourceStatusOptions : IMetadataSourceOptions
    {
        [Option("source", Required = false, HelpText = "Metadata source path", SetName = "query-result")]
        public string MetadataSourcePath { get; set; }
    }

    [Verb("merge", HelpText = "Merges one or more incremental metadata sources into a single source")]
    public class MergeQueryResultOptions : IMetadataSourceOptions
    {
        [Option("source", Required = true, HelpText = "Path to the incremental metadata source to merge. ")]
        public string MetadataSourcePath { get; set; }

        [Option("destination", Required = false, HelpText = "Destination file for fetch results. Must have .zip extension")]
        public string OutFile { get; set; }
    }

    [Verb("query", HelpText = "Queries update metadata in a local updates metadata source")]
    public class QueryMetadataOptions : IMetadataSourceOptions, IMetadataFilterOptions
    {

        [Option("source", Required = true, HelpText = "Query content from a metadata source file")]
        public string MetadataSourcePath { get; set; }

        [Option("products", Required = true, HelpText = "Read products", SetName ="products")]
        public bool Products { get; set; }

        [Option("classifications", Required = true, HelpText = "Read classifications", SetName = "classifications")]
        public bool Classifications { get; set; }

        [Option("detectoids", Required = true, HelpText = "Read detectoids", SetName = "detectoids")]
        public bool Detectoids { get; set; }

        [Option("drivers", Required = true, HelpText = "Read drivers", SetName = "drivers")]
        public bool Drivers { get; set; }

        [Option("updates", Required = true, HelpText = "Read updates", SetName = "updates")]
        public bool Updates { get; set; }

        [Option("files", Required = true, HelpText = "Query files", SetName = "files")]
        public bool Files { get; set; }

        [Option("id-filter", Required = false, Separator = '+', HelpText = "ID filter")]
        public IEnumerable<string> IdFilter { get; set; }

        [Option("file-hash", Required = false, HelpText = "File hash", SetName ="files")]
        public string FileHash { get; set; }

        [Option("title-filter", Required = false, HelpText = "Title filter")]
        public string TitleFilter { get; set; }

        [Option("product-filter", Required = false, Separator = '+', HelpText = "Product filter")]
        public IEnumerable<string> ProductsFilter { get; set; }

        [Option("classification-filter", Required = false, Separator = '+', HelpText = "Classification filter")]
        public IEnumerable<string> ClassificationsFilter { get; set; }

        [Option("skip-superseded", Required = false, Default = false, HelpText = "Ignore superseded updates")]
        public bool SkipSuperseded { get; set; }

        [Option("count-only", Required = false, Default = false, HelpText = "Count updates, do not display update information")]
        public bool CountOnly { get; set; }

        [Option("first", Required = false, Default = 0, HelpText = "Display first x updates only")]
        public int FirstX { get; set; }
    }

    [Verb("export", HelpText = "Export select update metadata from a metadata source.")]
    public class MetadataSourceExportOptions : IMetadataSourceOptions, IMetadataFilterOptions
    {
        [Option("source", Required = true, HelpText = "Path to metadata source file")]
        public string MetadataSourcePath { get; set; }

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

        [Option("skip-superseded", Required = false, Default = false, HelpText = "Do not export superseded updates")]
        public bool SkipSuperseded { get; set; }
    }

    [Verb("run-upstream-server", HelpText = "Serve updates to downstream servers")]
    public class RunUpstreamServerOptions : IMetadataSourceOptions, IMetadataFilterOptions
    {
        [Option("metadata-source", Required = true, HelpText = "Path to metadata source file")]
        public string MetadataSourcePath { get; set; }

        [Option("content-source", Required = false, HelpText = "Path to content source")]
        public string ContentSourcePath { get; set; }

        [Option("service-config", Required = true, HelpText = "Path to service configuration JSON file")]
        public string ServiceConfigurationPath { get; set; }

        [Option("port", Required = false, Default = 32150, HelpText = "The port to bind the server to.")]
        public int Port { get; set; }

        [Option("endpoint", Required = false, Default = "*", HelpText = "The port to bind the server to.")]
        public string Endpoint { get; set; }

        [Option("id-filter", Required = false, Separator = '+', HelpText = "ID filter")]
        public IEnumerable<string> IdFilter { get; set; }

        [Option("title-filter", Required = false, HelpText = "Title filter")]
        public string TitleFilter { get; set; }

        [Option("product-filter", Required = false, Separator = '+', HelpText = "Product filter")]
        public IEnumerable<string> ProductsFilter { get; set; }

        [Option("classification-filter", Required = false, Separator = '+', HelpText = "Classification filter")]
        public IEnumerable<string> ClassificationsFilter { get; set; }

        [Option("skip-superseded", Required = false, Default = false, HelpText = "Do not serve superseded updates")]
        public bool SkipSuperseded { get; set; }
    }
}
