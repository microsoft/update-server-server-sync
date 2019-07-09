// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Interface implemented by updates that superseed other updates
    /// </summary>
    public interface IUpdateWithSupersededUpdates
    {
        /// <summary>
        /// List of Update Ids superseded by an update.
        /// </summary>
        /// <value>List of update <see cref="Identity"/></value>
        List<Identity> SupersededUpdates { get; }
    }
}
