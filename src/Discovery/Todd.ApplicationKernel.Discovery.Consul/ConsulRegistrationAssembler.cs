// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Consul;
using Newtonsoft.Json;
using Todd.Applicationkernel.Core.Abstractions;
using Todd.Applicationkernel.Core.Discovery;

namespace Todd.ApplicationKernel.Discovery.Consul
{
    [JsonObject]
    public class ConsulRegistration
    {
        /// <summary>
        /// Persisted as part of the KV Key therefore not serialised.
        /// </summary>
        [JsonIgnore]
        internal string DeploymentId { get; set; }

        /// <summary>
        /// Persisted as part of the KV Key therefore not serialised.
        /// </summary>
        [JsonIgnore]
        internal ApplicationKernelAddress Address { get; set; }

        /// <summary>
        /// Persisted in a separate KV Subkey, therefore not serialised but held here to enable cleaner assembly to MembershipEntry.
        /// </summary>
        /// <remarks>
        /// Stored in a separate KV otherwise the regular updates to IAmAlive cause the 's KV.ModifyIndex to change 
        /// which in turn cause UpdateRow operations to fail.
        /// </remarks>
        [JsonIgnore]
        internal DateTime IAmAliveTime { get; set; }

        /// <summary>
        /// Used to compare CAS value on update, persisted as KV.ModifyIndex therefore not serialised.
        /// </summary>
        [JsonIgnore]
        internal ulong LastIndex { get; set; }

        //Public properties are serialized to the KV.Value
        [JsonProperty]
        public string Hostname { get; set; }

        [JsonProperty]
        public int ProxyPort { get; set; }

        [JsonProperty]
        public DateTime StartTime { get; set; }

        [JsonProperty]
        public ApplicationKernelStatus Status { get; set; }

        [JsonProperty]
        public string Name { get; set; }
    }
    internal class ConsulRegistrationAssembler
    {
        internal static AgentServiceRegistration FromMembershipEntry(string deploymentId, MembershipEntry entry, string etag)
        {
            var ret = new AgentServiceRegistration
            {
                ID = deploymentId,
                Name = entry.ApplicationKernelName,
                Address = entry.ApplicationKernelAddress.Endpoint.Address.ToString(),
                Port = entry.ApplicationKernelAddress.Endpoint.Port,
                Tags = new[] {
                    $"Generation={entry.ApplicationKernelAddress.Generation}",
                    $"Status={entry.Status}",
                    $"ProxyPort={entry.ProxyPort}"
                },
                Meta = new Dictionary<string, string>
                {
                    ["Name"] = entry.ApplicationKernelName,
                    ["HostName"] = entry.HostName
                }
            };
            return ret;
        }
    }
}
