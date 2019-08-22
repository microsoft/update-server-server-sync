// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.Client;

namespace Microsoft.UpdateServices.Storage
{
    /// <summary>
    /// Defines access methods to a source of update content
    /// </summary>
    public interface IUpdateContentSink
    {
        /// <summary>
        /// Raised on progress for long running content store operations
        /// </summary>
        /// <value>
        /// Progress data.
        /// </value>
        event EventHandler<OperationProgress> Progress;

        /// <summary>
        /// Download content
        /// </summary>
        /// <param name="files">The files to download</param>
        void Add(IEnumerable<UpdateFile> files);
    }
}
