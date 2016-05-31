using System;

namespace Retry.CircuitBreaker
{
    /// <summary>
    /// Exception thrown when a circuit is open.
    /// </summary>
    public class OpenCircuitException : Exception
    {
        public OpenCircuitException() : base("Open Circuit.") { }
    }

    /// <summary>
    /// Exception thrown when a circuit is closing.
    /// </summary>
    public class ClosingCircuitException : Exception
    {
        public ClosingCircuitException() : base("Closing Circuit.") { }
    }
}
