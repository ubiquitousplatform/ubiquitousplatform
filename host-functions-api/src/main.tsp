import "@typespec/http";
import "@typespec/rest";
import "@typespec/openapi3";

using TypeSpec.Http;
using TypeSpec.Rest;
using TypeSpec.OpenAPI;

@service({
    title: "KeyValue",
})
@server("https://localhost:8566", "Mock Ubiquitous Server")
@route("/keyValue")
namespace KeyValue {
    @summary("Gets a value by key")
    @route("/{key}")
    @get
    op get(@path key: string): GetResult;

    @summary("Sets a given key to a given value")
    @route("/{key}")
    @post
    op set(@path key: string, @body value: string): void;

    model GetResult {
        key: string;
        value: string;
    }

    /* model Pet {
        @minLength(100)
        name: string;

        @minValue(0)
        @maxValue(100)
        age: int32;

        kind: "dog" | "cat" | "fish";
    }*/
}

// TODO: define the logs namespace (with span support, log levels, etc.)
