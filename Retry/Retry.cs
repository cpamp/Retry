using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Retry.CircuitBreaker;

namespace Retry
{
    /// <summary>
    /// Class for simulating Try, Catch, Retry (n) times.
    /// </summary>
    /// <typeparam name="TResult">Type to be returned.</typeparam>
    public class Retry<TResult>
    {
        /// <summary>
        /// Delegate for catch block function.
        /// </summary>
        /// <param name="e">Exception that is being handled.</param>
        /// <returns>TResult</returns>
        public delegate TResult CatchFunction(Exception e);

        /// <summary>
        /// Function to try.
        /// </summary>
        private Func<TResult> tryFunction;

        /// <summary>
        /// Exceptions to catch and their catching functions.
        /// </summary>
        private IDictionary<Type, CatchFunction> exceptionCatchFunctions;

        /// <summary>
        /// Id of this retry instance.
        /// </summary>
        private string retryId;

        /// <summary>
        /// The circuit breaker
        /// </summary>
        private CircuitBreaker<TResult> circuitBreaker;
        
        /// <summary>
        /// Collection of results from run once runs.
        /// </summary>
        public static RunResults<TResult> Results = new RunResults<TResult>();

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tryFunc">Try code block to execute.</param>
        /// <param name="exCatch"><see cref="IDictionary{TKey, TValue}"/> containing expected <see cref="Exception"/> <see cref="Type"/> 
        /// as key and <see cref="CatchFunction"/> to invoke for that <see cref="Exception"/> as value.</param>
        public Retry(Func<TResult> tryFunc, IDictionary<Type, CatchFunction> exCatch) :
            this(tryFunc, exCatch, 1, 0, 0, null)
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tryFunc">Try code block to execute.</param>
        /// <param name="exCatch"><see cref="IDictionary{TKey, TValue}"/> containing expected <see cref="Exception"/> <see cref="Type"/> 
        /// as key and <see cref="CatchFunction"/> to invoke for that <see cref="Exception"/> as value.</param>
        /// <param name="maxTries">Maximum number of times to retry, minimum once.</param>
        public Retry(Func<TResult> tryFunc, IDictionary<Type, CatchFunction> exCatch,
            int maxTries) :
            this(tryFunc, exCatch, maxTries, 0, 0, null)
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tryFunc">Try code block to execute.</param>
        /// <param name="exCatch"><see cref="IDictionary{TKey, TValue}"/> containing expected <see cref="Exception"/> <see cref="Type"/> 
        /// as key and <see cref="CatchFunction"/> to invoke for that <see cref="Exception"/> as value.</param>
        /// <param name="maxTries">Maximum number of times to retry, minimum once.</param>
        /// <param name="millisecondsToWait">Milliseconds to delay next try.</param>
        public Retry(Func<TResult> tryFunc, IDictionary<Type, CatchFunction> exCatch,
            int maxTries, int millisecondsToWait) :
            this(tryFunc, exCatch, maxTries, millisecondsToWait, 0, null)
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tryFunc">Try code block to execute.</param>
        /// <param name="exCatch"><see cref="IDictionary{TKey, TValue}"/> containing expected <see cref="Exception"/> <see cref="Type"/> 
        /// as key and <see cref="CatchFunction"/> to invoke for that <see cref="Exception"/> as value.</param>
        /// <param name="maxTries">Maximum number of times to retry, minimum once.</param>
        /// <param name="millisecondsToWait">Milliseconds to delay next try.</param>
        /// <param name="maxWaits">Maximum times to wait.</param>
        public Retry(Func<TResult> tryFunc, IDictionary<Type, CatchFunction> exCatch,
            int maxTries, int millisecondsToWait, int maxWaits) :
            this(tryFunc, exCatch, maxTries, millisecondsToWait, maxWaits, null)
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tryFunc">Try code block to execute.</param>
        /// <param name="exCatch"><see cref="IDictionary{TKey, TValue}"/> containing expected <see cref="Exception"/> <see cref="Type"/> 
        /// as key and <see cref="CatchFunction"/> to invoke for that <see cref="Exception"/> as value.</param>
        /// <param name="maxTries">Maximum number of times to retry, minimum once.</param>
        /// <param name="millisecondsToWait">Milliseconds to delay next try.</param>
        /// <param name="maxWaits">Maximum times to wait.</param>
        /// <param name="id">Unique id of to associate with this call.</param>
        public Retry(Func<TResult> tryFunc, IDictionary<Type, CatchFunction> exCatch,
            int maxTries, int millisecondsToWait, int maxWaits, string id)
        {
            retryId = id;
            tryFunction = tryFunc;
            exceptionCatchFunctions = exCatch;
            maxTries = Math.Max(maxTries, 1);
            circuitBreaker = new CircuitBreaker<TResult>(maxTries, millisecondsToWait, maxWaits);
        }
        #endregion

        /// <summary>
        /// Handles exceptions thrown
        /// </summary>
        /// <param name="e">The <see cref="Exception"/> thrown.</param>
        /// <param name="exCatch"><see cref="IDictionary{TKey, TValue}"/> containing expected <see cref="Exception"/> <see cref="Type"/> 
        /// as key and <see cref="CatchFunction"/> to invoke for that <see cref="Exception"/> as value.</param>
        /// <returns>Result of catch function.</returns>
        private TResult HandleException(Exception e)
        {
            TResult result = default(TResult);
            bool handled = false;

            foreach (var ec in exceptionCatchFunctions)
            {
                if (e.GetType() == ec.Key)
                {
                    if (ec.Value != null)
                    {
                        CatchFunction catchFunc = ec.Value;
                        result = catchFunc(e);
                    }
                    handled = true;
                    break;
                }
            }

            if (!handled) throw e;
            return result;
        }

        /// <summary>
        /// Try, catch, then retry (n) times until max tries reached, an unexpected exception is thrown, 
        /// or try block executes without an exception.
        /// </summary>
        /// <returns><see cref="TResult"/></returns>
        private TResult RunRetry()
        {
            TResult result = default(TResult);

            circuitBreaker.Reset();

            while (circuitBreaker.Running)
            {
                try
                {
                    result = circuitBreaker.Execute(tryFunction);
                }
                catch (OpenCircuitException)
                {
                    System.Diagnostics.Debug.WriteLine("Open Circuit.");
                }
                catch (Exception e)
                {
                    result = HandleException(e);
                }
            }

            return result;
        }

        /// <summary>
        /// Try, catch, then retry (n) times until max tries reached, an unexpected exception is thrown, 
        /// or try block executes without an exception.
        /// </summary>
        /// <returns><see cref="TResult"/></returns>
        public TResult Run()
        {
            TResult result = default(TResult);

            if (Results.CanRun(retryId))
            {
                result = RunRetry();
            }
            else
            {
                result = Results.GetResult(retryId);
            }

            return result;
        }

        /// <summary>
        /// Try, catch, then retry (n) times asynchronously until max tries reached, an unexpected exception is thrown, 
        /// or try block executes without an exception.
        /// </summary>
        /// <returns>Task<<see cref="TResult"/>></returns>
        public async Task<TResult> RunAsync()
        {
            TResult result = default(TResult);

            if (Results.CanRun(retryId))
            {
                result = await Task.Run(() => RunRetry());
            }
            else
            {
                result = Results.GetResult(retryId);
            }

            return result;
        }
    }
}
