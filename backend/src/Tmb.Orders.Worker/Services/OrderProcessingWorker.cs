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

    private const int STATUS_FINALIZED = 2;

    // pequeno helper pra saber o resultado do update
    private enum UpdateResult
    {
        Updated,
        DuplicateMessage,
        NotFound
    }

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

    private async Task ProcessMessageHandler(ProcessMessageEventArgs args)
    {
        try
        {
            var body = args.Message.Body.ToString();
            var messageId = args.Message.MessageId ?? Guid.NewGuid().ToString();

            _logger.LogInformation("Mensagem recebida do Service Bus: {Body} | MessageId={MessageId}", body, messageId);

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("OrderId", out var orderIdElement))
            {
                _logger.LogWarning("Mensagem sem OrderId, enviando para dead-letter. MessageId={MessageId}", messageId);
                await args.DeadLetterMessageAsync(args.Message);
                return;
            }

            var orderId = orderIdElement.GetGuid();

          
            await Task.Delay(TimeSpan.FromSeconds(5), args.CancellationToken);

            var result = await UpdateOrderStatusAsync(orderId, STATUS_FINALIZED, messageId);

            switch (result)
            {
                case UpdateResult.Updated:
                    _logger.LogInformation(
                        "Pedido {OrderId} atualizado para FINALIZED. MessageId={MessageId}",
                        orderId, messageId);
                    break;

                case UpdateResult.DuplicateMessage:
                    _logger.LogInformation(
                        "Mensagem duplicada ignorada para OrderId={OrderId}. MessageId={MessageId}",
                        orderId, messageId);
                    break;

                case UpdateResult.NotFound:
                    _logger.LogWarning(
                        "Nenhum pedido encontrado com Id {OrderId} para atualizar. MessageId={MessageId}",
                        orderId, messageId);
                    break;
            }

          
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

    private async Task<UpdateResult> UpdateOrderStatusAsync(Guid orderId, int newStatus, string messageId)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;

        
        cmd.CommandText = """
            SELECT "Status", "LastProcessedMessageId"
            FROM "Orders"
            WHERE "Id" = @id
            FOR UPDATE;
        """;

        cmd.Parameters.AddWithValue("id", orderId);

        int currentStatus;
        string? lastProcessedMessageId;

        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (!await reader.ReadAsync())
            {
                // não encontrou o pedido
                await tx.RollbackAsync();
                return UpdateResult.NotFound;
            }

            currentStatus = reader.GetInt32(0);
            lastProcessedMessageId = reader.IsDBNull(1) ? null : reader.GetString(1);
        }

      
        if (!string.IsNullOrWhiteSpace(lastProcessedMessageId) &&
            lastProcessedMessageId == messageId)
        {
            await tx.RollbackAsync();
            return UpdateResult.DuplicateMessage;
        }

        cmd.Parameters.Clear();

    
        cmd.CommandText = """
            UPDATE "Orders"
            SET "Status" = @status,
                "LastProcessedMessageId" = @messageId
            WHERE "Id" = @id;
        """;

        cmd.Parameters.AddWithValue("status", newStatus);
        cmd.Parameters.AddWithValue("messageId", messageId);
        cmd.Parameters.AddWithValue("id", orderId);

        var rows = await cmd.ExecuteNonQueryAsync();
        if (rows == 0)
        {
       
            await tx.RollbackAsync();
            return UpdateResult.NotFound;
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
        return UpdateResult.Updated;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _processor.StopProcessingAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
