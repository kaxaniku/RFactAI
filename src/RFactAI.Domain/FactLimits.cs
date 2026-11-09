namespace RFactAI.Domain;

public static class FactLimits
{
    public const int DefaultCount = 3;
    public const int MinCount = 1;
    public const int MaxCount = 10;
    public const int MaxCategoryLength = 50;

    /// <summary>Maximum number of fact rows kept per user; the oldest are removed first.</summary>
    public const int MaxHistoryPerUser = 100;

    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;
    public const int MaxEmailLength = 254;
}
