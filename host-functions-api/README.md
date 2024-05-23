# Build Process

## 1. Build TypeSpec project to emit an OpenAPI Spec file

1. cd to src/
2. run `npm i` to install typespec compiler, etc.
3. run `npm run tsp:compile`


Example typespec: https://github.com/connorjs/swapi-typespec/



## 2. Generate typescript client from OpenAPI spec file
1. Install openapi-generator globally
2. run `openapi-generator generate -i tsp-output/@typespec/openapi3/openapi.yaml -o generated-sdks/typescript -g typescript-fetch`

TODO:
- figure out how to namespace it in OpenAPI spec
- figure out how to add versioning to paths