// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.Storage.Blob;
using Microsoft.PackageGraph.Storage;
using Microsoft.PackageGraph.Storage.Local;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace Microsoft.PackageGraph.Utilitites.Upsync
{
    public class MetadataStoreOptions : IMetadataStoreOptions
    {
        public string Alias { get; set; }
        public string Path { get; set; }

        public string Type { get; set; }

        public string StoreConnectionString { get; set; }
    }

    class MetadataStoreCreator
    {
        private const string StoreAliasesConfigFile = "store-aliases.json";

        public static void CreateAlias(StoreAliasCreateOptions storeOptions)
        {
            List<StoreAliasCreateOptions> storeAliases = LoadStoreAliases(StoreAliasesConfigFile);

            storeAliases.RemoveAll(alias => alias.Alias == storeOptions.Alias);

            var store = CreateFromOptions(
                new MetadataStoreOptions()
                {
                    Path = storeOptions.Path,
                    StoreConnectionString = storeOptions.StoreConnectionString,
                    Type = storeOptions.Type
                });
            if (store != null)
            {
                storeAliases.Add(storeOptions);
                File.WriteAllText(StoreAliasesConfigFile, JsonConvert.SerializeObject(storeAliases));
            }
        }

        public static void DeleteAlias(StoreAliasDeleteOptions options)
        {
            List<StoreAliasCreateOptions> storeAliases = LoadStoreAliases(StoreAliasesConfigFile);
            if (options.All)
            {
                File.Delete(StoreAliasesConfigFile);
                Console.WriteLine("All store aliases have been deleted!");
            }
            else
            {
                if (storeAliases.RemoveAll(alias => alias.Alias == options.Alias) > 0)
                {
                    File.WriteAllText(StoreAliasesConfigFile, JsonConvert.SerializeObject(storeAliases));
                    Console.WriteLine($"Alias {options.Alias} deleted");
                }
                else
                {
                    Console.WriteLine($"Alias {options.Alias} not found");
                }
            }
        }

        public static void ListAliases(StoreAliasListOptions options)
        {
            List<StoreAliasCreateOptions> storeAliases = LoadStoreAliases(StoreAliasesConfigFile);
            var aliasesToList = 
                string.IsNullOrEmpty(options.Alias) ? 
                storeAliases : 
                storeAliases.Where(alias => alias.Alias == options.Alias);
            

            if (aliasesToList.Any())
            {
                foreach (var alias in storeAliases)
                {
                    Console.WriteLine($"Alias            : {alias.Alias}");
                    Console.WriteLine($"Path             : {alias.Path}");
                    Console.WriteLine($"Type             : {alias.Type}");
                    Console.WriteLine($"Connection string: {alias.StoreConnectionString}");
                }
            }
            else
            {
                Console.WriteLine($"No aliases found");
            }
        }

        private static List<StoreAliasCreateOptions> LoadStoreAliases(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    return JsonConvert.DeserializeObject<List<StoreAliasCreateOptions>>(File.ReadAllText(path));
                }
                catch (Exception) { }
            }

            return new List<StoreAliasCreateOptions>();
        }

        public static IMetadataStore OpenFromOptions(IMetadataStoreOptions sourceOptions)
        {
            if (!string.IsNullOrEmpty(sourceOptions.Alias))
            {
                List<StoreAliasCreateOptions> storeAliases = LoadStoreAliases(StoreAliasesConfigFile);
                var alias = sourceOptions.Alias;
                sourceOptions = storeAliases.FirstOrDefault(alias => alias.Alias == sourceOptions.Alias);
                if (sourceOptions == null)
                {
                    Console.WriteLine($"Alias {alias} not found");
                    return null;
                }
            }

            IMetadataStore source = null;
            if (!Console.IsOutputRedirected)
            {
                Console.Write($"Opening package source [{sourceOptions.Path}] ");
            }

            if (sourceOptions.Type == "local")
            {
                try
                {
                    source = PackageStore.Open(sourceOptions.Path);
                    if (!Console.IsOutputRedirected)
                    {
                        ConsoleOutput.WriteGreen("Done!");
                    }

                    if (source.IsReindexingRequired)
                    {
                        ConsoleOutput.WriteRed("Warning: Package source must be reindexed!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    ConsoleOutput.WriteRed($"Cannot open the package store: {ex.Message}");
                }
            }
            else if (sourceOptions.Type == "azure-blob")
            {
                if (string.IsNullOrEmpty(sourceOptions.StoreConnectionString))
                {
                    var azureContainer = new CloudBlobContainer(new Uri(sourceOptions.Path));
                    return Microsoft.PackageGraph.Storage.Azure.PackageStore.Open(azureContainer);
                }
                else if (Azure.Storage.CloudStorageAccount.TryParse(sourceOptions.StoreConnectionString, out var storageAccount))
                {
                    var blobClient = storageAccount.CreateCloudBlobClient();
                    return Microsoft.PackageGraph.Storage.Azure.PackageStore.Open(blobClient, sourceOptions.Path);
                }
                else
                {
                    ConsoleOutput.WriteRed($"The connection string is invalid: {sourceOptions.StoreConnectionString}");
                    return null;
                }
            }
            else
            {
                ConsoleOutput.WriteRed($"Unknown store type {sourceOptions.Type}");
            }

            return source;
        }

        public static IMetadataStore CreateFromOptions(IMetadataStoreOptions sourceOptions)
        {
            if (!string.IsNullOrEmpty(sourceOptions.Alias))
            {
                List<StoreAliasCreateOptions> storeAliases = LoadStoreAliases(StoreAliasesConfigFile);
                var alias = sourceOptions.Alias;
                sourceOptions = storeAliases.FirstOrDefault(alias => alias.Alias == sourceOptions.Alias);
                if (sourceOptions == null)
                {
                    Console.WriteLine($"Alias {alias} not found");
                    return null;
                }
            }

            IMetadataStore source = null;
            Console.Write($"Creating package source [{sourceOptions.Path}] ");

            if (sourceOptions.Type == "local")
            {
                try
                {
                    source = PackageStore.OpenOrCreate(sourceOptions.Path);
                    ConsoleOutput.WriteGreen("Done!");

                    if (source.IsReindexingRequired)
                    {
                        ConsoleOutput.WriteRed("Warning: Package source must be reindexed!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    ConsoleOutput.WriteRed($"Cannot open the package store: {ex.Message}");
                }
            }
            else if (sourceOptions.Type == "azure-blob")
            {
                if (string.IsNullOrEmpty(sourceOptions.StoreConnectionString))
                {
                    ConsoleOutput.WriteRed("The connection string is missing. Use --azure-connection-string to set it");
                    return null;
                }

                if (Azure.Storage.CloudStorageAccount.TryParse(sourceOptions.StoreConnectionString, out var storageAccount))
                {
                    var blobClient = storageAccount.CreateCloudBlobClient();
                    return Microsoft.PackageGraph.Storage.Azure.PackageStore.OpenOrCreate(blobClient, sourceOptions.Path);
                }
                else
                {
                    ConsoleOutput.WriteRed($"The connection string is invalid: {sourceOptions.StoreConnectionString}");
                    return null;
                }
            }
            else
            {
                ConsoleOutput.WriteRed($"Unknown store type {sourceOptions.Type}");
            }

            return source;
        }

        
    }
}
