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

    [Verb("sync-categories", HelpText = "Syncs categories metadata in a repository.")]
    public class CategoriesSyncOptions : IRepositoryPathOption
    {
        [Option("repo-path", Required = false, HelpText = "Repo location, if not using the default one")]
        public string RepositoryPath { get; set; }
    }

    [Verb("sync-updates", HelpText = "Syncs updates with an upstream server")]
    public class UpdatesSyncOptions : IRepositoryPathOption
    {
        [Option("repo-path", Required = false, HelpText = "Repo location, if not using the default one")]
        public string RepositoryPath { get; set; }

        [Option("product-filter", Required = false, Separator = '+', HelpText = "Product filter for sync'ing updates")]
        public IEnumerable<string> ProductsFilter { get; set; }

        [Option("classification-filter", Required = false, Separator = '+', HelpText = "Classification filter for sync'ing updates")]
        public IEnumerable<string> ClassificationsFilter { get; set; }

        [Option("id-filter", Required = false, HelpText = "ID filter")]
        public string IdFilter { get; set; }

        [Option("title-filter", Required = false, HelpText = "Title filter")]
        public string TitleFilter { get; set; }
    }

    [Verb("init", HelpText = "Initializes a new local updates repository")]
    public class InitRepositoryOptions : IRepositoryPathOption
    {
        [Option("repo-path", Required = false, HelpText = "Repo location, if not using the current directory")]
        public string RepositoryPath { get; set; }
    }

    [Verb("delete", HelpText = "Deletes a local updates repository")]
    public class DeleteRepositoryOptions : IRepositoryPathOption
    {
        [Option("repo-path", Required = false, HelpText = "Repo location, if not using the current directory")]
        public string RepositoryPath { get; set; }
    }

    [Verb("query", HelpText = "Queries update metadata in a local repository")]
    public class QueryRepositoryOptions : IRepositoryPathOption
    {
        [Option("repo-path", Required = false, HelpText = "Repo location, if not using the current directory")]
        public string RepositoryPath { get; set; }

        [Option("products", Required = true, HelpText = "Read products", SetName ="products")]
        public bool Products { get; set; }

        [Option("classifications", Required = true, HelpText = "Read classifications", SetName = "classifications")]
        public bool Classifications { get; set; }

        [Option("detectoids", Required = true, HelpText = "Read detectoids", SetName = "detectoids")]
        public bool Detectoids { get; set; }

        [Option("configuration", Required = true, HelpText = "Read configuration", SetName = "configuration")]
        public bool Configuration { get; set; }

        [Option("drivers", Required = true, HelpText = "Read drivers", SetName = "drivers")]
        public bool Drivers { get; set; }

        [Option("updates", Required = true, HelpText = "Read updates", SetName = "updates")]
        public bool Updates { get; set; }

        [Option("id-filter", Required = false, HelpText = "ID filter")]
        public string IdFilter { get; set; }

        [Option("title-filter", Required = false, HelpText = "Title filter")]
        public string TitleFilter { get; set; }
    }

    [Verb("export", HelpText = "Export update metadata from the repository.")]
    public class RepositoryExportOptions : IRepositoryPathOption
    {
        [Option("repo-path", Required = false, HelpText = "Repo location, if not using the default one")]
        public string RepositoryPath { get; set; }

        [Option("export-file", Required = true, HelpText = "File where to export updates. If the file exists, it will be overwritten.")]
        public string ExportFile { get; set; }

        [Option("drivers", Required = false, HelpText = "Exports driver updates", SetName = "drivers")]
        public bool Drivers { get; set; }

        [Option("id-filter", Required = false, HelpText = "ID filter")]
        public string IdFilter { get; set; }

        [Option("title-filter", Required = false, HelpText = "Title filter")]
        public string TitleFilter { get; set; }
    }
}
