using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using org.apache.zookeeper;
using Todd.Applicationkernel.Core.Abstractions.Discovery;

namespace Todd.ApplicationKernel.Discovery
{
    public class ZookeeperServiceDiscoveryProvider : IServiceDiscoveryProvider
    {
        private readonly ILogger logger;

        private const int ZOOKEEPER_CONNECTION_TIMEOUT = 2000;

        private readonly ZooKeeperWatcher watcher;

        /// <summary>
        /// The deployment connection string. for eg. "192.168.1.1,192.168.1.2/ClusterId"
        /// </summary>
        private readonly string deploymentConnectionString;

        /// <summary>
        /// the node name for this deployment. for eg. /ClusterId
        /// </summary>
        private readonly string clusterPath;

        /// <summary>
        /// The root connection string. for eg. "192.168.1.1,192.168.1.2"
        /// </summary>
        private readonly string rootConnectionString;
        public ZookeeperServiceDiscoveryProvider(
           ILogger<ZookeeperServiceDiscoveryProvider> logger,
           IOptions<ZooKeeperClusteringOptions> clusteringOptions)
        {
            this.logger = logger;
            var options = clusteringOptions.Value;
            watcher = new ZooKeeperWatcher(logger);
            this.clusterPath = "/" + options.ClusterId;
            rootConnectionString = options.ConnectionString;
            deploymentConnectionString = options.ConnectionString + this.clusterPath;
        }
        public async Task Initialize()
        {
            await UsingZookeeper(rootConnectionString, async zk =>
            {
                try
                {
                    await zk.createAsync(this.clusterPath, null, ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
                    await zk.sync(this.clusterPath);
                    //if we got here we know that we've just created the deployment path with version=0
                    this.logger.LogInformation("Created new deployment path: {DeploymentPath}", this.clusterPath);
                }
                catch (KeeperException.NodeExistsException)
                {
                    this.logger.LogDebug("Deployment path already exists: {DeploymentPath}", this.clusterPath);
                }
            });

        }

        public async Task Register(ServiceRegistration registration)
        {
            ArgumentNullException.ThrowIfNull(registration);
            ArgumentException.ThrowIfNullOrEmpty(registration.ServiceId);
            ArgumentException.ThrowIfNullOrEmpty(registration.ServiceName);
            ArgumentException.ThrowIfNullOrEmpty(registration.Address);

            try
            {
                var servicePath = $"{clusterPath}/{registration.ServiceName}";
                var instancePath = $"{servicePath}/{registration.ServiceId}";
                var aliveStatusPath = $"{instancePath}/status";

                // 准备完整的服务实例信息
                var serviceInstance = new ServiceInstance
                {
                    ServiceId = registration.ServiceId,
                    ServiceName = registration.ServiceName,
                    Address = registration.Address,
                    Port = registration.Port,
                    Tags = registration.Tags,
                    Metadata = registration.Metadata,
                    Health = new ServiceHealth 
                    { 
                        Status = ServiceStatus.Passing,
                        LastCheckTime = DateTime.UtcNow
                    }
                };

                // 准备存活状态信息
                var aliveStatus = new ServiceAliveStatus
                {
                    LastUpdateTime = DateTime.UtcNow,
                    Status = ServiceStatus.Passing
                };

                byte[] instanceData = Serialize(serviceInstance);
                byte[] aliveStatusData = Serialize(aliveStatus);

                // 使用事务确保原子性
                var success = await TryTransaction(t => t
                    // 创建服务目录（如果不存在）
                    .create(servicePath, null, ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT)
                    // 创建服务实例节点（持久节点）
                    .create(instancePath, instanceData, ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT)
                    // 创建存活状态节点（临时节点，会话断开自动删除）
                    .create(aliveStatusPath, aliveStatusData, ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.EPHEMERAL));

                if (!success)
                {
                    // 如果事务失败，尝试更新现有节点
                    await UsingZookeeper(async zk =>
                    {
                        // 确保服务目录存在
                        if (await zk.existsAsync(servicePath) == null)
                        {
                            await zk.createAsync(servicePath, null, ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
                        }

                        // 更新或创建服务实例节点
                        if (await zk.existsAsync(instancePath) != null)
                        {
                            await zk.setDataAsync(instancePath, instanceData);
                        }
                        else
                        {
                            await zk.createAsync(instancePath, instanceData, ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
                        }

                        // 更新或创建存活状态节点
                        if (await zk.existsAsync(aliveStatusPath) != null)
                        {
                            await zk.setDataAsync(aliveStatusPath, aliveStatusData);
                        }
                        else
                        {
                            await zk.createAsync(aliveStatusPath, aliveStatusData, ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.EPHEMERAL);
                        }
                    }, rootConnectionString);
                }

                logger.LogInformation("Service {ServiceId} registered with ZooKeeper", registration.ServiceId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to register service {ServiceId}", registration.ServiceId);
                throw;
            }
        }

        private class ServiceAliveStatus
        {
            public DateTime LastUpdateTime { get; set; }
            public ServiceStatus Status { get; set; }
        }

        string rowPath = ConvertToRowPath(entry.SiloAddress);
        string rowIAmAlivePath = ConvertToRowIAmAlivePath(entry.SiloAddress);
        byte[] newRowData = Serialize(entry);
        byte[] newRowIAmAliveData = Serialize(entry.IAmAliveTime);

        int expectedTableVersion = int.Parse(tableVersion.VersionEtag, CultureInfo.InvariantCulture);

        return TryTransaction(t => t
            .setData("/", null, expectedTableVersion)//increments the version of node "/"
            .create(rowPath, newRowData, ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT)
            .create(rowIAmAlivePath, newRowIAmAliveData, ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT));
        }

        public async Task Deregister(string serviceId)
        {
            ArgumentException.ThrowIfNullOrEmpty(serviceId);

            try
            {
                await UsingZookeeper(async zk =>
                {
                    var children = await zk.getChildrenAsync(clusterPath);
                    foreach (var serviceName in children)
                    {
                        var servicePath = $"{clusterPath}/{serviceName}";
                        var instancePath = $"{servicePath}/{serviceId}";

                        if (await zk.existsAsync(instancePath) != null)
                        {
                            await zk.deleteAsync(instancePath);
                            logger.LogInformation("Service {ServiceId} deregistered from ZooKeeper", serviceId);
                            return;
                        }
                    }
                }, rootConnectionString);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to deregister service {ServiceId}", serviceId);
                throw;
            }
        }

        public async Task<IEnumerable<ServiceInstance>> GetServiceList()
        {
            var result = new List<ServiceInstance>();
            try
            {
                await UsingZookeeper(async zk =>
                {
                    var services = await zk.getChildrenAsync(clusterPath);
                    foreach (var serviceName in services)
                    {
                        var instances = await GetServiceInstances(zk, serviceName);
                        result.AddRange(instances);
                    }
                }, rootConnectionString);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get service list");
                throw;
            }
            return result;
        }

        public async Task<IEnumerable<ServiceInstance>> GetServiceList(string serviceName, bool passingOnly = true, string[] tags = null)
        {
            ArgumentException.ThrowIfNullOrEmpty(serviceName);

            try
            {
                var servicePath = $"{clusterPath}/{serviceName}";
                var instances = new List<ServiceInstance>();

                await UsingZookeeper(async zk =>
                {
                    if (await zk.existsAsync(servicePath) != null)
                    {
                        instances.AddRange(await GetServiceInstances(zk, serviceName));
                    }
                }, rootConnectionString);

                // 标签过滤
                if (tags?.Length > 0)
                {
                    instances = instances.Where(i => i.Tags?.Any(t => tags.Contains(t)) ?? false).ToList();
                }

                return instances;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get service instances for {ServiceName}", serviceName);
                throw;
            }
        }

        private async Task<IEnumerable<ServiceInstance>> GetServiceInstances(ZooKeeper zk, string serviceName)
        {
            var servicePath = $"{clusterPath}/{serviceName}";
            var instances = new List<ServiceInstance>();

            var children = await zk.getChildrenAsync(servicePath);
            foreach (var instanceId in children)
            {
                var instancePath = $"{servicePath}/{instanceId}";
                var data = await zk.getDataAsync(instancePath);
                if (data.Data?.Length > 0)
                {
                    var instance = Deserialize<ServiceInstance>(data.Data);
                    instances.Add(instance);
                }
            }

            return instances;
        }
        private async Task<bool> TryTransaction(Func<Transaction, Transaction> transactionFunc)
        {
            try
            {
                await UsingZookeeper(zk => transactionFunc(zk.transaction()).commitAsync(), this.deploymentConnectionString, this.watcher);
                return true;
            }
            catch (KeeperException e)
            {
                //these exceptions are thrown when the transaction fails to commit due to semantical reasons
                if (e is KeeperException.NodeExistsException || e is KeeperException.NoNodeException ||
                    e is KeeperException.BadVersionException)
                {
                    return false;
                }
                throw;
            }
        }
        private static Task<T> UsingZookeeper<T>(Func<ZooKeeper, Task<T>> zkMethod, string deploymentConnectionString, ZooKeeperWatcher watcher, bool canBeReadOnly = false)
        {
            return ZooKeeper.Using(deploymentConnectionString, ZOOKEEPER_CONNECTION_TIMEOUT, watcher, zkMethod, canBeReadOnly);
        }

        private Task UsingZookeeper(string connectString, Func<ZooKeeper, Task> zkMethod)
        {
            return ZooKeeper.Using(connectString, ZOOKEEPER_CONNECTION_TIMEOUT, watcher, zkMethod);
        }
        private static byte[] Serialize(object obj)
        {
            return
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj, Formatting.None,
                    DiscoverySerializerSettings.Instance));
        }

    }
    internal class ZooKeeperWatcher : Watcher
    {
        private readonly ILogger logger;
        public ZooKeeperWatcher(ILogger logger)
        {
            this.logger = logger;
        }

        public override Task process(WatchedEvent @event)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(@event.ToString());
            }
            return Task.CompletedTask;
        }
    }
}
