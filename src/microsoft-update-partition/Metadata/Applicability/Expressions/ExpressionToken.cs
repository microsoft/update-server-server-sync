// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Applicability
{
    /// <summary>
    /// Stores Microsoft Update expression tokens as key-value pairs.
    /// </summary>
    public class ExpressionToken
    {
        /// <summary>
        /// The token name
        /// </summary>
        [JsonProperty]
        public string Name;

        /// <summary>
        /// The string value of the token, as read from the metadata XML
        /// </summary>
        [JsonProperty]
        public string RawValue;

        /// <summary>
        /// The typed value of the token
        /// </summary>
        [JsonProperty]
        public readonly Type ValueType;

        internal ExpressionToken(string name, string rawValue, Type valueType)
        {
            Name = name;
            RawValue = rawValue;
            ValueType = valueType;
        }

        /// <summary>
        /// Implicit cast to int value
        /// </summary>
        /// <param name="token">The token to cast</param>
        public static implicit operator int(ExpressionToken token)
        {
            if (token.ValueType != typeof(int))
            {
                throw new InvalidCastException();
            }

            return Convert.ToInt32(token.RawValue);
        }

        /// <summary>
        /// Implicit cast to Int16
        /// </summary>
        /// <param name="token">The token to cast</param>
        public static implicit operator Int16(ExpressionToken token)
        {
            if (token.ValueType != typeof(Int16))
            {
                throw new InvalidCastException();
            }

            return Convert.ToInt16(token.RawValue);
        }

        /// <summary>
        /// Implicit cast to UInt16
        /// </summary>
        /// <param name="token">The token to cast</param>
        public static implicit operator UInt16(ExpressionToken token)
        {
            if (token.ValueType != typeof(UInt16))
            {
                throw new InvalidCastException();
            }

            return Convert.ToUInt16(token.RawValue);
        }

        /// <summary>
        /// Implicit cast to byte
        /// </summary>
        /// <param name="token">The token to cast</param>
        public static implicit operator byte(ExpressionToken token)
        {
            if (token.ValueType != typeof(byte))
            {
                throw new InvalidCastException();
            }

            return Convert.ToByte(token.RawValue);
        }

        /// <summary>
        /// Implicit cast to UInt36
        /// </summary>
        /// <param name="token">The token to cast</param>
        public static implicit operator UInt32(ExpressionToken token)
        {
            if (token.ValueType != typeof(UInt32))
            {
                throw new InvalidCastException();
            }

            return Convert.ToUInt32(token.RawValue);
        }

        /// <summary>
        /// Implicit cast to string
        /// </summary>
        /// <param name="token">The token to cast</param>
        public static implicit operator string(ExpressionToken token)
        {
            if (token.ValueType != typeof(string))
            {
                throw new InvalidCastException();
            }

            return token.RawValue;
        }

        /// <summary>
        /// Implicit cast to Version object
        /// </summary>
        /// <param name="token">The token to cast</param>
        public static implicit operator Version(ExpressionToken token)
        {
            if (token.ValueType != typeof(Version))
            {
                throw new InvalidCastException();
            }

            return new Version(token.RawValue);
        }

        /// <summary>
        /// Implicit cast to comparison operator. Converts from string comparison operator (e.g. "LessThan") to comparison operator
        /// </summary>
        /// <param name="token">The token to cast</param>
        public static implicit operator ComparisonOperator(ExpressionToken token)
        {
            if (token.ValueType != typeof(ComparisonOperator))
            {
                throw new InvalidCastException();
            }

            return token.RawValue switch
            {
                "LessThan" => ComparisonOperator.LessThan,
                "GreaterThanOrEqualTo" => ComparisonOperator.GreaterThanOrEqualTo,
                "EqualTo" => ComparisonOperator.EqualTo,
                "GreaterThan" => ComparisonOperator.GreaterThan,
                "LessThanOrEqualTo" => ComparisonOperator.LessThanOrEqualTo,
                "Contains" => ComparisonOperator.Contains,
                _ => throw new NotImplementedException($"Unknown comparison operator {token.RawValue}"),
            };
        }

        /// <summary>
        /// Implicit cast to bool
        /// </summary>
        /// <param name="token">The token to cast</param>
        public static implicit operator bool(ExpressionToken token)
        {
            if (token.ValueType != typeof(bool))
            {
                throw new InvalidCastException();
            }

            return token.RawValue switch
            {
                "yes" or "true" => true,
                "no" or "false" => false,
                _ => throw new NotImplementedException($"Unknown bool value {token.RawValue}"),
            };
        }

        /// <summary>
        /// Override ToString
        /// </summary>
        /// <returns>string representation for the token value</returns>
        public override string ToString()
        {
            return RawValue;
        }

        /// <summary>
        /// Override GetHashCode. Uses the token value for the hash code.
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            return RawValue.GetHashCode();
        }
    }
}
