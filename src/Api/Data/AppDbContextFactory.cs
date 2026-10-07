using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Api.Data;

/// <summary>
/// Used by "dotnet ef migrations" / "dotnet ef database update" at design-time.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=channeldb;Username=channelapp;Password=channelapp";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(cs)
            .Options;

        return new AppDbContext(options);
    }
}
