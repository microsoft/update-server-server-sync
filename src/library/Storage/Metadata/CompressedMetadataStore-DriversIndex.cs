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
        /// Driver metadata serialized as JSON for quick reading.
        /// The data in this list is parsed from driver update metadata XML
        /// </summary>
        private List<DriverMetadata> DriverMetadataStore;

        // Mapping from metadata index (index in the list above) to driver index (index into Updates list)
        private Dictionary<int, int> MetadataToDriverMap;

        // Mapping from driver index to metadata index (reverse of above)
        private Dictionary<int, KeyValuePair<int, int>> DriverToMetadataMap;

        // Dictionary of hardware ID maps and the corresponding driver metadata that contains them
        private Dictionary<string, List<int>> HardwareIdMap;

        // Index of driver versions
        private Dictionary<int, DriverVersion> DriverVersionIndex;

        // Index of driver feature scores
        private Dictionary<int, List<DriverFeatureScore>> DriverFeatureScoreIndex;

        // Computer hardware IDs indexes bu the driver metadata that containst them
        private Dictionary<int, List<Guid>> MetadataToComputerHardwareIdMap;

        private const string DriversIndexEntryName = @"drivers-index/metadata.json";
        private const string DriverVersionIndexEntryName = @"drivers-index/version-index.json";
        private const string DriverFeatureScoreIndexEntryName = @"drivers-index/feature-score-index.json";
        private const string DriverToMetadataMapEntryName = @"drivers-index/driver-to-metadata-map.json";

        private const string HardwareIdIndexEntryName = @"drivers-index/hardware-id-index.json";
        private const string ComputerHardwareIdIndexEntryName = @"drivers-index/computer-hardware-id-index.json";

        /// <summary>
        /// Extract driver metadata from an update XML and added relevant information to indexes and maps
        /// for quick retrieval
        /// </summary>
        /// <param name="updateIndex">The index of the update to parse.</param>
        /// <param name="xdoc">The XML document that contains the metadata</param>
        private void ExtractAndIndexDriverMetadata(int updateIndex, XDocument xdoc)
        {
            var driverMetadata = DriverMetadata.GetDriverMetadataFromXml(xdoc);

            if (driverMetadata.Count == 0)
            {
                return;
            }

            // Make note of the current length; this becomes the index of the first driver metadata
            var startIndex = DriverMetadataStore.Count;

            // Add version information to a dictionary
            AddDriverVersionToIndex(driverMetadata, startIndex);

            // Add feature score information to a dictionary
            AddDriverFeatureScoreToIndex(driverMetadata, startIndex);

            // Add hardware IDs to the index
            AddDriverMetadataToHWIDIndex(driverMetadata, startIndex);

            // Add target computer hw ids to the index
            AddDriverMetadataToComputerHwIdIndex(driverMetadata, startIndex);

            // Save the driver metadata to the list of all metadata; allows for fast lookup later without the need
            // to reparse the metadata from update XML
            DriverMetadataStore.AddRange(driverMetadata);

            // Map the driver update to the range of driver metadatas we added in the driver metadata store. This allows
            // quick lookup of driver metadata without having to load all the metadata in memory
            DriverToMetadataMap.Add(updateIndex, new KeyValuePair<int, int>(startIndex, driverMetadata.Count));
        }

        /// <summary>
        /// Index feature scores from the list of drivers metadata
        /// </summary>
        /// <param name="driverMetadata">List of driver metadata</param>
        /// <param name="startIndexInMetadataStore">The index of the first update in this list when added to the metadata store</param>
        private void AddDriverFeatureScoreToIndex(List<DriverMetadata> driverMetadata, int startIndexInMetadataStore)
        {
            for (int i = 0; i < driverMetadata.Count; i++)
            {
                if (driverMetadata[i].FeatureScores.Count > 0)
                {
                    DriverFeatureScoreIndex.Add(startIndexInMetadataStore + i, driverMetadata[i].FeatureScores);
                }
            }
        }

        /// <summary>
        /// Index driver versions from the driver metadata list
        /// </summary>
        /// <param name="driverMetadata">List of driver metadata to add</param>
        /// <param name="startIndexInMetadataStore">The index of the first update in this list when added to the metadata store</param>
        private void AddDriverVersionToIndex(List<DriverMetadata> driverMetadata, int startIndexInMetadataStore)
        {
            for (int i = 0; i < driverMetadata.Count; i++)
            {
                DriverVersionIndex.Add(
                    startIndexInMetadataStore + i, driverMetadata[i].Versioning);
            }
        }

        /// <summary>
        /// Index hardware ids from the driver metadata list
        /// </summary>
        /// <param name="driverMetadata">List of driver metadata to add</param>
        /// <param name="startIndexInMetadataStore">The index of the first update in this list when added to the metadata store</param>
        private void AddDriverMetadataToHWIDIndex(List<DriverMetadata> driverMetadata, int startIndexInMetadataStore)
        {
            // Build the hardware ID dictionary
            for (int i = 0; i < driverMetadata.Count; i++)
            {
                if (HardwareIdMap.ContainsKey(driverMetadata[i].HardwareID))
                {
                    // Save the metadata index that corresponds to the HW ID
                    HardwareIdMap[driverMetadata[i].HardwareID].Add(startIndexInMetadataStore + i);
                }
                else
                {
                    HardwareIdMap.Add(driverMetadata[i].HardwareID, new List<int>() { startIndexInMetadataStore + i });
                }
            }
        }

        /// <summary>
        /// Index computer hardware ids from the driver metadata list
        /// </summary>
        /// <param name="driverMetadata">List of driver metadata to add</param>
        /// <param name="startIndexInMetadataStore">The index of the first update in this list when added to the metadata store</param>
        private void AddDriverMetadataToComputerHwIdIndex(List<DriverMetadata> driverMetadata, int startIndexInMetadataStore)
        {
            for (int i = 0; i < driverMetadata.Count; i++)
            {
                List<Guid> computerHwIdSet = null;
                if (driverMetadata[i].TargetComputerHardwareId.Count > 0 && driverMetadata[i].DistributionComputerHardwareId.Count > 0)
                {
                    // If both target and distribution computer hardware ids are present, take the intersection
                    computerHwIdSet = driverMetadata[i].TargetComputerHardwareId.Intersect(driverMetadata[i].DistributionComputerHardwareId).ToList();
                }
                else if (driverMetadata[i].TargetComputerHardwareId.Count > 0)
                {
                    // Just target computer hardware ids present
                    computerHwIdSet = driverMetadata[i].TargetComputerHardwareId;
                }
                else if (driverMetadata[i].DistributionComputerHardwareId.Count > 0)
                {
                    // Just distribution computer hardware ids present
                    computerHwIdSet = driverMetadata[i].DistributionComputerHardwareId;
                }

                if (computerHwIdSet != null)
                {
                    MetadataToComputerHardwareIdMap.Add(startIndexInMetadataStore + i, computerHwIdSet);
                }
            }
        }

        void OnNewStore_InitializeDriversIndex()
        {
            DriverToMetadataMap = new Dictionary<int, KeyValuePair<int, int>>();
            DriverMetadataStore = new List<DriverMetadata>();
            MetadataToDriverMap = new Dictionary<int, int>();

            HardwareIdMap = new Dictionary<string, List<int>>();

            DriverVersionIndex = new Dictionary<int, DriverVersion>();
            MetadataToComputerHardwareIdMap = new Dictionary<int, List<Guid>>();
            DriverFeatureScoreIndex = new Dictionary<int, List<DriverFeatureScore>>();
        }

        void OnDeltaStore_InitializeDriversIndex()
        {
            DriverMetadataStore = new List<DriverMetadata>();
            DriverToMetadataMap = new Dictionary<int, KeyValuePair<int, int>>();
            MetadataToDriverMap = new Dictionary<int, int>();

            HardwareIdMap = new Dictionary<string, List<int>>();

            DriverVersionIndex = new Dictionary<int, DriverVersion>();
            MetadataToComputerHardwareIdMap = new Dictionary<int, List<Guid>>();

            DriverFeatureScoreIndex = new Dictionary<int, List<DriverFeatureScore>>();
        }

        private void SaveDriversIndex()
        {
            CommitProgress?.Invoke(this, new OperationProgress() { CurrentOperation = OperationType.IndexingDriversStart });
            SerializeIndexToArchive(DriversIndexEntryName, DriverMetadataStore);
            SerializeIndexToArchive(DriverToMetadataMapEntryName, DriverToMetadataMap);
            SerializeIndexToArchive(DriverVersionIndexEntryName, DriverVersionIndex);
            SerializeIndexToArchive(DriverFeatureScoreIndexEntryName, DriverFeatureScoreIndex);

            SerializeIndexToArchive(HardwareIdIndexEntryName, HardwareIdMap);
            SerializeIndexToArchive(ComputerHardwareIdIndexEntryName, MetadataToComputerHardwareIdMap);

            CommitProgress?.Invoke(this, new OperationProgress() { CurrentOperation = OperationType.IndexingDriversEnd });
        }

        private void ReadDriverMetadata()
        {
            DriverMetadataStore = DeserializeIndexFromArchive<List<DriverMetadata>> (DriversIndexEntryName);

            // Build a reverse lookup from metadata index to update index
            if (DriverToMetadataMap == null)
            {
                DriverToMetadataMap = DeserializeIndexFromArchive<Dictionary<int, KeyValuePair<int, int>>>(DriverToMetadataMapEntryName);

                MetadataToDriverMap = new Dictionary<int, int>();
                foreach (var driverEntry in DriverToMetadataMap)
                {
                    var metadataCount = driverEntry.Value.Value;
                    var metadataStartIndex = driverEntry.Value.Key;
                    for (int i = 0; i < metadataCount; i++)
                    {
                        MetadataToDriverMap.Add(metadataStartIndex + i, driverEntry.Key);
                    }
                }
            }
        }

        private void ReadHardwareIdIndexes()
        {
            if (IsDeltaSource)
            {
                // Recursively read the indexes from all baselines
                BaselineSource.ReadHardwareIdIndexes();
            }

            // Build a reverse lookup from metadata index to update index
            if (DriverToMetadataMap == null)
            {
                DriverToMetadataMap = DeserializeIndexFromArchive<Dictionary<int, KeyValuePair<int, int>>>(DriverToMetadataMapEntryName);

                MetadataToDriverMap = new Dictionary<int, int>();
                foreach (var driverEntry in DriverToMetadataMap)
                {
                    var metadataCount = driverEntry.Value.Value;
                    var metadataStartIndex = driverEntry.Value.Key;
                    for (int i = 0; i < metadataCount; i++)
                    {
                        MetadataToDriverMap.Add(metadataStartIndex + i, driverEntry.Key);
                    }
                }
            }

            HardwareIdMap = DeserializeIndexFromArchive<Dictionary<string, List<int>>>(HardwareIdIndexEntryName);
            MetadataToComputerHardwareIdMap = DeserializeIndexFromArchive<Dictionary<int, List<Guid>>>(ComputerHardwareIdIndexEntryName);

            DriverVersionIndex = DeserializeIndexFromArchive<Dictionary<int, DriverVersion>>(DriverVersionIndexEntryName);
            DriverFeatureScoreIndex = DeserializeIndexFromArchive<Dictionary<int, List<DriverFeatureScore>>>(DriverFeatureScoreIndexEntryName);
        }

        /// <summary>
        /// Checks if an update contains driver metadata
        /// </summary>
        /// <param name="updateIdentity">Update identity</param>
        /// <returns>True if the update has driver metadata, false otherwise</returns>
        public bool HasDriverMetadata(Identity updateIdentity)
        {
            return GetDriverMetadata(this[updateIdentity]) != null;
        }

        /// <summary>
        /// Retrieves driver metadata for an update
        /// </summary>
        /// <param name="updateIdentity">Update identity</param>
        /// <returns>Collection of driver metadata entries</returns>
        public IEnumerable<DriverMetadata> GetDriverMetadata(Identity updateIdentity)
        {
            if (UpdateTypeMap[this[updateIdentity]] != (uint)UpdateType.Driver)
            {
                throw new Exception($"Update {updateIdentity} is not a driver");
            }

            return GetDriverMetadata(this[updateIdentity]);
        }

        private IEnumerable<DriverMetadata> GetDriverMetadata(int updateIndex)
        {
            lock (this)
            {
                if (DriverMetadataStore == null)
                {
                    ReadDriverMetadata();
                }
            }

            if (DriverToMetadataMap.ContainsKey(updateIndex))
            {
                return DriverMetadataStore.GetRange(DriverToMetadataMap[updateIndex].Key, DriverToMetadataMap[updateIndex].Value);
            }
            else if (IsInBaseline(updateIndex))
            {
                return GetDriverMetadata(updateIndex);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Finds the best match for a driver update that matches the specified hardware ids and computer hardware ids
        /// </summary>
        /// <param name="hardwareIds">Device hardware ids, sorted from specific to generic</param>
        /// <param name="computerHardwareIds">List of computer hardware ids</param>
        /// <param name="installedPrerequisites">List of prerequisites installed on the target computer. Used to filter updates to just those applicable to a system</param>
        /// <returns>If a driver match is found, matching information; null otherwise</returns>
        public DriverMatchResult MatchDriver(IEnumerable<string> hardwareIds, IEnumerable<Guid> computerHardwareIds, List<Guid> installedPrerequisites)
        {
            lock (this)
            {
                if (HardwareIdMap == null)
                {
                    ReadHardwareIdIndexes();
                }
            }

            // Go through the list of compatible hardware ids and try to find a match;
            // Stop at the first match; the list must be sorted in specific to generic order
            // and as such the first match is the best one
            foreach(var hardwareId in hardwareIds)
            {
                var driverMatch = MatchDriverMetadata(hardwareId, computerHardwareIds, installedPrerequisites);
                if (driverMatch != null)
                {
                    driverMatch.MatchedHardwareId = hardwareId;
                    return driverMatch;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the best match for a hardware id and computer hardware ids
        /// </summary>
        /// <param name="hardwareId">Device hardware id</param>
        /// <param name="computerHardwareIds">List of computer hardware ids</param>
        /// <param name="installedPrerequisites">List of prerequisites installed on the target computer. Used to filter updates to just those applicable to a system</param>
        /// <returns>Best matched driver</returns>
        private DriverMatchResult MatchDriverMetadata(string hardwareId, IEnumerable<Guid> computerHardwareIds, List<Guid> installedPrerequisites)
        {
            List<int> matchedDriverMetadataIndexes = GetDriverMetadataForHardwareId(hardwareId.ToLower());

            matchedDriverMetadataIndexes.Sort();

            if (matchedDriverMetadataIndexes.Count == 0)
            {
                return null;
            }

            // Remove drivers that are not applicable
            matchedDriverMetadataIndexes.RemoveAll(
                metadataIndex =>
                !Updates[this[GetDriverIndex(metadataIndex)]].IsApplicable(installedPrerequisites));

            var bestMatch = MatchByComputerHardwareId(matchedDriverMetadataIndexes, computerHardwareIds);
            if (bestMatch == null)
            {
                bestMatch = MatchBySimpleHardwareId(matchedDriverMetadataIndexes);
            }

            return bestMatch;
        }

        /// <summary>
        /// From the list of drivers matched to a device on hardware id, attempts to match by computer hardware id
        /// </summary>
        /// <param name="metadataMatchedByHwId">List of driver metadata that was matched by a hardware id</param>
        /// <param name="computerHardwareIds">List of computer hardware ids compatible with a system</param>
        /// <returns>Best driver match based on computer hardware id matching</returns>
        private DriverMatchResult MatchByComputerHardwareId(List<int> metadataMatchedByHwId, IEnumerable<Guid> computerHardwareIds)
        {
            // Filter to driver updates that target a computer hardware id
            var metadataWithComputerHwId = metadataMatchedByHwId.Where(m => HasComputerHardwareIds(m));

            // Try to match a computer hardware id against the list of updates matched by a hardware id
            foreach (var computerHardwareId in computerHardwareIds)
            {
                // Get all current HWID matches that match a computer HW ID
                var computerHwIdMatch = metadataWithComputerHwId.Where(metadataIndex => GetComputerHardwareIds(metadataIndex).Contains(computerHardwareId));
                if (computerHwIdMatch.Count() > 0)
                {
                    // Found one or more driver updates that targets a computer hardware id
                    // Find matches with a feature score
                    var matchesWithFeatureScore = computerHwIdMatch.Where(metadataIndex => HasFeatureScores(metadataIndex));

                    if (matchesWithFeatureScore.Count() > 0)
                    {
                        // Select the driver with the best feature score (lower value is better)
                        var bestScore = matchesWithFeatureScore.SelectMany(metadataIndex => GetFeatureScores(metadataIndex)).Min();
                        var matchedMetadataIndex = matchesWithFeatureScore
                            .Where(metadataIndex => GetFeatureScores(metadataIndex).Any(featureScore => featureScore.Score == bestScore.Score))
                            .First();

                        return new DriverMatchResult(GetDriverUpdateFromMetadataIndex(matchedMetadataIndex))
                        {
                            MatchedVersion = GetDriverVersion(matchedMetadataIndex),
                            MatchedFeatureScore = bestScore,
                            MatchedComputerHardwareId = computerHardwareId
                        };
                    }
                    else
                    {
                        // None of the drivers have a feature score; sort them by version information
                        var bestVersion = computerHwIdMatch.Select(metadataIndex => GetDriverVersion(metadataIndex)).Max();

                        var matchedMetadataIndex = computerHwIdMatch
                            .Where(metadataIndex => GetDriverVersion(metadataIndex).Equals(bestVersion))
                            .First();

                        return new DriverMatchResult(GetDriverUpdateFromMetadataIndex(matchedMetadataIndex))
                        {
                            MatchedVersion = bestVersion,
                            MatchedComputerHardwareId = computerHardwareId
                        };
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the best match for a driver that was matched by just hardware ID matching (no computer hardware id or feature score available)
        /// </summary>
        /// <param name="metadataMatchedByHwId">The list of drivers matched by hardware id</param>
        /// <returns>Best driver match based on device hardware id</returns>
        private DriverMatchResult MatchBySimpleHardwareId(List<int> metadataMatchedByHwId)
        {
            // There is no match on computer hardware id; return the best match out of updates without computer hardware id targeting
            // Do not consider drivers that target a computer hardware ID
            var metadataMatchedByOnlyHwId = metadataMatchedByHwId.Where(metadataIndex => !HasComputerHardwareIds(metadataIndex));

            if (metadataMatchedByOnlyHwId.Count() > 0)
            {
                // Find the best version and return the driver update
                var bestVersion = metadataMatchedByOnlyHwId.Select(metadataIndex => GetDriverVersion(metadataIndex)).Max();

                var matchedMetadataIndex = metadataMatchedByOnlyHwId
                    .Where(metadataIndex => GetDriverVersion(metadataIndex).Equals(bestVersion))
                    .First();

                return new DriverMatchResult(GetDriverUpdateFromMetadataIndex(matchedMetadataIndex))
                {
                    MatchedVersion = bestVersion,
                };
            }
            else
            {
                // no match
                return null;
            }
        }

        /// <summary>
        /// Gets driver metadata entries that match the specified HW ID. This method is recursive across all delta baselines.
        /// </summary>
        /// <param name="hardwareId">Device hardware id</param>
        /// <returns>List of driver metadata indexes that match the device hardware id</returns>
        private List<int> GetDriverMetadataForHardwareId(string hardwareId)
        {
            lock (this)
            {
                if (HardwareIdMap == null)
                {
                    ReadHardwareIdIndexes();
                }
            }

            List<int> matchedDriverMetadataIndexes = new List<int>();

            if (HardwareIdMap.ContainsKey(hardwareId))
            {
                matchedDriverMetadataIndexes.AddRange(HardwareIdMap[hardwareId]);
            }

            if (IsDeltaSource)
            {
                matchedDriverMetadataIndexes.AddRange(BaselineSource.GetDriverMetadataForHardwareId(hardwareId));
            }

            return matchedDriverMetadataIndexes;
        }

        /// These getters recusively go into all delta baselines for a metadata source and retrieve driver metadata
        /// Recursive getters are prefered to avoid having to accumulate driver indexes into the latest delta source - it can lead to huge memory utilization over time.
        /// Instead, each incremental metadata source contains its own complete set of driver metadata and we query recursively across all deltas
        #region Recursive getters

        private DriverUpdate GetDriverUpdateFromMetadataIndex(int metadataIndex) => Updates[this[GetDriverIndex(metadataIndex)]] as DriverUpdate;

        private int GetDriverIndex(int metadataIndex)
        {
            if (MetadataToDriverMap.TryGetValue(metadataIndex, out int driverIndex))
            {
                return driverIndex;
            }
            else if (IsDeltaSource)
            {
                return BaselineSource.GetDriverIndex(metadataIndex);
            }
            else
            {
                throw new IndexOutOfRangeException("Driver metadata index cannot be found");
            }
        }

        private DriverVersion GetDriverVersion(int metadataIndex)
        {
            if (DriverVersionIndex.TryGetValue(metadataIndex, out DriverVersion version))
            {
                return version;
            }
            else if (IsDeltaSource)
            {
                return BaselineSource.GetDriverVersion(metadataIndex);
            }
            else
            {
                throw new IndexOutOfRangeException("Driver version index cannot be found");
            }
        }

        private List<Guid> GetComputerHardwareIds(int metadataIndex)
        {
            if (MetadataToComputerHardwareIdMap.TryGetValue(metadataIndex, out List<Guid> computerHardwareIds))
            {
                return computerHardwareIds;
            }
            else if (IsDeltaSource)
            {
                return BaselineSource.GetComputerHardwareIds(metadataIndex);
            }
            else
            {
                throw new IndexOutOfRangeException("Driver version index cannot be found");
            }
        }

        private bool HasComputerHardwareIds(int metadataIndex)
        {
            if (MetadataToComputerHardwareIdMap.ContainsKey(metadataIndex))
            {
                return true;
            }
            else if (IsDeltaSource)
            {
                return BaselineSource.HasComputerHardwareIds(metadataIndex);
            }
            else
            {
                return false;
            }
        }

        private List<DriverFeatureScore> GetFeatureScores(int metadataIndex)
        {
            if (DriverFeatureScoreIndex.TryGetValue(metadataIndex, out List<DriverFeatureScore> featureScores))
            {
                return featureScores;
            }
            else if (IsDeltaSource)
            {
                return BaselineSource.GetFeatureScores(metadataIndex);
            }
            else
            {
                throw new IndexOutOfRangeException("Driver version index cannot be found");
            }
        }

        private bool HasFeatureScores(int metadataIndex)
        {
            if (DriverFeatureScoreIndex.ContainsKey(metadataIndex))
            {
                return true;
            }
            else if (IsDeltaSource)
            {
                return BaselineSource.HasFeatureScores(metadataIndex);
            }
            else
            {
                return false;
            }
        }
        #endregion
    }
}
