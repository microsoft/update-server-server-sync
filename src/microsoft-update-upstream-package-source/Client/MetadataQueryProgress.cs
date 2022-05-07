// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Source
{
    /// <summary>
    /// The MetadataQueryStage enumeration represents, for reporting purposes, the possible execution states of an update metadata query.
    /// </summary>
    public enum MetadataQueryStage
    {
        /// <summary>
        /// The query is being prepared.
        /// </summary>
        Unknown,
        /// <summary>
        /// Authentication is starting.
        /// </summary>
        AuthenticateStart,
        /// <summary>
        /// Authentication ended.
        /// </summary>
        AuthenticateEnd,
        /// <summary>
        /// Retrieving server configuration. The server configuration is required before retrieving updates metadata.
        /// </summary>
        GetServerConfigStart,
        /// <summary>
        /// Server configuration retrieval is complete.
        /// </summary>
        GetServerConfigEnd,
        /// <summary>
        /// Getting the list of update IDs (or category IDs).
        /// </summary>
        GetRevisionIdsStart,
        /// <summary>
        /// The list of update IDs has been retrieved
        /// </summary>
        GetRevisionIdsEnd,
        /// <summary>
        /// Start getting metadata for all retrieved updated IDs
        /// </summary>
        GetUpdateMetadataStart,
        /// <summary>
        /// Progress while getting update metadata. Reports percent progress.
        /// </summary>
        GetUpdateMetadataProgress,
        /// <summary>
        /// Metadata retrieval is complete
        /// </summary>
        GetUpdateMetadataEnd
    }

    /// <summary>
    /// Provides progress data for a metadata query.
    /// </summary>
    public class MetadataQueryProgress : EventArgs
    {
        /// <summary>
        /// Percent done. Not all query stages support progress reporting.
        /// </summary>
        /// <value>
        /// Percent done value, in the [0,100] range.
        /// </value>
        public double PercentDone { get; internal set; }

        /// <summary>
        /// Number of work items in a stage. Reported only for stages that support percent done reporting. 
        /// </summary>
        /// <value>
        /// Number of work items (updates, etc.) to process
        /// </value>
        public int Maximum { get; internal set; }

        /// <summary>
        /// Number of work items processed. Reported only for stages that support percent done reporting.
        /// </summary>
        /// <value>
        /// Number of work items (updates, etc.) processed so far.</value>
        public int Current { get; internal set; }

        /// <summary>
        /// The current stage in the query.
        /// </summary>
        /// <value>One of the possible stages from <see cref="MetadataQueryStage"/></value>
        public MetadataQueryStage CurrentTask { get; internal set; }

        internal MetadataQueryProgress()
        {
            CurrentTask = MetadataQueryStage.Unknown;
        }
    }
}
