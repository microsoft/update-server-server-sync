// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.ObjectModel;
using Microsoft.PackageGraph.Storage;
using Microsoft.PackageGraph.Storage.Index;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Prerequisites;
using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Index
{
    class PrerequisitesIndex : SimpleIndex<int, List<List<Guid>>>, ISimpleMetadataIndex<int , List<IPrerequisite>>
    {
        public const string Name = AvailableIndexes.PrerequisitesIndexName;

        public override IndexDefinition Definition => MicrosoftUpdatePartitionRegistration.Prerequisites;

        public PrerequisitesIndex(IIndexContainer container) : base(container, Name, MicrosoftUpdatePartitionRegistration.MicrosoftUpdatePartitionName)
        {
        }

        public override void IndexPackage(IPackage package, int packageIndex)
        {
            if (package is MicrosoftUpdatePackage microsoftUpdate && 
                microsoftUpdate.Prerequisites != null && 
                microsoftUpdate.Prerequisites.Count > 0)
            {
                var prerequisiteGuids = new List<List<Guid>>();
                foreach (var prereq in microsoftUpdate.Prerequisites)
                {
                    if (prereq is Simple simple)
                    {
                        prerequisiteGuids.Add(new List<Guid>() { simple.UpdateId });
                    }
                    else if (prereq is AtLeastOne atLeastOne)
                    {
                        prerequisiteGuids.Add(new List<Guid>(atLeastOne.Simple.Select(s => s.UpdateId)));
                        if (atLeastOne.IsCategory)
                        {
                            // Add an empty GUID to mark the list of guids as "category"
                            prerequisiteGuids.Last().Add(Guid.Empty);
                        }
                    }
                }

                base.Add(packageIndex, prerequisiteGuids);
            }
        }

        public bool TryGet(int packageIndex, out List<IPrerequisite> entry)
        {
            if (base.TryGet(packageIndex, out var prerequisitesList))
            {
                entry = new List<IPrerequisite>();
                foreach (var prerequisite in prerequisitesList)
                {
                    if (prerequisite.Count == 1)
                    {
                        entry.Add(new Simple(prerequisite.First()));
                    }
                    else
                    {
                        entry.Add(new AtLeastOne(prerequisite));
                    }
                }

                return true;
            }
            else
            {
                entry = null;
                return false;
            }
        }
    }
}
