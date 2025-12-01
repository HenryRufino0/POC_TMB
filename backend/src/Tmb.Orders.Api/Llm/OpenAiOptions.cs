namespace Tmb.Orders.Api.Llm
{
    public class OpenAiOptions
    {
        public const string SectionName = "OpenAI";

        public string ApiKey { get; set; } = string.Empty;

        // pode trocar o modelo depois se quiser
        public string Model { get; set; } = "gpt-4.1-mini";

        // se estiver usando OpenAI “puro”
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    }
}
