// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.Partitions;
using Newtonsoft.Json;

namespace Microsoft.PackageGraph.Storage.Index
{
    class IndexDefinition
    {
        public string Name;

        public int Version;

        public string PartitionName;

        public string Tag;

        [JsonIgnore]
        public IIndexFactory Factory;

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }
            else if (obj is IndexDefinition other)
            {
                return this.Name == other.Name &&
                    this.PartitionName == other.PartitionName &&
                    this.Tag == other.Tag &&
                    this.Version == other.Version;

            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return (Name + PartitionName + Tag + Version.ToString()).GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
