namespace Models;

public sealed record TransformedRecord(
    long Id,
    string NormalizedPayload,
    DateTimeOffset ProcessedAt);
