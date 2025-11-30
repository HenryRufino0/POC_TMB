using System;
using Tmb.Orders.Domain.Enums;

namespace Tmb.Orders.Domain.Entities;

public class OrderStatusHistory
{
    public long Id { get; private set; }
    public Guid OrderId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime ChangedAt { get; private set; }

    private OrderStatusHistory() { } // EF

    public OrderStatusHistory(Guid orderId, OrderStatus status)
    {
        OrderId = orderId;
        Status = status;
        ChangedAt = DateTime.UtcNow;
    }

    public static OrderStatusHistory Create(Guid orderId, OrderStatus status)
        => new(orderId, status);


}
