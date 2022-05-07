// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Drivers
{
    /// <summary>
    /// The result of finding the best driver update for a device. 
    /// <para>
    /// Contains ranking information that can be used to determine if the matched driver update is better than an installed driver.
    /// </para>
    /// </summary>
    public class DriverMatchResult
    {
        /// <summary>
        /// The matched driver
        /// </summary>
        /// <value>
        /// An update of type Driver
        /// </value>
        public DriverUpdate Driver;

        /// <summary>
        /// The version of the matched driver
        /// </summary>
        /// <value>
        /// Driver version consisting of timestamp and 4 part version
        /// </value>
        public DriverVersion MatchedVersion;

        /// <summary>
        /// The feature score of the matched driver if available; null otherwise
        /// </summary>
        /// <value>
        /// Driver feature score
        /// </value>
        public DriverFeatureScore MatchedFeatureScore;

        /// <summary>
        /// The most specific device hardwared ID that was matched
        /// </summary>
        /// <value>
        /// Hardware ID string
        /// </value>
        public string MatchedHardwareId;

        /// <summary>
        /// The computer hardware ID that was matched, if any. Null otherwise
        /// </summary>
        /// <value>
        /// Computer hardware ID (GUID)
        /// </value>
        public Guid? MatchedComputerHardwareId;

        /// <summary>
        /// Create a driver match result for a device
        /// </summary>
        /// <param name="driver">The driver that matched a device hardware id</param>
        public DriverMatchResult(DriverUpdate driver)
        {
            Driver = driver;
        }
    }
}
