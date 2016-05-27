namespace Retry.CircuitBreaker
{
    /// <summary>
    /// State of the a circuit breaker.
    /// </summary>
    public enum CircuitBreakerState
    {
        Closed,
        Open,
        HalfOpen
    }
}
