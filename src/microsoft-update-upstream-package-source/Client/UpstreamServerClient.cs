// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.WebServices.ServerSync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Content;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Source
{
    /// <summary>
    /// <para>
    /// Retrieves update metadata for expired updates from an upstream update server.
    /// </para>
    /// <para>
    /// This class should only be used for retrieving individual expired updates when their ID is known. For querying updates use <see cref="UpstreamUpdatesSource"/>. 
    /// For querying products and classifications, use <see cref="UpstreamCategoriesSource"/>
    /// </para>
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

        internal async Task RefreshAccessToken(string accountName, Guid? accountGuid)
        {
            var progress = new MetadataQueryProgress
            {
                CurrentTask = MetadataQueryStage.AuthenticateStart
            };
            MetadataQueryProgress?.Invoke(this, progress);

            var authenticator = new ClientAuthenticator(UpstreamEndpoint, accountName, accountGuid ?? new Guid());
            AccessToken = await authenticator.Authenticate(AccessToken);

            progress.CurrentTask = MetadataQueryStage.AuthenticateEnd;
            MetadataQueryProgress?.Invoke(this, progress);
        }

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
            var progress = new MetadataQueryProgress
            {
                CurrentTask = MetadataQueryStage.GetServerConfigStart
            };
            MetadataQueryProgress?.Invoke(this, progress);

            var result = await QueryConfigData();

            progress.CurrentTask = MetadataQueryStage.GetServerConfigEnd;
            MetadataQueryProgress?.Invoke(this, progress);

            return result;
        }

        /// <summary>
        /// Attempts to retrieve metadata for an update that has expired and was removed from the update catalog index.
        /// Sometimes, the metadata for the expired update can still be retrieved.
        /// This method takes the update ID (without revision), a starting revision and a maximum range of revisions to attempt retrieval for. This method returns the metadata corresponding to the first revision found.
        /// </summary>
        /// <param name="partialId">The update ID, without the revision part.</param>
        /// <param name="revisionHint">The revision at which to start the search.</param>
        /// <param name="searchSpaceWindow">The range of revisions to attempt retrieval for.</param>
        /// <returns>An update if a revision was found, null otherwise</returns>
        public async Task<MicrosoftUpdatePackage> TryGetExpiredUpdate(Guid partialId, int revisionHint, int searchSpaceWindow)
        {
            if (AccessToken == null || AccessToken.ExpiresIn(TimeSpan.FromMinutes(2)))
            {
                await RefreshAccessToken(null, null);
            }

            // If no configuration is known, query it now
            if (ConfigData == null)
            {
                await RefreshServerConfigData();
            }

            var (updateData, _) = TryGetExpiredUpdateInternal(partialId, revisionHint, searchSpaceWindow);

            if (updateData != null)
            {
                return InMemoryUpdateFactory.FromServerSyncData(updateData, null);
            }

            return null;
        }

        private async Task<ServerSyncConfigData> QueryConfigData()
        {
            var configDataRequest = new GetConfigDataRequest
            {
                GetConfigData = new GetConfigDataRequestBody()
                {
                    configAnchor = null,
                    cookie = AccessToken.AccessCookie
                }
            };

            var configDataReply = await ServerSyncClient.GetConfigDataAsync(configDataRequest);
            if (configDataReply == null || configDataReply.GetConfigDataResponse1 == null || configDataReply.GetConfigDataResponse1.GetConfigDataResult == null)
            {
                throw new Exception("Failed to get config data.");
            }

            return configDataReply.GetConfigDataResponse1.GetConfigDataResult;
        }

        internal IEnumerable<MicrosoftUpdatePackageIdentity> GetCategoryIds(out string newAnchor, string oldAnchor = null)
        {
            if (AccessToken == null || AccessToken.ExpiresIn(TimeSpan.FromMinutes(2)))
            {
                RefreshAccessToken(null, null).GetAwaiter().GetResult();
            }

            // If no configuration is known, query it now
            if (ConfigData == null)
            {
                RefreshServerConfigData().GetAwaiter().GetResult();
            }

            // Create a request for categories
            var revisionIdRequest = new GetRevisionIdListRequest
            {
                GetRevisionIdList = new GetRevisionIdListRequestBody()
                {
                    cookie = AccessToken.AccessCookie,
                    filter = new ServerSyncFilter()
                }
            };

            if (!string.IsNullOrEmpty(oldAnchor))
            {
                revisionIdRequest.GetRevisionIdList.filter.Anchor = oldAnchor;
            }

            // GetConfig must be true to request just categories
            revisionIdRequest.GetRevisionIdList.filter.GetConfig = true;

            var revisionsIdReply = ServerSyncClient.GetRevisionIdListAsync(revisionIdRequest).GetAwaiter().GetResult();
            if (revisionsIdReply == null || 
                revisionsIdReply.GetRevisionIdListResponse1 == null || 
                revisionsIdReply.GetRevisionIdListResponse1.GetRevisionIdListResult == null)
            {
                throw new Exception("Failed to get revision ID list");
            }

            newAnchor = revisionsIdReply.GetRevisionIdListResponse1.GetRevisionIdListResult.Anchor;

            // Return IDs and the anchor for this query. The anchor can be used to get a delta list in the future.
            return revisionsIdReply
                .GetRevisionIdListResponse1
                .GetRevisionIdListResult
                .NewRevisions
                .Select(
                    rawId => new MicrosoftUpdatePackageIdentity(rawId.UpdateID, rawId.RevisionNumber));
        }

        internal IEnumerable<MicrosoftUpdatePackageIdentity> GetUpdateIds(UpstreamSourceFilter updatesFilter, out string newAnchor)
        {
            if (AccessToken == null || AccessToken.ExpiresIn(TimeSpan.FromMinutes(2)))
            {
                RefreshAccessToken(null, null).GetAwaiter().GetResult();
            }

            // If no configuration is known, query it now
            if (ConfigData == null)
            {
                RefreshServerConfigData().GetAwaiter().GetResult();
            }

            // Create a request for categories
            var revisionIdRequest = new GetRevisionIdListRequest
            {
                GetRevisionIdList = new GetRevisionIdListRequestBody()
                {
                    cookie = AccessToken.AccessCookie,
                    filter = updatesFilter.ToServerSyncFilter()
                }
            };

            // GetConfig must be false to request updates
            revisionIdRequest.GetRevisionIdList.filter.GetConfig = false;

            var revisionsIdReply = ServerSyncClient.GetRevisionIdListAsync(revisionIdRequest).GetAwaiter().GetResult();
            if (revisionsIdReply == null || revisionsIdReply.GetRevisionIdListResponse1 == null || revisionsIdReply.GetRevisionIdListResponse1.GetRevisionIdListResult == null)
            {
                throw new Exception("Failed to get revision ID list");
            }

            newAnchor = null;

            // Return IDs and the anchor for this query. The anchor can be used to get a delta list in the future.
            return revisionsIdReply.GetRevisionIdListResponse1.GetRevisionIdListResult.NewRevisions.Select(
                rawId => new MicrosoftUpdatePackageIdentity(rawId.UpdateID, rawId.RevisionNumber));
        }

        /// <summary>
        /// Retrieves update data for the list of update ids
        /// </summary>
        /// <param name="updateIds">The ids to retrieve data for</param>
        internal List<MicrosoftUpdatePackage> GetUpdateDataForIds(List<MicrosoftUpdatePackageIdentity> updateIds)
        {
            if (AccessToken == null || AccessToken.ExpiresIn(TimeSpan.FromMinutes(2)))
            {
                RefreshAccessToken(null, null).GetAwaiter().GetResult();
            }

            // If no configuration is known, query it now
            if (ConfigData == null)
            {
                RefreshServerConfigData().GetAwaiter().GetResult();
            }

            var rawUpdateIds = updateIds
                .Select(id => new UpdateIdentity() { UpdateID = id.ID, RevisionNumber = id.Revision })
                .ToList();

            // Data retrieval is done is done in batches of upto MaxNumberOfUpdatesPerRequest
            var retrieveBatches = CreateBatchedListFromFlatList(rawUpdateIds, ConfigData.MaxNumberOfUpdatesPerRequest);

            var packages = new ConcurrentBag<MicrosoftUpdatePackage>();

            // Run batches in parallel
            retrieveBatches.AsParallel().ForAll(batch =>
            {
                var updateDataRequest = new GetUpdateDataRequest
                {
                    GetUpdateData = new GetUpdateDataRequestBody()
                    {
                        cookie = AccessToken.AccessCookie,
                        updateIds = batch
                    }
                };

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
                    catch(Exception)
                    {
                        System.Threading.Thread.Sleep(5000);
                        updateDataReply = null;
                    }
                    retryCount++;
                } while (updateDataReply == null && retryCount < 10);

                if (updateDataReply == null || updateDataReply.GetUpdateDataResponse1 == null || updateDataReply.GetUpdateDataResponse1.GetUpdateDataResult == null)
                {
                    throw new Exception("Failed to get update metadata");
                }

                // Parse the list of raw files into a more usable format
                var filesList = updateDataReply.GetUpdateDataResponse1.GetUpdateDataResult.fileUrls
                .Select(rawFile => InMemoryUpdateFactory.FromServerSyncData(rawFile))
                .ToDictionary(file => file.DigestBase64);

                foreach (var rawUpdate in updateDataReply.GetUpdateDataResponse1.GetUpdateDataResult.updates)
                {
                    packages.Add(InMemoryUpdateFactory.FromServerSyncData(rawUpdate, filesList));
                }
            });

            return packages.ToList();
        }

        private (ServerSyncUpdateData updateData, List<UpdateFileUrl> files) TryGetExpiredUpdateInternal(Guid partialUpdateId, int revisionHint, int searchSpaceWindow)
        {
            var effectiveSearchSpaceWindow = Math.Max(searchSpaceWindow, revisionHint % 100);
            var currentRevision = revisionHint;
            do
            {
                if ((currentRevision % 100) > effectiveSearchSpaceWindow)
                {
                    currentRevision -= currentRevision % 100;
                    currentRevision += effectiveSearchSpaceWindow;
                }

                var updateDataRequest = new GetUpdateDataRequest
                {
                    GetUpdateData = new GetUpdateDataRequestBody()
                    {
                        cookie = AccessToken.AccessCookie,
                        updateIds = new UpdateIdentity[]
                        {
                            new UpdateIdentity() 
                            { 
                                UpdateID = partialUpdateId, 
                                RevisionNumber = currentRevision 
                            }
                        }
                    }
                };

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
                    catch(System.ServiceModel.FaultException)
                    {
                        updateDataReply = null;
                        break;
                    }
                    retryCount++;
                } while (updateDataReply == null && retryCount < 10);

                if (updateDataReply == null || updateDataReply.GetUpdateDataResponse1 == null || updateDataReply.GetUpdateDataResponse1.GetUpdateDataResult == null)
                {
                    currentRevision--;
                }
                else
                {
                    // Parse the list of raw files into a more usable format
                    var filesList = new List<UpdateFileUrl>(
                        updateDataReply.GetUpdateDataResponse1.GetUpdateDataResult.fileUrls.Select(
                            rawFile => new UpdateFileUrl(
                                Convert.ToBase64String(rawFile.FileDigest),
                                rawFile.MUUrl,
                                rawFile.UssUrl
                                )));
                    var update = updateDataReply.GetUpdateDataResponse1.GetUpdateDataResult.updates.First();

                    return (update, filesList);
                }
            } while (currentRevision > 0);

            return (null, null);
        }


        /// <summary>
        /// Breaks down a flat list of objects in a list of batches, each batch having a maximum allowed size
        /// </summary>
        /// <typeparam name="T">The type of objects to batch</typeparam>
        /// <param name="flatList">The flat list of objects to break down</param>
        /// <param name="maxBatchSize">The maximum size of a batch</param>
        /// <returns>The batched list</returns>
        private static List<T[]> CreateBatchedListFromFlatList<T>(List<T> flatList, int maxBatchSize)
        {
            // Figure out how many batches we have
            var batchCount = flatList.Count / maxBatchSize;
            // One more batch for the remaininig objects, if any
            batchCount += flatList.Count % maxBatchSize == 0 ? 0 : 1;

            List<T[]> batches = new(batchCount);
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
    }
}
