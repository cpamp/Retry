using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Retry
{
    public class RunResults<TResult>
    {
        /// <summary>
        /// Lock
        /// </summary>
        private static readonly Object thisLock = new Object();

        /// <summary>
        /// Collection of results from run once runs.
        /// </summary>
        private Dictionary<string, TResult> runResults = new Dictionary<string, TResult>();

        /// <summary>
        /// Check if retry can only be ran once and add to runOnceIds collection.
        /// </summary>
        /// <param name="id">Id of Retry</param>
        /// <returns>True if can run, false if cannot run.</returns>
        public bool CanRun(string id)
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
        /// Get result value.
        /// </summary>
        /// <param name="id">Id of value to get.</param>
        /// <returns></returns>
        public TResult GetResult(string id)
        {
            TResult result = default(TResult);
            runResults.TryGetValue(id, out result);
            return result;
        }

        /// <summary>
        /// Add result to stored results
        /// </summary>
        /// <param name="id">Unique id.</param>
        /// <param name="value">Value to add.</param>
        public void AddResult(string id, TResult value)
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
        public void RemoveResult(string id)
        {
            lock (thisLock)
            {
                runResults.Remove(id);
            }
        }

        /// <summary>
        /// Removes all results from the runResults collection.
        /// </summary>
        public void ClearResults()
        {
            lock (thisLock)
            {
                runResults.Clear();
            }
        }
    }
}
