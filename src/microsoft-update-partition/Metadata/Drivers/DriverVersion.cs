// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Drivers
{
    /// <summary>
    /// Stores driver versioning data, comprised of driver date and 4 part version.
    /// Implements custom comparison and equality check for driver version
    /// </summary>
    public class DriverVersion : IComparable
    {
        /// <summary>
        /// Drive date
        /// </summary>
        public DateTime Date;

        /// <summary>
        /// Driver version; 4 part version
        /// </summary>
        public ulong Version;

        /// <summary>
        /// Gets the driver version in 4 part string format
        /// </summary>
        /// <value>
        /// Driver version in 4 part format</value>
        [JsonIgnore]
        public string VersionString => $"{Version >> 48}.{(0x0000FFFFFFFFFFFF & Version) >> 32}.{(0x00000000FFFFFFFF & Version) >> 16}.{0x000000000000FFFF & Version}";

        /// <summary>
        /// Parses a driver date string into a DateTime. The date string is expected to be in the format yyyy-mm-dd
        /// </summary>
        /// <param name="dateString">Date string</param>
        /// <returns>DateTime parsed from the input string</returns>
        internal static DateTime ParseDateFromString(string dateString)
        {
            return DateTime.ParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parses a 4-part version string x.y.z.y into a 64 bit version
        /// </summary>
        /// <param name="versionString">4 part version string</param>
        /// <returns>64 bit version</returns>
        internal static ulong ParseVersionFromString(string versionString)
        {
            string versionPattern = @"^(?<major>[\d]+)\.(?<minor>[\d]+)\.(?<revision>[\d]+)\.(?<build>[\d]+)$";
            var versionMatch = Regex.Match(versionString, versionPattern);

            if (!versionMatch.Success)
            {
                throw new Exception("Unexpected driver version");
            }

            return ((ulong)ushort.Parse(versionMatch.Groups["major"].Value) << 48)
                + ((ulong)ushort.Parse(versionMatch.Groups["minor"].Value) << 32)
                + ((ulong)ushort.Parse(versionMatch.Groups["revision"].Value) << 16)
                + ((ulong)ushort.Parse(versionMatch.Groups["build"].Value));
        }

        /// <summary>
        /// Compare two driver versions by date and version
        /// </summary>
        /// <param name="obj">Other driver version to compare to</param>
        /// <returns>-1, 0, 1 for less than, equal and greated than respectively</returns>
        public int CompareTo(object obj)
        {
            if (obj is null)
            {
                return 1;
            }

            if (obj is not DriverVersion)
            {
                return -1;
            }

            var other = obj as DriverVersion;

            if (Date == other.Date && Version == other.Version)
            {
                return 0;
            }
            else if (Date == other.Date)
            {
                return Version.CompareTo(other.Version);
            }
            else
            {
                return Date.CompareTo(other.Date);
            }
        }

        /// <summary>
        /// Equality override for driver versions.
        /// </summary>
        /// <param name="obj">Other driver version to check equality with</param>
        /// <returns>True if versions match (same date and version), false otherwise</returns>
        public override bool Equals(object obj)
        {
            return this.CompareTo(obj) == 0;
        }

        /// <summary>
        /// GetHashCode override
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            return Date.GetHashCode() | Version.GetHashCode();
        }
    }
}
