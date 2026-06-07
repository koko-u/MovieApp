using System;
using System.Globalization;
using AutoRegisterAnnotation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Movie.Application;

public static class ApplicationServiceCollectionExtension
{
    static ApplicationServiceCollectionExtension()
    {
        // configure FluentValidation to use invariant culture
        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.InvariantCulture;
    }

    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        Action<ServiceTypePair>? onRegistered = null
    )
    {
        services.AddValidatorsFromAssemblyContaining(typeof(Application));
        services.AddAutoRegisterServices(typeof(Application), onRegistered);
        return services;
    }
}
