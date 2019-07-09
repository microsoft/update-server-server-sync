// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.ServiceModel;

namespace Microsoft.UpdateServices.Tests
{
    [TestClass]
    public class AuthenticationTests
    {
        /// <summary>
        /// Test authentication with an invalid URL
        /// </summary>
        [TestMethod]
        public void AuthenticateInvalidUrl()
        {
            var authenticator = new ClientAuthenticator(new Endpoint("https://bad.url"));
            Assert.ThrowsException<EndpointNotFoundException>(() => authenticator.Authenticate().GetAwaiter().GetResult());
        }

        /// <summary>
        /// Test authentication for the default Microsoft update server
        /// </summary>
        [TestMethod]
        public void AuthenticateDefaultUrl()
        {
            var authenticator = new ClientAuthenticator();
            var token = authenticator.Authenticate().GetAwaiter().GetResult();

            // Ensure the token is valid and not expired
            Assert.IsNotNull(token);
            Assert.IsFalse(token.Expired);
        }

        /// <summary>
        /// Test authentication for a custom WSUS server
        /// </summary>
        [TestMethod]
        public void AuthenticateCustomUrl()
        {
            // Set the endpoint parameter. It is actually the same default MSFT endpoint
            var authenticator = new ClientAuthenticator(Endpoint.Default);
            var token = authenticator.Authenticate().GetAwaiter().GetResult();

            // Ensure the token is valid and not expired
            Assert.IsNotNull(token);
            Assert.IsFalse(token.Expired);
        }

        /// <summary>
        /// Test authentication and then re-authentication using the old token
        /// </summary>
        [TestMethod]
        public void ReuseServiceAccessToken()
        {
            var authenticator = new ClientAuthenticator();

            // Retrieve a token
            var token = authenticator.Authenticate().GetAwaiter().GetResult();

            // Ensure the token is valid and not expired
            Assert.IsNotNull(token);
            Assert.IsFalse(token.Expired);

            var reAuthToken = authenticator.Authenticate(token).GetAwaiter().GetResult();
            // Ensure the token is valid and not expired
            Assert.IsNotNull(reAuthToken);
            Assert.IsFalse(reAuthToken.Expired);
        }

        /// <summary>
        /// Test serialization and deserialization of the access token
        /// </summary>
        [TestMethod]
        public void SerializeServiceAccessToken()
        {
            var authenticator = new ClientAuthenticator();

            // Retrieve a token
            var token = authenticator.Authenticate().GetAwaiter().GetResult();

            // Ensure the token is valid and not expired
            Assert.IsNotNull(token);
            Assert.IsFalse(token.Expired);

            // Serialize the token
            var json = token.ToJson();

            // Deserialize the token and check that it's still valid
            var deserializedToken = ServiceAccessToken.FromJson(json);
            Assert.IsNotNull(deserializedToken);
            Assert.IsFalse(deserializedToken.Expired);

            // Use the deserialized token to re-authenticate
            var reAuthToken = authenticator.Authenticate(token).GetAwaiter().GetResult();
            Assert.IsNotNull(reAuthToken);
            Assert.IsFalse(reAuthToken.Expired);
        }
    }
}
