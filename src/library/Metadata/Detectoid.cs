// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Detectoid metadata
    /// </summary>
    public class Detectoid : MicrosoftUpdate
    {
        [JsonConstructor]
        private Detectoid()
        {

        }

        public Detectoid(ServerSyncUpdateData serverSyncUpdateData, XDocument xdoc) : base(serverSyncUpdateData)
        {
            var titleAndDescription = GetTitleAndDescriptionFromXml(xdoc);
            Title = titleAndDescription.Key;
            Description = titleAndDescription.Value;
            UpdateType = MicrosoftUpdateType.Detectoid;
        }
    }
}
