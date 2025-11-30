namespace Tmb.Orders.Application.Orders;

public interface IOrderService
{
    Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<List<OrderResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<OrderResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
