// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Storage
{
    public partial class CompressedMetadataStore
    {
        /// <summary>
        /// Dictionary of superseded updates; the key is a superseded update, the value is the index
        /// of the update that superseded it
        /// </summary>
        private Dictionary<Guid, int> SupersededUpdates;

        /// <summary>
        /// Index of updates that supersed other updates; values are list of updates being superseded
        /// by the key update
        /// </summary>
        private Dictionary<int, List<Guid>> SupersedingUpdates;

        private const string SupersedingIndexEntryName = "superseding-index.json";
        private const string SupersededIndexEntryName = "superseded-index.json";

        void OnDeltaStore_InitializeSupersededIndex()
        {
            SupersededUpdates = new Dictionary<Guid, int>();
            SupersedingUpdates = new Dictionary<int, List<Guid>>();
        }

        void OnNewStore_InitializeSupersededIndex()
        {
            SupersededUpdates = new Dictionary<Guid, int>();
            SupersedingUpdates = new Dictionary<int, List<Guid>>();
        }

        private void ExtractSupersedingInformation(int updateIndex, Identity updateIdentity, XDocument xdoc)
        {
            var supersededUpdates = SupersededUpdatesParser.Parse(xdoc);

            foreach(var supersededUpdate in supersededUpdates)
            {
                // Mark the update as superseded and save the index of the update superseding it
                if (!SupersededUpdates.TryAdd(supersededUpdate.ID, updateIndex))
                {
                    SupersededUpdates[supersededUpdate.ID]  = updateIndex;
                }
            }

            if (supersededUpdates.Count > 0)
            {
                SupersedingUpdates.Add(updateIndex, supersededUpdates.Select(u => u.ID).ToList());
            }
        }

        private void SaveSupersededndex()
        {
            CommitProgress?.Invoke(this, new OperationProgress() { CurrentOperation = OperationType.ProcessSupersedeDataStart });
            SerializeIndexToArchive(SupersededIndexEntryName, SupersededUpdates);
            SerializeIndexToArchive(SupersedingIndexEntryName, SupersedingUpdates);
            CommitProgress?.Invoke(this, new OperationProgress() { CurrentOperation = OperationType.ProcessSupersedeDataEnd });
        }

        private void ReadSupersededIndex()
        {
            SupersededUpdates = DeserializeIndexFromArchive<Dictionary<Guid, int>>(SupersededIndexEntryName);
        }

        private void ReadSupersedingIndex()
        {
            SupersedingUpdates = DeserializeIndexFromArchive<Dictionary<int, List<Guid>>>(SupersedingIndexEntryName);
        }

        /// <summary>
        /// Checks if an update has been superseded
        /// </summary>
        /// <param name="updateIdentity">Update identity to check if superseded</param>
        /// <returns>false if not superseded, true otherwise</returns>
        public bool IsSuperseded(Identity updateIdentity)
        {
            return GetSupersedingUpdateIndex(updateIdentity) >= 0;
        }

        /// <summary>
        /// Gets the update that superseded the update specified
        /// </summary>
        /// <param name="updateIdentity">Update identity to check if superseded</param>
        /// <returns>The update that superseded the update specified</returns>
        public Identity GetSupersedingUpdate(Identity updateIdentity)
        {
            var supersedingIndex = GetSupersedingUpdateIndex(updateIdentity);
            if (supersedingIndex < 0)
            {
                throw new Exception($"The update {updateIdentity} is not superseded");
            }

            return this[supersedingIndex];
        }

        /// <summary>
        /// Checks if an update has been superseded, and if yes, returns the index of the update that superseded it
        /// </summary>
        /// <param name="updateIdentity">Update identity to check if superseded</param>
        /// <returns>-1 if the update was not superseded, index of superseding update otherwise</returns>
        int GetSupersedingUpdateIndex(Identity updateIdentity)
        {
            lock (this)
            {
                // lazy load the superseded index
                if (SupersededUpdates == null)
                {
                    ReadSupersededIndex();
                }
            }

            if (SupersededUpdates.TryGetValue(updateIdentity.ID, out int supersedingIndex))
            {
                return supersedingIndex;
            }
            else if (IsDeltaSource)
            {
                return BaselineSource.GetSupersedingUpdateIndex(updateIdentity);
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Checks if an update superseds other updates
        /// </summary>
        /// <param name="updateIdentity">The update to check if it superseds other updates</param>
        /// <returns>True if the update superseds other updates, false otherwise</returns>
        public bool IsSuperseding(Identity updateIdentity)
        {
            return GetSupersededUpdates(this[updateIdentity]) != null;
        }

        /// <summary>
        /// Gets the list of update that the specified update superseds
        /// </summary>
        /// <param name="updateIdentity">The update to get list of superseded updates for</param>
        /// <returns>List of updates superseded by the specified update</returns>
        public IReadOnlyList<Guid> GetSupersededUpdates(Identity updateIdentity)
        {
            var supersededList = GetSupersededUpdates(this[updateIdentity]);
            if (supersededList == null)
            {
                throw new Exception($"Update {updateIdentity} does not supersed any updates");
            }

            return supersededList;
        }

        IReadOnlyList<Guid> GetSupersededUpdates(int updateIndex)
        {
            lock (this)
            {
                // lazy load the superseded index
                if (SupersedingUpdates == null)
                {
                    ReadSupersedingIndex();
                }
            }

            if (SupersedingUpdates.TryGetValue(updateIndex, out List<Guid> supersededList))
            {
                return supersededList;
            }
            else if (IsInBaseline(updateIndex))
            {
                return BaselineSource.GetSupersededUpdates(updateIndex);
            }
            else
            {
                return null;
            }
        }
    }
}
