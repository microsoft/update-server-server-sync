// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ICSharpCode.SharpZipLib.Zip;
using Microsoft.UpdateServices.Client;
using Microsoft.UpdateServices.Compression;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.Metadata.Prerequisites;
using Microsoft.UpdateServices.Storage;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.UpdateServices.Storage
{
    public partial class CompressedMetadataStore
    {
        [JsonProperty]
        private List<KeyValuePair<int, Identity>> IdentityAndIndexList { get; set; }

        [JsonProperty]
        private Dictionary<int, uint> UpdateTypeMap;

        private Dictionary<int, Identity> IndexToIdentity;
        private Dictionary<Identity, int> IdentityToIndex;

        // Identity to Index
        private int this[Identity id] => IdentityToIndex[id];

        private Identity this[int index] => IndexToIdentity[index];

        /// <summary>
        /// Gets the list of updates or categories returned by a query.
        /// </summary>
        /// <value>List of updates</value>
        [JsonIgnore]
        public SortedSet<Identity> Identities { get; private set; }

        /// <summary>
        /// Gets the int based index of all update identities in the metadata source
        /// </summary>
        /// <returns>Dictionary of int to Identity</returns>
        public IReadOnlyDictionary<int, Identity> GetIndex()
        {
            return IndexToIdentity;
        }
    }
}
