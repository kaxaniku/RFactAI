namespace RFactAI.Application.Facts;

public interface IFactService
{
    Task<FactResult> GetFactsAsync(string category, int count, CancellationToken cancellationToken = default);
}
