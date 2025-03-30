// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Todd.ApplicationKernel.Discovery.ZooKeeper
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
        }
    }
}
