# Build Process

## 1. Build TypeSpec project to emit an OpenAPI Spec file

1. Install typespec globally (see [TypeSpec Docs](https://typespec.io/docs)).
2. cd to src/
3. run `tsp install`
4. run `tsp compile .`


Example typespec: https://github.com/connorjs/swapi-typespec/



## 2. Generate typescript client from OpenAPI spec file
1. Install openapi-generator globally
2. run `openapi-generator generate -i tsp-output/@typespec/openapi3/openapi.yaml -o generated-sdks/typescript -g typescript-fetch`

TODO:
- figure out how to namespace it in OpenAPI spec
- figure out how to add versioning to paths