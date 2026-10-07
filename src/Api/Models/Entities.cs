namespace Api.Models;

public sealed record Organization(
    int Id,
    string Name,
    string Country,
    DateTime CreatedAt);

public sealed record Company(
    int Id,
    int OrganizationId,
    string Name,
    string Industry,
    DateTime FoundedAt);

public sealed record Department(
    int Id,
    int CompanyId,
    string Name,
    string Code,
    int Budget);

public sealed record Employee(
    int Id,
    int DepartmentId,
    string FirstName,
    string LastName,
    string Email,
    string Title,
    decimal Salary,
    DateTime HiredAt);

public sealed record Address(
    int Id,
    int EmployeeId,
    string Street,
    string City,
    string Country,
    string PostalCode,
    bool IsPrimary);

public sealed record Project(
    int Id,
    int DepartmentId,
    string Name,
    string Status,
    DateTime StartDate,
    DateTime? EndDate);

public sealed record TaskItem(
    int Id,
    int ProjectId,
    int? AssigneeId,
    string Title,
    string Status,
    int Priority);

public sealed record OrganizationDetail(
    int Id,
    string Name,
    string Country,
    IReadOnlyList<CompanySummary> Companies);

public sealed record CompanySummary(
    int Id,
    string Name,
    string Industry,
    int DepartmentCount,
    int EmployeeCount);

public sealed record CompanyDetail(
    int Id,
    string Name,
    string Industry,
    OrganizationBrief Organization,
    IReadOnlyList<DepartmentSummary> Departments);

public sealed record OrganizationBrief(int Id, string Name);

public sealed record DepartmentSummary(
    int Id,
    string Name,
    string Code,
    int EmployeeCount,
    int ProjectCount);

public sealed record DepartmentDetail(
    int Id,
    string Name,
    string Code,
    int Budget,
    CompanyBrief Company,
    IReadOnlyList<EmployeeSummary> Employees,
    IReadOnlyList<ProjectSummary> Projects);

public sealed record CompanyBrief(int Id, string Name);

public sealed record EmployeeSummary(
    int Id,
    string FullName,
    string Email,
    string Title);

public sealed record EmployeeDetail(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string Title,
    decimal Salary,
    DateTime HiredAt,
    DepartmentBrief Department,
    IReadOnlyList<Address> Addresses);

public sealed record DepartmentBrief(int Id, string Name, string Code);

public sealed record ProjectSummary(
    int Id,
    string Name,
    string Status,
    int TaskCount);

public sealed record SeedResult(
    int Organizations,
    int Companies,
    int Departments,
    int Employees,
    int Addresses,
    int Projects,
    int Tasks,
    double ElapsedSeconds);
