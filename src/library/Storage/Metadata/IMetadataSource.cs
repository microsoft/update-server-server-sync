// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.Client;
using Microsoft.UpdateServices.Metadata.Prerequisites;
using Microsoft.UpdateServices.WebServices.ServerSync;

namespace Microsoft.UpdateServices.Storage
{
    /// <summary>
    /// Provides access to update metadata as well as indexes for fast queries on update metadata.
    /// IMetadataSource is obtained from the <see cref="UpstreamServerClient"/> when retrieving categories or updates.
    /// </summary>
    public interface IMetadataSource
    {
        /// <summary>
        /// Get the upstream server that this metadata source was created from
        /// </summary>
        Endpoint UpstreamSource { get; }

        /// <summary>
        /// List of filters applied to updates when added to this metadata source
        /// </summary>
        IReadOnlyList<QueryFilter> Filters { get; }

        /// <summary>
        /// Returns the anchor received after the last updates query that used the specified filter
        /// </summary>
        /// <param name="filter">The filter used in the query</param>
        /// <returns>Anchor string</returns>
        string GetAnchorForFilter(QueryFilter filter);

        /// <summary>
        /// The upstream server anchor associated with categories stored in a metadata collection
        /// </summary>
        string CategoriesAnchor { get; }

        /// <summary>
        /// Returns all categories present in the metadata store
        /// </summary>
        /// <returns>List of categories: classifications, detectoids, products</returns>
        ICollection<Update> GetCategories();

        /// <summary>
        /// Returns all categories that match the filter
        /// </summary>
        /// <param name="filter">Categories filter</param>
        /// <returns>List of categories that match the filter</returns>
        List<Update> GetCategories(MetadataFilter filter);

        /// <summary>
        /// Returns all updates that match the filter
        /// </summary>
        /// <param name="filter">Updates filter</param>
        /// <returns>List of updates that match the filter</returns>
        List<Update> GetUpdates(MetadataFilter filter);

        /// <summary>
        /// Returns all updates 
        /// </summary>
        /// <returns>List of updates</returns>
        ICollection<Update> GetUpdates();

        /// <summary>
        /// Get an update by ID
        /// </summary>
        /// <param name="updateId">The update ID to lookup</param>
        /// <returns>The requested update</returns>
        Update GetUpdate(Identity updateId);

        /// <summary>
        /// Gets the updates (software, drivers) index
        /// </summary>
        /// <value>List of products</value>
        IReadOnlyDictionary<Identity, Update> UpdatesIndex { get; }

        /// <summary>
        /// Gets the categories index (products, classifications, detectoids)
        /// </summary>
        /// <value>List of categories</value>
        IReadOnlyDictionary<Identity, Update> CategoriesIndex { get; }

        /// <summary>
        /// Gets the classifications index
        /// </summary>
        /// <value>List of classifications</value>
        IReadOnlyDictionary<Identity, Classification> ClassificationsIndex { get; }

        /// <summary>
        /// Gets the detectoids index
        /// </summary>
        /// <value>List of detectoids</value>
        IReadOnlyDictionary<Identity, Detectoid> DetectoidsIndex { get; }

        /// <summary>
        /// Gets the products index
        /// </summary>
        /// <value>List of updates</value>
        IReadOnlyDictionary<Identity, Product> ProductsIndex { get; }

        /// <summary>
        /// Gets a stream over an update's XML data
        /// </summary>
        /// <param name="updateIdentity">The update ID to get the XML metadata stream for</param>
        /// <returns>Metadata stream</returns>
        Stream GetUpdateMetadataStream(Identity updateIdentity);

        /// <summary>
        /// Gets an updates's product IDs
        /// </summary>
        /// <param name="updateIdentity">The update ID to get products Ids for</param>
        /// <returns>List of product ids for an update</returns>
        List<Guid> GetUpdateProductIds(Identity updateIdentity);

        /// <summary>
        /// Gets an updates's classification IDs
        /// </summary>
        /// <param name="updateIdentity">The update ID to get classification Ids for</param>
        /// <returns>List of classification ids for an update</returns>
        List<Guid> GetUpdateClassificationIds(Identity updateIdentity);

        /// <summary>
        /// The checksum of the updates in the metadata source.
        /// </summary>
        string Checksum { get; }

        /// <summary>
        /// Set the credentials used to connec to to the upstream server
        /// </summary>
        /// <param name="accountName">Account name</param>
        /// <param name="AccountGuid">Account GUID</param>
        void SetUpstreamCredentials(string accountName, Guid AccountGuid);

        /// <summary>
        /// The account name used when updates were added to this metadata source
        /// </summary>
        string UpstreamAccountName { get; }

        /// <summary>
        /// The account GUID used when updates were added to this metadata source
        /// </summary>
        Guid UpstreamAccountGuid { get; }

        /// <summary>
        /// Retrieves the title of an update
        /// </summary>
        /// <param name="updateIdentity"></param>
        /// <returns></returns>
        string GetUpdateTitle(Identity updateIdentity);

        /// <summary>
        /// Checks if an update is a bundle (contains other updates)
        /// </summary>
        /// <param name="updateIdentity">The update to check</param>
        /// <returns>True if the update contains other updates, false otherwise</returns>
        bool IsBundle(Identity updateIdentity);

        /// <summary>
        /// Checks if an update is a bundle (contains other updates)
        /// </summary>
        /// <param name="updateIdentity">The update to check</param>
        /// <returns>True if the update contains other updates, false otherwise</returns>
        bool IsBundled(Identity updateIdentity);

