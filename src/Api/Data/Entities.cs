namespace Api.Data.Entities;

public class Organization
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Country { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Company> Companies { get; set; } = new List<Company>();
}

public class Company
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Name { get; set; } = "";
    public string Industry { get; set; } = "";
    public DateTime FoundedAt { get; set; }
    public Organization Organization { get; set; } = null!;
    public ICollection<Department> Departments { get; set; } = new List<Department>();
}

public class Department
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public int Budget { get; set; }
    public Company Company { get; set; } = null!;
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}

public class Employee
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Title { get; set; } = "";
    public decimal Salary { get; set; }
    public DateTime HiredAt { get; set; }
    public Department Department { get; set; } = null!;
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
}

public class Address
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public bool IsPrimary { get; set; }
    public Employee Employee { get; set; } = null!;
}

public class Project
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Department Department { get; set; } = null!;
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}

public class TaskItem
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int? AssigneeId { get; set; }
    public string Title { get; set; } = "";
    public string Status { get; set; } = "";
    public int Priority { get; set; } = 3;
    public Project Project { get; set; } = null!;
    public Employee? Assignee { get; set; }
}
