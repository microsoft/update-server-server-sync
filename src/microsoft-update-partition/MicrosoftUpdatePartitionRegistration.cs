// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.MicrosoftUpdate.Index;
using Microsoft.PackageGraph.Partitions;
using Microsoft.PackageGraph.Storage.Index;
using System;
using System.Collections.Generic;

namespace Microsoft.PackageGraph.MicrosoftUpdate
{
    /// <summary>
    /// Registers Microsoft Update as a source for packages metadata.
    /// </summary>
    abstract class MicrosoftUpdatePartitionRegistration
    {
        internal const string MicrosoftUpdatePartitionName = "MicrosoftUpdate";

        internal static readonly MicrosoftUpdatePartition PartitionSingleton = new(); 

        internal static readonly IndexDefinition DriverMetadata = new()
        {
            Name = DriverMetadataIndex.Name,
            PartitionName = MicrosoftUpdatePartitionName,
            Version = DriverMetadataIndex.CurrentVersion,
            Tag = "stream",
            Factory = PartitionSingleton
        };

        internal static readonly IndexDefinition KbArticle = new()
        {
            Name = KbArticleIndex.Name,
            PartitionName = MicrosoftUpdatePartitionName,
            Version = KbArticleIndex.CurrentVersion,
            Tag = "stream",
            Factory = PartitionSingleton
        };

        internal static readonly IndexDefinition IsSuperseded = new()
        {
            Name = IsSupersededIndex.Name,
            PartitionName = MicrosoftUpdatePartitionName,
            Version = IsSupersededIndex.CurrentVersion,
            Tag = "stream",
            Factory = PartitionSingleton
        };

        internal static readonly IndexDefinition IsSuperseding = new()
        {
            Name = IsSupersedingIndex.Name,
            PartitionName = MicrosoftUpdatePartitionName,
            Version = IsSupersededIndex.CurrentVersion,
            Tag = "stream",
            Factory = PartitionSingleton
        };

        internal static readonly IndexDefinition IsBundle = new()
        {
            Name = IsBundleIndex.Name,
            PartitionName = MicrosoftUpdatePartitionName,
            Version = IsSupersededIndex.CurrentVersion,
            Tag = "stream",
            Factory = PartitionSingleton
        };

        internal static readonly IndexDefinition BundledWith = new()
        {
            Name = BundledWithIndex.Name,
            PartitionName = MicrosoftUpdatePartitionName,
            Version = IsSupersededIndex.CurrentVersion,
            Tag = "stream",
            Factory = PartitionSingleton
        };

        internal static readonly IndexDefinition Prerequisites = new()
        {
            Name = PrerequisitesIndex.Name,
            PartitionName = MicrosoftUpdatePartitionName,
            Version = IsSupersededIndex.CurrentVersion,
            Tag = "stream",
            Factory = PartitionSingleton
        };

        internal static readonly IndexDefinition Categories = new()
        {
            Name = CategoriesIndex.Name,
            PartitionName = MicrosoftUpdatePartitionName,
            Version = IsSupersededIndex.CurrentVersion,
            Tag = "stream",
            Factory = PartitionSingleton
        };

        internal static readonly IndexDefinition Files = new()
        {
            Name = FilesIndex.Name,
            PartitionName = MicrosoftUpdatePartitionName,
            Version = FilesIndex.CurrentVersion,
            Tag = "stream",
            Factory = PartitionSingleton
        };
    }
}
