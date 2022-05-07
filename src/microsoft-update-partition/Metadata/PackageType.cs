// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.PackageGraph.MicrosoftUpdate
{
    [JsonConverter(typeof(StringEnumConverter))]
    enum StoredPackageType : int
    {
        MicrosoftUpdateDetectoid = 0,
        MicrosoftUpdateClassification,
        MicrosoftUpdateProduct,
        MicrosoftUpdateSoftware,
        MicrosoftUpdateDriver
    }
}
