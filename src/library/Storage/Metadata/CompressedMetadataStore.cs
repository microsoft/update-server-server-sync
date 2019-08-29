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
using System.Text;

namespace Microsoft.UpdateServices.Storage
{
    /// <summary>
    /// Stores update metadata into a compressed file on disk. Supports storing incremental changes in metadata from a baseline.
    /// </summary>
    public partial class CompressedMetadataStore : IMetadataSource
    {
        /// <summary>
        /// Gets the filters used for the query. A QueryResult has exactly one filter.
        /// </summary>
        /// <value>Query filter</value>
        [JsonIgnore]
        public IReadOnlyList<QueryFilter> Filters => (Filter == null ? new List<QueryFilter>() : new List<QueryFilter>() {Filter });

        /// <summary>
        /// Returns the anchor received after the last updates query that used the specified filter
        /// </summary>
        /// <param name="filter">The filter used in the query</param>
        /// <returns>Anchor string</returns>
        public string GetAnchorForFilter(QueryFilter filter)
        {
            return Filter?.Anchor;
        }

        /// <summary>
        /// The 1 filter applied to this QueryResult
        /// </summary>
        [JsonProperty]
        private QueryFilter Filter;

        /// <summary>
        /// Server anchor received when sync'ing categories
        /// </summary>
        [JsonProperty]
        public string CategoriesAnchor { get; private set; }

        /// <summary>
        /// The account name used when updates were added to this metadata source
        /// </summary>
        [JsonProperty]
        public string UpstreamAccountName { get; private set; }

        /// <summary>
        /// The account GUID used when updates were added to this metadata source
        /// </summary>
        [JsonProperty]
        public Guid UpstreamAccountGuid { get; private set; }

        /// <summary>
        /// Gets the dictionary of file URLs associated with updates returned by the query, indexed by file content hash.
        /// </summary>
        /// <value>Dictionary of update files</value>
        //[JsonIgnore]
        //public IReadOnlyDictionary<string, UpdateFileUrl> FilesIndex => Files;

        private ZipFile InputFile;
        ZipOutputStream OutputFile;

        /// <summary>
        /// Gets the path to the file on disk that contains a serialized version of this query result.
        /// </summary>
        [JsonIgnore]
        public string FilePath { get; private set; }

        private const string IndexFileName = "index.json";

        #region IMetadataCollection Indexes

        /// <summary>
        /// Gets the updates (software, drivers) index
        /// </summary>
        /// <value>List of products</value>
        [JsonIgnore]
        public IReadOnlyDictionary<Identity, Update> UpdatesIndex { get; private set; }

        /// <summary>
        /// Gets the classifications index
        /// </summary>
        /// <value>List of classifications</value>
        [JsonIgnore]
        public IReadOnlyDictionary<Identity, Classification> ClassificationsIndex { get; private set; }

        /// <summary>
        /// Gets the detectoids index
        /// </summary>
        /// <value>List of detectoids</value>
        [JsonIgnore]
        public IReadOnlyDictionary<Identity, Detectoid> DetectoidsIndex { get; private set; }

        /// <summary>
        /// Gets the categories index (products, classifications, detectoids)
        /// </summary>
        /// <value>List of categories</value>
        [JsonIgnore]
        public IReadOnlyDictionary<Identity, Update> CategoriesIndex { get; private set; }

        /// <summary>
        /// Gets the products index
        /// </summary>
        /// <value>List of updates</value>
        [JsonIgnore]
        public IReadOnlyDictionary<Identity, Product> ProductsIndex { get; private set; }

        #endregion

        ConcurrentDictionary<Identity, Update> Updates;
        ConcurrentDictionary<Identity, Update> Categories;

        [JsonProperty]
        Dictionary<int, List<int>> ProductsTree;

        /// <summary>
        /// The serialization version of this object
        /// </summary>
        [JsonProperty]
        private int Version = 1;

        /// <summary>
        /// The current serialization version of this object
        /// </summary>
        private const int CurrentVersion = 1;

        /// <summary>
        /// Flag that indicates that metadata for the contained updates has been loaded.
        /// If false, call the Hydrate() method.
        /// </summary>
        [JsonIgnore]
        public bool IsHydrated { get; private set; }

        /// <summary>
        /// Gets the endpoint of the upstream server that is the source for this metadata source
        /// </summary>
        [JsonProperty]
        public Endpoint UpstreamSource { get; private set; }

        /// <summary>
        /// Progress notifications during the export operations
        /// </summary>
        public event EventHandler<OperationProgress> ExportProgress;

        [JsonConstructor]
        private CompressedMetadataStore()
        {

        }

