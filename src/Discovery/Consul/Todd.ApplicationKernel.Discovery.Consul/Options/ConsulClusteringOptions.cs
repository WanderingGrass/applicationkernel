using Consul;
using Todd.Applicationkernel.Core.Abstractions;

namespace Todd.ApplicationKernel.Discovery.Consul.Options
{
    public class ConsulClusteringOptions
    {
        /// <summary>
        /// Consul KV root folder name.
        /// </summary>
        public string KvRootFolder { get; set; }

        /// <summary>
        /// Factory for the used Consul-Client.
        /// </summary>
        public Func<IConsulClient> CreateClient { get; private set; }

        /// <summary>
        /// Configures the <see cref="CreateClient"/> using the provided callback.
        /// </summary>
        public void ConfigureConsulClient(Func<IConsulClient> createClientCallback)
        {
            CreateClient = createClientCallback ?? throw new ArgumentNullException(nameof(createClientCallback));
        }

        /// <summary>
        /// Configures the <see cref="CreateClient"/> using the consul-address and a acl-token.
        /// </summary>
        public void ConfigureConsulClient(Uri address, string aclClientToken = null)
        {
            if (address is null) throw new ArgumentNullException(nameof(address));

            CreateClient = () => new ConsulClient(config =>
            {
                config.Address = address;
                config.Token = aclClientToken;
            });
        }

        public ConsulClusteringOptions()
        {
            this.CreateClient = () => new ConsulClient();
        }

        internal void Validate(string name)
        {
            if (CreateClient is null)
            {
                throw new ApplicationKernelConfigurationException($"No callback specified. Use the {GetType().Name}.{nameof(ConsulClusteringOptions.ConfigureConsulClient)} method to configure the consul client.");
            }
        }
    }
}
