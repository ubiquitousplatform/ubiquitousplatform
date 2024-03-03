# WebAssembly Integration Test Project

This project contains a test harness for an Extism-compatible WebAssembly module that exposes multiple test methods.

Each test method is usable in isolation.  The idea behind this repo is to allow a host to implement unit tests that measure coverage of functionality exposed within the host. They are in this context referred to as integration tests since they test the integration between the plugin host and the plugin sdk.

This test codebase is written in js, but that is irrelevant to the executing wasm engine.  the code will function the same in regard to the host regardless.

In order to import and use the wasm module, you will have to expose the following host functions:
```js

// ... all the functions exposed from Extism for logging, vars, http, etc. Provided natively by Extism host function

// A function that takes only a single memory pointer as a parameter.
// Inside that function, it must implement the Ubiquitous Host Invoke Dispatcher in order to pass the test.
Ubiquitous.HostInvoke(I64)

// Sends a message to the host with a string as the parameter.
HelloHostString(string)

// Sends a message to the host with a JSON string as the parameter ({someInt: 1, message: "%message%"})
HelloHostJSON(string)
HelloHostWebpack(byte[])
```

## Test Methods

| Function Alias                | Description                                                                                                                                    |
| ----------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| doNothing                     | No params, no return value, just a sample call                                                                                                 |
| no-params_string-return       | No params, returns a hardcoded `Hello, World!` string.                                                                                         |
| strlen                        | Takes in a string and returns the length of the string                                                                                         |
| max                           | Takes in an array of integers and returns the maximum value                                                                                    |
| min                           | Takes in an array of integers and returns the minimum value                                                                                    |
| callStaticHostMethod(message) | Calls the host method "HelloHost" with the string `Hello from WASM! Message: {message}` if a message is supplied, otherwise `Hello from WASM!` |
| callJSONHostMethod            |                                                                                                                                                |
| createHostError(message)      | Throws a host error with the specified message                                                                                                 |
|                               |                                                                                                                                                |






### Void Function with No Parameters

#### Entrypoint
`generic-void_function-no_params`

#### Description

### Void Function with Parameters

### Function with a string return value

// JavaScript
const instance = await WebAssembly.instantiate(wasmModule, {});
let result = instance.exports.plainFunctionWithReturn();
console.log(result);

### 
### 3. Call a function that calls a host function
// JavaScript
const instance = await WebAssembly.instantiate(wasmModule, {});
let result = instance.exports.plainFunctionWithReturn();
console.log(result);
### 4. Call a function that calls a host function multiple times
// JavaScript
const importObject = {
    env: {
        hostFunction: () => console.log('Host function called')
    }
};
const instance = await WebAssembly.instantiate(wasmModule, importObject);
instance.exports.functionWithMultipleHostCalls();
### 5. Call a function that calls multiple host functions multiple times
// JavaScript
const importObject = {
    env: {
        hostFunction1: () => console.log('Host function 1 called'),
        hostFunction2: () => console.log('Host function 2 called')
    }
};
const instance = await WebAssembly.instantiate(wasmModule, importObject);
instance.exports.functionWithMultipleHostCalls();
### 6. Call a function that uses Extism Vars
// JavaScript
const instance = await WebAssembly.instantiate(wasmModule, {});
instance.exports.functionWithExtismVars();
### 7. Call a function that uses Extism Logs
// JavaScript
const instance = await WebAssembly.instantiate(wasmModule, {});
instance.exports.functionWithExtismLogs();
### 8. Call a function that uses Ubiquitous Vars
// JavaScript
const instance = await WebAssembly.instantiate(wasmModule, {});
instance.exports.functionWithUbiquitousVars();
### 9. Call a function that uses Ubiquitous Logs
// JavaScript
const instance = await WebAssembly.instantiate(wasmModule, {});
instance.exports.functionWithUbiquitousLogs();

Please note that the actual implementation of the functions in the WebAssembly module and the host functions in the JavaScript environment are not shown in this README.