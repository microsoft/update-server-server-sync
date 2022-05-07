
using System;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;
using Microsoft.PackageGraph.MicrosoftUpdate.Source;
using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Storage.Local;

namespace Microsoft.PackageGraph.Samples
{
    internal class Program
    {
        static void Main(string[] _)
        {
            GetAvailableUpdatesForWindows();

            PrintSupersededUpdates();

            DownloadUpdateContent();
        }

        private static void GetAvailableUpdatesForWindows()
        {
            // Create a categories source from the Microsoft Update Catalog
            UpstreamCategoriesSource categoriesSource = new(Endpoint.Default);

            // Create a local store to save categories and updates locally
            using var packageStore = PackageStore.OpenOrCreate("./store");
            categoriesSource.MetadataCopyProgress += PackageStore_MetadataCopyProgress;

            // Copy categories from the upstream source to the local store
            Console.WriteLine("Fetching categories from upstream and saving them to the local store...");
            categoriesSource.CopyTo(packageStore, CancellationToken.None);
            Console.WriteLine();
            Console.WriteLine($"Copied {packageStore.GetPendingPackages().Count} new categories");

            // Flush not required; done here for demonstration purposes to clear the pending package count
            packageStore.Flush();

            // Create a filter to retrieve selected updates by product name
            var updatesFilter = new UpstreamSourceFilter();

            // Set a "windows 11" product filter.
            // First find the "Windows" product
            var windowsProduct = packageStore
                .OfType<ProductCategory>()
                .First(category => category.Title.Equals("Windows"));
            // Find the "Windows 11" product that is a child of "Windows"
            var windows11Product = packageStore
                .OfType<ProductCategory>()
                .First(category => category.Categories.Contains(windowsProduct.Id.ID) && 
                category.Title.Equals("Windows 11"));
            updatesFilter.ProductsFilter.Add(windows11Product.Id.ID);

            // Allow all available update classifications for the product selected
            updatesFilter
                .ClassificationsFilter
                .AddRange(packageStore.OfType<ClassificationCategory>().Select(classification => classification.Id.ID));
            Console.WriteLine($"Filtering to product \"{windows11Product.Title}\", all  classifications.");

            // Create an upstream updates source from the Microsoft Update Catalog
            Console.WriteLine("Fetching matching updates from upstream and saving them to the local store...");
            UpstreamUpdatesSource updatesSource = new(Endpoint.Default, updatesFilter);
            updatesSource.MetadataCopyProgress += PackageStore_MetadataCopyProgress;

            // Copy updates from the upstream to the local store
            updatesSource.CopyTo(packageStore, CancellationToken.None);
            Console.WriteLine();
            Console.WriteLine($"Copied {packageStore.GetPendingPackages().Count} new updates");
        }

        private static void PrintSupersededUpdates()
        {
            // Open the local updates store
            using var packageStore = PackageStore.Open("./store");

            // Grab the first cumulative update that is superseded by another update
            var firstUpdateAvailable = packageStore
                .OfType<SoftwareUpdate>()
                .FirstOrDefault(update => update.IsSupersededBy?.Count > 0 && 
                update.Title.Contains("cumulative", StringComparison.OrdinalIgnoreCase));

            if (firstUpdateAvailable is null)
            {
                Console.WriteLine("No update in the store has been superseded");
                return;
            }

            Console.WriteLine($"Software update in the store: {firstUpdateAvailable.Title}");
            Console.WriteLine($"Superseded by:");
            foreach (var supersededUpdateId in firstUpdateAvailable.IsSupersededBy)
            {
                var supersededByUpdate = packageStore
                    .FirstOrDefault(update => update.Id == supersededUpdateId);
                if (supersededByUpdate is not null)
                {
                    Console.WriteLine($"    {supersededByUpdate.Title}");
                }
            }
        }

        private static void DownloadUpdateContent()
        {
            // Open the local updates store
            using var packageStore = PackageStore.Open("./store");

            // Grab the first update that has some content
            var updateWithContent = packageStore
                .OfType<SoftwareUpdate>()
                .FirstOrDefault(update => update.Files?.Count() > 0);

            if (updateWithContent is null)
            {
                Console.WriteLine("No update in the store has content");
                return;
            }

            var contentFileToDownload = updateWithContent.Files.First();
            Console.WriteLine($"Downloading {contentFileToDownload.FileName}, size {contentFileToDownload.Size}");

            var contentStore = new FileSystemContentStore("./content");
            contentStore.Progress += ContentStore_Progress;

            contentStore.Download(new List<IContentFile> { contentFileToDownload }, CancellationToken.None);
        }

        private static void ContentStore_Progress(object? sender, ObjectModel.ContentOperationProgress e)
        {
            Console.CursorLeft = 0;
            if (e.CurrentOperation == PackagesOperationType.DownloadFileProgress)
            {
                Console.Write($"Downloading update content {e.Current}/{e.Maximum}");
            }
            else if (e.CurrentOperation == PackagesOperationType.HashFileProgress)
            {
                Console.Write($"Hashing update content {e.Current}/{e.Maximum}");
            }
            else if (e.CurrentOperation == PackagesOperationType.DownloadFileEnd || e.CurrentOperation == PackagesOperationType.HashFileEnd)
            {
                Console.WriteLine();
            }
        }

        static void PackageStore_MetadataCopyProgress(object? sender, Microsoft.PackageGraph.Storage.PackageStoreEventArgs e)
        {
            Console.CursorLeft = 0;
            Console.Write($"Copying package metadata {e.Current}/{e.Total}");
        }

    }
}