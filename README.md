# Channel Pipeline Architecture

**.NET 10** | Central Package Management | Server GC (throughput-optimized) | Channel Pipeline + PostgreSQL API

## Stack

| Layer | Choice |
|-------|--------|
| Runtime | **.NET 10** (`net10.0`) |
| Package mgmt | **Central Package Management** (`Directory.Packages.props`) |
| GC | **Server GC** + Concurrent + DATAS + RetainVM |
| API | Minimal APIs, categorized `MapGroup`, OpenAPI |
| Data | Npgsql + nested relational seed |

## Connection String

`src/Api/appsettings.json`:

```json
"ConnectionStrings": {
  "Default": "Host=localhost;Port=5432;Database=channeldb;Username=channelapp;Password=channelapp"
}
```

Override via env: `ConnectionStrings__Default=...`

## GC Tuning (Directory.Build.props)

```xml
<ServerGarbageCollection>true</ServerGarbageCollection>
<ConcurrentGarbageCollection>true</ConcurrentGarbageCollection>
<GarbageCollectionAdaptationMode>1</GarbageCollectionAdaptationMode>
<RetainVMGarbageCollection>true</RetainVMGarbageCollection>
```

## oha Stress Test (500 concurrent, 60s)

```
Success rate:   100%
Requests/sec:   ~5,019
Average:        0.10 s
p50:            0.09 s
p99:            0.26 s
Total:          ~300,787 requests
```

## Run

```bash
dotnet run --project src/Api
curl -X POST http://localhost:5080/api/seed
curl http://localhost:5080/openapi/v1.json
oha -c 500 -z 60s --no-tui "http://localhost:5080/api/employees?take=20"
```

## License

MIT
