using Microsoft.UpdateServices.Metadata.Content;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UpdateServices.LocalCache
{
    public enum RepoOperationTypes
    {
        Unknown,
        ExportMetadataStart,
        ExportMetadataEnd,
        ExportUpdateXmlBlobStart,
        ExportUpdateXmlBlobProgress,
        ExportUpdateXmlBlobEnd,
        CompressExportFileStart,
        CompressExportFileEnd,
        DownloadFileStart,
        DownloadFileProgress,
        DownloadFileEnd,
        HashFileStart,
        HashFileProgress,
        HashFileEnd
    }

    /// <summary>
    /// Contains progress data for operations on local repositories
    /// </summary>
    public class RepoOperationProgress : EventArgs
    {
        public double PercentDone { get; internal set; }

        public long Maximum { get; internal set; }

        public long Current { get; internal set; }

        public RepoOperationTypes CurrentOperation { get; internal set; }

        public RepoOperationProgress()
        {
            CurrentOperation = RepoOperationTypes.Unknown;
        }
    }

    public class RepoContentOperationProgress : RepoOperationProgress
    {
        public UpdateFile File { get; internal set; }
    }
}
