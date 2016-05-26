using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Retry
{
    /// <summary>
    /// Class for simulating Try, Catch, Retry (n) times.
    /// </summary>
    /// <typeparam name="TResult">Type to be returned.</typeparam>
    public static class Retry<TResult>
    {
        /// <summary>
        /// Delegate for catch block function.
        /// </summary>
        /// <param name="e">Exception that is being handled.</param>
        /// <returns>TResult</returns>
        public delegate TResult CatchFunc(Exception e);

        /// <summary>
        /// Lock
        /// </summary>
        private static readonly Object thisLock = new Object();
        
        ///// <summary>
        ///// Collection of Id's that can only be ran once.
        ///// </summary>
        //private static HashSet<string> runOnceIds = new HashSet<string>();

        /// <summary>
        /// Collection of results from run once runs.
        /// </summary>
        private static Dictionary<string, TResult> runResults = new Dictionary<string, TResult>();

        /// <summary>
        /// Check if retry can only be ran once and add to runOnceIds collection.
        /// </summary>
        /// <param name="id">Id of Retry</param>
        /// <returns>True if can run, false if cannot run.</returns>
        private static bool CanRun(string id)
        {
            bool result = true;

            lock (thisLock)
            {
                if (id != null && runResults.ContainsKey(id))
                {
                    result = false;
                }
            }

            return result;
        }

        /// <summary>
        /// Add result to stored results
        /// </summary>
        /// <param name="id">Unique id.</param>
        /// <param name="value">Value to add.</param>
        public static void AddResult(string id, TResult value)
        {
            lock (thisLock)
            {
                if (id != null)
                    runResults.Add(id, value);
            }
        }

        /// <summary>
        /// Remove result from stored results.
        /// </summary>
        /// <param name="id">Unique id.</param>
        public static void RemoveResult(string id)
        {
            lock (thisLock)
            {
                runResults.Remove(id);
            }
        }

        /// <summary>
        /// Removes all ids from the RunOnce collection.
        /// </summary>
        public static void ClearResults()
        {
            lock (thisLock)
            {
                runResults.Clear();
            }
        }

        /// <summary>
        /// Handles exceptions thrown
        /// </summary>
        /// <param name="e">The <see cref="Exception"/> thrown.</param>
        /// <param name="exCatch"><see cref="IDictionary{TKey, TValue}"/> containing expected <see cref="Exception"/> <see cref="Type"/> 
        /// as key and <see cref="CatchFunc"/> to invoke for that <see cref="Exception"/> as value.</param>
        /// <returns>Result of catch function.</returns>
        private static TResult HandleException(Exception e, IDictionary<Type, CatchFunc> exCatch)
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
        private static TResult RunRetry(Func<TResult> tryFunc, IDictionary<Type, CatchFunc> exCatch,
            int maxTries = 1, int millisecondsDelay = 0)
        {
            TResult result = default(TResult);
            int numTries = 0;
            maxTries = Math.Max(maxTries, 1);

            while (numTries <= maxTries)
            {
                try
                {
                    result = tryFunc();
                    break;
                }
                catch (Exception e)
                {
                    result = HandleException(e, exCatch);
                }

                numTries++;
                if (millisecondsDelay > 0 && numTries <= maxTries)
                {
                    System.Threading.Thread.Sleep(millisecondsDelay);
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
        public static TResult Run<TException>(Func<TResult> tryFunc, CatchFunc catchFunc = null,
            int maxTries = 1, int millisecondsDelay = 0, string id = null) where TException : Exception
        {
            TResult result = default(TResult);

            if (CanRun(id))
            {
                result = RunRetry(
                tryFunc,
                new Dictionary<Type, CatchFunc>() { { typeof(TException), catchFunc } },
                maxTries,
                millisecondsDelay);
                AddResult(id, result);
            }
            else
            {
                runResults.TryGetValue(id, out result);
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
        public static TResult Run(Func<TResult> tryFunc, IDictionary<Type, CatchFunc> exCatch,
            int maxTries = 1, int millisecondsDelay = 0, string id = null)
        {
            TResult result = default(TResult);

            if (CanRun(id))
            {
                result = RunRetry(tryFunc, exCatch, maxTries, millisecondsDelay);
            }
            else
            {
                runResults.TryGetValue(id, out result);
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
        /// <returns>Task</returns>
        public static async Task<TResult> RunAsync<TException>(Func<TResult> tryFunc, CatchFunc catchFunc = null,
            int maxTries = 1, int millisecondsDelay = 0, string id = null) where TException : Exception
        {
            TResult result = default(TResult);

            if (CanRun(id))
            {
                result = await Task.Run(() => RunRetry(
                tryFunc,
                new Dictionary<Type, CatchFunc>() { { typeof(TException), catchFunc } },
                maxTries,
                millisecondsDelay));
            }
            else
            {
                runResults.TryGetValue(id, out result);
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
        /// <returns>Task</returns>
        public static async Task<TResult> RunAsync(Func<TResult> tryFunc, IDictionary<Type, CatchFunc> exCatch,
            int maxTries = 1, int millisecondsDelay = 0, string id = null)
        {
            TResult result = default(TResult);

            if (CanRun(id))
            {
                result = await Task.Run(() => RunRetry(tryFunc, exCatch, maxTries, millisecondsDelay));
            }
            else
            {
                runResults.TryGetValue(id, out result);
            }

            return result;
        }
    }
}
