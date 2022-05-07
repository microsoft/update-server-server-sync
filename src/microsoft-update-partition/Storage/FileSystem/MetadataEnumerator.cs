// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.ObjectModel;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.PackageGraph.Storage.Local
{
    class LocalStoreEnumerator : IEnumerator<IPackage>
    {
        readonly IMetadataStore _Source;
        readonly IEnumerator<IPackageIdentity> IdentitiesEnumerator;

        public LocalStoreEnumerator(IEnumerable<IPackageIdentity> identityEnumerator, IMetadataStore packageSource)
        {
            _Source = packageSource;
            IdentitiesEnumerator = identityEnumerator.GetEnumerator();
        }

        public IPackage Current => _Source.GetPackage(IdentitiesEnumerator.Current);

        object IEnumerator.Current => _Source.GetPackage(IdentitiesEnumerator.Current);

        public void Dispose()
        {
            IdentitiesEnumerator.Dispose();
        }

        public bool MoveNext()
        {
            return IdentitiesEnumerator.MoveNext();
        }

        public void Reset()
        {
            IdentitiesEnumerator.Reset();
        }
    }
}
