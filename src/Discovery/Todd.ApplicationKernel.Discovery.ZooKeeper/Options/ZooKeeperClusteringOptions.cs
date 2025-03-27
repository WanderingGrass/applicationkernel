using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Todd.ApplicationKernel.Discovery
{
    public class ZooKeeperClusteringOptions
    {
        public const string DefaultClusterId = "default";
        public string ClusterId { get; set; } = DefaultClusterId;
        public string ConnectionString { get; set; }

        public TimeSpan RefreshPeriod { get; set; } = TimeSpan.FromMinutes(1);
    }
}
