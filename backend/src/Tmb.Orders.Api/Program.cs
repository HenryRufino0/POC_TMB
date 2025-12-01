using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Tmb.Orders.Infrastructure.DependencyInjection;
using Tmb.Orders.Infrastructure.Persistence;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Tmb.Orders.Api.Configurations;
using Tmb.Orders.Api.Messaging;
using Microsoft.EntityFrameworkCore;
using Tmb.Orders.Api.Llm;

var builder = WebApplication.CreateBuilder(args);

// ================== SERVICE BUS ==================
builder.Services.Configure<ServiceBusOptions>(
    builder.Configuration.GetSection(ServiceBusOptions.SectionName));

builder.Services.AddSingleton<ServiceBusClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value;
    return new ServiceBusClient(options.ConnectionString);
});

builder.Services.AddScoped<OrderCreatedPublisher>();

// ================== CORS ==================
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

// ================== CONTROLLERS + SWAGGER ==================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ================== INFRA (DB, ETC) ==================
builder.Services.AddInfrastructure(builder.Configuration);

// ================== HEALTH CHECKS ==================
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

// ================== OPENAI / LLM ==================
builder.Services.Configure<OpenAiOptions>(
    builder.Configuration.GetSection(OpenAiOptions.SectionName));

builder.Services.AddHttpClient<OpenAiClient>();

// ================== BUILD APP ==================
var app = builder.Build();

// CORS
app.UseCors("AllowFrontend");

// ====== APLICA MIGRATIONS AUTOMATICAMENTE ======
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

// Endpoint de healthcheck usado pelo Docker
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
