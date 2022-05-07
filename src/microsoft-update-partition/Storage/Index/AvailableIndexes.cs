// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.PackageGraph.MicrosoftUpdate.Index
{
    abstract class AvailableIndexes
    {
        public const string DriverMetadataIndexName = "mu-driver-metadata";
        public const string KbArticleIndexName = "mu-kbarticle";
        public const string IsSupersededIndexName = "mu-is-superseded";
        public const string IsSupersedingIndexName = "mu-is-superseding";
        public const string IsBundleIndexName = "mu-is-bundled";
        public const string BundledWithIndexName = "mu-bundled-with";
        public const string PrerequisitesIndexName = "mu-prerequisites";
        public const string CategoriesIndexName = "mu-categories";
        public const string FilesIndexName = "mu-files";
    }
}
