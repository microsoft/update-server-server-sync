// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Applicability;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Content;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Handlers;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Parsers;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Prerequisites;
using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata
{
    /// <summary>
    /// A base class for all updates stored on an upstream Microsoft Update server.
    /// <para>
    /// Stores generic update metadata applicable to both categories (classifications, products, detectoids) and updates (software and driver updates).
    /// </para>
    /// </summary>
    public abstract class MicrosoftUpdatePackage : IPackage
    {
        internal MicrosoftUpdatePackageIdentity _Id;

        private IMetadataSource _MetadataSource;

        internal IMetadataLookup _FastLookupSource;

        /// <summary>
        /// Returns the identity of this Microsoft Update package
        /// </summary>
        public MicrosoftUpdatePackageIdentity Id => _Id;

        /// <summary>
        /// Returns the identity of this Microsoft Update package
        /// </summary>
        IPackageIdentity IPackage.Id => _Id;

        /// <summary>
        /// Get the category or update title
        /// </summary>
        public string Title
        {
            get
            {
                if (_TitleLoaded)
                {
                    return _Title;
                }
                else if (_FastLookupSource != null)
                {
                    _FastLookupSource.TrySimpleKeyLookup<string>(_Id, Storage.Index.AvailableIndexes.TitlesIndexName, out string title);
                    return title;
                }
                else if (_MetadataSource != null)
                {
                    LoadNonIndexedMetadataBase();
                    _TitleLoaded = true;
                    return _Title;
                }
                else
                {
                    return null;
                }
            }
        }
        private bool _TitleLoaded;
        private string _Title;

        /// <summary>
        /// The list of category IDs associated to this update
        /// </summary>
        public IReadOnlyList<Guid> Categories
        {
            get
            {
                if (_CategoriesLoaded)
                {
                    return _Categories;
                }
                else if (_FastLookupSource != null)
                {
                    _FastLookupSource.TrySimpleKeyLookup<List<Guid>>(this._Id, Index.AvailableIndexes.CategoriesIndexName, out _Categories);
                    _CategoriesLoaded = true;
                }

                return _Categories;
            }
        }
        private List<Guid> _Categories;
        private bool _CategoriesLoaded;

        /// <summary>
        /// The list of categories associated with this update.
        /// </summary>
        /// <param name="knownCategories">List of known categories. The updates's category IDs will be resolved from this list.</param>
        /// <returns>List of categories for the update</returns>
        public List<MicrosoftUpdatePackage> GetCategories(ILookup<Guid, MicrosoftUpdatePackage> knownCategories)
        {
            var prerequisites = Prerequisites;
            if (prerequisites == null)
            {
                return null;
            }

            return prerequisites
                .OfType<AtLeastOne>()
                .SelectMany(atLeastOne => atLeastOne.Simple)
                .Select(simple => simple.UpdateId)
                .Where(simple => knownCategories.Contains(simple))
                .Select(simple => knownCategories[simple].First())
                .ToList();
        }

        /// <summary>
        /// Get the list of prerequisites
        /// </summary>
        /// <value>
        /// List of prerequisites
        /// </value>
        public List<IPrerequisite> Prerequisites
        {
            get
            {
                if (_PrerequisitesLoaded)
                {
                    return _Prerequisites;
                }
                else if (_FastLookupSource != null)
                {
                    _FastLookupSource.TryListKeyLookup<IPrerequisite>(this._Id, Index.AvailableIndexes.PrerequisitesIndexName, out _Prerequisites);
                    _PrerequisitesLoaded = true;
                }

                return _Prerequisites;
            }
        }

        private List<IPrerequisite> _Prerequisites;
        private bool _PrerequisitesLoaded;

        /// <summary>
        /// Determines if the update is applicable based on its list of prerequisites and the list of installed updates (prerequisites) on a computer
        /// </summary>
        /// <param name="installedPrerequisites">List of installed updates on a computer</param>
        /// <returns>True if all prerequisites are met, false otherwise</returns>
        public bool IsApplicable(List<Guid> installedPrerequisites)
        {
            return PrerequisitesAnalyzer.IsApplicable(this, installedPrerequisites);
        }

        /// <summary>
        /// Get the category or update description
        /// </summary>
        [JsonProperty]
        public string Description
        {
            get
            {
                if (_DescriptionLoaded)
                {
                    return _Description;
                }
				else if (_FastLookupSource != null)
				{
					_FastLookupSource.TrySimpleKeyLookup<string>(_Id, Storage.Index.AvailableIndexes.DescriptionsIndexName, out string description);
					return description;
				}
				else if (_MetadataSource != null)
				{
					LoadNonIndexedMetadataBase();
					_DescriptionLoaded = true;
					return _Description;
				}
				else
				{
					return null;
				}
			}
        }
		private bool _DescriptionLoaded;
		private string _Description = null;

        /// <summary>
        /// Gets the list of files (content) for update
        /// </summary>
        /// <value>
        /// List of content files
        /// </value>
        public IEnumerable<IContentFile> Files
        {
            get
            {
                if (_FilesLoaded)
                {
                    return _Files;
                }
                else if (_FastLookupSource != null)
                {
                    _FastLookupSource.TryListKeyLookup<UpdateFile>(this._Id, Index.AvailableIndexes.FilesIndexName, out _Files);
                    _FilesLoaded = true;
                }
                else if (_MetadataSource != null)
                {
                    _Files = _MetadataSource.GetFiles<UpdateFile>(this._Id).Cast<UpdateFile>().ToList();
                    _FilesLoaded = true;
                }

                return _Files;
            }
        }

        private List<UpdateFile> _Files = null;
        private bool _FilesLoaded = false;

        /// <summary>
        /// Gets the handler that can apply this update to a Windows device
        /// </summary>
        public HandlerMetadata Handler
        {
            get
            {
                if (_HandlerLoaded)
                {
                    return _Handler;
                }
                else if (_MetadataSource != null)
                {
                    LoadApplicabilityRules();
                }

                return _Handler;
            }
        }
        private HandlerMetadata _Handler = null;
        private bool _HandlerLoaded = false;

        /// <summary>
        /// Gets the applicability rules for an update
        /// </summary>
        public List<ApplicabilityRule> ApplicabilityRules
        {
            get
            {
                if (_ApplicabilityRulesLoaded)
                {
                    return _ApplicabilityRules;
                }
                else if (_MetadataSource != null)
                {
                    LoadApplicabilityRules();
                }

                return _ApplicabilityRules;
            }
        }

        private List<ApplicabilityRule> _ApplicabilityRules;
        private bool _ApplicabilityRulesLoaded = false;

        internal bool _MetadataLoaded = false;

        /// <summary>
        /// Releases raw metadata cached in memory.
        /// </summary>
        public void ReleaseMetadataBytes()
        {
            _MetadataBytes = null;
        }

        /// <summary>
        /// Returns the XML metadata stream from which this update metadata was created.
        /// </summary>
        /// <returns>XML stream, UTF8 encoded</returns>
        public Stream GetMetadataStream()
        {
            if (_MetadataBytes != null)
            {
                return new GZipStream(new MemoryStream(_MetadataBytes, false), CompressionMode.Decompress);
            }
            else if (_MetadataSource != null)
            {
                return _MetadataSource.GetMetadata(_Id);
            }
            else
            {
                return null;
            }
        }

        private byte[] _MetadataBytes;

        /// <summary>
        /// Matches keywords in the title of the update
        /// </summary>
        /// <param name="keywords">List of keywords to match. All keywords must match</param>
        /// <returns>True if all keywords match, false otherwise.</returns>
        public bool MatchTitle(string[] keywords)
        {
            foreach (var keyword in keywords)
            {
                if (!Title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates a MicrosoftUpdatePackage from raw metadata and a list of content files associated with the package
        /// </summary>
        /// <param name="metadata">RAW metadata. Expected to be UTF8 encoded XML</param>
        /// <param name="filesCollection">List of URLs for content files</param>
        /// <returns>Rehydrated MicrosoftUpdatePackage object</returns>
        public static MicrosoftUpdatePackage FromMetadataXml(byte[] metadata, Dictionary<string, UpdateFileUrl> filesCollection)
        {
            var metadataStream = new GZipStream(new MemoryStream(metadata, false), CompressionMode.Decompress);
            var createdUpdate = FromMetadataXml(metadataStream, filesCollection) as MicrosoftUpdatePackage;
            createdUpdate._MetadataBytes = metadata;

            return createdUpdate;
        }

        internal static MicrosoftUpdatePackage FromStoredMetadataXml(Stream metadataStream, IMetadataSource metadataStore)
        {
            XPathDocument document = new(metadataStream);
            XPathNavigator navigator = document.CreateNavigator();

            XmlNamespaceManager manager = new(navigator.NameTable);
            manager.AddNamespace("upd", "http://schemas.microsoft.com/msus/2002/12/Update");
            manager.AddNamespace("cat", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/Category");
            manager.AddNamespace("drv", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsDriver");
            manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            manager.AddNamespace("cmd", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/CommandLineInstallation");
            manager.AddNamespace("psf", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsPatch");
            manager.AddNamespace("cbs", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/Cbs");
            manager.AddNamespace("msp", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsInstaller");
            manager.AddNamespace("wsi", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsSetup");

            // Get the update type
            var updateType = UpdateParser.GetUpdateType(navigator, manager).ToLowerInvariant();
            var id = UpdateParser.GetUpdateId(navigator, manager);

            MicrosoftUpdatePackage createdUpdate;
            switch (updateType)
            {
                case "detectoid":
                    createdUpdate = new DetectoidCategory(id, navigator, manager);
                    break;

                case "category":
                    var categoryType = UpdateParser.GetCategory(navigator, manager).ToLowerInvariant();
                    if (categoryType == "updateclassification")
                    {
                        createdUpdate = new ClassificationCategory(id, navigator, manager);
                    }
                    else if (categoryType == "product" || categoryType == "company" || categoryType == "productfamily")
                    {
                        createdUpdate = new ProductCategory(id, navigator, manager);
                    }
                    else
                    {
                        throw new Exception($"Unexpected category type {categoryType}");
                    }
                    break;

                case "driver":
                    createdUpdate = new DriverUpdate(id, navigator, manager);
                    break;

                case "software":
                    createdUpdate = new SoftwareUpdate(id, navigator, manager);
                    break;

                default:
                    throw new Exception($"Unexpected update type: {updateType}");
            }

            createdUpdate._MetadataSource = metadataStore;

            if (metadataStore != null)
            {
                createdUpdate._Files = metadataStore.GetFiles<UpdateFile>(createdUpdate._Id).Cast<UpdateFile>().ToList();
                createdUpdate._FilesLoaded = true;
            }

            return createdUpdate;
        }

        static MicrosoftUpdatePackage FromMetadataXml(Stream metadataStream, Dictionary<string, UpdateFileUrl> filesCollection)
        {
            XPathDocument document = new(metadataStream);
            XPathNavigator navigator = document.CreateNavigator();

            XmlNamespaceManager manager = new(navigator.NameTable);
            manager.AddNamespace("upd", "http://schemas.microsoft.com/msus/2002/12/Update");
            manager.AddNamespace("cat", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/Category");
            manager.AddNamespace("drv", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsDriver");
            manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            manager.AddNamespace("cmd", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/CommandLineInstallation");
            manager.AddNamespace("psf", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsPatch");
            manager.AddNamespace("cbs", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/Cbs");
            manager.AddNamespace("msp", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsInstaller");
            manager.AddNamespace("wsi", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsSetup");

            // Get the update type
            var updateType = UpdateParser.GetUpdateType(navigator, manager).ToLowerInvariant();
            var id = UpdateParser.GetUpdateId(navigator, manager);

            MicrosoftUpdatePackage createdUpdate;
            switch (updateType)
            {
                case "detectoid":
                    createdUpdate = new DetectoidCategory(id, navigator, manager);
                    break;

                case "category":
                    var categoryType = UpdateParser.GetCategory(navigator, manager).ToLowerInvariant();
                    if (categoryType == "updateclassification")
                    {
                        createdUpdate = new ClassificationCategory(id, navigator, manager);
                    }
                    else if (categoryType == "product" || categoryType == "company" || categoryType == "productfamily")
                    {
                        createdUpdate = new ProductCategory(id, navigator, manager);
                    }
                    else
                    {
                        throw new Exception($"Unexpected category type {categoryType}");
                    }
                    break;

                case "driver":
                    createdUpdate = new DriverUpdate(id, navigator, manager);
                    break;

                case "software":
                    createdUpdate = new SoftwareUpdate(id, navigator, manager);
                    break;

                default:
                    throw new Exception($"Unexpected update type: {updateType}");
            }

            createdUpdate.MergeFileInformation(navigator, manager, filesCollection);
            return createdUpdate;
        }

        internal static MicrosoftUpdatePackage FromTypeAndStore(StoredPackageType updateType, MicrosoftUpdatePackageIdentity id, IMetadataLookup metadataLookup, IMetadataSource metadataSource)
        {
            return updateType switch
            {
                StoredPackageType.MicrosoftUpdateDetectoid => new DetectoidCategory(id, metadataLookup, metadataSource),
                StoredPackageType.MicrosoftUpdateProduct => new ProductCategory(id, metadataLookup, metadataSource),
                StoredPackageType.MicrosoftUpdateClassification => new ClassificationCategory(id, metadataLookup, metadataSource),
                StoredPackageType.MicrosoftUpdateDriver => new DriverUpdate(id, metadataLookup, metadataSource),
                StoredPackageType.MicrosoftUpdateSoftware => new SoftwareUpdate(id, metadataLookup, metadataSource),
                _ => throw new Exception($"Unexpected update type: {updateType}"),
            };
        }

        internal MicrosoftUpdatePackage(MicrosoftUpdatePackageIdentity id, XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            _Id = id;
            _Title = UpdateParser.GetTitle(metadataNavigator, namespaceManager);
            _TitleLoaded = true;

            _Description = UpdateParser.GetDescription(metadataNavigator, namespaceManager);
            _DescriptionLoaded = true;
            _MetadataLoaded = true;

            _Prerequisites = PrerequisiteParser.FromXml(metadataNavigator, namespaceManager);
            _PrerequisitesLoaded = true;

            _Categories = _Prerequisites
                .OfType<AtLeastOne>()
                .Where(p => p.IsCategory)
                .SelectMany(p => p.Simple)
                .Select(p => p.UpdateId)
                .ToList();
            _CategoriesLoaded = true;
        }

        internal MicrosoftUpdatePackage(MicrosoftUpdatePackageIdentity id, IMetadataLookup metadataLookup, IMetadataSource metadataSource)
        {
            _Id = id;
            _FastLookupSource = metadataLookup;
            _MetadataSource = metadataSource;
        }

        private void MergeFileInformation(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager, Dictionary<string, UpdateFileUrl> filesCollection)
        {
            _Files = UpdateFileParser.ParseFiles(metadataNavigator, namespaceManager);
            if (_Files.Count == 0)
            {
                return;
            }

            if (filesCollection == null)
            {
                throw new Exception($"Update {_Id} has unresolved files");
            }

            foreach (var file in _Files.OfType<UpdateFile>())
            {
                file.Urls = new List<UpdateFileUrl>();
                foreach (var digest in file.Digests)
                {
                    if (filesCollection.TryGetValue(digest.DigestBase64, out var url))
                    {
                        file.Urls.Add(url);
                    }
                }

                if (file.Urls.Count == 0)
                {
                    throw new Exception($"Update {_Id} has unresolved file {file.Digest.DigestBase64}");
                }
            }

            _FilesLoaded = true;
        }

        /// <summary>
        /// Implemented in derived classes; Retrieves metadata not indexed by the base Microsoft Update Package class.
        /// </summary>
        /// <param name="metadataNavigator">XPath navigator</param>
        /// <param name="namespaceManager">XML namespace manager</param>
        internal abstract void LoadNonIndexedMetadata(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager);

        private void LoadApplicabilityRules()
        {
            lock (this)
            {
                if (_ApplicabilityRulesLoaded || _MetadataSource == null)
                {
                    return;
                }

                using (var metadataStream = _MetadataSource.GetMetadata(this._Id))
                {
                    XPathDocument document = new(metadataStream);
                    XPathNavigator navigator = document.CreateNavigator();

                    XmlNamespaceManager manager = new(navigator.NameTable);
                    manager.AddNamespace("upd", "http://schemas.microsoft.com/msus/2002/12/Update");
                    manager.AddNamespace("cat", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/Category");
                    manager.AddNamespace("drv", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsDriver");
                    manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                    manager.AddNamespace("cmd", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/CommandLineInstallation");
                    manager.AddNamespace("psf", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsPatch");
                    manager.AddNamespace("cbs", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/Cbs");
                    manager.AddNamespace("msp", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsInstaller");
                    manager.AddNamespace("wsi", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsSetup");

                    _ApplicabilityRules = ApplicabilityRule.FromXml(navigator, manager);
                    _Handler = HandlerMetadata.FromXml(navigator, manager);
                }

                _HandlerLoaded = true;
                _ApplicabilityRulesLoaded = true;
            }
        }

        internal void LoadNonIndexedMetadataBase()
        {
            lock(this)
            {
                if (_MetadataLoaded || _MetadataSource == null)
                {
                    return;
                }

                using (var metadataStream = _MetadataSource.GetMetadata(this._Id))
                {
                    XPathDocument document = new(metadataStream);
                    XPathNavigator navigator = document.CreateNavigator();

                    XmlNamespaceManager manager = new(navigator.NameTable);
                    manager.AddNamespace("upd", "http://schemas.microsoft.com/msus/2002/12/Update");
                    manager.AddNamespace("cat", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/Category");
                    manager.AddNamespace("drv", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsDriver");
                    manager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                    manager.AddNamespace("cmd", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/CommandLineInstallation");
                    manager.AddNamespace("psf", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsPatch");
                    manager.AddNamespace("cbs", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/Cbs");
                    manager.AddNamespace("msp", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsInstaller");
                    manager.AddNamespace("wsi", "http://schemas.microsoft.com/msus/2002/12/UpdateHandlers/WindowsSetup");
                
                    _Description = UpdateParser.GetDescription(navigator, manager);
                    _Title = UpdateParser.GetTitle(navigator, manager);

                    LoadNonIndexedMetadata(navigator, manager);
                }

                _MetadataLoaded = true;
            }
        }
    }
}
