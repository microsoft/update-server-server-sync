// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.MicrosoftUpdate.Compression;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;
using Microsoft.PackageGraph.Storage;
using Microsoft.UpdateServices.WebServices.ServerSync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata
{
    /// <summary>
    /// Exports updates from <see cref="IMetadataSource"/> to a WSUS compatible format
    /// </summary>
    public class WsusExporter
    {
        readonly IMetadataStore PackageSource;
        readonly ServerSyncConfigData ServiceConfiguration;

        readonly List<MicrosoftUpdatePackage> Classifications;
        readonly ILookup<Guid, MicrosoftUpdatePackage> ClassificationsLookup;
        readonly List<DetectoidCategory> Detectoids;
        readonly List<MicrosoftUpdatePackage> Products;
        readonly ILookup<Guid, MicrosoftUpdatePackage> ProductsLookup;

        /// <summary>
        /// Create a new exporter object from the specified store.
        /// </summary>
        /// <param name="source">The store that contains the updates to export</param>
        /// <param name="serviceConfiguration">The configuration of the service from where the updates were obtained.</param>
        public WsusExporter(IMetadataStore source, ServerSyncConfigData serviceConfiguration)
        {
            PackageSource = source;
            ServiceConfiguration = serviceConfiguration;

            Classifications = PackageSource.OfType<ClassificationCategory>().Cast<MicrosoftUpdatePackage>().ToList();
            ClassificationsLookup = Classifications.ToLookup(classification => classification.Id.ID);

            Products = PackageSource.OfType<ProductCategory>().Cast<MicrosoftUpdatePackage>().ToList();
            ProductsLookup = Products.ToLookup(product => product.Id.ID);

            Detectoids = PackageSource.OfType<DetectoidCategory>().ToList();
        }

        /// <summary>
        /// Exports the specified updates from a local update metadata source to a format compatible with WSUS 2016
        /// </summary>
        /// <param name="filter">The filter to apply during the export operation</param>
        /// <param name="exportFilePath">The export destination file (CAB)</param>
        public void Export(MetadataFilter filter, string exportFilePath)
        {
            var updatesToExport = filter.Apply<MicrosoftUpdatePackage>(PackageSource);
            var exportDirectory = Directory.GetParent(exportFilePath);
            if (!exportDirectory.Exists)
            {
                Directory.CreateDirectory(exportDirectory.FullName);
            }

            // Pack all XML blobs for updates to be exported into a flat text file
            var metadataFile = Path.Combine(exportDirectory.FullName, "metadata.txt");
            WriteMetadataFile(updatesToExport, metadataFile);

            // Write metadata for all exported updates, languages and files
            var packageXmlFile = Path.Combine(exportDirectory.FullName, "package.xml");
            WritePackagesXml(updatesToExport, packageXmlFile);

            // Add the above 2 files to a CAB archive

            try
            {
                CabinetUtility.CompressFiles(new List<string>() { metadataFile, packageXmlFile }, exportFilePath);
            }
            finally
            {
                // Delete temporary files
                if (File.Exists(metadataFile))
                {
                    File.Delete(metadataFile);
                }

                if (File.Exists(packageXmlFile))
                {
                    File.Delete(packageXmlFile);
                }
            }
        }

        /// <summary>
        /// Creates the metadata.txt file for a list of updates to export.
        /// Copies update IDs and XML data to this file
        /// </summary>
        /// <param name="updatesToExport">The updates to export</param>
        /// <param name="metadataTextFile">Destination metadata file</param>
        private void WriteMetadataFile(IEnumerable<MicrosoftUpdatePackage> updatesToExport, string metadataTextFile)
        {
            // Each line in the metadata text file contains multiple lines of the following format:
            // <update GUID>,<update revision>,<xml size>,<xml>\r\n
            // There is one line for each update exported

            // Open the metadata file for writing
            using var metadataFile = File.CreateText(metadataTextFile);
            var allUpdates = new List<MicrosoftUpdatePackage>(Detectoids);
            allUpdates.AddRange(Classifications);
            allUpdates.AddRange(Products);
            allUpdates.AddRange(updatesToExport);

            foreach (var update in allUpdates)
            {
                using var metadataStream = PackageSource.GetMetadata(update.Id);
                using var metadataReader = new StreamReader(metadataStream);
                var xmlData = metadataReader.ReadToEnd();

                // Write one line with GUID, revision, XML length, XML data
                metadataFile.WriteLine("{0},{1:x8},{2:x8},{3}", update.Id.ID, update.Id.Revision, xmlData.Length, xmlData);
            }
        }

        /// <summary>
        /// Writes the packages.xml file for a list of updates to export
        /// </summary>
        /// <param name="updates">The updates to export</param>
        /// <param name="packagesFilePath">Destination file to write the XML to</param>
        private void WritePackagesXml(
            IEnumerable<MicrosoftUpdatePackage> updates,
            string packagesFilePath)
        {
            XDocument packagesXml = new();

            XElement exportElement = new("ExportPackage");
            packagesXml.Add(exportElement);

            exportElement.Add(new XAttribute("ServerID", Guid.NewGuid().ToString()));
            exportElement.Add(new XAttribute("CreationTime", string.Format("{0:yyyy-MM-ddTHH:mm:ssZ}", DateTime.UtcNow)));
            exportElement.Add(new XAttribute("FormatVersion", "1.0"));
            exportElement.Add(new XAttribute("ProtocolVersion", "1.20"));

            // Create Languages element and add it to the XML
            exportElement.Add(CreateLanguagesElement(ServiceConfiguration));

            // Create a Files element and add it to the top level ExportPackage element
            exportElement.Add(CreateFilesElement(updates));

            // Create an Updates element that contains:
            var updatesElement = new XElement("Updates");
            exportElement.Add(updatesElement);

            // all detectoid, classifications and products
            updatesElement.Add(CreateCategoriesElements());

            // exported updates metadata
            updatesElement.Add(CreateUpdatesElements(updates));

            using var packagesFile = File.OpenWrite(packagesFilePath);
            using var xmlWriter = XmlWriter.Create(packagesFile, new XmlWriterSettings() { Encoding = new UTF8Encoding() });
            packagesXml.Save(xmlWriter);
        }

        /// <summary>
        /// Creates the languages XML node from supported server languages in the configuration
        /// </summary>
        /// <param name="serverConfig">The server configuration. Contains supported languages</param>
        /// <returns></returns>
        private static XElement CreateLanguagesElement(ServerSyncConfigData serverConfig)
        {
            var languagesElement = new XElement("Languages");
            foreach (var language in serverConfig.LanguageUpdateList)
            {
                var languageElement = new XElement("Language");
                languageElement.Add(new XAttribute("Id", language.LanguageID));
                languageElement.Add(new XAttribute("ShortName", language.ShortLanguage));
                languageElement.Add(new XAttribute("LongName", language.LongLanguage));
                languageElement.Add(new XAttribute("Enabled", "1"));

                languagesElement.Add(languageElement);
            }

            return languagesElement;
        }

        private static XElement CreateFilesElement(IEnumerable<MicrosoftUpdatePackage> updates)
        {
            var filesToExport = updates.Where(u => u.Files != null).SelectMany(u => u.Files).Distinct();

            // Add all the files
            // Get the distinct list of files to export
            var filesElement = new XElement("Files");
            foreach (var file in filesToExport)
            {
                var fileElement = new XElement("File");
                fileElement.Add(new XAttribute("Digest", file.Digest.DigestBase64));

                if (string.IsNullOrEmpty(file.Source))
                {
                    continue;
                }

                fileElement.Add(new XAttribute("MUUrl", file.Source));
                fileElement.Add(new XAttribute("Name", file.FileName));

                filesElement.Add(fileElement);
            }

            return filesElement;
        }

        /// <summary>
        /// Adds categories to the export XML (detectoids, classifications and products)
        /// </summary>
        /// <returns></returns>
        private List<XElement> CreateCategoriesElements()
        {
            var categoriesElements = new List<XElement>();
            var allCategories = new List<MicrosoftUpdatePackage>(Detectoids);
            allCategories.AddRange(Classifications);
            allCategories.AddRange(Products);
            foreach (var category in allCategories)
            {
                var categoryElement = new XElement("Update");
                categoryElement.Add(new XAttribute("UpdateId", category.Id.ID));
                categoryElement.Add(new XAttribute("RevisionNumber", category.Id.Revision));

                // Emptry Files, Categories and Classifications elements
                categoryElement.Add(new XElement("Files"));
                categoryElement.Add(new XElement("Categories"));
                categoryElement.Add(new XElement("Classifications"));

                // Add the new element to the return list
                categoriesElements.Add(categoryElement);
            }

            return categoriesElements;
        }

        /// <summary>
        /// Adds updates to the export XML (software, updates, etc.)
        /// </summary>
        /// <param name="updates">The updates to export</param>
        /// <returns></returns>
        private List<XElement> CreateUpdatesElements(IEnumerable<MicrosoftUpdatePackage> updates)
        {
            var categoriesElements = new List<XElement>();

            foreach (var update in updates)
            {
                var updateElement = new XElement("Update");
                updateElement.Add(new XAttribute("UpdateId", update.Id.ID));
                updateElement.Add(new XAttribute("RevisionNumber", update.Id.Revision));

                // Add the update's files
                var filesElement = new XElement("Files");
                if (update.Files != null)
                {
                    foreach (var file in update.Files)
                    {
                        var fileElement = new XElement("File");
                        fileElement.Add(new XAttribute("Digest", file.Digest.DigestBase64));
                        filesElement.Add(fileElement);
                    }
                }
                updateElement.Add(filesElement);

                // Add the update's categories
                var categoriesElement = new XElement("Categories");
                var products = update.GetCategories(ProductsLookup);
                if (products != null)
                {
                    foreach (var product in products)
                    {
                        var categoryElement = new XElement("Category");
                        categoryElement.Add(new XAttribute("Value", product.ToString()));
                        categoriesElement.Add(categoryElement);
                    }
                }
                updateElement.Add(categoriesElement);

                // Add the update's classifications
                var classificationsElement = new XElement("Classifications");
                var classifications = update.GetCategories(ClassificationsLookup);
                if (classifications != null)
                {
                    foreach (var classification in classifications)
                    {
                        var classificationElement = new XElement("Classification");
                        classificationElement.Add(new XAttribute("Value", classification.ToString()));
                        classificationsElement.Add(classificationElement);
                    }
                }
                updateElement.Add(classificationsElement);

                // Add the new element to the return list
                categoriesElements.Add(updateElement);
            }

            return categoriesElements;
        }

        /// <summary>
        /// Given a list of updates to export, it finds all updates bundled with updates to be exported and adds them
        /// to the list as well. This is done recursively, until all bundled updates have been included
        /// </summary>
        /// <param name="updatesToExport">The updates to export. Bundled updates are added to this list</param>
        /// <param name="source">The update metadata to export from.</param>
        public static void CompleteTheListOfExportUpdates(List<MicrosoftUpdatePackage> updatesToExport, IMetadataStore source)
        {
            bool additionalUpdatesFound = false;
            do
            {
                var additionalUpdates = new List<MicrosoftUpdatePackageIdentity>();
                foreach (var selectedUpdate in updatesToExport)
                {
                    if (selectedUpdate is SoftwareUpdate softwareUpdate
                        && softwareUpdate.BundledUpdates != null)
                    {
                        foreach (var bundledUpdate in softwareUpdate.BundledUpdates)
                        {
                            if (!updatesToExport.Any(u => u.Id.Equals(bundledUpdate)))
                            {
                                additionalUpdates.Add(bundledUpdate);
                            }
                        }
                    }
                }

                foreach (var additionalUpdate in additionalUpdates)
                {
                    // Bundled updates should appear in the list before the updates that bundle them
                    updatesToExport.Insert(0, source.GetPackage(additionalUpdate) as MicrosoftUpdatePackage);
                }

                additionalUpdatesFound = additionalUpdates.Count > 0;
            } while (additionalUpdatesFound);
        }
    }
}
