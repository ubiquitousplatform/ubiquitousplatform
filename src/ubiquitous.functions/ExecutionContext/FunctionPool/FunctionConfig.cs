using System;

namespace ubiquitous.functions.ExecutionContext.FunctionPool
{
    public class FunctionConfig
    {
        string Runtime;
        string FunctionName;
        string Version;
        Dictionary<string, string> EnvironmentVariables;
        public string StorageId { get; set; }
    }
}

