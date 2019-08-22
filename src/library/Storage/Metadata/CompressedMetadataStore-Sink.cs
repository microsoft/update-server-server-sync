// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ICSharpCode.SharpZipLib.Zip;
using Microsoft.UpdateServices.Client;
using Microsoft.UpdateServices.Compression;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Metadata.Content;
using Microsoft.UpdateServices.Metadata.Prerequisites;
using Microsoft.UpdateServices.Storage;
using Microsoft.UpdateServices.WebServices.ServerSync;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Storage
{
    public partial class CompressedMetadataStore : IMetadataSink
    {
        private Dictionary<int, Update> AddedUpdates = new Dictionary<int, Update>();

        /// <summary>
        /// Progress notifications during the commit phase of creating an updates metadata source
        /// </summary>
        public event EventHandler<OperationProgress> CommitProgress;

        /// <summary>
        /// Flushes out all query results content to the output file and puts the metadata store in read mode
        /// </summary>
        public void Commit()
        {
            if (OutputFile != null && !OutputFile.IsFinished)
            {
                SaveTitlesIndex();
                SavePrerequisitesIndex();
                SaveBundlesIndex();
                SaveProductClassificationIndex();
                SaveFilesIndex();
                SaveSupersededndex();

                // Come last, after all indexes have been updated
                WriteIndex();
                OutputFile.Finish();
                OutputFile.Close();
                OutputFile = null;

                InputFile = new ZipFile(FilePath);
            }
        }

        /// <summary>
        /// Adds a list of updates to the query result. The XML metadata is written to disk to avoid running out of memory
        /// </summary>
        /// <param name="overTheWireUpdates">The updates to add to the result</param>
        public void AddUpdates(IEnumerable<ServerSyncUpdateData> overTheWireUpdates)
        {
            foreach (var overTheWireUpdate in overTheWireUpdates)
            {
                var updateIdentity = new Identity(overTheWireUpdate.Id);

                bool newEntryToBeAdded = false;
                lock (Identities)
                {
                    if (!Identities.Contains(updateIdentity))
                    {
                        newEntryToBeAdded = true;
                    }
                }

                if (newEntryToBeAdded)
                {
                    // We need to parse the XML update blob
                    string updateXml = overTheWireUpdate.XmlUpdateBlob;
                    if (string.IsNullOrEmpty(updateXml))
                    {
                        // If the plain text blob is not availabe, use the compressed XML blob
                        if (overTheWireUpdate.XmlUpdateBlobCompressed == null || overTheWireUpdate.XmlUpdateBlobCompressed.Length == 0)
                        {
                            throw new Exception("Missing XmlUpdateBlobCompressed");
                        }

                        // Note: This only works on Windows.
                        updateXml = CabinetUtility.DecompressData(overTheWireUpdate.XmlUpdateBlobCompressed);
                    }

                    var xdoc = XDocument.Parse(updateXml, LoadOptions.None);
                    var newUpdate = Update.FromUpdateXml(updateIdentity, xdoc);
                    AddUpdate(newUpdate, updateXml, out var newUpdateIndex, xdoc);
                }
            }
        }

        /// <summary>
        /// Adds an update to the query result. The XML metadata is written to disk to avoid running out of memory
        /// </summary>
        /// <param name="update">The updates to add to the result</param>
        /// <param name="updateMetadata">The update metadata to add</param>
        /// <param name="newUpdateIndex">The index of the new entry, if it was added to the index</param>
        /// <param name="updateXmlDoc">Update XML document used to load additional data if needed</param>
        /// <returns>True if a new entry was added, false otherwise</returns>
        private bool AddUpdate(Update update, string updateMetadata, out int newUpdateIndex, XDocument updateXmlDoc)
        {
            var updateIdentity = update.Identity;
            newUpdateIndex = 0;

            lock (Identities)
            {
                if (!Identities.Contains(updateIdentity))
                {
                    newUpdateIndex = AddUpdateEntry(update, updateXmlDoc);

                    var updateEntryName = GetUpdateXmlPath(updateIdentity);
                    OutputFile.PutNextEntry(new ZipEntry(updateEntryName));
                    OutputFile.Write(Encoding.UTF8.GetBytes(updateMetadata));
                    OutputFile.CloseEntry();
                    OutputFile.Flush();

                    return true;
                }
            }

            return false;
        }

        private int AddUpdateEntry(Update update, XDocument updateXmlDoc)
        {
            var newUpdateIndex = Identities.Count;
            Identities.Add(update.Identity);

            IdentityToIndex.Add(update.Identity, newUpdateIndex);
            IndexToIdentity.Add(newUpdateIndex, update.Identity);

            if (update is Detectoid)
            {
                UpdateTypeMap.Add(newUpdateIndex, (uint)UpdateType.Detectoid);
                Categories.TryAdd(update.Identity, update);
            }
            else if (update is Classification)
            {
                UpdateTypeMap.Add(newUpdateIndex, (uint)UpdateType.Classification);
                Categories.TryAdd(update.Identity, update);
            }
            else if (update is Product)
            {
                UpdateTypeMap.Add(newUpdateIndex, (uint)UpdateType.Product);
                Categories.TryAdd(update.Identity, update);
            }
            else if (update is SoftwareUpdate)
            {
                UpdateTypeMap.Add(newUpdateIndex, (uint)UpdateType.Software);
                Updates.TryAdd(update.Identity, update);
            }
            else if (update is DriverUpdate)
            {
                UpdateTypeMap.Add(newUpdateIndex, (uint)UpdateType.Driver);
                Updates.TryAdd(update.Identity, update);
            }

            ExtractAndIndexTitle(newUpdateIndex, updateXmlDoc);

            AddedUpdates.Add(newUpdateIndex, update);

            AddUpdateBundleInformation(newUpdateIndex, update.Identity, updateXmlDoc);
            AddPrerequisiteInformation(newUpdateIndex, update.Identity, updateXmlDoc);
            AddUpdateFileInformationToIndex(newUpdateIndex, update.Identity, updateXmlDoc);
            ExtractSupersedingInformation(newUpdateIndex, update.Identity, updateXmlDoc);

            return newUpdateIndex;
        }

        /// <summary>
        /// Sets the filter used when adding updates to the metadata collection
        /// </summary>
        /// <param name="filter">Filter</param>
        public void SetQueryFilter(QueryFilter filter)
        {
            if (OutputFile == null)
            {
                throw new Exception("QueryResult in not in write mode");
            }

            Filter = new QueryFilter()
            {
                ClassificationsFilter = new List<Identity>(filter.ClassificationsFilter),
                ProductsFilter = new List<Identity>(filter.ProductsFilter),
                Anchor = filter.Anchor
            };
        }

        /// <summary>
        /// Sets the categories anchor for categories in this collection
        /// </summary>
        /// <param name="anchor"></param>
        public void SetCategoriesAnchor(string anchor)
        {
            if (OutputFile == null)
            {
                throw new Exception("QueryResult in not in write mode");
            }

            CategoriesAnchor = anchor;
        }

        /// <summary>
        /// Adds an update content file URL () to the result
        /// </summary>
        /// <param name="file">The file to add</param>
        public void AddFile(UpdateFileUrl file)
        {
            if (OutputFile == null)
            {
                throw new Exception("QueryResult in not in write mode");
            }

            lock (Files)
            {
                if (!Files.ContainsKey(file.DigestBase64))
                {
                    Files.Add(file.DigestBase64, file);
                }
            }
        }
    }
}
