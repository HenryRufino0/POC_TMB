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

    // se quiser pode até apagar a AskOrdersResponse, não vamos mais usar
    // public class AskOrdersResponse { public string Answer { get; set; } = string.Empty; }

    [HttpPost]
    public async Task<ActionResult> Ask(
        [FromBody] AskOrdersRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest("A pergunta não pode ser vazia.");
        }

        var now        = DateTime.UtcNow;
        var today      = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // total de pedidos hoje
        var totalHoje = await _dbContext.Orders
            .CountAsync(o => o.DataCriacao.Date == today, cancellationToken);

        // pendentes
        var pendentes = await _dbContext.Orders
            .CountAsync(o => o.Status == OrderStatus.Pending, cancellationToken);

        // finalizados no mês
        var finalizadosMes = await _dbContext.Orders
            .Where(o => o.Status == OrderStatus.Finalized && o.DataCriacao >= monthStart)
            .Include(o => o.StatusHistory)
            .ToListAsync(cancellationToken);

        var valorTotalFinalizadosMes = finalizadosMes.Sum(o => o.Valor);

        // tempo médio entre primeiro e último status
        var tempos = new List<double>();

        foreach (var order in finalizadosMes)
        {
            var first = order.StatusHistory.OrderBy(h => h.ChangedAt).FirstOrDefault();
            var last  = order.StatusHistory.OrderBy(h => h.ChangedAt).LastOrDefault();

            if (first != null && last != null)
            {
                var minutes = (last.ChangedAt - first.ChangedAt).TotalMinutes;
                if (minutes >= 0)
                    tempos.Add(minutes);
            }
        }

        var tempoMedioMinutos = tempos.Count > 0 ? tempos.Average() : 0;

        var metrics = new
        {
            totalPedidosHoje = totalHoje,
            pedidosPendentes = pendentes,
            valorTotalPedidosFinalizadosMes = valorTotalFinalizadosMes,
            tempoMedioAprovacaoMinutos = Math.Round(tempoMedioMinutos, 2),
            mesReferencia = monthStart.ToString("yyyy-MM")
        };

        var metricsJson = System.Text.Json.JsonSerializer.Serialize(metrics);

        var prompt = $@"
Você é um assistente de análise de pedidos de um sistema interno.

Aqui estão métricas calculadas a partir do banco de dados (em JSON):

{metricsJson}

O usuário fez a seguinte pergunta (em português sobre pedidos):

""{request.Question}""

Use APENAS os dados deste JSON para responder.
Responda em português, em tom simples e amigável.
Se a pergunta não puder ser respondida com esses dados, diga claramente que só consegue responder
sobre quantidade de pedidos hoje, pedidos pendentes, tempo médio de aprovação
e valor total de pedidos finalizados no mês.
";

        string answer;

        try
        {
            var llmAnswer = await _llm.AskAsync(prompt, cancellationToken);

            if (string.IsNullOrWhiteSpace(llmAnswer))
            {
                // fallback se LLM falhar / quota / sem chave
                answer = $@"
Aqui vai um resumo baseado nos dados atuais:

- Pedidos hoje: {totalHoje}
- Pedidos pendentes: {pendentes}
- Valor total de pedidos finalizados no mês ({monthStart:MM/yyyy}): {valorTotalFinalizadosMes:C}
- Tempo médio para aprovar pedidos: {Math.Round(tempoMedioMinutos, 2)} minutos.

Obs: não consegui falar com a IA externa agora (limite de uso ou chave ausente),
então essa resposta foi montada diretamente a partir das métricas do banco.
";
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

- Pedidos hoje: {totalHoje}
- Pedidos pendentes: {pendentes}
- Valor total de pedidos finalizados no mês ({monthStart:MM/yyyy}): {valorTotalFinalizadosMes:C}
- Tempo médio para aprovar pedidos: {Math.Round(tempoMedioMinutos, 2)} minutos.

Obs: ocorreu um erro inesperado ao chamar a IA externa,
então essa resposta foi montada diretamente a partir das métricas do banco.
";
        }

        // 👈 AQUI é a mágica: garante camelCase na resposta
        return Ok(new { answer });
    }
}
