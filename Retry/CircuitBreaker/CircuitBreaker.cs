using System;

namespace Retry.CircuitBreaker
{
    /// <summary>
    /// Circuit breaker class for managing retry attempts
    /// </summary>
    /// <typeparam name="TResult">Type expected to be returned from Execute</typeparam>
    public class CircuitBreaker<TResult>
    {
        /// <summary>
        /// The state of the circuit breaker.
        /// </summary>
        private CircuitBreakerState _state;

        /// <summary>
        /// Lock
        /// </summary>
        private readonly object thisLock = new object();

        /// <summary>
        /// Last exception which occurred.
        /// </summary>
        public Exception LastException { get; set; }

        /// <summary>
        /// Number of times the circuit breaker has failed.
        /// </summary>
        public int FailCount { get; set; }

        /// <summary>
        /// Failed half open tries.
        /// </summary>
        public int FailedHalfOpenCount { get; set; }

        /// <summary>
        /// Number of times the circuit breaker is allowed to fail.
        /// </summary>
        public int Threshold { get; }

        /// <summary>
        /// Number of times the circuit break is allowed to fail when half open.
        /// </summary>
        public int HalfOpenThreshold { get; }

        /// <summary>
        /// The state of the circuit breaker.
        /// </summary>
        public CircuitBreakerState State
        {
            get { return _state; }
            set
            {
                lock (thisLock)
                {
                    _state = value;
                    OnStateChanged();
                }
            }
        }

        /// <summary>
        /// Time in milliseconds to wait before trying an open circuit.
        /// </summary>
        public int Timeout { get; }

        /// <summary>
        /// Whether the circuit is running or not.
        /// </summary>
        public bool Running { get; set; }

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public CircuitBreaker() : this(1, 0) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="threshold">Circuit threshold.</param>
        public CircuitBreaker(int threshold) : this(threshold, 0) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="threshold">Circuit threshold.</param>
        /// <param name="timeout">Time in milliseconds to wait before trying an open circuit.</param>
        /// <param name="async">Whether the caller is async. If not, circuit stays open permanently 
        /// and is not tried again. Defaults true.</param>
        public CircuitBreaker(int threshold, int timeout, int halfOpenThreshold = -1)
        {
            FailCount = 0;
            State = CircuitBreakerState.Closed;
            Threshold = threshold;
            Timeout = timeout;
            HalfOpenThreshold = halfOpenThreshold;
            Running = true;
        }
        #endregion

        /// <summary>
        /// Fire the circuit
        /// </summary>
        public TResult Execute(Func<TResult> execFunc)
        {
            TResult result = default(TResult);

            if (State == CircuitBreakerState.Closed ||
                State == CircuitBreakerState.HalfOpen)
            {
                try
                {
                    result = execFunc();
                }
                catch (Exception e)
                {
                    CaughtException(e);
                    throw e;
                }
            }
            else
            {
                throw new OpenCircuitException();
            }

            Running = false;
            return result;
        }

        /// <summary>
        /// Increment fail counter and trip if exceeds threshold.
        /// </summary>
        public void CaughtException(Exception e)
        {
            LastException = e;
            FailCount++;
            if (State == CircuitBreakerState.HalfOpen)
            {
                FailedHalfOpenCount++;
                Trip();
            }
            else if (FailCount > Threshold)
            {
                Trip();
            }
        }

        /// <summary>
        /// Trip the circuit
        /// </summary>
        public void Trip()
        {
            State = CircuitBreakerState.Open;
        }

        /// <summary>
        /// Set the circuit breaker to closed.
        /// </summary>
        public void Reset()
        {
            State = CircuitBreakerState.Closed;
        }

        /// <summary>
        /// Wait for timeout to continue
        /// </summary>
        public void Wait()
        {
            System.Threading.Thread.Sleep(Timeout);
            State = CircuitBreakerState.HalfOpen;
        }

        /// <summary>
        /// Handler for state changes.
        /// </summary>
        public void OnStateChanged()
        {
            if (State == CircuitBreakerState.Open && 
                HalfOpenThreshold > 0 && 
                HalfOpenThreshold > FailedHalfOpenCount)
            {
                Wait();
            }
            else if (State == CircuitBreakerState.Open && HalfOpenThreshold < 0 ||
                HalfOpenThreshold <= FailedHalfOpenCount)
            {
                Running = false;
            }
            else if (State == CircuitBreakerState.Closed)
            {
                FailCount = 0;
            }
        }
    }
}
