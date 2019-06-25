// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Interface implemented by updates that can superseed other updates
    /// </summary>
    public interface IUpdateWithSuperseededUpdates
    {
        List<MicrosoftUpdateIdentity> SuperseededUpdates { get; }
    }
}
