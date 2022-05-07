// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Drivers
{
    /// <summary>
    /// Stores driver feature score.
    /// <para>
    /// For more information, see <see href="https://docs.microsoft.com/en-us/windows-hardware/drivers/install/feature-score--windows-vista-and-later-">driver feature score documentation</see>
    /// </para>
    /// </summary>
    public class DriverFeatureScore : IComparable
    {
        /// <summary>
        /// Operating system
        /// </summary>
        /// <value>
        /// Operating system or processor architecture
        /// </value>
        public string OperatingSystem;

        /// <summary>
        /// FeatureScore; higher is better when comparing two drivers that match the same HW ID
        /// </summary>
        /// <value>
        /// Value between 0 and 255; lower is better
        /// </value>
        public byte Score;

        /// <summary>
        /// Compare two driver feature scores
        /// <para>
        /// A smaller feature score is better; if sorting feature scores, take the smaller value as the better driver
        /// </para>
        /// </summary>
        /// <param name="obj">Other object</param>
        /// <returns>-1 if other feature score is lower (better), 0 if equal and 1 if higher (worse)</returns>
        public int CompareTo(object obj)
        {
            if (obj is null)
            {
                return 1;
            }

            if (obj is DriverFeatureScore other)
            {
                return this.Score.CompareTo(other.Score);
            }
            else
            {
                return -1;
            }
        }
    }
}
