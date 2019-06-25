// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.UpdateServices
{
    /// <summary>
    /// Endpoint definition for an Upstream Update Service
    /// </summary>
    public class Endpoint
    {
        /// <summary>
        /// The Microsoft upstream server root address. Used to construct the default endpoint.
        /// </summary>
        private const string MicrosoftUpstreamRoot = @"https://sws.update.microsoft.com";

        /// <summary>
        /// The upstream server root address
        /// </summary>
        public readonly string UpstreamRoot;

        /// <summary>
        /// The server-to-server sync service address built from the root address
        /// </summary>
        public string ServerSyncRoot => UpstreamRoot + @"/ServerSyncWebService/ServerSyncWebService.asmx";

        /// <summary>
        /// The protocol version supported by this library
        /// </summary>
        public const string ProtocolVersion = "1.21";

        public Endpoint(string rootRemoteAddress)
        {
            UpstreamRoot = rootRemoteAddress;
        }

        /// <summary>
        /// Accessor for the default Microsoft endpoint
        /// </summary>
        public static Endpoint Default => new Endpoint(MicrosoftUpstreamRoot);

        /// <summary>
        /// Creates a complete URL to a DSS authentication web service based on the upstream URL and the DSS relative URL
        /// </summary>
        /// <param name="serviceRelativeUrl">The DSS service URL (relative)</param>
        /// <returns>The complete URL to the DSS authentication endpoint</returns>
        internal string GetAuthenticationEndpointFromRelativeUrl(string serviceRelativeUrl)
        {
            return UpstreamRoot + "/" + serviceRelativeUrl;
        }
    }
}
