// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Metadata;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Microsoft.UpdateServices.Storage
{
    public partial class CompressedMetadataStore
    {
        private Dictionary<int, string> UpdateTitlesIndex;

        private const string TitleIndexEntryName = "titles.json";

        private void ExtractAndIndexTitle(int updateIndex, XDocument xdoc)
        {
            UpdateTitlesIndex.Add(updateIndex, Update.GetTitleFromXml(xdoc));
        }

        private void SaveTitlesIndex()
        {
            CommitProgress?.Invoke(this, new OperationProgress() { CurrentOperation = OperationType.IndexingTitlesStart });
            SerializeIndexToArchive(TitleIndexEntryName, UpdateTitlesIndex);
            CommitProgress?.Invoke(this, new OperationProgress() { CurrentOperation = OperationType.IndexingTitlesEnd });
        }

        private void ReadTitlesIndex()
        {
            UpdateTitlesIndex = DeserializeIndexFromArchive<Dictionary<int, string>>(TitleIndexEntryName);
        }

        /// <summary>
        /// Retrieves the title of an update
        /// </summary>
        /// <param name="updateIdentity"></param>
        /// <returns></returns>
        public string GetUpdateTitle(Identity updateIdentity)
        {
            return GetUpdateTitle(IdentityToIndex[updateIdentity]);
        }

        private string GetUpdateTitle(int updateIndex)
        {
            lock (this)
            {
                // lazy load the titles index
                if (UpdateTitlesIndex == null)
                {
                    ReadTitlesIndex();
                }
            }

            string title;
            if (UpdateTitlesIndex.TryGetValue(updateIndex, out title))
            {
                return title;
            }
            else if (IsDeltaSource)
            {
                title = BaselineSource.GetUpdateTitle(updateIndex);
            }
            else
            {
                throw new Exception($"Update title for {IndexToIdentity[updateIndex]} was not found");
            }

            return title;
        }
    }
}
