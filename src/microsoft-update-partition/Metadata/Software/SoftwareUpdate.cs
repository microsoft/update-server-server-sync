// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using System;
using Microsoft.PackageGraph.MicrosoftUpdate.Index;
using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Storage;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Parsers;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata
{
    /// <summary>
    /// Represents a software update in the Microsoft Update catalog.
    /// </summary>
    public class SoftwareUpdate : MicrosoftUpdatePackage
    {
        /// <summary>
        /// Software update support URL, if available
        /// </summary>
        public string SupportUrl
        {
            get
            {
                if (_MetadataLoaded)
                {
                    return _SupportUrl;
                }

                LoadNonIndexedMetadataBase();
                return _SupportUrl;
            }
        }
        private string _SupportUrl;

        /// <summary>
        /// KB article ID associated with this software update
        /// </summary>
        public string KBArticleId
        {
            get
            {
                if (_KBArticleIdLoaded)
                {
                    return _KBArticleId;
                }

                if (_FastLookupSource != null)
                {
                    _FastLookupSource.TrySimpleKeyLookup<string>(this._Id, AvailableIndexes.KbArticleIndexName, out _KBArticleId);
                    _KBArticleIdLoaded = true;
                }
                else
                {
                    LoadNonIndexedMetadataBase();
                }

                return _KBArticleId;
            }
        }
        private string _KBArticleId;
        private bool _KBArticleIdLoaded;

        /// <summary>
        /// Whether this software update is an OS upgrade
        /// </summary>
        public string OsUpgrade
        {
            get
            {
                if (_MetadataLoaded)
                {
                    return _OsUpgrade;
                }

                LoadNonIndexedMetadataBase();
                return _OsUpgrade;
            }
        }
        private string _OsUpgrade;

        /// <summary>
        /// List of software updates that supersede this update
        /// </summary>
        public IReadOnlyList<IPackageIdentity> IsSupersededBy
        {
            get
            {
                if (_IsSupersededByLoaded)
                {
                    return _IsSupersededBy;
                }
                else if (_FastLookupSource != null)
                {
                    _FastLookupSource.TryPackageListLookupByCustomKey<Guid>(Id.ID, AvailableIndexes.IsSupersededIndexName, out _IsSupersededBy);
                }

                _IsSupersededByLoaded = true;
                return _IsSupersededBy;
            }
        }
        private List<IPackageIdentity> _IsSupersededBy;
        private bool _IsSupersededByLoaded;

        /// <summary>
        /// List of Update Ids superseded by this update.
        /// </summary>
        /// <value>List of update ids (GUID)</value>
        public IReadOnlyList<Guid> SupersededUpdates
        {
            get
            {
                if (_SupersededUpdatesLoaded)
                {
                    return _SupersededUpdates;
                }
                else if (_FastLookupSource != null)
                {
                    _FastLookupSource.TryListKeyLookup<Guid>(_Id, AvailableIndexes.IsSupersedingIndexName, out _SupersededUpdates);
                    _SupersededUpdatesLoaded = true;
                }
                else
                {
                    LoadNonIndexedMetadataBase();
                }

                return _SupersededUpdates;
            }
        }
        private List<Guid> _SupersededUpdates;
        private bool _SupersededUpdatesLoaded;

        /// <summary>
        /// List of updates bundled within this update. Software updates can bundle 1 or more updates together in an update bundle.
        /// </summary>
        public List<MicrosoftUpdatePackageIdentity> BundledUpdates
        {
            get
            {
                if (_BundledUpdatesLoaded)
                {
                    return _BundledUpdates;
                }
                else if (_FastLookupSource != null)
                {
                    _FastLookupSource.TryListKeyLookup<MicrosoftUpdatePackageIdentity>(_Id, AvailableIndexes.IsBundleIndexName, out _BundledUpdates);
                    _BundledUpdatesLoaded = true;
                }

                return _BundledUpdates;
            }
        }
        private List<MicrosoftUpdatePackageIdentity> _BundledUpdates;
        private bool _BundledUpdatesLoaded;

        /// <summary>
        /// List of updates within which this updates is bundled. An update can belong to multiple, distinct bundles
        /// </summary>
        public List<IPackageIdentity> BundledWithUpdates
        {
            get
            {
                if (_BundledWithUpdatesLoaded)
                {
                    return _BundledWithUpdates;
                }
                else if (_FastLookupSource != null)
                {
                    _FastLookupSource.TryPackageListLookupByCustomKey<MicrosoftUpdatePackageIdentity>(_Id, AvailableIndexes.BundledWithIndexName, out _BundledWithUpdates);
                    _BundledWithUpdatesLoaded = true;
                }

                return _BundledWithUpdates;
            }
        }
        private List<IPackageIdentity> _BundledWithUpdates;
        private bool _BundledWithUpdatesLoaded;


        internal SoftwareUpdate(MicrosoftUpdatePackageIdentity id, XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager) : base(id, metadataNavigator, namespaceManager)
        {
            LoadNonIndexedMetadata(metadataNavigator, namespaceManager);
            ParseIndexedMetadata(metadataNavigator, namespaceManager);
        }

        internal SoftwareUpdate(MicrosoftUpdatePackageIdentity id, IMetadataLookup metadataLookup, IMetadataSource metadataSource) : base(id, metadataLookup, metadataSource)
        {
        }

        private void ParseIndexedMetadata(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            _SupersededUpdates = SupersededUpdatesParser.Parse(metadataNavigator, namespaceManager);
            _SupersededUpdatesLoaded = true;

            _BundledUpdates = BundlesUpdatesParser.Parse(metadataNavigator, namespaceManager);
            _BundledUpdatesLoaded = true;
        }

        internal override void LoadNonIndexedMetadata(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            var parsedAttributes = SoftwareUpdateParser.GetSoftwareUpdateProperties(metadataNavigator, namespaceManager);
            _SupportUrl = parsedAttributes.SupportUrl;

            _KBArticleId = parsedAttributes.KBArticleID;
            _KBArticleIdLoaded = true;

            _OsUpgrade = parsedAttributes.OSUpgrade;
        }
    }
}
