// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Implemented by updates that can bundle other updates
    /// </summary>
    public interface IUpdateWithBundledUpdates
    {
        List<MicrosoftUpdateIdentity> BundledUpdates { get; }
    }
}
