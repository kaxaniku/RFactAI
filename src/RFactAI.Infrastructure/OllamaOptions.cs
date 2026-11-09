namespace RFactAI.Infrastructure;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "qwen2.5:14b";

    /// <summary>A 14B model on CPU can be slow, so the default is generous.</summary>
    public int TimeoutSeconds { get; set; } = 120;
}
