using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Retry.CircuitBreaker
{
    public class CircuitBreaker<TResult>
    {
        private CircuitBreakerState _state;

        private readonly object thisLock = new object();

        /// <summary>
        /// Number of times the circuit breaker has failed.
        /// </summary>
        public int FailCount { get; set; }

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
        /// Number of times the circuit breaker is allowed to fail.
        /// </summary>
        public int Threshold { get; }

        /// <summary>
        /// Time in milliseconds to wait before trying an open circuit.
        /// </summary>
        public int Timeout { get; }

        /// <summary>
        /// Whether the caller is async or not.
        /// </summary>
        public bool Async { get; }

        /// <summary>
        /// Whether the circuit is going to continue or not.
        /// </summary>
        public bool Continue { get; set; }

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public CircuitBreaker() : this(1, 0, false) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="threshold">Circuit threshold.</param>
        public CircuitBreaker(int threshold) : this(threshold, 0, false) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="threshold">Circuit threshold.</param>
        /// <param name="timeout">Time in milliseconds to wait before trying an open circuit.</param>
        /// <param name="async">Whether the caller is async. If not, circuit stays open permanently 
        /// and is not tried again. Defaults true.</param>
        public CircuitBreaker(int threshold, int timeout, bool async = true)
        {
            FailCount = 0;
            State = CircuitBreakerState.Closed;
            Threshold = threshold;
            Timeout = timeout;
            Async = async;
            Continue = true;
        }
        #endregion

        /// <summary>
        /// Fire the circuit
        /// </summary>
        public TResult Execute(Func<TResult> execFunc)
        {
            TResult result = default(TResult);

            if (State == CircuitBreakerState.Open)
            {
                throw new OpenCircuitException();
            }
            else if (State != CircuitBreakerState.Open)
            {
                try
                {
                    execFunc();
                }
                catch (Exception e)
                {
                    CaughtException();
                    throw e;
                }
            }

            Continue = false;
            return result;
        }

        /// <summary>
        /// Increment fail counter and trip if exceeds threshold.
        /// </summary>
        public void CaughtException()
        {
            FailCount++;
            if (FailCount > Threshold)
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
        public async void Wait()
        {
            await Task.Run(() => { System.Threading.Thread.Sleep(Timeout); });
            State = CircuitBreakerState.HalfOpen;
        }

        /// <summary>
        /// Handler for state changes.
        /// </summary>
        public void OnStateChanged()
        {
            if (State == CircuitBreakerState.Open && Async)
            {
                Wait();
            }
            else if (State == CircuitBreakerState.Open && !Async)
            {
                Continue = false;
            }
            else if (State == CircuitBreakerState.Closed)
            {
                FailCount = 0;
            }
        }
    }
}
