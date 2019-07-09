using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Microsoft.UpdateServices.Query;
using Microsoft.UpdateServices.Metadata.Content;

namespace Microsoft.UpdateServices.Storage
{
    /// <summary>
    /// Supported repository export formats
    /// </summary>
    public enum RepoExportFormat
    {
        /// <summary>
        /// Export format compatible with WSUS 2016
        /// </summary>
        WSUS_2016,
    }

    /// <summary>
    /// The <see cref="UpdateRetrievalMode"/> enumeration contains retrieval modes for updates in a local repository.
    /// <para>Because update extended attributes are large, they don't get loaded when a repository
    /// is opened. They can be retrieved by calling one of the GetUpdates method for a IRepository, like <see cref="IRepository.GetUpdate(Identity, UpdateRetrievalMode)"/> or <see cref="IRepository.GetUpdates(UpdateRetrievalMode)"/></para>
    /// </summary>
    public enum UpdateRetrievalMode
    {
        /// <summary>
        /// Basic metadata includes ID, title, description, superseded, classifications, products
        /// </summary>
        Basic,
        /// <summary>
        /// Extended metadata is update type specifyc and can include driver HW ID, KB article numbers, file information, pre-requisites etc.
        /// </summary>
        Extended
    }

    /// <summary>
    /// Interface to manage a repository that contains Microsoft updates.
    /// <para>A repository tracks an upstream update server.</para>
    /// </summary>
    public interface IRepository
    {
        /// <summary>
        /// Raised on progress for long running repository operations
        /// </summary>
        /// <value>
        /// Progress data.
        /// </value>
        event EventHandler<OperationProgress> RepositoryOperationProgress;

        /// <summary>
        /// Gets the configuration of the repository
        /// </summary>
        /// <value>Repository configuration</value>
        RepoConfiguration Configuration { get; }

        /// <summary>
        /// Merge new updates or categories into the repository
        /// </summary>
        /// <param name="queryResult">The query results to merge</param>
        void MergeQueryResult(QueryResult queryResult);

        /// <summary>
        /// Download content for an update
        /// </summary>
        /// <param name="update">The update to download content for</param>
        void DownloadUpdateContent(IUpdateWithFiles update);

        /// <summary>
        /// Delete the repository
        /// </summary>
        void Delete();

        /// <summary>
        /// Export selected updates from the repository, using the specified format
        /// </summary>
        /// <param name="filter">Filter which updates to export from the repository</param>
        /// <param name="exportFilePath">Export file path</param>
        /// <param name="format">Export file format</param>
        void Export(RepositoryFilter filter, string exportFilePath, RepoExportFormat format);

        /// <summary>
        /// Returns all categories that match the filter
        /// </summary>
        /// <param name="filter">Categories filter</param>
        /// <returns>List of categories that match the filter</returns>
        List<Update> GetCategories(RepositoryFilter filter);

        /// <summary>
        /// Returns all categories present in the repository
        /// </summary>
        /// <returns>List of categories: classifications, detectoids, products</returns>
        List<Update> GetCategories();

        /// <summary>
        /// Returns all updates that match the filter
        /// </summary>
        /// <param name="filter">Updates filter</param>
        /// <param name="metadataMode">Level of metadata to retrieve.</param>
        /// <returns>List of updates that match the filter</returns>
        List<Update> GetUpdates(RepositoryFilter filter, UpdateRetrievalMode metadataMode);

        /// <summary>
        /// Returns all updates present in the repository
        /// </summary>
        /// <param name="metadataMode">Level of metadata to retrieve.</param>
        /// <returns>List of updates</returns>
        List<Update> GetUpdates(UpdateRetrievalMode metadataMode);

        /// <summary>
        /// Get an update in the repository by ID
        /// </summary>
        /// <param name="updateId">The update ID to lookup</param>
        /// <param name="metadataMode">Level of metadata to retrieve.</param>
        /// <returns>The requested update</returns>
        Update GetUpdate(Identity updateId, UpdateRetrievalMode metadataMode);

        /// <summary>
        /// Gets the products index
        /// </summary>
        /// <value>List of products</value>
        IReadOnlyDictionary<Identity, Product> ProductsIndex { get; }

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
        /// Gets the categories index (products, classifications, detectoids)
        /// </summary>
        /// <value>List of categories</value>
        IReadOnlyDictionary<Identity, Update> CategoriesIndex{ get; }

        /// <summary>
        /// Gets the updates indexUpdates index
        /// </summary>
        /// <value>List of updates</value>
        IReadOnlyDictionary<Identity, Update> UpdatesIndex { get; }
    }
}
