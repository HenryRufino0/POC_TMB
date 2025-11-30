using Microsoft.EntityFrameworkCore;
using Tmb.Orders.Application.Orders;
using Tmb.Orders.Domain.Entities;
using Tmb.Orders.Infrastructure.Persistence;

namespace Tmb.Orders.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly OrdersDbContext _dbContext;

    public OrderService(OrdersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = new Order(request.Cliente, request.Produto, request.Valor);

        // status inicial como PROCESSING (1)
        order.MarkAsProcessing();

        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(order);
    }

    public async Task<List<OrderResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _dbContext.Orders
            .Include(o => o.StatusHistory)
            .OrderByDescending(o => o.DataCriacao)
            .ToListAsync(cancellationToken);

        return orders.Select(MapToResponse).ToList();
    }

    public async Task<OrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        return order is null ? null : MapToResponse(order);
    }

    private static OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            Cliente = order.Cliente,
            Produto = order.Produto,
            Valor = order.Valor,
            Status = order.Status,              
            DataCriacao = order.DataCriacao,
            StatusHistory = order.StatusHistory
                .OrderBy(h => h.ChangedAt)
                .Select(h => new OrderStatusHistoryResponse
                {
                    Status = h.Status,
                    ChangedAt = h.ChangedAt
                })
                .ToList()
        };
    }
}
