// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.UpdateServices.WebServices.DssAuthentication;

namespace Microsoft.UpdateServices.Server
{
    /// <summary>
    /// Authentication service implementation; mock implementation, all requests get an authorization cookie, regardless of credentials
    /// </summary>
    class AuthenticationWebService : IDSSAuthAspNetCore
    {
        /// <summary>
        /// Return a mock cookie
        /// </summary>
        /// <param name="request">The SOAP request for an authorization cookie</param>
        /// <returns>The authorization cookie</returns>
        public Task<AuthorizationCookie> GetAuthorizationCookieAsync(GetAuthorizationCookieRequest request)
        {
            return Task.FromResult(new AuthorizationCookie() { CookieData = new byte[5], PlugInId = "15" });
        }

        public Task<PingResponse> PingAsync(PingRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
