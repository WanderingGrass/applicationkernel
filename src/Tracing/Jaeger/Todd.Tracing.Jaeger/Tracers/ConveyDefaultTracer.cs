using System.Reflection;
using Jaeger;
using Jaeger.Reporters;
using Jaeger.Samplers;
using OpenTracing;

namespace Todd.ApplicationKernel.Tracing.Jaeger.Tracers;

internal sealed class ConveyDefaultTracer
{
    public static ITracer Create()
        => new Tracer.Builder(Assembly.GetEntryAssembly().FullName)
            .WithReporter(new NoopReporter())
            .WithSampler(new ConstSampler(false))
            .Build();
}