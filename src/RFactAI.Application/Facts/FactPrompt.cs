using Microsoft.Extensions.AI;

namespace RFactAI.Application.Facts;

public static class FactPrompt
{
    public const string System =
        """
        You are a fact generator. Respond ONLY with a single JSON object, with no prose and no markdown, in exactly this shape:
        {"category": "<string>", "facts": ["<string>", "..."]}
        Every fact must be true, a single concise sentence, and distinct from the others.
        """;

    public const string RetryNudge =
        "Your last reply was not valid JSON in the required shape. Reply again with ONLY the JSON object.";

    public static string User(string category, int count) =>
        $"Give me exactly {count} interesting, true facts about the category \"{category}\".";

    public static List<ChatMessage> Create(string category, int count) =>
    [
        new(ChatRole.System, System),
        new(ChatRole.User, User(category, count))
    ];
}
