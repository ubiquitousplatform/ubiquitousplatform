#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
WASM_DIR="$SCRIPT_DIR/wasm"

echo "=== Building guest WASM module ==="
mkdir -p "$WASM_DIR"

# macOS: work around Xcode license issue by pointing to CommandLineTools SDK
export SDKROOT="${SDKROOT:-/Library/Developer/CommandLineTools/SDKs/MacOSX.sdk}"
export DEVELOPER_DIR="${DEVELOPER_DIR:-/Library/Developer/CommandLineTools}"

cd "$SCRIPT_DIR/guest-rust"
cargo build --release

cp target/wasm32-unknown-unknown/release/bench_guest.wasm "$WASM_DIR/bench_guest.wasm"
echo "✓ Built: $WASM_DIR/bench_guest.wasm ($(du -h "$WASM_DIR/bench_guest.wasm" | cut -f1))"
