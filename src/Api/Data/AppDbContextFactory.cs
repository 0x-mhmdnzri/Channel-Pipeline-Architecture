using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Api.Data;

/// <summary>
/// Design-time factory for "dotnet ef migrations" / "dotnet ef database update".
/// Connection string is read only from appsettings.json / environment — no hardcoded value.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = ResolveContentRoot();

        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' not found. " +
                "Set ConnectionStrings:Default in appsettings.json " +
                $"(looked in: {basePath}).");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(cs);

        return new AppDbContext(optionsBuilder.Options);
    }

    private static string ResolveContentRoot()
    {
        var basePath = Directory.GetCurrentDirectory();

        if (File.Exists(Path.Combine(basePath, "appsettings.json")))
            return basePath;

        var apiPath = Path.Combine(basePath, "src", "Api");
        if (File.Exists(Path.Combine(apiPath, "appsettings.json")))
            return apiPath;

        var dir = new DirectoryInfo(basePath);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Api", "appsettings.json");
            if (File.Exists(candidate))
                return Path.GetDirectoryName(candidate)!;

            candidate = Path.Combine(dir.FullName, "appsettings.json");
            if (File.Exists(candidate))
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            "Could not find appsettings.json. Run dotnet ef from the solution root or src/Api.");
    }
}
