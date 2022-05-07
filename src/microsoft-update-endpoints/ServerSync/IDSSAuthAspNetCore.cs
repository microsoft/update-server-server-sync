// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.UpdateServices.WebServices.DssAuthentication;
using System.ServiceModel;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ServerSync
{
    /// <summary>
    /// Declare an interface for the DSSAuthWebService (WCF) that can be used in AspNetCore with slight modifications.
    /// GetAuthorizationCookieAsync returns AuthorizationCookie instead of the nested GetAuthorizationCookieResult\GetAuthorizationResultBody
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("dotnet-svcutil", "1.0.0.1")]
    [ServiceContractAttribute(Namespace = "http://www.microsoft.com/SoftwareDistribution/Server/DssAuthWebService", ConfigurationName = "Microsoft.UpdateServices.WebServices.DssAuthentication.IDSSAuthWebService")]
    interface IDSSAuthAspNetCore
    {

        [OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/Server/DssAuthWebService/GetAuthori" +
            "zationCookie", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/Server/DssAuthWebService/IDSSAuthWe" +
            "bService/GetAuthorizationCookieResponse")]
        Task<AuthorizationCookie> GetAuthorizationCookieAsync(GetAuthorizationCookieRequest request);

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/Server/IMonitorable/Ping", ReplyAction = "http://www.microsoft.com/SoftwareDistribution/Server/DssAuthWebService/IDSSAuthWe" +
            "bService/PingResponse")]
        System.Threading.Tasks.Task<PingResponse> PingAsync(PingRequest request);
    }
}