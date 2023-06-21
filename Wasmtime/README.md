
wasi-data-sharing is built from https://petermalmgren.com/serverside-wasm-data/ and the example branch: https://github.com/pmalmgren/wasi-data-sharing/tree/shared-linear-memory-demo

It expects the host to publish the following methods with signatures, under the module "host":

```rust
    fn get_input_size() -> i32;
    fn get_input(ptr: i32);
    fn set_output(ptr: i32, size: i32);
```

