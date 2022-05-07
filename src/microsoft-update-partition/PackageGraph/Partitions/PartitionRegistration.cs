// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.MicrosoftUpdate;
using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Storage.Index;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PackageGraph.Partitions
{
    class PartitionRegistration
    {
        internal static Dictionary<string, PartitionDefinition> KnownPartitionsIndex =
            new()
            {
                {
                    "",
                    new PartitionDefinition()
                    {
                        Factory = null,
                        HasExternalContentFileMetadata = false,
                        Indexes = new List<IndexDefinition>() { TitlesIndex.TitlesIndexDefinition },
                        HandlesIdentities = false,
                    }
                },
                {
                    MicrosoftUpdatePartitionRegistration.MicrosoftUpdatePartitionName,
                    new()
                    {
                        Name = MicrosoftUpdatePartitionRegistration.MicrosoftUpdatePartitionName,
                        Factory = MicrosoftUpdatePartitionRegistration.PartitionSingleton,
                        HasExternalContentFileMetadata = true,
                        Indexes = new List<IndexDefinition>() 
                        {
                            MicrosoftUpdatePartitionRegistration.KbArticle ,
                            MicrosoftUpdatePartitionRegistration.DriverMetadata,
                            MicrosoftUpdatePartitionRegistration.IsSuperseded,
                            MicrosoftUpdatePartitionRegistration.IsSuperseding,
                            MicrosoftUpdatePartitionRegistration.IsBundle,
                            MicrosoftUpdatePartitionRegistration.BundledWith,
                            MicrosoftUpdatePartitionRegistration.Prerequisites,
                            MicrosoftUpdatePartitionRegistration.Categories,
                            MicrosoftUpdatePartitionRegistration.Files
                        }
                    }
                }
            };

        public static void RegisterPartition(PartitionDefinition partitionDefinition)
        {
            lock(KnownPartitionsIndex)
            {
                KnownPartitionsIndex.Add(partitionDefinition.Name, partitionDefinition);
            }
        }

        public static bool TryGetPartition(string partitionName, out PartitionDefinition partitionDefinition)
        {
            lock(KnownPartitionsIndex)
            {
                return KnownPartitionsIndex.TryGetValue(partitionName, out partitionDefinition);
            }
        }

        public static bool TryGetPartitionFromPackage(IPackage package, out PartitionDefinition partitionDefinition)
        {
            lock (KnownPartitionsIndex)
            {
                return KnownPartitionsIndex.TryGetValue(package.Id.Partition, out partitionDefinition);
            }
        }

        public static bool TryGetPartitionFromPackageId(IPackageIdentity packageId, out PartitionDefinition partitionDefinition)
        {
            lock (KnownPartitionsIndex)
            {
                return KnownPartitionsIndex.TryGetValue(packageId.Partition, out partitionDefinition);
            }
        }

        public static List<PartitionDefinition> GetAllPartitions()
        {
            lock (KnownPartitionsIndex)
            {
                return KnownPartitionsIndex.Values.ToList();
            }
        }

        public static IPackage TryCreatePackageFromUri(string sourceUri)
        {
            foreach(var partition in KnownPartitionsIndex.Values)
            {
                if (partition.Factory.CanCreatePackageFromSource(sourceUri))
                {
                    return partition.Factory.FromSource(sourceUri);
                }
            }

            return null;
        }
    }
}
