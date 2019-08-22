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
    public partial class CompressedMetadataStore
    {

        Dictionary<Guid, PrerequisiteGraphNode> Graph;

        /// <summary>
        /// Gets updates that have prerequisites and no other update depends on them
        /// </summary>
        /// <returns>List of GUIDS of leaf updates</returns>
        public IEnumerable<Guid> GetLeafUpdates()
        {
            CreateGraph();
            return Graph.Values.Where(node => node.Dependents.Count == 0).Select(node => node.UpdateId);
        }

        /// <summary>
        /// Gets updates that have prerequisites and have other updates depende on them
        /// </summary>
        /// <returns>List of GUIDS of non leaf updates</returns>
        public IEnumerable<Guid> GetNonLeafUpdates()
        {
            CreateGraph();
            return Graph.Values.Where(node => node.Dependents.Count > 0 && node.Prerequisites.Count > 0).Select(node => node.UpdateId);
        }

        /// <summary>
        /// Get updates with no prerequisites
        /// </summary>
        /// <returns>List of GUIDS of root updates</returns>
        public IEnumerable<Guid> GetRootUpdates()
        {
            CreateGraph();
            return Graph.Values.Where(node => node.Prerequisites.Count == 0).Select(node => node.UpdateId);
        }

        private void CreateGraph()
        {
            if (Graph != null)
            {
                return;
            }

            CommitProgress?.Invoke(this, new OperationProgress() { CurrentOperation = OperationType.PrerequisiteGraphUpdateStart });

            Graph = new Dictionary<Guid, PrerequisiteGraphNode>();

            var updateIndexesWithPrerequisites = IndexToIdentity.Keys.Where(i => HasPrerequisites(i)).ToList();
            var progress = new OperationProgress() { CurrentOperation = OperationType.PrerequisiteGraphUpdateProgress, Maximum = updateIndexesWithPrerequisites.Count };

            foreach (var updateIndex in updateIndexesWithPrerequisites)
            {
                PrerequisiteGraphNode updateNode;
                if (!Graph.ContainsKey(this[updateIndex].ID))
                {
                    updateNode = new PrerequisiteGraphNode(this[updateIndex].ID);
                    Graph.Add(this[updateIndex].ID, updateNode);
                }
                else
                {
                    updateNode = Graph[this[updateIndex].ID];
                }

                var prerequisites = GetPrerequisites(updateIndex);
                var flatListPrerequisites = prerequisites.SelectMany(p =>
                {
                    if (p is Simple)
                    {
                        return new List<Guid>() { (p as Simple).UpdateId };
                    }
                    else if (p is AtLeastOne)
                    {
                        return (p as AtLeastOne).Simple.Select(s => s.UpdateId);
                    }
                    else
                    {
                        throw new Exception("Unknown prerequisite type");
                    }
                });

                foreach (var prerequisite in flatListPrerequisites)
                {
                    PrerequisiteGraphNode prerequisiteNode;
                    if (Graph.ContainsKey(prerequisite))
                    {
                        prerequisiteNode = Graph[prerequisite];
                    }
                    else
                    {
                        prerequisiteNode = new PrerequisiteGraphNode(prerequisite);
                        Graph.Add(prerequisite, prerequisiteNode);
                    }

                    updateNode.Prerequisites.TryAdd(prerequisite, prerequisiteNode);
                    prerequisiteNode.Dependents.TryAdd(this[updateIndex].ID, updateNode);
                }

                progress.Current++;
                if (progress.Current % 1000 == 0)
                {
                    CommitProgress?.Invoke(this, progress);
                }
            }

            CommitProgress?.Invoke(this, new OperationProgress() { CurrentOperation = OperationType.PrerequisiteGraphUpdateEnd });
        }
    }
}
