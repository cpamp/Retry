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
        public delegate TResult CatchFunc(Exception e);

        /// <summary>
        /// The circuit breaker
        /// </summary>
        private CircuitBreaker<TResult> cb;
        
        /// <summary>
        /// Collection of results from run once runs.
        /// </summary>
        public static RunResults<TResult> Results = new RunResults<TResult>();

        /// <summary>
        /// Handles exceptions thrown
        /// </summary>
        /// <param name="e">The <see cref="Exception"/> thrown.</param>
        /// <param name="exCatch"><see cref="IDictionary{TKey, TValue}"/> containing expected <see cref="Exception"/> <see cref="Type"/> 
        /// as key and <see cref="CatchFunc"/> to invoke for that <see cref="Exception"/> as value.</param>
        /// <returns>Result of catch function.</returns>
        private TResult HandleException(Exception e, IDictionary<Type, CatchFunc> exCatch)
        {
            TResult result = default(TResult);
            bool handled = false;

            foreach (var ec in exCatch)
            {
                if (e.GetType() == ec.Key)
                {
                    if (ec.Value != null)
                    {
                        CatchFunc catchFunc = ec.Value;
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
        /// <param name="tryFunc">Try code block to execute.</param>
        /// <param name="exCatch"><see cref="IDictionary{TKey, TValue}"/> containing expected <see cref="Exception"/> <see cref="Type"/> 
        /// as key and <see cref="CatchFunc"/> to invoke for that <see cref="Exception"/> as value.</param>
        /// <param name="maxTries">Maximum number of times to retry, minimum once.</param>
        /// <param name="millisecondsDelay">Milliseconds to delay next try.</param>
        /// <returns>tryFunc return value or catchFunc return value.</returns>
        private TResult RunRetry(Func<TResult> tryFunc, IDictionary<Type, CatchFunc> exCatch,
            int maxTries = 1, int millisecondsDelay = 0, int halfOpenThreshold = -1)
        {
            TResult result = default(TResult);
            maxTries = Math.Max(maxTries, 1);

            cb = new CircuitBreaker<TResult>(maxTries, millisecondsDelay, halfOpenThreshold);

            while (cb.Running)
            {
                try
                {
                    result = cb.Execute(tryFunc);
                }
                catch (OpenCircuitException)
                {
                    System.Console.WriteLine("Open Circuit");
                }
                catch (Exception e)
                {
                    result = HandleException(e, exCatch);
                }
            }

            return result;
        }

        /// <summary>
        /// Try, catch, then retry (n) times until max tries reached, an unexpected exception is thrown, 
        /// or try block executes without an exception.
        /// </summary>
        /// <typeparam name="TException">Expected <see cref="Exception"/> to handle.</typeparam>
        /// <param name="tryFunc">Try code block to execute.</param>
        /// <param name="catchFunc">Catch code block to execute.</param>
        /// <param name="maxTries">Maximum number of times to retry, minimum once.</param>
        /// <param name="millisecondsDelay">Milliseconds to delay next try.</param>
        /// <param name="id">Unique id of to associate with this call.</param>
        /// <returns>tryFunc return value or catchFunc return value.</returns>
        public TResult Run<TException>(Func<TResult> tryFunc, CatchFunc catchFunc = null,
            int maxTries = 1, int millisecondsDelay = 0, string id = null) where TException : Exception
        {
            TResult result = default(TResult);

            if (Results.CanRun(id))
            {
                result = RunRetry(
                tryFunc,
                new Dictionary<Type, CatchFunc>() { { typeof(TException), catchFunc } },
                maxTries,
                millisecondsDelay);
                Results.AddResult(id, result);
            }
            else
            {
                result = Results.GetResult(id);
            }

            return result;
        }

        /// <summary>
        /// Try, catch, then retry (n) times until max tries reached, an unexpected exception is thrown, 
        /// or try block executes without an exception.
        /// </summary>
        /// <param name="tryFunc">Try code block to execute.</param>
        /// <param name="exCatch"><see cref="IDictionary{TKey, TValue}"/> containing expected <see cref="Exception"/> <see cref="Type"/> 
        /// as key and <see cref="CatchFunc"/> to invoke for that <see cref="Exception"/> as value.</param>
        /// <param name="maxTries">Maximum number of times to retry, minimum once.</param>
        /// <param name="millisecondsDelay">Milliseconds to delay next try.</param>
        /// <param name="id">Unique id of to associate with this call.</param>
        /// <returns>tryFunc return value or catchFunc return value.</returns>
        public TResult Run(Func<TResult> tryFunc, IDictionary<Type, CatchFunc> exCatch,
            int maxTries = 1, int millisecondsDelay = 0, string id = null)
        {
            TResult result = default(TResult);

            if (Results.CanRun(id))
            {
                result = RunRetry(tryFunc, exCatch, maxTries, millisecondsDelay);
            }
            else
            {
                result = Results.GetResult(id);
            }

            return result;
        }

        /// <summary>
        /// Try, catch, then retry (n) times asynchronously until max tries reached, an unexpected exception is thrown, 
        /// or try block executes without an exception.
        /// </summary>
        /// <param name="tryFunc">Try code block to execute.</param>
        /// <param name="catchFunc">Catch code block to execute.</param>
        /// <param name="maxTries">Maximum number of times to retry, minimum once.</param>
        /// <param name="millisecondsDelay">Milliseconds to delay next try.</param>
        /// <param name="id">Unique id of to associate with this call.</param>
        /// <param name="halfOpenThreshold">Number of times HalfOpen attempts are allowed to fail.</param>
        /// <returns>Task</returns>
        public async Task<TResult> RunAsync<TException>(Func<TResult> tryFunc, CatchFunc catchFunc = null,
            int maxTries = 1, int millisecondsDelay = 0, string id = null, int halfOpenThreshold = -1) where TException : Exception
        {
            TResult result = default(TResult);

            if (Results.CanRun(id))
            {
                result = await Task.Run(() => RunRetry(
                tryFunc,
                new Dictionary<Type, CatchFunc>() { { typeof(TException), catchFunc } },
                maxTries,
                millisecondsDelay,
                halfOpenThreshold));
            }
            else
            {
                result = Results.GetResult(id);
            }

            return result;
        }

        /// <summary>
        /// Try, catch, then retry (n) times asynchronously until max tries reached, an unexpected exception is thrown, 
        /// or try block executes without an exception.
        /// </summary>
        /// <param name="tryFunc">Try code block to execute.</param>
        /// <param name="exCatch"><see cref="IDictionary{TKey, TValue}"/> containing expected <see cref="Exception"/> <see cref="Type"/> 
        /// as key and <see cref="Func{TResult}"/> to invoke for that <see cref="Exception"/> as value.</param>
        /// <param name="maxTries">Maximum number of times to retry, minimum once.</param>
        /// <param name="millisecondsDelay">Milliseconds to delay next try.</param>
        /// <param name="id">Unique id of to associate with this call.</param>
        /// <param name="halfOpenThreshold">Number of times HalfOpen attempts are allowed to fail.</param>
        /// <returns>Task</returns>
        public async Task<TResult> RunAsync(Func<TResult> tryFunc, IDictionary<Type, CatchFunc> exCatch,
            int maxTries = 1, int millisecondsDelay = 0, string id = null, int halfOpenThreshold = -1)
        {
            TResult result = default(TResult);

            if (Results.CanRun(id))
            {
                result = await Task.Run(() => RunRetry(
                    tryFunc, 
                    exCatch, 
                    maxTries,
                    millisecondsDelay, 
                    halfOpenThreshold));
            }
            else
            {
                result = Results.GetResult(id);
            }

            return result;
        }
    }
}
