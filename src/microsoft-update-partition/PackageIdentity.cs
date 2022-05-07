// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.ObjectModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata
{
    /// <summary>
    /// Represents the identity of a Microsoft Update package (update)
    /// </summary>
    public class MicrosoftUpdatePackageIdentity : IPackageIdentity
    {
        /// <summary>
        /// Gets the ID part of the identity
        /// </summary>
        /// <value>GUID identity</value>
        [JsonProperty]
        public Guid ID { get; private set; }

        /// <summary>
        /// Gets the revision part of the identity
        /// </summary>
        /// <value>Revision integer</value>
        [JsonProperty]
        public int Revision { get; private set; }

        /// <inheritdoc cref="IPackageIdentity.Partition"/>
        [JsonProperty]
        public string Partition => MicrosoftUpdatePartitionRegistration.MicrosoftUpdatePartitionName;

        /// <inheritdoc cref="IPackageIdentity.OpenId"/>
        [JsonIgnore]
        public byte[] OpenId { get; private set; }

        /// <inheritdoc cref="IPackageIdentity.OpenIdHex"/>
        [JsonIgnore]
        public string OpenIdHex => BitConverter.ToString(OpenId).Replace("-", "");

        // Keys used for fast equality comparison.

        /// <summary>
        /// Last 64 bits of the ID guid
        /// </summary>
        private UInt64 Key1;

        /// <summary>
        /// First 64 bits of the ID guid
        /// </summary>
        private UInt64 Key2;

        /// <summary>
        /// Revision number
        /// </summary>
        private Int32 Key3;

        [JsonConstructor]
        private MicrosoftUpdatePackageIdentity() { }

        /// <summary>
        /// Create an update identity from GUID and revision
        /// </summary>
        /// <param name="id">Update GUID</param>
        /// <param name="revision">Update revision</param>
        public MicrosoftUpdatePackageIdentity(Guid id, int revision)
        {
            ID = id;
            Revision = revision;
            GenerateOpenId();
            GenerateQuickLookupKeys();
        }

        private void GenerateOpenId()
        {
            using var sha512 = SHA512.Create();
            OpenId = sha512.ComputeHash(Encoding.UTF8.GetBytes($"{Partition}-{ID}-{Revision}"));
        }

        /// <summary>
        /// Re-creates the quick lookup keys after this object is deserialized. The keys are not serialized to save storage space
        /// </summary>
        /// <param name="context">Deserialization context. Not used.</param>
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            GenerateQuickLookupKeys();
            GenerateOpenId();
        }

        /// <summary>
        /// Packs the GUID and Revision into integer values for quick comparison
        /// </summary>
        private void GenerateQuickLookupKeys()
        {
            var idBytes = ID.ToByteArray().Select(b => (ulong)b).ToList();
            Key1 = (idBytes[0] << 56) | (idBytes[1] << 48) | (idBytes[2] << 40) | (idBytes[3] << 32) | (idBytes[4] << 24) | (idBytes[5] << 16) | (idBytes[6] << 8) | idBytes[7];
            Key2 = (idBytes[8] << 56) | (idBytes[9] << 48) | (idBytes[10] << 40) | (idBytes[11] << 32) | (idBytes[12] << 24) | (idBytes[13] << 16) | (idBytes[14] << 8) | idBytes[15];
            Key3 = Revision;
        }

        /// <summary>
        /// Comparison override. 
        /// </summary>
        /// <param name="obj">The other Identity object</param>
        /// <returns>
        /// <para>-1 if this instance precedes obj in the sort order</para>
        /// <para>0 if this instance occurs in the same position in the sort order as obj</para>
        /// <para>1 if this instance follows obj in the sort order. </para>
        /// </returns>
        public int CompareTo(object obj)
        {
            if (obj is not MicrosoftUpdatePackageIdentity)
            {
                return -1;
            }

            var other = obj as MicrosoftUpdatePackageIdentity;

            if ((this.Key1 > other.Key1) ||
                (this.Key1 == other.Key1 && this.Key2 > other.Key2) ||
                (this.Key1 == other.Key1 && this.Key2 == other.Key2 && this.Key3 > other.Key3))
            {
                return 1;
            }
            else if (this.Key1 == other.Key1 && this.Key2 == other.Key2 && this.Key3 == other.Key3)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Equals override. Checks that both ID and Revision match
        /// </summary>
        /// <param name="obj">The other Identity</param>
        /// <returns>True if identities are equal, false otherwise</returns>
        public override bool Equals(object obj)
        {
            if (obj is null || obj is not MicrosoftUpdatePackageIdentity)
            {
                return false;
            }

            var other = obj as MicrosoftUpdatePackageIdentity;
            return this.Key1 == other.Key1 && this.Key2 == other.Key2 && this.Key3 == other.Key3;
        }

        /// <summary>
        /// Equality operator override. Matches Equals return value;
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns>True if the two Identity objects are equal, false otherwise</returns>
        public static bool operator ==(MicrosoftUpdatePackageIdentity lhs, MicrosoftUpdatePackageIdentity rhs)
        {
            if (lhs is null)
            {
                return rhs is null;
            }
            else
            {
                return lhs.Equals(rhs);
            }
        }

        /// <summary>
        /// Inequality operator override. The reverse of Equals.
        /// </summary>
        /// <param name="lhs">Left Identity</param>
        /// <param name="rhs">Right Identity</param>
        /// <returns>True if the two Identity objects are not equal, false otherwise.</returns>
        public static bool operator !=(MicrosoftUpdatePackageIdentity lhs, MicrosoftUpdatePackageIdentity rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Returns a hash code based on both ID and Revision.
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            return ID.GetHashCode() | Revision;
        }

        /// <summary>
        /// Returns a string representation of the Identity, based on ID and Revision
        /// </summary>
        /// <returns>Identity string</returns>
        public override string ToString()
        {
            return $"{Partition}:{ID}:{Revision}";
        }

        /// <summary>
        /// Create update identity from string representation
        /// </summary>
        /// <param name="packageIdentityString">Update ID in string form</param>
        /// <returns>Microsoft update package identity</returns>
        /// <exception cref="FormatException">If the string does not have the required format</exception>
        public static MicrosoftUpdatePackageIdentity FromString(string packageIdentityString)
        {
            var split = packageIdentityString.Split(new char[] { ':' }, 3);
            if (split.Length != 3 ||
                split[0] != MicrosoftUpdatePartitionRegistration.MicrosoftUpdatePartitionName ||
                !Guid.TryParse(split[1], out var id) ||
                !int.TryParse(split[2], out var rev))
            {
                throw new FormatException("Invalid package identity string format");
            }

            return new MicrosoftUpdatePackageIdentity(id, rev);
        }
    }
}
