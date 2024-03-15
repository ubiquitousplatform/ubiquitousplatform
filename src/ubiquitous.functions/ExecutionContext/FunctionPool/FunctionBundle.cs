namespace ubiquitous.functions.ExecutionContext.FunctionPool
{
    public record FunctionBundle
    {
        public string? FunctionName;
        public string? Version; // TODO: ULID
        public byte[]? Code;
        public byte[]? Sha256;

    }


    /*


    Features:
     - Create an instance of a runner
     - Runner should have a list of all functions and their versions
     - Each function+version should have a config (just like Lambda)
     - Store functions using ubiquitous.storage
     - Runtime for now will just be `deno run xxx`
     - Support input/output (from either Event system or from Background Runner)
      - Take inspiration from Lambda event format

    Later
     - Enable default concurrency setting


    */
}

