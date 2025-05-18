using System;
using System.Net;

namespace Todd.ApplicationKernel.Base.Types;
public class AppOptions
{
    private IPAddress advertisedIPAddress;
    private int appPort = DEFAULT_APP_PORT;
    /// <summary>
    /// The IP address used for clustering.
    /// </summary>
    public IPAddress AdvertisedIPAddress
    {
        get => advertisedIPAddress;
        set
        {
            if (value is null)
            {
                throw new Exception(
                    $"No listening address specified. "
                    + $"to configure endpoints and ensure that {nameof(AdvertisedIPAddress)} is set.");
            }

            if (value == IPAddress.Any
                || value == IPAddress.IPv6Any
                || value == IPAddress.None
                || value == IPAddress.IPv6None)
            {
                throw new Exception(
                    $"Invalid value specified for {nameof(AdvertisedIPAddress)}. The value was {value}");
            }

            advertisedIPAddress = value;
        }
    }

    /// <summary>
    /// Gets or sets the port this app uses for app-to-app communication.
    /// 这里主要是处理内部的通信端口,专门处理内部的服务之间的通信
    /// </summary>
    public int AppPort
    {
        get => appPort;
        set
        {
            if (value == 0)
            {
                throw new Exception(
                    $"No listening port specified. "
                    + $"to configure endpoints and ensure that {nameof(AppPort)} is set.");
            }

            appPort = value;
        }
    }
    public const int DEFAULT_APP_PORT = 11111;
    /// <summary>
    /// Gets or sets the port this app uses for app-to-client (gateway) communication. Specify 0 to disable gateway functionality.
    /// 这里主要是处理对外的访问端口,专门处理客户端的连接
    /// </summary>
    public int GatewayPort { get; set; } = DEFAULT_GATEWAY_PORT;

    /// <summary>
    /// The default value for <see cref="GatewayPort"/>.
    /// </summary>
    public const int DEFAULT_GATEWAY_PORT = 30000;

    /// <summary>
    /// Gets or sets the endpoint used to listen for app to app communication.
    /// If not set will default to <see cref="AdvertisedIPAddress"/> + <see cref="AppPort"/>
    /// </summary>
    public IPEndPoint AppListeningEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the endpoint used to listen for client to app communication.
    /// If not set will default to <see cref="AdvertisedIPAddress"/> + <see cref="GatewayPort"/>
    /// </summary>
    public IPEndPoint GatewayListeningEndpoint { get; set; }

}
