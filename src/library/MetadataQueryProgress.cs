// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UpdateServices
{
    public enum QuerySubTaskTypes
    {
        Unknown,
        AuthenticateStart,
        AuthenticateEnd,
        GetServerConfigStart,
        GetServerConfigEnd,
        GetRevisionIdsStart,
        GetRevisionIdsEnd,
        GetUpdateMetadataStart,
        GetUpdateMetadataProgress,
        GetUpdateMetadataEnd
    }

    /// <summary>
    /// Contains progress data for configuration data queries
    /// </summary>
    public class MetadataQueryProgress : EventArgs
    {
        public double PercentDone { get; internal set; }

        public int Maximum { get; internal set; }

        public int Current { get; internal set; }

        public bool IsComplete { get; internal set; }

        public QuerySubTaskTypes CurrentTask { get; internal set; }

        public MetadataQueryProgress()
        {
            CurrentTask = QuerySubTaskTypes.Unknown;
        }
    }
}
