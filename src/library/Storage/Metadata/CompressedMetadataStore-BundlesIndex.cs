// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ICSharpCode.SharpZipLib.Zip;
using Microsoft.UpdateServices.Client;
using Microsoft.UpdateServices.Compression;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.Metadata.Prerequisites;
using Microsoft.UpdateServices.Storage;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Storage
{
    public partial class CompressedMetadataStore
    {
        private Dictionary<int, List<int>> BundlesIndex { get; set; }

        private const string BundlesIndexEntryName = "bundles.json";

        // Bundled updates that have not been added to the metadata source yet
        // When the metadata sink is commited, this list should be empty
        private Dictionary<Identity, List<int>> PendingBundledUpdates = new Dictionary<Identity, List<int>>();

        private Dictionary<int, List<int>> IsBundledTable { get; set; }

        private void AddUpdateBundleInformation(int newUpdateIndex, Identity newUpdateIdentity, XDocument updateXml)
        {
            var bundledUpdates = BundlesUpdatesParser.Parse(updateXml);

            if (PendingBundledUpdates.ContainsKey(newUpdateIdentity))
            {
                // We've seen this update before, as bundled with other updates
                // Now that we have an update for it, adds its bundling information
                IsBundledTable.Add(newUpdateIndex, PendingBundledUpdates[newUpdateIdentity]);
                foreach (var newUpdateParentBundleIndex in PendingBundledUpdates[newUpdateIdentity])
                {
                    // A bundled update that was pending before was added; add it to the parent's list of bundled updates
                    if (!BundlesIndex[newUpdateParentBundleIndex].Contains(newUpdateIndex))
                    {
                        BundlesIndex[newUpdateParentBundleIndex].Add(newUpdateIndex);
                    }
                }

                // Remove from list of pending updates;
                PendingBundledUpdates.Remove(newUpdateIdentity);
            }
            else
            {
                // When initially added, updates are not considered bundled
                // Updates become bundled when they are found within another update
                IsBundledTable.Add(newUpdateIndex, new List<int>());
            }

            if (bundledUpdates.Count > 0)
            {
                var knownBundledUpdates = bundledUpdates.Where(u => Identities.Contains(u)).Select(id => this[id]);
                var unknownBundledUpdates = bundledUpdates.Where(u => !Identities.Contains(u));

                // Add known bundled updates
                BundlesIndex.Add(
                    newUpdateIndex,
                    new List<int>(knownBundledUpdates));

                // Mark all bundled updates as bundled
                foreach (var bundledUpdate in knownBundledUpdates)
                {
                    // Try to mark the bundled update as bundled by adding it to the bundled table
                    if (IsBundledTable.ContainsKey(bundledUpdate))
                    {
                        // An entry already exists; switch it to true now, regardless of the previous value
                        if (!IsBundledTable[bundledUpdate].Contains(newUpdateIndex))
                        {
                            IsBundledTable[bundledUpdate].Add(newUpdateIndex);
                        }
                    }
                    else
                    {
                        IsBundledTable.Add(bundledUpdate, new List<int>() { newUpdateIndex });
                    }
                }

                // Add unknown bundled updates to a pending list
                foreach (var bundledUpdate in unknownBundledUpdates)
                {
                    // The bundled update was not added to the metadata collection yet. Put it on a pending list with a mapping to its parent bundle
                    if (PendingBundledUpdates.ContainsKey(bundledUpdate))
                    {
                        if (!PendingBundledUpdates[bundledUpdate].Contains(newUpdateIndex))
                        {
                            PendingBundledUpdates[bundledUpdate].Add(newUpdateIndex);
                        }
                    }
                    else
                    {
                        PendingBundledUpdates.TryAdd(bundledUpdate, new List<int>() { newUpdateIndex});
                    }
                }
            }
        }

        /// <summary>
        /// Saves the bundling information to the metadata source
        /// </summary>
        private void SaveBundlesIndex()
        {
            if (PendingBundledUpdates.Count > 0)
            {
                throw new Exception("Unresolved bundle updates");
            }

            CommitProgress?.Invoke(this, new OperationProgress() { CurrentOperation = OperationType.IndexingBundlesStart });

            SerializeIndexToArchive(
                BundlesIndexEntryName,
                new KeyValuePair<Dictionary<int, List<int>>, Dictionary<int, List<int>>>(BundlesIndex, IsBundledTable));
            CommitProgress?.Invoke(this, new OperationProgress() { CurrentOperation = OperationType.IndexingBundlesEnd });
        }

        private void OnNewStore_InitializeBundles()
        {
            IsBundledTable = new Dictionary<int, List<int>>();
            BundlesIndex = new Dictionary<int, List<int>>();
        }

        private void OnDeltaStore_InitializeBundes()
        {
            IsBundledTable = new Dictionary<int, List<int>>();
            BundlesIndex = new Dictionary<int, List<int>>();
        }

        private void ReadBundlesIndex()
        {
            var indexPair = DeserializeIndexFromArchive<KeyValuePair<Dictionary<int, List<int>>, Dictionary<int, List<int>>>>(BundlesIndexEntryName);
            BundlesIndex = indexPair.Key;
            IsBundledTable = indexPair.Value;
        }

        private void EnsureBundlesIndexLoaded()
        {
            lock (this)
            {
                // lazy load the bundles index
                if (BundlesIndex == null)
                {
                    ReadBundlesIndex();
                }
            }
        }

        /// <summary>
        /// Checks if an update is a bundle (contains other updates)
        /// </summary>
        /// <param name="updateIdentity">The update to check</param>
        /// <returns>True if the update contains other updates, false otherwise</returns>
        public bool IsBundle(Identity updateIdentity)
        {
            return IsBundle(this[updateIdentity]);
        }

        private bool IsBundle(int updateIndex)
        {
            EnsureBundlesIndexLoaded();
            if (BundlesIndex.ContainsKey(updateIndex))
            {
                return true;
            }
            else if (IsInBaseline(updateIndex))
            {
                return BaselineSource.IsBundle(updateIndex);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if an update is bundled with another update
        /// </summary>
        /// <param name="updateIdentity">The update to check</param>
        /// <returns>True if there is another update that contains this update</returns>
        public bool IsBundled(Identity updateIdentity)
        {
            return IsBundled(this[updateIdentity]).Count > 0;
        }

        private List<int> IsBundled(int updateIndex)
        {
            EnsureBundlesIndexLoaded();

            if (IsBundledTable.ContainsKey(updateIndex))
            {
                return IsBundledTable[updateIndex];
            }
            else if (IsInBaseline(updateIndex))
            {
                return BaselineSource.IsBundled(updateIndex);
            }
            else
            {
                throw new Exception($"Cannot find bundling information for update {this[updateIndex]}");
            }
        }

        /// <summary>
        /// Gets the bundle update to which this update belongs to
        /// </summary>
        /// <param name="updateIdentity">The update whose parent bundle to get</param>
        /// <returns>The parent bundle</returns>
        public IEnumerable<Identity> GetBundle(Identity updateIdentity)
        {
            var bundleParents = IsBundled(this[updateIdentity]);
            if (bundleParents.Count == 0)
            {
                throw new Exception($"Update {updateIdentity} does not belong to a bundle");
            }
            else
            {
                return bundleParents.Select(index => this[index]);
            }
        }

        /// <summary>
        /// Gets the list of updates that are bundled withing the specified update
        /// </summary>
        /// <param name="updateIdentity">The update to get bundled updates for</param>
        /// <returns>List of bundled updates</returns>
        public IEnumerable<Identity> GetBundledUpdates(Identity updateIdentity)
        {
            return GetBundledUpdates(this[updateIdentity]);
        }

        private IEnumerable<Identity> GetBundledUpdates(int updateIndex)
        {
            EnsureBundlesIndexLoaded();

            if (BundlesIndex.TryGetValue(updateIndex, out var result))
            {
                return result.Select(index => this[index]);
            }
            else if (IsInBaseline(updateIndex))
            {
                return BaselineSource.GetBundledUpdates(updateIndex);
            }
            else
            {
                throw new Exception($"Update {this[updateIndex]} is not a bundle");
            }
        }
    }
}
