// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.UpdateServices.WebServices.ClientSync;
using System.IO;
using System.Threading;
using System.ServiceModel;
using Microsoft.PackageGraph.Storage;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Drivers;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Prerequisites;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Content;
using System.Text;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ClientSync
{
    /// <summary>
    /// Update server implementation. Provides updates to Windows Update clients.
    /// <para>The communication protocol with clients is SOAP.</para>
    /// </summary>
    public partial class ClientSyncWebService : IClientSyncWebService
    {
        /// <summary>
        /// The local repository from where updates are served.
        /// </summary>
        public IMetadataStore MetadataSource { get; private set; }

        readonly ReaderWriterLockSlim MetadataSourceLock = new();

        /// <summary>
        /// Mapping of update index to its identity
        /// Update indexes are used when communicating with clients, as they are smaller that full Identities
        /// </summary>
        Dictionary<int, MicrosoftUpdatePackageIdentity> MetadataSourceIndex;

        Config ServiceConfiguration;

        private IEnumerable<Guid> RootUpdates;

        private IEnumerable<Guid> NonLeafUpdates;

        private IEnumerable<Guid> LeafUpdatesGuids;

        private List<Guid> SoftwareLeafUpdateGuids;

        private Dictionary<Guid, int> IdToRevisionMap;
        private Dictionary<Guid, MicrosoftUpdatePackageIdentity> IdToFullIdentityMap;

        private const int MaxUpdatesInResponse = 50;

        private string ContentRoot;

        DriverUpdateMatching DriverMatcher;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ClientSyncWebService()
        {
            ApprovedSoftwareUpdates = new HashSet<MicrosoftUpdatePackageIdentity>();
            ApprovedDriverUpdates = new HashSet<MicrosoftUpdatePackageIdentity>();
        }

        /// <summary>
        /// Sets the host name for the server that serves the update content
        /// </summary>
        /// <param name="hostName"></param>
        public void SetContentURLBase(string hostName)
        {
            ContentRoot = hostName;
        }

        /// <summary>
        /// Sets the service configuratoin
        /// </summary>
        /// <param name="serviceConfiguration">Service configuration</param>
        public void SetServiceConfiguration(Config serviceConfiguration)
        {
            ServiceConfiguration = serviceConfiguration;
        }

        /// <summary>
        /// Sets the source of update metadata
        /// </summary>
        /// <param name="metadataSource">The source for updates metadata</param>
        public void SetPackageStore(IMetadataStore metadataSource)
        {
            MetadataSourceLock.EnterWriteLock();

            MetadataSource = metadataSource;

            if (MetadataSource != null)
            {
                PrerequisitesGraph prereqGraph = PrerequisitesGraph.FromIndexedPackageSource(MetadataSource);

                // Get leaf updates - updates that have prerequisites and no dependents
                LeafUpdatesGuids = prereqGraph.GetLeafUpdates();

                // Get non leaft updates: updates that have prerequisites and dependents
                NonLeafUpdates = prereqGraph.GetNonLeafUpdates();

                // Get root updates: updates that have no prerequisites
                RootUpdates = prereqGraph.GetRootUpdates();

                // Filter out leaf updates and only retain software ones that are not superseded
                var leafSoftwareUpdates = MetadataSource.
                    OfType<SoftwareUpdate>()
                    .Where(u => u.IsSupersededBy == null || u.IsSupersededBy.Count == 0)
                    .GroupBy(u => u.Id.ID)
                    .Select(k => k.Key)
                    .ToHashSet();
                SoftwareLeafUpdateGuids = LeafUpdatesGuids.Where(g => leafSoftwareUpdates.Contains(g)).ToList();

                // Get the mapping of update index to identity that is used in the metadata source.
                MetadataSourceIndex = new Dictionary<int, MicrosoftUpdatePackageIdentity>();
                foreach(var package in MetadataSource.OfType<MicrosoftUpdatePackage>())
                {
                    MetadataSourceIndex.Add(MetadataSource.GetPackageIndex(package.Id), package.Id);
                }

                var latestRevisionSelector = MetadataSourceIndex
                    .ToDictionary(k => k.Value, v => v.Key)
                    .GroupBy(p => p.Key.ID)
                    .Select(group => group.OrderBy(g => g.Key.Revision).Last());

                // Create a mapping for index to update GUID
                IdToRevisionMap = latestRevisionSelector.ToDictionary(k => k.Key.ID, v => v.Value);

                // Create a mapping from GUID to full identity
                IdToFullIdentityMap = latestRevisionSelector.ToDictionary(k => k.Key.ID, v => v.Key);

                DriverMatcher = DriverUpdateMatching.FromPackageSource(MetadataSource);
            }
            else
            {
                LeafUpdatesGuids = null;
                NonLeafUpdates = null;
                RootUpdates = null;
                SoftwareLeafUpdateGuids = null;
                MetadataSourceIndex = null;
                IdToRevisionMap = null;
                IdToFullIdentityMap = null;
                DriverMatcher = null;
            }

            MetadataSourceLock.ExitWriteLock();
        }

        /// <summary>
        /// Handle get configuration requests from clients
        /// </summary>
        /// <param name="clientConfiguration">The client configuration as received from a Windows client</param>
        /// <returns>The server configuration to be sent to a Windows client</returns>
        public Task<Config> GetConfig2Async(ClientConfiguration clientConfiguration)
        {
            return Task.FromResult(ServiceConfiguration);
        }

        /// <summary>
        /// Handle get configuration requests from clients
        /// </summary>
        /// <param name="protocolVersion">The version of the Windows client connecting to this server</param>
        /// <returns>The server configuration to be sent to a Windows client</returns>
        public Task<Config> GetConfigAsync(string protocolVersion)
        {
            return Task.FromResult(ServiceConfiguration);
        }

        /// <summary>
        /// Handle get cookie requests. All requests are all granted access and a cookie is issued.
        /// </summary>
        /// <param name="authCookies">Authorization cookies received from the client</param>
        /// <param name="oldCookie">Old cookie from client</param>
        /// <param name="lastChange"></param>
        /// <param name="currentTime"></param>
        /// <param name="protocolVersion">Client supported protocol version</param>
        /// <returns>A new cookie</returns>
        public Task<Cookie> GetCookieAsync(AuthorizationCookie[] authCookies, Cookie oldCookie, DateTime lastChange, DateTime currentTime, string protocolVersion)
        {
            return Task.FromResult(new Cookie() { Expiration = DateTime.Now.AddDays(5), EncryptedData = new byte[12] });
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="cookie">Not implemented</param>
        /// <param name="updateIDs">Not implemented</param>
        /// <param name="infoTypes">Not implemented</param>
        /// <param name="locales">Not implemented</param>
        /// <param name="deviceAttributes">Not implemented</param>
        /// <returns>Not implemented</returns>
        public Task<ExtendedUpdateInfo2> GetExtendedUpdateInfo2Async(Cookie cookie, UpdateIdentity[] updateIDs, XmlUpdateFragmentType[] infoTypes, string[] locales, string deviceAttributes)
        {
            throw new NotImplementedException();
        }

        string GetCoreFragment(MicrosoftUpdatePackageIdentity updateIdentity)
        {
            using var xmlStream = MetadataSource.GetMetadata(updateIdentity);
            using var xmlReader = new StreamReader(xmlStream, Encoding.Unicode);
            return UpdateXmlTransformer.GetCoreFragmentFromMetadataXml(xmlReader.ReadToEnd());
        }

        string GetExtendedFragment(MicrosoftUpdatePackageIdentity updateIdentity)
        {
            using var xmlStream = MetadataSource.GetMetadata(updateIdentity);
            using var xmlReader = new StreamReader(xmlStream, Encoding.Unicode);
            return UpdateXmlTransformer.GetExtendedFragmentFromMetadataXml(xmlReader.ReadToEnd());
        }

        string GetLocalizedProperties(MicrosoftUpdatePackageIdentity updateIdentity, string[] languages)
        {
            using var xmlStream = MetadataSource.GetMetadata(updateIdentity);
            using var xmlReader = new StreamReader(xmlStream, Encoding.Unicode);
            return UpdateXmlTransformer.GetLocalizedPropertiesFromMetadataXml(xmlReader.ReadToEnd(), languages);
        }

        /// <summary>
        /// Handle requests for extended update information. The extended information is extracted from update metadata.
        /// Extended information also includes file URLs
        /// </summary>
        /// <param name="cookie">Access cookie</param>
        /// <param name="revisionIDs">Revision Ids for which to get extended information</param>
        /// <param name="infoTypes">The type of extended information requested</param>
        /// <param name="locales">The language to use when getting language dependent extended information</param>
        /// <param name="deviceAttributes">Device attributes; unused</param>
        /// <returns>Extended update information response.</returns>
        public Task<ExtendedUpdateInfo> GetExtendedUpdateInfoAsync(Cookie cookie, int[] revisionIDs, XmlUpdateFragmentType[] infoTypes, string[] locales, string deviceAttributes)
        {
            MetadataSourceLock.EnterReadLock();

            if (MetadataSource == null)
            {
                throw new FaultException();
            }

            List<MicrosoftUpdatePackage> requestedUpdates = new();
            foreach (var requestedRevision in revisionIDs)
            {
                if (!MetadataSourceIndex.TryGetValue(requestedRevision, out MicrosoftUpdatePackageIdentity id))
                {
                    throw new Exception("RevisionID not found");
                }

                requestedUpdates.Add(MetadataSource.GetPackage(id) as MicrosoftUpdatePackage);
            }

            var updateDataList = new List<UpdateData>();

            if (infoTypes.Contains(XmlUpdateFragmentType.Extended))
            {
                for (int i = 0; i < requestedUpdates.Count; i++)
                {
                    updateDataList.Add(new UpdateData()
                    {
                        ID = revisionIDs[i],
                        Xml = GetExtendedFragment(requestedUpdates[i].Id)
                    });
                }
            }
            

            if (infoTypes.Contains(XmlUpdateFragmentType.LocalizedProperties))
            {
                for (int i = 0; i < requestedUpdates.Count; i++)
                {
                    var localizedXml = GetLocalizedProperties(requestedUpdates[i].Id, locales);

                    if (!string.IsNullOrEmpty(localizedXml))
                    {
                        updateDataList.Add(new UpdateData()
                        {
                            ID = revisionIDs[i],
                            Xml = GetLocalizedProperties(requestedUpdates[i].Id, locales)
                        });
                    }
                }
            }

            var files = requestedUpdates.Where(u => u.Files != null && u.Files.Any()).SelectMany(u => u.Files.OfType<UpdateFile>()).Distinct().ToList();
            var fileList = new List<FileLocation>();
            for (int i = 0; i < files.Count; i++)
            {
                fileList.Add(new FileLocation()
                {
                    FileDigest = Convert.FromBase64String(files[i].Digest.DigestBase64),
                    Url = string.IsNullOrEmpty(ContentRoot) ? files[i].Urls[0].MuUrl : $"{ContentRoot}/{files[i].Digest.HexString.ToLower()}"
                });
            }

            var response = new ExtendedUpdateInfo();

            if (updateDataList.Count > 0)
            {
                response.Updates = updateDataList.ToArray();
            }
            
            if (fileList.Count > 0)
            {
                response.FileLocations = fileList.ToArray();
            }

            MetadataSourceLock.ExitReadLock();

            return Task.FromResult(response);
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="fileDigests"></param>
        /// <returns>Not implemented</returns>
        public Task<GetFileLocationsResults> GetFileLocationsAsync(Cookie cookie, byte[][] fileDigests)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Not implemented</returns>
        public Task<GetTimestampsResponse> GetTimestampsAsync(GetTimestampsRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="globalIDs"></param>
        /// <param name="deviceAttributes"></param>
        /// <returns>Not implemented</returns>
        public Task<RefreshCacheResult[]> RefreshCacheAsync(Cookie cookie, UpdateIdentity[] globalIDs, string deviceAttributes)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="computerInfo"></param>
        /// <returns>Not implemented</returns>
        public Task RegisterComputerAsync(Cookie cookie, ComputerInfo computerInfo)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Not implemented</returns>
        public Task<StartCategoryScanResponse> StartCategoryScanAsync(StartCategoryScanRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="cookie"></param>
        /// <param name="installedNonLeafUpdateIDs"></param>
        /// <param name="printerUpdateIDs"></param>
        /// <param name="deviceAttributes"></param>
        /// <returns>Not implemented</returns>
        public Task<SyncInfo> SyncPrinterCatalogAsync(Cookie cookie, int[] installedNonLeafUpdateIDs, int[] printerUpdateIDs, string deviceAttributes)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Handle requests to sync updates. A client presents the list of installed updates and detectoids and the server
        /// replies with a list of more applicable updates, if any.
        /// </summary>
        /// <param name="cookie">Access cookie</param>
        /// <param name="parameters">Request parameters: list of installed updates, list of known updates, etc.</param>
        /// <returns>SyncInfo containing updates applicable to the caller.</returns>
        public Task<SyncInfo> SyncUpdatesAsync(Cookie cookie, SyncUpdateParameters parameters)
        {
            if (parameters.SkipSoftwareSync)
            {
                return DoDriversSync(parameters);   
            }
            else
            {
                return DoSoftwareUpdateSync(parameters);
            }
        }

        

        /// <summary>
        /// Converts the a list of client supplied update indexes into a list of update identities
        /// </summary>
        /// <param name="clientIndexes">Client update indexes (ints)</param>
        /// <returns>List of update identities that correspond to the client's indexes</returns>
        private List<MicrosoftUpdatePackageIdentity> GetUpdateIdentitiesFromClientIndexes(int[] clientIndexes)
        {
            var updateIdentities = new List<MicrosoftUpdatePackageIdentity>();
            if (clientIndexes != null)
            {
                foreach (var nonLeafRevision in clientIndexes)
                {
                    if (!MetadataSourceIndex.TryGetValue(nonLeafRevision, out MicrosoftUpdatePackageIdentity nonLeafId))
                    {
                        throw new Exception("RevisionID not found");
                    }

                    updateIdentities.Add(nonLeafId);
                }
            }
            return updateIdentities;
        }

        /// <summary>
        /// Extract installed non-leaf updates from the response and maps them to a GUID
        /// </summary>
        /// <param name="parameters">Sync parameters</param>
        /// <returns>List of update GUIDs</returns>
        private List<Guid> GetInstalledNotLeafGuidsFromSyncParameters(SyncUpdateParameters parameters)
        {
            return GetUpdateIdentitiesFromClientIndexes(parameters.InstalledNonLeafUpdateIDs)
                .Select(u => u.ID)
                .ToList();
        }

        /// <summary>
        /// Extract list of other known updates from the client and maps them to a  GUID
        /// </summary>
        /// <param name="parameters">Sync parameters</param>
        /// <returns>List of update GUIDs</returns>
        private List<Guid> GetOtherCachedUpdateGuidsFromSyncParameters(SyncUpdateParameters parameters)
        {
            return GetUpdateIdentitiesFromClientIndexes(parameters.OtherCachedUpdateIDs)
                .Select(u => u.ID)
                .ToList();
        }
    }
}
