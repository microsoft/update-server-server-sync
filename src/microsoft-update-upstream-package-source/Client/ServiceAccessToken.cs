// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Source
{
    /// <summary>
    /// Grants access to an upstream update server. Requried for most requests to an update server.
    /// </summary>
    class ServiceAccessToken
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
        internal UpdateServices.WebServices.DssAuthentication.AuthorizationCookie AuthCookie { get; set; }

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
        /// <value>True is expired, false otherwise</value>
        public bool Expired { get { return ExpiresIn(TimeSpan.FromMilliseconds(0)); } }

        /// <summary>
        /// Check if the access token will expire within the specified time span
        /// </summary>
        /// <param name="timeSpan">Time span from current time.</param>
        /// <returns>True if the token will expire before the timespan passes, false otherwise</returns>
        public bool ExpiresIn(TimeSpan timeSpan)
        {
            return AccessCookie == null || AccessCookie.Expiration < DateTime.Now.AddMinutes(timeSpan.TotalMinutes);
        }

        /// <summary>
        /// Serializes an instance of <see cref="ServiceAccessToken"/> to JSON
        /// </summary>
        /// <returns>JSON string</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        /// <summary>
        /// Deserialize an instance of <see cref="ServiceAccessToken"/> from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string containing the serialized ServiceAccessToken</param>
        /// <returns>Deserialiazed ServiceAccessToken</returns>
        public static ServiceAccessToken FromJson(string json)
        {
            return JsonConvert.DeserializeObject<ServiceAccessToken>(json);
        }
    }
}
