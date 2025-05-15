using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SmartInsight.Tests.SQL.Common.Utilities
{
    /// <summary>
    /// Extension methods for assertions to make tests more readable
    /// </summary>
    public static class AssertionExtensions
    {
        /// <summary>
        /// Asserts that a value is not null and returns it for further assertions
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="value">Value to check</param>
        /// <param name="message">Optional message to include in the exception if the assertion fails</param>
        /// <returns>The non-null value</returns>
        /// <exception cref="Xunit.Sdk.XunitException">Thrown when the value is null</exception>
        public static T ShouldNotBeNull<T>(this T? value, string? message = null)
        {
            Assert.NotNull(value);
            return value;
        }
        
        /// <summary>
        /// Asserts that a value is null
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="value">Value to check</param>
        /// <param name="message">Optional message to include in the exception if the assertion fails</param>
        public static void ShouldBeNull<T>(this T? value, string? message = null)
        {
            Assert.Null(value);
        }
        
        /// <summary>
        /// Asserts that a string is not null or empty
        /// </summary>
        /// <param name="value">String to check</param>
        /// <param name="message">Optional message to include in the exception if the assertion fails</param>
        /// <returns>The non-null, non-empty string</returns>
        /// <exception cref="Xunit.Sdk.XunitException">Thrown when the string is null or empty</exception>
        public static string ShouldNotBeNullOrEmpty(this string? value, string? message = null)
        {
            Assert.NotNull(value);
            Assert.NotEmpty(value);
            return value;
        }
        
        /// <summary>
        /// Asserts that a collection is not null or empty
        /// </summary>
        /// <typeparam name="T">Type of items in the collection</typeparam>
        /// <param name="collection">Collection to check</param>
        /// <param name="message">Optional message to include in the exception if the assertion fails</param>
        /// <returns>The non-null, non-empty collection</returns>
        /// <exception cref="Xunit.Sdk.XunitException">Thrown when the collection is null or empty</exception>
        public static IEnumerable<T> ShouldNotBeNullOrEmpty<T>(this IEnumerable<T>? collection, string? message = null)
        {
            Assert.NotNull(collection);
            Assert.NotEmpty(collection);
            return collection;
        }
        
        /// <summary>
        /// Asserts that a collection has exactly the specified number of items
        /// </summary>
        /// <typeparam name="T">Type of items in the collection</typeparam>
        /// <param name="collection">Collection to check</param>
        /// <param name="count">Expected number of items</param>
        /// <param name="message">Optional message to include in the exception if the assertion fails</param>
        /// <returns>The collection</returns>
        /// <exception cref="Xunit.Sdk.XunitException">Thrown when the collection does not have the expected number of items</exception>
        public static IEnumerable<T> ShouldHaveCount<T>(this IEnumerable<T>? collection, int count, string? message = null)
        {
            collection.ShouldNotBeNull(message);
            Assert.Equal(count, collection.Count());
            return collection;
        }
        
        /// <summary>
        /// Asserts that a value equals the expected value
        /// </summary>
        /// <typeparam name="T">Type of the values</typeparam>
        /// <param name="actual">Actual value</param>
        /// <param name="expected">Expected value</param>
        /// <param name="message">Optional message to include in the exception if the assertion fails</param>
        /// <returns>The actual value</returns>
        /// <exception cref="Xunit.Sdk.XunitException">Thrown when the values are not equal</exception>
        public static T ShouldEqual<T>(this T actual, T expected, string? message = null)
        {
            Assert.Equal(expected, actual);
            return actual;
        }
        
        /// <summary>
        /// Asserts that a value is true
        /// </summary>
        /// <param name="value">Value to check</param>
        /// <param name="message">Optional message to include in the exception if the assertion fails</param>
        /// <exception cref="Xunit.Sdk.XunitException">Thrown when the value is not true</exception>
        public static void ShouldBeTrue(this bool value, string? message = null)
        {
            Assert.True(value, message);
        }
        
        /// <summary>
        /// Asserts that a value is false
        /// </summary>
        /// <param name="value">Value to check</param>
        /// <param name="message">Optional message to include in the exception if the assertion fails</param>
        /// <exception cref="Xunit.Sdk.XunitException">Thrown when the value is not false</exception>
        public static void ShouldBeFalse(this bool value, string? message = null)
        {
            Assert.False(value, message);
        }
        
        /// <summary>
        /// Asserts that an action throws an exception of the specified type
        /// </summary>
        /// <typeparam name="TException">Expected exception type</typeparam>
        /// <param name="action">Action that should throw</param>
        /// <param name="message">Optional message to include in the exception if the assertion fails</param>
        /// <returns>The thrown exception for further assertions</returns>
        /// <exception cref="Xunit.Sdk.XunitException">Thrown when the action doesn't throw or throws a different exception type</exception>
        public static TException ShouldThrow<TException>(this Action action, string? message = null) where TException : Exception
        {
            return Assert.Throws<TException>(action);
        }
    }
} 