using System;
namespace ubiquitous.functions
{
    public class FunctionConfig
    {
        string FunctionName;
        string Version;
        int MinInstances;
        int MaxInstances;
        int OverprovisionPercentage;
        Dictionary<string, string> EnvironmentVariables;
        public string StorageId { get; set; }
    }
}

