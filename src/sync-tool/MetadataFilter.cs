// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Metadata.Prerequisites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.UpdateServices.Tools.UpdateRepo
{
    /// <summary>
    /// Class for filtering updates by various metadata filters
    /// </summary>
    class MetadataFilter
    {
        /// <summary>
        /// Applies a command line based filter to a list of updates
        /// </summary>
        /// <param name="updates">The updates to filter. Updates that do not pass the filter are removed from this list.</param>
        /// <param name="filter">The command line filter specified by the user.</param>
        public static void Apply(List<MicrosoftUpdate> updates, IUpdatesFilter filter)
        {
            // Apply the classification filter
            foreach (var classificationFilter in filter.ClassificationsFilter)
            {
                if (!Guid.TryParse(classificationFilter, out Guid classificationId))
                {
                    ConsoleOutput.WriteRed("The classification filter must contain only GUIDs!");
                    return;
                }

                updates.RemoveAll(u => !(u as IUpdateWithClassification).ClassificationIds.Contains(classificationId));
            }

            // Apply the product filter
            foreach (var productFilter in filter.ProductsFilter)
            {
                if (!Guid.TryParse(productFilter, out Guid productId))
                {
                    ConsoleOutput.WriteRed("The product ID filter must contain only GUIDs!");
                    return;
                }

                updates.RemoveAll(u => !(u as IUpdateWithProduct).ProductIds.Contains(productId));
            }

            if (!string.IsNullOrEmpty(filter.TitleFilter))
            {
                var filterTokens = filter.TitleFilter.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                updates.RemoveAll(category => !category.MatchTitle(filterTokens));
            }

            if (filter.IdFilter.Count() > 0)
            {
                var idFilter = new List<Guid>();
                foreach (var stringId in filter.IdFilter)
                {
                    if (!Guid.TryParse(stringId, out Guid guidId))
                    {
                        ConsoleOutput.WriteRed("The ID filter must be a GUID string!");
                        return;
                    }

                    idFilter.Add(guidId);
                }

                // Remove all updates that don't match the ID filter
                updates.RemoveAll(u => !idFilter.Contains(u.Identity.Raw.UpdateID));
            }
        }
    }
}
