// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UpdateServices.Tools.UpdateRepo
{
    public interface IRepositoryPathOption
    {
        string RepositoryPath { get; }
    }

    public interface IUpdatesFilter
    {
        IEnumerable<string> ProductsFilter { get; }

        IEnumerable<string> ClassificationsFilter { get; }

        IEnumerable<string> IdFilter { get; }
        
        string TitleFilter { get; }
    }

    [Verb("sync-categories", HelpText = "Syncs categories metadata in a repository.")]
    public class CategoriesSyncOptions : IRepositoryPathOption
    {
        [Option("repo-path", Required = false, HelpText = "Repo location; if not set, the current directory is used.")]
        public string RepositoryPath { get; set; }
    }

    [Verb("sync-updates", HelpText = "Syncs updates with an upstream server")]
    public class UpdatesSyncOptions : IRepositoryPathOption
    {
        [Option("repo-path", Required = false, HelpText = "Repo location; if not set, the current directory is used.")]
        public string RepositoryPath { get; set; }

        [Option("product-filter", Required = false, Separator = '+', HelpText = "Product filter for sync'ing updates")]
        public IEnumerable<string> ProductsFilter { get; set; }

        [Option("classification-filter", Required = false, Separator = '+', HelpText = "Classification filter for sync'ing updates")]
        public IEnumerable<string> ClassificationsFilter { get; set; }
    }

    [Verb("sync-content", HelpText = "Syncs update content with an upstream server")]
    public class ContentSyncOptions : IRepositoryPathOption, IUpdatesFilter
    {
        [Option("repo-path", Required = false, HelpText = "Repo location; if not set, the current directory is used.")]
        public string RepositoryPath { get; set; }

        [Option("drivers", Required = false, HelpText = "Filter to drivers only")]
        public bool Drivers { get; set; }

        [Option("product-filter", Required = false, Separator = '+', HelpText = "Product filter for sync'ing updates")]
        public IEnumerable<string> ProductsFilter { get; set; }

        [Option("classification-filter", Required = false, Separator = '+', HelpText = "Classification filter for sync'ing updates")]
        public IEnumerable<string> ClassificationsFilter { get; set; }

        [Option("id-filter", Required = false, Separator = '+', HelpText = "ID filter")]
        public IEnumerable<string> IdFilter { get; set; }

        [Option("title-filter", Required = false, HelpText = "Title filter")]
        public string TitleFilter { get; set; }
    }

    [Verb("init", HelpText = "Initializes a new local updates repository")]
    public class InitRepositoryOptions : IRepositoryPathOption
    {
        [Option("repo-path", Required = false, HelpText = "Repo location; if not set, the current directory is used.")]
        public string RepositoryPath { get; set; }

        [Option("upstream-server", Required = false, HelpText = "Upstream server address; if none is provided, the official Microsoft upstream server is used")]
        public string UpstreamServerAddress { get; set; }

        [Option("account-name", Required = false, HelpText = "Account name; if not set, a random GUID is used.")]
        public string AccountName { get; set; }

        [Option("account-guid", Required = false, HelpText = "Account GUID. If not set, a random GUID is used.")]
        public string AccountGuid { get; set; }
    }

    [Verb("status", HelpText = "Displays status information about and updates repository")]
    public class RepositoryStatusOptions : IRepositoryPathOption
    {
        [Option("repo-path", Required = false, HelpText = "Repo location; if not set, the current directory is used.")]
        public string RepositoryPath { get; set; }
    }

    [Verb("delete", HelpText = "Deletes a local updates repository")]
    public class DeleteRepositoryOptions : IRepositoryPathOption
    {
        [Option("repo-path", Required = false, HelpText = "Repo location; if not set, the current directory is used.")]
        public string RepositoryPath { get; set; }
    }

    [Verb("query", HelpText = "Queries update metadata in a local repository")]
    public class QueryRepositoryOptions : IRepositoryPathOption, IUpdatesFilter
    {
        [Option("repo-path", Required = false, HelpText = "Repo location; if not set, the current directory is used.")]
        public string RepositoryPath { get; set; }

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

        [Option("id-filter", Required = false, Separator = '+', HelpText = "ID filter")]
        public IEnumerable<string> IdFilter { get; set; }

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

        [Option("extended-metadata", Required = false, HelpText = "Query extended update metadata")]
        public bool ExtendedMetadata { get; set; }
    }

    [Verb("export", HelpText = "Export update metadata from the repository.")]
    public class RepositoryExportOptions : IRepositoryPathOption, IUpdatesFilter
    {
        [Option("repo-path", Required = false, HelpText = "Repo location; if not set, the current directory is used.")]
        public string RepositoryPath { get; set; }

        [Option("export-file", Required = true, HelpText = "File where to export updates. If the file exists, it will be overwritten.")]
        public string ExportFile { get; set; }

        [Option("product-filter", Required = false, Separator = '+', HelpText = "Product filter")]
        public IEnumerable<string> ProductsFilter { get; set; }

        [Option("classification-filter", Required = false, Separator = '+', HelpText = "Classification filter")]
        public IEnumerable<string> ClassificationsFilter { get; set; }

        [Option("id-filter", Required = false, Separator = '+', HelpText = "ID filter")]
        public IEnumerable<string> IdFilter { get; set; }

        [Option("title-filter", Required = false, HelpText = "Title filter")]
        public string TitleFilter { get; set; }
    }

    [Verb("run-upstream-server", HelpText = "Serve updates to downstream servers")]
    public class RunUpstreamServerOptions : IRepositoryPathOption, IUpdatesFilter
    {
        [Option("repo-path", Required = false, HelpText = "Repo location to serve updates from; if not set, the current directory is used.")]
        public string RepositoryPath { get; set; }

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

        [Option("metadata-only", Required = false, HelpText = "Do not serve updates content, just metadata")]
        public bool MetadataOnly { get; set; }

    }
}
