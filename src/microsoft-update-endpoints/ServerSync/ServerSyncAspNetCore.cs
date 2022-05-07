// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UpdateServices.WebServices.ServerSync;
using System.Linq;
using System.IO;
using System.Text;
using Microsoft.PackageGraph.Storage;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Prerequisites;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Content;
using System.Threading;
using Microsoft.PackageGraph.Storage.Local;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ServerSync
{
    /// <summary>
    /// Upstream update server implementation. Provides updates over the ServerSync protocol to downstream servers.
    /// <para>The communication protocol with clients is SOAP</para>
    /// </summary>
    public class ServerSyncWebService : IServerSyncAspNetCore
    {
        /// <summary>
        /// The source of upate metadata that this server serves.
        /// </summary>
        private IMetadataStore PackageStore;
        private readonly ReaderWriterLock PackageStoreLock = new();

        /// <summary>
        /// Cached service configuration
        /// </summary>
        private ServerSyncConfigData ServiceConfiguration;
        private readonly ReaderWriterLock ServiceConfigurationLock = new();

        private Dictionary<Guid, List<MicrosoftUpdatePackage>> ProductsIndex;
        private Dictionary<Guid, List<MicrosoftUpdatePackage>> ClassificationsIndex;

        private List<MicrosoftUpdatePackage> Categories;
        private Dictionary<MicrosoftUpdatePackageIdentity, MicrosoftUpdatePackage> Updates;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ServerSyncWebService()
        {
        }

        /// <summary>
        /// ASP.NET extension method for setting service configuration
        /// </summary>
        /// <param name="serviceConfig"></param>
        public void SetServerConfiguration(ServerSyncConfigData serviceConfig)
        {
            ServiceConfigurationLock.AcquireWriterLock(-1);
            ServiceConfiguration = serviceConfig;
            ServiceConfigurationLock.ReleaseWriterLock();
        }

        /// <summary>
        /// Sets the package store for this instance of the server
        /// </summary>
        /// <param name="packageSource">The package store to server updates from</param>
        public void SetPackageStore(IMetadataStore packageSource)
        {
            PackageStoreLock.AcquireWriterLock(-1);

            PackageStore = packageSource;

            Categories = new List<MicrosoftUpdatePackage>();
            Updates = new Dictionary<MicrosoftUpdatePackageIdentity, MicrosoftUpdatePackage>();
            ProductsIndex = new Dictionary<Guid, List<MicrosoftUpdatePackage>>();
            ClassificationsIndex = new Dictionary<Guid, List<MicrosoftUpdatePackage>>();

            if (PackageStore != null)
            {
                Categories.AddRange(packageSource.OfType<ProductCategory>());
                Categories.AddRange(packageSource.OfType<ClassificationCategory>());
                Categories.AddRange(packageSource.OfType<DetectoidCategory>());
                foreach(var softwarePackage in packageSource.OfType<SoftwareUpdate>())
                {
                    Updates.Add(softwarePackage.Id as MicrosoftUpdatePackageIdentity, softwarePackage);
                }

                foreach (var driverUpdate in packageSource.OfType<DriverUpdate>())
                {
                    Updates.Add(driverUpdate.Id as MicrosoftUpdatePackageIdentity, driverUpdate);
                }

                foreach (var classification in Categories.OfType<ClassificationCategory>())
                {
                    ClassificationsIndex.TryAdd(classification.Id.ID, new List<MicrosoftUpdatePackage>());
                }

                foreach (var product in Categories.OfType<ProductCategory>())
                {
                    ProductsIndex.TryAdd(product.Id.ID, new List<MicrosoftUpdatePackage>());
                }

                foreach (var update in Updates.Values)
                {
                    if (update.Prerequisites != null)
                    {
                        foreach (var prerequisite in update.Prerequisites.OfType<AtLeastOne>().SelectMany(p => p.Simple))
                        {
                            if (ClassificationsIndex.ContainsKey(prerequisite.UpdateId))
                            {
                                ClassificationsIndex[prerequisite.UpdateId].Add(update);
                            }

                            if (ProductsIndex.ContainsKey(prerequisite.UpdateId))
                            {
                                ProductsIndex[prerequisite.UpdateId].Add(update);
                            }
                        }

                        foreach (var prerequisite in update.Prerequisites.OfType<Simple>())
                        {
                            if (ClassificationsIndex.ContainsKey(prerequisite.UpdateId))
                            {
                                ClassificationsIndex[prerequisite.UpdateId].Add(update);
                            }

                            if (ProductsIndex.ContainsKey(prerequisite.UpdateId))
                            {
                                ProductsIndex[prerequisite.UpdateId].Add(update);
                            }
                        }
                    }
                }
            }

            PackageStoreLock.ReleaseWriterLock();
        }

        /// <summary>
        /// Handle authentication data requests
        /// </summary>
        /// <param name="request">The request data. Not used</param>
        /// <returns>Exactly one canned authentication method</returns>
        public Task<ServerAuthConfig> GetAuthConfigAsync(GetAuthConfigRequest request)
        {
            // Build the standard response
            var result = new ServerAuthConfig()
            {
                LastChange = DateTime.Now,
                AuthInfo = new AuthPlugInInfo[]
                {
                    new AuthPlugInInfo() { PlugInID = "DssTargeting", ServiceUrl = "DssAuthWebService/DssAuthWebService.asmx" } 
                }
            };

            GetAuthConfigResponse response = new(
                new GetAuthConfigResponseBody() 
                { 
                    GetAuthConfigResult = result 
                });

            return Task.FromResult(response.GetAuthConfigResponse1.GetAuthConfigResult);
        }

        /// <summary>
        /// Handle service configuration requests
        /// </summary>
        /// <param name="request">Service configuration request</param>
        /// <returns>Returns the cached service configuration of the upstream server the local repo is tracking</returns>
        public Task<ServerSyncConfigData> GetConfigDataAsync(GetConfigDataRequest request)
        {
            ServerSyncConfigData capturedConfigData;
            ServiceConfigurationLock.AcquireReaderLock(-1);
            capturedConfigData = ServiceConfiguration;
            ServiceConfigurationLock.ReleaseReaderLock();
            return Task.FromResult(capturedConfigData);
        }

        /// <summary>
        /// Handle request for a cookie
        /// </summary>
        /// <param name="request">Cookie request. Not used; all requests are granted</param>
        /// <returns>A cookie that expires in 5 days.</returns>
        public Task<Cookie> GetCookieAsync(GetCookieRequest request)
        {
            return Task.FromResult(new Cookie() { Expiration = DateTime.Now.AddDays(5), EncryptedData = new byte[12] });
        }

        /// <summary>
        /// Return a list of update ids
        /// </summary>
        /// <param name="request">Request data. Can specify categories or updates, filters, etc.</param>
        /// <returns></returns>
        public Task<RevisionIdList> GetRevisionIdListAsync(GetRevisionIdListRequest request)
        {
            var response = new RevisionIdList
            {
                Anchor = DateTime.Now.ToString()
            };

            PackageStoreLock.AcquireReaderLock(-1);

            try
            {
                if (request.GetRevisionIdList.filter.GetConfig == true)
                {
                    response.NewRevisions = Categories.Select(u => new UpdateIdentity() { UpdateID = u.Id.ID, RevisionNumber = u.Id.Revision }).ToArray();
                }
                else
                {
                    var productsFilter = request.GetRevisionIdList.filter.Categories;
                    var classificationsFilter = request.GetRevisionIdList.filter.Classifications;

                    var effectiveProductsFilter = productsFilter == null ? ProductsIndex.Keys : productsFilter.Select(p => p.Id);
                    var effectiveClassificationsFilter = classificationsFilter == null ? ClassificationsIndex.Keys : classificationsFilter.Select(c => c.Id);

                    var filteredByProduct = new List<MicrosoftUpdatePackageIdentity>();
                    foreach (var product in effectiveProductsFilter)
                    {
                        if (ProductsIndex.ContainsKey(product))
                        {
                            filteredByProduct.AddRange(ProductsIndex[product].Select(u => u.Id));
                        }
                    }

                    var filteredByClassification = new List<MicrosoftUpdatePackageIdentity>();
                    foreach (var classification in effectiveClassificationsFilter)
                    {
                        if (ClassificationsIndex.ContainsKey(classification))
                        {
                            filteredByClassification.AddRange(ClassificationsIndex[classification].Select(u => u.Id));
                        }
                    }

                    // Also select all updates that are bundled with updates matching the filter
                    var filteredResult = filteredByProduct.Intersect(filteredByClassification);
                    List<MicrosoftUpdatePackageIdentity> bundledUpdates = new();
                    foreach(var result in filteredResult)
                    {
                        if (Updates[result] is SoftwareUpdate softwareUpdate)
                        {
                            if (softwareUpdate.BundledUpdates != null)
                            {
                                bundledUpdates.AddRange(softwareUpdate.BundledUpdates);
                            }
                        }
                    }
                    
                    // Deduplicate result and convert to raw identity format
                    response.NewRevisions = filteredResult
                        .Union(bundledUpdates)
                        .Distinct()
                        .Select(u => new UpdateIdentity() { UpdateID = u.ID, RevisionNumber = u.Revision })
                        .ToArray();
                }
            }
            catch(Exception) { }

            PackageStoreLock.ReleaseReaderLock();

            return Task.FromResult(response);
        }

        /// <summary>
        /// Return metadata for updates
        /// </summary>
        /// <param name="request">The request; contains IDs for updates to retrieve metadata for</param>
        /// <returns>Update metadata for requested updates</returns>
        public Task<ServerUpdateData> GetUpdateDataAsync(GetUpdateDataRequest request)
        {
            var response = new ServerUpdateData();

            ServerSyncConfigData serviceConfiguration;
            ServiceConfigurationLock.AcquireReaderLock(-1);
            serviceConfiguration = ServiceConfiguration;
            ServiceConfigurationLock.ReleaseReaderLock();

            if (serviceConfiguration == null)
            {
                return Task.FromResult(response);
            }

            if (PackageStore == null)
            {
                return Task.FromResult(response);
            }

            // Make sure the request is not larger than the config says
            var updateRequestCount = request.GetUpdateData.updateIds.Length;
            if (updateRequestCount > serviceConfiguration.MaxNumberOfUpdatesPerRequest)
            {
                return null;
            }

            PackageStoreLock.AcquireReaderLock(-1);

            var returnUpdatesList = new List<ServerSyncUpdateData>();
            var returnFilesList = new List<ServerSyncUrlData>();

            try
            {
                foreach (var rawIdentity in request.GetUpdateData.updateIds)
                {
                    var updateIdentity = new MicrosoftUpdatePackageIdentity(rawIdentity.UpdateID, rawIdentity.RevisionNumber);

                    if (!PackageStore.ContainsPackage(updateIdentity))
                    {
                        throw new Exception("Update not found");
                    }

                    var update = PackageStore.GetPackage(updateIdentity) as MicrosoftUpdatePackage;
                    if (update.Files != null)
                    {
                        // if update contains files, we must also gather file information
                        foreach (var updateFile in update.Files)
                        {
                            var microsoftUpdateFile = updateFile as UpdateFile;
                            returnFilesList.Add(
                                new ServerSyncUrlData()
                                {
                                    FileDigest = Convert.FromBase64String(updateFile.Digest.DigestBase64),
                                    MUUrl = microsoftUpdateFile.Urls[0].MuUrl,
                                    UssUrl = $"microsoftupdate/content/{FileSystemContentStore.GetContentDirectoryName(microsoftUpdateFile.Digest)}/{updateFile.FileName}"
                                });
                        }
                    }

                    var rawUpdateData = new ServerSyncUpdateData
                    {
                        Id = rawIdentity
                    };

                    using (var metadataReader = new StreamReader(PackageStore.GetMetadata(update.Id), Encoding.Unicode))
                    {
                        rawUpdateData.XmlUpdateBlob = metadataReader.ReadToEnd();
                    }

                    returnUpdatesList.Add(rawUpdateData);
                }
            }
            catch(Exception) { }

            response.updates = returnUpdatesList.ToArray();
            // Deduplicate list of files
            response.fileUrls = returnFilesList.GroupBy(f => f.MUUrl).Select(k => k.First()).ToArray();

            PackageStoreLock.ReleaseReaderLock();

            return Task.FromResult(response);
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<GetUpdateDecryptionDataResponse> GetUpdateDecryptionDataAsync(GetUpdateDecryptionDataRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<PingResponse> PingAsync(PingRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<GetDeploymentsResponse> GetDeploymentsAsync(GetDeploymentsRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<GetDriverIdListResponse> GetDriverIdListAsync(GetDriverIdListRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<GetDriverSetDataResponse> GetDriverSetDataAsync(GetDriverSetDataRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<GetRelatedRevisionsForUpdatesResponse> GetRelatedRevisionsForUpdatesAsync(GetRelatedRevisionsForUpdatesRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<DownloadFilesResponse> DownloadFilesAsync(DownloadFilesRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
