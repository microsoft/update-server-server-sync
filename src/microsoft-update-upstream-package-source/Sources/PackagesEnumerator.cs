// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Storage;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.UpdateServices.MicrosoftUpdate.ServerServerSync.Sources
{
    class PackagesEnumerator : IEnumerator<IPackage>
    {
        readonly IMetadataStore _Source;
        readonly IEnumerator<IPackageIdentity> _IdentitiesEnumerator;

        public PackagesEnumerator(IEnumerable<IPackageIdentity> identityEnumerator, IMetadataStore categoriesSource)
        {
            _Source = categoriesSource;
            _IdentitiesEnumerator = identityEnumerator.GetEnumerator();
        }

        public IPackage Current => _Source.GetPackage(_IdentitiesEnumerator.Current);

        object IEnumerator.Current => _Source.GetPackage(_IdentitiesEnumerator.Current);

        public void Dispose()
        {
            
        }

        public bool MoveNext()
        {
            return _IdentitiesEnumerator.MoveNext();
        }

        public void Reset()
        {
            _IdentitiesEnumerator.Reset();
        }
    }
}
