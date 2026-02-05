using Microsoft.Extensions.DependencyInjection;

namespace DbLoading.Database.Mock;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMockDatabase(this IServiceCollection services)
    {
        services.AddSingleton<IDbConnectionFactory, MockDbConnectionFactory>();
        return services;
    }
}
