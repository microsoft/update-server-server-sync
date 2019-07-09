using Microsoft.UpdateServices.Metadata.Content;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UpdateServices.Storage
{
    /// <summary>
    /// The RepoOperationType enumeration represents, for reporting purposes, the possible sub states of a repository operation
    /// </summary>
    public enum OperationType
    {
        /// <summary>
        /// Operation initializing
        /// </summary>
        Unknown,
        /// <summary>
        /// Start exporting metadata
        /// </summary>
        ExportMetadataStart,
        /// <summary>
        /// Finished exporting metadata
        /// </summary>
        ExportMetadataEnd,
        /// <summary>
        /// Started exporting XML data 
        /// </summary>
        ExportUpdateXmlBlobStart,
        /// <summary>
        /// Progress for exporting XML data
        /// </summary>
        ExportUpdateXmlBlobProgress,
        /// <summary>
        /// Finished exporting XML data
        /// </summary>
        ExportUpdateXmlBlobEnd,
        /// <summary>
        /// Started compressing the exported data
        /// </summary>
        CompressExportFileStart,
        /// <summary>
        /// Finished compressing the exported data
        /// </summary>
        CompressExportFileEnd,
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
    public class OperationProgress : EventArgs
    {
        /// <summary>
        /// Percent done. Not all operation types support progress reporting.
        /// </summary>
        /// <value>
        /// Percent done value, in the [0,100] range.
        /// </value>
        public double PercentDone { get; internal set; }

        /// <summary>
        /// Number of work items. Reported only for operations types that support percent done reporting. 
        /// </summary>
        /// <value>
        /// Number of work items (updates, etc.) to process
        /// </value>
        public long Maximum { get; internal set; }

        /// <summary>
        /// Number of work items processed. Reported only for operations that support percent done reporting.
        /// </summary>
        /// <value>
        /// Number of work items (updates, etc.) processed so far.</value>
        public long Current { get; internal set; }

        /// <summary>
        /// The operation that is currently executing.
        /// </summary>
        /// <value>One of the possible operations from <see cref="OperationType"/></value>
        public OperationType CurrentOperation { get; internal set; }

        internal OperationProgress()
        {
            CurrentOperation = OperationType.Unknown;
        }
    }

    /// <summary>
    /// Represents progress data for operations that process files
    /// </summary>
    public class ContentOperationProgress : OperationProgress
    {
        /// <summary>
        ///  The file being processed
        /// </summary>
        /// <value>Update file processed</value>
        public UpdateFile File { get; internal set; }
    }
}
