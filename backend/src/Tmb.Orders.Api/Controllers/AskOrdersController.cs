using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tmb.Orders.Api.Llm;
using Tmb.Orders.Domain.Entities;
using Tmb.Orders.Domain.Enums;
using Tmb.Orders.Infrastructure.Persistence;

namespace Tmb.Orders.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AskOrdersController : ControllerBase
{
    private readonly OrdersDbContext _dbContext;
    private readonly OpenAiClient _llm;
    private readonly ILogger<AskOrdersController> _logger;

    public AskOrdersController(
        OrdersDbContext dbContext,
        OpenAiClient llm,
        ILogger<AskOrdersController> logger)
    {
        _dbContext = dbContext;
        _llm = llm;
        _logger = logger;
    }

    public class AskOrdersRequest
    {
        public string Question { get; set; } = string.Empty;
    }

    [HttpPost]
    public async Task<ActionResult> Ask(
        [FromBody] AskOrdersRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest("A pergunta não pode ser vazia.");
        }

        var now = DateTime.UtcNow;
        var today = now.Date;

        
        var diffToMonday = (int)today.DayOfWeek - (int)DayOfWeek.Monday;
        if (diffToMonday < 0) diffToMonday += 7;
        var weekStart = today.AddDays(-diffToMonday);

        var monthStart = new DateTime(
            now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

       

        var totalPedidosHoje = await _dbContext.Orders
            .CountAsync(o => o.DataCriacao.Date == today, cancellationToken);

        var totalPedidosSemana = await _dbContext.Orders
            .CountAsync(o =>
                o.DataCriacao.Date >= weekStart &&
                o.DataCriacao.Date <= today,
                cancellationToken);

        var pedidosPendentes = await _dbContext.Orders
            .CountAsync(o => o.Status == OrderStatus.Pending, cancellationToken);

        var totalFinalizadosHoje = await _dbContext.Orders
            .CountAsync(o =>
                o.Status == OrderStatus.Finalized &&
                o.DataCriacao.Date == today,
                cancellationToken);

        
        var totalFinalizadosSemana = await _dbContext.Orders
            .CountAsync(o =>
                o.Status == OrderStatus.Finalized &&
                o.DataCriacao.Date >= weekStart &&
                o.DataCriacao.Date <= today,
                cancellationToken);

        
        var finalizadosMes = await _dbContext.Orders
            .Where(o => o.Status == OrderStatus.Finalized &&
                        o.DataCriacao >= monthStart)
            .Include(o => o.StatusHistory)
            .ToListAsync(cancellationToken);

        var totalFinalizadosMes = finalizadosMes.Count;
        var valorTotalFinalizadosMes = finalizadosMes.Sum(o => o.Valor);

        var tempos = new List<double>();

        foreach (var order in finalizadosMes)
        {
            var first = order.StatusHistory
                .OrderBy(h => h.ChangedAt)
                .FirstOrDefault();
            var last = order.StatusHistory
                .OrderBy(h => h.ChangedAt)
                .LastOrDefault();

            if (first != null && last != null)
            {
                var minutes = (last.ChangedAt - first.ChangedAt).TotalMinutes;
                if (minutes >= 0)
                    tempos.Add(minutes);
            }
        }

        var tempoMedioMinutos = tempos.Count > 0
            ? Math.Round(tempos.Average(), 2)
            : 0;

       
        var porStatus = await _dbContext.Orders
            .GroupBy(o => o.Status)
            .Select(g => new
            {
                status = g.Key.ToString(),
                quantidade = g.Count(),
                valorTotal = g.Sum(o => o.Valor)
            })
            .ToListAsync(cancellationToken);

        var metrics = new
        {
            referencia = new
            {
                hoje = today.ToString("yyyy-MM-dd"),
                inicioSemana = weekStart.ToString("yyyy-MM-dd"),
                inicioMes = monthStart.ToString("yyyy-MM-dd")
            },
            totais = new
            {
                pedidosHoje = totalPedidosHoje,
                pedidosSemana = totalPedidosSemana,
                pedidosPendentes = pedidosPendentes,
                finalizadosHoje = totalFinalizadosHoje,
                finalizadosSemana = totalFinalizadosSemana,
                finalizadosMes = totalFinalizadosMes
            },
            valores = new
            {
                finalizadosMes = valorTotalFinalizadosMes
            },
            tempos = new
            {
                aprovacaoMediaMinutos = tempoMedioMinutos
            },
            porStatus
        };

        var metricsJson = System.Text.Json.JsonSerializer.Serialize(
            metrics,
            new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

        var prompt = $@"
Você é um assistente de análise de pedidos de um painel interno.

### DADOS DISPONÍVEIS (em JSON)
{metricsJson}

Significado dos campos principais:
- totais.pedidosHoje: quantidade de pedidos criados hoje.
- totais.pedidosSemana: quantidade de pedidos criados da segunda-feira até hoje.
- totais.pedidosPendentes: pedidos com status PENDING.
- totais.finalizadosHoje: pedidos com status FINALIZED criados hoje.
- totais.finalizadosSemana: pedidos com status FINALIZED criados nesta semana.
- totais.finalizadosMes: pedidos com status FINALIZED criados neste mês.
- valores.finalizadosMes: valor financeiro total dos pedidos finalizados neste mês.
- tempos.aprovacaoMediaMinutos: tempo médio em minutos para um pedido sair do primeiro status até chegar em FINALIZED.
- porStatus: lista com quantidade e valor total por status (Pending, Processing, Finalized).

### PERGUNTA DO USUÁRIO (em português):
""{request.Question}""

### INSTRUÇÕES
1. Responda SEMPRE em português, de forma simples e direta.
2. Use APENAS os dados do JSON acima. Não invente números.
3. Se a pergunta pedir algo que não está nesses dados
   (por exemplo, lista de clientes, detalhes de um pedido específico, datas exatas),
   explique o que você CONSEGUE responder com as métricas atuais
   e, se fizer sentido, ofereça um resumo dos principais números.
4. Quando a pergunta for de contagem, responda explicitamente com números.
5. Quando falar de valores, deixe claro que é valor financeiro (R$).";

        string answer;

        try
        {
            var llmAnswer = await _llm.AskAsync(prompt, cancellationToken);

            if (string.IsNullOrWhiteSpace(llmAnswer))
            {
                answer = $@"
Aqui vai um resumo baseado nos dados atuais:

- Pedidos hoje: {totalPedidosHoje}
- Pedidos na semana: {totalPedidosSemana}
- Pedidos pendentes: {pedidosPendentes}
- Pedidos finalizados hoje: {totalFinalizadosHoje}
- Pedidos finalizados na semana: {totalFinalizadosSemana}
- Pedidos finalizados no mês: {totalFinalizadosMes}
- Valor total de pedidos finalizados no mês ({monthStart:MM/yyyy}): {valorTotalFinalizadosMes:C}
- Tempo médio para aprovar pedidos: {tempoMedioMinutos} minutos.

Obs: não consegui falar com a IA externa agora (limite de uso ou chave ausente),
então essa resposta foi montada diretamente a partir das métricas do banco.";
            }
            else
            {
                answer = llmAnswer;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar AskOrders. Usando fallback local.");

            answer = $@"
Aqui vai um resumo baseado nos dados atuais:

- Pedidos hoje: {totalPedidosHoje}
- Pedidos na semana: {totalPedidosSemana}
- Pedidos pendentes: {pedidosPendentes}
- Pedidos finalizados hoje: {totalFinalizadosHoje}
- Pedidos finalizados na semana: {totalFinalizadosSemana}
- Pedidos finalizados no mês: {totalFinalizadosMes}
- Valor total de pedidos finalizados no mês ({monthStart:MM/yyyy}): {valorTotalFinalizadosMes:C}
- Tempo médio para aprovar pedidos: {tempoMedioMinutos} minutos.

Obs: ocorreu um erro inesperado ao chamar a IA externa,
então essa resposta foi montada diretamente a partir das métricas do banco.";
        }

        return Ok(new { answer });
    }
}
