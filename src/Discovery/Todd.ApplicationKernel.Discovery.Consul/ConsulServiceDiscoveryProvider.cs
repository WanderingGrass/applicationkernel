// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Todd.Applicationkernel.Core.Abstractions;
using Todd.Applicationkernel.Core.Discovery;
using Todd.ApplicationKernel.Discovery.Consul.Options;



namespace Todd.ApplicationKernel.Discovery.Consul
{
    public class ConsulServiceDiscoveryProvider : IServiceDiscoveryProvider
    {
        private IConsulClient _consulClient;
        private readonly ILogger<ConsulServiceDiscoveryProvider> _logger;
        private readonly ConsulClusteringOptions _options;

        public ConsulServiceDiscoveryProvider(
            ILogger<ConsulServiceDiscoveryProvider> logger,
            IOptions<ConsulClusteringOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public Task Initialize()
        {
            _consulClient = _options.CreateClient() ?? throw new InvalidOperationException("Failed to create Consul client");
            return Task.CompletedTask;
        }

        public async Task Register(MembershipEntry entry, TableVersion tableVersion)
        {
            var serviceRegistration = ConsulRegistrationAssembler.FromMembershipEntry("1", entry, "0");
            await _consulClient.Agent.ServiceRegister(serviceRegistration);
        }

        public async Task Deregister(string serviceId)
        {
            ArgumentException.ThrowIfNullOrEmpty(serviceId);
            try
            {
                await _consulClient.Agent.ServiceDeregister(serviceId);
                _logger.LogInformation("Service {ServiceId} deregistered from Consul", serviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deregister service {ServiceId} from Consul", serviceId);
                throw;
            }
        }
        public async Task<MembershipTableData> ReadRow(ApplicationKernelAddress key)
        {
            throw new NotImplementedException();
        }
        public async Task<MembershipTableData> ReadAll()
        {
            throw new NotImplementedException();
        }
    }
}
