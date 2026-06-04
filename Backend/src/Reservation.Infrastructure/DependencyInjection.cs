using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reservation.Domain.Interfaces;
using Reservation.Infrastructure.Clock;
using Reservation.Infrastructure.Persistence;
using Reservation.Infrastructure.Persistence.Interceptors;

namespace Reservation.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<UpdatedAtInterceptor>();

        services.AddDbContext<ReservationDbContext>((sp, options) =>
        {
            options.UseSqlite(configuration.GetConnectionString("Default"));
            options.AddInterceptors(sp.GetRequiredService<UpdatedAtInterceptor>());
        });

        return services;
    }
}
