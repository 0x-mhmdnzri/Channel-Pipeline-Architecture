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

## GC Tuning (Directory.Build.props)

```xml
<ServerGarbageCollection>true</ServerGarbageCollection>
<ConcurrentGarbageCollection>true</ConcurrentGarbageCollection>
<GarbageCollectionAdaptationMode>1</GarbageCollectionAdaptationMode> <!-- DATAS -->
<RetainVMGarbageCollection>true</RetainVMGarbageCollection>
```

Applied to **all** projects via `Directory.Build.props` so there is no per-project drift.

## oha Stress Test (500 concurrent, 60s)

```
Success rate:   100%
Requests/sec:   ~5,019
Average:        0.10 s
p50:            0.09 s
p99:            0.26 s
Total:          ~300,787 requests
```

(Previously ~529 RPS on .NET 8 without Server GC tuning — ~10× improvement.)

## Run

```bash
dotnet run --project src/Api
curl -X POST http://localhost:5080/api/seed
curl http://localhost:5080/openapi/v1.json   # OpenAPI document
oha -c 500 -z 60s --no-tui "http://localhost:5080/api/employees?take=20"
```

## Projects

- `src/Api` — PostgreSQL Minimal API
- `src/Host` / `src/Pipelines` — Channel pipeline demo
- `Directory.Build.props` — TFM + GC + CPM switch
- `Directory.Packages.props` — all package versions

## License

MIT
