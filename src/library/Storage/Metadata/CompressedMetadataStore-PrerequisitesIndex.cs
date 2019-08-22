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
        private List<KeyValuePair<int, List<Guid>>> PrerequisitesList;

        private Dictionary<int, List<Prerequisite>> PrerequisitesIndex;

        private const string PrerequisitesListEntryName = "prerequisites-list.json";

        private Dictionary<Identity, List<Prerequisite>> AddedPrerequisites = new Dictionary<Identity, List<Prerequisite>>();

        private void AddPrerequisiteInformation(int newUpdateIndex, Identity newUpdateIdentity, XDocument updateXml)
        {
            var prerequisites = Prerequisite.FromXml(updateXml);

            if (prerequisites.Count > 0)
            {
                // Save this for later when we use prerequisites to resolve product and classification
                AddedPrerequisites.Add(newUpdateIdentity, prerequisites);
            }

            foreach (var prereq in prerequisites)
            {
                if (prereq is Simple)
                {
                    PrerequisitesList.Add(new KeyValuePair<int, List<Guid>>(newUpdateIndex, new List<Guid>() { (prereq as Simple).UpdateId }));
                }
                else if (prereq is AtLeastOne)
                {
                    PrerequisitesList.Add(
                        new KeyValuePair<int, List<Guid>>(
                            newUpdateIndex,
                            new List<Guid>((prereq as AtLeastOne).Simple.Select(s => s.UpdateId))));

                    if ((prereq as AtLeastOne).IsCategory)
                    {
                        // Add an empty guid at the end to mark that this atLeast group is a category group
                        PrerequisitesList.Last().Value.Add(Guid.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// Saves the prerequisites information to the metadata source
        /// </summary>
        private void SavePrerequisitesIndex()
        {
            CommitProgress?.Invoke(this, new OperationProgress() { CurrentOperation = OperationType.IndexingPrerequisitesStart });
            SerializeIndexToArchive(PrerequisitesListEntryName, PrerequisitesList);
            CommitProgress?.Invoke(this, new OperationProgress() { CurrentOperation = OperationType.IndexingPrerequisitesEnd });
        }

        private void OnNewStore_InitializePrerequisites()
        {
            PrerequisitesList = new List<KeyValuePair<int, List<Guid>>>();
        }

        private void OnDeltaStore_InitializePrerequisites()
        {
            PrerequisitesList = new List<KeyValuePair<int, List<Guid>>>();
        }

        private void ReadPrerequisitesIndex()
        {
            PrerequisitesList = DeserializeIndexFromArchive<List<KeyValuePair<int, List<Guid>>>>(PrerequisitesListEntryName);

            PrerequisitesIndex = new Dictionary<int, List<Prerequisite>>();

            PrerequisitesList.GroupBy(l => l.Key).ToList().ForEach(group =>
            {
                List<Prerequisite> rehydratedPrerequisites = new List<Prerequisite>();
                var updateIndex = group.Key;
                foreach(var prereqEntry in group)
                {
                    var prereqList = prereqEntry.Value;
                    if (prereqList.Count == 1)
                    {
                        rehydratedPrerequisites.Add(new Simple(prereqList[0]));
                    }
                    else
                    {
                        rehydratedPrerequisites.Add(new AtLeastOne(prereqList));
                    }
                }

                PrerequisitesIndex.Add(updateIndex, rehydratedPrerequisites);
            });

            PrerequisitesList.Clear();
        }

        private void EnsurePrerequisitesIndexLoaded()
        {
            lock (this)
            {
                // lazy load the prerequisites index
                if (PrerequisitesIndex == null)
                {
                    ReadPrerequisitesIndex();
                }
            }
        }

        /// <summary>
        /// Check if an update has prerequisites
        /// </summary>
        /// <param name="updateIdentity">The update to check prerequisites for</param>
        /// <returns>True if an update has prerequisites, false otherwise</returns>
        public bool HasPrerequisites(Identity updateIdentity)
        {
            return HasPrerequisites(this[updateIdentity]);
        }

        private bool HasPrerequisites(int updateIndex)
        {
            EnsurePrerequisitesIndexLoaded();

            if (PrerequisitesIndex.ContainsKey(updateIndex))
            {
                return true;
            }
            else if (IsInBaseline(updateIndex))
            {
                return BaselineSource.HasPrerequisites(updateIndex);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the list of prerequisites for an update
        /// </summary>
        /// <param name="updateIdentity">The update to get prerequisites for</param>
        /// <returns>List of prerequisites</returns>
        public List<Prerequisite> GetPrerequisites(Identity updateIdentity)
        {
            return GetPrerequisites(this[updateIdentity]);
        }

        private List<Prerequisite> GetPrerequisites(int updateIndex)
        {
            EnsurePrerequisitesIndexLoaded();

            if (PrerequisitesIndex.ContainsKey(updateIndex))
            {
                return PrerequisitesIndex[updateIndex];
            }
            else if (IsInBaseline(updateIndex))
            {
                return BaselineSource.GetPrerequisites(updateIndex);
            }
            else
            {
                throw new Exception("The update does not have prerequisites");
            }
        }
    }
}
