// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.MicrosoftUpdate.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PackageGraph.Utilitites.Upsync
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

        public static MetadataFilter MicrosoftUpdateFilterFromCommandLine(IMetadataFilterOptions filterOptions)
        {
            var filter = new MetadataFilter()
            {
                TitleFilter = filterOptions.TitleFilter,
                HardwareIdFilter = filterOptions.HardwareIdFilter,
                KbArticleFilter = filterOptions.KbArticleFilter.ToList()
            };

            if (!string.IsNullOrEmpty(filterOptions.ComputerHardwareIdFilter))
            {
                if (!Guid.TryParse(filterOptions.ComputerHardwareIdFilter, out Guid computerHardwareIdFilterGuid))
                {
                    ConsoleOutput.WriteRed($"The computer hardware id must be a GUID. It was {filterOptions.ComputerHardwareIdFilter}");
                    return null;
                }
                else
                {
                    filter.ComputerHardwareIdFilter = computerHardwareIdFilterGuid;
                }
            }

            var classificationFilter = StringGuidsToGuids(filterOptions.ClassificationsFilter);
            if (classificationFilter == null)
            {
                ConsoleOutput.WriteRed("The classification filter must contain only GUIDs!");
                return null;
            }

            var productsFilter = StringGuidsToGuids(filterOptions.ProductsFilter);
            if (productsFilter == null)
            {
                ConsoleOutput.WriteRed("The product ID filter must contain only GUIDs!");
                return null;
            }

            filter.CategoryFilter = new List<Guid>(productsFilter);
            filter.CategoryFilter.AddRange(classificationFilter);

            filter.IdFilter = StringGuidsToGuids(filterOptions.IdFilter);
            if (filter.IdFilter == null)
            {
                ConsoleOutput.WriteRed("The update ID filter must contain only GUIDs!");
                return null;
            }

            filter.SkipSuperseded = filterOptions.SkipSuperseded;
            filter.FirstX = filterOptions.FirstX;

            return filter;
        }
    }
}
