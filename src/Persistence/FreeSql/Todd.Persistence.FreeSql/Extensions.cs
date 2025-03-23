using FreeSql;
using Microsoft.Extensions.DependencyInjection;
using Todd.ApplicationKernel.Base;

namespace Todd.ApplicationKernel.Persistence.FreeSql
{
    public static class Extensions
    {
        private const string SectionName = "freesql";
        private const string RegistryName = "persistence.freesql";
        public static IApplicationKernelBuilder AddFreeSql(this IApplicationKernelBuilder builder, string sectionName = SectionName)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
            {
                sectionName = SectionName;
            }

            if (!builder.TryRegister(RegistryName))
            {
                return builder;
            }
            //多数据库初始化
            var fsql = new FreeSqlCloud();
            var mysqlConnection = builder.GetOptions<FreeSqlOption>(SectionName);
            if (mysqlConnection is null)
            {
                throw new ArgumentException("FreeSql ConnectionString cannot be empty.", nameof(SectionName));
            }
            foreach (var item in mysqlConnection.ConnectionString)
            {
                fsql.Register(item.Type, () => new FreeSqlBuilder()
                    .UseConnectionString(DataType.MySql, item.Value)
                    .UseAutoSyncStructure(false)
                    .Build());
            }
            builder.Services.AddSingleton<IFreeSql>(fsql);
            return builder;
        }
    }
}
