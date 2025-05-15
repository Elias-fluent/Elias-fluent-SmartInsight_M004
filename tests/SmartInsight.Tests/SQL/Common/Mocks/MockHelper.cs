using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Language;
using Moq.Language.Flow;

namespace SmartInsight.Tests.SQL.Common.Mocks
{
    /// <summary>
    /// Helper methods for common mocking scenarios
    /// </summary>
    public static class MockHelper
    {
        /// <summary>
        /// Sets up a mock to return a value from an asynchronous method
        /// </summary>
        /// <typeparam name="T">Type of the mock</typeparam>
        /// <typeparam name="TResult">Type of the result</typeparam>
        /// <param name="setup">Mock setup</param>
        /// <param name="result">Result to return</param>
        /// <returns>Mock setup for further configuration</returns>
        public static IReturnsResult<T> ReturnsAsync<T, TResult>(
            this IReturns<T, Task<TResult>> setup,
            TResult result) where T : class
        {
            return setup.Returns(Task.FromResult(result));
        }

        /// <summary>
        /// Sets up a mock to throw an exception from an asynchronous method
        /// </summary>
        /// <typeparam name="T">Type of the mock</typeparam>
        /// <typeparam name="TResult">Type of the result</typeparam>
        /// <param name="setup">Mock setup</param>
        /// <param name="exception">Exception to throw</param>
        /// <returns>Mock setup for further configuration</returns>
        public static IReturnsResult<T> ThrowsAsync<T, TResult>(
            this IReturns<T, Task<TResult>> setup,
            Exception exception) where T : class
        {
            return setup.Returns(Task.FromException<TResult>(exception));
        }

        /// <summary>
        /// Sets up a mock to return a completed task from an asynchronous method with no result
        /// </summary>
        /// <typeparam name="T">Type of the mock</typeparam>
        /// <param name="setup">Mock setup</param>
        /// <returns>Mock setup for further configuration</returns>
        public static IReturnsResult<T> ReturnsCompletedTask<T>(
            this IReturns<T, Task> setup) where T : class
        {
            return setup.Returns(Task.CompletedTask);
        }

        /// <summary>
        /// Sets up a mock to throw an exception from an asynchronous method with no result
        /// </summary>
        /// <typeparam name="T">Type of the mock</typeparam>
        /// <param name="setup">Mock setup</param>
        /// <param name="exception">Exception to throw</param>
        /// <returns>Mock setup for further configuration</returns>
        public static IReturnsResult<T> ThrowsAsync<T>(
            this IReturns<T, Task> setup,
            Exception exception) where T : class
        {
            return setup.Returns(Task.FromException(exception));
        }

        /// <summary>
        /// Verifies that a mock method was called exactly once
        /// </summary>
        /// <typeparam name="T">Type of the mock</typeparam>
        /// <param name="mock">Mock object</param>
        /// <param name="expression">Expression to verify</param>
        public static void VerifyCalledOnce<T>(this Mock<T> mock, Expression<Action<T>> expression) where T : class
        {
            mock.Verify(expression, Times.Once);
        }

        /// <summary>
        /// Verifies that a mock method was never called
        /// </summary>
        /// <typeparam name="T">Type of the mock</typeparam>
        /// <param name="mock">Mock object</param>
        /// <param name="expression">Expression to verify</param>
        public static void VerifyNeverCalled<T>(this Mock<T> mock, Expression<Action<T>> expression) where T : class
        {
            mock.Verify(expression, Times.Never);
        }
    }
} 