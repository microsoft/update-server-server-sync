// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.PackageGraph.Storage
{
    interface ISimpleMetadataIndex<I, T>
    {
        bool TryGet(I key, out T value);
    }

    interface IListMetadataIndex<T>
    {
        bool TryGet(int packageIndex, out List<T> entry);
    }
}
