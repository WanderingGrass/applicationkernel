// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Consul;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Todd.Applicationkernel.Core.Discovery;



namespace Todd.ApplicationKernel.Discovery
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

        public async Task Register(ServiceRegistration registration)
        {
            ArgumentNullException.ThrowIfNull(registration);
            ArgumentException.ThrowIfNullOrEmpty(registration.ServiceId);
            ArgumentException.ThrowIfNullOrEmpty(registration.ServiceName);
            ArgumentException.ThrowIfNullOrEmpty(registration.Address);

            if (!IsConsulEnabled(registration.Enabled))
            {
                _logger.LogInformation("Consul registration is disabled");
                return;
            }
            try
            {
                var check = CreateServiceCheck(registration);
                var consulRegistration = new AgentServiceRegistration
                {
                    ID = registration.ServiceId,
                    Name = registration.ServiceName,
                    Address = registration.Address,
                    Port = registration.Port,
                    Tags = registration.Tags,
                    Meta = registration.Metadata,
                    Check = check
                };

                var result = await _consulClient.Agent.ServiceRegister(consulRegistration);
                _logger.LogInformation("Service {ServiceId} registered with Consul. Result: {@Result}",
                    registration.ServiceId, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register service {ServiceId} with Consul",
                    registration.ServiceId);
                throw;
            }
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
        public async Task<IEnumerable<ServiceInstance>> GetServiceList()
        {
            try
            {
                var services = await _consulClient.Agent.Services();
                return services.Response.Values.Select(service => new ServiceInstance
                {
                    ServiceId = service.ID,
                    ServiceName = service.Service,
                    Address = service.Address,
                    Port = service.Port,
                    Tags = service.Tags,
                    Metadata = service.Meta
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all services from Consul");
                throw;
            }
        }
        public async Task<IEnumerable<ServiceInstance>> GetServiceList(string serviceName,
        bool passingOnly = true, string[] tags = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(serviceName);
            try
            {
                var queryOptions = new QueryOptions
                {
                    WaitTime = TimeSpan.FromSeconds(5)
                };

                var queryResult = await _consulClient.Health.Service(
                    serviceName,
                    tag: tags?.FirstOrDefault() ?? string.Empty,
                    passingOnly: passingOnly,
                    queryOptions);

                var services = queryResult.Response
                    .Where(serviceEntry => serviceEntry.Service != null);

                // 如果指定了多个标签，进行过滤
                if (tags != null && tags.Length > 1)
                {
                    services = services.Where(s => tags.All(tag => s.Service.Tags.Contains(tag)));
                }

                return services.Select(serviceEntry => new ServiceInstance
                {
                    ServiceId = serviceEntry.Service.ID,
                    ServiceName = serviceEntry.Service.Service,
                    Address = serviceEntry.Service.Address,
                    Port = serviceEntry.Service.Port,
                    Tags = serviceEntry.Service.Tags,
                    Metadata = serviceEntry.Service.Meta,
                    Health = new ServiceHealth
                    {
                        Status = GetServiceStatus(serviceEntry.Checks),
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get service instances for {ServiceName} from Consul", serviceName);
                throw;
            }
        }
        private static ServiceStatus GetServiceStatus(HealthCheck[] checks)
        {
            if (checks == null || !checks.Any())
                return ServiceStatus.Unknown;

            if (checks.Any(c => c.Status == HealthStatus.Critical))
                return ServiceStatus.Critical;

            if (checks.Any(c => c.Status == HealthStatus.Warning))
                return ServiceStatus.Warning;

            return ServiceStatus.Passing;
        }
        private AgentServiceCheck CreateServiceCheck(ServiceRegistration registration)
        {
            if (registration.Check == null)
            {
                return new AgentServiceCheck
                {
                    TCP = $"{registration.Address}:{registration.Port}",
                    Interval = ParseTime("10s"),
                    DeregisterCriticalServiceAfter = ParseTime("30s")
                };
            }

            var check = new AgentServiceCheck
            {
                Interval = ParseTime(registration.Check.Interval ?? "10s"),
                DeregisterCriticalServiceAfter = ParseTime(registration.Check.DeregisterAfter ?? "30s")
            };

            switch (registration.Check.Type?.ToUpperInvariant())
            {
                case "TCP":
                    check.TCP = $"{registration.Address}:{registration.Port}";
                    break;
                case "HTTP":
                    var pingEndpoint = BuildPingEndpoint(registration.Check.Endpoint);
                    var scheme = registration.Address.StartsWith("http", StringComparison.InvariantCultureIgnoreCase)
                        ? string.Empty
                        : "http://";
                    check.HTTP = $"{scheme}{registration.Address}{(registration.Port > 0 ? $":{registration.Port}" : string.Empty)}{pingEndpoint}";
                    break;
                case "GRPC":
                    check.GRPC = $"{registration.Address}:{registration.Port}/{registration.Check.Endpoint}";
                    check.GRPCUseTLS = registration.Address.StartsWith("https", StringComparison.InvariantCultureIgnoreCase);
                    break;
                default:
                    break;
            }

            return check;
        }

        private static TimeSpan ParseTime(string value)
            => string.IsNullOrWhiteSpace(value)
                ? TimeSpan.FromSeconds(5)
                : int.TryParse(value, out var number)
                    ? TimeSpan.FromSeconds(number)
                    : TimeSpan.Parse(value);

        private static bool IsConsulEnabled(bool defaultEnabled)
        {
            var consulEnabled = Environment.GetEnvironmentVariable("CONSUL_ENABLED")?.ToLowerInvariant();
            return !string.IsNullOrWhiteSpace(consulEnabled)
                ? consulEnabled is "true" or "1"
                : defaultEnabled;
        }

        private static string BuildPingEndpoint(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return string.Empty;
            }

            var pingEndpoint = endpoint.StartsWith("/") ? endpoint : $"/{endpoint}";
            return pingEndpoint.TrimEnd('/');
        }
    }
}
