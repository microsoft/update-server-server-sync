// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace Microsoft.PackageGraph.Storage.Index
{
    class IndexSerialization
    {
        public static T DeserializeIndexFromStream<T>(Stream inputStream)
        {
            JsonSerializer serializer = new();

            using var sr = new StreamReader(inputStream);
            using var reader = new JsonTextReader(sr);
            return serializer.Deserialize<T>(reader);
        }

        public static void SerializeIndexToStream<T>(Stream destinationStream, T index)
        {
            using var sw = new StreamWriter(destinationStream, Encoding.UTF8, 4 * 1024, true);
            using var writer = new JsonTextWriter(sw);
            JsonSerializer serializer = new();
            serializer.Serialize(writer, index);
        }
    }
}
