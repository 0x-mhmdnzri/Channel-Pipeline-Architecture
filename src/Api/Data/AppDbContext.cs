using Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(e =>
        {
            e.ToTable("organizations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.Country).HasColumnName("country").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Company>(e =>
        {
            e.ToTable("companies");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.OrganizationId).HasColumnName("organization_id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.Industry).HasColumnName("industry").IsRequired();
            e.Property(x => x.FoundedAt).HasColumnName("founded_at");
            e.HasOne(x => x.Organization).WithMany(o => o.Companies)
                .HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.OrganizationId).HasDatabaseName("ix_companies_org");
        });

        modelBuilder.Entity<Department>(e =>
        {
            e.ToTable("departments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.CompanyId).HasColumnName("company_id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.Code).HasColumnName("code").IsRequired();
            e.Property(x => x.Budget).HasColumnName("budget");
            e.HasOne(x => x.Company).WithMany(c => c.Departments)
                .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.CompanyId).HasDatabaseName("ix_departments_company");
        });

        modelBuilder.Entity<Employee>(e =>
        {
            e.ToTable("employees");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.DepartmentId).HasColumnName("department_id");
            e.Property(x => x.FirstName).HasColumnName("first_name").IsRequired();
            e.Property(x => x.LastName).HasColumnName("last_name").IsRequired();
            e.Property(x => x.Email).HasColumnName("email").IsRequired();
            e.Property(x => x.Title).HasColumnName("title").IsRequired();
            e.Property(x => x.Salary).HasColumnName("salary").HasPrecision(12, 2);
            e.Property(x => x.HiredAt).HasColumnName("hired_at");
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.DepartmentId).HasDatabaseName("ix_employees_dept");
            e.HasOne(x => x.Department).WithMany(d => d.Employees)
                .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Address>(e =>
        {
            e.ToTable("addresses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.EmployeeId).HasColumnName("employee_id");
            e.Property(x => x.Street).HasColumnName("street").IsRequired();
            e.Property(x => x.City).HasColumnName("city").IsRequired();
            e.Property(x => x.Country).HasColumnName("country").IsRequired();
            e.Property(x => x.PostalCode).HasColumnName("postal_code").IsRequired();
            e.Property(x => x.IsPrimary).HasColumnName("is_primary");
            e.HasOne(x => x.Employee).WithMany(emp => emp.Addresses)
                .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.EmployeeId).HasDatabaseName("ix_addresses_emp");
        });

        modelBuilder.Entity<Project>(e =>
        {
            e.ToTable("projects");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.DepartmentId).HasColumnName("department_id");
            e.Property(x => x.Name).HasColumnName("name").IsRequired();
            e.Property(x => x.Status).HasColumnName("status").IsRequired();
            e.Property(x => x.StartDate).HasColumnName("start_date");
            e.Property(x => x.EndDate).HasColumnName("end_date");
            e.HasOne(x => x.Department).WithMany(d => d.Projects)
                .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.DepartmentId).HasDatabaseName("ix_projects_dept");
        });

        modelBuilder.Entity<TaskItem>(e =>
        {
            e.ToTable("tasks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ProjectId).HasColumnName("project_id");
            e.Property(x => x.AssigneeId).HasColumnName("assignee_id");
            e.Property(x => x.Title).HasColumnName("title").IsRequired();
            e.Property(x => x.Status).HasColumnName("status").IsRequired();
            e.Property(x => x.Priority).HasColumnName("priority");
            e.HasOne(x => x.Project).WithMany(p => p.Tasks)
                .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Assignee).WithMany(emp => emp.AssignedTasks)
                .HasForeignKey(x => x.AssigneeId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.ProjectId).HasDatabaseName("ix_tasks_project");
            e.HasIndex(x => x.AssigneeId).HasDatabaseName("ix_tasks_assignee");
        });
    }
}
