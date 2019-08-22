// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ICSharpCode.SharpZipLib.Zip;
using Microsoft.UpdateServices.Client;
using Microsoft.UpdateServices.Compression;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.Metadata.Prerequisites;
using Microsoft.UpdateServices.Storage;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.UpdateServices.Storage
{
    public partial class CompressedMetadataStore
    {
        internal void OnDeserialized()
        {
            if (!IsDeltaSource)
            {
                // There is no baseline, so the index end is -1; all indexes will be considered in the delta
                BaselineIndexesEnd = -1;
                BaselineIdentities = new SortedSet<Identity>();

                // Rebuild index to identity and identity to index from the flat list
                IndexToIdentity = IdentityAndIndexList.ToDictionary(p => p.Key, p => p.Value);
                IdentityToIndex = IdentityAndIndexList.ToDictionary(p => p.Value, p => p.Key);
            }
            else
            {
                ValidateBaseline();

                // Merge baseline with delta indexes to create a complete index
                IdentityAndIndexList.AddRange(BaselineSource.IdentityAndIndexList);

                // Rebuild index to identity and identity to index from the flat list
                IndexToIdentity = IdentityAndIndexList.ToDictionary(p => p.Key, p => p.Value);
                IdentityToIndex = IdentityAndIndexList.ToDictionary(p => p.Value, p => p.Key);

                // Unlike BaselineIndexes, BaselineIdentities is not serialized to save space. Build it from BaselineIndexes
                BaselineIdentities = new SortedSet<Identity>(IndexToIdentity.Where(p => IsInBaseline(p.Key)).Select(p => p.Value));

                foreach(var typeEntry in BaselineSource.UpdateTypeMap)
                {
                    UpdateTypeMap.Add(typeEntry.Key, typeEntry.Value);
                }
            }

            Identities = new SortedSet<Identity>(IdentityToIndex.Keys);

            Updates = new ConcurrentDictionary<Identity, Update>();
            Categories = new ConcurrentDictionary<Identity, Update>();

            // Create update placeholders
            InstantiateUpdatePlaceholders();

            // Populate indexes
            UpdatesIndex = Updates;
            CategoriesIndex = Categories;
            ProductsIndex = Categories.Values.OfType<Product>().ToDictionary(p => p.Identity);
            ClassificationsIndex = Categories.Values.OfType<Classification>().ToDictionary(c => c.Identity);
            DetectoidsIndex = Categories.Values.OfType<Detectoid>().ToDictionary(d => d.Identity);
        }

        [OnSerializing]
        internal void OnSerializingMethod(StreamingContext context)
        {
            ComputeChecksum();

            if (IsDeltaSource)
            {
                // Do not save identities that are present in the baseline
                IdentityAndIndexList = IndexToIdentity.Where(p => !IsInBaseline(p.Key)).ToList();

                for(int i = 0; i <= BaselineIndexesEnd; i++)
                {
                    UpdateAndProductIndex.Remove(i);
                    UpdateAndClassificationIndex.Remove(i);
                }

                foreach(var typeEntry in BaselineSource.UpdateTypeMap)
                {
                    if (UpdateTypeMap.ContainsKey(typeEntry.Key))
                    {
                        UpdateTypeMap.Remove(typeEntry.Key);
                    }
                }
            }
            else
            {
                IdentityAndIndexList = IndexToIdentity.ToList();
            }
        }

        /// <summary>
        /// Create Update objects for all updates in this source; no metadata is loaded at this time, and each update
        /// is given a pointer to this source so it can load metadata on demand
        /// </summary>
        private void InstantiateUpdatePlaceholders()
        {
            // Create update objects; the updates metadata is loaded on demand when update attributes are accessed
            UpdateTypeMap.AsParallel().ForAll(updateTypeEntry =>
            {
                var identity = IndexToIdentity[updateTypeEntry.Key];
                UpdateType updateType = (UpdateType)updateTypeEntry.Value;

                switch (updateType)
                {
                    case UpdateType.Software:
                        Updates.TryAdd(identity, new SoftwareUpdate(identity, IsInBaseline(updateTypeEntry.Key) ? BaselineSource : this));
                        break;

                    case UpdateType.Driver:
                        Updates.TryAdd(identity, new DriverUpdate(identity, IsInBaseline(updateTypeEntry.Key) ? BaselineSource : this));
                        break;

                    case UpdateType.Detectoid:
                        Categories.TryAdd(identity, new Detectoid(identity, IsInBaseline(updateTypeEntry.Key) ? BaselineSource : this));
                        break;

                    case UpdateType.Classification:
                        Categories.TryAdd(identity, new Classification(identity, IsInBaseline(updateTypeEntry.Key) ? BaselineSource : this));
                        break;

                    case UpdateType.Product:
                        Categories.TryAdd(identity, new Product(identity, IsInBaseline(updateTypeEntry.Key) ? BaselineSource : this));
                        break;
                }
            });
        }
    }
}
