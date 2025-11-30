using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Npgsql;
using Tmb.Orders.Worker.Configurations;
using Tmb.Orders.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

// 1) Service Bus config
builder.Services.Configure<ServiceBusOptions>(
    builder.Configuration.GetSection(ServiceBusOptions.SectionName));

// 2) Connection string do Postgres
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection") ??
    builder.Configuration["ConnectionStrings:DefaultConnection"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string do banco não encontrada.");
}

// registra a connection string como singleton
builder.Services.AddSingleton(new NpgsqlDataSourceBuilder(connectionString).Build());

// 3) ServiceBusClient
builder.Services.AddSingleton<ServiceBusClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
    return new ServiceBusClient(options.ConnectionString);
});

// 4) BackgroundService que processa os pedidos
builder.Services.AddHostedService<OrderProcessingWorker>();

var host = builder.Build();
host.Run();
