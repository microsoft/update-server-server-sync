// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Interface implemented by updates that have one or more classifications
    /// </summary>
    public interface IUpdateWithClassification
    {
        /// <summary>
        /// Get the list of classifications for an update
        /// </summary>
        /// <value>
        /// List of GUIDs; each GUID maps to a classification GUID
        /// </value>
        List<Guid> ClassificationIds { get; }
    }

    /// <summary>
    /// Represents a Classification. Used to clasify updates on an upstream server.
    /// <para>
    /// Example classifications: drivers, security updates, feature packs etc.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// var server = new UpstreamServerClient(Endpoint.Default);
    /// 
    /// // Query categories
    /// var categoriesQueryResult = await server.GetCategories();
    /// 
    /// // Get classifications
    /// var products = categoriesQueryResult.Updates.OfType&lt;Classification&gt;();
    /// </code>
    /// </example>
    public class Classification : Update
    {
        [JsonConstructor]
        private Classification()
        {

        }

        internal Classification(ServerSyncUpdateData serverSyncUpdateData, XDocument xdoc) : base(serverSyncUpdateData)
        {
            GetTitleAndDescriptionFromXml(xdoc);
            UpdateType = UpdateType.Classification;
        }
    }
}
