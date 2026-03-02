# Release & Deployment System

> Atomic deployments with distributed consensus.

---

## Release Lifecycle

```
ubiq build --release
    │
    ▼
┌─────────────────┐
│  Release Bundle  │  ← Compiled WASM + manifest + checksums
│  myapp-v1.2.0   │
└────────┬────────┘
         │
    ubiq deploy
         │
         ▼
┌─────────────────────────────────────────────────┐
│              2-Phase Commit Protocol              │
│                                                   │
│  Phase 1: PREPARE                                 │
│    ┌──────┐  ┌──────┐  ┌──────┐                  │
│    │Node 1│  │Node 2│  │Node 3│  ← Download +    │
│    │  ✓   │  │  ✓   │  │  ✓   │    Validate      │
│    └──────┘  └──────┘  └──────┘                  │
│                                                   │
│  Phase 2: COMMIT                                  │
│    ┌──────┐  ┌──────┐  ┌──────┐                  │
│    │Node 1│  │Node 2│  │Node 3│  ← Atomic switch │
│    │ v1.2 │  │ v1.2 │  │ v1.2 │                  │
│    └──────┘  └──────┘  └──────┘                  │
└─────────────────────────────────────────────────┘
```

---

## Release Bundles

### What's in a Bundle

```
myapp-v1.2.0.ubiq
├── manifest.json          # Bundle metadata
├── functions/
│   ├── hello.wasm         # Compiled WASM modules
│   ├── get-users.wasm
│   └── create-user.wasm
├── plugins/
│   └── auth.wasm
├── config/
│   └── ubiq.toml          # Frozen project config
└── checksums.sha256
```

### `manifest.json`

```json
{
  "name": "myapp",
  "version": "1.2.0",
  "created_at": "2026-02-22T10:30:00Z",
  "build_hash": "sha256:a1b2c3d4...",
  "functions": [
    {
      "name": "hello",
      "hash": "sha256:e5f6a7b8...",
      "size_bytes": 15234,
      "permissions": ["kv:read"]
    },
    {
      "name": "get-users",
      "hash": "sha256:c9d0e1f2...",
      "size_bytes": 18456,
      "permissions": ["kv:read", "kv:write"]
    }
  ],
  "plugins": [
    {
      "name": "@ubiq/auth",
      "version": "1.0.0",
      "hash": "sha256:1a2b3c4d..."
    }
  ],
  "runtime_version": "1.0",
  "previous_version": "1.1.0"
}
```

### Bundle Storage
- Content-addressed (hash-based) for deduplication
- Immutable once created
- Stored locally and on deployment targets
- Old bundles retained for rollback (configurable retention)

---

## Deployment Strategies

### Blue-Green (Default)

```
Before:  [v1.1] ← all traffic
After:   [v1.2] ← all traffic  |  [v1.1] ← standby (for rollback)
```

- New version deployed alongside old
- Traffic switched atomically
- Instant rollback: switch back to old version

### Canary

```bash
ubiq deploy --canary 10    # 10% traffic to v1.2
ubiq deploy --canary 50    # 50% traffic to v1.2
ubiq deploy --promote      # 100% traffic to v1.2
```

- Gradual traffic shift
- Monitor error rates between steps
- Auto-rollback if error rate exceeds threshold

### Rolling

```
t0:  [v1.1] [v1.1] [v1.1]
t1:  [v1.2] [v1.1] [v1.1]
t2:  [v1.2] [v1.2] [v1.1]
t3:  [v1.2] [v1.2] [v1.2]
```

- Update one node at a time
- Healthcheck between each node
- Rollback all if any node fails healthcheck

---

## 2-Phase Commit Protocol

### Why 2PC?
In a distributed system, you can't just "deploy" to multiple nodes independently. If node 1 succeeds and node 2 fails, you have an inconsistent state. 2PC guarantees **atomicity** — either all nodes switch or none do.

### Phase 1: Prepare

