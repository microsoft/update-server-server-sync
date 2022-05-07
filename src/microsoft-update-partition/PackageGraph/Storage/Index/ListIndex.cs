// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.PackageGraph.Storage.Index
{
    abstract class ListIndex<I,T> : IIndex
    {
        protected Dictionary<I, List<T>> Index;

        protected IIndexStreamContainer _Container;

        public abstract IndexDefinition Definition { get; }

        protected string IndexName;
        protected string PartitionName;

        private bool IsIndexLoaded = false;

        public const int CurrentVersion = 0;

        protected bool SaveWithSwappedKeyValue = false;

        public bool IsDirty { get; private set; }

        protected ListIndex(IIndexContainer container, string indexName, string partitionName = null)
        {
            if (container is IIndexStreamContainer streamContainer)
            {
                _Container = streamContainer;
            }
            else
            {
                throw new Exception("Index container type not compatible with this index");
            }

            IndexName = indexName;
            PartitionName = partitionName;
            IsDirty = false;
        }

        public void Add(I key, T entry)
        {
            if (!IsIndexLoaded)
            {
                ReadIndex();
            }

            if (Index.TryGetValue(key, out var list))
            {
                list.Add(entry);
            }
            else
            {
                Index.Add(key, new List<T>() { entry });
            }

            IsDirty = true;
        }

        public void Save(Stream destination)
        {
            if (_Container == null)
            {
                throw new Exception("The index was initialized with a non-streamable container");
            }

            if (!IsIndexLoaded)
            {
                // The index was not loaded, hence it did not change. Simply copy the serialized index
                // from the old place to the new stream
                if (_Container.TryGetIndexReadStream(Definition, out Stream indexStream))
                {
                    using (indexStream)
                    {
                        indexStream.CopyTo(destination);
                    }
                }
                else
                {
                    Index = new Dictionary<I, List<T>>();
                    IsIndexLoaded = true;
                }
            }
            else if (SaveWithSwappedKeyValue)
            {
                IndexSerialization.SerializeIndexToStream(destination, Index.Select(pair => new KeyValuePair<List<T>, I>(pair.Value, pair.Key)));
            }
            else
            {
                IndexSerialization.SerializeIndexToStream(destination, Index);
            }
        }
        protected void ReadIndex()
        {
            if (_Container == null)
            {
                throw new Exception("The index was initialized with a non-streamable container");
            }

            lock (this)
            {
                if (!IsIndexLoaded)
                {
                    try
                    {
                        if (_Container.TryGetIndexReadStream(Definition, out Stream indexStream))
                        {
                            using (indexStream)
                            {
                                if (SaveWithSwappedKeyValue)
                                {
                                    var swappedList = IndexSerialization.DeserializeIndexFromStream<List<KeyValuePair<List<T>, I>>>(indexStream);
                                    Index = swappedList.ToDictionary(pair => pair.Value, pair => pair.Key);
                                }
                                else
                                {
                                    Index = IndexSerialization.DeserializeIndexFromStream<Dictionary<I, List<T>>>(indexStream);
                                }
                            }
                        }
                    }
                    catch (Exception) { }

                    if (Index == null)
                    {
                        Index = new Dictionary<I, List<T>>();
                    }
                }

                IsIndexLoaded = true;
            }
        }

        public bool TryGet(I key, out List<T> data)
        {
            if (!IsIndexLoaded)
            {
                ReadIndex();
            }
            if (Index.TryGetValue((I)key, out data))
            {
                return true;
            }
            else
            {
                data = null;
                return false;
            }
        }

        public abstract void IndexPackage(IPackage package, int packageIndex);
    }
}
