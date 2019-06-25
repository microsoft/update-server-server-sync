// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.LocalCache;
using Microsoft.UpdateServices.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.ServiceModel;

namespace Microsoft.UpdateServices.Tests
{
    [TestClass]
    public class LocalRepositoryTests
    {
        [TestInitialize]
        public void Initialize()
        {
            Repository.Delete(Environment.CurrentDirectory);
        }

        /// <summary>
        /// Test saving results (updates) into a repository
        /// </summary>
        [TestMethod]
        public void CategoriesAndUpdatesCachingTest()
        {
            var upstreamServerClient = new UpstreamServerClient(Endpoint.Default);
            var categories = upstreamServerClient.GetCategories().GetAwaiter().GetResult();

            var localRepository = Repository.FromDirectory(Environment.CurrentDirectory);

            // Insert categories into store
            localRepository.MergeQueryResult(categories);

            // Create a filter with the first product and all classifications
            var filter = new QueryFilter(localRepository.Categories.Products.Take(1), localRepository.Categories.Classifications);

            // Get updates
            
            var updates = upstreamServerClient.GetUpdates(filter).GetAwaiter().GetResult();
            
            // Ingest updates into store
            localRepository.MergeQueryResult(updates);

            // Reload the store
            var reloadedRepository = Repository.FromDirectory(Environment.CurrentDirectory);

            Assert.IsTrue(reloadedRepository.Categories.Categories.Count == localRepository.Categories.Categories.Count);
            Assert.IsTrue(reloadedRepository.Updates.Updates.Count == localRepository.Updates.Updates.Count);
        }

        /// <summary>
        /// Test caching access tokens and service configuration
        /// </summary>
        [TestMethod]
        public void AuthenticationAndConfigCaching()
        {
            var upstreamServerClient = new UpstreamServerClient(Endpoint.Default);
            var categories = upstreamServerClient.GetCategories().GetAwaiter().GetResult();

            var localRepository = Repository.FromDirectory(Environment.CurrentDirectory);
            localRepository.CacheAccessToken(upstreamServerClient.AccessToken);
            localRepository.CacheServiceConfiguration(upstreamServerClient.ConfigData);

            // Insert categories into store
            localRepository.MergeQueryResult(categories);


            // Reload the store
            var reloadedRepository = Repository.FromDirectory(Environment.CurrentDirectory);

            var cachedToken = reloadedRepository.GetAccessToken();
            Assert.IsNotNull(cachedToken);

            var serviceConfig = reloadedRepository.GetServiceConfiguration();
            Assert.IsNotNull(serviceConfig);

            var upstreamServerClientWithCache = new UpstreamServerClient(Endpoint.Default, serviceConfig, cachedToken);
            var categories1 = upstreamServerClientWithCache.GetCategories().GetAwaiter().GetResult();

            Assert.IsNotNull(categories1);
            Assert.IsTrue(categories.Updates.Count == categories1.Updates.Count);
        }

    }
}
