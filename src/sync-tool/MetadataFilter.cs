// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Storage;
using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Metadata.Prerequisites;
using Microsoft.UpdateServices.Server;
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
        private static List<Guid> StringGuidsToGuids(IEnumerable<string> stringGuids)
        {
            var returnList = new List<Guid>();
            foreach (var guidString in stringGuids)
            {
                if (!Guid.TryParse(guidString, out Guid guid))
                {
                    return null;
                }

                returnList.Add(guid);
            }

            return returnList;
        }

        public static RepositoryFilter RepositoryFilterFromCommandLineFilter(IUpdatesFilter filterOptions)
        {
            var filter = new RepositoryFilter()
            {
                TitleFilter = filterOptions.TitleFilter
            };

            filter.ClassificationFilter = StringGuidsToGuids(filterOptions.ClassificationsFilter);
            if (filter.ClassificationFilter == null)
            {
                ConsoleOutput.WriteRed("The classification filter must contain only GUIDs!");
                return null;
            }

            filter.ProductFilter = StringGuidsToGuids(filterOptions.ProductsFilter);
            if (filter.ProductFilter == null)
            {
                ConsoleOutput.WriteRed("The product ID filter must contain only GUIDs!");
                return null;
            }

            filter.IdFilter = StringGuidsToGuids(filterOptions.IdFilter);
            if (filter.IdFilter == null)
            {
                ConsoleOutput.WriteRed("The update ID filter must contain only GUIDs!");
                return null;
            }

            return filter;
        }
    }
}
