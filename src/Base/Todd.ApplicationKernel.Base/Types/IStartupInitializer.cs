namespace Todd.ApplicationKernel.Base.Types;
public interface IStartupInitializer : IInitializer
{
    void AddInitializer(IInitializer initializer);
}
