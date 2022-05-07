// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.PackageGraph.Storage.Azure
{
    class PackageStoreEntry
    {
        [JsonConstructor]
        private PackageStoreEntry()
        {
        }

        public PackageStoreEntry(string id, int index)
        {
            PackageIndex = index;
            PackageId = id;
        }

        public string PackageId { get; set; }

        public string PartitionName { get; set; }

        public int PackageType { get; set; }

        public long MetadataOffset { get; set; }
        
        public long MetadataLength { get; set; }

        public long FileListOffset { get; set; }

        public long FileListLength { get; set; }

        public long PackageIndex { get; set; }
    }
}
