using Microsoft.PackageGraph.ObjectModel;

namespace Microsoft.PackageGraph.Storage.Index
{
	class DescriptionsIndex : SimpleIndex<int, string>, ISimpleMetadataIndex<int, string>
	{
		internal static readonly IndexDefinition DescriptionsIndexDefinition =
			new()
			{
				Name = AvailableIndexes.DescriptionsIndexName,
				PartitionName = null,
				Version = DescriptionsIndex.CurrentVersion,
				Factory = new InternalIndexFactory(),
				Tag = "stream"
			};

		public override IndexDefinition Definition => DescriptionsIndexDefinition;

		public DescriptionsIndex(IIndexContainer container) : base(container, AvailableIndexes.TitlesIndexName)
		{
		}

		public override void IndexPackage(IPackage package, int packageIndex)
		{
			Add(packageIndex, package.Description);
		}

		public new bool TryGet(int packageIndex, out string description)
		{
			return base.TryGet(packageIndex, out description);
		}
	}
}
