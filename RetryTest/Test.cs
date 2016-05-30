using System;
using Retry;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TryRetryTest
{
    [TestClass]
    public class Test
    {
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Loop to test. Throws <see cref="IndexOutOfRangeException"/> on fail.
        /// </summary>
        /// <param name="fail">Whether the loop should throw <see cref="IndexOutOfRangeException"/> or not.</param>
        /// <returns>Returns 0 on pass.</returns>
        private int Thrower(bool fail = true)
        {
            TestContext.WriteLine("Thrower");
            if (fail)
            {
                SqlConnection conn = new SqlConnection(@"Connection Timeout=1");
                conn.Open();
            }
            return 1;
        }

        /// <summary>
        /// Method for catching exceptions.
        /// </summary>
        /// <returns>Returns 1 on catch.</returns>
        private int Catcher(Exception e)
        {
            TestContext.WriteLine("Catcher");
            SqlException sEx = e as SqlException;
            return -1;
        }

        /// <summary>
        /// Test for the wrong exception.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(SqlException))]
        public void WrongException()
        {
            Dictionary<Type, Retry<int>.CatchFunction> exCatch =
                new Dictionary<Type, Retry<int>.CatchFunction>()
            { { typeof(NullReferenceException), Catcher } };

            Retry<int> retry = new Retry<int>(
                () => Thrower(),
                exCatch);
            retry.Run();
        }

        /// <summary>
        /// Test for a failed attempt despite retrying.
        /// </summary>
        [TestMethod]
        public void Failed()
        {
            Dictionary<Type, Retry<int>.CatchFunction> exCatch =
                new Dictionary<Type, Retry<int>.CatchFunction>()
            { { typeof(SqlException), Catcher } };

            Retry<int> retry = new Retry<int>(
                () => Thrower(),
                exCatch);
            int result = retry.Run();
            Assert.AreEqual<int>(-1, result);
        }

        /// <summary>
        /// Test for a passed attempt without retrying.
        /// </summary>
        [TestMethod]
        public void Passed()
        {
            Dictionary<Type, Retry<int>.CatchFunction> exCatch =
                new Dictionary<Type, Retry<int>.CatchFunction>()
            { { typeof(SqlException), Catcher } };

            Retry<int> retry = new Retry<int>(
                () => Thrower(false),
                exCatch);
            int result = retry.Run();
            Assert.AreEqual<int>(1, result);
        }

        /// <summary>
        /// Test tryonce case with id
        /// </summary>
        [TestMethod]
        public void RunOnce()
        {
            Dictionary<Type, Retry<int>.CatchFunction> exCatch =
                new Dictionary<Type, Retry<int>.CatchFunction>()
            { { typeof(IndexOutOfRangeException), Catcher } };

            Dictionary<Type, Retry<int>.CatchFunction> exCatch2 =
                new Dictionary<Type, Retry<int>.CatchFunction>()
            { { typeof(SqlException), Catcher } };

            Retry<int> retry2 = new Retry<int>(
                () => Thrower(),
                exCatch2, 1, 0, 0, "Test2");

            Retry<int> retry = new Retry<int>(
                () =>
                {
                    TestContext.WriteLine("Outer Thrower");
                    retry2.Run();
                    throw new IndexOutOfRangeException();
                },
                exCatch, 5, 0, 0, "Test");

            int result = retry.Run();
            Assert.AreEqual<int>(-1, result);
        }

        /// <summary>
        /// Test stored results.
        /// </summary>
        [TestMethod]
        public void RunOnceStoreResults()
        {
            Dictionary<Type, Retry<int>.CatchFunction> exCatch =
                new Dictionary<Type, Retry<int>.CatchFunction>()
            { { typeof(SqlException), Catcher } };

            Retry<int> retry = new Retry<int>(
                () => Thrower(false),
                exCatch, 1, 0, 0, "Test3");
            Retry<int> retry2 = new Retry<int>(
                () => Thrower(false),
                exCatch, 1, 0, 0, "Test4");

            retry.Run();
            int result = retry.Run();

            retry2.Run();
            int result2 = retry2.Run();
            Assert.AreEqual<int>(result + result2, 2);
        }

        /// <summary>
        /// Test for an async failed attempt despite retrying.
        /// </summary>
        [TestMethod]
        public async Task FailedAsync()
        {
            Dictionary<Type, Retry<int>.CatchFunction> exCatch =
                new Dictionary<Type, Retry<int>.CatchFunction>()
            { { typeof(SqlException), Catcher } };

            Retry<int> retry = new Retry<int>(
                () => Thrower(),
                exCatch, 5, 100, 5, "test");

            int result = await retry.RunAsync();
            Assert.AreEqual<int>(-1, result);
        }
    }
}
