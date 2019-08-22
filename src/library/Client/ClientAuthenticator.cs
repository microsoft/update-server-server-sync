// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UpdateServices.WebServices.DssAuthentication;
using Microsoft.UpdateServices.WebServices.ServerSync;

namespace Microsoft.UpdateServices.Client
{
    /// <summary>
    /// Implements authentication with an upstream update server.
    /// <para>
    /// Use the ClientAuthenticator to obtain an access token for accessing metadata and content on an upstream update server.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// var authenticator = new ClientAuthenticator(Endpoint.Default);
    /// var accessToken = await authenticator.Authenticate();
    /// </code>
    /// </example>
    class ClientAuthenticator
    {
        /// <summary>
        /// Gets the update server endpoint this instance of ClientAuthenticator authenticates with.
        /// </summary>
        public readonly Endpoint UpstreamEndpoint;

        /// <summary>
        /// Initializes a new instance of the ClientAuthenticator class to authenticate with the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to authenticate with.</param>
        public ClientAuthenticator(Endpoint endpoint)
        {
            UpstreamEndpoint = endpoint;
            AccountGuid = new Guid();
            AccountName = new Guid().ToString();
        }

        /// <summary>
        /// Initializes a new instance of the ClientAuthenticator that authenticates with the official
        /// Microsoft upstream update server.
        /// </summary>
        public ClientAuthenticator()
        {
            UpstreamEndpoint = Endpoint.Default;
            AccountGuid = new Guid();
            AccountName = new Guid().ToString();
        }

        /// <summary>
        /// Account name used when authenticating. If null, a random GUID string is used.
        /// </summary>
        private readonly string AccountName = null;

        /// <summary>
        /// Account GUID used for authenticating. If null, a random GUID is used
        /// </summary>
        private readonly Guid? AccountGuid = null;

        /// <summary>
        /// Initializes a new instance of the ClientAuthenticator class to authenticate with the specified endpoint, using
        /// specified credentials.
        /// </summary>
        /// <param name="endpoint">The endpoint to authenticate with.</param>
        /// <param name="accountName">Account name.</param>
        /// <param name="accountGuid">Account GUID.</param>
        public ClientAuthenticator(Endpoint endpoint, string accountName, Guid accountGuid)
        {
            UpstreamEndpoint = endpoint;

            if (accountGuid != null)
            {
                AccountGuid = accountGuid;
            }
            else
            {
                AccountGuid = new Guid();
            }
            
            if (!string.IsNullOrEmpty(accountName))
            {
                AccountName = accountName;
            }
            else
            {
                AccountName = new Guid().ToString();
            }
        }

        /// <summary>
        /// Performs authentication with an upstream update server, using a previously issued service access token.
        /// </summary>
        /// <remarks>
        /// Refreshing an old token with this method is faster than obtaining a new token as it requires fewer server roundtrips.
        /// 
        /// If the access cookie does not expire within 30 minutes, the function succeeds and the old token is returned.
        /// </remarks>
        /// <param name="cachedAccessToken">The previously issued access token.</param>
        /// <returns>The new ServiceAccessToken</returns>
        public async Task<ServiceAccessToken> Authenticate(ServiceAccessToken cachedAccessToken)
        {
            if (cachedAccessToken == null)
            {
                return await Authenticate();
            }

            ServiceAccessToken newAccessToken = new ServiceAccessToken()
            {
                AuthCookie = cachedAccessToken.AuthCookie,
                AccessCookie = cachedAccessToken.AccessCookie,
                AuthenticationInfo = cachedAccessToken.AuthenticationInfo
            };

            // Check if the cached access cookie expires in the next 30 minutes; if not, return the new token
            // with this cookie
            if (!newAccessToken.ExpiresIn(TimeSpan.FromMinutes(30)))
            {
                return newAccessToken;
            }

            bool restartAuthenticationRequired = false;

            // Get a new access cookie
            try
            {
                newAccessToken.AccessCookie = await GetServerAccessCookie(newAccessToken.AuthCookie);
            }
            catch (UpstreamServerException ex)
            {
                if (ex.ErrorCode == UpstreamServerErrorCode.InvalidAuthorizationCookie)
                {
                    // The authorization cookie is expired or invalid. Restart the authentication protocol
                    restartAuthenticationRequired = true;
                }
                else
                {
                    throw ex;
                }
            }

            return restartAuthenticationRequired ? await Authenticate() : newAccessToken;
        }

        /// <summary>
        /// Performs authentication with an upstream update service.
        /// </summary>
        /// <returns>A new access token.</returns>
        public async Task<ServiceAccessToken> Authenticate()
        {
            ServiceAccessToken newAccessToken = new ServiceAccessToken();

            newAccessToken.AuthenticationInfo = (await GetAuthenticationInfo()).ToList();
            newAccessToken.AuthCookie = await GetAuthorizationCookie(newAccessToken.AuthenticationInfo[0]);
            newAccessToken.AccessCookie = await GetServerAccessCookie(newAccessToken.AuthCookie);

            return newAccessToken;
        }

