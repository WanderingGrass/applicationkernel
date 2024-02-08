namespace Todd.ApplicationKernel.Base;
internal class ServiceId : IServiceId
{
    public string Id { get; } = $"{Guid.NewGuid():N}";
}