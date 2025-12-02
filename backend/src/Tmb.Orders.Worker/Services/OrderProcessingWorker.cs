using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Npgsql;
using Tmb.Orders.Worker.Configurations;

namespace Tmb.Orders.Worker.Services;

public class OrderProcessingWorker : BackgroundService
{
    private readonly ServiceBusProcessor _processor;
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<OrderProcessingWorker> _logger;

    public OrderProcessingWorker(
        ServiceBusClient client,
        IOptions<ServiceBusOptions> options,
        NpgsqlDataSource dataSource,
        ILogger<OrderProcessingWorker> logger)
    {
        _dataSource = dataSource;
        _logger = logger;

        var sbOptions = options.Value;

        _processor = client.CreateProcessor(sbOptions.QueueName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 1,
            AutoCompleteMessages = false
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor.ProcessMessageAsync += ProcessMessageHandler;
        _processor.ProcessErrorAsync += ProcessErrorHandler;

        _logger.LogInformation("OrderProcessingWorker iniciado. Aguardando mensagens...");

        await _processor.StartProcessingAsync(stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
    
    private const int STATUS_FINALIZED = 2;

private async Task ProcessMessageHandler(ProcessMessageEventArgs args)
{
    try
    {
        var body = args.Message.Body.ToString();
        _logger.LogInformation("Mensagem recebida do Service Bus: {Body}", body);

        var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("OrderId", out var orderIdElement))
        {
            _logger.LogWarning("Mensagem sem OrderId, enviando para dead-letter.");
            await args.DeadLetterMessageAsync(args.Message);
            return;
        }

        var orderId = orderIdElement.GetGuid();

        await Task.Delay(TimeSpan.FromSeconds(5));

        await UpdateOrderStatusAsync(orderId, STATUS_FINALIZED);

        _logger.LogInformation("Pedido {OrderId} atualizado para FINALIZED.", orderId);

        await args.CompleteMessageAsync(args.Message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Erro ao processar mensagem. Abandonando para reentrega.");
        await args.AbandonMessageAsync(args.Message);
    }
}

    private Task ProcessErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception,
            "Erro no Service Bus. Entidade: {EntityPath}; Namespace: {FullyQualifiedNamespace}",
            args.EntityPath,
            args.FullyQualifiedNamespace);

        return Task.CompletedTask;
    }

   private async Task UpdateOrderStatusAsync(Guid orderId, int newStatus)
{
    await using var conn = await _dataSource.OpenConnectionAsync();
    await using var tx = await conn.BeginTransactionAsync();
    await using var cmd = conn.CreateCommand();

    cmd.Transaction = tx;

    cmd.CommandText = """
        UPDATE "Orders"
        SET "Status" = @status
        WHERE "Id" = @id;
    """;

    cmd.Parameters.AddWithValue("status", newStatus);
    cmd.Parameters.AddWithValue("id", orderId);

    var rows = await cmd.ExecuteNonQueryAsync();
    if (rows == 0)
    {
        _logger.LogWarning("Nenhum pedido encontrado com Id {OrderId} para atualizar.", orderId);
        await tx.RollbackAsync();
        return;
    }
    cmd.Parameters.Clear();
    cmd.CommandText = """
        INSERT INTO "OrderStatusHistories" ("ChangedAt", "OrderId", "Status")
        VALUES (@changedAt, @orderId, @status);
    """;

    cmd.Parameters.AddWithValue("changedAt", DateTime.UtcNow);
    cmd.Parameters.AddWithValue("orderId", orderId);
    cmd.Parameters.AddWithValue("status", newStatus);

    await cmd.ExecuteNonQueryAsync();

    await tx.CommitAsync();
}
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _processor.StopProcessingAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
