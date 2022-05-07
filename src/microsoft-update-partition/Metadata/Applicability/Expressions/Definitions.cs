// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Applicability
{
    enum TokenRequirements
    {
        Required,
        Optional
    }

    /// <summary>
    /// Comparison operators used in Microsoft Update applicability expressions
    /// </summary>
    public enum ComparisonOperator
    {
        /// <summary>
        /// Less than
        /// </summary>
        LessThan,

        /// <summary>
        /// Greater or equal to
        /// </summary>
        GreaterThanOrEqualTo,

        /// <summary>
        /// Equal to
        /// </summary>
        EqualTo,

        /// <summary>
        /// Greater than
        /// </summary>
        GreaterThan,

        /// <summary>
        /// Less or equal to
        /// </summary>
        LessThanOrEqualTo,

        /// <summary>
        /// String contains
        /// </summary>
        Contains
    }

    /// <summary>
    /// Microsoft Update applicability expression types
    /// </summary>
    public enum ExpressionType
    {
        /// <summary>
        /// Processor type queries
        /// </summary>
        Processor,

        /// <summary>
        /// Windows version expression
        /// </summary>
        WindowsVersion,

        /// <summary>
        /// Platform version expression
        /// </summary>
        PlatformVersion,

        /// <summary>
        /// Platform type expression
        /// </summary>
        Platform,

        /// <summary>
        /// Windows language expression
        /// </summary>
        WindowsLanguage,

        /// <summary>
        /// MUI language installed expression
        /// </summary>
        MuiInstalled,

        /// <summary>
        /// Registry value exists expression
        /// </summary>
        RegValueExists,

        /// <summary>
        /// Convert registry string to version string
        /// </summary>
        RegSzToVersion,

        /// <summary>
        /// Registry string value expression
        /// </summary>
        RegSz,

        /// <summary>
        /// Expandable registry string expression
        /// </summary>
        RegExpandSz,

        /// <summary>
        /// Registry DWORD expression
        /// </summary>
        RegDword,

        /// <summary>
        /// Registry key exists expression
        /// </summary>
        RegKeyExists,

        /// <summary>
        /// Multi registry key expression
        /// </summary>
        RegKeyLoop,

        /// <summary>
        /// Substring in registry string expression
        /// </summary>
        WUv4RegKeySubstring,

        /// <summary>
        /// Generic registry value expression
        /// </summary>
        WUv4RegKeyValue,

        /// <summary>
        /// File version expression
        /// </summary>
        FileVersion,

        /// <summary>
        /// File size expression
        /// </summary>
        FileSize,

        /// <summary>
        /// File size, with the file name prefix coming from a registry string value
        /// </summary>
        FileSizePrependRegSz,

        /// <summary>
        /// File exists expression
        /// </summary>
        FileExists,

        /// <summary>
        /// File exists expression, with file name prefix coming fom a registry string value
        /// </summary>
        FileExistsPrependRegSz,

        /// <summary>
        /// File create expression
        /// </summary>
        FileCreated,

        /// <summary>
        /// File create expression, with file name prefix coming fom a registry string value
        /// </summary>
        FileCreatedPrependRegSz,

        /// <summary>
        /// File create expression, with file version prefix coming fom a registry string value
        /// </summary>
        FileVersionPrependRegSz,

        /// <summary>
        /// File modified expression
        /// </summary>
        FileModified,

        /// <summary>
        /// File modified expression, with file name prefix coming fom a registry string value
        /// </summary>
        FileModifiedPrependRegSz,

        /// <summary>
        /// WMI query expression
        /// </summary>
        WmiQuery,

        /// <summary>
        /// Windows license expression
        /// </summary>
        LicenseDword,

        /// <summary>
        /// CBS package installed expression
        /// </summary>
        CbsPackageInstalledByIdentity,

        /// <summary>
        /// CBS package installed expression
        /// </summary>
        CbsPackageInstalled,

        /// <summary>
        /// CBS package installable expression
        /// </summary>
        CbsPackageInstallable,

        /// <summary>
        /// This expression stores metadata used by the CBS package-level expressions
        /// </summary>
        CbsPackageApplicabilityMetadata,

        /// <summary>
        /// MSI component installed by product
        /// </summary>
        MsiComponentInstalledForProduct,

        /// <summary>
        /// Component metadata expression
        /// </summary>
        Component,

        /// <summary>
        /// Product metadata expression
        /// </summary>
        Product,

        /// <summary>
        /// Product code based expression
        /// </summary>
        ProductCode,

        /// <summary>
        /// Feature name based expression
        /// </summary>
        Feature,

        /// <summary>
        /// MSI patch installed by product name expression
        /// </summary>
        MsiPatchInstalledForProduct,

        /// <summary>
        /// MSI patch installed expression
        /// </summary>
        MsiPatchInstalled,

        /// <summary>
        /// Msi patch superseded expression
        /// </summary>
        MsiPatchSuperseded,

        /// <summary>
        /// MSI patch installable expression
        /// </summary>
        MsiPatchInstallable,

        /// <summary>
        /// This expression stores metadata used by the MSI patch based expressions
        /// </summary>
        MsiPatchMetadata,

        /// <summary>
        /// This expression stores metadata used by the MSI based expressions
        /// </summary>
        MsiApplicationMetadata,

        /// <summary>
        /// MSI product installed expression. Detects installed MSI products
        /// </summary>
        MsiProductInstalled,

        /// <summary>
        /// MSI feature installed expression. Detects installed features for a product
        /// </summary>
        MsiFeatureInstalledForProduct,

        /// <summary>
        /// MSI app installed expression. Detects installed MSI applications
        /// </summary>
        MsiApplicationInstalled,

        /// <summary>
        /// MSI app installable expression. Detected whether a MSI application can be installed
        /// </summary>
        MsiApplicationInstallable,

        /// <summary>
        /// Constant rule that always evaluates to false
        /// </summary>
        False,

        /// <summary>
        /// Constant rule that always evaluates to true
        /// </summary>
        True,

        /// <summary>
        /// Expression that evaluates based on data from GetSystemMetric API
        /// </summary>
        SystemMetric,

        /// <summary>
        /// Expression that evaluates based on D3D version available
        /// </summary>
        Direct3D,

        /// <summary>
        /// Expression that evaluates the mount of video memory available
        /// </summary>
        VideoMemory,

        /// <summary>
        /// Expression that evaluates the presence of a specified hardware sensor (accelerometer, etc.)
        /// </summary>
        SensorById,

        /// <summary>
        /// Expression that evaluates the amount of total RAM present on a device
        /// </summary>
        Memory,

        /// <summary>
        /// Expression that evaluates the presence of NFC capabilities
        /// </summary>
        NFC,

        /// <summary>
        /// Expression that evaluates the presence of a camera
        /// </summary>
        Camera,

        /// <summary>
        /// Expression that evaluates whether a release is installed
        /// </summary>
        ProductReleaseInstalled,

        /// <summary>
        /// Expression that evaluates the version of the release installed
        /// </summary>
        ProductReleaseVersion,

        /// <summary>
        /// Evaluates whether a driver is installed
        /// </summary>
        WindowsDriverInstalled,

        /// <summary>
        /// Expression for determining driver supersedence
        /// </summary>
        WindowsDriverSuperseded,

        /// <summary>
        /// Expression for evaluating whether a driver can be installed on a device
        /// </summary>
        WindowsDriverInstallable,

        /// <summary>
        /// Expression that stores metadata for other driver-metadata based expressions
        /// </summary>
        WindowsDriverMetaData,

        /// <summary>
        /// Expression the evaluates the feature score of the device
        /// </summary>
        FeatureScore,

        /// <summary>
        /// Expression that evaluates distribution hardware ids
        /// </summary>
        DistributionComputerHardwareId,

        /// <summary>
        /// Expression that evaluates target hardware IDs
        /// </summary>
        TargetComputerHardwareId,

        /// <summary>
        /// N/A
        /// </summary>
        CompatibleProvider,

        /// <summary>
        /// N/A
        /// </summary>
        WindowsDriver,

        /// <summary>
        /// Expression that checks if the version of Windows is within a range
        /// </summary>
        InstalledVersionRange,

        /// <summary>
        /// Expression that evaluates if running on a cluster
        /// </summary>
        ClusteredOS,

        /// <summary>
        /// Expression that evaluates if running on a cluster owner
        /// </summary>
        ClusterResourceOwner
    }

    class TokenDefinition
    {
        public readonly string Key;
        public readonly TokenRequirements Requirement;
        public readonly Type TargetType;

        public TokenDefinition(string key, TokenRequirements requirement, Type T)
        {
            Key = key;
            Requirement = requirement;
            TargetType = T;
        }
    }

    static class KnownExpressionDefinitions
    {
        private readonly static List<TokenDefinition> WindowsVersionTokens = new()
        {
            new TokenDefinition("MajorVersion", TokenRequirements.Optional, typeof(Int32)),
            new TokenDefinition("MinorVersion", TokenRequirements.Optional, typeof(Int32)),
            new TokenDefinition("BuildNumber", TokenRequirements.Optional, typeof(Int32)),
            new TokenDefinition("Comparison", TokenRequirements.Optional, typeof(ComparisonOperator)),
            new TokenDefinition("ProductType", TokenRequirements.Optional, typeof(byte)),
            new TokenDefinition("SuiteMask", TokenRequirements.Optional, typeof(UInt16)),
            new TokenDefinition("AllSuitesMustBePresent", TokenRequirements.Optional, typeof(bool)),
            new TokenDefinition("ServicePackMajor", TokenRequirements.Optional, typeof(UInt16)),
        };

        public static Dictionary<string, List<TokenDefinition>> NameToDefinitionMap = new()
        {
            {
                "bar:Processor",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Architecture", TokenRequirements.Required, typeof(Int16))
                }
            },
            { "bar:WindowsVersion", WindowsVersionTokens },
            { "bar:PlatformVersion", WindowsVersionTokens },
            {
                "bar:Platform",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("PlatformID", TokenRequirements.Required, typeof(string))
                }
            },
            {
                "bar:WindowsLanguage",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Language", TokenRequirements.Optional, typeof(string)),
                }
            },
            { "bar:MuiInstalled", new List<TokenDefinition>() { } },
            {
                "bar:RegValueExists",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Key", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Subkey", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Value", TokenRequirements.Optional, typeof(string)),
                    new TokenDefinition("Type", TokenRequirements.Optional, typeof(string)),
                    new TokenDefinition("RegType32", TokenRequirements.Optional, typeof(bool))
                }
            },
            {
                "bar:RegSzToVersion",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Key", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Subkey", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Value", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Data", TokenRequirements.Required, typeof(Version)),
                    new TokenDefinition("Comparison", TokenRequirements.Required, typeof(ComparisonOperator)),
                    new TokenDefinition("RegType32", TokenRequirements.Optional, typeof(bool))
                }
            },
            {
                "bar:RegSz",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Key", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Subkey", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Value", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Data", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Comparison", TokenRequirements.Required, typeof(ComparisonOperator)),
                    new TokenDefinition("RegType32", TokenRequirements.Optional, typeof(bool))
                }
            },
            {
                "bar:RegExpandSz",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Key", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Subkey", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Value", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Data", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Comparison", TokenRequirements.Required, typeof(ComparisonOperator)),
                    new TokenDefinition("RegType32", TokenRequirements.Optional, typeof(bool))
                }
            },

            {
                "bar:RegDword",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Key", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Subkey", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Value", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Data", TokenRequirements.Required, typeof(UInt32)),
                    new TokenDefinition("Comparison", TokenRequirements.Required, typeof(ComparisonOperator)),
                    new TokenDefinition("RegType32", TokenRequirements.Optional, typeof(bool))
                }
            },
            {
                "bar:RegKeyExists",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Key", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Subkey", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("RegType32", TokenRequirements.Optional, typeof(bool))
                }
            },
            {
                "bar:RegKeyLoop",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Key", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Subkey", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("TrueIf", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("RegType32", TokenRequirements.Optional, typeof(bool))
                }
            },
            {
                "bar:WUv4RegKeySubstring",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Key", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Subkey", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Value", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Data", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("RegType32", TokenRequirements.Optional, typeof(bool)),
                }
            },
            {
                "bar:WUv4RegKeyValue",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Key", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Subkey", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Value", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Data", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("RegType32", TokenRequirements.Optional, typeof(bool)),
                }
            },
            {
                "bar:FileVersion",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Path", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Version", TokenRequirements.Required, typeof(Version)),
                    new TokenDefinition("Comparison", TokenRequirements.Required, typeof(ComparisonOperator)),
                    new TokenDefinition("Csidl", TokenRequirements.Optional, typeof(int))
                }
            },
            {
                "bar:FileSize",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Path", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Comparison", TokenRequirements.Required, typeof(ComparisonOperator)),
                    new TokenDefinition("Csidl", TokenRequirements.Optional, typeof(int)),
                    new TokenDefinition("Size", TokenRequirements.Required, typeof(int)),
                }
            },
            {
                "bar:FileSizePrependRegSz",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Path", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Comparison", TokenRequirements.Required, typeof(ComparisonOperator)),
                    new TokenDefinition("Size", TokenRequirements.Required, typeof(int)),
                    new TokenDefinition("Key", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Subkey", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Value", TokenRequirements.Optional, typeof(string)),
                    new TokenDefinition("RegType32", TokenRequirements.Optional, typeof(bool)),
                }
            },
            {
                "bar:FileExists",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Path", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Csidl", TokenRequirements.Optional, typeof(int)),
                    new TokenDefinition("Size", TokenRequirements.Optional, typeof(int)),
                    new TokenDefinition("Language", TokenRequirements.Optional, typeof(int)),
                }
            },
            {
                "bar:FileExistsPrependRegSz",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Path", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Key", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Subkey", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Value", TokenRequirements.Optional, typeof(string)),
                    new TokenDefinition("RegType32", TokenRequirements.Optional, typeof(bool)),
                    new TokenDefinition("Version", TokenRequirements.Optional, typeof(string)),
                    new TokenDefinition("Language", TokenRequirements.Optional, typeof(string)),
                }
            },
            {
                "bar:FileCreated",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Path", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Comparison", TokenRequirements.Required, typeof(ComparisonOperator)),
                    new TokenDefinition("Created", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Csidl", TokenRequirements.Optional, typeof(int)),
                }
            },
            {
                "bar:FileCreatedPrependRegSz",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Path", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Comparison", TokenRequirements.Required, typeof(ComparisonOperator)),
                    new TokenDefinition("Created", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Key", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Subkey", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Value", TokenRequirements.Optional, typeof(string)),
                    new TokenDefinition("RegType32", TokenRequirements.Optional, typeof(bool))
                }
            },
            {
                "bar:FileVersionPrependRegSz",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Path", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Key", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Subkey", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Value", TokenRequirements.Optional, typeof(string)),
                    new TokenDefinition("RegType32", TokenRequirements.Optional, typeof(bool)),
                    new TokenDefinition("Version", TokenRequirements.Required, typeof(Version)),
                    new TokenDefinition("Comparison", TokenRequirements.Required, typeof(ComparisonOperator))
                }
            },
            {
                "bar:FileModified",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Csidl", TokenRequirements.Optional, typeof(int)),
                    new TokenDefinition("Modified", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Comparison", TokenRequirements.Required, typeof(ComparisonOperator)),
                    new TokenDefinition("Path", TokenRequirements.Required, typeof(string)),
                }
            },
            {
                "bar:FileModifiedPrependRegSz",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Path", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Comparison", TokenRequirements.Required, typeof(ComparisonOperator)),
                    new TokenDefinition("Modified", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Key", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Subkey", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Value", TokenRequirements.Optional, typeof(string)),
                    new TokenDefinition("RegType32", TokenRequirements.Optional, typeof(bool))
                }
            },
            {
                "bar:WmiQuery",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Namespace", TokenRequirements.Required, typeof(string)),
                new TokenDefinition("WqlQuery", TokenRequirements.Required, typeof(string))
                }
            },
            {
                "bar:LicenseDword",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Value", TokenRequirements.Optional, typeof(string)),
                new TokenDefinition("Comparison", TokenRequirements.Optional, typeof(ComparisonOperator)),
                new TokenDefinition("Data", TokenRequirements.Optional, typeof(UInt32)),
                }
            },
            {
                "cbsar:CbsPackageInstalledByIdentity",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("PackageIdentity", TokenRequirements.Required, typeof(string))
                }
            },
            {
                "cbsar:CbsPackageInstalled",
                new List<TokenDefinition>()
                {
                }
            },
            {
                "cbsar:CbsPackageInstallable",
                new List<TokenDefinition>()
                {
                }
            },
            {
                "cbsar:CbsPackageApplicabilityMetadata",
                new List<TokenDefinition>()
                {
                    // Grab the inner XML of the node; it contains CBS assembly XML
                    new TokenDefinition("+", TokenRequirements.Required, typeof(string))
                }
            },
            {
                "mar:MsiComponentInstalledForProduct",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("AllComponentsRequired", TokenRequirements.Required, typeof(bool)),
                    new TokenDefinition("AllProductsRequired", TokenRequirements.Optional, typeof(bool)),
                }
            },
            {
                "mar:Component",
                new List<TokenDefinition>()
                {
                    // Grab the value of the node
                    new TokenDefinition("*", TokenRequirements.Required, typeof(string)),
                }
            },
            {
                "mar:Product",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("*", TokenRequirements.Required, typeof(string)),
                }
            },
            {
                "mar:ProductCode",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Version", TokenRequirements.Optional, typeof(string)),
                    new TokenDefinition("UseRecacheReinstall", TokenRequirements.Optional, typeof(string)),
                    new TokenDefinition("*", TokenRequirements.Required, typeof(string)),
                }
            },
            {
                "mar:Feature",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("*", TokenRequirements.Required, typeof(string)),
                }
            },
            {
                "mar:MsiPatchInstalledForProduct",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("PatchCode", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("ProductCode", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("VersionMax", TokenRequirements.Optional, typeof(string)),
                    new TokenDefinition("VersionMin", TokenRequirements.Optional, typeof(string)),
                    new TokenDefinition("Language", TokenRequirements.Optional, typeof(string)),
                }
            },
            {
                "mar:MsiPatchInstalled",
                new List<TokenDefinition>()
                {
                }
            },
            {
                "mar:MsiPatchSuperseded",
                new List<TokenDefinition>()
                {
                }
            },
            {
                "mar:MsiPatchInstallable",
                new List<TokenDefinition>()
                {
                }
            },
            {
                "mar:MsiPatchMetadata",
                new List<TokenDefinition>()
                {
                    // Grab the inner XML of the node; it contains MSI patch XML
                    new TokenDefinition("+", TokenRequirements.Required, typeof(string))
                }
            },
            {
                "mar:MsiApplicationMetadata",
                new List<TokenDefinition>()
                {
                }
            },
            {
                "mar:MsiProductInstalled",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("ProductCode", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("VersionMin", TokenRequirements.Optional, typeof(string)),
                    new TokenDefinition("VersionMax", TokenRequirements.Optional, typeof(string)),
                    new TokenDefinition("ExcludeVersionMax", TokenRequirements.Optional, typeof(bool)),
                    new TokenDefinition("ExcludeVersionMin", TokenRequirements.Optional, typeof(bool)),
                    new TokenDefinition("Language", TokenRequirements.Optional, typeof(int))
                }
            },
            {
                "msiar:MsiProductInstalled",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("ProductCode", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("VersionMin", TokenRequirements.Optional, typeof(string)),
                    new TokenDefinition("VersionMax", TokenRequirements.Optional, typeof(string)),
                    new TokenDefinition("ExcludeVersionMax", TokenRequirements.Optional, typeof(bool)),
                    new TokenDefinition("ExcludeVersionMin", TokenRequirements.Optional, typeof(bool)),
                    new TokenDefinition("Language", TokenRequirements.Optional, typeof(int))
                }
            },
            {
                "mar:MsiFeatureInstalledForProduct",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("AllFeaturesRequired", TokenRequirements.Optional, typeof(bool)),
                    new TokenDefinition("AllProductsRequired", TokenRequirements.Optional, typeof(bool)),
                }
            },
            {
                "mar:MsiApplicationInstalled",
                new List<TokenDefinition>()
                {
                }
            },
            {
                "mar:MsiApplicationInstallable",
                new List<TokenDefinition>()
                {
                }
            },
            {
                "lar:False",
                new List<TokenDefinition>()
                {
                }
            },
            {
                "lar:True",
                new List<TokenDefinition>()
                {
                }
            },
            {
                "bar:SystemMetric",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Index", TokenRequirements.Optional, typeof(Int32)),
                    new TokenDefinition("Comparison", TokenRequirements.Optional, typeof(ComparisonOperator)),
                    new TokenDefinition("Value", TokenRequirements.Optional, typeof(Int32)),
                }
            },
            {
                "bar:Direct3D",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("HardwareVersion", TokenRequirements.Required, typeof(UInt32)),
                    new TokenDefinition("FeatureLevelMajor", TokenRequirements.Required, typeof(UInt32)),
                    new TokenDefinition("FeatureLevelMinor", TokenRequirements.Required, typeof(UInt32)),
                }
            },
            {
                "bar:Memory",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("MinSizeInMB", TokenRequirements.Required, typeof(UInt32)),
                    new TokenDefinition("Type", TokenRequirements.Required, typeof(string))
                }
            },
            {
                "bar:VideoMemory",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("MinSizeInMB", TokenRequirements.Required, typeof(UInt32))
                }
            },
            {
                "bar:SensorById",
                new List<TokenDefinition>()
                {
                     new TokenDefinition("Id", TokenRequirements.Required, typeof(string))
                }
            },
            {
                "bar:NFC",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Capability", TokenRequirements.Required, typeof(string))
                }
            },
            {
                "bar:Camera",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Location", TokenRequirements.Required, typeof(string))
                }
            },
            {
                "ProductReleaseInstalled",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Version", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Name", TokenRequirements.Required, typeof(string)),
                }
            },
            {
                "upd:ProductReleaseVersion",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Version", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Name", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Comparison", TokenRequirements.Required, typeof(ComparisonOperator)),
                }
            },
            {
                "drv:WindowsDriverInstalled",
                new List<TokenDefinition>()
                {

                }
            },
            {
                "drv:WindowsDriverSuperseded",
                new List<TokenDefinition>()
                {

                }
            },
            {
                "drv:WindowsDriverInstallable",
                new List<TokenDefinition>()
                {

                }
            },
            {
                "drv:WindowsDriverMetaData",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("HardwareID", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Model", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("WhqlDriverID", TokenRequirements.Required, typeof(int)),
                    new TokenDefinition("Manufacturer", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Company", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Provider", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("DriverVerDate", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("DriverVerVersion", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Class", TokenRequirements.Required, typeof(string))
                }
            },
            {
                "drv:FeatureScore",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("FeatureScore", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("OperatingSystem", TokenRequirements.Required, typeof(string)),
                }
            },
            {
                "drv:DistributionComputerHardwareId",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("*", TokenRequirements.Required, typeof(string)),
                }
            },
            {
                "drv:TargetComputerHardwareId",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("*", TokenRequirements.Required, typeof(string)),
                }
            },
            {
                "drv:CompatibleProvider",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("*", TokenRequirements.Required, typeof(string)),
                }
            },
            {
                "drv:WindowsDriver",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("OemOnly", TokenRequirements.Optional, typeof(bool)),
                }
            },
            {
                "drv:InstalledVersionRange",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Max", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Min", TokenRequirements.Required, typeof(string)),
                }
            },
            {
                "bar:ClusteredOS",
                new List<TokenDefinition>()
                {
                }
            },
            {
                "bar:ClusterResourceOwner",
                new List<TokenDefinition>()
                {
                    new TokenDefinition("Key", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Subkey", TokenRequirements.Required, typeof(string)),
                    new TokenDefinition("Value", TokenRequirements.Optional, typeof(string)),
                    new TokenDefinition("RegType32", TokenRequirements.Optional, typeof(bool)),
                    new TokenDefinition("Prefix", TokenRequirements.Optional, typeof(string)),
                    new TokenDefinition("Suffix", TokenRequirements.Optional, typeof(string)),
                }
            },
        };

        public static Dictionary<string, ExpressionType> NameToTypeMap = new()
        {
            { "bar:Processor", ExpressionType.Processor },
            { "bar:WindowsVersion", ExpressionType.WindowsVersion },
            { "bar:PlatformVersion", ExpressionType.PlatformVersion },
            { "bar:Platform", ExpressionType.Platform },
            { "bar:WindowsLanguage", ExpressionType.WindowsLanguage },
            { "bar:MuiInstalled", ExpressionType.WindowsLanguage },
            { "bar:RegValueExists", ExpressionType.RegValueExists },
            { "bar:RegSzToVersion",ExpressionType.RegSzToVersion },
            { "bar:RegSz",ExpressionType.RegSz },
            { "bar:RegExpandSz",ExpressionType.RegExpandSz },
            { "bar:RegDword",ExpressionType.RegDword },
            {  "bar:RegKeyExists",ExpressionType.RegKeyExists },
            { "bar:RegKeyLoop",ExpressionType.RegKeyLoop },
            { "bar:WUv4RegKeySubstring",ExpressionType.WUv4RegKeySubstring },
            { "bar:WUv4RegKeyValue",ExpressionType.WUv4RegKeyValue },
            { "bar:FileVersion",ExpressionType.FileVersion },
            { "bar:FileSize",ExpressionType.FileSize },
            { "bar:FileSizePrependRegSz",ExpressionType.FileSizePrependRegSz },
            { "bar:FileExists",ExpressionType.FileExists },
            { "bar:FileExistsPrependRegSz",ExpressionType.FileExistsPrependRegSz },
            { "bar:FileCreated",ExpressionType.FileCreated },
            { "bar:FileCreatedPrependRegSz",ExpressionType.FileCreatedPrependRegSz },
            { "bar:FileVersionPrependRegSz",ExpressionType.FileVersionPrependRegSz },
            { "bar:FileModified",ExpressionType.FileModified },
            { "bar:FileModifiedPrependRegSz",ExpressionType.FileModifiedPrependRegSz },
            { "bar:WmiQuery",ExpressionType.WmiQuery },
            { "bar:LicenseDword",ExpressionType.LicenseDword },
            { "cbsar:CbsPackageInstalledByIdentity",ExpressionType.CbsPackageInstalledByIdentity },
            { "cbsar:CbsPackageInstalled",ExpressionType.CbsPackageInstalled },
            { "cbsar:CbsPackageInstallable",ExpressionType.CbsPackageInstallable },
            { "cbsar:CbsPackageApplicabilityMetadata",ExpressionType.CbsPackageApplicabilityMetadata },
            { "mar:MsiComponentInstalledForProduct",ExpressionType.MsiComponentInstalledForProduct },
            { "mar:Component",ExpressionType.Component },
            { "mar:Product",ExpressionType.Product },
            { "mar:ProductCode",ExpressionType.ProductCode },
            { "mar:Feature", ExpressionType.Feature },
            { "mar:MsiPatchInstalledForProduct",ExpressionType.MsiPatchInstalledForProduct },
            { "mar:MsiPatchInstalled",ExpressionType.MsiPatchInstalled },
            { "mar:MsiPatchSuperseded",ExpressionType.MsiPatchSuperseded },
            { "mar:MsiPatchInstallable",ExpressionType.MsiPatchInstallable },
            { "mar:MsiPatchMetadata", ExpressionType.MsiPatchMetadata },
            { "mar:MsiApplicationMetadata", ExpressionType.MsiApplicationMetadata },
            { "mar:MsiProductInstalled", ExpressionType.MsiProductInstalled },
            { "msiar:MsiProductInstalled", ExpressionType.MsiProductInstalled },
            { "mar:MsiFeatureInstalledForProduct", ExpressionType.MsiFeatureInstalledForProduct },
            { "mar:MsiApplicationInstalled", ExpressionType.MsiApplicationInstalled },
            { "mar:MsiApplicationInstallable", ExpressionType.MsiApplicationInstallable },
            { "lar:False", ExpressionType.False },
            { "lar:True", ExpressionType.True },
            { "bar:SystemMetric", ExpressionType.SystemMetric },
            { "bar:Direct3D", ExpressionType.Direct3D },
            { "bar:Memory", ExpressionType.Memory },
            { "bar:VideoMemory", ExpressionType.VideoMemory },
            { "bar:SensorById", ExpressionType.SensorById },
            { "bar:NFC", ExpressionType.NFC },
            { "bar:Camera", ExpressionType.Camera },
            { "ProductReleaseInstalled", ExpressionType.ProductReleaseInstalled },
            { "upd:ProductReleaseVersion", ExpressionType.ProductReleaseVersion },
            { "drv:WindowsDriverInstalled", ExpressionType.WindowsDriverInstalled },
            { "drv:WindowsDriverSuperseded", ExpressionType.WindowsDriverSuperseded },
            { "drv:WindowsDriverInstallable", ExpressionType.WindowsDriverInstallable },
            { "drv:WindowsDriverMetaData", ExpressionType.WindowsDriverMetaData },
            { "drv:FeatureScore", ExpressionType.FeatureScore },
            { "drv:DistributionComputerHardwareId", ExpressionType.DistributionComputerHardwareId },
            { "drv:TargetComputerHardwareId", ExpressionType.TargetComputerHardwareId },
            { "drv:CompatibleProvider", ExpressionType.CompatibleProvider },
            { "drv:WindowsDriver", ExpressionType.WindowsDriver },
            { "drv:InstalledVersionRange", ExpressionType.InstalledVersionRange },
            { "bar:ClusteredOS", ExpressionType.ClusteredOS },
            { "bar:ClusterResourceOwner", ExpressionType.ClusterResourceOwner },
        };
    }
}
