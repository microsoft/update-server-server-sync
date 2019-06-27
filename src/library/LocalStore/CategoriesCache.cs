using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UpdateServices.Query;
using Microsoft.UpdateServices.Metadata;
using Newtonsoft.Json;
using System.Linq;
using System.IO;

namespace Microsoft.UpdateServices.LocalCache
{
    /// <summary>
    /// Stores categories and update metadata locally, together with any anchors and filters
    /// used to retrieve the metadata
    /// </summary>
    public class CategoriesCache
    {
        /// <summary>
        /// The last categories query used. The categories query does not have filters, only an anchor
        /// </summary>
        [JsonProperty]
        public QueryFilter LastQuery { get; private set; }

        /// <summary>
        /// List of known categories
        /// </summary>
        [JsonIgnore]
        public Dictionary<MicrosoftUpdateIdentity, MicrosoftUpdate> Categories { get; private set; }

        /// <summary>
        /// List of known categories; only used to serialize the dictionary above as a flat list
        /// </summary>
        [JsonProperty]
        private List<MicrosoftUpdate> CategoriesList { get; set; }

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
        /// Filter for detectoids in the categories list
        /// </summary>
        [JsonIgnore]
        public IEnumerable<Detectoid> Detectoids => Categories.Values.OfType<Detectoid>();

        /// <summary>
        /// Filter for products in the categories list
        /// </summary>
        [JsonIgnore]
        public IEnumerable<MicrosoftProduct> Products => Categories.Values.OfType<MicrosoftProduct>();

        /// <summary>
        /// Filter for classifications in the categories list
        /// </summary>
        [JsonIgnore]
        public IEnumerable<Classification> Classifications => Categories.Values.OfType<Classification>();

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
        private CategoriesCache()
        { }

        /// <summary>
        /// Internal constructor called when a new updates repository is created
        /// </summary>
        /// <param name="parentRepository"></param>
        private CategoriesCache(Repository parentRepository)
        {
            Categories = new Dictionary<MicrosoftUpdateIdentity, MicrosoftUpdate>();
            ParentRepository = parentRepository;
            Version = CurrentVersion;
        }

        /// <summary>
        /// Loads a categories cache from a local repository (disk)
        /// </summary>
        /// <param name="parentRepository">The repository to load the categories cache from</param>
        /// <returns>Cached categories</returns>
        internal static CategoriesCache FromRepository(Repository parentRepository)
        {
            if (File.Exists(parentRepository.CategoriesFilePath))
            {
                using (var file = System.IO.File.OpenText(parentRepository.CategoriesFilePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Converters.Add(new MicrosoftUpdateConverter());

                    var deserializedCache = serializer.Deserialize(file, typeof(CategoriesCache)) as CategoriesCache;
                    if (deserializedCache.Version == CurrentVersion)
                    {
                        // Re-create the categories dictionary from the serialized list of categories
                        deserializedCache.Categories = deserializedCache.CategoriesList.ToDictionary(u => u.Identity);

                        // The categories list is only used to serialize categories to disk. For in-memory operations only
                        // the dictionary is used
                        deserializedCache.CategoriesList.Clear();

                        return deserializedCache;
                    }
                }
            }

            // There is no serialized cache, or the version did not match; return a new cache
            return new CategoriesCache(parentRepository);
        }

        /// <summary>
        /// Adds new updates or categories to the store.
        /// </summary>
        /// <param name="queryResult">The query result to merge with the store.</param>
        internal void MergeQueryResult(QueryResult queryResult)
        {
            if (!queryResult.Filter.IsCategoriesQuery)
            {
                throw new InvalidDataException("The query result does not contain cateogories");
            }

            
            bool changed = false;
            foreach (var newCategory in queryResult.Updates)
            {
                if (!Categories.ContainsKey(newCategory.Identity))
                {
                    Categories.Add(newCategory.Identity, newCategory);

                    // Prepare the path where to save the XML metadata
                    var xmlFilePath = ParentRepository.GetUpdateXmlPath(newCategory);
                    var parentDirectory = Path.GetDirectoryName(xmlFilePath);
                    if (!Directory.Exists(parentDirectory))
                    {
                        Directory.CreateDirectory(parentDirectory);
                    }

                    // The XML metadata will be overwritten
                    if (File.Exists(xmlFilePath))
                    {
                        File.Delete(xmlFilePath);
                    }

                    // Move the XML metadata file from the query result location to this repo
                    File.Move(queryResult.GetUpdateXmlPath(newCategory), xmlFilePath);

                    changed = true;
                }
            }

            if (changed)
            {
                var currentProducts = Products.ToList();
                foreach (var product in currentProducts)
                {
                    product.ResolveProduct(currentProducts);
                }

                LastQuery = queryResult.Filter;
                Commmit();
            }
        }

        /// <summary>
        /// Writes out the categories cache to the repository (disk)
        /// </summary>
        internal void Commmit()
        {
            // Refresh the categories list from the dictionary before serializing
            CategoriesList = Categories.Values.ToList();

            File.WriteAllText(ParentRepository.CategoriesFilePath, JsonConvert.SerializeObject(this));
        }

        /// <summary>
        /// Deletes the cached categories from a local repository (disk)
        /// </summary>
        internal static void Delete(Repository repository)
        {
            if (File.Exists(repository.CategoriesFilePath))
            {
                File.Delete(repository.CategoriesFilePath);
            }
        }

        /// <summary>
        /// Deletes the cached categories from the local repository (disk)
        /// </summary>
        internal void Delete()
        {
            Delete(ParentRepository);

            Categories.Clear();
            LastQuery = null;
        }
    }
}
