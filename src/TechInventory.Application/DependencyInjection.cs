using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TechInventory.Application.Auditing;
using TechInventory.Application.Behaviors;
using TechInventory.Application.Imports;

namespace TechInventory.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IAuditContext, AuditContext>();
        services.AddScoped<IDeviceImportProcessingService, DeviceImportProcessingService>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));

        return services;
    }
}
