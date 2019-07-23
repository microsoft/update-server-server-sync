// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Storage;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.WebServices.ServerSync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UpdateServices.Metadata.Content;

namespace Microsoft.UpdateServices.Client
{
    /// <summary>
    /// Query updates, metadata and content from an upstream update server.
    /// </summary>
    /// <remarks>
    /// It is recommended to use the UpstreamServerClient together with an <see cref="IRepository"/>. This enables caching of access tokens and service configuration,
    /// speeding up queries. Using a local repository enable retrieval of delta changes between the upstream server and the local repository.
    /// </remarks>
    public class UpstreamServerClient
    {
        /// <summary>
        /// Gets the update server <see cref="Endpoint"/> this client connects to.
        /// </summary>
        /// <value>
        /// Update server <see cref="Endpoint"/>
        /// </value>
        public Endpoint UpstreamEndpoint { get; private set; }

        /// <summary>
        /// Client used to issue SOAP requests
        /// </summary>
        private readonly IServerSyncWebService ServerSyncClient;

        /// <summary>
        /// Local updates cache. Contains cached access tokens, service configuration and updates
        /// </summary>
        internal IRepository LocalRepository;

        /// <summary>
        /// Cached access cookie. If not set in the constructor, a new access token will be obtained
        /// </summary>
        private ServiceAccessToken AccessToken;

        /// <summary>
        /// Service configuration data. Contains maximum query limits, etc.
        /// If not passed to the constructor, this class will retrieve it from the service
        /// </summary>
        private ServerSyncConfigData ConfigData;

        /// <summary>
        /// Raised on progress during a metadata query. Reports the current query stage.
        /// </summary>
        /// <value>Progress data</value>
        public event EventHandler<MetadataQueryProgress> MetadataQueryProgress;

        /// <summary>
        /// Initializes a new instance of UpstreamServerClient.
        /// </summary>
        /// <param name="upstreamEndpoint">The server endpoint this client will connect to.</param>
        /// <remarks>This constructor is not recommended for performance reasons. It is recommended to use the constructor that takes a local repository.
        /// Queries take a significant amount of time, and using a local repository enables delta queries, where only changes on the upstream server are retrieved.</remarks>
        public UpstreamServerClient(Endpoint upstreamEndpoint)
        {
            UpstreamEndpoint = upstreamEndpoint;
            LocalRepository = null;

            var httpBindingWithTimeout = new System.ServiceModel.BasicHttpBinding()
            {
                ReceiveTimeout = new TimeSpan(0, 10, 0),
                SendTimeout = new TimeSpan(0, 3, 0),
                MaxBufferSize = int.MaxValue,
                ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max,
                MaxReceivedMessageSize = int.MaxValue,
                AllowCookies = true
            };

            var serviceEndpoint = new System.ServiceModel.EndpointAddress(UpstreamEndpoint.ServerSyncURI);
            if (serviceEndpoint.Uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                httpBindingWithTimeout.Security.Mode = System.ServiceModel.BasicHttpSecurityMode.Transport;
            }

            ServerSyncClient = new ServerSyncWebServiceClient(httpBindingWithTimeout, serviceEndpoint);

            if (LocalRepository != null)
            {
                ConfigData = (LocalRepository as IRepositoryInternal).ServiceConfiguration;
                AccessToken = (LocalRepository as IRepositoryInternal).AccessToken;
            }
        }

        /// <summary>
        /// Initializes a new instance of UpstreamServerClient, based on the specified local repository. The upstream
        /// server endpoint is inherited from the local repository.
        /// </summary>
        /// <param name="localRepository">Local updates repository.
        /// <para>Cached data from the repository is used for queries.</para>
        /// <para>Query results are delta changes between the upstread server and the local repository.
        /// </para>
        /// </param>
        /// <example>
        /// <code>
        /// // Initialize a new local repository in the current directory, tracking the official Microsoft upstream server
        /// var newRepo = FileSystemRepository.Init(Environment.CurrentDirectory, Endpoint.Default.URI);
        /// 
        /// // Create a new client based on the local repository 
        /// var client = new UpstreamServerClient(newRepo);
        /// 
        /// var categories = await client.GetCategories();
        /// 
        /// // Save the categories query result in the local repository
        /// newRepo.MergeQueryResult(categories);
        /// </code>
        /// </example>
        public UpstreamServerClient(IRepository localRepository)
        {
            UpstreamEndpoint = localRepository.Configuration.UpstreamServerEndpoint;
            LocalRepository = localRepository;

            var httpBindingWithTimeout = new System.ServiceModel.BasicHttpBinding()
            {
                ReceiveTimeout = new TimeSpan(0, 10, 0),
                SendTimeout = new TimeSpan(0, 3, 0),
                MaxBufferSize = int.MaxValue,
                ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max,
                MaxReceivedMessageSize = int.MaxValue,
                AllowCookies = true
            };

            var serviceEndpoint = new System.ServiceModel.EndpointAddress(UpstreamEndpoint.ServerSyncURI);
            if (serviceEndpoint.Uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                httpBindingWithTimeout.Security.Mode = System.ServiceModel.BasicHttpSecurityMode.Transport;
            }

            ServerSyncClient = new ServerSyncWebServiceClient(httpBindingWithTimeout, serviceEndpoint);

            if (LocalRepository != null)
            {
                ConfigData = (LocalRepository as IRepositoryInternal).ServiceConfiguration;
                AccessToken = (LocalRepository as IRepositoryInternal).AccessToken;
            }
        }

