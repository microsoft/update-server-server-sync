// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Storage;
using Microsoft.UpdateServices.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.ServiceModel;
using Microsoft.UpdateServices.Client;

namespace Microsoft.UpdateServices.Tests
{
    [TestClass]
    public class LocalRepositoryTests
    {
        [TestInitialize]
        public void Initialize()
        {
            FileSystemRepository.Delete(Environment.CurrentDirectory);
        }

        /// <summary>
        /// Test saving results (updates) into a repository
        /// </summary>
        [TestMethod]
        public void CategoriesAndUpdatesCachingTest()
        {
            var upstreamServerClient = new UpstreamServerClient(Endpoint.Default);
            var categories = upstreamServerClient.GetCategories().GetAwaiter().GetResult();

            var localRepository = FileSystemRepository.Init(Environment.CurrentDirectory, Endpoint.Default.URI);

            // Insert categories into store
            localRepository.MergeQueryResult(categories);

            // Create a filter with the first product and all classifications
            var filter = new QueryFilter(localRepository.ProductsIndex.Values.Take(1), localRepository.ClassificationsIndex.Values);

            // Get updates
            
            var updates = upstreamServerClient.GetUpdates(filter).GetAwaiter().GetResult();
            
            // Ingest updates into store
            localRepository.MergeQueryResult(updates);

            // Reload the store
            var reloadedRepository = FileSystemRepository.Open(Environment.CurrentDirectory);

            Assert.IsTrue(reloadedRepository.CategoriesIndex.Count == localRepository.CategoriesIndex.Count);
            Assert.IsTrue(reloadedRepository.UpdatesIndex.Count == localRepository.UpdatesIndex.Count);
        }

        /// <summary>
        /// Test caching access tokens and service configuration
        /// </summary>
        [TestMethod]
        public void AuthenticationAndConfigCaching()
        {
            var localRepository = FileSystemRepository.Init(Environment.CurrentDirectory, Endpoint.Default.URI);
            var upstreamServerClient = new UpstreamServerClient(localRepository);
            var categories = upstreamServerClient.GetCategories().GetAwaiter().GetResult();

            // Insert categories into store
            localRepository.MergeQueryResult(categories);

            // Reload the store
            var reloadedRepository = FileSystemRepository.Open(Environment.CurrentDirectory);

            var upstreamServerClientWithCache = new UpstreamServerClient(localRepository);
            var categories1 = upstreamServerClientWithCache.GetCategories().GetAwaiter().GetResult();

            Assert.IsNotNull(categories1);
            Assert.IsTrue(categories1.Updates.Count == 0);
        }

    }
}
