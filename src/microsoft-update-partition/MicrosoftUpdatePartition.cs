// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.MicrosoftUpdate.Index;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;
using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Partitions;
using Microsoft.PackageGraph.Storage;
using Microsoft.PackageGraph.Storage.Index;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.PackageGraph.MicrosoftUpdate
{
    class MicrosoftUpdatePartition : IPackageFactory, IIndexFactory
    {
        public MicrosoftUpdatePartition()
        {

        }

        public IEnumerable<KeyValuePair<int, IPackageIdentity>> FilterPartitionIdentities(Dictionary<int, IPackageIdentity> packageIdentities)
        {
            return packageIdentities.Where(pair => pair.Value is MicrosoftUpdatePackageIdentity);
        }

        public IPackage FromStore(int updateType, IPackageIdentity id, IMetadataLookup store, IMetadataSource metadataSource)
        {
            if (id is MicrosoftUpdatePackageIdentity microsoftUpdateIdentity)
            {
                return MicrosoftUpdatePackage.FromTypeAndStore((StoredPackageType)updateType, microsoftUpdateIdentity, store, metadataSource);
            }

            throw new NotImplementedException();
        }

        public IPackage FromStream(Stream metadataStream, IMetadataSource backingMetadataStore)
        {
            var rehydratedUpdate = MicrosoftUpdatePackage.FromStoredMetadataXml(metadataStream, backingMetadataStore) as MicrosoftUpdatePackage;

            return rehydratedUpdate;
        }

        public int GetPackageType(IPackage package)
        {
            if (package is ProductCategory)
            {
                return (int)StoredPackageType.MicrosoftUpdateProduct;
            }
            else if (package is ClassificationCategory)
            {
                return (int)StoredPackageType.MicrosoftUpdateClassification;
            }
            else if (package is DetectoidCategory)
            {
                return (int)StoredPackageType.MicrosoftUpdateDetectoid;
            }
            else if (package is SoftwareUpdate)
            {
                return (int)StoredPackageType.MicrosoftUpdateSoftware;
            }
            else if (package is DriverUpdate)
            {
                return (int)StoredPackageType.MicrosoftUpdateDriver;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<KeyValuePair<int, IPackageIdentity>> IdentitiesFromJson(StreamReader jsonStream)
        {
            var deserializer = new JsonSerializer();
            var deserializedList = deserializer.Deserialize(
                jsonStream,
                typeof(List<KeyValuePair<int, MicrosoftUpdatePackageIdentity>>)) as List<KeyValuePair<int, MicrosoftUpdatePackageIdentity>>;

            return deserializedList
                .Select(pair => new KeyValuePair<int, IPackageIdentity>(pair.Key, pair.Value as IPackageIdentity));
        }

        public IPackageIdentity IdentityFromString(string packageIdentityString)
        {
            return MicrosoftUpdatePackageIdentity.FromString(packageIdentityString);
        }

        public IIndex CreateIndex(IndexDefinition definition, IIndexContainer container)
        {
            return definition.Name switch
            {
                DriverMetadataIndex.Name => new DriverMetadataIndex(container),
                KbArticleIndex.Name => new KbArticleIndex(container),
                IsSupersededIndex.Name => new IsSupersededIndex(container),
                IsSupersedingIndex.Name => new IsSupersedingIndex(container),
                BundledWithIndex.Name => new BundledWithIndex(container),
                IsBundleIndex.Name => new IsBundleIndex(container),
                PrerequisitesIndex.Name => new PrerequisitesIndex(container),
                CategoriesIndex.Name => new CategoriesIndex(container),
                FilesIndex.Name => new FilesIndex(container),
                _ => throw new NotImplementedException(),
            };
        }

        public IPackage FromSource(string sourceUri)
        {
            throw new NotSupportedException();
        }

        public bool CanCreatePackageFromSource(string sourceUri)
        {
            return false;
        }
    }
}
