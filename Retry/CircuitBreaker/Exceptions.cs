using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Retry.CircuitBreaker
{
    public class OpenCircuitException : Exception
    {
        public OpenCircuitException() : base("Open Circuit.") { }
    }
}
