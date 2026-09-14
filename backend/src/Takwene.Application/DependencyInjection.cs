using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Takwene.Application.Interfaces;
using Takwene.Application.Services;

namespace Takwene.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<IArtistService, ArtistService>();
        services.AddScoped<ITrackService, TrackService>();

        return services;
    }
}
