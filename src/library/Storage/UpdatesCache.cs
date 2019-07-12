// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UpdateServices.Query;
using Microsoft.UpdateServices.Metadata;
using Newtonsoft.Json;
using System.Linq;
using System.IO;
using Microsoft.UpdateServices.Metadata.Prerequisites;

namespace Microsoft.UpdateServices.Storage
{
    /// <summary>
    /// Stores update metadata locally, together with any anchors and filters
    /// used to retrieve the metadata
    /// </summary>
    internal class UpdatesCache
    {
        /// <summary>
        /// Dictionary of known updates
        /// </summary>
        [JsonIgnore]
        public Dictionary<Identity, Update> Index { get; protected set; }

        /// <summary>
        /// List of known updates
        /// </summary>
        [JsonIgnore]
        public IEnumerable<Update> Updates => Index.Values;

        /// <summary>
        /// List of known updates; only used to serialize the dictionary above as a flat list
        /// </summary>
        [JsonProperty]
        private List<Update> UpdatesList { get; set; }

        /// <summary>
        /// List of update queries used. Each update query contains the filters used and the anchor associated
        /// with the filter
        /// </summary>
        [JsonProperty]
        public List<QueryFilter> UpdateQueries { get; private set; }

        /// <summary>
        /// The version of this object. Used to compare against the current version when deserializing
        /// </summary>
        [JsonProperty]
        private int Version;

        /// <summary>
        /// The object version currently implemented by this code
        /// </summary>
        [JsonIgnore]
        const int CurrentVersion = 5;

        /// <summary>
        /// The updates repository that contains this object. Used when serializing-deserializing to 
        /// resolve paths
        /// </summary>
        [JsonIgnore]
        private IRepository ParentRepository;

        /// <summary>
        /// Private constructor used by the deserializer
        /// </summary>
        [JsonConstructor]
        private UpdatesCache()
        { }

        /// <summary>
        /// Internal constructor called when a new updates repository is created
        /// </summary>
        /// <param name="parentRepository"></param>
        internal UpdatesCache(IRepository parentRepository)
        {
            Index = new Dictionary<Identity, Update>();
            UpdateQueries = new List<QueryFilter>();
            ParentRepository = parentRepository;
            Version = CurrentVersion;
        }

        /// <summary>
        /// Loads an updates cache from JSON
        /// </summary>
        /// <param name="jsonStream">The JSON stream to deserialize from</param>
        /// <param name="parentRepository">The repository that becomes the parent of the deserialized cache</param>
        /// <returns>Cached updates</returns>
        internal static UpdatesCache FromJson(StreamReader jsonStream, IRepository parentRepository)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new UpdateConverter());

            var deserializedCache = serializer.Deserialize(jsonStream, typeof(UpdatesCache)) as UpdatesCache;
            if (deserializedCache.Version == CurrentVersion)
            {
                // Re-create the updates dictionary from the serialized list of updates
                deserializedCache.Index = deserializedCache.UpdatesList.ToDictionary(u => u.Identity);

                // The updates list is only used to serialize updates to disk. For in-memory operations only
                // the dictionary is used
                deserializedCache.UpdatesList.Clear();
                deserializedCache.ParentRepository = parentRepository;
                return deserializedCache;
            }

