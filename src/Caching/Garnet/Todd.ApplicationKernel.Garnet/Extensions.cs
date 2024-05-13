using Todd.ApplicationKernel.Base;

namespace Todd.ApplicationKernel.Garnet;

public static class Extensions
{
    public static IApplicationKernelBuilder AddGarnet(this IApplicationKernelBuilder builder, string sectionName)
    {
        return builder;
    }
}
