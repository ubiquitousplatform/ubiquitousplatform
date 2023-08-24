using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ubiquitous.functions.ExecutionContext.FunctionPool;

namespace ubiquitous.functions.ExecutionContext.RuntimeQueue
{
    internal interface IRuntimeQueue
    {
        public WasmRuntime GetRuntime(FunctionConfig functionConfig);
    }
}
