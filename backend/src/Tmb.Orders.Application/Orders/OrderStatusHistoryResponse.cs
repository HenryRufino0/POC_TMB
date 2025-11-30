using Tmb.Orders.Domain.Enums;

namespace Tmb.Orders.Application.Orders;

public class OrderStatusHistoryResponse
{
    public OrderStatus Status { get; set; }
    public DateTime ChangedAt { get; set; }
}
