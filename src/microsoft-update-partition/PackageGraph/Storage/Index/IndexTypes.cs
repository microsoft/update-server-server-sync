// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Microsoft.PackageGraph.Partitions;
using System;

namespace Microsoft.PackageGraph.Storage.Index
{
    abstract class AvailableIndexes
    {
        public const string TitlesIndexName = "titles";
        public const string DescriptionsIndexName = "descriptions";
        public const string CreationDatesIndexName = "creationDates";
    }

    class InternalIndexFactory : IIndexFactory
    {
        public IIndex CreateIndex(IndexDefinition definition, IIndexContainer container)
        {
            return definition.Name switch
            {
                AvailableIndexes.TitlesIndexName => new TitlesIndex(container),
                AvailableIndexes.DescriptionsIndexName => new DescriptionsIndex(container),
                AvailableIndexes.CreationDatesIndexName => new CreationDatesIndex(container),
                _ => throw new NotImplementedException(),
            };
        }
    }
}
