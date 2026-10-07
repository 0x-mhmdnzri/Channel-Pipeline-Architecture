using Npgsql;

namespace Api.Data;

public static class Schema
{
    public static async Task EnsureCreatedAsync(CancellationToken ct = default)
    {
        await using var conn = await Db.OpenAsync(ct).ConfigureAwait(false);

        const string sql = """
            CREATE TABLE IF NOT EXISTS organizations (
                id          SERIAL PRIMARY KEY,
                name        TEXT NOT NULL,
                country     TEXT NOT NULL,
                created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS companies (
                id              SERIAL PRIMARY KEY,
                organization_id INT NOT NULL REFERENCES organizations(id) ON DELETE CASCADE,
                name            TEXT NOT NULL,
                industry        TEXT NOT NULL,
                founded_at      TIMESTAMPTZ NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_companies_org ON companies(organization_id);

            CREATE TABLE IF NOT EXISTS departments (
                id          SERIAL PRIMARY KEY,
                company_id  INT NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
                name        TEXT NOT NULL,
                code        TEXT NOT NULL,
                budget      INT NOT NULL DEFAULT 0
            );
            CREATE INDEX IF NOT EXISTS ix_departments_company ON departments(company_id);

            CREATE TABLE IF NOT EXISTS employees (
                id              SERIAL PRIMARY KEY,
                department_id   INT NOT NULL REFERENCES departments(id) ON DELETE CASCADE,
                first_name      TEXT NOT NULL,
                last_name       TEXT NOT NULL,
                email           TEXT NOT NULL UNIQUE,
                title           TEXT NOT NULL,
                salary          NUMERIC(12,2) NOT NULL,
                hired_at        TIMESTAMPTZ NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_employees_dept ON employees(department_id);

            CREATE TABLE IF NOT EXISTS addresses (
                id          SERIAL PRIMARY KEY,
                employee_id INT NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
                street      TEXT NOT NULL,
                city        TEXT NOT NULL,
                country     TEXT NOT NULL,
                postal_code TEXT NOT NULL,
                is_primary  BOOLEAN NOT NULL DEFAULT FALSE
            );
            CREATE INDEX IF NOT EXISTS ix_addresses_emp ON addresses(employee_id);

            CREATE TABLE IF NOT EXISTS projects (
                id              SERIAL PRIMARY KEY,
                department_id   INT NOT NULL REFERENCES departments(id) ON DELETE CASCADE,
                name            TEXT NOT NULL,
                status          TEXT NOT NULL,
                start_date      TIMESTAMPTZ NOT NULL,
                end_date        TIMESTAMPTZ NULL
            );
            CREATE INDEX IF NOT EXISTS ix_projects_dept ON projects(department_id);

            CREATE TABLE IF NOT EXISTS tasks (
                id          SERIAL PRIMARY KEY,
                project_id  INT NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
                assignee_id INT NULL REFERENCES employees(id) ON DELETE SET NULL,
                title       TEXT NOT NULL,
                status      TEXT NOT NULL,
                priority    INT NOT NULL DEFAULT 3
            );
            CREATE INDEX IF NOT EXISTS ix_tasks_project ON tasks(project_id);
            CREATE INDEX IF NOT EXISTS ix_tasks_assignee ON tasks(assignee_id);
            """;

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public static async Task DropAllAsync(CancellationToken ct = default)
    {
        await using var conn = await Db.OpenAsync(ct).ConfigureAwait(false);
        const string sql = """
            DROP TABLE IF EXISTS tasks CASCADE;
            DROP TABLE IF EXISTS projects CASCADE;
            DROP TABLE IF EXISTS addresses CASCADE;
            DROP TABLE IF EXISTS employees CASCADE;
            DROP TABLE IF EXISTS departments CASCADE;
            DROP TABLE IF EXISTS companies CASCADE;
            DROP TABLE IF EXISTS organizations CASCADE;
            """;
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }
}
