// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Content;
using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Storage;
using Microsoft.PackageGraph.Storage.Index;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Index
{
    class FilesIndex : SimpleIndex<int, List<UpdateFile>>, ISimpleMetadataIndex<int, List<UpdateFile>>
    {
        public const string Name = AvailableIndexes.FilesIndexName;

        public override IndexDefinition Definition => MicrosoftUpdatePartitionRegistration.Files;

        public FilesIndex(IIndexContainer container) : base(container, Name, MicrosoftUpdatePartitionRegistration.MicrosoftUpdatePartitionName)
        {
        }

        public override void IndexPackage(IPackage package, int packageIndex)
        {
            if (package is MicrosoftUpdatePackage microsoftUpdatePackage &&
                microsoftUpdatePackage.Files != null &&
                microsoftUpdatePackage.Files.Any())
            {
                base.Add(packageIndex, microsoftUpdatePackage.Files.OfType<UpdateFile>().ToList());
            }
        }
    }
}