        /// <summary>
        /// Create a new update metadata store that saves its content to the specified file
        /// </summary>
        /// <param name="storeFile">File where to save the store</param>
        /// <param name="upstreamSource">The upstream from which stored metadata was aquired from</param>
        public CompressedMetadataStore(string storeFile, Endpoint upstreamSource)
        {
            OutputFile = new ZipOutputStream(File.Create(storeFile));
            FilePath = storeFile;

            // When creating a baseline query result, the baseline indexes are empty
            BaselineIndexesEnd = -1;
            BaselineIdentities = new SortedSet<Identity>();

            Identities = new SortedSet<Identity>();
            IndexToIdentity = new Dictionary<int, Identity>();
            IdentityToIndex = new Dictionary<Identity, int>();
            ProductsTree = new Dictionary<int, List<int>>();
            UpdateTypeMap = new Dictionary<int, uint>();

            Updates = new ConcurrentDictionary<Identity, Update>();
            Categories = new ConcurrentDictionary<Identity, Update>();

            UpstreamSource = upstreamSource;
            UpdateAndProductIndex = new Dictionary<int, List<Guid>>();
            UpdateAndClassificationIndex = new Dictionary<int, List<Guid>>();

            UpstreamAccountName = Guid.NewGuid().ToString();
            UpstreamAccountGuid = Guid.NewGuid();

            UpdateTitlesIndex = new Dictionary<int, string>();

            // Initialize bundle indexes
            OnNewStore_InitializeBundles();

            // Initialize prerequisites
            OnNewStore_InitializePrerequisites();

            // Initialize update classification and product information
            OnNewStore_InitializeProductClassification();

            // Initialize files index
            OnNewStore_InitializeFilesIndex();

            // Initialize superseding index
            OnNewStore_InitializeSupersededIndex();
        }

        /// <summary>
        /// Create a QueryResult from a serialized result.
        /// </summary>
        /// <param name="queryResultFile">The file that contains a serialized query result</param>
        /// <returns></returns>
        public static CompressedMetadataStore Open(string queryResultFile)
        {
            var resultArchive = new ZipFile(queryResultFile);

            var indexEntry = resultArchive.GetEntry(IndexFileName);
            using (var indexStream = resultArchive.GetInputStream(indexEntry))
            {
                using (var indexStreamReader = new StreamReader(indexStream))
                {
                    var deserializedResult = JsonConvert.DeserializeObject<CompressedMetadataStore>(indexStreamReader.ReadToEnd());
                    if (deserializedResult.Version != CurrentVersion)
                    {
                        throw new Exception("Invalid version");
                    }

                    deserializedResult.FilePath = queryResultFile;

                    try
                    {
                        deserializedResult.OnDeserialized();
                    }
                    catch(Exception ex)
                    {
                        resultArchive.Close();
                        throw ex;
                    }

                    deserializedResult.InputFile = resultArchive;

                    return deserializedResult;
                }
            }
        }

        /// <summary>
        /// Returns a stream over the update XML metadata
        /// </summary>
        /// <param name="updateId">The update ID to get XML metadata for</param>
        /// <returns>Stream of the XML metadata</returns>
        public Stream GetUpdateMetadataStream(Metadata.Identity updateId)
        {
            if (InputFile == null)
            {
                throw new Exception("Query result is not in read mode");
            }

            if (IsDeltaSource && BaselineIdentities.Contains(updateId))
            {
                return BaselineSource.GetUpdateMetadataStream(updateId);
            }
            else
            {
                var entryIndex = InputFile.FindEntry(GetUpdateXmlPath(updateId), true);
                if (entryIndex < 0)
                {
                    throw new KeyNotFoundException();
                }

                return InputFile.GetInputStream(entryIndex);
            }
        }

        /// <summary>
        /// Returns the path to the update XML in query result ZIP archive
        /// </summary>
        /// <param name="updateId">The update to get the path for.</param>
        /// <returns>A fully qualified path to the XML file belonging to the specified update</returns>
        private string GetUpdateXmlPath(Metadata.Identity updateId)
        {
            return $"{GetUpdateIndex(updateId)}/{updateId.ToString()}.xml";
        }

        /// <summary>
        /// Returns an index for an update (number between 0 and 255) based on the update\s ID.
        /// </summary>
        /// <param name="updateId">The update to get the index for</param>
        /// <returns>String representation of the index</returns>
        private static string GetUpdateIndex(Metadata.Identity updateId)
        {
            // The index is the last 8 bits of the update ID.
            return updateId.Raw.UpdateID.ToByteArray().Last().ToString();
        }

        private void WriteIndex()
        {
            if (OutputFile == null)
            {
                throw new Exception("Query result is not in write mode");
            }

            var indexString = JsonConvert.SerializeObject(this);

            OutputFile.PutNextEntry(new ZipEntry(IndexFileName));
            OutputFile.Write(Encoding.UTF8.GetBytes(indexString));
            OutputFile.CloseEntry();
        }

