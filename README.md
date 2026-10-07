# Channel Pipeline Architecture

Lightweight high-throughput data processing + **AOT-ready Minimal API** with PostgreSQL.

## Projects

| Project | Description |
|---------|-------------|
| `src/Host` | Original Channel Pipeline demo |
| `src/Pipelines` | Channel-based pipeline stages |
| `src/Api` | **PostgreSQL API** – Native AOT oriented, categorized endpoints, Swagger |

## API (`src/Api`)

### Features
- **Native AOT ready** (`PublishAot=true`)
- Clean **categorized endpoints** via `MapGroup`
- Nested relationships: Organization → Company → Department → Employee → Address / Project → Task
- Mock data seeder (thousands of rows)
- Swagger UI at `/swagger`

### Endpoints

| Group | Path | Description |
|-------|------|-------------|
| Health | `GET /api/health` | Liveness |
| Health | `GET /api/health/db` | DB connectivity |
| Seed | `POST /api/seed` | Seed ~3.6k employees + nested data |
| Seed | `POST /api/seed/large` | Larger seed |
| Organizations | `GET /api/organizations` | List |
| Organizations | `GET /api/organizations/{id}` | Detail + companies |
| Companies | `GET /api/companies` | List (optional `?organizationId=`) |
| Companies | `GET /api/companies/{id}` | Detail + departments |
| Departments | `GET /api/departments/{id}` | Detail + employees + projects |
| Employees | `GET /api/employees` | List (paging + filter) |
| Employees | `GET /api/employees/{id}` | Detail + addresses |

### Run

```bash
# Ensure PostgreSQL is running and connection string is set
dotnet run --project src/Api

# Seed
curl -X POST http://localhost:5080/api/seed

# Swagger
open http://localhost:5080/swagger
```

### Stress test (oha)

```bash
oha -c 500 -z 60s --no-tui "http://localhost:5080/api/employees?take=20"
```

### AOT Publish

```bash
dotnet publish src/Api -c Release -r linux-x64
```

> Note: Full Native AOT + Swashbuckle has limitations. For pure AOT production builds,
> exclude Swagger or switch to Microsoft.AspNetCore.OpenApi + Scalar only.

## License

MIT
