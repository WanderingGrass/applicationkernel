using Todd.ApplicationKernel.Base.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Todd.ApplicationKernel.Base;

public interface IApplicationKernelBuilder
{
    IServiceCollection Services { get; }
    IConfiguration Configuration { get; }
    bool TryRegister(string name);
    void AddBuildAction(Action<IServiceProvider> execute);
    void AddInitializer(IInitializer initializer);
    void AddInitializer<TInitializer>() where TInitializer : IInitializer;
    IServiceProvider Build();
   
}

