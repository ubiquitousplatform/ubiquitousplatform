namespace ubiquitous.functions.ExecutionContext.FunctionPool
{
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



    public record FunctionDefinition
    {
        string? functionName;
        string? version;
        int minContexts = 0;
        int maxContexts = 10;
        int overprovisionTargetPercentage = 20;
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

