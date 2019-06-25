// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Stores information for an update file
    /// </summary>
    public class UpdateFileUrl
    {
        /// <summary>
        /// The file digest
        /// </summary>
        public string DigestBase64 { get; set; }

        /// <summary>
        /// The MU URL
        /// </summary>
        public string MuUrl { get; set; }

        /// <summary>
        /// The USS URL
        /// </summary>
        public string UssUrl { get; set; }

        /// <summary>
        /// Private constructor for deserialization
        /// </summary>
        [JsonConstructor]
        private UpdateFileUrl() { }

        /// <summary>
        /// Construct object from raw ServerSyncUrlData
        /// </summary>
        /// <param name="serverSyncUrlData"></param>
        public UpdateFileUrl(ServerSyncUrlData serverSyncUrlData)
        {
            DigestBase64 = Convert.ToBase64String(serverSyncUrlData.FileDigest);
            MuUrl = serverSyncUrlData.MUUrl;
            UssUrl = serverSyncUrlData.UssUrl;
        }

        /// <summary>
        /// Equality override. Two FileUrls are equal if they have the same digest
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return ReferenceEquals(obj, null) || !(obj is UpdateFileUrl)
                ? false
                : this.DigestBase64.Equals((obj as UpdateFileUrl).DigestBase64);
        }

        public override int GetHashCode()
        {
            return DigestBase64.GetHashCode();
        }

        public static bool operator ==(UpdateFileUrl lhs, UpdateFileUrl rhs) => ReferenceEquals(lhs, null) ? ReferenceEquals(rhs, null) : lhs.Equals(rhs);

        public static bool operator !=(UpdateFileUrl lhs, UpdateFileUrl rhs)
        {
            return !(lhs == rhs);
        }
    }
}
