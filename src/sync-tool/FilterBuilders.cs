// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.UpdateServices.Metadata;
using Microsoft.UpdateServices.Storage;
using System;
using System.Collections.Generic;

namespace Microsoft.UpdateServices.Tools.UpdateRepo
{
    /// <summary>
    /// Class for building metadata filters from command line options
    /// </summary>
    class FilterBuilder
    {
        internal static List<Guid> StringGuidsToGuids(IEnumerable<string> stringGuids)
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

        public static MetadataFilter MetadataFilterFromCommandLine(IMetadataFilterOptions filterOptions)
        {
            var filter = new MetadataFilter()
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

            filter.SkipSuperseded = filterOptions.SkipSuperseded;

            return filter;
        }

        public static MetadataFilter QueryFilterFromCommandLineFilter(ISyncQueryFilter filterOptions)
        {
            var filter = new MetadataFilter();

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

            return filter;
        }
    }
}
