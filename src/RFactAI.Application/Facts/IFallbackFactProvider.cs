namespace RFactAI.Application.Facts;

public interface IFallbackFactProvider
{
    /// <summary>Returns curated facts for the category, or null when none exist.</summary>
    FactResponse? Get(string category, int count);
}
