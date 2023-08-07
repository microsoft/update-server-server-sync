using Microsoft.PackageGraph.ObjectModel;

namespace Microsoft.PackageGraph.Storage.Index
{
	class CreationDatesIndex : SimpleIndex<int, string>, ISimpleMetadataIndex<int, string>
	{
		internal static readonly IndexDefinition CreationDatesIndexDefinition =
			new()
			{
				Name = AvailableIndexes.CreationDatesIndexName,
				PartitionName = null,
				Version = CreationDatesIndex.CurrentVersion,
				Factory = new InternalIndexFactory(),
				Tag = "stream"
			};

		public override IndexDefinition Definition => CreationDatesIndexDefinition;

		public CreationDatesIndex(IIndexContainer container) : base(container, AvailableIndexes.CreationDatesIndexName)
		{
		}

		public override void IndexPackage(IPackage package, int packageIndex)
		{
			Add(packageIndex, package.CreationDate);
		}

		public new bool TryGet(int packageIndex, out string creationDate)
		{
			return base.TryGet(packageIndex, out creationDate);
		}
	}
}
