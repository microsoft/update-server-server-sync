// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.MicrosoftUpdate.Index;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Drivers;
using Microsoft.PackageGraph.Storage;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata
{
    /// <summary>
    /// Represents a driver update from the Microsoft Update catalog
    /// </summary>
    public class DriverUpdate : MicrosoftUpdatePackage
    {
        private List<DriverMetadata> _Metadata;
        private bool _DriverMetadataLoaded = false;

        internal DriverUpdate(MicrosoftUpdatePackageIdentity id, XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager) : base(id, metadataNavigator, namespaceManager)
        {
            LoadNonIndexedMetadata(metadataNavigator, namespaceManager);
        }

        internal DriverUpdate(MicrosoftUpdatePackageIdentity id, IMetadataLookup metadataLookup, IMetadataSource metadataSource) : base(id, metadataLookup, metadataSource)
        {

        }

        /// <summary>
        /// Gets a list of driver specific metadata associated with this driver update.
        /// </summary>
        /// <returns>List of driver specific metadata</returns>
        public List<DriverMetadata> GetDriverMetadata()
        {
            if (_MetadataLoaded || _DriverMetadataLoaded)
            {
                return _Metadata;
            }

            if(_FastLookupSource != null)
            {
                _FastLookupSource.TryListKeyLookup<DriverMetadata>(this.Id, AvailableIndexes.DriverMetadataIndexName, out _Metadata);
                _DriverMetadataLoaded = true;
            }
            else
            {
                LoadNonIndexedMetadataBase();
            }

            return _Metadata;
        }

        internal override void LoadNonIndexedMetadata(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            _Metadata = Parsers.DriverMetadataParser.GetAllMetadataEntries(metadataNavigator, namespaceManager);
            _DriverMetadataLoaded = true;
        }
    }
}
