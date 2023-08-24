namespace ubiquitous.functions.ExecutionContext.FunctionPool
{
    public class CapacityConfig
    {
        public int MinCapacity { get; set; }
        public int MaxCapacity { get; set; }
        public int OverprovisionTargetPercentage { get; set; }
    }
}