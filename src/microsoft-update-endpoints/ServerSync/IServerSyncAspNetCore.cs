// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.UpdateServices.WebServices.ServerSync;
using System.ServiceModel;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ServerSync
{
    /// <summary>
    /// Declare an interface for the ServerSyncWebService (WCF) that can be used in AspNetCore with slight modifications.
    /// SOAP operations that return *Response\*ResponseBody are modified to return the actual data in the *ResponseBody data contract. This
    /// ensures proper serialization with SoapCore
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("dotnet-svcutil", "1.0.0.1")]
    [ServiceContractAttribute(Namespace = "http://www.microsoft.com/SoftwareDistribution", ConfigurationName = "IServerSyncWebService")]
    interface IServerSyncAspNetCore
    {
        [OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/GetAuthConfig", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/IServerSyncWebService/GetAuthConfig" +
            "Response")]
        Task<ServerAuthConfig> GetAuthConfigAsync(GetAuthConfigRequest request);

        [OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/GetCookie", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/IServerSyncWebService/GetCookieResp" +
            "onse")]
        Task<Cookie> GetCookieAsync(GetCookieRequest request);

        [OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/GetConfigData", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/IServerSyncWebService/GetConfigData" +
            "Response")]
        Task<ServerSyncConfigData> GetConfigDataAsync(GetConfigDataRequest request);

        [OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/GetRevisionIdList", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/IServerSyncWebService/GetRevisionId" +
            "ListResponse")]
        Task<RevisionIdList> GetRevisionIdListAsync(GetRevisionIdListRequest request);

        [OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/GetUpdateData", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/IServerSyncWebService/GetUpdateData" +
            "Response")]
        Task<ServerUpdateData> GetUpdateDataAsync(GetUpdateDataRequest request);

        [OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/GetUpdateDecryptionData", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/IServerSyncWebService/GetUpdateDecr" +
            "yptionDataResponse")]
        Task<GetUpdateDecryptionDataResponse> GetUpdateDecryptionDataAsync(GetUpdateDecryptionDataRequest request);

        [OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/GetDriverIdList", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/IServerSyncWebService/GetDriverIdLi" +
            "stResponse")]
        Task<GetDriverIdListResponse> GetDriverIdListAsync(GetDriverIdListRequest request);

        [OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/GetDriverSetData", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/IServerSyncWebService/GetDriverSetD" +
            "ataResponse")]
        Task<GetDriverSetDataResponse> GetDriverSetDataAsync(GetDriverSetDataRequest request);

        [OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/DownloadFiles", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/IServerSyncWebService/DownloadFiles" +
            "Response")]
        Task<DownloadFilesResponse> DownloadFilesAsync(DownloadFilesRequest request);

        [OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/GetDeployments", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/IServerSyncWebService/GetDeployment" +
            "sResponse")]
        Task<GetDeploymentsResponse> GetDeploymentsAsync(GetDeploymentsRequest request);

        [OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/GetRelatedRevisionsForUpdates", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/IServerSyncWebService/GetRelatedRev" +
            "isionsForUpdatesResponse")]
        Task<GetRelatedRevisionsForUpdatesResponse> GetRelatedRevisionsForUpdatesAsync(GetRelatedRevisionsForUpdatesRequest request);

        [OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/Server/IMonitorable/Ping", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/IServerSyncWebService/PingResponse")]
        Task<PingResponse> PingAsync(PingRequest request);
    }
}