// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Xml.Linq;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Classification metadata
    /// </summary>
    public class Classification : MicrosoftUpdate
    {
        [JsonConstructor]
        private Classification()
        {

        }

        public Classification(ServerSyncUpdateData serverSyncUpdateData, XDocument xdoc) : base(serverSyncUpdateData)
        {
            var titleAndDescription = GetTitleAndDescriptionFromXml(xdoc);
            Title = titleAndDescription.Key;
            Description = titleAndDescription.Value;
            UpdateType = MicrosoftUpdateType.Classification;
        }
    }
}
