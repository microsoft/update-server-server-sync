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
    class IsSupersededIndex : ListIndex<Guid, int>, ISimpleMetadataIndex<Guid, List<int>>
    {
        public const string Name = AvailableIndexes.IsSupersededIndexName;

        public override IndexDefinition Definition => MicrosoftUpdatePartitionRegistration.IsSuperseded;

        public IsSupersededIndex(IIndexContainer container) : base(container, Name, MicrosoftUpdatePartitionRegistration.MicrosoftUpdatePartitionName)
        {
        }

        public override void IndexPackage(IPackage package, int packageIndex)
        {
            if (package is SoftwareUpdate softwareUpdate)
            {
                if(softwareUpdate.SupersededUpdates != null && softwareUpdate.SupersededUpdates.Count > 0)
                {
                    foreach (var supersededUpdate in softwareUpdate.SupersededUpdates)
                    {
                        base.Add(supersededUpdate, packageIndex);
                    }
                }
            }
        }
    }
}
