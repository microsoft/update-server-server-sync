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
    /// Wraps around a WebService UpdateIdentity to add comparison and equality operations required for fast indexing
    /// </summary>
    public class MicrosoftUpdateIdentity : IComparable
    {
        /// <summary>
        /// The UpdateIdentity received on the wire
        /// </summary>
        [JsonProperty]
        public UpdateIdentity Raw { get; private set; }

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
        private MicrosoftUpdateIdentity()
        {
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
        public MicrosoftUpdateIdentity(UpdateIdentity identity)
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

        public int CompareTo(object obj)
        {
            if (!(obj is MicrosoftUpdateIdentity))
            {
                return -1;
            }

            var other = obj as MicrosoftUpdateIdentity;

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

        public override bool Equals(object obj)
        {
            if (!(obj is MicrosoftUpdateIdentity))
            {
                return false;
            }

            var other = obj as MicrosoftUpdateIdentity;
            return this.Key1 == other.Key1 && this.Key2 == other.Key2 && this.Key3 == other.Key3;
        }

        public static bool operator ==(MicrosoftUpdateIdentity lhs, MicrosoftUpdateIdentity rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(MicrosoftUpdateIdentity lhs, MicrosoftUpdateIdentity rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            return Raw.UpdateID.GetHashCode() | Raw.RevisionNumber;
        }

        public override string ToString()
        {
            return $"{Raw.UpdateID}-{Raw.RevisionNumber}";
        }
    }
}
