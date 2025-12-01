using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Tmb.Orders.Api.Llm;

public class OpenAiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiClient> _logger;

    public OpenAiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAiClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        var model  = _configuration["OpenAI:Model"] ?? "gpt-4.1-mini";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("OpenAI API key não configurada. Retornando string vazia.");
            // Deixa o controller decidir o fallback
            return string.Empty;
        }

        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new
            {
                model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var json    = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "https://api.openai.com/v1/chat/completions",
                content,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Erro ao chamar OpenAI: {Status} - {Body}", response.StatusCode, errorBody);

                // NÃO lança exceção, só devolve vazio
                return string.Empty;
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc    = JsonDocument.Parse(responseJson);
            var root         = doc.RootElement;

            var message = root
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            // Se por algum motivo vier nulo, devolve vazio pra cair no fallback
            return message ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exceção ao chamar OpenAI");
            // Mesma ideia: nunca quebrar, sempre devolver vazio
            return string.Empty;
        }
    }
}
