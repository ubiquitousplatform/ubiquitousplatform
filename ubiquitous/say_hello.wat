(module
  (import "ubiquitous" "host" (func $host (param i32)))
  (func (export "run") 
  i32.const 42
  call $host)
)