        /// <summary>
        /// Updates the access token of this client
        /// </summary>
        /// <returns>Nothing</returns>
        internal async Task RefreshAccessToken()
        {
            var progress = new MetadataQueryProgress();
            progress.CurrentTask = MetadataQueryStage.AuthenticateStart;
            MetadataQueryProgress?.Invoke(this, progress);

            if (LocalRepository != null)
            {
                var authenticator = new ClientAuthenticator(UpstreamEndpoint, LocalRepository.Configuration.AccountName, LocalRepository.Configuration.AccountGuid.Value);
                AccessToken = await authenticator.Authenticate(AccessToken);
            }
            else
            {
                var authenticator = new ClientAuthenticator(UpstreamEndpoint);
                AccessToken = await authenticator.Authenticate(AccessToken);
            }
            
            progress.CurrentTask = MetadataQueryStage.AuthenticateEnd;
            MetadataQueryProgress?.Invoke(this, progress);

            if (LocalRepository != null)
            {
                (LocalRepository as IRepositoryInternal).SetAccessToken(AccessToken);
            }
        }

        /// <summary>
        /// Updates the server config data for this client
        /// </summary>
        /// <returns></returns>
        internal async Task RefreshServerConfigData()
        {
            var progress = new MetadataQueryProgress();
            progress.CurrentTask = MetadataQueryStage.GetServerConfigStart;
            MetadataQueryProgress?.Invoke(this, progress);

            ConfigData = await QueryConfigData();

            progress.CurrentTask = MetadataQueryStage.GetServerConfigEnd;
            MetadataQueryProgress?.Invoke(this, progress);

            if (LocalRepository != null)
            {
                (LocalRepository as IRepositoryInternal).SetServiceConfiguration(ConfigData);
            }
        }

        /// <summary>
        /// Gets the list of categories from the upstream update server.
        /// <para>If the client was initialized with a repository, only new or changed categories not present in the repository are retrieved.</para>
        /// </summary>
        /// <returns>A query result containing all or changed categories.</returns>
        public async Task<Query.QueryResult> GetCategories()
        {
            var progress = new MetadataQueryProgress();
        
            if (AccessToken == null || AccessToken.ExpiresIn(TimeSpan.FromMinutes(2)))
            {
                await RefreshAccessToken();
            }

            // If no configuration is known, query it now
            if (ConfigData == null)
            {
                await RefreshServerConfigData();
            }

            // Query IDs for all categories known to the upstream server
            progress.CurrentTask = MetadataQueryStage.GetRevisionIdsStart;
            MetadataQueryProgress?.Invoke(this, progress);

            var localRepositoryInternal = LocalRepository == null ? null : LocalRepository as IRepositoryInternal;
            var categoryQueryResult = await GetCategoryIds(localRepositoryInternal?.GetCategoriesAnchor());

            progress.CurrentTask = MetadataQueryStage.GetRevisionIdsEnd;
            MetadataQueryProgress?.Invoke(this, progress);

            var cachedCategories = LocalRepository?.CategoriesIndex;

            // Find all updates that did not change
            var unchangedUpdates = GetUnchangedUpdates(cachedCategories, categoryQueryResult.identities);

            // Create the list of updates to query data for. Remove those updates that were reported as "new"
            // but for which we already have metadata
            var updatesToRetrieveDataFor = categoryQueryResult.identities.Where(
                newUpdateId => !unchangedUpdates.ContainsKey(newUpdateId));

            // Retrieve metadata for all new categories
            progress.CurrentTask = MetadataQueryStage.GetUpdateMetadataStart;
            progress.Maximum = updatesToRetrieveDataFor.Count();
            progress.Current = 0;
            MetadataQueryProgress?.Invoke(this, progress);

            var queryResult = Query.QueryResult.CreateCategoriesQueryResult(categoryQueryResult.anchor);

            await GetUpdateDataForIds(
                updatesToRetrieveDataFor.Select(id => id.Raw).ToList(), queryResult);

            progress.CurrentTask = MetadataQueryStage.GetUpdateMetadataEnd;
            MetadataQueryProgress?.Invoke(this, progress);

            return queryResult;
        }

