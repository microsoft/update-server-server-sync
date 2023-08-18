// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;
using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Storage;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Source
{
    /// <summary>
    /// Retrieves all available categories from the Microsoft Update catalog.
    /// <para>
    /// Categories consist of Detectoids, Products and Classifications.
    /// </para>
    /// <para>
    /// Products and classifications are used to categorize updates; they are useful as filters for selectively
    /// sync'ing updates from an upstream server.
    /// </para>
    /// </summary>
    public class UpstreamCategoriesSource : IMetadataSource
    {
        private readonly UpstreamServerClient _Client;

        private List<MicrosoftUpdatePackageIdentity> _Identities;

        /// <summary>
        /// Progress indicator during metadata copy operations
        /// </summary>
        public event EventHandler<PackageStoreEventArgs> MetadataCopyProgress;

#pragma warning disable 0067
        /// <summary>
        /// Progress indicator during source open operations. Not used by UpstreamCategoriesSource.
        /// </summary>
        public event EventHandler<PackageStoreEventArgs> OpenProgress;

        /// <summary>
        /// Create a new MicrosoftUpdate package source that retrieves updates from the specified endpoint
        /// </summary>
        /// <param name="upstreamEndpoint">Endpoint to get updates from</param>
        public UpstreamCategoriesSource(Endpoint upstreamEndpoint)
        {
            _Client = new UpstreamServerClient(upstreamEndpoint);
        }

        private void RetrievePackageIdentities()
        {
            lock(this)
            {
                if (_Identities == null)
                {
                    _Identities = _Client.GetCategoryIds(out var _).ToList();
                    _Identities.Sort();
                }
            }
        }

        /// <summary>
        /// Breaks down a flat list of objects in a list of batches, each batch having a maximum allowed size
        /// </summary>
        /// <typeparam name="T">The type of objects to batch</typeparam>
        /// <param name="flatList">The flat list of objects to break down</param>
        /// <param name="maxBatchSize">The maximum size of a batch</param>
        /// <returns>The batched list</returns>
        private static List<List<T>> CreateBatchedListFromFlatList<T>(List<T> flatList, int maxBatchSize)
        {
            // Figure out how many batches we have
            var batchCount = flatList.Count / maxBatchSize;
            // One more batch for the remaininig objects, if any
            batchCount += flatList.Count % maxBatchSize == 0 ? 0 : 1;

            List<List<T>> batches = new(batchCount);
            for (int i = 0; i < batchCount; i++)
            {
                var batchSize = maxBatchSize;
                // If this is the last batch, the size might not be the max allowed size but the remainder of elements
                if (i == batchCount - 1 && flatList.Count % maxBatchSize != 0)
                {
                    batchSize = flatList.Count % maxBatchSize;
                }

                // Add the new batch to the batches list
                batches.Add(flatList.GetRange(i * maxBatchSize, batchSize));
            }

            return batches;
        }

        /// <summary>
        /// Retrieves categories from the upstream source
        /// </summary>
        /// <param name="cancelToken">Cancellation token</param>
        /// <returns>List of Microsoft Update categories</returns>
        public List<MicrosoftUpdatePackage> GetCategories(CancellationToken cancelToken)
        {
            var categoriesList = new List<MicrosoftUpdatePackage>();

            RetrievePackageIdentities();

            var batches = CreateBatchedListFromFlatList(_Identities, 50);

            var progressArgs = new PackageStoreEventArgs() { Total = _Identities.Count, Current = 0 };
            batches.ForEach(batch =>
            {
                if (cancelToken.IsCancellationRequested)
                {
                    return;
                }

                var retrievedBatch = _Client.GetUpdateDataForIds(batch.ToList());

                lock (categoriesList)
                {
                    categoriesList.AddRange(retrievedBatch);
                }

                lock (progressArgs)
                {
                    progressArgs.Current += retrievedBatch.Count;
                    MetadataCopyProgress?.Invoke(this, progressArgs);
                }
            });

            return categoriesList;
        }

        /// <inheritdoc cref="IMetadataSource.CopyTo(IMetadataSink, CancellationToken)"/>
        public void CopyTo(IMetadataSink destination, CancellationToken cancelToken)
        {
            RetrievePackageIdentities();

            List<MicrosoftUpdatePackageIdentity> unavailableUpdates;

            if (destination is IMetadataStore baseline)
            {
                unavailableUpdates = new List<MicrosoftUpdatePackageIdentity>();
                unavailableUpdates = _Identities.Where(id => !baseline.ContainsPackage(id)).ToList();
            }
            else
            {
                unavailableUpdates = _Identities;
            }

            if (unavailableUpdates.Count > 0)
            {
                var batches = CreateBatchedListFromFlatList(unavailableUpdates, 50);

                var progressArgs = new PackageStoreEventArgs() { Total = unavailableUpdates.Count, Current = 0 };
                batches.AsParallel().ForAll(batch =>
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var retrievedPackages = _Client.GetUpdateDataForIds(batch.ToList());
                    destination.AddPackages(retrievedPackages);

                    lock(progressArgs)
                    {
                        progressArgs.Current += retrievedPackages.Count;
                        MetadataCopyProgress?.Invoke(this, progressArgs);
                    }
                });
            }
            else
            {
                MetadataCopyProgress?.Invoke(this, new PackageStoreEventArgs());
            }
        }

        /// <summary>
        /// Filtered copy not implemented for the categories source as categories cannot be filtered when
        /// sync'ing from an upstream server.
        /// </summary>
        /// <param name="destination">Destination store for the retrieved metadata</param>
        /// <param name="filter">Filter to apply during the copy operation</param>
        /// <param name="cancelToken">Cancellation token</param>
        /// <exception cref="NotImplementedException"></exception>
        public void CopyTo(IMetadataSink destination, IMetadataFilter filter, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented for an upstream categories source
        /// </summary>
        /// <param name="packageIdentity">Identity of the category to retrieve</param>
        /// <returns>Category metadata as stream</returns>
        /// <exception cref="NotImplementedException"></exception>
        public Stream GetMetadata(IPackageIdentity packageIdentity)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented for an upstream categories source
        /// </summary>
        /// <param name="packageIdentity">Indentity of category to lookup</param>
        /// <returns>True if found, false otherwise</returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool ContainsMetadata(IPackageIdentity packageIdentity)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented for an upstream update source. Also, do not contain files.
        /// </summary>
        /// <typeparam name="T">Type of file to retrieve.</typeparam>
        /// <param name="packageIdentity">Identity of the category to retrieve files for.</param>
        /// <returns>List of files in the category</returns>
        /// <exception cref="NotImplementedException"></exception>
        public List<T> GetFiles<T>(IPackageIdentity packageIdentity)
        {
            throw new NotImplementedException();
        }
    }
}
