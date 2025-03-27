// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Todd.Applicationkernel.Core.Abstractions.Discovery;

namespace Todd.ApplicationKernel.Discovery
{
    internal class DiscoverySerializerSettings : JsonSerializerSettings
    {
        public static readonly DiscoverySerializerSettings Instance = new DiscoverySerializerSettings();

        private DiscoverySerializerSettings()
        {
            // 添加基本设置
            DateFormatHandling = DateFormatHandling.IsoDateFormat;
            NullValueHandling = NullValueHandling.Ignore;
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            
            // 添加转换器
            Converters.Add(new StringEnumConverter());
            Converters.Add(new ServiceInstanceConverter());
        }

        private class ServiceInstanceConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(ServiceInstance);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var jo = JObject.Load(reader);
                return new ServiceInstance
                {
                    ServiceId = jo["ServiceId"]?.Value<string>(),
                    ServiceName = jo["ServiceName"]?.Value<string>(),
                    Address = jo["Address"]?.Value<string>(),
                    Port = jo["Port"]?.Value<int>() ?? 0,
                    Tags = jo["Tags"]?.ToObject<string[]>(),
                    Metadata = jo["Metadata"]?.ToObject<Dictionary<string, string>>(),
                    Health = jo["Health"]?.ToObject<ServiceHealth>()
                };
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var instance = (ServiceInstance)value;
                writer.WriteStartObject();
                
                writer.WritePropertyName("ServiceId");
                writer.WriteValue(instance.ServiceId);
                
                writer.WritePropertyName("ServiceName");
                writer.WriteValue(instance.ServiceName);
                
                writer.WritePropertyName("Address");
                writer.WriteValue(instance.Address);
                
                writer.WritePropertyName("Port");
                writer.WriteValue(instance.Port);
                
                if (instance.Tags?.Length > 0)
                {
                    writer.WritePropertyName("Tags");
                    serializer.Serialize(writer, instance.Tags);
                }
                
                if (instance.Metadata?.Count > 0)
                {
                    writer.WritePropertyName("Metadata");
                    serializer.Serialize(writer, instance.Metadata);
                }
                
                if (instance.Health != null)
                {
                    writer.WritePropertyName("Health");
                    serializer.Serialize(writer, instance.Health);
                }
                
                writer.WriteEndObject();
            }
        }
    }
}
