// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;
using System.Collections.Generic;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ClientSync
{

    public partial class ClientSyncWebService
    {
        readonly private HashSet<MicrosoftUpdatePackageIdentity> ApprovedSoftwareUpdates;
        private bool AreAllSoftwareUpdatesApproved = true;
        readonly private HashSet<MicrosoftUpdatePackageIdentity> ApprovedDriverUpdates;

        /// <summary>
        /// Delegate method called to report updates applicable to a client but which are not approved and thus not offered
        /// </summary>
        /// <param name="requestedUnapprovedUpdates"></param>
        public delegate void UnApprovedUpdatesRequestedDelegate(IEnumerable<MicrosoftUpdatePackage> requestedUnapprovedUpdates);

#pragma warning disable 0067
        /// <summary>
        /// Event raised when software updates are applicable to a client but are not approved for distribution
        /// </summary>
        public event UnApprovedUpdatesRequestedDelegate OnUnApprovedSoftwareUpdatesRequested;
#pragma warning restore 0067

        /// <summary>
        /// Event raised when driver updates are applicable to a client but are not approved for distribution
        /// </summary>
        public event UnApprovedUpdatesRequestedDelegate OnUnApprovedDriverUpdatesRequested;

        /// <summary>
        /// Adds an update identity to the list of approved software updates.
        /// Approved updates are made available to clients connecting to this service.
        /// </summary>
        /// <param name="approvedUpdate">Approved update</param>
        public void AddApprovedSoftwareUpdate(MicrosoftUpdatePackageIdentity approvedUpdate)
        {
            AreAllSoftwareUpdatesApproved = false;
            ApprovedSoftwareUpdates.Add(approvedUpdate);
        }

        /// <summary>
        /// Adds a list of update identities to the list of approved software updates.
        /// Approved updates are made available to clients connecting to this service.
        /// </summary>
        /// <param name="approvedUpdates">List of approved updates</param>
        public void AddApprovedSoftwareUpdates(IEnumerable<MicrosoftUpdatePackageIdentity> approvedUpdates)
        {
            AreAllSoftwareUpdatesApproved = false;
            foreach (var approvedUpdate in approvedUpdates)
            {
                ApprovedSoftwareUpdates.Add(approvedUpdate);
            }
        }

        /// <summary>
        /// Adds an update identities to the list of approved driver updates.
        /// Approved updates are made available to clients connecting to this service.
        /// </summary>
        /// <param name="approvedUpdate">Approved driver update</param>
        public void AddApprovedDriverUpdate(MicrosoftUpdatePackageIdentity approvedUpdate)
        {
            ApprovedDriverUpdates.Add(approvedUpdate);
        }

        /// <summary>
        /// Adds a list of update identities to the list of approved driver updates.
        /// Approved updates are made available to clients connecting to this service.
        /// </summary>
        /// <param name="approvedUpdates"></param>
        public void AddApprovedDriverUpdates(IEnumerable<MicrosoftUpdatePackageIdentity> approvedUpdates)
        {
            foreach (var approvedUpdate in approvedUpdates)
            {
                ApprovedDriverUpdates.Add(approvedUpdate);
            }
        }

        /// <summary>
        /// Removes an approved software update from the list of approved software updates.
        /// The software update will not be given to connecting clients anymore.
        /// </summary>
        /// <param name="updateIdentity">Identity of update to un-approve</param>
        public void RemoveApprovedSoftwareUpdate(MicrosoftUpdatePackageIdentity updateIdentity)
        {
            ApprovedSoftwareUpdates.Remove(updateIdentity);
        }

        /// <summary>
        /// Removes an approved software update from the list of approved software updates.
        /// The software update will not be given to connecting clients anymore.
        /// </summary>
        /// <param name="updateIdentity">Identity of update to un-approve</param>
        public void RemoveApprovedDriverUpdate(MicrosoftUpdatePackageIdentity updateIdentity)
        {
            ApprovedDriverUpdates.Remove(updateIdentity);
        }

        /// <summary>
        /// Clears the list of approved driver updates.
        /// Un-approved updates are not made available to connecting clients.
        /// </summary>
        public void ClearApprovedDriverUpdates()
        {
            ApprovedDriverUpdates.Clear();
        }

        /// <summary>
        /// Clears the list of approved software updates.
        /// Un-approved updates are not made available to connecting clients.
        /// </summary>
        public void ClearApprovedSoftwareUpdates()
        {
            ApprovedSoftwareUpdates.Clear();
        }
    }
}
