using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UpdateServices.Query;
using Microsoft.UpdateServices.Metadata;
using Newtonsoft.Json;
using System.Linq;
using System.IO;
using Microsoft.UpdateServices.Metadata.Prerequisites;

namespace Microsoft.UpdateServices.LocalCache
{
    /// <summary>
    /// Stores update metadata locally, together with any anchors and filters
    /// used to retrieve the metadata
    /// </summary>
    public class UpdatesCache
    {
        /// <summary>
        /// List of known updates
        /// </summary>
        [JsonIgnore]
        public Dictionary<MicrosoftUpdateIdentity, MicrosoftUpdate> Updates { get; protected set; }

        /// <summary>
        /// List of known updates; only used to serialize the dictionary above as a flat list
        /// </summary>
        [JsonProperty]
        private List<MicrosoftUpdate> UpdatesList { get; set; }

        /// <summary>
        /// Filter for driver updates in the updates list
        /// </summary>
        [JsonIgnore]
        public IEnumerable<DriverUpdate> Drivers => Updates.Values.OfType<DriverUpdate>();

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
        const int CurrentVersion = 2;

        /// <summary>
        /// The updates repository that contains this object. Used when serializing-deserializing to 
        /// resolve paths
        /// </summary>
        [JsonIgnore]
        internal Repository ParentRepository;

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
        private UpdatesCache(Repository parentRepository)
        {
            Updates = new Dictionary<MicrosoftUpdateIdentity, MicrosoftUpdate>();
            UpdateQueries = new List<QueryFilter>();
            ParentRepository = parentRepository;
            Version = CurrentVersion;
        }

        /// <summary>
        /// Loads an updates cache from a local repository (disk)
        /// </summary>
        /// <param name="parentRepository">The repository to load the updates cache from</param>
        /// <returns>Cached updates</returns>
        internal static UpdatesCache FromRepository(Repository parentRepository)
        {
            if (File.Exists(parentRepository.UpdatesFilePath))
            {
                using (var file = System.IO.File.OpenText(parentRepository.UpdatesFilePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Converters.Add(new MicrosoftUpdateConverter());

                    var deserializedCache = serializer.Deserialize(file, typeof(UpdatesCache)) as UpdatesCache;
                    if (deserializedCache.Version == CurrentVersion)
                    {
                        // Re-create the updates dictionary from the serialized list of updates
                        deserializedCache.Updates = deserializedCache.UpdatesList.ToDictionary(u => u.Identity);

                        // The updates list is only used to serialize updates to disk. For in-memory operations only
                        // the dictionary is used
                        deserializedCache.UpdatesList.Clear();
                        return deserializedCache;
                    }
                }
            }

            // There is no serialized cache, or the version did not match; return a new cache
            return new UpdatesCache(parentRepository);
        }

        /// <summary>
        /// Adds new updates or categories to the store.
        /// </summary>
        /// <param name="queryResult">The query result to merge with the store.</param>
        /// <param name="categories">The known categories. Used to resolve update product IDs to a product category</param>
        public void MergeQueryResult(QueryResult queryResult, CategoriesCache categories)
        {
            if (queryResult.Filter.IsCategoriesQuery)
            {
                throw new InvalidDataException("The query result does not contain updates");
            }

            bool changed = false;

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
                changed = true;
            }
            
            foreach (var newUpdate in queryResult.Updates)
            {
                if (!Updates.ContainsKey(newUpdate.Identity))
                {
                    Updates.Add(newUpdate.Identity, newUpdate);

                    // Save the XML metadata blob separately. It does not get serialized as JSON
                    var xmlFilePath = ParentRepository.GetUpdateXmlPath(newUpdate);
                    if (!File.Exists(xmlFilePath))
                    {
                        var parentDirectory = Path.GetDirectoryName(xmlFilePath);
                        if (!Directory.Exists(parentDirectory))
                        {
                            Directory.CreateDirectory(parentDirectory);
                        }

                        File.WriteAllText(xmlFilePath, newUpdate.XmlData);
                    }

                    changed = true;
                }
            }

            if (changed)
            {
                var productsList = categories.Products.ToList();
                var classificationsList = categories.Classifications.ToList();

                foreach (var update in Updates.Values)
                {
                    var updateWithProduct = update as IUpdateWithProduct;
                    if (updateWithProduct != null)
                    {
                        updateWithProduct.ResolveProduct(productsList);
                    }

                    var updateWithClassification = update as IUpdateWithClassification;
                    if (updateWithClassification != null)
                    {
                        updateWithClassification.ResolveClassification(classificationsList);
                    }
                }

                Commit();
            }
        }

        /// <summary>
        /// Deletes the cached categories from a local repository (disk)
        /// </summary>
        internal static void Delete(Repository repository)
        {
            if (File.Exists(repository.UpdatesFilePath))
            {
                File.Delete(repository.UpdatesFilePath);
            }
        }


        /// <summary>
        /// Deletes the cached updates from the local repository (disk)
        /// </summary>
        internal void Delete()
        {
            Delete(ParentRepository);

            Updates.Clear();
            UpdateQueries.Clear();
        }

        /// <summary>
        /// Writes out the updates cache to the repository (disk)
        /// </summary>
        internal void Commit()
        {
            // Refresh the updates list from the dictionary before serializing
            UpdatesList = Updates.Values.ToList();
            File.WriteAllText(ParentRepository.UpdatesFilePath, JsonConvert.SerializeObject(this));
        }
    }
}
