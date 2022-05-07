// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.ObjectModel;
using System.IO;

namespace Microsoft.PackageGraph.Storage.Index
{
    interface IIndex
    {
        void Save(Stream destination);

        bool IsDirty { get; }

        void IndexPackage(IPackage package, int packageIndex);

        IndexDefinition Definition { get; }
    }
}
