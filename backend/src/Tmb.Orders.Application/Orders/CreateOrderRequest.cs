namespace Tmb.Orders.Application.Orders;

public class CreateOrderRequest
{
    public string Cliente { get; set; } = default!;
    public string Produto { get; set; } = default!;
    public decimal Valor { get; set; }
}
