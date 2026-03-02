# Plugin System

> Extend Ubiquitous with community-built or first-party capabilities.

---

## Overview

Plugins are **WASM modules** that extend the platform. Because they run in the same sandbox as user functions, they inherit all security guarantees — a plugin cannot access anything it doesn't explicitly declare.

---

## Plugin Types

### Middleware Plugins
Intercept requests and responses in the pipeline.

```
Request → [CORS] → [Auth] → [RateLimit] → Function → [RateLimit] → [Auth] → [CORS] → Response
```

Examples: CORS, authentication, rate limiting, request validation, response caching.

### Service Plugins
Provide new host functions that user functions can call.

Examples: email sending, SMS, payment processing, AI inference, database drivers.

### Trigger Plugins
Provide new event sources that can invoke functions.

Examples: webhook receivers, queue consumers, file system watchers, database change streams.

---

## Using a Plugin

### Install
```bash
ubiq plugin add @ubiq/cors
ubiq plugin add @ubiq/auth
ubiq plugin add community/slack-notifications
```

### Configure in `ubiq.toml`

```toml
[middleware]
global = ["@ubiq/cors", "@ubiq/auth"]

[middleware.config."@ubiq/cors"]
origins = ["https://myapp.com", "https://staging.myapp.com"]
methods = ["GET", "POST", "PUT", "DELETE"]

[middleware.config."@ubiq/auth"]
provider = "jwt"
secret = "${JWT_SECRET}"
exclude = ["/health", "/public/*"]
```

### Use Service Plugin in a Function

```typescript
import { email } from '@ubiq/email';  // Provided by the email plugin

export async function POST(input) {
  await email.send({
    to: input.email,
    subject: 'Welcome!',
    body: 'Thanks for signing up.'
  });
  return { sent: true };
}
```

---

## Creating a Plugin

### Scaffold
```bash
ubiq new plugin my-plugin
```

```
my-plugin/
├── ubiq-plugin.toml     # Plugin manifest
├── src/
│   └── index.ts         # Plugin entry point
├── tests/
│   └── index.test.ts
└── README.md
```

### Plugin Manifest: `ubiq-plugin.toml`

```toml
[plugin]
name = "my-auth-plugin"
version = "1.0.0"
description = "JWT authentication middleware"
type = "middleware"          # "middleware", "service", or "trigger"
author = "Luke P"
license = "MIT"
repository = "https://github.com/lukep/ubiq-auth"

# What this plugin needs access to
[permissions.required]
http_outbound = ["auth0.com/oauth/*"]  # JWT validation endpoint
kv_read = ["auth:*"]                    # Cache validated tokens
kv_write = ["auth:*"]

# What this plugin provides
[provides]
middleware = ["auth"]
host_functions = ["auth.getUser", "auth.requireRole"]

# Configuration schema
[config]
provider = { type = "string", required = true, description = "Auth provider (jwt, oauth2, apikey)" }
secret = { type = "string", required = true, secret = true, description = "JWT signing secret" }
exclude = { type = "array", default = [], description = "Routes to skip auth" }
```

### Middleware Plugin Example

```typescript
// src/index.ts
import { MiddlewarePlugin, Request, Response, log } from '@ubiq/plugin-sdk';

export default class AuthMiddleware implements MiddlewarePlugin {
  private config: { provider: string; secret: string; exclude: string[] };

  init(config: any) {
    this.config = config;
  }

  async onRequest(req: Request): Promise<Request | Response> {
    // Skip excluded routes
    if (this.config.exclude.some(pattern => req.path.match(pattern))) {
      return req; // Pass through
    }

    const token = req.headers['authorization']?.replace('Bearer ', '');
    if (!token) {
      return { status: 401, body: { error: 'Missing authorization token' } };
    }

    try {
      const user = verifyJwt(token, this.config.secret);
      req.context.user = user;
      return req; // Continue to next middleware/function
    } catch (err) {
      log.warn('Auth failed', { error: err.message });
      return { status: 401, body: { error: 'Invalid token' } };
    }
  }

  async onResponse(res: Response): Promise<Response> {
    return res; // Pass through
  }
}
```

---

## Permission Audit System

### The Problem
Third-party code can be a security risk. Even in a sandbox, plugins with overly broad permissions can exfiltrate data.

### The Solution
Every plugin declares its permissions. The runtime enforces them. The community can audit them.

### Audit Report

```bash
ubiq plugin audit community/slack-notifications
```

```
Plugin: community/slack-notifications v2.1.0

Declared Permissions:
  ✓ http:post:hooks.slack.com/services/*
  ✓ config:read:SLACK_WEBHOOK_URL

Actual Usage (static analysis + runtime profiling):
  ✓ http:post:hooks.slack.com/services/T00000000/B00000000/*
  ✗ config:read:DATABASE_URL  ← NOT DECLARED (would be blocked)

Security Score: 95/100
  - All HTTP calls go to declared domains ✓
  - No filesystem access ✓
  - No undeclared KV access ✓
  - Minimal permission surface ✓
  - Warning: Sends user-provided data to external URL (-5)
```

### Permission Enforcement

| Permission | Declared | Runtime Behavior |
|-----------|----------|-----------------|
| `http:post:hooks.slack.com/*` | Yes | Allowed |
| `http:get:evil.com/exfil` | No | **Blocked + logged** |
| `kv:read:auth:*` | Yes | Allowed |
| `kv:read:user:*` | No | **Blocked + logged** |

---

## Plugin Registry

### Publishing

```bash
ubiq plugin publish
```

Publishes to the Ubiquitous Plugin Registry (like npm for plugins).

### Registry Features
- Verified publishers (checkmark for official and audited plugins)
- Security audit scores displayed
- Permission summary visible before install
- Download counts and community ratings
- Source code link required (transparency)

### Discovery

```bash
ubiq plugin search auth
```

```
Results for "auth":
  @ubiq/auth          ★★★★★  Official JWT/OAuth2 auth middleware     12.4K installs
  community/passkey   ★★★★☆  WebAuthn/Passkey authentication         3.2K installs
  community/magic-link ★★★★☆  Email magic link auth                  1.8K installs
```

---

## First-Party Plugins (Official)

| Plugin | Type | Description |
|--------|------|-------------|
| `@ubiq/cors` | Middleware | CORS headers configuration |
| `@ubiq/auth` | Middleware + Service | JWT, OAuth2, API key authentication |
| `@ubiq/rate-limit` | Middleware | Token bucket rate limiting |
| `@ubiq/cache` | Middleware | Response caching with TTL |
| `@ubiq/validate` | Middleware | Request schema validation |
| `@ubiq/email` | Service | Send transactional email (Resend, SendGrid, SES) |
| `@ubiq/queue` | Service + Trigger | Background job queue |
| `@ubiq/cron` | Trigger | Cron scheduling (built-in, exposed as plugin) |
| `@ubiq/webhook` | Trigger | Webhook receiver with signature verification |