        /// <summary>
        /// Gets the list of updates that are bundled withing the specified update
        /// </summary>
        /// <param name="updateIdentity">The update to get bundled updates for</param>
        /// <returns>List of bundled updates</returns>
        IEnumerable<Identity> GetBundledUpdates(Identity updateIdentity);

        /// <summary>
        /// Gets the bundle update to which this update belongs to
        /// </summary>
        /// <param name="updateIdentity">The update whose parent bundle to get</param>
        /// <returns>List of bundle updates that contain this update</returns>
        IEnumerable<Identity> GetBundle(Identity updateIdentity);

        /// <summary>
        /// Check if an update has prerequisites
        /// </summary>
        /// <param name="updateIdentity">The update to check prerequisites for</param>
        /// <returns>True if an update has prerequisites, false otherwise</returns>
        bool HasPrerequisites(Identity updateIdentity);

        /// <summary>
        /// Gets the list of prerequisites for an update
        /// </summary>
        /// <param name="updateIdentity">The update to get prerequisites for</param>
        /// <returns>List of prerequisites</returns>
        List<Prerequisite> GetPrerequisites(Identity updateIdentity);

        /// <summary>
        /// Check if this update has a parent product
        /// </summary>
        /// <param name="updateIdentity">The update to check</param>
        /// <returns>True if the update has a parent product, false otherwise</returns>
        bool HasProduct(Identity updateIdentity);


        /// <summary>
        /// Check if this update has a classification
        /// </summary>
        /// <param name="updateIdentity">The update to check classifications</param>
        /// <returns>True if the update has classifications, false otherwise</returns>
        bool HasClassification(Identity updateIdentity);

        /// <summary>
        /// Retrieves url information for a file
        /// </summary>
        /// <param name="checksum">The file checksum</param>
        /// <returns>Update URL information</returns>
        UpdateFileUrl GetFile(string checksum);

        /// <summary>
        /// Checks if the metadata source contains URL information for a file identified by its content checksum
        /// </summary>
        /// <param name="checksum">The file contents checksum</param>
        /// <returns>True if the store contains file URL information, false otherwise</returns>
        bool HasFile(string checksum);

        /// <summary>
        /// Checks if an update contains files
        /// </summary>
        /// <param name="updateIdentity">Update identity</param>
        /// <returns>True if the update contains files, false otherwise</returns>
        bool HasFiles(Identity updateIdentity);

        /// <summary>
        /// Retrieves files for an update
        /// </summary>
        /// <param name="updateIdentity">Update identity</param>
        /// <returns>List of files in the update</returns>
        List<UpdateFile> GetFiles(Identity updateIdentity);

        /// <summary>
        /// Checks if an update superseds other updates
        /// </summary>
        /// <param name="updateIdentity">The update to check if it superseds other updates</param>
        /// <returns>True if the update superseds other updates, false otherwise</returns>
        bool IsSuperseding(Identity updateIdentity);

        /// <summary>
        /// Checks if an update has been superseded
        /// </summary>
        /// <param name="updateIdentity">Update identity to check if superseded</param>
        /// <returns>false if not superseded, true otherwise</returns>
        bool IsSuperseded(Identity updateIdentity);

        /// <summary>
        /// Gets the update that superseded the update specified
        /// </summary>
        /// <param name="updateIdentity">Update identity to check if superseded</param>
        /// <returns>The update that superseded the update specified</returns>
        Identity GetSupersedingUpdate(Identity updateIdentity);

        /// <summary>
        /// Gets the list of updates superseded by the specified update
        /// </summary>
        /// <param name="updateIdentity">The update to get list of superseded updates for</param>
        /// <returns>List of updates superseded by the specified update</returns>
        IReadOnlyList<Guid> GetSupersededUpdates(Identity updateIdentity);

        /// <summary>
        /// Gets updates that have prerequisites and no other update depends on them
        /// </summary>
        /// <returns>List of GUIDS of leaf updates</returns>
        IEnumerable<Guid> GetLeafUpdates();

        /// <summary>
        /// Gets updates that have prerequisites and have other updates depende on them
        /// </summary>
        /// <returns>List of GUIDS of non leaf updates</returns>
        IEnumerable<Guid> GetNonLeafUpdates();

        /// <summary>
        /// Get updates with no prerequisites
        /// </summary>
        /// <returns>List of GUIDS of root updates</returns>
        IEnumerable<Guid> GetRootUpdates();

        /// <summary>
        /// Gets the int based index of all update identities in the metadata source
        /// </summary>
        /// <returns>Dictionary of int to Identity</returns>
        IReadOnlyDictionary<int, Identity> GetIndex();

        /// <summary>
        /// Exports the selected updates from the metadata source
        /// </summary>
        /// <param name="filter">Export filter</param>
        /// <param name="exportFile">Export file path</param>
        /// <param name="format">Export format</param>
        /// <param name="serverConfiguration">Server configuration.</param>
        /// <returns>List of categories that match the filter</returns>
        void Export(MetadataFilter filter, string exportFile, RepoExportFormat format, ServerSyncConfigData serverConfiguration);
    }

    /// <summary>
    /// Formats for exporting from a metadata source
    /// </summary>
    public enum RepoExportFormat
    {
        /// <summary>
        /// Export to WSUS 2016 compatible format
        /// </summary>
        WSUS_2016
    }
}
