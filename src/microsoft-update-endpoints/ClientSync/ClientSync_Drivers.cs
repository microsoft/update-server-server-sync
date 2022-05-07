// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.UpdateServices.WebServices.ClientSync;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Drivers;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Endpoints.ClientSync
{

    public partial class ClientSyncWebService
    {
        /// <summary>
        /// Handle driver sync requests
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private Task<SyncInfo> DoDriversSync(SyncUpdateParameters parameters)
        {
            // Get list of driver updates known to the client
            var cachedDrivers = GetUpdateIdentitiesFromClientIndexes(parameters.CachedDriverIDs);

            // Get list of installed non-leaf updates. Used to match pre-requisites for driver updates
            var installedNonLeafUpdatesGuids = GetInstalledNotLeafGuidsFromSyncParameters(parameters);

            // Initialize the response
            var syncResult = new SyncInfo()
            {
                NewCookie = new Cookie() { Expiration = DateTime.Now.AddDays(5), EncryptedData = new byte[12] },
                DriverSyncNotNeeded = "false",
                Truncated = false
            };

            List<Guid> computerHardwareIds = parameters.ComputerSpec.HardwareIDs != null ? parameters.ComputerSpec.HardwareIDs.ToList() : new List<Guid>();

            List<UpdateInfo> driverUpdates = new();

            List<DriverUpdate> unapprovedDriversMatched = new();

            // Go through all client reported devices
            foreach (var device in parameters.SystemSpec)
            {
                // Combine the list hardware ids and compatible hwids; we will
                // match them in this order, from specific to less specific
                var hardwareIdsToMatch = new List<string>(device.HardwareIDs);
                if (device.CompatibleIDs != null)
                {
                    hardwareIdsToMatch.AddRange(device.CompatibleIDs);
                }

                // Get best match driver
                var driverMatchResult = DriverMatcher.MatchDriver(hardwareIdsToMatch, computerHardwareIds, installedNonLeafUpdatesGuids);

                // If we have a match and the client does not have it, add it to the list
                if (driverMatchResult != null &&
                    !cachedDrivers.Contains(driverMatchResult.Driver.Id) &&
                    !IsInstalledDriverBetterMatch(device.installedDriver, driverMatchResult, hardwareIdsToMatch, computerHardwareIds))
                {
                    if (ApprovedDriverUpdates.Contains(driverMatchResult.Driver.Id))
                    {
                        // Get core XML fragment for driver update
                        var coreXml = GetCoreFragment(driverMatchResult.Driver.Id);

                        driverUpdates.Add(new UpdateInfo()
                        {
                            Deployment = new Deployment()
                            {
                                Action = DeploymentAction.Install,
                                ID = 25000,
                                AutoDownload = "0",
                                AutoSelect = "0",
                                SupersedenceBehavior = "0",
                                IsAssigned = true,
                                LastChangeTime = "2019-08-06"
                            },
                            ID = IdToRevisionMap[driverMatchResult.Driver.Id.ID],
                            IsLeaf = true,
                            Xml = coreXml,
                            IsShared = false,
                            Verification = null
                        });
                    }
                    else
                    {
                        unapprovedDriversMatched.Add(driverMatchResult.Driver);
                    }
                }

                // Stop matching if we have max updates already
                if (driverUpdates.Count == MaxUpdatesInResponse)
                {
                    syncResult.Truncated = true;
                    break;
                }
            }

            if(unapprovedDriversMatched.Count > 0)
            {
                OnUnApprovedDriverUpdatesRequested?.Invoke(unapprovedDriversMatched);
            }

            syncResult.NewUpdates = driverUpdates.ToArray();

            return Task.FromResult(syncResult);
        }

        /// <summary>
        /// Check if the currently installed driver is a better match than the best driver we found in the updates source
        /// </summary>
        /// <param name="installedDriver">The installed driver</param>
        /// <param name="matchResult">The best driver match found in the updates source</param>
        /// <param name="hardwareIdList">The list of hardware ids for the device</param>
        /// <param name="computerHardwareIds">List of computer hardware ids</param>
        /// <returns>True if the installed driver is a better match, false otherwise</returns>
        private static bool IsInstalledDriverBetterMatch(InstalledDriver installedDriver, DriverMatchResult matchResult, List<string> hardwareIdList, List<Guid> computerHardwareIds)
        {
            if (installedDriver.MatchingComputerHWID.HasValue)
            {
                if (!matchResult.MatchedComputerHardwareId.HasValue)
                {
                    // The installed driver matched a computer HW ID while the match result did not; the installed driver is better
                    return false;
                }
                else
                {
                    // Both installed and matched driver matched a computer hardware id
                    // Compare them on how specific the match was
                    var installedDriverComputerMatchIndex = computerHardwareIds.IndexOf(installedDriver.MatchingComputerHWID.Value);
                    var matchedDriverComputerMatchIndex = computerHardwareIds.IndexOf(matchResult.MatchedComputerHardwareId.Value);

                    if (installedDriverComputerMatchIndex == matchedDriverComputerMatchIndex)
                    {
                        // The installed and matched drivers matched on the same computer hardware id;
                        // Compare them based on feature score

                        // Get the installed driver's features score
                        // A driver rank is formatted as 0xSSGGTHHH, where the value of 0x00GG0000 is the feature score
                        var installedDriverFeatureScore = (byte)((installedDriver.DriverRank & 0x00FF0000) >> 24);

                        // If the match result does not have a feature score, consider the score to be 255
                        var matchResultEffectiveFeatureScore = matchResult.MatchedFeatureScore == null ? byte.MaxValue : matchResult.MatchedFeatureScore.Score;

                        if (installedDriverFeatureScore != matchResultEffectiveFeatureScore)
                        {
                            // The installed driver is a better match if the feature score is less that the match result feature score
                            return installedDriverFeatureScore < matchResultEffectiveFeatureScore;
                        }
                    }
                    else
                    {
                        // Installed driver is better if it matched on a more specific computer hardware id (appears sooner in the list of computer hardware ids)
                        return installedDriverComputerMatchIndex < matchedDriverComputerMatchIndex;
                    }
                }
            } else if (matchResult.MatchedComputerHardwareId.HasValue)
            {
                // The installed driver did not match a computer hardware id but the match result did match
                return true;
            }

            // The installed and matched drivers have the same ranking so far; compare them by how specific is the hardware id match

            var installedDriverMatchIndex = hardwareIdList.IndexOf(installedDriver.MatchingID);
            var matchResultMatchIndex = hardwareIdList.IndexOf(matchResult.MatchedHardwareId);
            if (installedDriverMatchIndex == matchResultMatchIndex)
            {
                // Both our driver match and the installed driver matched the same HWID. Figure out the best one by comparing versions
                if (matchResult.MatchedVersion.Date == installedDriver.DriverVerDate)
                {
                    return ((ulong)installedDriver.DriverVerVersion > matchResult.MatchedVersion.Version);
                }
                else
                {
                    // The installed driver is better if it has a higher timestamp
                    return installedDriver.DriverVerDate > matchResult.MatchedVersion.Date;
                }
            }
            else
            {
                // Installed driver is better if it matched on a more specific device hardware id (appears sooner in the list of device hardware ids)
                return installedDriverMatchIndex < matchResultMatchIndex;
            }
        }
    }
}
