npm run build
echo "Testing all functions"
echo "Testing strlen. expected output: 11"
extism call dist/test-harness.wasm strlen --input="test string" --wasi #--log-level trace
echo "\nTesting getClosest. expected output: fastest world"
extism call dist/test-harness.wasm getClosest --input="fastett" --wasi
echo "\nTesting randomstr. expected output: 56 character long random string"
extism call dist/test-harness.wasm randomstr --input="56" --wasi
echo "\nTesting returnHelloWorld. expected output: Hello, World!"
extism call dist/test-harness.wasm returnHelloWorld --wasi
echo "\nTesting doNothing. expect empty output."
extism call dist/test-harness.wasm doNothing --wasi
echo "\nTesting intArrayStatsJSON. expect empty output."
extism call dist/test-harness.wasm intArrayStatsJSON --input="[1, 2, 5, -1, 5, 5, 9, 100, 204]" --wasi
./copy-to-ubiquitous.functions.tests.unit.sh