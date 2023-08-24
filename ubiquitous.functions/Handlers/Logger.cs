namespace ubiquitous.functions.Handlers
{
    public class HandlerResult
    {
        public string CorrelationId { get; set; }
    }
    internal class Logger
    {
        // TODO: use a real logger (e.g. Serilog)
        public static HandlerResult Log(string message)
        {
            //   Console.WriteLine("FROM LOGGER HANDLER: {0}", message);
            return new HandlerResult() { CorrelationId = "abcd" };
        }
    }
}
