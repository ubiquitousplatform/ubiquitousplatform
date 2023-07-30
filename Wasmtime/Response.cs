using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WasmtimeExamples
{
    public class LogResponse
    {

        public List<string> something { get; set; }
    }
    public class Response
    {
        public bool ok { get; set; }
        public string type { get; set; }
        public LogResponse payload { get; set; }
    }
}
