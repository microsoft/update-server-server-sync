// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Represents the identity of an update.
    /// <para>An update's identity is the pair ID (Guid) - Revision (integer).</para>
    /// </summary>
    public class Identity : IComparable
    {
        /// <summary>
        /// The UpdateIdentity received on the wire
        /// </summary>
        [JsonProperty]
        internal UpdateIdentity Raw { get; set; }

        /// <summary>
        /// Gets the ID part of the identity
        /// </summary>
        /// <value>GUID identity</value>
        [JsonIgnore]
        public Guid ID => Raw.UpdateID;

        /// <summary>
        /// Gets the revision part of the identity
        /// </summary>
        /// <value>Revision integer</value>
        [JsonIgnore]
        public int Revision => Raw.RevisionNumber;

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

        /// <summary>
        /// Private constructor used by the deserializer
        /// </summary>
        [JsonConstructor]
        private Identity()
        {
        }

        /// <summary>
        /// Initialize an update identity from GUID and revision
        /// </summary>
        /// <param name="id">Update GUID</param>
        /// <param name="revision">Update revision</param>
        public Identity(Guid id, int revision)
        {
            Raw = new UpdateIdentity() { UpdateID = id, RevisionNumber = revision };

            GenerateQuickLookupKeys();
        }

        /// <summary>
        /// Re-creates the quick lookup keys after this object is deserialized. The keys are not serialized to save storage space
        /// </summary>
        /// <param name="context">Deserialization context. Not used.</param>
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            GenerateQuickLookupKeys();
        }

        /// <summary>
        /// Creates an identity wrapper over the on-the-wire identity.
        /// </summary>
        /// <param name="identity"></param>
        internal Identity(UpdateIdentity identity)
        {
            // Save the original Guid ID and revision
            Raw = identity;

            GenerateQuickLookupKeys();
        }

        /// <summary>
        /// Packs the GUID and Revision into integer values for quick comparison
        /// </summary>
        private void GenerateQuickLookupKeys()
        {
            var idBytes = Raw.UpdateID.ToByteArray().Select(b => (ulong)b).ToList();
            Key1 = (idBytes[0] << 56) | (idBytes[1] << 48) | (idBytes[2] << 40) | (idBytes[3] << 32) | (idBytes[4] << 24) | (idBytes[5] << 16) | (idBytes[6] << 8) | idBytes[7];
            Key2 = (idBytes[8] << 56) | (idBytes[9] << 48) | (idBytes[10] << 40) | (idBytes[11] << 32) | (idBytes[12] << 24) | (idBytes[13] << 16) | (idBytes[14] << 8) | idBytes[15];
            Key3 = Raw.RevisionNumber;
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
            if (!(obj is Identity))
            {
                return -1;
            }

            var other = obj as Identity;

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
            if (object.ReferenceEquals(obj, null) || !(obj is Identity))
            {
                return false;
            }

            var other = obj as Identity;
            return this.Key1 == other.Key1 && this.Key2 == other.Key2 && this.Key3 == other.Key3;
        }

        /// <summary>
        /// Equality operator override. Matches Equals return value;
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns>True if the two Identity objects are equal, false otherwise</returns>
        public static bool operator ==(Identity lhs, Identity rhs)
        {
            if (object.ReferenceEquals(lhs, null))
            {
                return object.ReferenceEquals(rhs, null);
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
        public static bool operator !=(Identity lhs, Identity rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Returns a hash code based on both ID and Revision.
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            return Raw.UpdateID.GetHashCode() | Raw.RevisionNumber;
        }

        /// <summary>
        /// Returns a string representation of the Identity, based on ID and Revision
        /// </summary>
        /// <returns>Identity string</returns>
        public override string ToString()
        {
            return $"{Raw.UpdateID}-{Raw.RevisionNumber}";
        }
    }
}
