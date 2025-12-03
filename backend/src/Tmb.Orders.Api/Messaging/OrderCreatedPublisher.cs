using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Tmb.Orders.Api.Configurations;

namespace Tmb.Orders.Api.Messaging
{
    public class OrderCreatedPublisher
    {
        private readonly ServiceBusSender _sender;

        public OrderCreatedPublisher(ServiceBusClient client, IOptions<ServiceBusOptions> options)
        {
            var sbOptions = options.Value;
            _sender = client.CreateSender(sbOptions.QueueName);
        }

        public async Task PublishAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            var payload = new
            {
                OrderId = orderId
            };

            string body = JsonSerializer.Serialize(payload);

            var message = new ServiceBusMessage(body)
            {
                Subject = "order-created",
                CorrelationId = orderId.ToString()
            };

            message.ApplicationProperties["EventType"] = "OrderCreated";

            await _sender.SendMessageAsync(message, cancellationToken);
        }
    }
}
