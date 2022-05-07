// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;
using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Source
{
    /// <summary>
    /// Retrieves updates from the Microsoft Update catalog or a WSUS upstream server.
    /// </summary>
    public class UpstreamUpdatesSource : IMetadataSource
    {
        private readonly UpstreamServerClient _Client;
        private UpstreamSourceFilter _Filter;

        private List<MicrosoftUpdatePackageIdentity> _Identities;

        /// <summary>
        /// Progress indicator during metadata copy operations
        /// </summary>
        public event EventHandler<PackageStoreEventArgs> MetadataCopyProgress;

#pragma warning disable 0067
        /// <summary>
        /// Progress indicator during source open operations. Not used by UpstreamUpdatesSource.
        /// </summary>
        public event EventHandler<PackageStoreEventArgs> OpenProgress;

        /// <summary>
        /// Create a new MicrosoftUpdate package source that retrieves updates from the specified endpoint
        /// </summary>
        /// <param name="upstreamEndpoint">Endpoint to get updates from</param>
        /// <param name="filter">Filter to apply when retrieving updates from this source.</param>
        public UpstreamUpdatesSource(Endpoint upstreamEndpoint, UpstreamSourceFilter filter)
        {
            _Client = new UpstreamServerClient(upstreamEndpoint);
            _Filter = filter;
        }

        private void RetrievePackageIdentities()
        {
            lock (this)
            {
                if (_Identities == null)
                {
                    _Identities = _Client.GetUpdateIds(_Filter, out var _).ToList();
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
                    progressArgs.Current++;
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

            if (destination is IMetadataStore destinationBaseline)
            {
                 unavailableUpdates = _Identities.Where(u => !destinationBaseline.ContainsPackage(u)).ToList();
            }
            else
            {
                unavailableUpdates = _Identities;
            }

            if (unavailableUpdates.Count > 0)
            {
                var progressArgs = new PackageStoreEventArgs() { Total = unavailableUpdates.Count, Current = 0 };
                var batches = CreateBatchedListFromFlatList(unavailableUpdates, 50);
                
                MetadataCopyProgress?.Invoke(this, progressArgs);
                batches.AsParallel().ForAll(batch =>
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var retrievedPackages = _Client.GetUpdateDataForIds(batch.ToList());
                    destination.AddPackages(retrievedPackages);
                    retrievedPackages.ForEach(u => u.ReleaseMetadataBytes());

                    lock(progressArgs)
                    {
                        progressArgs.Current += retrievedPackages.Count;
                        MetadataCopyProgress?.Invoke(this, progressArgs);
                    }                    
                });
            }
        }

        /// <inheritdoc cref="IMetadataSource.CopyTo(IMetadataSink, IMetadataFilter, CancellationToken)"/>
        public void CopyTo(IMetadataSink destination, IMetadataFilter filter, CancellationToken cancelToken)
        {
            if (filter is UpstreamSourceFilter categoriesFilter)
            {
                _Filter = categoriesFilter;
            }

            CopyTo(destination, cancelToken);
        }

        /// <summary>
        /// Not implemented for an upstream update source
        /// </summary>
        /// <param name="packageIdentity">Identity of update to retrieve</param>
        /// <returns>Update metadata as stream</returns>
        /// <exception cref="NotImplementedException"></exception>
        public Stream GetMetadata(IPackageIdentity packageIdentity)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented for an upstream update source
        /// </summary>
        /// <param name="packageIdentity">Indentity of package to lookup</param>
        /// <returns>True if found, false otherwise</returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool ContainsMetadata(IPackageIdentity packageIdentity)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented for an upstream update source
        /// </summary>
        /// <typeparam name="T">Type of file to retrieve.</typeparam>
        /// <param name="packageIdentity">Identity of the package to retrieve files for.</param>
        /// <returns>List of files in the package</returns>
        /// <exception cref="NotImplementedException"></exception>
        public List<T> GetFiles<T>(IPackageIdentity packageIdentity)
        {
            throw new NotImplementedException();
        }
    }
}
