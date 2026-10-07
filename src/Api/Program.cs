using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Api.Concurrency;
using Api.Data;
using Api.Endpoints;
using Api.Models;

var builder = WebApplication.CreateBuilder(args);

var connStr = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Connection string 'Default' is missing. Set ConnectionStrings:Default in appsettings.json.");
Db.Configure(connStr);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(connStr));

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddSingleton<MutationQueue>();
builder.Services.AddSingleton<IdempotencyStore>();
builder.Services.AddSingleton<WriteGate>();

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.MapGet("/", () => Results.Redirect("/openapi/v1.json"));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.MapHealthEndpoints();
app.MapSeedEndpoints();
app.MapMutationEndpoints();
app.MapOrganizationsEndpoints();
app.MapCompaniesEndpoints();
app.MapDepartmentsEndpoints();
app.MapEmployeesEndpoints();

app.Run("http://0.0.0.0:5080");

[JsonSerializable(typeof(Api.Models.Organization))]
[JsonSerializable(typeof(List<Api.Models.Organization>))]
[JsonSerializable(typeof(Api.Models.Company))]
[JsonSerializable(typeof(List<Api.Models.Company>))]
[JsonSerializable(typeof(Api.Models.Department))]
[JsonSerializable(typeof(Api.Models.Employee))]
[JsonSerializable(typeof(List<Api.Models.Employee>))]
[JsonSerializable(typeof(Api.Models.Address))]
[JsonSerializable(typeof(List<Api.Models.Address>))]
[JsonSerializable(typeof(Api.Models.Project))]
[JsonSerializable(typeof(Api.Models.TaskItem))]
[JsonSerializable(typeof(OrganizationDetail))]
[JsonSerializable(typeof(CompanySummary))]
[JsonSerializable(typeof(List<CompanySummary>))]
[JsonSerializable(typeof(CompanyDetail))]
[JsonSerializable(typeof(OrganizationBrief))]
[JsonSerializable(typeof(DepartmentSummary))]
[JsonSerializable(typeof(List<DepartmentSummary>))]
[JsonSerializable(typeof(DepartmentDetail))]
[JsonSerializable(typeof(CompanyBrief))]
[JsonSerializable(typeof(EmployeeSummary))]
[JsonSerializable(typeof(List<EmployeeSummary>))]
[JsonSerializable(typeof(EmployeeDetail))]
[JsonSerializable(typeof(DepartmentBrief))]
[JsonSerializable(typeof(ProjectSummary))]
[JsonSerializable(typeof(List<ProjectSummary>))]
[JsonSerializable(typeof(SeedResult))]
[JsonSerializable(typeof(Api.Endpoints.HealthStatus))]
[JsonSerializable(typeof(Api.Endpoints.DbHealthStatus))]
[JsonSerializable(typeof(UpdateTitleRequest))]
[JsonSerializable(typeof(UpdateTitleResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
