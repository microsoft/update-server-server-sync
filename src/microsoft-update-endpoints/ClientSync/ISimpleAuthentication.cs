// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ClientSync
{
    /// <summary>
    /// Declare an interface for the SimpleAuthSoap (WCF) service that can be used in AspNetCore with slight modifications.
    /// GetAuthorizationCookieAsync returns AuthorizationCookie instead of the nested GetAuthorizationCookieResult\GetAuthorizationResultBody
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.0.1-preview-30422-0661")]
    [System.ServiceModel.ServiceContractAttribute(Namespace = "http://www.microsoft.com/SoftwareDistribution/Server/SimpleAuthWebService", ConfigurationName = "Microsoft.UpdateServices.WebServices.ClientAuthentication.SimpleAuthSoap")]
    interface ISimpleAuthenticationWebService
    {

        [System.ServiceModel.OperationContractAttribute(Action = "http://www.microsoft.com/SoftwareDistribution/Server/SimpleAuthWebService/GetAuth" +
            "orizationCookie", ReplyAction = "*")]
        System.Threading.Tasks.Task<Microsoft.UpdateServices.WebServices.ClientAuthentication.AuthorizationCookie> GetAuthorizationCookieAsync(Microsoft.UpdateServices.WebServices.ClientAuthentication.GetAuthorizationCookieRequest request);
    }
}