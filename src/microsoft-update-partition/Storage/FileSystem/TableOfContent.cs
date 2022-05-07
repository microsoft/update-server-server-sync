// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.PackageGraph.Storage.Local
{
    class TableOfContent
    {
        public int TocVersion;
        public int DeltaSectionCount;
        public List<long> DeltaSectionPackageCount;

        [JsonIgnore]
        public const int CurrentVersion = 0;
    }
}
