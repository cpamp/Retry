using System;
using Retry;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.SqlClient;

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
            if (fail) {
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
            Retry<int> retry = new Retry<int>();
            retry.Run<NullReferenceException>(
                () => Thrower(),
                Catcher);
        }

        /// <summary>
        /// Test for a failed attempt despite retrying.
        /// </summary>
        [TestMethod]
        public void Failed()
        {
            Retry<int> retry = new Retry<int>();
            int result = retry.Run<SqlException>(
                () => Thrower(),
                Catcher);
            Assert.AreEqual<int>(-1, result);
        }

        /// <summary>
        /// Test for a passed attempt without retrying.
        /// </summary>
        [TestMethod]
        public void Passed()
        {
            Retry<int> retry = new Retry<int>();
            int result = retry.Run<SqlException>(
                () => Thrower(false),
                Catcher);
            Assert.AreEqual<int>(1, result);
        }

        /// <summary>
        /// Test tryonce case with id
        /// </summary>
        [TestMethod]
        public void RunOnce()
        {
            Retry<int> retry = new Retry<int>();
            int result = retry.Run<IndexOutOfRangeException>(
                () => {
                    TestContext.WriteLine("Outer Thrower");
                    Retry<int>.Run<SqlException>(
                        () => Thrower(),
                        Catcher, 1, 0, "Test2");
                    throw new IndexOutOfRangeException();
                },
                Catcher, 5, 0, "Test");
            Assert.AreEqual<int>(-1, result);
        }

        /// <summary>
        /// Test stored results.
        /// </summary>
        [TestMethod]
        public void RunOnceStoreResults()
        {
            Retry<int> retry = new Retry<int>();
            retry.Run<SqlException>(
                () => Thrower(false),
                Catcher, 1, 0, "Test3");
            int result = retry.Run<SqlException>(
                () => Thrower(false),
                Catcher, 1, 0, "Test3");
            retry.Run<SqlException>(
                () => Thrower(false),
                Catcher, 1, 0, "Test4");
            int result2 = retry.Run<SqlException>(
                () => Thrower(false),
                Catcher, 1, 0, "Test4");
            Assert.AreEqual<int>(result + result2, 2);
        }
    }
}
