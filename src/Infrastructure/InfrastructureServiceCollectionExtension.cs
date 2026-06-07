using System;
using AutoRegisterAnnotation;
using KozLibraries.DapperDateOnlySupport;
using Microsoft.Extensions.DependencyInjection;

namespace Movie.Infrastructure;

public static class InfrastructureServiceCollectionExtension
{
    static InfrastructureServiceCollectionExtension()
    {
        // register DateOnly Type Handler
        Dapper.SqlMapper.AddTypeHandler(new DateOnlyHandler());
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        Action<ServiceTypePair>? onRegistered = null
    )
    {
        services.AddAutoRegisterServices(typeof(Infrastructure), onRegistered);
        return services;
    }
}
