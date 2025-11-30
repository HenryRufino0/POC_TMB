using Tmb.Orders.Domain.Enums;

namespace Tmb.Orders.Application.Orders;

public class OrderResponse
{
    public Guid Id { get; set; }
    public string Cliente { get; set; } = default!;
    public string Produto { get; set; } = default!;
    public decimal Valor { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime DataCriacao { get; set; }

    public List<OrderStatusHistoryResponse> StatusHistory { get; set; } = new();
}
