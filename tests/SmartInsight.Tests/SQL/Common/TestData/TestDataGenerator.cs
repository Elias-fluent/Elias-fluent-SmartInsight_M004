using System;
using System.Collections.Generic;

namespace SmartInsight.Tests.SQL.Common.TestData
{
    /// <summary>
    /// Utility for generating test data for tests
    /// </summary>
    public static class TestDataGenerator
    {
        private static readonly Random _random = new Random();
        
        /// <summary>
        /// Generate a random string of the specified length
        /// </summary>
        /// <param name="length">Length of the string to generate</param>
        /// <returns>Random string</returns>
        public static string GenerateRandomString(int length = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            
            var stringChars = new char[length];
            
            for (int i = 0; i < length; i++)
            {
                stringChars[i] = chars[_random.Next(chars.Length)];
            }
            
            return new string(stringChars);
        }
        
        /// <summary>
        /// Generate a random email address
        /// </summary>
        /// <returns>Random email address</returns>
        public static string GenerateRandomEmail()
        {
            return $"{GenerateRandomString(8)}@{GenerateRandomString(5)}.com";
        }
        
        /// <summary>
        /// Generate a random integer within the specified range
        /// </summary>
        /// <param name="min">Minimum value (inclusive)</param>
        /// <param name="max">Maximum value (exclusive)</param>
        /// <returns>Random integer</returns>
        public static int GenerateRandomInt(int min = 0, int max = int.MaxValue)
        {
            return _random.Next(min, max);
        }
        
        /// <summary>
        /// Generate a random double within the specified range
        /// </summary>
        /// <param name="min">Minimum value (inclusive)</param>
        /// <param name="max">Maximum value (exclusive)</param>
        /// <returns>Random double</returns>
        public static double GenerateRandomDouble(double min = 0, double max = 1)
        {
            return min + (_random.NextDouble() * (max - min));
        }
        
        /// <summary>
        /// Generate a random boolean
        /// </summary>
        /// <returns>Random boolean</returns>
        public static bool GenerateRandomBool()
        {
            return _random.Next(2) == 1;
        }
        
        /// <summary>
        /// Generate a random DateTime within the specified range
        /// </summary>
        /// <param name="minYear">Minimum year (inclusive)</param>
        /// <param name="maxYear">Maximum year (inclusive)</param>
        /// <returns>Random DateTime</returns>
        public static DateTime GenerateRandomDateTime(int minYear = 2000, int maxYear = 2030)
        {
            int year = _random.Next(minYear, maxYear + 1);
            int month = _random.Next(1, 13);
            int day = _random.Next(1, DateTime.DaysInMonth(year, month) + 1);
            int hour = _random.Next(0, 24);
            int minute = _random.Next(0, 60);
            int second = _random.Next(0, 60);
            
            return new DateTime(year, month, day, hour, minute, second);
        }
        
        /// <summary>
        /// Generate a list of random items using the provided generator function
        /// </summary>
        /// <typeparam name="T">Type of items to generate</typeparam>
        /// <param name="count">Number of items to generate</param>
        /// <param name="generator">Function to generate each item</param>
        /// <returns>List of generated items</returns>
        public static List<T> GenerateRandomList<T>(int count, Func<int, T> generator)
        {
            var list = new List<T>(count);
            
            for (int i = 0; i < count; i++)
            {
                list.Add(generator(i));
            }
            
            return list;
        }
        
        /// <summary>
        /// Select a random item from the provided list
        /// </summary>
        /// <typeparam name="T">Type of items in the list</typeparam>
        /// <param name="items">List of items</param>
        /// <returns>Random item from the list</returns>
        public static T SelectRandomItem<T>(IList<T> items)
        {
            if (items == null || items.Count == 0)
            {
                throw new ArgumentException("The list cannot be null or empty", nameof(items));
            }
            
            return items[_random.Next(items.Count)];
        }
        
        /// <summary>
        /// Generate a random GUID
        /// </summary>
        /// <returns>Random GUID</returns>
        public static Guid GenerateRandomGuid()
        {
            return Guid.NewGuid();
        }
    }
} 