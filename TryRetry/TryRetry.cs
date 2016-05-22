﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            return Retry(
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
        /// <param name="exceptionCatch">Dictionary containing expected <see cref="Exception"/> <see cref="Type"/> 
        /// as key and <see cref="Func{TResult}"/> to invoke for that <see cref="Exception"/> as value.</param>
        /// <param name="maxTries">Maximum number of times to retry, minimum once.</param>
        /// <param name="millisecondsDelay">Milliseconds to delay next try.</param>
        /// <returns>tryFunc return value or catchFunc return value.</returns>
        public static TResult Retry(Func<TResult> tryFunc, IDictionary<Type, Func<TResult>> exceptionCatch,
            int maxTries = 1, int millisecondsDelay = 0)
        {
            TResult result = default(TResult);
            bool handled = false;
            int numTries = 0;
            maxTries = maxTries < 0 ? 1 : maxTries;

            while (numTries <= maxTries)
            {
                handled = false;
                try
                {
                    result = tryFunc();
                    break;
                }
                catch (Exception e)
                {
                    foreach (var ec in exceptionCatch)
                    {
                        if (e.GetType() == ec.Key)
                        {
                            if (ec.Value != null) { result = ec.Value.Invoke(); }
                            handled = true;
                            break;
                        }
                    }

                    if (!handled) throw e;
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
    }
}
