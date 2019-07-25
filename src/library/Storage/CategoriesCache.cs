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

namespace Microsoft.UpdateServices.Storage
{
    /// <summary>
    /// Stores categories and update metadata locally, together with any anchors and filters
    /// used to retrieve the metadata
    /// </summary>
    internal class CategoriesCache
    {
        /// <summary>
        /// The last categories query used. The categories query does not have filters, only an anchor
        /// </summary>
        [JsonProperty]
        public QueryFilter LastQuery { get; private set; }

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
        /// All known categories
        /// </summary>
        [JsonProperty]
        private List<Update> Categories;

        /// <summary>
        /// Detectoids index
        /// </summary>
        [JsonIgnore]
        public Dictionary<Identity, Detectoid> Detectoids;

        /// <summary>
        /// Products index
        /// </summary>
        [JsonIgnore]
        public Dictionary<Identity, Product> Products;

        /// <summary>
        /// Classifications index
        /// </summary>
        [JsonIgnore]
        public Dictionary<Identity, Classification> Classifications;

        /// <summary>
        /// All categories index
        /// </summary>
        [JsonIgnore]
        public Dictionary<Identity, Update> CategoriesIndex;

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
        private CategoriesCache()
        { }

        /// <summary>
        /// Internal constructor called when a new updates repository is created
        /// </summary>
        /// <param name="parentRepository"></param>
        internal CategoriesCache(IRepository parentRepository)
        {
            Detectoids = new Dictionary<Identity, Detectoid>();
            Products = new Dictionary<Identity, Product>();
            Classifications = new Dictionary<Identity, Classification>();
            CategoriesIndex = new Dictionary<Identity, Update>();
            Categories = new List<Update>();
            ParentRepository = parentRepository;
            Version = CurrentVersion;
        }

        /// <summary>
        /// Loads a categories cache from JSON
        /// </summary>
        /// <param name="jsonStream">The JSON stream to deserialize from</param>
        /// <param name="parentRepository">The repository that becomes the parent repository of the deserialized cache object</param>
        /// <returns>Cached categories</returns>
        internal static CategoriesCache FromJson(StreamReader jsonStream, IRepository parentRepository)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new UpdateConverter());

            var deserializedCache = serializer.Deserialize(jsonStream, typeof(CategoriesCache)) as CategoriesCache;
            if (deserializedCache.Version == CurrentVersion)
            {
                deserializedCache.ParentRepository = parentRepository;

                // Build the indexes
                deserializedCache.Detectoids = deserializedCache.Categories.OfType<Detectoid>().ToDictionary(d => d.Identity);
                deserializedCache.Classifications = deserializedCache.Categories.OfType<Classification>().ToDictionary(c => c.Identity);
                deserializedCache.Products = deserializedCache.Categories.OfType<Product>().ToDictionary(p => p.Identity);
                deserializedCache.CategoriesIndex = deserializedCache.Categories.ToDictionary(p => p.Identity);

                return deserializedCache;
            }

            // There is no serialized cache, or the version did not match; return a new cache
            return new CategoriesCache(parentRepository);
        }

        /// <summary>
        /// Serializes the categories cache to JSON
        /// </summary>
        internal void ToJson(StreamWriter jsonStream)
        {
            new JsonSerializer().Serialize(jsonStream, this);
        }

        /// <summary>
        /// Returns all categories in the repository that match the specified filter
        /// </summary>
        /// <param name="filter">The filter to apply</param>
        /// <returns>Categories that match the filter</returns>
        public List<Update> GetCategories(RepositoryFilter filter)
        {
            var filteredCategories = new List<Update>();

            // Apply the title filter
            if (!string.IsNullOrEmpty(filter.TitleFilter))
            {
                var filterTokens = filter.TitleFilter.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                filteredCategories.AddRange(Categories.Where(category => category.MatchTitle(filterTokens)));
            }
            else
            {
                filteredCategories.AddRange(Categories);
            }

            // Apply the id filter
            if (filter.IdFilter.Count() > 0)
            {
                // Remove all updates that don't match the ID filter
                filteredCategories.RemoveAll(u => !filter.IdFilter.Contains(u.Identity.Raw.UpdateID));
            }

            return filteredCategories;
        }

        public List<Update> GetCategories()
        {
            var allCategories = new List<Update>();
            allCategories.AddRange(Categories);

            return allCategories;
        }

        /// <summary>
        /// Adds new updates or categories to the store.
        /// </summary>
        /// <param name="queryResult">The query result to merge with the store.</param>
        /// <param name="cachedChanged">On return, set to true if there were changes to this cache, false otherwise</param>
        internal void MergeQueryResult(QueryResult queryResult, out bool cachedChanged)
        {
            cachedChanged = false;
            if (!queryResult.Filter.IsCategoriesQuery)
            {
                throw new InvalidDataException("The query result does not contain cateogories");
            }

            var parentRepositoryInternal = ParentRepository as IRepositoryInternal;

            foreach (var newCategory in queryResult.Updates)
            {
                bool categoryExists = true;
                if (newCategory is Product)
                {
                    if (!Products.ContainsKey(newCategory.Identity))
                    {
                        Products.Add(newCategory.Identity, newCategory as Product);
                        categoryExists = false;
                    }
                }
                else if (newCategory is Classification)
                {
                    if (!Classifications.ContainsKey(newCategory.Identity))
                    {
                        Classifications.Add(newCategory.Identity, newCategory as Classification);
                        categoryExists = false;
                    }
                }
                else if (newCategory is Detectoid)
                {
                    if (!Classifications.ContainsKey(newCategory.Identity))
                    {
                        Detectoids.Add(newCategory.Identity, newCategory as Detectoid);
                        categoryExists = false;
                    }
                }
                else
                {
                    throw new Exception($"Update {newCategory.Identity} is not category");
                }

                if (!categoryExists)
                {
                    // Mark the time the category was inserted into the store
                    newCategory.LastChanged = DateTime.Now;

                    // Add it to the list of categories that gets serialized
                    Categories.Add(newCategory);
                    CategoriesIndex.Add(newCategory.Identity, newCategory);

                    // Copy the XML to the local repository
                    using (var newMetadataStream = File.OpenRead(queryResult.GetUpdateXmlPath(newCategory)))
                    {
                        using (var updateXmlWriter = parentRepositoryInternal.GetUpdateXmlWriteStream(newCategory))
                        {
                            newMetadataStream.CopyTo(updateXmlWriter);
                        }
                    }

                    cachedChanged = true;
                }

                // Delete the temporary XML file
                File.Delete(queryResult.GetUpdateXmlPath(newCategory));
            }

            if (cachedChanged)
            {
                var currentProducts = Products.Values.ToList();
                foreach (var product in Products.Values)
                {
                    (product as IUpdateWithProductInternal).ResolveProduct(currentProducts);
                }

                LastQuery = queryResult.Filter;
            }
        }
    }
}
