// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Metadata.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Storage
{
    public partial class CompressedMetadataStore
    {
        /// <summary>
        /// Files indexed by content hash
        /// </summary>
        private Dictionary<string, UpdateFileUrl> Files;

        /// <summary>
        /// Files indexes by update index
        /// </summary>
        private Dictionary<int, List<UpdateFile>> UpdateFilesIndex;

        private const string FilesIndexEntryName = "files-index.json";
        private const string UpdateFilesIndexEntryName = "update-files-index.json";

        void OnDeltaStore_InitializeFilesIndex()
        {
            Files = new Dictionary<string, UpdateFileUrl>();
            UpdateFilesIndex = new Dictionary<int, List<UpdateFile>>();
        }

        void OnNewStore_InitializeFilesIndex()
        {
            Files = new Dictionary<string, UpdateFileUrl>();
            UpdateFilesIndex = new Dictionary<int, List<UpdateFile>>();
        }

        private void AddUpdateFileInformationToIndex(int newUpdateIndex, Identity newUpdateIdentity, XDocument updateXml)
        {
            var files = UpdateFileParser.Parse(updateXml, this);
            if (files.Count > 0)
            {
                UpdateFilesIndex.Add(newUpdateIndex, files);
            }
        }

        private void SaveFilesIndex()
        {
            CommitProgress?.Invoke(this, new OperationProgress() { CurrentOperation = OperationType.IndexingFilesStart });
            SerializeIndexToArchive(FilesIndexEntryName, Files);
            SerializeIndexToArchive(UpdateFilesIndexEntryName, UpdateFilesIndex);
            CommitProgress?.Invoke(this, new OperationProgress() { CurrentOperation = OperationType.IndexingFilesEnd });
        }

        /// <summary>
        /// Reads the index of files indexed by hash
        /// </summary>
        private void ReadFilesIndex()
        {
            Files = DeserializeIndexFromArchive<Dictionary<string, UpdateFileUrl>>(FilesIndexEntryName);
        }

        /// <summary>
        /// Reads the index of files indexed by update index
        /// </summary>
        private void ReadUpdateFilesIndex()
        {
            UpdateFilesIndex = DeserializeIndexFromArchive<Dictionary<int, List<UpdateFile>>>(UpdateFilesIndexEntryName);
        }

        /// <summary>
        /// Checks if the metadata source contains URL information for a file identified by its content checksum
        /// </summary>
        /// <param name="checksum">The file contents checksum</param>
        /// <returns>True if the store contains file URL information, false otherwise</returns>
        public bool HasFile(string checksum)
        {
            lock (this)
            {
                if (Files == null)
                {
                    ReadFilesIndex();
                }
            }

            if (Files.TryGetValue(checksum, out UpdateFileUrl fileUrl))
            {
                return true;
            }
            else if (IsDeltaSource)
            {
                return BaselineSource.HasFile(checksum);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Retrieves url information for a file
        /// </summary>
        /// <param name="checksum">The file checksum</param>
        /// <returns>Update URL information</returns>
        public UpdateFileUrl GetFile(string checksum)
        {
            lock (this)
            {
                if (Files == null)
                {
                    ReadFilesIndex();
                }
            }

            if (Files.TryGetValue(checksum, out UpdateFileUrl fileUrl))
            {
                return fileUrl;
            }
            else if (IsDeltaSource)
            {
                return BaselineSource.GetFile(checksum);
            }
            else
            {
                throw new Exception($"The metadata source does not contain a file entry with checksum {checksum}");
            }
        }


        /// <summary>
        /// Checks if an update contains files
        /// </summary>
        /// <param name="updateIdentity">Update identity</param>
        /// <returns>True if the update contains files, false otherwise</returns>
        public bool HasFiles(Identity updateIdentity)
        {
            return GetFiles(this[updateIdentity]) != null;
        }

        private List<UpdateFile> GetFiles(int updateIndex)
        {
            lock (this)
            {
                if (UpdateFilesIndex == null)
                {
                    ReadUpdateFilesIndex();
                }
            }

            if (UpdateFilesIndex.TryGetValue(updateIndex, out List<UpdateFile> files))
            {
                return files;
            }
            else if (IsInBaseline(updateIndex))
            {
                return BaselineSource.GetFiles(updateIndex);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieves files for an update
        /// </summary>
        /// <param name="updateIdentity">Update identity</param>
        /// <returns>List of files in the update</returns>
        public List<UpdateFile> GetFiles(Identity updateIdentity)
        {
            return GetFiles(this[updateIdentity]);
        }
    }
}
