// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.Storage.Index;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.PackageGraph.Storage.Local
{
    class IndexTableOfContents
    {
        public int Version;

        public List<IndexDefinition> ContainedIndexes;

        [JsonIgnore]
        public const int CurrentVersion = 0;
    }
}
