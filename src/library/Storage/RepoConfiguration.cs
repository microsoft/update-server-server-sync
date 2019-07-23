// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;
using System.IO;
using Microsoft.UpdateServices.Client;
using System.Runtime.Serialization;

namespace Microsoft.UpdateServices.Storage
{
    /// <summary>
    /// Represents configuration for an updates repository.
    /// </summary>
    public class RepoConfiguration
    {
        /// <summary>
        /// The address of the upstream server from which this repository was cloned
        /// </summary>
        /// <value>Upstream update server endpoint</value>
        [JsonProperty]
        public Endpoint UpstreamServerEndpoint { get; set; }

        /// <summary>
        /// The account name used when authenticating with the upstream server
        /// </summary>
        [JsonProperty]
        public string AccountName { get; set; }

        /// <summary>
        /// The account GUID used when authenticating with the upstream server
        /// </summary>
        [JsonProperty]
        public Guid? AccountGuid { get; set; }

        /// <summary>
        /// The version of this object. Used to compare against the current version when deserializing
        /// </summary>
        [JsonProperty]
        private int Version;

        /// <summary>
        /// The object version currently implemented by this code
        /// </summary>
        [JsonIgnore]
        const int CurrentVersion = 1;

        [JsonConstructor]
        private RepoConfiguration() { }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (AccountName == null)
            {
                AccountName = new Guid().ToString();
            }

            if (AccountGuid == null)
            {
                AccountGuid = new Guid();
            }
        }

        internal RepoConfiguration(Endpoint upstreamServer)
        {
            UpstreamServerEndpoint = upstreamServer;
            Version = CurrentVersion;

            AccountName = new Guid().ToString();
            AccountGuid = new Guid();
        }

        internal static RepoConfiguration ReadFromFile(string configFile)
        {
            if (File.Exists(configFile))
            {
                var config = JsonConvert.DeserializeObject<RepoConfiguration>(File.ReadAllText(configFile));
                if (config.Version != CurrentVersion)
                {
                    return null;
                }

                return config;
            }

            return null;
        }

        internal void SaveToFile(string configFile)
        {
            File.WriteAllText(configFile, JsonConvert.SerializeObject(this));
        }
    }
}