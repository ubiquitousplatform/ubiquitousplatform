# CLI Module — `ubiq`

> The single entry point for all developer interactions with Ubiquitous.

---

## Philosophy

One tool, zero dependencies, every workflow. Inspired by Rails' `rails`, Laravel's `artisan`, and Phoenix's `mix phx`.

---

## Installation

```bash
# macOS / Linux
curl -fsSL https://install.ubiq.dev | sh

# Homebrew
brew install ubiquitous/tap/ubiq

# Windows
winget install ubiquitous.ubiq

# From source
cargo install ubiq-cli
```

After installation, `ubiq` is a single static binary. No runtime, no JVM, no Node.js.

---

## Commands

### Project Management

| Command | Description |
|---------|-------------|
| `ubiq new <name>` | Create a new project with starter template |
| `ubiq new function <name>` | Scaffold a new function + test file |
| `ubiq new plugin <name>` | Scaffold a new plugin |
| `ubiq init` | Initialize Ubiquitous in an existing directory |

#### `ubiq new myapp`

```
Creating project "myapp"...
  ✓ myapp/ubiq.toml
  ✓ myapp/functions/hello.ts
  ✓ myapp/functions/hello.test.ts
  ✓ myapp/.gitignore

Done! Run:
  cd myapp
  ubiq dev
```

---

### Development

| Command | Description |
|---------|-------------|
| `ubiq dev` | Start dev server with file watcher and hot reload |
| `ubiq watch` | Alias for `ubiq dev` |
| `ubiq run <function> [input]` | Invoke a single function with optional input |
| `ubiq build` | Compile all functions to WASM |
| `ubiq test` | Run all tests |
| `ubiq test <function>` | Run tests for a specific function |
| `ubiq test --watch` | Re-run tests on file changes |
| `ubiq dashboard` | Open the telemetry dashboard |

#### `ubiq dev`

```
Ubiquitous v0.1.0 — Dev Server

  Compiling functions...
  ✓ hello.ts → hello.wasm (147ms)
  ✓ get-users.ts → get-users.wasm (152ms)

  Listening on http://localhost:3000
  Dashboard at http://localhost:3000/_dashboard

  Watching for changes...
```

On file save:
```
  ↻ hello.ts changed
  ✓ hello.ts → hello.wasm (89ms, hot-swapped)
```

---

### Deployment

| Command | Description |
|---------|-------------|
| `ubiq deploy` | Build release bundle and deploy to production |
| `ubiq deploy --staging` | Deploy to staging environment |
| `ubiq deploy --canary 10` | Canary deploy to 10% of traffic |
| `ubiq rollback` | Roll back to previous release |
| `ubiq releases` | List recent releases |
| `ubiq status` | Show current deployment status |

#### `ubiq deploy`

```
Building release bundle...
  ✓ hello.ts → hello.wasm (892ms, optimized)
  ✓ get-users.ts → get-users.wasm (901ms, optimized)

Bundle: myapp-v1.2.0 (sha256: a1b2c3...)
  2 functions | 48KB total | 0 permission changes

Deploying to production (3 nodes)...
  Phase 1: Prepare
    ✓ node-us-east-1: bundle received, validated
    ✓ node-eu-west-1: bundle received, validated
    ✓ node-ap-south-1: bundle received, validated
  Phase 2: Commit
    ✓ All nodes switched to v1.2.0

Deployed successfully in 4.2s
```

---

### Utilities

| Command | Description |
|---------|-------------|
| `ubiq info` | Show project info (functions, versions, sizes) |
| `ubiq doctor` | Diagnose environment issues |
| `ubiq upgrade` | Self-update to latest version |
| `ubiq config` | View/edit CLI configuration |
| `ubiq logs` | Tail production logs |
| `ubiq login` | Authenticate with Ubiquitous Cloud (future) |

---

## Global Flags

| Flag | Description |
|------|-------------|
| `--verbose` / `-v` | Verbose output |
| `--quiet` / `-q` | Suppress output |
| `--dir <path>` | Working directory (default: `.`) |
| `--config <path>` | Config file (default: `ubiq.toml`) |
| `--no-color` | Disable colored output |
| `--json` | JSON output (for scripting) |

---

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Success |
| `1` | General error |
| `2` | Compilation error |
| `3` | Test failure |
| `4` | Deployment failure |
| `5` | Configuration error |

---

## Implementation Notes

### Language Choice
The CLI should be written in the same language as the runtime (Rust or Go) to share the compilation and execution engine. A single binary that includes both CLI and runtime avoids any version mismatch issues.

### Bundled Compilers
The CLI bundles or downloads-on-first-use all required compilers:
- **Javy** or **extism-js** for JS/TS → WASM
- **esbuild** for TS → JS bundling
- Architecture-specific binaries cached in `~/.ubiq/toolchain/`

### Shell Completions
`ubiq completion bash/zsh/fish` generates shell completions.

### Config File: `~/.ubiq/config.toml`
```toml
[defaults]
port = 3000

[telemetry]
enabled = true

[cloud]
token = "ubq_..."
default_target = "production"
```
