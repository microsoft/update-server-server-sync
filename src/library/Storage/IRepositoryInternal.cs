// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Client;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Query;
using Microsoft.UpdateServices.WebServices.ServerSync;
using System.IO;

namespace Microsoft.UpdateServices.Storage
{
    interface IRepositoryInternal
    {
        /// <summary>
        /// Returns the configuration of the upstream this repository is tracking
        /// </summary>
        ServerSyncConfigData ServiceConfiguration { get; }

        /// <summary>
        /// Update the upstream server configuration
        /// </summary>
        /// <param name="configData">New service configuration</param>
        void SetServiceConfiguration(ServerSyncConfigData configData);

        /// <summary>
        /// Returns the access token for the upstream server
        /// </summary>
        ServiceAccessToken AccessToken { get; }

        /// <summary>
        /// Set the access token for the upstream server
        /// </summary>
        /// <param name="newAccessToken"></param>
        void SetAccessToken(ServiceAccessToken newAccessToken);


        /// <summary>
        /// Checks the existence of the XML belonging to an update
        /// </summary>
        /// <param name="update">The update to check</param>
        /// <returns>True if XML data is available for the update, false otherwise</returns>
        bool IsUpdateXmlAvailable(Update update);

        /// <summary>
        /// Get a writeable stream to add or change an update's XML data
        /// </summary>
        /// <param name="update">The update to get the write stream for</param>
        /// <returns>Writeable stream</returns>
        Stream GetUpdateXmlWriteStream(Update update);

        /// <summary>
        /// Gets a stream reader to an update's XML data
        /// </summary>
        /// <param name="update">The update to get the read stream for </param>
        /// <returns>stream reader</returns>
        StreamReader GetUpdateXmlReader(Update update);

        /// <summary>
        /// Get the last anchor received for a categories sync
        /// </summary>
        /// <returns>Anchor string</returns>
        string GetCategoriesAnchor();

        /// <summary>
        /// Gets the last anchor received for updates sync for a specific filter
        /// </summary>
        /// <param name="filter">The filter used to query for updates.</param>
        /// <returns>Anchor string</returns>
        string GetUpdatesAnchorForFilter(QueryFilter filter);
    }
}
