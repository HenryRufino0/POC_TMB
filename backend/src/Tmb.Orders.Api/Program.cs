using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Azure.Messaging.ServiceBus;

using Tmb.Orders.Infrastructure.DependencyInjection;
using Tmb.Orders.Infrastructure.Persistence;
using Tmb.Orders.Api.Configurations;
using Tmb.Orders.Api.Messaging;
using Tmb.Orders.Api.Llm;

using HealthChecks.AzureServiceBus;
using HealthChecks.NpgSql;

var builder = WebApplication.CreateBuilder(args);


// Service Bus 

builder.Services.Configure<ServiceBusOptions>(
    builder.Configuration.GetSection(ServiceBusOptions.SectionName));

builder.Services.AddSingleton<ServiceBusClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
    return new ServiceBusClient(options.ConnectionString);
});

builder.Services.AddScoped<OrderCreatedPublisher>();


// CORS (frontend)

var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:3000";

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(frontendUrl)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);


// Health Checks (Postgres e Service Bus)

var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

// pega ServiceBusOptions de novo só pra configurar o healthcheck
var sbOptions = builder.Configuration
    .GetSection(ServiceBusOptions.SectionName)
    .Get<ServiceBusOptions>() ?? new ServiceBusOptions();

builder.Services.AddHealthChecks()
    .AddNpgSql(dbConnectionString, name: "postgres")
    .AddAzureServiceBusQueue(
        sbOptions.ConnectionString,
        queueName: sbOptions.QueueName,
        name: "servicebus-queue");


// LLM

builder.Services.Configure<OpenAiOptions>(
    builder.Configuration.GetSection(OpenAiOptions.SectionName));

builder.Services.AddHttpClient<OpenAiClient>();

var app = builder.Build();

// CORS
app.UseCors("AllowFrontend");

// Aplica migrations na inicialização
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    db.Database.Migrate();
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();


// Endpoint de HealthCheck (Docker)

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
