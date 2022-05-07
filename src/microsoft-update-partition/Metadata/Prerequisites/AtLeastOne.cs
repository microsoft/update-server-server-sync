// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Prerequisites
{
    /// <summary>
    /// A collection of prerequisites, of which at least one must be met for the AtLeastOne prerequisite to be satisfied.
    /// </summary>
    public class AtLeastOne : IPrerequisite
    {
        /// <summary>
        /// Get the list of simple prerequisites that are part of the group
        /// </summary>
        /// <value>
        /// List of simple prerequisites
        /// </value>
        public List<Simple> Simple { get; private set; }

        /// <summary>
        /// Check if the AtLestOne prerequisite is a "category" prerequisite. Category prerequisites are not true prerequisites,
        /// just a way to encode a product and classification for an update.
        /// </summary>
        public bool IsCategory { get; private set; }

        internal AtLeastOne(IEnumerable<Guid> ids)
        {
            Simple = new List<Simple>(ids.Select(id => new Prerequisites.Simple(id)));
            IsCategory = ids.Last().Equals(Guid.Empty);

            if (IsCategory)
            {
                Simple.RemoveAt(Simple.Count - 1);
            }
        }

        internal AtLeastOne(IEnumerable<Guid> ids, bool isCategory)
        {
            Simple = new List<Simple>(ids.Select(id => new Prerequisites.Simple(id)));
            IsCategory = isCategory;
        }
    }
}
