namespace Todd.ApplicationKernel.Discovery.ZooKeeper.Options
{
    public class ZooKeeperClusteringOptions
    {
        public const string DefaultClusterId = "default";
        public string ClusterId { get; set; } = DefaultClusterId;
        public string ConnectionString { get; set; }

        public TimeSpan RefreshPeriod { get; set; } = TimeSpan.FromMinutes(1);
    }
}
