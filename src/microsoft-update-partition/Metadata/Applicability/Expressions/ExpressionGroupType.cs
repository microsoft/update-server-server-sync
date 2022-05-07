// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Applicability
{
    /// <summary>
    /// Represents the type of boolean logic to apply to expressions within a group
    /// </summary>
    public enum ExpressionGroupType
    {
        /// <summary>
        /// Evaluate to always true
        /// </summary>
        True,

        /// <summary>
        /// Evaluate to always false
        /// </summary>
        False,

        /// <summary>
        /// Negate the evaluated value of an expression or group
        /// </summary>
        Not,

        /// <summary>
        /// OR the evaluated expressions
        /// </summary>
        Or,

        /// <summary>
        /// AND the evaluated expressions
        /// </summary>
        And
    }
}
