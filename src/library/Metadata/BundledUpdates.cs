// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Interface implemented by updates that can bundle other updates
    /// </summary>
    public interface IUpdateWithBundledUpdates
    {
        /// <summary>
        /// List of bundled updates
        /// </summary>
        /// <value>
        /// List of update identities.
        /// </value>
        List<Identity> BundledUpdates { get; }
    }
}
