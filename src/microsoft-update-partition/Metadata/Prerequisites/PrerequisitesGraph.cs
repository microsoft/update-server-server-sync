// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.Storage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Prerequisites
{
    /// <summary>
    /// Models the prerequisite graph for all packages contained within a metadata store
    /// </summary>
    public class PrerequisitesGraph
    {
        private readonly Dictionary<Guid, PrerequisiteGraphNode> Graph;

        private PrerequisitesGraph(Dictionary<Guid, PrerequisiteGraphNode> graph)
        {
            Graph = graph;
        }

        /// <summary>
        /// Creates a prerequisite graph for all the packages contained in the specified store
        /// </summary>
        /// <param name="source">Package metadata store</param>
        /// <returns></returns>
        /// <exception cref="Exception">If an unknown prerequisite type is encountered</exception>
        public static PrerequisitesGraph FromIndexedPackageSource(IMetadataStore source)
        {
            var graph = new Dictionary<Guid, PrerequisiteGraphNode>();

            var allMicrosoftUpdatePackages = source.OfType<MicrosoftUpdatePackage>();
            var updatesWithPrerequisites = allMicrosoftUpdatePackages.Where(update => update.Prerequisites != null && update.Prerequisites.Count > 0);

            foreach (var updateWithPrerequisites in updatesWithPrerequisites)
            {
                var updateGuid = updateWithPrerequisites.Id.ID;
                PrerequisiteGraphNode updateNode;
                if (!graph.ContainsKey(updateGuid))
                {
                    updateNode = new PrerequisiteGraphNode(updateGuid);
                    graph.Add(updateGuid, updateNode);
                }
                else
                {
                    updateNode = graph[updateGuid];
                }

                var prerequisites = updateWithPrerequisites.Prerequisites;
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
                    if (graph.ContainsKey(prerequisite))
                    {
                        prerequisiteNode = graph[prerequisite];
                    }
                    else
                    {
                        prerequisiteNode = new PrerequisiteGraphNode(prerequisite);
                        graph.Add(prerequisite, prerequisiteNode);
                    }

                    updateNode.Prerequisites.TryAdd(prerequisite, prerequisiteNode);
                    prerequisiteNode.Dependents.TryAdd(updateGuid, updateNode);
                }
            }

            return new PrerequisitesGraph(graph);
        }

        /// <summary>
        /// Gets updates that have prerequisites but no other update depends on them
        /// </summary>
        /// <returns>List of GUIDS of leaf updates</returns>
        public IEnumerable<Guid> GetLeafUpdates() => Graph.Values.Where(node => node.Dependents.Count == 0).Select(node => node.UpdateId);

        /// <summary>
        /// Gets updates that have prerequisites and also have other updates depend on them
        /// </summary>
        /// <returns>List of GUIDS of non leaf updates</returns>
        public IEnumerable<Guid> GetNonLeafUpdates() => Graph.Values.Where(node => node.Dependents.Count > 0 && node.Prerequisites.Count > 0).Select(node => node.UpdateId);

        /// <summary>
        /// Get updates with no prerequisites
        /// </summary>
        /// <returns>List of GUIDS of root updates</returns>
        public IEnumerable<Guid> GetRootUpdates() => Graph.Values.Where(node => node.Prerequisites.Count == 0).Select(node => node.UpdateId);
    }
}
