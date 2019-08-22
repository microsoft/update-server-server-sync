// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Microsoft.UpdateServices.Metadata.Content;

namespace Microsoft.UpdateServices.Storage
{
    /// <summary>
    /// Interface for objects that can store update metadata
    /// </summary>
    public interface IMetadataSink
    {
        /// <summary>
        /// Flushes out all pending changes and closes the sink. No more changes can be made to the sink after calling this method.
        /// </summary>
        void Commit();

        /// <summary>
        /// Adds a list of updates to the update metadata collection.
        /// </summary>
        /// <param name="overTheWireUpdates">The updates to add to the result, as received from the upstream server</param>
        void AddUpdates(IEnumerable<ServerSyncUpdateData> overTheWireUpdates);

        /// <summary>
        /// Sets the categories anchor for the categories in the metadata collection
        /// </summary>
        /// <param name="anchor"></param>
        void SetCategoriesAnchor(string anchor);

        /// <summary>
        /// Sets the filter used when adding updates to the metadata collection
        /// </summary>
        /// <param name="filter">Filter</param>
        void SetQueryFilter(QueryFilter filter);

        /// <summary>
        /// Adds an update content file URL to the metadata collection
        /// </summary>
        /// <param name="file">The file to add</param>
        void AddFile(UpdateFileUrl file);

        /// <summary>
        /// Event raised on progress during a long-running commit operation
        /// </summary>
        event EventHandler<OperationProgress> CommitProgress;
    }
}
