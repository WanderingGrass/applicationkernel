// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Todd.ApplicationKernel.Base
{
    public static class ApplicationKernelBuilderExtensions
    {

        /// <summary>
        /// Adds services to the container. This can be called multiple times and the results will be additive.
        /// </summary>
        /// <param name="builder">The <see cref="IBuilder" /> to configure.</param>
        /// <param name="configureDelegate"></param>
        /// <returns>The same instance of the <see cref="IBuilder"/> for chaining.</returns>
        public static IApplicationKernelBuilder ConfigureServices(this IApplicationKernelBuilder builder, Action<IServiceCollection> configureDelegate)
        {
            ArgumentNullException.ThrowIfNull(configureDelegate);
            configureDelegate(builder.Services);
            return builder;
        }

        /// <summary>
        /// Registers an action used to configure a particular type of options.
        /// </summary>
        /// <typeparam name="TOptions">The options type to be configured.</typeparam>
        /// <param name="builder">The  builder.</param>
        /// <param name="configureOptions">The action used to configure the options.</param>
        /// <returns>The  builder.</returns>
        public static IApplicationKernelBuilder Configure<TOptions>(this IApplicationKernelBuilder builder, Action<TOptions> configureOptions) where TOptions : class
        {
            return builder.ConfigureServices(services => services.Configure(configureOptions));
        }

        /// <summary>
        /// Registers a configuration instance which <typeparamref name="TOptions"/> will bind against.
        /// </summary>
        /// <typeparam name="TOptions">The options type to be configured.</typeparam>
        /// <param name="builder">The  builder.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The  builder.</returns>
        public static IApplicationKernelBuilder Configure<TOptions>(this IApplicationKernelBuilder builder, IConfiguration configuration) where TOptions : class
        {
            return builder.ConfigureServices(services => services.AddOptions<TOptions>().Bind(configuration));
        }
        /// <summary>
        /// Adds a delegate for configuring the provided <see cref="ILoggingBuilder"/>. This may be called multiple times.
        /// </summary>
        /// <param name="builder">The <see cref="IBuilder" /> to configure.</param>
        /// <param name="configureLogging">The delegate that configures the <see cref="ILoggingBuilder"/>.</param>
        /// <returns>The same instance of the <see cref="IBuilder"/> for chaining.</returns>
        public static IApplicationKernelBuilder ConfigureLogging(this IApplicationKernelBuilder builder, Action<ILoggingBuilder> configureLogging)
        {
            return builder.ConfigureServices(collection => collection.AddLogging(configureLogging));
        }

    }
}
