// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System;

namespace Microsoft.PackageGraph.ObjectModel
{
    /// <summary>
    /// Represents digest information for an update content file
    /// </summary>
    public interface IContentFileDigest
    {
        /// <summary>
        /// Gets the digest algorithm used
        /// </summary>
        /// <value>Digest algorithm name</value>
        string Algorithm { get; }

        /// <summary>
        /// Gets the base64 encoded digest
        /// </summary>
        /// <value>Base64 encoded string</value>
        string DigestBase64 { get; }

        /// <summary>
        /// Gets the HEX string representation of the digest 
        /// </summary>
        string HexString { get; }
    }
}
