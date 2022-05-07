// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.WebServices.ClientSync;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ClientSync
{
    /// <summary>
    /// Declare an interface for the ClientSoap (WCF) service that can be used in AspNetCore with slight modifications.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.1-preview-30422-0661")]
    [System.ServiceModel.ServiceContractAttribute(Namespace = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService", ConfigurationName = "Microsoft.UpdateServices.WebServices.ClientSync.ClientSoap")]
    interface IClientSyncWebService
    {

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetConfig", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetConfigRe" +
            "sponse")]
        System.Threading.Tasks.Task<Config> GetConfigAsync(string protocolVersion);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetConfig2", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetConfig2R" +
            "esponse")]
        System.Threading.Tasks.Task<Config> GetConfig2Async(ClientConfiguration clientConfiguration);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetCookie", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetCookieRe" +
            "sponse")]
        System.Threading.Tasks.Task<Cookie> GetCookieAsync(AuthorizationCookie[] authCookies, Cookie oldCookie, System.DateTime lastChange, System.DateTime currentTime, string protocolVersion);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/RegisterCom" +
            "puter", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/RegisterCom" +
            "puterResponse")]
        System.Threading.Tasks.Task RegisterComputerAsync(Cookie cookie, ComputerInfo computerInfo);

        // CODEGEN: Generating message contract since the operation has multiple return values.
        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/StartCatego" +
            "ryScan", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/StartCatego" +
            "ryScanResponse")]
        System.Threading.Tasks.Task<StartCategoryScanResponse> StartCategoryScanAsync(StartCategoryScanRequest request);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/SyncUpdates" +
            "", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/SyncUpdates" +
            "Response")]
        System.Threading.Tasks.Task<SyncInfo> SyncUpdatesAsync(Cookie cookie, SyncUpdateParameters parameters);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/SyncPrinter" +
            "Catalog", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/SyncPrinter" +
            "CatalogResponse")]
        System.Threading.Tasks.Task<SyncInfo> SyncPrinterCatalogAsync(Cookie cookie, int[] installedNonLeafUpdateIDs, int[] printerUpdateIDs, string deviceAttributes);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/RefreshCach" +
            "e", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/RefreshCach" +
            "eResponse")]
        System.Threading.Tasks.Task<RefreshCacheResult[]> RefreshCacheAsync(Cookie cookie, UpdateIdentity[] globalIDs, string deviceAttributes);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetExtended" +
            "UpdateInfo", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetExtended" +
            "UpdateInfoResponse")]
        System.Threading.Tasks.Task<ExtendedUpdateInfo> GetExtendedUpdateInfoAsync(Cookie cookie, int[] revisionIDs, XmlUpdateFragmentType[] infoTypes, string[] locales, string deviceAttributes);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetExtended" +
            "UpdateInfo2", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetExtended" +
            "UpdateInfo2Response")]
        System.Threading.Tasks.Task<ExtendedUpdateInfo2> GetExtendedUpdateInfo2Async(Cookie cookie, UpdateIdentity[] updateIDs, XmlUpdateFragmentType[] infoTypes, string[] locales, string deviceAttributes);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetFileLoca" +
            "tions", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetFileLoca" +
            "tionsResponse")]
        System.Threading.Tasks.Task<GetFileLocationsResults> GetFileLocationsAsync(Cookie cookie, byte[][] fileDigests);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetTimestam" +
            "ps", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/Server/ClientWebService/GetTimestam" +
            "psResponse")]
        System.Threading.Tasks.Task<GetTimestampsResponse> GetTimestampsAsync(GetTimestampsRequest request);
    }
}