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
        private Dictionary<int, string> KbArticleIndex;

        private const string KbArticleIndexEntryName = "kbarticle-index.json";

        private void ExtractAndIndexKbArticle(int updateIndex, XDocument xdoc, Update update)
        {
            if (update is SoftwareUpdate softwareUpdate)
            {
                var kbArticle = SoftwareUpdate.GetPropertiesFromXml(xdoc).kbArticle;

                if (!string.IsNullOrEmpty(kbArticle))
                {
                    KbArticleIndex.Add(updateIndex, kbArticle);
                }
            }
        }

        private void OnNewStore_InitializeKbArticles()
        {
            KbArticleIndex = new Dictionary<int, string>();
        }

        private void OnDeltaStore_InitializeKbArticles()
        {
            KbArticleIndex = new Dictionary<int, string>();
        }

        private void SaveKbArticleIndex()
        {
            SerializeIndexToArchive(KbArticleIndexEntryName, KbArticleIndex);
        }

        private void ReadKbArticleIndex()
        {
            KbArticleIndex = DeserializeIndexFromArchive<Dictionary<int, string>>(KbArticleIndexEntryName);
        }

        /// <summary>
        /// Retrieves the KB article for a software update
        /// </summary>
        /// <param name="updateIdentity">update identity</param>
        /// <returns></returns>
        public string GetKbArticle(Identity updateIdentity)
        {
            return GetKbArticle(IdentityToIndex[updateIdentity]);
        }

        private string GetKbArticle(int updateIndex)
        {
            lock (this)
            {
                if (KbArticleIndex == null)
                {
                    ReadKbArticleIndex();
                }
            }

            if (KbArticleIndex.TryGetValue(updateIndex, out string title))
            {
                return title;
            }
            else if (IsInBaseline(updateIndex))
            {
                return BaselineSource.GetKbArticle(updateIndex);
            }
            else
            {
                return null;
            }
        }
    }
}
