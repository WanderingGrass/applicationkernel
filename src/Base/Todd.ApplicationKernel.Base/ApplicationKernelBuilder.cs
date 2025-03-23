using Todd.ApplicationKernel.Base.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Todd.ApplicationKernel.Base;
public sealed class ApplicationKernelBuilder : IApplicationKernelBuilder
{
    private readonly ConcurrentDictionary<string, bool> _registry = new();
    private readonly List<Action<IServiceProvider>> _buildActions;
    private readonly IServiceCollection _services;
    IServiceCollection IApplicationKernelBuilder.Services => _services;

    public IConfiguration Configuration { get; }

    private ApplicationKernelBuilder(IServiceCollection services, IConfiguration configuration)
    {
        _buildActions = new List<Action<IServiceProvider>>();
        _services = services;
        _services.AddSingleton<IStartupInitializer>(new StartupInitializer());
        Configuration = configuration;
    }

    public static IApplicationKernelBuilder Create(IServiceCollection services, IConfiguration? configuration = null)
        => new ApplicationKernelBuilder(services, configuration);

    public bool TryRegister(string name) => _registry.TryAdd(name, true);

    public void AddBuildAction(Action<IServiceProvider> execute)
        => _buildActions.Add(execute);

    public void AddInitializer(IInitializer initializer)
        => AddBuildAction(sp =>
        {
            var startupInitializer = sp.GetRequiredService<IStartupInitializer>();
            startupInitializer.AddInitializer(initializer);
        });

    public void AddInitializer<TInitializer>() where TInitializer : IInitializer
        => AddBuildAction(sp =>
        {
            var initializer = sp.GetRequiredService<TInitializer>();
            var startupInitializer = sp.GetRequiredService<IStartupInitializer>();
            startupInitializer.AddInitializer(initializer);
        });

    public IServiceProvider Build()
    {
        var serviceProvider = _services.BuildServiceProvider();
        _buildActions.ForEach(a =>
        {
            ValidateSystemConfiguration(serviceProvider);
            a(serviceProvider);
        });

        static void ValidateSystemConfiguration(IServiceProvider serviceProvider)
        {
            var validators = serviceProvider.GetServices<IConfigurationValidator>();
            foreach (var validator in validators)
            {
                validator.ValidateConfiguration();
            }
        }
        return serviceProvider;
    }
}
