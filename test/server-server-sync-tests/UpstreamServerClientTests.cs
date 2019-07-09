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
    public class UpstreamServerClientTests
    {
        bool QueryProgressCalled = false;

        [TestInitialize]
        public void Initialize()
        {
            FileSystemRepository.Delete(Environment.CurrentDirectory);
        }

        /// <summary>
        /// Test categories query with an invalid URL
        /// </summary>
        [TestMethod]
        public void QueryCategoriesBadUrl()
        {
            var upstreamServerClient = new UpstreamServerClient(new Endpoint("https://bad.url"));
            Assert.ThrowsException<EndpointNotFoundException>(() => upstreamServerClient.GetCategories().GetAwaiter().GetResult());
        }

        /// <summary>
        /// Test quering for categories from default Microsoft update server
        /// </summary>
        [TestMethod]
        public void QueryCategoriesNoCache()
        {
            var upstreamServerClient = new UpstreamServerClient(Endpoint.Default);
            upstreamServerClient.MetadataQueryProgress += UpstreamServerClient_MetadataQueryProgress;
            var categories = upstreamServerClient.GetCategories().GetAwaiter().GetResult();

            Assert.IsTrue(QueryProgressCalled);
            Assert.IsTrue(categories != null);
            Assert.IsTrue(categories.Updates != null && categories.Updates.Count > 0);
        }

        /// <summary>
        /// Test quering for updates from default Microsoft update server
        /// </summary>
        [TestMethod]
        public void QueryUpdates()
        {
            var upstreamServerClient = new UpstreamServerClient(Endpoint.Default);

            // Get categories; we need them to create a filter
            var categories = upstreamServerClient.GetCategories().GetAwaiter().GetResult();

            // Ingest categories into a new local repository
            IRepository localRepository = FileSystemRepository.Init(Environment.CurrentDirectory, Endpoint.Default.URI);
            localRepository.MergeQueryResult(categories);

            // Create a filter with the first product and all classifications
            var filter = new QueryFilter(localRepository.ProductsIndex.Values.Take(1), localRepository.ClassificationsIndex.Values);

            // Get updates
            upstreamServerClient.MetadataQueryProgress += UpstreamServerClient_MetadataQueryProgress;
            var updates = upstreamServerClient.GetUpdates(filter).GetAwaiter().GetResult();
            Assert.IsTrue(QueryProgressCalled);
            Assert.IsNotNull(updates);

            // Ingest updates into store and try a cached query
            localRepository.MergeQueryResult(updates);

            var cachedupstreamServerClient = new UpstreamServerClient(localRepository);
            var refreshedUpdates = cachedupstreamServerClient.GetUpdates(filter).GetAwaiter().GetResult();
            Assert.IsNotNull(refreshedUpdates);
            // Practically, no updates should have changes since the previous query (it could happen though)
            Assert.IsTrue(refreshedUpdates.Updates.Count == 0);
        }

        /// <summary>
        /// Test authentication for the default Microsoft update server
        /// </summary>
        [TestMethod]
        public void QueryCategoriesWithCache()
        {
            var upstreamServerClient = new UpstreamServerClient(Endpoint.Default);
            upstreamServerClient.MetadataQueryProgress += UpstreamServerClient_MetadataQueryProgress;
            var categories = upstreamServerClient.GetCategories().GetAwaiter().GetResult();

            Assert.IsTrue(QueryProgressCalled);
            Assert.IsTrue(categories != null);
            Assert.IsTrue(categories.Updates != null && categories.Updates.Count > 0);

            var localRepository = FileSystemRepository.Init(Environment.CurrentDirectory, Endpoint.Default.URI);
            localRepository.MergeQueryResult(categories);

            Assert.IsTrue(localRepository.GetCategories().Count() == categories.Updates.Count);

            var cachedUpstreamServerClient = new UpstreamServerClient(localRepository);
            var refreshedCategories = cachedUpstreamServerClient.GetCategories().GetAwaiter().GetResult();

            Assert.IsTrue(QueryProgressCalled);
            Assert.IsTrue(refreshedCategories != null);

            // Merge the new query results; 
            localRepository.MergeQueryResult(refreshedCategories);

            Assert.IsTrue(localRepository.GetCategories().Count == categories.Updates.Count);

            // Check that the 2 lists match
            // It's possible that the server changed categories between the 2 calls, but unlikely
            foreach(var cachedCategory in categories.Updates)
            {
                Assert.IsTrue(localRepository.ClassificationsIndex.ContainsKey(cachedCategory.Identity) ||
                    localRepository.ProductsIndex.ContainsKey(cachedCategory.Identity) ||
                    localRepository.DetectoidsIndex.ContainsKey(cachedCategory.Identity));
            }
        }

        private void UpstreamServerClient_MetadataQueryProgress(object sender, MetadataQueryProgress e)
        {
            QueryProgressCalled = true;
        }
    }
}
