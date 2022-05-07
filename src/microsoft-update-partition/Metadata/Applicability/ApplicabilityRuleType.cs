// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Applicability
{
    /// <summary>
    /// Possible types of applicability rules used by Microsoft Update
    /// </summary>
    public enum ApplicabilityRuleType
    {
        /// <summary>
        /// Checks if an update is installed
        /// </summary>
        IsInstalled,
        
        /// <summary>
        /// Checks if an update is installable
        /// </summary>
        IsInstallable,

        /// <summary>
        /// This rules stores metadata used by CBS-based rules.
        /// <para>
        /// This rule is expected to exist when a CBS rule is present
        /// </para>
        /// </summary>
        CbsPackageApplicabilityMetadata,

        /// <summary>
        /// This rules only stores metadata used by the MSI-based rules.
        /// <para>
        /// This rule is expected to exist when a MSI rule is present
        /// </para>
        /// </summary>
        MsiPatchMetadata,

        /// <summary>
        /// This rules only stores metadata used by the MSI-based rules.
        /// <para>
        /// This rule is expected to exist when a MSI rule is present
        /// </para>
        /// </summary>
        MsiApplicationMetadata,

        /// <summary>
        /// This rules only stores metadata used by the driver-based rules.
        /// <para>
        /// This rule is expected to always exists when a driver rule is present
        /// </para>
        /// </summary>
        WindowsDriverMetadata,

        /// <summary>
        /// Evaluates applicability for a driver
        /// </summary>
        WindowsDriver,

        /// <summary>
        /// Checks applicability based on windows version
        /// </summary>
        WindowsVersion,

        /// <summary>
        /// Checks applicability based on superseded state
        /// </summary>
        IsSuperseded
    }
}
