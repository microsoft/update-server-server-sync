// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UpdateServices.Metadata
{
    /// <summary>
    /// Deserialization converter that instantiates the correct update object based on the type encoded in the JSON
    /// </summary>
    class UpdateConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Update).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader,
            Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject item = JObject.Load(reader);
            var updateType = (UpdateType)item["UpdateType"].Value<int>();

            if (updateType == UpdateType.Product)
            {
                return item.ToObject<Product>();
            }
            else if (updateType == UpdateType.Detectoid)
            {
                return item.ToObject<Detectoid>();
            }
            else if (updateType == UpdateType.Classification)
            {
                return item.ToObject<Classification>();
            }
            else if (updateType == UpdateType.Driver)
            {
                return item.ToObject<DriverUpdate>();
            }
            else if (updateType == UpdateType.Software)
            {
                return item.ToObject<SoftwareUpdate>();
            }
            else
            {
                throw new Exception("Unexpected update type");
            }
        }

        public override void WriteJson(JsonWriter writer,
            object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
