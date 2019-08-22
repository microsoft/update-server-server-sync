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
    public interface IUpdateContentSource
    {
        /// <summary>
        /// Raised on progress for long running content store operations
        /// </summary>
        /// <value>
        /// Progress data.
        /// </value>
        event EventHandler<OperationProgress> Progress;

        /// <summary>
        /// Checks if an update file has been downloaded
        /// </summary>
        /// <param name="file">File to check if it was downloaded</param>
        /// <returns>True if the file was downloaded, false otherwise</returns>
        bool Contains(UpdateFile file);

        /// <summary>
        /// Gets a read only stream for an update content file
        /// </summary>
        /// <param name="updateFile">The update file to open</param>
        /// <returns>Read only stream for the requested update content file</returns>
        Stream Get(UpdateFile updateFile);
    }
}