        /// <summary>
        /// Deletes the temporary directory that contains XML metadata
        /// </summary>
        public void Dispose()
        {
            if (OutputFile != null)
            {
                WriteIndex();
                OutputFile.Finish();
                OutputFile.Close();
                OutputFile = null;
            }
            else if (InputFile != null)
            {
                InputFile.Close();
                InputFile = null;
            }

            if (IsDeltaSource && BaselineSource != null)
            {
                BaselineSource.Dispose();
                BaselineSource = null;
            }
        }

        /// <summary>
        /// Delete the query result
        /// </summary>
        public void Delete()
        {
            Dispose();
            
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }

        /// <summary>
        /// Returns all categories present in the metadata store
        /// </summary>
        /// <returns>List of categories: classifications, detectoids, products</returns>
        public ICollection<Update> GetCategories()
        {
            return Categories.Values;
        }

        /// <summary>
        /// Returns all categories that match the filter
        /// </summary>
        /// <param name="filter">Categories filter</param>
        /// <returns>List of categories that match the filter</returns>
        public List<Update> GetCategories(MetadataFilter filter)
        {
            var filteredCategories = new List<Update>();

            // Apply the title filter
            if (!string.IsNullOrEmpty(filter.TitleFilter))
            {
                var filterTokens = filter.TitleFilter.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                filteredCategories.AddRange(Categories.Values.Where(category => category.MatchTitle(filterTokens)));
            }
            else
            {
                filteredCategories.AddRange(Categories.Values);
            }

            // Apply the id filter
            if (filter.IdFilter.Count() > 0)
            {
                // Remove all updates that don't match the ID filter
                filteredCategories.RemoveAll(u => !filter.IdFilter.Contains(u.Identity.Raw.UpdateID));
            }

            return filteredCategories;
        }

        /// <summary>
        /// Returns all updates that match the filter
        /// </summary>
        /// <param name="filter">Updates filter</param>
        /// <returns>List of updates that match the filter</returns>
        public List<Update> GetUpdates(MetadataFilter filter)
        {
            return MetadataFilter.FilterUpdatesList(Updates.Values, filter);
        }

        /// <summary>
        /// Returns all updates 
        /// </summary>
        /// <returns>List of updates</returns>
        public ICollection<Update> GetUpdates()
        {
            return Updates.Values;
        }

        /// <summary>
        /// Get an update by ID
        /// </summary>
        /// <param name="updateId">The update ID to lookup</param>
        /// <returns>The requested update</returns>
        public Update GetUpdate(Identity updateId)
        {
            return Updates[updateId];
        }

        /// <summary>
        /// Set the credentials used to connecto to the upstream server
        /// </summary>
        /// <param name="accountName"></param>
        /// <param name="AccountGuid"></param>
        public void SetUpstreamCredentials(string accountName, Guid AccountGuid)
        {
            if (OutputFile == null)
            {
                throw new Exception("Query result is not in write mode");
            }

            UpstreamAccountName = accountName;
            UpstreamAccountGuid = AccountGuid;
        }

        private T DeserializeIndexFromArchive<T>(string indexName)
        {
            var indexEntry = InputFile.GetEntry(indexName);

            using (var indexStream = InputFile.GetInputStream(indexEntry))
            {
                JsonSerializer serializer = new JsonSerializer();

                using (StreamReader sr = new StreamReader(indexStream))
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    return serializer.Deserialize<T>(reader);
                }
            }
        }

        private void SerializeIndexToArchive<T>(string indexName, T index)
        {
            OutputFile.PutNextEntry(new ZipEntry(indexName));

            using (StreamWriter sw = new StreamWriter(OutputFile, Encoding.UTF8, 4 * 1024, true))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(writer, index);
            }

            OutputFile.CloseEntry();
        }

        /// <summary>
        /// Exports the selected updates from the metadata source
        /// </summary>
        /// <param name="filter">Export filter</param>
        /// <param name="exportFile">Export file path</param>
        /// <param name="format">Export format</param>
        /// <param name="serverConfiguration">Server configuration.</param>
        /// <returns>List of categories that match the filter</returns>
        public void Export(MetadataFilter filter, string exportFile, RepoExportFormat format, ServerSyncConfigData serverConfiguration)
        {
            var updatesToExport = GetUpdates(filter);

            if (format == RepoExportFormat.WSUS_2016)
            {
                var exporter = new WsusExport(this, serverConfiguration);
                exporter.ExportProgress += Exporter_ExportProgress;
                exporter.Export(updatesToExport, exportFile);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void Exporter_ExportProgress(object sender, OperationProgress e)
        {
            ExportProgress?.Invoke(this, e);
        }
    }
}