        /// <summary>
        /// Gets the list of updates matching the query filter from an upstream update server.
        /// </summary>
        /// <param name="updatesFilter">Updates filter. See <see cref="Query.QueryFilter"/> for details.</param>
        /// <returns>A query result containing all or changed updates that match the filter.</returns>
        /// <remarks>
        /// When a local repository is used to initialize the UpstreamServerClient, the query result is a delta relative to the local repository.
        /// The query result is not merged into the store. The caller can merge the query result using <see cref="IRepository.MergeQueryResult(Query.QueryResult)"/>
        /// </remarks>
        public async Task<Query.QueryResult> GetUpdates(Query.QueryFilter updatesFilter)
        {
            var progress = new MetadataQueryProgress();

            if (updatesFilter == null || updatesFilter.ProductsFilter.Count == 0 || updatesFilter.ClassificationsFilter.Count == 0)
            {
                throw new Exception("The filter cannot be null of empty");
            }

            if (AccessToken == null || AccessToken.ExpiresIn(TimeSpan.FromMinutes(2)))
            {
                await RefreshAccessToken();
            }

            // If no configuration is known, query it now
            if (ConfigData == null)
            {
                await RefreshServerConfigData();
            }

            progress.CurrentTask = MetadataQueryStage.GetRevisionIdsStart;
            MetadataQueryProgress?.Invoke(this, progress);

            var localRepositoryInternal = LocalRepository == null ? null : LocalRepository as IRepositoryInternal;
            updatesFilter.Anchor = localRepositoryInternal?.GetUpdatesAnchorForFilter(updatesFilter);
            var updatesQueryResult = await GetUpdateIds(updatesFilter);

            progress.CurrentTask = MetadataQueryStage.GetRevisionIdsEnd;
            MetadataQueryProgress?.Invoke(this, progress);

            // Find all updates that did not change
            var unchangedUpdates = GetUnchangedUpdates(LocalRepository?.UpdatesIndex, updatesQueryResult.identities);

            // Create the list of updates to query data for. Remove those updates that were reported as "new"
            // but for which we already have metadata
            var updatesToRetrieveDataFor = updatesQueryResult.identities.Where(
                newUpdateId => !unchangedUpdates.ContainsKey(newUpdateId));

            progress.CurrentTask = MetadataQueryStage.GetUpdateMetadataStart;
            progress.Maximum = updatesToRetrieveDataFor.Count();
            progress.Current = 0;
            MetadataQueryProgress?.Invoke(this, progress);

            var queryResult = Query.QueryResult.CreateUpdatesQueryResult(updatesFilter, updatesQueryResult.anchor);

            await GetUpdateDataForIds(
                updatesToRetrieveDataFor.Select(id => id.Raw).ToList(), queryResult);

            progress.CurrentTask = MetadataQueryStage.GetUpdateMetadataEnd;
            MetadataQueryProgress?.Invoke(this, progress);

            return queryResult;
        }

        /// <summary>
        /// Retrieves configuration data from the service
        /// </summary>
        /// <returns>The service configuration</returns>
        private async Task<ServerSyncConfigData> QueryConfigData()
        {
            var configDataRequest = new GetConfigDataRequest();
            configDataRequest.GetConfigData = new GetConfigDataRequestBody();
            configDataRequest.GetConfigData.configAnchor = null;
            configDataRequest.GetConfigData.cookie = AccessToken.AccessCookie;

            var configDataReply = await ServerSyncClient.GetConfigDataAsync(configDataRequest);
            if (configDataReply == null || configDataReply.GetConfigDataResponse1 == null || configDataReply.GetConfigDataResponse1.GetConfigDataResult == null)
            {
                throw new Exception("Failed to get config data.");
            }

            return configDataReply.GetConfigDataResponse1.GetConfigDataResult;
        }

