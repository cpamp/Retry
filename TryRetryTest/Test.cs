using System;
using TryRetry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        private int Looper(bool fail = true)
        {
            TestContext.WriteLine("Looper");
            int[] arr = { 1, 2 };
            for (int i = 0; fail; i++)
            {
                int j = arr[i];
            }
            return 1;
        }

        /// <summary>
        /// Method for catching exceptions.
        /// </summary>
        /// <returns>Returns 1 on catch.</returns>
        private int Catcher()
        {
            TestContext.WriteLine("Catcher");
            return -1;
        }

        /// <summary>
        /// Test for the wrong exception.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(System.IndexOutOfRangeException))]
        public void WrongException()
        {
            TryRetry<int>.Retry<NullReferenceException>(
                () => Looper(),
                Catcher);
        }

        /// <summary>
        /// Test for a failed attempt despite retrying.
        /// </summary>
        [TestMethod]
        public void Failed()
        {
            int result = TryRetry<int>.Retry<IndexOutOfRangeException>(
                () => Looper(),
                Catcher);
            Assert.AreEqual<int>(-1, result);
        }

        /// <summary>
        /// Test for a passed attempt without retrying.
        /// </summary>
        [TestMethod]
        public void Passed()
        {
            int result = TryRetry<int>.Retry<IndexOutOfRangeException>(
                () => Looper(false),
                Catcher);
            Assert.AreEqual<int>(1, result);
        }
    }
}
