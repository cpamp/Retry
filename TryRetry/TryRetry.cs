using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TryRetry
{
    /// <summary>
    /// Class for simulating Try, Catch, Retry (n) times.
    /// </summary>
    /// <typeparam name="TResult">Type to be returned.</typeparam>
    public static class TryRetry<TResult>
    {
        /// <summary>
        /// Delegate for catch block function.
        /// </summary>
        /// <param name="e">Exception that is being handled.</param>
        /// <returns>TResult</returns>
        public delegate TResult CatchFunc(Exception e);

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
        private static TResult RetryLoop(Func<TResult> tryFunc, IDictionary<Type, CatchFunc> exCatch,
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
        /// <returns>tryFunc return value or catchFunc return value.</returns>
        public static TResult Retry<TException>(Func<TResult> tryFunc, CatchFunc catchFunc = null,
            int maxTries = 1, int millisecondsDelay = 0) where TException : Exception
        {
            return RetryLoop(
                tryFunc,
                new Dictionary<Type, CatchFunc>(){ { typeof(TException), catchFunc } },
                maxTries,
                millisecondsDelay);
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
        public static TResult Retry(Func<TResult> tryFunc, IDictionary<Type, CatchFunc> exCatch,
            int maxTries = 1, int millisecondsDelay = 0)
        {
            return RetryLoop(tryFunc, exCatch, maxTries, millisecondsDelay);
        }

        /// <summary>
        /// Try, catch, then retry (n) times asynchronously until max tries reached, an unexpected exception is thrown, 
        /// or try block executes without an exception.
        /// </summary>
        /// <param name="tryFunc">Try code block to execute.</param>
        /// <param name="catchFunc">Catch code block to execute.</param>
        /// <param name="maxTries">Maximum number of times to retry, minimum once.</param>
        /// <param name="millisecondsDelay">Milliseconds to delay next try.</param>
        /// <returns>Task</returns>
        public static async Task<TResult> RetryAsync<TException>(Func<TResult> tryFunc, CatchFunc catchFunc = null,
            int maxTries = 1, int millisecondsDelay = 0) where TException : Exception
        {
            return await Task.Run(() => RetryLoop(
                tryFunc,
                new Dictionary<Type, CatchFunc>() { { typeof(TException), catchFunc } },
                maxTries,
                millisecondsDelay));
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
        /// <returns>Task</returns>
        public static async Task<TResult> RetryAsync(Func<TResult> tryFunc, IDictionary<Type, CatchFunc> exCatch,
            int maxTries = 1, int millisecondsDelay = 0)
        {
            return await Task.Run(() => RetryLoop(tryFunc, exCatch, maxTries, millisecondsDelay));
        }
    }
}
