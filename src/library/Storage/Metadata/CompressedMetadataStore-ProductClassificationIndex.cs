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
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Storage
{
    public partial class CompressedMetadataStore
    {
        private Dictionary<int, List<Guid>> UpdateAndProductIndex { get; set; }

        private Dictionary<int, List<Guid>> UpdateAndClassificationIndex { get; set; }

        private const string ProductClassificationEntryName = "product-classification.json";

        /// <summary>
        /// Saves the product and classification information to the metadata source
        /// </summary>
        private void SaveProductClassificationIndex()
        {
            CommitProgress?.Invoke(this, new OperationProgress() { CurrentOperation = OperationType.IndexingCategoriesStart });
            ResolveProductsAndClassifications();

            SerializeIndexToArchive(
                ProductClassificationEntryName,
                new KeyValuePair<Dictionary<int, List<Guid>>, Dictionary<int, List<Guid>>>(
                    UpdateAndProductIndex,
                    UpdateAndClassificationIndex));
            CommitProgress?.Invoke(this, new OperationProgress() { CurrentOperation = OperationType.IndexingCategoriesEnd });
        }

        private void ReadProductClassificationIndex()
        {
            var deserializedPair = DeserializeIndexFromArchive<KeyValuePair<Dictionary<int, List<Guid>>, Dictionary<int, List<Guid>>>>(
                ProductClassificationEntryName);

            UpdateAndProductIndex = deserializedPair.Key;
            UpdateAndClassificationIndex = deserializedPair.Value;
        }

        private void OnNewStore_InitializeProductClassification()
        {
            UpdateAndProductIndex = new Dictionary<int, List<Guid>>();
            UpdateAndClassificationIndex = new Dictionary<int, List<Guid>>();
        }

        private void OnDeltaStore_InitializeProductClassification()
        {
            UpdateAndProductIndex = new Dictionary<int, List<Guid>>();
            UpdateAndClassificationIndex = new Dictionary<int, List<Guid>>();
        }

        private void ResolveProductsAndClassifications()
        {
            var progress = new OperationProgress() { CurrentOperation = OperationType.IndexingCategoriesProgress, Maximum = AddedUpdates.Count };

            var productsList = Categories.Values.OfType<Product>().Select(p => p.Identity).ToList();
            var classificationsList = Categories.Values.OfType<Classification>().Select(c => c.Identity).ToHashSet();

            // Fill in product and classification information.
            foreach (var updateEntry in AddedUpdates)
            {
                if (AddedPrerequisites.ContainsKey(updateEntry.Value.Identity))
                {
                    // Find product information and add it to the index
                    var updateProductsList = CategoryResolver.ResolveProductFromPrerequisites(AddedPrerequisites[updateEntry.Value.Identity], productsList);
                    if (updateProductsList.Count > 0)
                    {
                        UpdateAndProductIndex.Add(updateEntry.Key, updateProductsList);
                    }

                    // Find classification information and add it to the index
                    var updateClassificationsList = CategoryResolver.ResolveClassificationFromPrerequisites(AddedPrerequisites[updateEntry.Value.Identity], classificationsList);
                    if (updateClassificationsList.Count > 0)
                    {
                        UpdateAndClassificationIndex.Add(updateEntry.Key, updateClassificationsList);
                    }
                }

                progress.Current++;
                if (progress.Current % 1000 == 0)
                {
                    CommitProgress?.Invoke(this, progress);
                }
            }
        }

        private void EnsureProductClassificationIndexLoaded()
        {
            lock (this)
            {
                // lazy load the categories and products index
                if (UpdateAndProductIndex == null)
                {
                    ReadProductClassificationIndex();
                }
            }
        }

        /// <summary>
        /// Gets an updates's product IDs
        /// </summary>
        /// <param name="updateIdentity">The update ID to get products Ids for</param>
        /// <returns>List of product ids for an update</returns>
        public List<Guid> GetUpdateProductIds(Identity updateIdentity)
        {
            return GetUpdateProductIds(this[updateIdentity]);
        }

        private List<Guid> GetUpdateProductIds(int updateIndex)
        {
            EnsureProductClassificationIndexLoaded();

            if (UpdateAndProductIndex.ContainsKey(updateIndex))
            {
                return UpdateAndProductIndex[updateIndex];
            }
            else if (IsInBaseline(updateIndex))
            {
                return BaselineSource.GetUpdateProductIds(updateIndex);
            }
            else
            {
                throw new Exception($"Update {this[updateIndex]} does not have product information");
            }
        }

        /// <summary>
        /// Gets an updates's classification IDs
        /// </summary>
        /// <param name="updateIdentity">The update ID to get classification Ids for</param>
        /// <returns>List of classification ids for an update</returns>
        public List<Guid> GetUpdateClassificationIds(Identity updateIdentity)
        {
            return GetUpdateClassificationIds(this[updateIdentity]);
        }

        private List<Guid> GetUpdateClassificationIds(int updateIndex)
        {
            EnsureProductClassificationIndexLoaded();

            if (UpdateAndClassificationIndex.ContainsKey(updateIndex))
            {
                return UpdateAndClassificationIndex[updateIndex];
            }
            else if (IsInBaseline(updateIndex))
            {
                return BaselineSource.GetUpdateClassificationIds(updateIndex);
            }
            else
            {
                throw new Exception($"Update {this[updateIndex]} does not have classifications");
            }
        }

        /// <summary>
        /// Check if this update has a parent product
        /// </summary>
        /// <param name="updateIdentity">The update to check</param>
        /// <returns>True if the update has a parent product, false otherwise</returns>
        public bool HasProduct(Identity updateIdentity)
        {
            return HasProduct(this[updateIdentity]);
        }

        private bool HasProduct(int updateIndex)
        {
            EnsureProductClassificationIndexLoaded();

            if (UpdateAndProductIndex.ContainsKey(updateIndex))
            {
                return true;
            }
            else if (IsInBaseline(updateIndex))
            {
                return BaselineSource.HasProduct(updateIndex);
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Check if this update has a classification
        /// </summary>
        /// <param name="updateIdentity">The update to check classifications</param>
        /// <returns>True if the update has classifications, false otherwise</returns>
        public bool HasClassification(Identity updateIdentity)
        {
            return HasClassification(this[updateIdentity]);
        }

        private bool HasClassification(int updateIndex)
        {
            EnsureProductClassificationIndexLoaded();

            if (UpdateAndClassificationIndex.ContainsKey(updateIndex))
            {
                return true;
            }
            else if (IsInBaseline(updateIndex))
            {
                return BaselineSource.HasClassification(updateIndex);
            }
            else
            {
                return false;
            }
        }
    }
}
