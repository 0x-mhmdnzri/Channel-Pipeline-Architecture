using Npgsql;

namespace Api.Data;

/// <summary>
/// Thin Npgsql helper for raw SQL paths (Seeder, hot-path queries).
/// Connection string is set once from appsettings via <see cref="Configure"/>.
/// </summary>
public static class Db
{
    private static string? _connectionString;

    public static void Configure(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString));

        _connectionString = connectionString;
    }

    public static string ConnectionString =>
        _connectionString
        ?? throw new InvalidOperationException(
            "Db is not configured. Call Db.Configure() with ConnectionStrings:Default from appsettings.json.");

    public static NpgsqlConnection CreateConnection()
        => new(ConnectionString);

    public static async Task<NpgsqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var conn = CreateConnection();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        return conn;
    }
}
