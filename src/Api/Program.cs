using System.Text.Json.Serialization;
using Api.Data;
using Api.Endpoints;
using Api.Models;

var builder = WebApplication.CreateBuilder(args);

var connStr = builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Port=5432;Database=channeldb;Username=channelapp;Password=channelapp";
Db.Configure(connStr);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi(); // /openapi/v1.json
app.MapGet("/", () => Results.Redirect("/openapi/v1.json"));

await Schema.EnsureCreatedAsync();

app.MapHealthEndpoints();
app.MapSeedEndpoints();
app.MapOrganizationsEndpoints();
app.MapCompaniesEndpoints();
app.MapDepartmentsEndpoints();
app.MapEmployeesEndpoints();

app.Run("http://0.0.0.0:5080");

[JsonSerializable(typeof(Organization))]
[JsonSerializable(typeof(List<Organization>))]
[JsonSerializable(typeof(Company))]
[JsonSerializable(typeof(List<Company>))]
[JsonSerializable(typeof(Department))]
[JsonSerializable(typeof(Employee))]
[JsonSerializable(typeof(List<Employee>))]
[JsonSerializable(typeof(Address))]
[JsonSerializable(typeof(List<Address>))]
[JsonSerializable(typeof(Project))]
[JsonSerializable(typeof(TaskItem))]
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
internal partial class AppJsonSerializerContext : JsonSerializerContext;
