#!/bin/bash
echo "building rust project"
# build the Rust project
cd rust-example
cargo build --release --target wasm32-unknown-unknown
cd ..

# copy the compiled wasm file to the C# project output directory
echo "deploying file to current directory"
cp rust-example/target/wasm32-unknown-unknown/release/ubiq_wasm.wasm .
