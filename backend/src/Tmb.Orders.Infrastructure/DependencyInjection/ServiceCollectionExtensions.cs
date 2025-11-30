using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tmb.Orders.Application.Orders;
using Tmb.Orders.Infrastructure.Persistence;
using Tmb.Orders.Infrastructure.Services;

namespace Tmb.Orders.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não configurada.");

        services.AddDbContext<OrdersDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        // Serviço de pedidos
        services.AddScoped<IOrderService, OrderService>();

        return services;
    }
}
