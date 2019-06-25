// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.UpdateServices
{
    /// <summary>
    /// Class that encapsulates all data required to authenticate against an upstream update server.
    /// Internally it stores the server authentication info, authentication cookie and access cookies.
    /// 
    /// This class can be persisted (serialized).
    /// </summary>
    public class ServiceAccessToken
    {
        /// <summary>
        /// Authentication data received from an update server
        /// </summary>
        [JsonProperty]
        internal List<AuthPlugInInfo> AuthenticationInfo { get; set; }

        /// <summary>
        /// Authorization cookie received from a DSS
        /// </summary>
        [JsonProperty]
        internal WebServices.DssAuthentication.AuthorizationCookie AuthCookie { get; set; }

        /// <summary>
        /// Access cookie received from the upstream update server
        /// </summary>
        [JsonProperty]
        internal Cookie AccessCookie { get; set; }

        internal ServiceAccessToken()
        {
        }

        /// <summary>
        /// Check if the access token is expired
        /// </summary>
        public bool Expired { get { return ExpiresIn(TimeSpan.FromMilliseconds(0)); } }

        /// <summary>
        /// Check if the access token will expire within the specified time span
        /// </summary>
        /// <param name="timeSpanInFuture">Time span from current time to check expiration</param>
        /// <returns>True if the token will expire before the timespan passes, false otherwise</returns>
        public bool ExpiresIn(TimeSpan timeSpanInFuture)
        {
            return AccessCookie == null ? true : AccessCookie.Expiration < DateTime.Now.AddMinutes(timeSpanInFuture.TotalMinutes);
        }

        /// <summary>
        /// Serialize this object to JSON
        /// </summary>
        /// <returns>JSON string</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        /// <summary>
        /// Create a ServiceAccessToken from a serialized token
        /// </summary>
        /// <param name="json">The JSON string containing a serialized token</param>
        /// <returns>Deserialiazed ServiceAccessToken</returns>
        public static ServiceAccessToken FromJson(string json)
        {
            return JsonConvert.DeserializeObject<ServiceAccessToken>(json);
        }
    }
}
