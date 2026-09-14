using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Takwene.Application.Interfaces;
using Takwene.Infrastructure.Persistence;
using Takwene.Infrastructure.Services;

namespace Takwene.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=takwene.db";

        services.AddDbContext<TakweneDbContext>(options =>
            options.UseSqlite(connectionString, b => b.MigrationsAssembly(typeof(TakweneDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<TakweneDbContext>());
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
