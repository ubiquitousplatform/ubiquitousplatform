namespace ubiquitous.logs
{
    public class Logger
    {
        
        
        // TODO: write log methods

        public Logger() { }

        public Logger(string name) { }  


        // We should force structured logging.  If all you want to do is fill the "message" property, that's fine, but the timestamp and trace information will be there regardless.
        // how do we accept a generic JSON payload in C#?
        // also, we need to constructor inject the config or somehow connect the logger to the config so it knows what log level should apply to it.
        public void Trace(string message) { }
        public void Debug(string message) { }
        public void Info(string message) {
        
        }

        public void Warn(string message) { }
        public void Error(string message) { }
        public void Fatal(string message) { }


    }
}