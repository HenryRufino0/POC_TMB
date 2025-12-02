using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Npgsql;
using Tmb.Orders.Worker.Configurations;
using Tmb.Orders.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configura o Service Bus
builder.Services.Configure<ServiceBusOptions>(
    builder.Configuration.GetSection(ServiceBusOptions.SectionName));

// Conexão (connection string) do Postgress
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection") ??
    builder.Configuration["ConnectionStrings:DefaultConnection"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string do banco não encontrada.");
}

// registra a conexão (connection string) como singleton
builder.Services.AddSingleton(new NpgsqlDataSourceBuilder(connectionString).Build());

// ServiceBusClient
builder.Services.AddSingleton<ServiceBusClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
    return new ServiceBusClient(options.ConnectionString);
});

// BackgroundService que processa os pedidos
builder.Services.AddHostedService<OrderProcessingWorker>();

var host = builder.Build();
host.Run();
