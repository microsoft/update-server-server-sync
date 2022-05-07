// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.PackageGraph.ObjectModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.PackageGraph.Storage.Index
{
    abstract class SimpleIndex<I,T> : IIndex
    {
        protected Dictionary<I, T> Index;

        protected IIndexStreamContainer _Container;

        public abstract IndexDefinition Definition { get; }

        protected string IndexName;
        protected string PartitionName;

        private bool IsIndexLoaded = false;

        public const int CurrentVersion = 0;

        protected bool SaveWithSwappedKeyValue = false;

        public bool IsDirty { get; private set; }

        protected SimpleIndex(IIndexContainer container, string indexName, string partitionName = null)
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
            IsDirty = true;

            if (!IsIndexLoaded)
            {
                ReadIndex();
            }

            Index.Add(key, entry);
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
                    Index = new Dictionary<I, T>();
                    IsIndexLoaded = true;
                }
            }
            else if (SaveWithSwappedKeyValue)
            {
                IndexSerialization.SerializeIndexToStream(destination, Index.Select(pair => new KeyValuePair<T, I>(pair.Value, pair.Key)));
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
                                    var swappedList = IndexSerialization.DeserializeIndexFromStream<List<KeyValuePair<T, I>>>(indexStream);
                                    Index = swappedList.ToDictionary(pair => pair.Value, pair => pair.Key);
                                }
                                else
                                {
                                    Index = IndexSerialization.DeserializeIndexFromStream<Dictionary<I, T>>(indexStream);
                                }
                            }
                        }
                    }
                    catch (Exception) { }

                    if (Index == null)
                    {
                        Index = new Dictionary<I, T>();
                    }
                }

                IsIndexLoaded = true;
            }
        }

        public bool TryGet(I key, out T data)
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
                data = default;
                return false;
            }
        }

        public IEnumerable<KeyValuePair<I, T>> GetAllEntries()
        {
            if (!IsIndexLoaded)
            {
                ReadIndex();
            }

            return Index;
        }

        public abstract void IndexPackage(IPackage package, int packageIndex);
    }
}