1. Coordinator (CLI or leader node) sends `PREPARE(bundle)` to all nodes
2. Each node:
   - Downloads the bundle (if not already cached)
   - Validates checksums
   - Compiles/loads WASM modules (pre-warm)
   - Runs health checks on the new version
   - Responds `PREPARED` or `FAILED`
3. If all nodes respond `PREPARED` → proceed to Phase 2
4. If any node responds `FAILED` → send `ABORT` to all nodes

### Phase 2: Commit

1. Coordinator sends `COMMIT(version)` to all nodes
2. Each node atomically switches traffic to the new version
3. Each node responds `COMMITTED`
4. If any node fails to commit → `ROLLBACK` to previous version on all nodes

### Failure Handling

| Scenario | Action |
|----------|--------|
| Node unreachable during Prepare | Abort deployment |
| Node fails validation in Prepare | Abort deployment |
| Node unreachable during Commit | Retry with timeout, then rollback all |
| Node fails to switch in Commit | Rollback all nodes |
| Coordinator crashes during Prepare | Nodes timeout and release prepared bundle |
| Coordinator crashes during Commit | Recovery log replays commit on restart |

### Timeout Protocol
- Prepare phase: 60 second timeout per node
- Commit phase: 10 second timeout per node
- Recovery: coordinator writes intent log before each phase

---

## Runtime Cluster

### Node Architecture

```
┌──────────────────────────────────────────────┐
│  Ubiquitous Node                              │
│                                               │
│  ┌───────────┐  ┌────────────┐  ┌─────────┐  │
│  │  Runtime   │  │  Release   │  │  Raft   │  │
│  │  Engine    │  │  Manager   │  │  Agent  │  │
│  │            │  │            │  │         │  │
│  │  Function  │  │  Bundle    │  │  Leader │  │
│  │  Pool      │  │  Storage   │  │  Election│ │
│  │            │  │            │  │         │  │
│  │  Host Fns  │  │  Traffic   │  │  Health │  │
│  │            │  │  Router    │  │  Check  │  │
│  └───────────┘  └────────────┘  └─────────┘  │
│                                               │
│  ┌───────────────────────────────────────┐    │
│  │  Telemetry (OTLP export)              │    │
│  └───────────────────────────────────────┘    │
└──────────────────────────────────────────────┘
```

### Cluster Formation
- Nodes discover each other via:
  - Static configuration in `ubiq.toml`
  - DNS-based discovery
  - Multicast/mDNS for local networks
- Raft consensus for leader election
- Leader coordinates deployments and scheduling

### No Kubernetes Required
The cluster is **self-contained**. No external orchestrator needed:
- Binary includes everything
- Gossip protocol for membership
- Leader election for coordination
- Health checking built in
- Auto-redistribution on node failure

---

## Rollback

### Manual Rollback

```bash
ubiq rollback                 # Roll back to previous version
ubiq rollback --to v1.0.0     # Roll back to specific version
```

### Automatic Rollback
Configure auto-rollback triggers:

```toml
[deploy.production]
auto_rollback = true
error_rate_threshold = 5.0   # Roll back if error rate exceeds 5%
window_seconds = 60           # Measured over 60-second window
```

### Rollback Process
1. Same 2-phase commit protocol (Prepare → Commit)
2. Phase 1: nodes load the rollback version (usually cached from previous deployment)
3. Phase 2: atomic traffic switch
4. Typically <10 seconds for a rollback

---

## Local ↔ Production Parity

| Aspect | Local (`ubiq dev`) | Production (`ubiq deploy`) |
|--------|-------------------|---------------------------|
| WASM Engine | Same | Same |
| Host Functions | Same API | Same API |
| Resource Limits | Same (configurable) | Same (enforced) |
| Sandbox | Same | Same |
| KV Backend | SQLite | Redis/DynamoDB |
| Storage Backend | Filesystem | S3 |
| Event Backend | In-process | NATS/Kafka |
| Deployment | Instant (file watch) | 2-phase commit |
| Telemetry | Dashboard | OTLP export |

The function code is byte-for-byte identical. Only the backing services change.
