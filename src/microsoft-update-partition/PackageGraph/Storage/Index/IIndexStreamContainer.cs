// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;

namespace Microsoft.PackageGraph.Storage.Index
{
    interface IIndexStreamContainer : IIndexContainer
    {
        bool TryGetIndexReadStream(IndexDefinition indexDefinition, out Stream indexStream);
    }
}
