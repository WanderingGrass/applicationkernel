using IdGen;
using IdGen.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Todd.ApplicationKernel.Base;

namespace Todd.Applicationkernel.Core.IdGen
{
    public static class Extensions
    {
        private const string SectionName = "idGen";
    
        public static IApplicationKernelBuilder AddIdGen(this IApplicationKernelBuilder builder, string sectionName = SectionName, Func<IdGeneratorOptions> idGeneratorOpstions=null)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
            {
                sectionName = SectionName;
            }
            var options = builder.GetOptions<IdGenOptions>(sectionName);
            return builder.AddIdGen(options, idGeneratorOpstions);
        }
        public static IApplicationKernelBuilder AddIdGen(this IApplicationKernelBuilder builder, IdGenOptions options,Func<IdGeneratorOptions> idGeneratorOsptions)
        {
            builder.Services.AddSingleton(options);
            if (idGeneratorOsptions is not null)
            {
                builder.Services.AddIdGen(options.GeneratorId, idGeneratorOsptions);
                return builder;
            }
            builder.Services.AddIdGen(options.GeneratorId);
            return builder;
        }
    }
}
