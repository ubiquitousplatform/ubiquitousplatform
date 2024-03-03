npm run build
echo "Testing all functions"
echo "Testing strlen. expected output: 11"
extism call dist/test-harness.wasm strlen --input="test string" --wasi #--log-level trace
echo "\nTesting get_closest. expected output: fastest world"
extism call dist/test-harness.wasm get_closest --input="fastett" --wasi
echo "\nTesting randomstr. expected output: 56 character long random string"
extism call dist/test-harness.wasm randomstr --input="56" --wasi
echo "\nTesting returnHelloWorld. expected output: Hello, World!"
extism call dist/test-harness.wasm returnHelloWorld --wasi
