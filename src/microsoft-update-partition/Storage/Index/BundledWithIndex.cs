// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;
using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Storage;
using Microsoft.PackageGraph.Storage.Index;
using System.Collections.Generic;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Index
{
    class BundledWithIndex : ListIndex<MicrosoftUpdatePackageIdentity, int>, ISimpleMetadataIndex<MicrosoftUpdatePackageIdentity, List<int>>
    {
        public const string Name = AvailableIndexes.BundledWithIndexName;

        public override IndexDefinition Definition => MicrosoftUpdatePartitionRegistration.BundledWith;

        public BundledWithIndex(IIndexContainer container) : base(container, Name, MicrosoftUpdatePartitionRegistration.MicrosoftUpdatePartitionName)
        {
            SaveWithSwappedKeyValue = true;
        }

        public override void IndexPackage(IPackage package, int packageIndex)
        {
            if (package is SoftwareUpdate softwareUpdate)
            {
                if (softwareUpdate.BundledUpdates != null && softwareUpdate.BundledUpdates.Count > 0)
                {
                    foreach (var bundledUpdate in softwareUpdate.BundledUpdates)
                    {
                        base.Add(bundledUpdate, packageIndex);
                    }
                }
            }
        }
    }
}
