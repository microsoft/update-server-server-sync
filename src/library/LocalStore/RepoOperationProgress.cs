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
        CompressExportFileEnd
    }

    /// <summary>
    /// Contains progress data for operations on local repositories
    /// </summary>
    public class RepoOperationProgress : EventArgs
    {
        public double PercentDone { get; internal set; }

        public int Maximum { get; internal set; }

        public int Current { get; internal set; }

        public RepoOperationTypes CurrentOperation { get; internal set; }

        public RepoOperationProgress()
        {
            CurrentOperation = RepoOperationTypes.Unknown;
        }
    }
}
