// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Client;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.Query;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microsoft.UpdateServices.Storage
{
    /// <summary>
    /// Updates repository implementation that stores all metadata, content and configuration
    /// on the local file system.
    /// </summary>
    public class FileSystemRepository : IRepository, IRepositoryInternal
    {
        /// <summary>
        /// The file containing repository configuration
        /// </summary>
        private const string RepositoryConfigFileName = ".repo";
        private string RepositoryConfigFilePath => Path.Combine(LocalPath, RepositoryConfigFileName);

        /// <summary>
        /// The file containing authentication data
        /// </summary>
        private const string AuthenticationFileName = "wu-server-auth.json";
        private string AuthenticationFilePath => Path.Combine(LocalPath, AuthenticationFileName);

        /// <summary>
        /// The file containing server configuration data
        /// </summary>
        private const string ServiceConfigurationFileName = "wu-server-config.json";
        private string ServiceConfigurationFilePath => Path.Combine(LocalPath, ServiceConfigurationFileName);

        /// <summary>
        /// The file containing categories data
        /// </summary>
        const string CategoriesFileName = "wu-server-categories.json";
        private string CategoriesFilePath => Path.Combine(LocalPath, CategoriesFileName);

        /// <summary>
        /// The file containing updates metadata
        /// </summary>
        const string UpdatesFileName = "wu-server-updates.json";
        private string UpdatesFilePath => Path.Combine(LocalPath, UpdatesFileName);

        /// <summary>
        /// Root content directory name
        /// </summary>
        private const string ContentDirectoryName = "content";
        private string ContentDirectoryPath => Path.Combine(LocalPath, ContentDirectoryName);

        /// <summary>
        /// Root XML metadata directory name
        /// </summary>
        private const string XmlMetadataDirectoryName = "xml-data";
        private string XmlMetadataDirectoryPath => Path.Combine(LocalPath, XmlMetadataDirectoryName);

        /// <summary>
        /// Update content files index
        /// </summary>
        private const string ContentFilesFileName = "wu-server-files.json";
        private string ContentFilesFilePath => Path.Combine(LocalPath, ContentFilesFileName);

        private bool HasCategoriesIndex
        {
            get
            {
                return File.Exists(CategoriesFilePath);
            }
        }

        /// <summary>
        /// Manager for locally cached categories
        /// </summary>
        private CategoriesCache Categories;

        /// <summary>
        /// Returns a stream reader for the categories index file
        /// </summary>
        /// <returns>Stream reader</returns>
        public StreamReader GetCategoriesIndexReader()
        {
            return File.OpenText(CategoriesFilePath);
        }

        /// <summary>
        /// Returns a stream writer for the categories index file. The index file is overwritten.
        /// </summary>
        /// <returns>Stream writer</returns>
        private StreamWriter GetCategoriesIndexWriter()
        {
            return File.CreateText(CategoriesFilePath);
        }

        /// <summary>
        /// Check if the repository has an updates index
        /// </summary>
        private bool HasUpdatesIndex
        {
            get
            {
                return File.Exists(UpdatesFilePath);
            }
        }

        /// <summary>
        /// Returns a stream reader for the updates index file
        /// </summary>
        /// <returns>Stream reader</returns>
        private StreamReader GetUpdatesIndexReader()
        {
            return File.OpenText(UpdatesFilePath);
        }

        /// <summary>
        /// Returns a stream writer for the updates index file. The index file is overwritten.
        /// </summary>
        /// <returns>Stream writer</returns>
        private StreamWriter GetUpdatesIndexWriter()
        {
            return File.CreateText(UpdatesFilePath);
        }

        /// <summary>
        /// Manager for locally cached updates
        /// </summary>
        private UpdatesCache Updates;

        /// <summary>
        /// Raised on progress for long running repository operations
        /// </summary>
        /// <value>
        /// Progress data.
        /// </value>
        public event EventHandler<OperationProgress> RepositoryOperationProgress;

        /// <summary>
        /// Gets the configuration of the repository
        /// </summary>
        /// <value>Repository configuration</value>
        public RepoConfiguration Configuration { get; private set; }

        /// <summary>
        /// Index with metadata for update content files
        /// </summary>
        private Dictionary<string, UpdateFileUrl> Files;

        /// <summary>
        /// Given an update file, returns the path to the file in local store
        /// </summary>
        /// <param name="updateFile">The file to get the path for</param>
        /// <returns>Fully qualified path to the file. The path might not exist.</returns>
        private string GetUpdateFilePath(UpdateFile updateFile)
        {
            if (updateFile.Digests.Count == 0)
            {
                throw new Exception("Cannot determine file path for update with no digest");
            }

            byte[] hashBytes = Convert.FromBase64String(updateFile.Digests[0].DigestBase64);
            var contentSubDirectory = string.Format("{0:X}", hashBytes.Last());

            return Path.Combine(LocalPath, ContentDirectoryName, contentSubDirectory, updateFile.Digests[0].DigestBase64.Replace('/', '_'), updateFile.FileName);
        }

        /// <summary>
        /// Returns the path to the file that marks whether an update content file was successfully downloaded.
        /// The marker file is written after the update content file is downloaded and its hash verified
        /// </summary>
        /// <param name="updateFile">Update content file for which to retrieve the marker file path</param>
        /// <returns>The marker file path. This file might not exist.</returns>
        private string GetUpdateFileMarkerPath(UpdateFile updateFile)
        {
            return GetUpdateFilePath(updateFile) + ".done";
        }

        /// <summary>
        /// Given an update, returns the path to its XML file in the store.
        /// </summary>
        /// <param name="update">The update to get the path for</param>
        /// <returns>Fully qualified path to the file. The path might not exist.</returns>
        private string GetUpdateXmlPath(Update update)
        {
            var contentSubDirectory = update.Identity.ID.ToByteArray().Last().ToString();
            return Path.Combine(LocalPath, XmlMetadataDirectoryName, contentSubDirectory, update.Identity.ToString() + ".xml");
        }

        private string LocalPath = null;

        private FileSystemRepository() { }

        /// <summary>
        /// Checks if a repository exists at the specified path
        /// </summary>
        /// <param name="repoDirectory">Path to check</param>
        /// <returns>True if a repository exists at the specified path, false otherwise</returns>
        public static bool RepoExists(string repoDirectory)
        {
            var checkRepository = new FileSystemRepository() { LocalPath = repoDirectory };

            if (!Directory.Exists(repoDirectory) || !File.Exists(checkRepository.RepositoryConfigFilePath))
            {
                return false;
            }

            checkRepository.Configuration = RepoConfiguration.ReadFromFile(checkRepository.RepositoryConfigFilePath);
            return checkRepository.Configuration != null;
        }

        /// <summary>
        /// Initializes a new local updates repository that sync's updates from the specified upstream update server
        /// </summary>
        /// <param name="repoDirectory">Directory to initialize the new repository in</param>
        /// <param name="upstreamServerAddress">Upstream server from where to sync updates</param>
        /// <returns>A newly initialize repository</returns>
        public static FileSystemRepository Init(string repoDirectory, string upstreamServerAddress)
        {
            if (RepoExists(repoDirectory))
            {
                return null;
            }

            var newRepository = new FileSystemRepository() { LocalPath = repoDirectory, Configuration = new RepoConfiguration(new Endpoint(upstreamServerAddress)) };
            newRepository.Updates = new UpdatesCache(newRepository);
            newRepository.Categories = new CategoriesCache(newRepository);
            newRepository.Configuration.SaveToFile(newRepository.RepositoryConfigFilePath);

            newRepository.Files = new Dictionary<string, UpdateFileUrl>();

            return newRepository;
        }

        /// <summary>
        /// Opens a repository from a directory
        /// </summary>
        /// <param name="repoDirectory">The directory path to the repository</param>
        /// <returns>An updates repository</returns>
        public static FileSystemRepository Open(string repoDirectory)
        {
            if (!RepoExists(repoDirectory))
            {
                return null;
            }

            var newRepository = new FileSystemRepository() { LocalPath = repoDirectory };
            newRepository.Configuration = RepoConfiguration.ReadFromFile(newRepository.RepositoryConfigFilePath);

            if (File.Exists(newRepository.ServiceConfigurationFilePath))
            {
                newRepository.ServiceConfiguration = JsonConvert.DeserializeObject<ServerSyncConfigData>(File.ReadAllText(newRepository.ServiceConfigurationFilePath));
            }

            if (File.Exists(newRepository.AuthenticationFilePath))
            {
                newRepository.AccessToken = JsonConvert.DeserializeObject<ServiceAccessToken>(File.ReadAllText(newRepository.AuthenticationFilePath));
            }

            if (File.Exists(newRepository.ContentFilesFilePath))
            {
                newRepository.Files = ReadFilesIndex(newRepository.ContentFilesFilePath);
            }
            else
            {
                newRepository.Files = new Dictionary<string, UpdateFileUrl>();
            }

            if (newRepository.HasCategoriesIndex)
            {
                using (var indexReader = newRepository.GetCategoriesIndexReader())
                {
                    newRepository.Categories = CategoriesCache.FromJson(indexReader, newRepository);
                }
            }
            else
            {
                newRepository.Categories = new CategoriesCache(newRepository);
            }

            if (newRepository.HasUpdatesIndex)
            {
                using (var indexReader = newRepository.GetUpdatesIndexReader())
                {
                    newRepository.Updates = UpdatesCache.FromJson(indexReader, newRepository);
                }
            }
            else
            {
                newRepository.Updates = new UpdatesCache(newRepository);
            }

            return newRepository;
        }

        /// <summary>
        /// Reads the content files index from a JSON file
        /// </summary>
        /// <param name="path">Path to the JSON file</param>
        /// <returns>A content files index (dictionary)</returns>
        private static Dictionary<string, UpdateFileUrl> ReadFilesIndex(string path)
        {
            using (var indexReader = File.OpenText(path))
            {
                JsonSerializer deserializer = new JsonSerializer();
                return deserializer.Deserialize(indexReader, typeof(Dictionary<string, UpdateFileUrl>)) as Dictionary<string, UpdateFileUrl>;
            }
        }

        /// <summary>
        /// Writes the current file index to a JSON file
        /// </summary>
        private void WriteFilesIndex()
        {
            using (var indexWriter = File.CreateText(ContentFilesFilePath))
            {
                JsonSerializer deserializer = new JsonSerializer();
                deserializer.Serialize(indexWriter, Files);
            }
        }

        /// <summary>
        /// Delete the repository stored at the specified path
        /// </summary>
        /// <param name="path">The path that contains the repository to delete</param>
        public static void Delete(string path)
        {
            // Create an empty repository, only initialize the path that will be used to delete
            // the various files that make up the repository
            var repoToDelete = new FileSystemRepository() { LocalPath = path };
            repoToDelete.Delete();
        }

        /// <summary>
        /// Deletes the repository from disk and clears all cached data from memory
        /// </summary>
        public void Delete()
        {
            var filesToDelete = new string[] { AuthenticationFilePath, ServiceConfigurationFilePath, RepositoryConfigFilePath, CategoriesFilePath, UpdatesFilePath, ContentFilesFilePath };
            foreach(var fileToDelete in filesToDelete)
            {
                if (File.Exists(fileToDelete))
                {
                    File.Delete(fileToDelete);
                }
            }

            var directoriesToDelete = new string[] { XmlMetadataDirectoryPath, ContentDirectoryPath };
            foreach(var dirToDelete in directoriesToDelete)
            {
                if (Directory.Exists(dirToDelete))
                {
                    Directory.Delete(dirToDelete, true);
                }
            }
        }

        /// <summary>
        /// Merge new updates or categories into the repository
        /// </summary>
        /// <param name="queryResult">The query results to merge</param>
        public void MergeQueryResult(QueryResult queryResult)
        {
            if (queryResult.Filter.IsCategoriesQuery)
            {
                Categories.MergeQueryResult(queryResult, out bool cacheChanged);
                if (cacheChanged)
                {
                    using (var indexWriter = GetCategoriesIndexWriter())
                    {
                        Categories.ToJson(indexWriter);
                    }
                }
            }
            else
            {
                Updates.MergeQueryResult(queryResult, Categories, out bool cacheChanged);
                if (cacheChanged)
                {
                    using (var indexWriter = GetUpdatesIndexWriter())
                    {
                        Updates.ToJson(indexWriter);
                    }
                }
            }

            // If content files metadata changed, re-write the files index
            var newFiles = queryResult.Files.Values.Except(Files.Values).ToList();
            if (newFiles.Count > 0)
            {
                newFiles.ForEach(f => Files.Add(f.DigestBase64, f));
                WriteFilesIndex();
            }
        }

        /// <summary>
        /// Cached access token
        /// </summary>
        ServiceAccessToken AccessToken;

        ServiceAccessToken IRepositoryInternal.AccessToken => AccessToken;

        /// <summary>
        /// Caches an access token for future use. Overwrites the previusly cached token.
        /// </summary>
        /// <param name="accessToken">The token to cache.</param>
        void SetAccessToken(ServiceAccessToken accessToken)
        {
            AccessToken = accessToken;
            File.WriteAllText(AuthenticationFilePath, JsonConvert.SerializeObject(accessToken));
        }

        /// <summary>
        /// Cached service configuration
        /// </summary>
        ServerSyncConfigData IRepositoryInternal.ServiceConfiguration => ServiceConfiguration;
        private ServerSyncConfigData ServiceConfiguration;

        /// <summary>
        /// Cache service configuration to disk. Overwrites the previously cached configuration.
        /// </summary>
        /// <param name="serverConfig">The configuration to cache</param>
        internal void SetServiceConfiguration(ServerSyncConfigData serverConfig)
        {
            ServiceConfiguration = serverConfig;
            File.WriteAllText(ServiceConfigurationFilePath, JsonConvert.SerializeObject(serverConfig));
        }

        /// <summary>
        /// Gets the products index
        /// </summary>
        /// <value>List of products</value>
        public IReadOnlyDictionary<Identity, Product> ProductsIndex => Categories.Products;

        /// <summary>
        /// Gets the classifications index
        /// </summary>
        /// <value>List of classifications</value>
        public IReadOnlyDictionary<Identity, Classification> ClassificationsIndex => Categories.Classifications;

        /// <summary>
        /// Gets the detectoids index
        /// </summary>
        /// <value>List of detectoids</value>
        public IReadOnlyDictionary<Identity, Detectoid> DetectoidsIndex => Categories.Detectoids;

        /// <summary>
        /// Gets the updates indexUpdates index
        /// </summary>
        /// <value>List of updates</value>
        public IReadOnlyDictionary<Identity, Update> UpdatesIndex => Updates.Index;

        /// <summary>
        /// Gets the categories index (products, classifications, detectoids)
        /// </summary>
        /// <value>List of categories</value>
        public IReadOnlyDictionary<Identity, Update> CategoriesIndex => Categories.CategoriesIndex;


        /// <summary>
        /// Checks if an update file has been downloaded
        /// </summary>
        /// <param name="file">File to check if it was downloaded</param>
        /// <returns>True if the file was downloaded, false otherwise</returns>
        private bool IsFileDownloaded(UpdateFile file)
        {
            return File.Exists(GetUpdateFileMarkerPath(file));
        }

        /// <summary>
        /// Download content for an update
        /// </summary>
        /// <param name="update">The update to download content for</param>
        public void DownloadUpdateContent(IUpdateWithFiles update)
        {
            var contentDownloader = new ContentDownloader();
            contentDownloader.OnDownloadProgress += ContentDownloader_OnDownloadProgress;

            var hashChecker = new ContentHash();
            hashChecker.OnHashingProgress += HashChecker_OnHashingProgress;

            var cancellationSource = new CancellationTokenSource();

            // Raise a download complete for each file that was already downloaded
            update
                .Files
                .Where(f => IsFileDownloaded(f))
                .ToList()
                .ForEach(f => RepositoryOperationProgress?.Invoke(this, new ContentOperationProgress() { CurrentOperation = OperationType.DownloadFileEnd, File = f }));

            foreach (var file in update.Files.Where(file => !IsFileDownloaded(file)))
            {
                RepositoryOperationProgress?.Invoke(this, new ContentOperationProgress() { CurrentOperation = OperationType.DownloadFileStart, File = file });

                // Create the directory structure where the file will be downloaded
                var contentFilePath = GetUpdateFilePath(file);
                var contentFileDirectory = Path.GetDirectoryName(contentFilePath);
                if (!Directory.Exists(contentFileDirectory))
                {
                    Directory.CreateDirectory(contentFileDirectory);
                }
                
                // Download the file (or resume and interrupted download)
                contentDownloader.DownloadToFile(GetUpdateFilePath(file), file, cancellationSource.Token);

                RepositoryOperationProgress?.Invoke(this, new ContentOperationProgress() { CurrentOperation = OperationType.DownloadFileEnd, File = file });

                RepositoryOperationProgress?.Invoke(this, new ContentOperationProgress() { CurrentOperation = OperationType.HashFileStart, File = file });

                // Check the hash; must match the strongest hash specified in the update metadata
                if (hashChecker.Check(file, contentFilePath))
                {
                    var markerFile = File.Create(GetUpdateFileMarkerPath(file));
                    markerFile.Dispose();
                }

                RepositoryOperationProgress?.Invoke(this, new ContentOperationProgress() { CurrentOperation = OperationType.HashFileEnd, File = file });
            }
        }

        /// <summary>
        /// Forwards hashing progress notifications to listeners of repository progress notifications
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Hashing progress notification</param>
        private void HashChecker_OnHashingProgress(object sender, OperationProgress e)
        {
            RepositoryOperationProgress?.Invoke(this, e);
        }

        /// <summary>
        /// Handles download progress notifications from the content downloader by forwarding them to registered event handlers of the store
        /// </summary>
        /// <param name="sender">The content downloader</param>
        /// <param name="e">Progress data</param>
        private void ContentDownloader_OnDownloadProgress(object sender, OperationProgress e)
        {
            RepositoryOperationProgress?.Invoke(this, e);
        }

        /// <summary>
        /// Export selected updates from the repository, using the specified format
        /// </summary>
        /// <param name="filter">Filter which updates to export from the repository</param>
        /// <param name="exportFilePath">Export file path</param>
        /// <param name="format">Export file format</param>
        public void Export(RepositoryFilter filter, string exportFilePath, RepoExportFormat format)
        {
            var updatesToExport = GetUpdates(filter, UpdateRetrievalMode.Extended);

            if (format == RepoExportFormat.WSUS_2016)
            {
                if (ServiceConfiguration == null)
                {
                    throw new Exception("Missing configuration data");
                }

                var exporter = new WsusExport(this);
                exporter.ExportProgress += Exporter_ExportProgress;
                exporter.Export(updatesToExport, exportFilePath);
            }
        }

        /// <summary>
        /// Forwarder for export notifications
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">Export progress</param>
        private void Exporter_ExportProgress(object sender, OperationProgress e)
        {
            RepositoryOperationProgress?.Invoke(this, e);
        }

        /// <summary>
        /// Check if an update XML is available locally
        /// </summary>
        /// <param name="update">The update to check</param>
        /// <returns>True if the updates's XML is available, false otherwise</returns>
        bool IRepositoryInternal.IsUpdateXmlAvailable(Update update)
        {
            return File.Exists(GetUpdateXmlPath(update));
        }

        /// <summary>
        /// Opens a writeable stream for an update's XML
        /// </summary>
        /// <param name="update">The update whose XML will be written</param>
        /// <returns>FileStream for update XML</returns>
        Stream IRepositoryInternal.GetUpdateXmlWriteStream(Update update)
        {
            var xmlPath = GetUpdateXmlPath(update);
            var xmlParentDirectory = Path.GetDirectoryName(xmlPath);
            if (!Directory.Exists(xmlParentDirectory))
            {
                Directory.CreateDirectory(xmlParentDirectory);
            }

            return File.Create(xmlPath);
        }

        /// <summary>
        /// Opens an update's XML for reading
        /// </summary>
        /// <param name="update">The update to read XML</param>
        /// <returns>StreamReader for the update XML</returns>
        StreamReader IRepositoryInternal.GetUpdateXmlReader(Update update)
        {
            return GetUpdateXmlReaderPrivate(update);
        }

        StreamReader GetUpdateXmlReaderPrivate(Update update)
        {
            return File.OpenText(GetUpdateXmlPath(update));
        }

        /// <summary>
        /// Returns all categories that match the filter
        /// </summary>
        /// <param name="filter">Categories filter</param>
        /// <returns>List of categories that match the filter</returns>
        public List<Update> GetCategories(RepositoryFilter filter)
        {
            return Categories.GetCategories(filter);
        }

        /// <summary>
        /// Returns all categories present in the repository
        /// </summary>
        /// <returns>List of categories: classifications, detectoids, products</returns>
        public List<Update> GetCategories()
        {
            return Categories.GetCategories();
        }

        /// <summary>
        /// Returns all updates that match the filter
        /// </summary>
        /// <param name="filter">Updates filter</param>
        /// <param name="metadataMode">Level of metadata to retrieve.</param>
        /// <returns>List of updates that match the filter</returns>
        public List<Update> GetUpdates(RepositoryFilter filter, UpdateRetrievalMode metadataMode)
        {
            var filteredUpdates = Updates.GetUpdates(filter);

            if(metadataMode == UpdateRetrievalMode.Extended)
            {
                filteredUpdates.ForEach(u =>
                {
                    using (var xmlReader = GetUpdateXmlReaderPrivate(u))
                    {
                        u.LoadExtendedAttributesFromXml(xmlReader, Files);
                    }
                });
            }

            return filteredUpdates;
        }

        /// <summary>
        /// Returns all updates present in the repository
        /// </summary>
        /// <param name="metadataMode">Level of metadata to retrieve.</param>
        /// <returns>List of updates</returns>
        public List<Update> GetUpdates(UpdateRetrievalMode metadataMode)
        {
            var allUpdates = Updates.GetUpdates();

            if (metadataMode == UpdateRetrievalMode.Extended)
            {
                allUpdates.ForEach(u =>
                {
                    using (var xmlReader = GetUpdateXmlReaderPrivate(u))
                    {
                        u.LoadExtendedAttributesFromXml(xmlReader, Files);
                    }
                });
            }

            return allUpdates;
        }

        /// <summary>
        /// Get an update in the repository by ID
        /// </summary>
        /// <param name="updateId">The update ID to lookup</param>
        /// <param name="metadataMode">Level of metadata to retrieve.</param>
        /// <returns>The requested update</returns>
        public Update GetUpdate(Identity updateId, UpdateRetrievalMode metadataMode)
        {
            var update = Updates.Index[updateId];
            if (metadataMode == UpdateRetrievalMode.Extended)
            {
                using (var xmlReader = GetUpdateXmlReaderPrivate(update))
                {
                    update.LoadExtendedAttributesFromXml(xmlReader, Files);
                }
            }

            return update;
        }

        /// <summary>
        /// Returns the anchor received after the last categories query
        /// </summary>
        /// <returns>Anchor string</returns>
        string IRepositoryInternal.GetCategoriesAnchor()
        {
            return Categories.LastQuery?.Anchor;
        }

        /// <summary>
        /// Returns the anchor received after the last updates query that used the specified filter
        /// </summary>
        /// <param name="filter">The filter used in the query</param>
        /// <returns>Anchor string</returns>
        string IRepositoryInternal.GetUpdatesAnchorForFilter(QueryFilter filter)
        {
            return Updates.UpdateQueries.Find(q => q.Equals(filter))?.Anchor;
        }

        void IRepositoryInternal.SetServiceConfiguration(ServerSyncConfigData configData)
        {
            SetServiceConfiguration(configData);
        }

        void IRepositoryInternal.SetAccessToken(ServiceAccessToken newAccessToken)
        {
            SetAccessToken(newAccessToken);
        }
    }
}
