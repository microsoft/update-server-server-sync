// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Prerequisites
{
    class PrerequisiteGraphNode
    {
        public Dictionary<Guid, PrerequisiteGraphNode> Dependents;
        public Dictionary<Guid, PrerequisiteGraphNode> Prerequisites;

        public Guid UpdateId;

        public PrerequisiteGraphNode(Guid updateId)
        {
            Dependents = new Dictionary<Guid, PrerequisiteGraphNode>();
            Prerequisites = new Dictionary<Guid, PrerequisiteGraphNode>();
            UpdateId = updateId;
        }
    }
}
