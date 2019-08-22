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
    /// Results are returned as <see cref="IMetadataSource"/>, through which advanced queries and filtering can be performed.
    /// </summary>
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
        /// Generate a unique file name for saving the results of a query
        /// </summary>
        /// <returns></returns>
        private string GetQueryResultFileName() => $"QueryResult-{DateTime.Now.ToFileTime()}.zip";

        /// <summary>
        /// Initializes a new instance of UpstreamServerClient.
        /// </summary>
        /// <param name="upstreamEndpoint">The server endpoint this client will connect to.</param>
        public UpstreamServerClient(Endpoint upstreamEndpoint)
        {
            UpstreamEndpoint = upstreamEndpoint;

            var httpBindingWithTimeout = new System.ServiceModel.BasicHttpBinding()
            {
                ReceiveTimeout = new TimeSpan(0, 3, 0),
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
        }

        /// <summary>
        /// Updates the access token of this client
        /// </summary>
        /// <returns>Nothing</returns>
        internal async Task RefreshAccessToken(string accountName, Guid? accountGuid)
        {
            var progress = new MetadataQueryProgress();
            progress.CurrentTask = MetadataQueryStage.AuthenticateStart;
            MetadataQueryProgress?.Invoke(this, progress);

            var authenticator = new ClientAuthenticator(UpstreamEndpoint, accountName, accountGuid.HasValue ? accountGuid.Value : new Guid());
            AccessToken = await authenticator.Authenticate(AccessToken);

            progress.CurrentTask = MetadataQueryStage.AuthenticateEnd;
            MetadataQueryProgress?.Invoke(this, progress);
        }

        /// <summary>
        /// Updates the server config data for this client
        /// </summary>
        /// <returns></returns>
        internal async Task RefreshServerConfigData()
        {
            ConfigData = await GetServerConfigData();
        }

        /// <summary>
        /// Retrieves configuration data from the upstream server.
        /// </summary>
        /// <returns>Server configuration data</returns>
        public async Task<ServerSyncConfigData> GetServerConfigData()
        {
            await RefreshAccessToken(Guid.NewGuid().ToString(), Guid.NewGuid());
            var progress = new MetadataQueryProgress();
            progress.CurrentTask = MetadataQueryStage.GetServerConfigStart;
            MetadataQueryProgress?.Invoke(this, progress);

            var result = await QueryConfigData();

            progress.CurrentTask = MetadataQueryStage.GetServerConfigEnd;
            MetadataQueryProgress?.Invoke(this, progress);

            return result;
        }

        /// <summary>
        /// Gets the list of categories from the upstream server and adds them to the specified metadata collection.
        /// </summary>
        /// <param name="destination">Metadata collection where to add the results. If the collection implements <see cref="IMetadataSource"/>, only delta changes are retrieved and added to the destination.</param>
        public async Task GetCategories(IMetadataSink destination)
        {
            var deltaMetadataSource = destination as IMetadataSource;
            var progress = new MetadataQueryProgress();

            if (AccessToken == null || AccessToken.ExpiresIn(TimeSpan.FromMinutes(2)))
            {
                await RefreshAccessToken(deltaMetadataSource?.UpstreamAccountName, deltaMetadataSource?.UpstreamAccountGuid);
            }

            // If no configuration is known, query it now
            if (ConfigData == null)
            {
                await RefreshServerConfigData();
            }

            // Query IDs for all categories known to the upstream server
            progress.CurrentTask = MetadataQueryStage.GetRevisionIdsStart;
            MetadataQueryProgress?.Invoke(this, progress);

            var categoriesAnchor = deltaMetadataSource?.CategoriesAnchor;
            var categoryQueryResult = await GetCategoryIds(categoriesAnchor);

            progress.CurrentTask = MetadataQueryStage.GetRevisionIdsEnd;
            MetadataQueryProgress?.Invoke(this, progress);

            var cachedCategories = deltaMetadataSource?.CategoriesIndex;

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

            destination.SetCategoriesAnchor(categoryQueryResult.anchor);

            GetUpdateDataForIds(
                updatesToRetrieveDataFor.Select(id => id.Raw).ToList(), destination);

            progress.CurrentTask = MetadataQueryStage.GetUpdateMetadataEnd;
            MetadataQueryProgress?.Invoke(this, progress);
        }

        /// <summary>
        /// Gets the list of categories from the upstream update server.
        /// </summary>
        /// <returns>An updates metadata source containing all.</returns>
        public async Task<CompressedMetadataStore> GetCategories()
        {
            var queryResult = new CompressedMetadataStore(GetQueryResultFileName(), UpstreamEndpoint);

            await GetCategories(queryResult);

            queryResult.Commit();

            return queryResult;
        }

        /// <summary>
        /// Gets the list of updates matching the query filter from an upstream update server.
        /// <para>
        /// If the destinatin metadata sink also implements <see cref="IMetadataSource", a delta query for changed categories is performed./>
        /// </para>
        /// </summary>
        /// <param name="updatesFilter">Updates filter. See <see cref="QueryFilter"/> for details.</param>
        /// <param name="destination">Metadata collection where to write the result. If the destination implements IMetadataSource, a delta retrieve is performed</param>
        /// <remarks>
        /// </remarks>
        public async Task GetUpdates(QueryFilter updatesFilter, IMetadataSink destination)
        {
            var deltaMetadataSource = destination as IMetadataSource;

            var progress = new MetadataQueryProgress();

            if (updatesFilter == null || updatesFilter.ProductsFilter.Count == 0 || updatesFilter.ClassificationsFilter.Count == 0)
            {
                throw new Exception("The filter cannot be null of empty");
            }

            if (AccessToken == null || AccessToken.ExpiresIn(TimeSpan.FromMinutes(2)))
            {
                await RefreshAccessToken(deltaMetadataSource?.UpstreamAccountName, deltaMetadataSource?.UpstreamAccountGuid);
            }

            // If no configuration is known, query it now
            if (ConfigData == null)
            {
                await RefreshServerConfigData();
            }

            progress.CurrentTask = MetadataQueryStage.GetRevisionIdsStart;
            MetadataQueryProgress?.Invoke(this, progress);

            var updatesAnchor = deltaMetadataSource?.GetAnchorForFilter(updatesFilter);
            updatesFilter.Anchor = updatesAnchor;
            var updatesQueryResult = await GetUpdateIds(updatesFilter);

            progress.CurrentTask = MetadataQueryStage.GetRevisionIdsEnd;
            MetadataQueryProgress?.Invoke(this, progress);

            // Find all updates that did not change
            var unchangedUpdates = GetUnchangedUpdates(deltaMetadataSource?.UpdatesIndex, updatesQueryResult.identities);

            // Create the list of updates to query data for. Remove those updates that were reported as "new"
            // but for which we already have metadata
            var updatesToRetrieveDataFor = updatesQueryResult.identities.Where(
                newUpdateId => !unchangedUpdates.ContainsKey(newUpdateId));

            progress.CurrentTask = MetadataQueryStage.GetUpdateMetadataStart;
            progress.Maximum = updatesToRetrieveDataFor.Count();
            progress.Current = 0;
            MetadataQueryProgress?.Invoke(this, progress);

            GetUpdateDataForIds(
                updatesToRetrieveDataFor.Select(id => id.Raw).ToList(), destination);

            // Update the QueryResult filter and anchor
            updatesFilter.Anchor = updatesQueryResult.anchor;
            destination.SetQueryFilter(updatesFilter);

            progress.CurrentTask = MetadataQueryStage.GetUpdateMetadataEnd;
            MetadataQueryProgress?.Invoke(this, progress);
        }

        /// <summary>
        /// Gets updates matching the query filter from an upstream update server.
        /// </summary>
        /// <param name="updatesFilter">Updates filter. See <see cref="QueryFilter"/> for details.</param>
        /// <returns>An updates metadata source containing updates that match the filter.</returns>
        public async Task<CompressedMetadataStore> GetUpdates(QueryFilter updatesFilter)
        {
            var queryResult = new CompressedMetadataStore(GetQueryResultFileName(), UpstreamEndpoint);

            await GetUpdates(updatesFilter, queryResult);

            queryResult.Commit();

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
        private async Task<(string anchor, IEnumerable<Metadata.Identity> identities)> GetUpdateIds(QueryFilter filter)
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
        /// <param name="destination">The metadata destination to write update metadata to</param>
        private void GetUpdateDataForIds(List<UpdateIdentity> updateIds, IMetadataSink destination)
        {
            // Data retrieval is done is done in batches of upto MaxNumberOfUpdatesPerRequest
            var retrieveBatches = CreateBatchedListFromFlatList(updateIds, ConfigData.MaxNumberOfUpdatesPerRequest);

            // Progress tracking and reporting
            int batchesDone = 0;
            var progress = new MetadataQueryProgress() { CurrentTask = MetadataQueryStage.GetUpdateMetadataProgress, Maximum = updateIds.Count, Current = 0 };
            MetadataQueryProgress?.Invoke(this, progress);

            // Run batches in parallel
            retrieveBatches.AsParallel().ForAll(batch =>
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
                        updateDataReply = ServerSyncClient.GetUpdateDataAsync(updateDataRequest).GetAwaiter().GetResult();
                    }
                    catch (System.TimeoutException)
                    {
                        updateDataReply = null;
                    }
                    retryCount++;
                } while (updateDataReply == null && retryCount < 10);

                if (updateDataReply == null || updateDataReply.GetUpdateDataResponse1 == null || updateDataReply.GetUpdateDataResponse1.GetUpdateDataResult == null)
                {
                    throw new Exception("Failed to get update metadata");
                }

                // Parse the list of raw files into a more usable format
                var filesList = new List<UpdateFileUrl>(updateDataReply.GetUpdateDataResponse1.GetUpdateDataResult.fileUrls.Select(rawFile => new UpdateFileUrl(rawFile)));

                // First add the files information to the store; it will be used to link update files with urls later
                filesList.ForEach(file => destination.AddFile(file));

                // Add the updates to the result, converting them to a higher level representation
                destination.AddUpdates(updateDataReply.GetUpdateDataResponse1.GetUpdateDataResult.updates);

                lock (destination)
                {
                    // Track progress
                    batchesDone++;
                    progress.PercentDone = ((double)batchesDone * 100) / retrieveBatches.Count;
                    progress.Current += batch.Count();
                    MetadataQueryProgress?.Invoke(this, progress);
                }
            });
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
