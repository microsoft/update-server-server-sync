// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Represents a detectoid. Detectoids determine applicabilty of updates for a computer and as such are used
    /// as pre-requisites for other updates.
    /// <para>
    /// Example detectoids: x64, x86, arm64, DirectX12 supported, etc.
    /// </para>
    /// </summary>
    /// <example>
    /// <code>
    /// var server = new UpstreamServerClient(Endpoint.Default);
    /// 
    /// // Query categories
    /// var categoriesQueryResult = await server.GetCategories();
    /// 
    /// // Get detectoids
    /// var detectoids = categoriesQueryResult.Updates.OfType&lt;Detectoid&gt;();
    /// </code>
    /// </example>
    public class Detectoid : Update
    {
        [JsonConstructor]
        private Detectoid()
        {

        }

        internal Detectoid(ServerSyncUpdateData serverSyncUpdateData, XDocument xdoc) : base(serverSyncUpdateData)
        {
            GetTitleAndDescriptionFromXml(xdoc);
            UpdateType = UpdateType.Detectoid;
        }
    }
}
