// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;
using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Storage;
using Microsoft.PackageGraph.Storage.Index;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Index
{
    class KbArticleIndex : SimpleIndex<int, string>, ISimpleMetadataIndex<int, string>
    {
        public const string Name = AvailableIndexes.KbArticleIndexName;

        public override IndexDefinition Definition => MicrosoftUpdatePartitionRegistration.KbArticle;

        public KbArticleIndex(IIndexContainer container) : base(container, Name, MicrosoftUpdatePartitionRegistration.MicrosoftUpdatePartitionName)
        {
        }

        public override void IndexPackage(IPackage package, int packageIndex)
        {
            if (package is SoftwareUpdate softwareUpdate)
            {
                if (!string.IsNullOrEmpty(softwareUpdate.KBArticleId))
                {
                    base.Add(packageIndex, softwareUpdate.KBArticleId);
                }
            }
        }
    }
}