            return new UpdatesCache(parentRepository);
        }

        /// <summary>
        /// Returns all updates in the repository that match the specified filter
        /// </summary>
        /// <param name="filter">The filter to apply</param>
        /// <returns>Updates that match the filter</returns>
        public List<Update> GetUpdates(RepositoryFilter filter)
        {
            var filteredUpdates = Index.Values.ToList();

            // Only consider filters with product and classification
            filteredUpdates.RemoveAll(u => !(u is IUpdateWithProduct) && !(u is IUpdateWithClassification));

            // Apply the classification filter
            foreach (var classificationId in filter.ClassificationFilter)
            {
                filteredUpdates.RemoveAll(u => !(u as IUpdateWithClassification).ClassificationIds.Contains(classificationId));
            }

            // Apply the product filter
            foreach (var productId in filter.ProductFilter)
            {
                filteredUpdates.RemoveAll(u => !(u as IUpdateWithProduct).ProductIds.Contains(productId));
            }

            // Apply the title filter
            if (!string.IsNullOrEmpty(filter.TitleFilter))
            {
                var filterTokens = filter.TitleFilter.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                filteredUpdates.RemoveAll(category => !category.MatchTitle(filterTokens));
            }

            // Apply the id filter
            if (filter.IdFilter.Count() > 0)
            {
                // Remove all updates that don't match the ID filter
                filteredUpdates.RemoveAll(u => !filter.IdFilter.Contains(u.Identity.Raw.UpdateID));
            }

            if (filter.SkipSuperseded)
            {
                filteredUpdates.RemoveAll(u => u.IsSuperseded);
            }

            // Return first X matches, if requested
            if (filter.FirstX > 0)
            {
                return filteredUpdates.Take(Math.Min(filter.FirstX, filteredUpdates.Count)).ToList();
            }
            else
            {
                return filteredUpdates;
            }
        }

        /// <summary>
        /// Returns all updates in the cache
        /// </summary>
        /// <returns>Updates all updates in the cache</returns>
        public List<Update> GetUpdates()
        {
            return Index.Values.ToList();
        }

        /// <summary>
        /// Adds new updates or categories to the store.
        /// </summary>
        /// <param name="queryResult">The query result to merge with the store.</param>
        /// <param name="categories">The known categories. Used to resolve update product IDs to a product category</param>
        /// <param name="cacheChanged">On return, set to true if there were changes to this cache, false otherwise</param>
        public void MergeQueryResult(QueryResult queryResult, CategoriesCache categories, out bool cacheChanged)
        {
            cacheChanged = false;

            if (queryResult.Filter.IsCategoriesQuery)
            {
                throw new InvalidDataException("The query result does not contain updates");
            }

            var existingFilter = UpdateQueries.Find(filter => filter.Equals(queryResult.Filter));
            if (existingFilter != null)
            {
                // The filter was used in the past; update the anchor
                existingFilter.Anchor = queryResult.Filter.Anchor;
            }
            else
            {
                // First time using this filter
                UpdateQueries.Add(queryResult.Filter);
                cacheChanged = true;
            }

            var parentRepositoryInternal = ParentRepository as IRepositoryInternal;

            List<Update> newBundlingUpdates = new List<Update>();
            
            foreach (var newUpdate in queryResult.Updates)
            {
                if (!Index.ContainsKey(newUpdate.Identity))
                {
                    // Mark the time the update was inserted into the store
                    newUpdate.LastChanged = DateTime.Now;

                    Index.Add(newUpdate.Identity, newUpdate);

                    using (var newMetadataStream = File.OpenRead(queryResult.GetUpdateXmlPath(newUpdate)))
                    {
                        using (var updateXmlWriter = parentRepositoryInternal.GetUpdateXmlWriteStream(newUpdate))
                        {
                            newMetadataStream.CopyTo(updateXmlWriter);
                        }
                    }

                    if (newUpdate is IUpdateWithBundledUpdates)
                    {
                        newBundlingUpdates.Add(newUpdate);
                    }

                    // Delete the temporary file
                    File.Delete(queryResult.GetUpdateXmlPath(newUpdate));

                    cacheChanged = true;
                }

                // If this new updates superseds any updates, mark those updates as superseded
                if (newUpdate is IUpdateWithSupersededUpdates)
                {
                    var supersedingUpdate = newUpdate as IUpdateWithSupersededUpdates;
                    // Iterate all superseded update ids
                    foreach (var supersededUpdateId in supersedingUpdate.SupersededUpdates)
                    {
                        // Find the update in the list of updates
                        foreach(var supersededUpdate in Index.Values.Where(u => u.Identity.Raw.UpdateID == supersededUpdateId.Raw.UpdateID))
                        {
                            // Mark it superseded if not already so
                            if (!supersededUpdate.IsSuperseded)
                            {
                                supersededUpdate.IsSuperseded = true;
                                cacheChanged = true;
                            }
                        }

                        // Look for superseeded updates in the query result as well
                        foreach (var supersededUpdate in queryResult.Updates.Where(u => u.Identity.Raw.UpdateID == supersededUpdateId.Raw.UpdateID))
                        {
                            // Mark it superseded if not already so
                            if (!supersededUpdate.IsSuperseded)
                            {
                                supersededUpdate.IsSuperseded = true;
                            }
                        }
                    }
                }
            }

            if (cacheChanged)
            {
                var productsList = categories.Products.Values.ToList();
                var classificationsList = categories.Classifications.Values.ToList();

                foreach (var update in Index.Values)
                {
                    var updateWithProduct = update as IUpdateWithProductInternal;
                    if (updateWithProduct != null)
                    {
                        updateWithProduct.ResolveProduct(productsList);
                    }

                    var updateWithClassification = update as IUpdateWithClassificationInternal;
                    if (updateWithClassification != null)
                    {
                        updateWithClassification.ResolveClassification(classificationsList);
                    }
                }
            }

            // Fixup missing classifications and products by inheriting them from the parent update that bundles them
            foreach (var updateWithBundledUpdates in newBundlingUpdates)
            {
                foreach (var bundledUpdate in (updateWithBundledUpdates as IUpdateWithBundledUpdates).BundledUpdates)
                {
                    if (Index.ContainsKey(bundledUpdate))
                    {
                        var update = Index[bundledUpdate];

                        if (update is IUpdateWithClassification)
                        {
                            var updateWithClassification = update as IUpdateWithClassification;
                            if (updateWithClassification.ClassificationIds.Count == 0)
                            {
                                updateWithClassification.ClassificationIds.AddRange((updateWithBundledUpdates as IUpdateWithClassification).ClassificationIds);
                            }
                        }

                        if (update is IUpdateWithProduct)
                        {
                            var updateWithProduct = update as IUpdateWithProduct;
                            if (updateWithProduct.ProductIds.Count == 0)
                            {
                                updateWithProduct.ProductIds.AddRange((updateWithBundledUpdates as IUpdateWithProduct).ProductIds);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Serializes the updates cache to JSON
        /// </summary>
        /// <param name="jsonStream">The stream to serialize to</param>
        internal void ToJson(StreamWriter jsonStream)
        {
            // Refresh the updates list from the dictionary before serializing
            UpdatesList = Index.Values.ToList();

            new JsonSerializer().Serialize(jsonStream, this);
        }
    }
}
