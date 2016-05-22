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
        /// Handles exceptions thrown
        /// </summary>
        /// <param name="e">The <see cref="Exception"/> thrown.</param>
        /// <param name="exCatch"><see cref="IDictionary{TKey, TValue}"/> containing expected <see cref="Exception"/> <see cref="Type"/> 
        /// as key and <see cref="Func{TResult}"/> to invoke for that <see cref="Exception"/> as value.</param>
        /// <returns>Result of catch function.</returns>
        private static TResult HandleException(Exception e, IDictionary<Type, Func<TResult>> exCatch)
        {
            TResult result = default(TResult);
            bool handled = false;

            foreach (var ec in exCatch)
            {
                if (e.GetType() == ec.Key)
                {
                    if (ec.Value != null) { result = ec.Value.Invoke(); }
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
        /// <typeparam name="TException">Expected <see cref="Exception"/> to handle.</typeparam>
        /// <param name="tryFunc">Try code block to execute.</param>
        /// <param name="exCatch"><see cref="IDictionary{TKey, TValue}"/> containing expected <see cref="Exception"/> <see cref="Type"/> 
        /// as key and <see cref="Func{TResult}"/> to invoke for that <see cref="Exception"/> as value.</param>
        /// <param name="maxTries">Maximum number of times to retry, minimum once.</param>
        /// <param name="millisecondsDelay">Milliseconds to delay next try.</param>
        /// <returns>tryFunc return value or catchFunc return value.</returns>
        private static TResult RetryLoop(Func<TResult> tryFunc, IDictionary<Type, Func<TResult>> exCatch,
            int maxTries = 1, int millisecondsDelay = 0)
        {
            TResult result = default(TResult);
            int numTries = 0;
            maxTries = maxTries < 0 ? 1 : maxTries;

            while (numTries <= maxTries)
            {
                try
                {
                    result = tryFunc();
                    break;
                }
                catch (Exception e)
                {
                    HandleException(e, exCatch);
                }
                finally
                {
                    numTries++;
                    if (millisecondsDelay > 0 && numTries <= maxTries)
                    {
                        System.Threading.Thread.Sleep(millisecondsDelay);
                    }
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
        public static TResult Retry<TException>(Func<TResult> tryFunc, Func<TResult> catchFunc = null,
            int maxTries = 1, int millisecondsDelay = 0) where TException : Exception, new()
        {
            return RetryLoop(
                tryFunc,
                new Dictionary<Type, Func<TResult>>(){ { new TException().GetType(), catchFunc } },
                maxTries,
                millisecondsDelay);
        }

        /// <summary>
        /// Try, catch, then retry (n) times until max tries reached, an unexpected exception is thrown, 
        /// or try block executes without an exception.
        /// </summary>
        /// <typeparam name="TException">Expected <see cref="Exception"/> to handle.</typeparam>
        /// <param name="tryFunc">Try code block to execute.</param>
        /// <param name="exCatch"><see cref="IDictionary{TKey, TValue}"/> containing expected <see cref="Exception"/> <see cref="Type"/> 
        /// as key and <see cref="Func{TResult}"/> to invoke for that <see cref="Exception"/> as value.</param>
        /// <param name="maxTries">Maximum number of times to retry, minimum once.</param>
        /// <param name="millisecondsDelay">Milliseconds to delay next try.</param>
        /// <returns>tryFunc return value or catchFunc return value.</returns>
        public static TResult Retry(Func<TResult> tryFunc, IDictionary<Type, Func<TResult>> exCatch,
            int maxTries = 1, int millisecondsDelay = 0)
        {
            return RetryLoop(tryFunc, exCatch, maxTries, millisecondsDelay);
        }
    }
}
