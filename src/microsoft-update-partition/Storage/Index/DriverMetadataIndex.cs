// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Drivers;
using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Storage;
using Microsoft.PackageGraph.Storage.Index;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Index
{
    class DriverMetadataIndex : SimpleIndex<int, List<DriverMetadata>>, ISimpleMetadataIndex<int, List<DriverMetadata>>
    {
        public const string Name = AvailableIndexes.DriverMetadataIndexName;

        public override IndexDefinition Definition => MicrosoftUpdatePartitionRegistration.DriverMetadata;

        public DriverMetadataIndex(IIndexContainer container) : base(container, AvailableIndexes.DriverMetadataIndexName, MicrosoftUpdatePartitionRegistration.MicrosoftUpdatePartitionName)
        {
        }

        public override void IndexPackage(IPackage package, int packageIndex)
        {
            if (package is DriverUpdate driverUpdate)
            {
                var driverMetadata = driverUpdate.GetDriverMetadata().ToList();
                if (driverMetadata != null && driverMetadata.Count > 0)
                {
                    Add(packageIndex, driverMetadata);
                }
            }
        }
    }
}
