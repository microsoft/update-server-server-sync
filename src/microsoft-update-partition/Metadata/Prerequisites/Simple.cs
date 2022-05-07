// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;
using System;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Prerequisites
{
    /// <summary>
    /// Simple prerequisite: a single update ID.
    /// <para>The update ID contained in a simple prerequisite must be installed before the update that has this prerequisite can be installed.</para>
    /// <para>The detectoid ID contained in a simple prerequisite must evaluate to true before the update that has this prerequisite can be installed. See <see cref="DetectoidCategory"/> for more details.</para>
    /// </summary>
    public class Simple : IPrerequisite
    {
        /// <summary>
        /// The update ID or detectoid ID that is required before an update can be installed.
        /// </summary>
        public Guid UpdateId { get; private set; }

        internal Simple(Guid id)
        {
            // Parse the guid from the XML data
            UpdateId = id;
        }
    }
}
