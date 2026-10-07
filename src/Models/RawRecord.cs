namespace Models;

public sealed record RawRecord(
    long Id,
    string Payload,
    DateTimeOffset ReceivedAt);
