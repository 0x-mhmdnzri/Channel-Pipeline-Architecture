using Npgsql;

namespace Api.Data;

public static class Db
{
    private static string _connectionString = "Host=localhost;Port=5432;Database=channeldb;Username=channelapp;Password=channelapp";

    public static void Configure(string connectionString)
    {
        _connectionString = connectionString;
    }

    public static NpgsqlConnection CreateConnection()
        => new(_connectionString);

    public static async Task<NpgsqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var conn = CreateConnection();
        await conn.OpenAsync(ct).ConfigureAwait(false);
        return conn;
    }
}
