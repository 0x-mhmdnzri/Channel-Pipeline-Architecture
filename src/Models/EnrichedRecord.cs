namespace Models;

public sealed record EnrichedRecord(
    long Id,
    string NormalizedPayload,
    string ExtraData,
    DateTimeOffset ProcessedAt);
