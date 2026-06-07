using System;
using System.Threading.Tasks;
using AutoRegisterAnnotation;
using MicroElements.AspNetCore.OpenApi.FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Movie.Application;
using Movie.Infrastructure;
using Scalar.AspNetCore;
using Serilog;

// Bootstrap Logger
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    // Configure Serilog
    builder.Host.UseSerilog(
        (context, provider, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(provider)
                .Enrich.FromLogContext();
        }
    );
    // Configure DI Validation
    builder.Host.UseDefaultServiceProvider(opts =>
    {
        opts.ValidateScopes = true;
        opts.ValidateOnBuild = true;
    });

    var onRegistered = (ServiceTypePair pair) =>
    {
        var (serviceType, implementationType, lifetime) = pair;
        Log.Debug(
            "Registering service: {serviceType} with implementation {implementationType} and lifetime {lifetime}",
            serviceType.Name,
            implementationType.Name,
            lifetime
        );
    };

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddProblemDetails();
    builder.Services.AddApplicationServices(onRegistered);
    builder.Services.AddInfrastructure(onRegistered);

    // OpenAPI configuration
    builder.Services.AddFluentValidationRulesToOpenApi();
    builder.Services.AddOpenApi(opts =>
    {
        opts.AddOperationTransformer(
            (operation, _, _) =>
            {
                operation.Summary = null;
                operation.Description = null;
                return Task.CompletedTask;
            }
        );
        opts.AddFluentValidationRules();
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(opts =>
        {
            opts.EnableDarkMode()
                .WithTitle("Movie Api Reference")
                .WithTheme(ScalarTheme.BluePlanet)
                .ShowOperationId()
                .WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.Curl)
                .WithDocumentDownloadType(DocumentDownloadType.Json)
                .WithJsonDocumentDownload()
                .PreserveSchemaPropertyOrder();
        });
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler();
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
