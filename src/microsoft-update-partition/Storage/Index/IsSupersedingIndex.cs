// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;
using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Storage;
using Microsoft.PackageGraph.Storage.Index;
using System;
using System.Collections.Generic;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Index
{
    class IsSupersedingIndex : SimpleIndex<int, List<Guid>>, ISimpleMetadataIndex<int, List<Guid>>
    {
        public const string Name = AvailableIndexes.IsSupersedingIndexName;

        public override IndexDefinition Definition => MicrosoftUpdatePartitionRegistration.IsSuperseding;

        public IsSupersedingIndex(IIndexContainer container) : base(container, Name, MicrosoftUpdatePartitionRegistration.MicrosoftUpdatePartitionName)
        {
        }

        public override void IndexPackage(IPackage package, int packageIndex)
        {
            if (package is SoftwareUpdate softwareUpdate)
            {
                var supersededUpdates = softwareUpdate.SupersededUpdates;
                if (supersededUpdates != null && supersededUpdates.Count > 0)
                {
                    base.Add(packageIndex, new List<Guid>(softwareUpdate.SupersededUpdates));
                }
            }
        }
    }
}
