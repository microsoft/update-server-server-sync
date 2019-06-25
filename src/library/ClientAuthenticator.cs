// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UpdateServices.WebServices.DssAuthentication;
using Microsoft.UpdateServices.WebServices.ServerSync;

namespace Microsoft.UpdateServices
{
    /// <summary>
    /// Provides authentication against an upstream update server.
    /// To get access to an upstream update service (USS), an access cookie is required. The flow to
    /// obtain an access cookie is:
    /// 1. Obtain auth info from the update service
    /// 2. Use the auth info to build the URL to the DSS authentication service
    /// 3. Obtain an authentication cookie from the DSS authentication service
    /// 4. Obtain an access cookie from the USS using the authorization token from the DSS
    /// 
    /// This class simplifies the above flow by exposing a single "Authenticate" operation that performs 1-4 above
    /// and returns a ServiceAccessToken containing the auth info, auth cookie and access cookie.
    /// </summary>
    public class ClientAuthenticator
    {
        /// <summary>
        /// The remote server the authenticator will run against.
        /// </summary>
        public readonly Endpoint UpstreamEndpoint;

        /// <summary>
        /// Create an authentication client that authenticates with the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to authenticate with.</param>
        public ClientAuthenticator(Endpoint endpoint = null)
        {
            UpstreamEndpoint = endpoint == null ? Endpoint.Default : endpoint;
        }

        /// <summary>
        /// Performs a fast re-authentication using the provided cached authentication token
        /// </summary>
        /// <param name="cachedAccessToken">A cached access token for fast re-authentication.</param>
        /// <returns>A ServiceAccessToken used to query the upstream update service.</returns>
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
                if (ex.ErrorCode == UpstreamServerErrorCodes.InvalidAuthorizationCookie)
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
        /// <returns>A ServiceAccessToken used to query the upstream update service.</returns>
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

            // Create a WSUS server sync client
            IServerSyncWebService serverSyncClient = new ServerSyncWebServiceClient(
                ServerSyncWebServiceClient.EndpointConfiguration.BasicHttpsBinding_IServerSyncWebService,
                UpstreamEndpoint.ServerSyncRoot);

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
            // Create a DSS client using the endpoint retrieved above
            IDSSAuthWebService authenticationService = new DSSAuthWebServiceClient(
                DSSAuthWebServiceClient.EndpointConfiguration.BasicHttpsBinding_IDSSAuthWebService,
                UpstreamEndpoint.GetAuthenticationEndpointFromRelativeUrl(authInfo.ServiceUrl));

            // Issue the request. All accounts are allowed, so we just generate a random account guid and name
            var cookieRequest = new GetAuthorizationCookieRequest();
            cookieRequest.GetAuthorizationCookie = new GetAuthorizationCookieRequestBody();
            cookieRequest.GetAuthorizationCookie.accountGuid = Guid.NewGuid().ToString();
            cookieRequest.GetAuthorizationCookie.accountName = Guid.NewGuid().ToString();

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
            // Create a service client on the default Microsoft upstream server.
            IServerSyncWebService serverSyncClient = new ServerSyncWebServiceClient(
                ServerSyncWebServiceClient.EndpointConfiguration.BasicHttpsBinding_IServerSyncWebService,
                UpstreamEndpoint.ServerSyncRoot);

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
