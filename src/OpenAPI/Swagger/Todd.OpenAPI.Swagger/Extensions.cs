using Todd.ApplicationKernel.OpenAPI.Swagger.Builders;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Todd.ApplicationKernel.Base;

namespace Todd.ApplicationKernel.OpenAPI.Swagger;

public static class Extensions
{
    private const string SectionName = "swagger";
    private const string RegistryName = "docs.swagger";

    public static IApplicationKernelBuilder AddSwaggerDocs(this IApplicationKernelBuilder builder, string sectionName = SectionName)
    {
        if (string.IsNullOrWhiteSpace(sectionName))
        {
            sectionName = SectionName;
        }
        var options = builder.GetOptions<SwaggerOptions>(sectionName);
        return builder.AddSwaggerDocs(options);
    }

    public static IApplicationKernelBuilder AddSwaggerDocs(this IApplicationKernelBuilder builder,
        Func<ISwaggerOptionsBuilder, ISwaggerOptionsBuilder> buildOptions)
    {
        var options = buildOptions(new SwaggerOptionsBuilder()).Build();
        return builder.AddSwaggerDocs(options);
    }

    public static IApplicationKernelBuilder AddSwaggerDocs(this IApplicationKernelBuilder builder, SwaggerOptions options)
    {
        builder.Services.AddSingleton(options);
        if (!options.Enabled || !builder.TryRegister(RegistryName))
        {
            return builder;
        }
        builder.Services.AddSwaggerGen(c =>
        {
            c.EnableAnnotations();
            c.SwaggerDoc(options.Name, new OpenApiInfo { Title = options.Title, Version = options.Version });
            if (options.IncludeSecurity)
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            In = ParameterLocation.Header
                        },
                        new string[] {}
                    }
                });
            }
        });

        return builder;
    }

    public static IApplicationBuilder UseSwaggerDocs(this IApplicationBuilder builder)
    {
        var options = builder.ApplicationServices.GetRequiredService<SwaggerOptions>();
        if (!options.Enabled)
        {
            return builder;
        }

        var routePrefix = string.IsNullOrWhiteSpace(options.RoutePrefix) ? string.Empty : options.RoutePrefix;

        builder.UseStaticFiles()
            .UseSwagger(c =>
            {
                c.RouteTemplate = string.Concat(routePrefix, "/{documentName}/swagger.json");
                c.SerializeAsV2 = options.SerializeAsOpenApiV2;
            });

        return options.ReDocEnabled
            ? builder.UseReDoc(c =>
            {
                c.RoutePrefix = routePrefix;
                c.SpecUrl = $"{options.Name}/swagger.json";
            })
            : builder.UseSwaggerUI(c =>
            {
                c.RoutePrefix = routePrefix;
                c.SwaggerEndpoint($"/{routePrefix}/{options.Name}/swagger.json".FormatEmptyRoutePrefix(),
                    options.Title);
            });
    }

    /// <summary>
    /// Replaces leading double forward slash caused by an empty route prefix
    /// </summary>
    /// <param name="route"></param>
    /// <returns></returns>
    private static string FormatEmptyRoutePrefix(this string route)
    {
        return route.Replace("//", "/");
    }
}
