﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Compression;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.Metadata.Prerequisites;
using Microsoft.UpdateServices.WebServices.ServerReporting;
using Microsoft.UpdateServices.WebServices.ServerSync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Storage
{
    class WsusExport
    {
        private readonly IMetadataSource MetadataSource;
        ServerSyncConfigData ServiceConfiguration;

        public event EventHandler<OperationProgress> ExportProgress;

        public WsusExport(IMetadataSource source, ServerSyncConfigData serviceConfiguration)
        {
            MetadataSource = source;
            ServiceConfiguration = serviceConfiguration;
        }

        /// <summary>
        /// Exports the specified updates from a local update metadata source to a format compatible with WSUS 2016
        /// </summary>
        /// <param name="updatesToExport">The updates to export. All categories from the source are also exported</param>
        /// <param name="exportFilePath">The export destination file (CAB)</param>
        public void Export(List<Update> updatesToExport, string exportFilePath)
        {
            var exportDirectory = Directory.GetParent(exportFilePath);
            if (!exportDirectory.Exists)
            {
                Directory.CreateDirectory(exportDirectory.FullName);
            }

            // Pack all XML blobs for updates to be exported into a flat text file
            var progress = new OperationProgress() { CurrentOperation = OperationType.ExportUpdateXmlBlobStart };
            ExportProgress?.Invoke(this, progress);

            var metadataFile = Path.Combine(exportDirectory.FullName, "metadata.txt");
            WriteMetadataFile(updatesToExport, metadataFile);

            progress.CurrentOperation = OperationType.ExportUpdateXmlBlobEnd;
            ExportProgress?.Invoke(this, progress);

            // Write metadata for all exported updates, languages and files
            progress.CurrentOperation = OperationType.ExportMetadataStart;
            ExportProgress?.Invoke(this, progress);

            var packageXmlFile = Path.Combine(exportDirectory.FullName, "package.xml");
            WritePackagesXml(updatesToExport, packageXmlFile);

            progress.CurrentOperation = OperationType.ExportMetadataEnd;
            ExportProgress?.Invoke(this, progress);

            // Add the above 2 files to a CAB archive
            progress.CurrentOperation = OperationType.CompressExportFileStart;
            ExportProgress?.Invoke(this, progress);

            var result = CabinetUtility.CompressFiles(new List<string>() { metadataFile, packageXmlFile }, exportFilePath);

            progress.CurrentOperation = OperationType.CompressExportFileEnd;
            ExportProgress?.Invoke(this, progress);

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

        /// <summary>
        /// Creates the metadata.txt file for a list of updates to export.
        /// Copies update IDs and XML data to this file
        /// </summary>
        /// <param name="updatesToExport">The updates to export</param>
        /// <param name="metadataTextFile">Destination metadata file</param>
        private void WriteMetadataFile(List<Update> updatesToExport, string metadataTextFile)
        {
            // Each line in the metadata text file contains multiple lines of the following format:
            // <update GUID>,<update revision>,<xml size>,<xml>\r\n
            // There is one line for each update exported

            // Open the metadata file for writing
            using (var metadataFile = File.CreateText(metadataTextFile))
            {
                var allUpdates = new List<Update>(MetadataSource.GetCategories());
                allUpdates.AddRange(updatesToExport);

                var progress = new OperationProgress() { CurrentOperation = OperationType.ExportUpdateXmlBlobProgress, Maximum = allUpdates.Count, Current = 0 };
                foreach (var update in allUpdates)
                {                    
                    using (var metadataStream = MetadataSource.GetUpdateMetadataStream(update.Identity))
                    {
                        using (var metadataReader = new StreamReader(metadataStream))
                        {
                            var xmlData = metadataReader.ReadToEnd();

                            // Write one line with GUID, revision, XML length, XML data
                            metadataFile.WriteLine("{0},{1:x8},{2:x8},{3}", update.Identity.Raw.UpdateID, update.Identity.Raw.RevisionNumber, xmlData.Length, xmlData);
                        }
                    }

                    progress.Current += 1;
                    ExportProgress?.Invoke(this, progress);
                }
            }
        }

        /// <summary>
        /// Writes the packages.xml file for a list of updates to export
        /// </summary>
        /// <param name="updates">The updates to export</param>
        /// <param name="packagesFilePath">Destination file to write the XML to</param>
        private void WritePackagesXml(
            List<Update> updates,
            string packagesFilePath)
        {
            XDocument packagesXml = new XDocument();

            XElement exportElement = new XElement("ExportPackage");
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

            using (var packagesFile = File.OpenWrite(packagesFilePath))
            {
                using (var xmlWriter = XmlWriter.Create(packagesFile, new XmlWriterSettings() { Encoding = new UTF8Encoding() }))
                {
                    packagesXml.Save(xmlWriter);
                }
            }
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

        private static XElement CreateFilesElement(List<Update> updates)
        {
            var filesToExport = updates.Where(u => u.HasFiles).SelectMany(u => u.Files).Distinct();

            // Add all the files
            // Get the distinct list of files to export
            var filesElement = new XElement("Files");
            foreach (var file in filesToExport)
            {
                var fileElement = new XElement("File");
                fileElement.Add(new XAttribute("Digest", file.Digests[0].DigestBase64));

                if (file.Urls.Count == 0)
                {
                    continue;
                }

                fileElement.Add(new XAttribute("MUUrl", file.Urls[0].MuUrl));
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
            var allCategories = MetadataSource.GetCategories();
            foreach (var category in allCategories)
            {
                var categoryElement = new XElement("Update");
                categoryElement.Add(new XAttribute("UpdateId", category.Identity.Raw.UpdateID));
                categoryElement.Add(new XAttribute("RevisionNumber", category.Identity.Raw.RevisionNumber));

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
        private static List<XElement> CreateUpdatesElements(List<Update> updates)
        {
            var categoriesElements = new List<XElement>();

            foreach (var update in updates)
            {
                var updateElement = new XElement("Update");
                updateElement.Add(new XAttribute("UpdateId", update.Identity.Raw.UpdateID));
                updateElement.Add(new XAttribute("RevisionNumber", update.Identity.Raw.RevisionNumber));

                // Add the update's files
                var filesElement = new XElement("Files");
                if (update.HasFiles)
                {
                    foreach (var file in update.Files)
                    {
                        var fileElement = new XElement("File");
                        fileElement.Add(new XAttribute("Digest", file.Digests[0].DigestBase64));
                        filesElement.Add(fileElement);
                    }
                }
                updateElement.Add(filesElement);

                // Add the update's categories
                var categoriesElement = new XElement("Categories");
                if (update.HasProduct)
                {
                    var products = update.ProductIds;
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
                if (update.HasClassification)
                {
                    var classifications = update.ClassificationIds;
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
        public static void CompleteTheListOfExportUpdates(List<Update> updatesToExport, IMetadataSource source)
        {
            bool additionalUpdatesFound = false;
            do
            {
                var additionalUpdates = new List<Identity>();
                foreach (var selectedUpdate in updatesToExport)
                {
                    if (selectedUpdate.IsBundle)
                    {
                        foreach (var bundledUpdate in selectedUpdate.BundledUpdates)
                        {
                            if (!updatesToExport.Any(u => u.Identity.Equals(bundledUpdate)))
                            {
                                additionalUpdates.Add(bundledUpdate);
                            }
                        }
                    }
                }

                foreach (var additionalUpdate in additionalUpdates)
                {
                    // Bundled updates should appear in the list before the updates that bundle them
                    updatesToExport.Insert(0, source.GetUpdate(additionalUpdate));
                }

                additionalUpdatesFound = additionalUpdates.Count > 0;
            } while (additionalUpdatesFound);
        }
    }
}
