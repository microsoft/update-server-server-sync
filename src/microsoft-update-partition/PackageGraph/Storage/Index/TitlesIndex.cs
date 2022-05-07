// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.ObjectModel;

namespace Microsoft.PackageGraph.Storage.Index
{
    class TitlesIndex : SimpleIndex<int, string>, ISimpleMetadataIndex<int, string>
    {
        internal static readonly IndexDefinition TitlesIndexDefinition =
            new()
            {
                Name = AvailableIndexes.TitlesIndexName,
                PartitionName = null,
                Version = TitlesIndex.CurrentVersion,
                Factory = new InternalIndexFactory(),
                Tag = "stream"
            };

        public override IndexDefinition Definition => TitlesIndexDefinition;

        public TitlesIndex(IIndexContainer container) : base(container, AvailableIndexes.TitlesIndexName)
        {
        }

        public override void IndexPackage(IPackage package, int packageIndex)
        {
            Add(packageIndex, package.Title);
        }

        public new bool TryGet(int packageIndex, out string title)
        {
            return base.TryGet(packageIndex, out title);
        }
    }
}
