# Channel Pipeline Architecture

A lightweight, high-throughput data processing architecture for .NET using `System.Threading.Channels`.

Designed for scenarios with **100M+ records** or continuous streaming, running in a **single process with multiple workers**.

No Clean Architecture, no Onion, no DDD, no CQRS ceremony.

## Core Idea

```
Ingestion → Channel → Transform Stage (N workers) → Channel → Enrich Stage → Channel → Persist Stage
```

Each stage is a simple class that reads from an input channel and writes to an output channel.

## Project Structure

```
src/
├── Host/                     # Program.cs + DI + Hosting
├── Pipelines/                # All processing logic lives here
│   ├── Ingestion/
│   ├── Stages/
│   ├── PipelineBuilder.cs
│   └── Pipeline.cs
├── Models/                  # Simple records / DTOs only
├── Infrastructure/          # Real I/O (Kafka, files, DB...)
└── Observability/           # Metrics, logging, health
```

## Quick Start

```bash
dotnet run --project src/Host
```

## Key Design Decisions

- **Bounded Channels** for natural backpressure
- **Configurable degree of parallelism** per stage
- **Zero unnecessary abstractions**
- **ArrayPool + Span** friendly (hot path ready)
- Easy to test each stage in isolation

## License

MIT