        /// <summary>
        /// Retrieves category IDs from the update server: classifications, products and detectoids
        /// </summary>
        /// <param name="oldAnchor">The anchor returned by a previous call to this function. Can be null.</param>
        /// <returns>The list of category IDs and an anchor. If an anchor was passed in, the
        /// list of category IDs is a delta list of categories changed since the anchor was generated.</returns>
        private async Task<(string anchor, IEnumerable<Metadata.Identity> identities)> GetCategoryIds(string oldAnchor = null)
        {
            // Create a request for categories
            var revisionIdRequest = new GetRevisionIdListRequest();
            revisionIdRequest.GetRevisionIdList = new GetRevisionIdListRequestBody();
            revisionIdRequest.GetRevisionIdList.cookie = AccessToken.AccessCookie;
            revisionIdRequest.GetRevisionIdList.filter = new ServerSyncFilter();
            if (!string.IsNullOrEmpty(oldAnchor))
            {
                revisionIdRequest.GetRevisionIdList.filter.Anchor = oldAnchor;
            }
            
            // GetConfig must be true to request just categories
            revisionIdRequest.GetRevisionIdList.filter.GetConfig = true;

            var revisionsIdReply = await ServerSyncClient.GetRevisionIdListAsync(revisionIdRequest);
            if (revisionsIdReply == null || revisionsIdReply.GetRevisionIdListResponse1 == null || revisionsIdReply.GetRevisionIdListResponse1.GetRevisionIdListResult == null)
            {
                throw new Exception("Failed to get revision ID list");
            }

            // Return IDs and the anchor for this query. The anchor can be used to get a delta list in the future.
            return (
                revisionsIdReply.GetRevisionIdListResponse1.GetRevisionIdListResult.Anchor,
                revisionsIdReply.GetRevisionIdListResponse1.GetRevisionIdListResult.NewRevisions.Select(rawId => new Metadata.Identity(rawId)));
        }

        /// <summary>
        /// Retrieves category IDs from the update server: classifications, products and detectoids
        /// </summary>
        /// <param name="filter">The filter to use.</param>
        /// <returns>The list of category IDs and an anchor. If teh filter contains an anchor, the
        /// list of category IDs is a delta list of categories changed since the anchor was generated.</returns>
        private async Task<(string anchor, IEnumerable<Metadata.Identity> identities)> GetUpdateIds(Query.QueryFilter filter)
        {
            // Create a request for categories
            var revisionIdRequest = new GetRevisionIdListRequest();
            revisionIdRequest.GetRevisionIdList = new GetRevisionIdListRequestBody();
            revisionIdRequest.GetRevisionIdList.cookie = AccessToken.AccessCookie;
            revisionIdRequest.GetRevisionIdList.filter = filter.ToServerSyncFilter();

            // GetConfig must be false to request updates
            revisionIdRequest.GetRevisionIdList.filter.GetConfig = false;

            var revisionsIdReply = await ServerSyncClient.GetRevisionIdListAsync(revisionIdRequest);
            if (revisionsIdReply == null || revisionsIdReply.GetRevisionIdListResponse1 == null || revisionsIdReply.GetRevisionIdListResponse1.GetRevisionIdListResult == null)
            {
                throw new Exception("Failed to get revision ID list");
            }

            // Return IDs and the anchor for this query. The anchor can be used to get a delta list in the future.
            return (
                revisionsIdReply.GetRevisionIdListResponse1.GetRevisionIdListResult.Anchor,
                revisionsIdReply.GetRevisionIdListResponse1.GetRevisionIdListResult.NewRevisions.Select(rawId => new Metadata.Identity(rawId)));
        }

