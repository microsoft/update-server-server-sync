// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.PackageGraph.ObjectModel
{
    /// <summary>
    /// The OperationType enumeration represents, for reporting purposes, the possible sub states of an operation on a metadata source
    /// </summary>
    public enum PackagesOperationType
    {
        /// <summary>
        /// Operation initializing
        /// </summary>
        Unknown,
        /// <summary>
        /// Started downloading a file
        /// </summary>
        DownloadFileStart,
        /// <summary>
        /// Progress for downloading a file
        /// </summary>
        DownloadFileProgress,
        /// <summary>
        /// Finished downloading a file
        /// </summary>
        DownloadFileEnd,
        /// <summary>
        /// Started checking the hash on a file
        /// </summary>
        HashFileStart,
        /// <summary>
        /// Progress of hash checking
        /// </summary>
        HashFileProgress,
        /// <summary>
        /// Completed the hash check for a file
        /// </summary>
        HashFileEnd
    }

    /// <summary>
    /// Represents progress data for operations on local repositories
    /// </summary>
    public class PackagesOperationProgress : EventArgs
    {
        /// <summary>
        /// Percent done. Not all operation types support progress reporting.
        /// </summary>
        /// <value>
        /// Percent done value, in the [0,100] range.
        /// </value>
        public double PercentDone => (Maximum == 0 ? 0 : ((double)Current * 100) / Maximum);

        /// <summary>
        /// Number of work items. Reported only for operations types that support percent done reporting. 
        /// </summary>
        /// <value>
        /// Number of work items (updates, etc.) to process
        /// </value>
        public long Maximum { get; set; }

        /// <summary>
        /// Number of work items processed. Reported only for operations that support percent done reporting.
        /// </summary>
        /// <value>
        /// Number of work items (updates, etc.) processed so far.</value>
        public long Current { get; set; }

        /// <summary>
        /// The operation that is currently executing.
        /// </summary>
        /// <value>One of the possible operations from <see cref="PackagesOperationType"/></value>
        public PackagesOperationType CurrentOperation { get; set; }

        internal PackagesOperationProgress()
        {
            CurrentOperation = PackagesOperationType.Unknown;
        }
    }

    /// <summary>
    /// Represents progress data for operations that process files
    /// </summary>
    public class ContentOperationProgress : PackagesOperationProgress
    {
        /// <summary>
        ///  The file being processed
        /// </summary>
        /// <value>Update file processed</value>
        public IContentFile File { get; set; }
    }
}
