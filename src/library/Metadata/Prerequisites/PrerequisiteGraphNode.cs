using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UpdateServices.Metadata.Prerequisites
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