        /// <summary>
        /// Retrieves update data for the list of update ids
        /// </summary>
        /// <param name="updateIds">The ids to retrieve data for</param>
        /// <param name="result">A QueryResult to which retrieved update metadata is appended</param>
        private async Task GetUpdateDataForIds(List<UpdateIdentity> updateIds, Query.QueryResult result)
        {
            // Data retrieval is done is done in batches of upto MaxNumberOfUpdatesPerRequest
            var retrieveBatches = CreateBatchedListFromFlatList(updateIds, ConfigData.MaxNumberOfUpdatesPerRequest);

            // Progress tracking and reporting
            int batchesDone = 0;
            var progress = new MetadataQueryProgress() { CurrentTask = MetadataQueryStage.GetUpdateMetadataProgress, Maximum = updateIds.Count, Current = 0 };
            MetadataQueryProgress?.Invoke(this, progress);

            foreach (var batch in retrieveBatches)
            {
                var updateDataRequest = new GetUpdateDataRequest();
                updateDataRequest.GetUpdateData = new GetUpdateDataRequestBody();
                updateDataRequest.GetUpdateData.cookie = AccessToken.AccessCookie;
                updateDataRequest.GetUpdateData.updateIds = batch;

                GetUpdateDataResponse updateDataReply;
                int retryCount = 0;
                do
                {
                    try
                    {
                        updateDataReply = await ServerSyncClient.GetUpdateDataAsync(updateDataRequest);
                    }
                    catch (System.TimeoutException)
                    {
                        updateDataReply = null;
                    }
                    retryCount++;
                } while (updateDataReply != null && retryCount < 3);

                if (updateDataReply == null || updateDataReply.GetUpdateDataResponse1 == null || updateDataReply.GetUpdateDataResponse1.GetUpdateDataResult == null)
                {
                    throw new Exception("Failed to get update metadata");
                }

                // Parse the list of raw files into a more usable format
                var filesList = new List<UpdateFileUrl>(updateDataReply.GetUpdateDataResponse1.GetUpdateDataResult.fileUrls.Select(rawFile => new UpdateFileUrl(rawFile)));

                // Add the updates to the result, converting them to a higher level representation
                foreach (var overTheWireUpdate in updateDataReply.GetUpdateDataResponse1.GetUpdateDataResult.updates)
                {
                    result.AddUpdate(Product.FromServerSyncUpdateData(overTheWireUpdate));
                }

                filesList.ForEach(file => result.AddFile(file));

                // Track progress
                batchesDone++;
                progress.PercentDone = ((double)batchesDone * 100) / retrieveBatches.Count;
                progress.Current += batch.Count();
                MetadataQueryProgress?.Invoke(this, progress);
            }
        }

        /// <summary>
        /// Breaks down a flat list of objects in a list of batches, each batch having a maximum allowed size
        /// </summary>
        /// <typeparam name="T">The type of objects to batch</typeparam>
        /// <param name="flatList">The flat list of objects to break down</param>
        /// <param name="maxBatchSize">The maximum size of a batch</param>
        /// <returns>The batched list</returns>
        private List<T[]> CreateBatchedListFromFlatList<T>(List<T> flatList, int maxBatchSize)
        {
            // Figure out how many batches we have
            var batchCount = flatList.Count / maxBatchSize;
            // One more batch for the remaininig objects, if any
            batchCount += flatList.Count % maxBatchSize == 0 ? 0 : 1;

            List<T[]> batches = new List<T[]>(batchCount);
            for (int i = 0; i < batchCount; i++)
            {
                var batchSize = maxBatchSize;
                // If this is the last batch, the size might not be the max allowed size but the remainder of elements
                if (i == batchCount - 1 && flatList.Count % maxBatchSize != 0)
                {
                    batchSize = flatList.Count % maxBatchSize;
                }

                // Add the new batch to the batches list
                batches.Add(flatList.GetRange(i * maxBatchSize, batchSize).ToArray());
            }

            return batches;
        }

        /// <summary>
        /// Given a list of cached updates and a list of new update identities( ID+revision), returns
        /// those cached updates that did not change.
        /// </summary>
        /// <param name="cachedUpdates">List of locally cached updates</param>
        /// <param name="newUpdateIds">List of new update identities reported by the upstream server</param>
        /// <returns>List of locally cached updates that did not change</returns>
        private Dictionary<Identity, Update> GetUnchangedUpdates(IReadOnlyDictionary<Identity, Update> cachedUpdates, IEnumerable<Identity> newUpdateIds)
        {
            if (cachedUpdates == null)
            {
                return new Dictionary<Identity, Update>();
            }

            var unchangedUpdates = new Dictionary<Identity, Update>();
            foreach (var newUpdateId in newUpdateIds)
            {
                // Find cached updates that match the ID+revision of new updates (did not really change)
                if (cachedUpdates.TryGetValue(newUpdateId, out Update unchangedUpdate))
                {
                    unchangedUpdates.Add(unchangedUpdate.Identity, unchangedUpdate);
                }
            }

            var newUpdatesDictionary = newUpdateIds.ToDictionary(id => id);

            foreach(var cachedUpdate in cachedUpdates.Values)
            {
                // Find those cached updates that do not appear in the new updates list received from the server
                if (!newUpdatesDictionary.ContainsKey(cachedUpdate.Identity))
                {
                    unchangedUpdates.Add(cachedUpdate.Identity, cachedUpdate);
                }
            }

            return unchangedUpdates;
        }
    }
}