        /// <summary>
        /// Retrieves authentication information from a WSUS server.
        /// </summary>
        /// <returns>List of supported authentication methods</returns>
        private async Task<AuthPlugInInfo[]> GetAuthenticationInfo()
        {
            GetAuthConfigResponse authConfigResponse;

            var httpBinding = new System.ServiceModel.BasicHttpBinding();
            var upstreamEndpoint = new System.ServiceModel.EndpointAddress(UpstreamEndpoint.ServerSyncURI);
            if (upstreamEndpoint.Uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                httpBinding.Security.Mode = System.ServiceModel.BasicHttpSecurityMode.Transport;
            }

            // Create a WSUS server sync client
            IServerSyncWebService serverSyncClient = new ServerSyncWebServiceClient(httpBinding, upstreamEndpoint);

            // Retrieve the authentication information
            authConfigResponse = await serverSyncClient.GetAuthConfigAsync(new GetAuthConfigRequest());

            if (authConfigResponse == null)
            {
                throw new Exception("Authentication config response was null.");
            }
            else if (authConfigResponse.GetAuthConfigResponse1.GetAuthConfigResult.AuthInfo == null)
            {
                throw new Exception("Authentication config payload was null.");
            }

            return authConfigResponse.GetAuthConfigResponse1.GetAuthConfigResult.AuthInfo;
        }

        /// <summary>
        /// Retrieves an authentication cookie from a DSS service.
        /// </summary>
        /// <returns>An authentication cookie</returns>
        private async Task<WebServices.DssAuthentication.AuthorizationCookie> GetAuthorizationCookie(AuthPlugInInfo authInfo)
        {
            var httpBinding = new System.ServiceModel.BasicHttpBinding();
            var upstreamEndpoint = new System.ServiceModel.EndpointAddress(UpstreamEndpoint.GetAuthenticationEndpointFromRelativeUrl(authInfo.ServiceUrl));

            if (upstreamEndpoint.Uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                httpBinding.Security.Mode = System.ServiceModel.BasicHttpSecurityMode.Transport;
            }

            // Create a DSS client using the endpoint retrieved above
            IDSSAuthWebService authenticationService = new DSSAuthWebServiceClient(httpBinding, upstreamEndpoint);

            // Issue the request. All accounts are allowed, so we just generate a random account guid and name
            var cookieRequest = new GetAuthorizationCookieRequest();
            cookieRequest.GetAuthorizationCookie = new GetAuthorizationCookieRequestBody();
            cookieRequest.GetAuthorizationCookie.accountGuid = AccountName;
            cookieRequest.GetAuthorizationCookie.accountName = AccountGuid.ToString();

            var getAuthCookieResponse = await authenticationService.GetAuthorizationCookieAsync(cookieRequest);

            if (getAuthCookieResponse == null ||
                getAuthCookieResponse.GetAuthorizationCookieResponse1.GetAuthorizationCookieResult.CookieData == null)
            {
                throw new Exception("Failed to get authorization token. Response or cookie is null.");
            }

            return getAuthCookieResponse.GetAuthorizationCookieResponse1.GetAuthorizationCookieResult;
        }

        /// <summary>
        /// Retrieves a server access cookie based on an authentication cookie.
        /// </summary>
        /// <param name="authCookie">The auth cookie to use when requesting the access cookie</param>
        /// <returns>An access cookie</returns>
        private async Task<Cookie> GetServerAccessCookie(WebServices.DssAuthentication.AuthorizationCookie authCookie)
        {
            var httpBinding = new System.ServiceModel.BasicHttpBinding();
            var upstreamEndpoint = new System.ServiceModel.EndpointAddress(UpstreamEndpoint.ServerSyncURI);
            if (upstreamEndpoint.Uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                httpBinding.Security.Mode = System.ServiceModel.BasicHttpSecurityMode.Transport;
            }

            // Create a service client on the default Microsoft upstream server.
            IServerSyncWebService serverSyncClient = new ServerSyncWebServiceClient(httpBinding, upstreamEndpoint);

            // Create an access cookie request using the authentication cookie parameter.
            var cookieRequest = new GetCookieRequest();
            cookieRequest.GetCookie = new GetCookieRequestBody();
            cookieRequest.GetCookie.authCookies = new WebServices.ServerSync.AuthorizationCookie[] { new WebServices.ServerSync.AuthorizationCookie() };
            cookieRequest.GetCookie.authCookies[0].CookieData = authCookie.CookieData;
            cookieRequest.GetCookie.authCookies[0].PlugInId = authCookie.PlugInId;
            cookieRequest.GetCookie.oldCookie = null;
            cookieRequest.GetCookie.protocolVersion = "1.7";

            GetCookieResponse cookieResponse;
            try
            {
                cookieResponse = await serverSyncClient.GetCookieAsync(cookieRequest);
            }
            catch(System.ServiceModel.FaultException ex)
            {
                throw new UpstreamServerException(ex);
            }

            if (cookieResponse == null ||
                cookieResponse.GetCookieResponse1.GetCookieResult.EncryptedData == null)
            {
                throw new Exception("Failed to get access cookie. Response or cookie is null.");
            }

            return cookieResponse.GetCookieResponse1.GetCookieResult;
        }
    }
}
