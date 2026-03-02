# Test Harness

> Write tests next to your functions. Run them in the same sandbox as production.

---

## Core Principle

Tests execute **inside WASM**. The same sandbox, the same resource limits, the same host functions. If a test passes locally, it passes in production. There is no "works in tests but not in prod."

---

## Writing Tests

### Test Placement

```
functions/
  hello.ts               ← function
  hello.test.ts           ← tests (same directory, .test suffix)
  users/
    create.ts
    create.test.ts
    get.ts
    get.test.ts
```

### Test API

```typescript
// functions/hello.test.ts
import { test, expect, describe, beforeEach } from '@ubiq/test';

describe('hello function', () => {
  test('returns greeting with name', () => {
    const result = hello({ name: 'Alice' });
    expect(result.message).toBe('Hello, Alice!');
  });

  test('returns default greeting', () => {
    const result = hello({});
    expect(result.message).toBe('Hello, World!');
  });

  test('handles null input', () => {
    const result = hello(null);
    expect(result.message).toBe('Hello, World!');
  });
});
```

### Assertion Library

Minimal, built-in assertions:

```typescript
expect(value).toBe(expected);              // Strict equality
expect(value).toEqual(expected);           // Deep equality
expect(value).toBeTruthy();
expect(value).toBeFalsy();
expect(value).toBeNull();
expect(value).toContain(item);             // Array/string contains
expect(value).toHaveLength(n);
expect(value).toBeGreaterThan(n);
expect(value).toBeLessThan(n);
expect(value).toMatch(/pattern/);          // Regex match
expect(fn).toThrow();                      // Function throws
expect(fn).toThrow('specific message');
```

### Mocking Host Functions

```typescript
import { test, expect, mock } from '@ubiq/test';

test('create-user saves to KV and emits event', () => {
  // Mock the KV store
  const kvMock = mock.kv({
    get: (key) => null,  // User doesn't exist yet
    set: (key, value) => { /* capture */ }
  });

  // Mock the event system
  const eventsMock = mock.events();

  // Run the function
  const result = createUser({ name: 'Alice', email: 'alice@example.com' });

  // Assert KV was called
  expect(kvMock.set).toHaveBeenCalledWith(
    expect.stringMatching(/^user:/),
    expect.stringContaining('Alice')
  );

  // Assert event was emitted
  expect(eventsMock.emit).toHaveBeenCalledWith('user.created', expect.objectContaining({
    name: 'Alice'
  }));

  // Assert response
  expect(result).toHaveProperty('id');
  expect(result.name).toBe('Alice');
});
```

### Integration Tests

For testing with real host functions (not mocked):

```typescript
import { test, expect, integration } from '@ubiq/test';

integration('KV roundtrip', async () => {
  const kv = integration.kv();  // Real KV (dev backend)

  await kv.set('test:key', 'test:value');
  const value = await kv.get('test:key');
  expect(value).toBe('test:value');

  await kv.delete('test:key');
  const deleted = await kv.get('test:key');
  expect(deleted).toBeNull();
});
```

---

## Running Tests

### All Tests
```bash
ubiq test
```

```
Running tests...

  functions/hello.test.ts
    ✓ returns greeting with name (0.4ms)
    ✓ returns default greeting (0.2ms)
    ✓ handles null input (0.3ms)

  functions/users/create.test.ts
    ✓ create-user saves to KV and emits event (1.2ms)
    ✓ rejects duplicate email (0.8ms)
    ✗ validates email format (0.3ms)
        Expected: toThrow('Invalid email')
        Received: no throw
        at functions/users/create.test.ts:24:18

Tests: 5 passed, 1 failed, 6 total
Time:  0.42s
```

### Specific Function
```bash
ubiq test hello
```

### Watch Mode
```bash
ubiq test --watch
```
Re-runs affected tests when source files change. Only recompiles and re-runs the changed function's tests.

### Verbose Output
```bash
ubiq test --verbose
```
Shows console output from functions during test execution.

### CI/CD Output
```bash
ubiq test --reporter junit --output test-results.xml
```

---

## Test Execution Architecture

```
ubiq test
    │
    ▼
┌─────────────────────────────────────────────┐
│  Test Runner (host)                          │
│                                              │
│  1. Find all .test.ts files                  │
│  2. For each test file:                      │
│     a. Bundle test + function together       │
│     b. Compile to WASM                       │
│     c. Load into sandbox                     │
│     d. Execute test runner inside WASM       │
│     e. Collect results via host function     │
│                                              │
│  3. Report results                           │
└─────────────────────────────────────────────┘
```

### Why Run Tests in WASM?
1. **Identical environment**: Same resource limits, same host functions, same sandbox
2. **Security**: Tests can't access the host system (prevents test pollution)
3. **Portability**: Tests produce the same results on every platform
4. **Performance isolation**: A slow test can't affect other tests (separate instances)

---

## Current Implementation

The existing test harness in `test-harness/` demonstrates this architecture:
- TypeScript tests compiled to WASM via esbuild + extism-js
- Tests call host functions (`debug`, `ubiqDispatch`)
- Results communicated back via host function IPC
- Binary header protocol for structured communication

### Existing Test Methods
From the current test harness:
- `doNothing` — void function, no I/O
- `returnHelloWorld` — returns a static string
- `strlen` — string input → number output
- `max` — CSV numbers → max value
- `intArrayStatsJSON` — JSON array → stats object
- `callStaticHostMethod` — exercises host function calls
- `echoToStdout` — tests console output capture
- `setVar` / `getVar` — tests WASM variable persistence

This existing harness validates the approach. The next step is to formalize the test API and make it user-friendly.

---

## Performance Targets

| Metric | Target |
|--------|--------|
| Test compilation | < 500ms per test file |
| Test execution | < 10ms per simple test |
| Full suite (100 tests) | < 5 seconds |
| Watch mode re-run | < 1 second from save to results |
