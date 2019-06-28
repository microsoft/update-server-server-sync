// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.LocalCache;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.WebServices.ServerSync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.UpdateServices
{
    /// <summary>
    /// Query updates, metadata and content from an upstream update server
    /// </summary>
    public class UpstreamServerClient
    {
        /// <summary>
        /// The endpoint this instance is connecting to
        /// </summary>
        public readonly Endpoint UpstreamEndpoint;

        /// <summary>
        /// Client used to issue SOAP requests
        /// </summary>
        private readonly IServerSyncWebService ServerSyncClient;

        /// <summary>
        /// Cached access cookie. If not set in the constructor, a new access token will be obtained
        /// </summary>
        public ServiceAccessToken AccessToken;

        /// <summary>
        /// Service configuration data. Contains maximum query limits, etc.
        /// If not passed to the constructor, this class will retrieve it from the service
        /// </summary>
        public ServerSyncConfigData ConfigData;

        public event EventHandler<MetadataQueryProgress> MetadataQueryProgress;
        public event EventHandler<MetadataQueryProgress> MetadataQueryComplete;

        /// <summary>
        /// Instantiate a client used to communicate the upstream update server and the specified endpoint.
        /// </summary>
        /// <param name="upstreamEndpoint">The server endpoint</param>
        /// <param name="configData">Optional cached server configuration. If null, the config data is queries from the server</param>
        /// <param name="accessToken">Optional cached access token. If null, a new access token is requested.</param>
        public UpstreamServerClient(Endpoint upstreamEndpoint, ServerSyncConfigData configData = null, ServiceAccessToken accessToken = null)
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

            httpBindingWithTimeout.Security.Mode = System.ServiceModel.BasicHttpSecurityMode.Transport;

            ServerSyncClient = new ServerSyncWebServiceClient(
                httpBindingWithTimeout,
                new System.ServiceModel.EndpointAddress(UpstreamEndpoint.ServerSyncRoot));

            AccessToken = accessToken;
            ConfigData = configData;
        }

        /// <summary>
        /// Updates the access token of this client
        /// </summary>
        /// <returns>Nothing</returns>
        public async Task RefreshAccessToken()
        {
            var progress = new MetadataQueryProgress();
            progress.CurrentTask = QuerySubTaskTypes.AuthenticateStart;
            MetadataQueryProgress?.Invoke(this, progress);

            var authenticator = new ClientAuthenticator(UpstreamEndpoint);
            AccessToken = await authenticator.Authenticate(AccessToken);

            progress.CurrentTask = QuerySubTaskTypes.AuthenticateEnd;
            MetadataQueryProgress?.Invoke(this, progress);
        }

        /// <summary>
        /// Updates the server config data for this client
        /// </summary>
        /// <returns></returns>
        public async Task RefreshServerConfigData()
        {
            var progress = new MetadataQueryProgress();
            progress.CurrentTask = QuerySubTaskTypes.GetServerConfigStart;
            MetadataQueryProgress?.Invoke(this, progress);

            ConfigData = await QueryConfigData();

            progress.CurrentTask = QuerySubTaskTypes.GetServerConfigEnd;
            MetadataQueryProgress?.Invoke(this, progress);
        }
        
        /// <summary>
        /// Retrieves the list of categories from the upstream update server
        /// </summary>
        /// <param name="cachedMetadata">Cached categories. If provided, this method retrieves only the updates that are new or changed</param>
        /// <returns>A categories query result: new categories list and anchor</returns>
        public async Task<Query.QueryResult> GetCategories(CategoriesCache cachedMetadata = null)
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
            progress.CurrentTask = QuerySubTaskTypes.GetRevisionIdsStart;
            MetadataQueryProgress?.Invoke(this, progress);

            var categoryQueryResult = await GetCategoryIds(cachedMetadata?.LastQuery?.Anchor);

            progress.CurrentTask = QuerySubTaskTypes.GetRevisionIdsEnd;
            MetadataQueryProgress?.Invoke(this, progress);

            var cachedCategories = cachedMetadata?.Categories;

            // Find all updates that did not change
            var unchangedUpdates = GetUnchangedUpdates(cachedCategories, categoryQueryResult.identities);

            // Create the list of updates to query data for. Remove those updates that were reported as "new"
            // but for which we already have metadata
            var updatesToRetrieveDataFor = categoryQueryResult.identities.Where(
                newUpdateId => !unchangedUpdates.ContainsKey(newUpdateId));

            // Retrieve metadata for all new categories
            progress.CurrentTask = QuerySubTaskTypes.GetUpdateMetadataStart;
            progress.Maximum = updatesToRetrieveDataFor.Count();
            progress.Current = 0;
            MetadataQueryProgress?.Invoke(this, progress);

            var queryResult = Query.QueryResult.CreateCategoriesQueryResult(categoryQueryResult.anchor);

            await GetUpdateDataForIds(
                updatesToRetrieveDataFor.Select(id => id.Raw).ToList(), queryResult);

            progress.CurrentTask = QuerySubTaskTypes.GetUpdateMetadataEnd;
            MetadataQueryProgress?.Invoke(this, progress);

            return queryResult;
        }

        /// <summary>
        /// Retrieves the list of updates from the upstream update server, using the specified filter
        /// </summary>
        /// <param name="cachedMetadata">Cached updates. If provided, this method retrieves only the updates that are new or changed</param>
        /// <returns>An updates query result: new updates list and anchor</returns>
        public async Task<Query.QueryResult> GetUpdates(Query.QueryFilter updatesFilter, UpdatesCache cachedMetadata = null)
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
            
            // Check if this filter was used in the past, and if yes its anchor will be used to get only changed updates since then
            var cachedFilter = cachedMetadata?.UpdateQueries.Find(filter => filter.Equals(updatesFilter));

            // If the filter was not used in the past, use the passed in filter
            var effectiveFilter = cachedFilter == null ? updatesFilter : cachedFilter;

            progress.CurrentTask = QuerySubTaskTypes.GetRevisionIdsStart;
            MetadataQueryProgress?.Invoke(this, progress);

            var updatesQueryResult = await GetUpdateIds(effectiveFilter);

            progress.CurrentTask = QuerySubTaskTypes.GetRevisionIdsEnd;
            MetadataQueryProgress?.Invoke(this, progress);

            var cachedUpdates = cachedMetadata?.Updates;

            // Find all updates that did not change
            var unchangedUpdates = GetUnchangedUpdates(cachedUpdates, updatesQueryResult.identities);

            // Create the list of updates to query data for. Remove those updates that were reported as "new"
            // but for which we already have metadata
            var updatesToRetrieveDataFor = updatesQueryResult.identities.Where(
                newUpdateId => !unchangedUpdates.ContainsKey(newUpdateId));

            progress.CurrentTask = QuerySubTaskTypes.GetUpdateMetadataStart;
            progress.Maximum = updatesToRetrieveDataFor.Count();
            progress.Current = 0;
            MetadataQueryProgress?.Invoke(this, progress);

            var queryResult = Query.QueryResult.CreateUpdatesQueryResult(effectiveFilter, updatesQueryResult.anchor);

            await GetUpdateDataForIds(
                updatesToRetrieveDataFor.Select(id => id.Raw).ToList(), queryResult);

            progress.CurrentTask = QuerySubTaskTypes.GetUpdateMetadataEnd;
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
        private async Task<(string anchor, IEnumerable<Metadata.MicrosoftUpdateIdentity> identities)> GetCategoryIds(string oldAnchor = null)
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
                revisionsIdReply.GetRevisionIdListResponse1.GetRevisionIdListResult.NewRevisions.Select(rawId => new Metadata.MicrosoftUpdateIdentity(rawId)));
        }

        /// <summary>
        /// Retrieves category IDs from the update server: classifications, products and detectoids
        /// </summary>
        /// <param name="oldAnchor">The anchor returned by a previous call to this function. Can be null.</param>
        /// <returns>The list of category IDs and an anchor. If an anchor was passed in, the
        /// list of category IDs is a delta list of categories changed since the anchor was generated.</returns>
        private async Task<(string anchor, IEnumerable<Metadata.MicrosoftUpdateIdentity> identities)> GetUpdateIds(Query.QueryFilter filter)
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
                revisionsIdReply.GetRevisionIdListResponse1.GetRevisionIdListResult.NewRevisions.Select(rawId => new Metadata.MicrosoftUpdateIdentity(rawId)));
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
            var progress = new MetadataQueryProgress() { CurrentTask = QuerySubTaskTypes.GetUpdateMetadataProgress, Maximum = updateIds.Count, Current = 0 };
            MetadataQueryProgress?.Invoke(this, progress);

            foreach (var batch in retrieveBatches)
            {
                var updateDataRequest = new GetUpdateDataRequest();
                updateDataRequest.GetUpdateData = new GetUpdateDataRequestBody();
                updateDataRequest.GetUpdateData.cookie = AccessToken.AccessCookie;
                updateDataRequest.GetUpdateData.updateIds = batch;
                var updateDataReply = await ServerSyncClient.GetUpdateDataAsync(updateDataRequest);

                if (updateDataReply == null || updateDataReply.GetUpdateDataResponse1 == null || updateDataReply.GetUpdateDataResponse1.GetUpdateDataResult == null)
                {
                    throw new Exception("Failed to get update metadata");
                }

                // Parse the list of raw files into a more usable format
                var filesList = new List<UpdateFileUrl>(updateDataReply.GetUpdateDataResponse1.GetUpdateDataResult.fileUrls.Select(rawFile => new UpdateFileUrl(rawFile)));

                // Add the updates to the result, converting them to a higher level representation
                foreach (var overTheWireUpdate in updateDataReply.GetUpdateDataResponse1.GetUpdateDataResult.updates)
                {
                    result.AddUpdate(MicrosoftProduct.FromServerSyncUpdateData(overTheWireUpdate, filesList));
                }

                // Track progress
                batchesDone++;
                progress.PercentDone = ((double)batchesDone * 100) / retrieveBatches.Count;
                progress.Current += batch.Count();
                MetadataQueryProgress?.Invoke(this, progress);
            }

            progress.IsComplete = true;
            MetadataQueryComplete?.Invoke(this, progress);
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
        private Dictionary<MicrosoftUpdateIdentity, MicrosoftUpdate> GetUnchangedUpdates(Dictionary<MicrosoftUpdateIdentity, MicrosoftUpdate> cachedUpdates, IEnumerable<MicrosoftUpdateIdentity> newUpdateIds)
        {
            if (cachedUpdates == null)
            {
                return new Dictionary<MicrosoftUpdateIdentity, MicrosoftUpdate>();
            }

            var unchangedUpdates = new Dictionary<MicrosoftUpdateIdentity, MicrosoftUpdate>();
            foreach (var newUpdateId in newUpdateIds)
            {
                // Find cached updates that match the ID+revision of new updates (did not really change)
                if (cachedUpdates.TryGetValue(newUpdateId, out MicrosoftUpdate unchangedUpdate))
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
