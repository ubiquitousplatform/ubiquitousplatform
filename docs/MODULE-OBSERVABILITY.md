# Observability & Telemetry

> Built-in tracing, metrics, and logging for every function invocation.

---

## Design Philosophy

Observability should be **zero-config and always-on**. Every function invocation automatically produces traces, metrics, and logs. In development, these are displayed in a local dashboard. In production, they're exported via OTLP.

---

## Automatic Instrumentation

Every function invocation generates the following **without any user code**:

### Trace Spans

```
HTTP Request (root span)
├── Router (match function)
├── Middleware Pipeline
│   ├── CORS middleware
│   ├── Auth middleware
│   └── Rate limit middleware
├── Pool Checkout
├── WASM Execution
│   ├── Function: create-user (user code)
│   ├── Host: kv.set (host function call)
│   ├── Host: kv.set
│   └── Host: events.emit
├── Pool Checkin
└── Response Serialization
```

### Metrics (per function)

| Metric | Type | Description |
|--------|------|-------------|
| `ubiq.invocation.count` | Counter | Total invocations |
| `ubiq.invocation.duration` | Histogram | Execution time (ms) |
| `ubiq.invocation.error_count` | Counter | Failed invocations |
| `ubiq.pool.size` | Gauge | Current pool size |
| `ubiq.pool.checkout_wait` | Histogram | Time waiting for instance |
| `ubiq.memory.usage` | Gauge | WASM memory usage per instance |
| `ubiq.host_fn.duration` | Histogram | Host function call latency |
| `ubiq.kv.operations` | Counter | KV read/write counts |
| `ubiq.http_out.duration` | Histogram | Outbound HTTP latency |

### Log Records

Every `log.info()`, `console.log()`, etc. from user code becomes a structured OTLP log record with:
- Timestamp
- Function name
- Invocation ID
- Log level
- Message
- Structured attributes

---

## Development Dashboard

### Access
Automatically available during `ubiq dev` at `http://localhost:PORT/_dashboard`.

### Features

#### Live Invocation Feed
Real-time stream of all function invocations:
```
10:30:01  GET /hello          200  1.2ms   hello
10:30:02  POST /users         201  4.5ms   create-user
10:30:03  GET /users          200  2.1ms   get-users
10:30:03  POST /users         400  0.8ms   create-user  ← validation error
```

#### Trace Detail View
Click any invocation to see the full trace:
- Waterfall visualization of spans
- Timing breakdown (routing, middleware, execution, host functions)
- Request/response bodies
- Log entries within the invocation
- Resource usage (memory, CPU time)

#### Function Overview
Per-function stats:
- Invocation count, error rate, p50/p95/p99 latency
- Recent errors with stack traces
- Pool utilization and scaling events
- Source file and last compilation time

#### Resource Monitor
- Memory usage per function instance
- Pool sizes and checkout queue depth
- KV store usage
- Storage usage

### Implementation Options

| Runtime | Dashboard Technology |
|---------|---------------------|
| C# / .NET | .NET Aspire Dashboard (built-in OTLP UI) |
| Rust | Custom lightweight web UI (htmx + embedded assets) |
| Go | Custom lightweight web UI |

The Aspire dashboard is a strong argument for keeping C# in the stack, at least for the dev experience layer. It provides distributed tracing, structured logs, metrics, and resource monitoring out of the box.

---

## Production Telemetry

### OTLP Export Configuration

```toml
[telemetry]
enabled = true
endpoint = "http://otel-collector:4317"   # gRPC endpoint
protocol = "grpc"                          # or "http/protobuf"
headers = { "Authorization" = "Bearer ${OTEL_TOKEN}" }

[telemetry.resource]
service_name = "myapp"
environment = "production"

[telemetry.sampling]
strategy = "probabilistic"
rate = 0.1                                 # Sample 10% of traces
```

### Compatible Backends
- **Jaeger** — open source distributed tracing
- **Grafana Tempo** — trace backend for Grafana stack
- **Grafana Loki** — log aggregation
- **Prometheus** — metrics collection
- **Datadog** — commercial APM
- **New Relic** — commercial APM
- **Honeycomb** — observability platform
- **AWS X-Ray** — AWS tracing
- **Azure Monitor** — Azure monitoring

### Custom Attributes

Functions can add custom span attributes:

```typescript
import { trace } from '@ubiq/sdk';

export default function(input) {
  trace.setAttribute('user.id', input.userId);
  trace.setAttribute('user.plan', input.plan);

  // Business logic...
}
```

These attributes are searchable in your tracing backend.

---

## Error Tracking

### Automatic Error Capture
- Unhandled exceptions in functions are captured as span errors
- Stack traces included (with source maps for JS/TS)
- Error categorization:
  - `user_error` — function returned an error response
  - `timeout` — execution exceeded time limit
  - `oom` — out of memory
  - `permission_denied` — function called unauthorized host function
  - `runtime_error` — WASM execution failure

### Source Maps
For JavaScript/TypeScript, source maps are generated during compilation and used to map WASM stack traces back to original source lines.

```
Error in create-user.ts:
  TypeError: Cannot read property 'email' of undefined
    at createUser (functions/create-user.ts:15:22)    ← mapped from WASM offset
    at handler (functions/create-user.ts:8:10)
```

---

## Health Checks

### Built-in Endpoints

| Endpoint | Description |
|----------|-------------|
| `/_health` | Basic health check (200 OK) |
| `/_health/ready` | Readiness probe (pool warmed, services connected) |
| `/_health/live` | Liveness probe (process alive) |
| `/_metrics` | Prometheus-format metrics |

These are available in both dev and production, and are used by the cluster for node health monitoring.
