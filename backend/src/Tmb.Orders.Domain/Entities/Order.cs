using System;
using System.Collections.Generic;
using Tmb.Orders.Domain.Enums;

namespace Tmb.Orders.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public string Cliente { get; private set; } = default!;
    public string Produto { get; private set; } = default!;
    public decimal Valor { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime DataCriacao { get; private set; }

    
    public List<OrderStatusHistory> StatusHistory { get; private set; } = new();

    
    public string? LastProcessedMessageId { get; private set; }

    private Order() { } 

    public Order(string cliente, string produto, decimal valor)
    {
        Id = Guid.NewGuid();
        Cliente = cliente;
        Produto = produto;
        Valor = valor;
        Status = OrderStatus.Pending;
        DataCriacao = DateTime.UtcNow;

        AddStatusHistory(Status);
    }

    

    public void MarkAsProcessing(string? messageId = null)
    {
        if (Status == OrderStatus.Finalized)
            return;

        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Status inválido para processamento.");

        Status = OrderStatus.Processing;
        LastProcessedMessageId = messageId;
        AddStatusHistory(Status);
    }

    public void MarkAsCompleted(string? messageId = null)
    {
        if (Status == OrderStatus.Finalized)
            return;

        if (Status != OrderStatus.Processing)
            throw new InvalidOperationException("Status deve ser 'Processando'.");

        Status = OrderStatus.Finalized;
        LastProcessedMessageId = messageId;
        AddStatusHistory(Status);
    }

    public void MarkAsFinalized()
    {
        if (Status == OrderStatus.Finalized) return;

        Status = OrderStatus.Finalized;
        AddStatusHistory(Status);
    }

    private void AddStatusHistory(OrderStatus status)
    {
        StatusHistory.Add(new OrderStatusHistory(Id, status));
    }

    
}
