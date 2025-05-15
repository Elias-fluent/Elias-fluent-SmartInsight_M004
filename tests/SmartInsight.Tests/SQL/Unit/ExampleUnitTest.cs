using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using SmartInsight.Tests.SQL.Common.Mocks;
using SmartInsight.Tests.SQL.Common.TestData;
using SmartInsight.Tests.SQL.Common.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace SmartInsight.Tests.SQL.Unit
{
    /// <summary>
    /// Example unit test to demonstrate the unit testing framework
    /// </summary>
    public class ExampleUnitTest : UnitTestBase
    {
        /// <summary>
        /// Initializes a new instance of the ExampleUnitTest class
        /// </summary>
        /// <param name="outputHelper">XUnit test output helper for logging</param>
        public ExampleUnitTest(ITestOutputHelper outputHelper) 
            : base(outputHelper)
        {
        }

        [Fact]
        public void ShouldDemonstrateAssertionExtensions()
        {
            // Arrange
            var testString = "test";
            var testList = new List<string> { "one", "two", "three" };
            
            // Act & Assert
            testString.ShouldNotBeNull();
            testString.ShouldEqual("test");
            
            testList.ShouldNotBeNullOrEmpty();
            testList.ShouldHaveCount(3);
            
            true.ShouldBeTrue();
            false.ShouldBeFalse();
            
            // Demonstrate exception assertion for synchronous code
            var exception = Assert.Throws<InvalidOperationException>(() => 
                ThrowTestException());
            
            Assert.Equal("Test exception", exception.Message);
        }
        
        private void ThrowTestException()
        {
            throw new InvalidOperationException("Test exception");
        }
        
        [Fact]
        public async Task ShouldDemonstrateAsyncExceptions()
        {
            // Demonstrate exception assertion for asynchronous code
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await ThrowTestExceptionAsync());
            
            Assert.Equal("Async test exception", exception.Message);
        }
        
        private async Task ThrowTestExceptionAsync()
        {
            await Task.Delay(1); // Simulate some async work
            throw new InvalidOperationException("Async test exception");
        }
        
        [Fact]
        public void ShouldDemonstrateTestDataGenerator()
        {
            // Arrange & Act
            var randomString = TestDataGenerator.GenerateRandomString(15);
            var randomEmail = TestDataGenerator.GenerateRandomEmail();
            var randomInt = TestDataGenerator.GenerateRandomInt(1, 100);
            var randomDate = TestDataGenerator.GenerateRandomDateTime();
            var randomGuid = TestDataGenerator.GenerateRandomGuid();
            
            // Assert
            randomString.ShouldNotBeNullOrEmpty();
            Assert.Equal(15, randomString.Length);
            
            randomEmail.ShouldNotBeNullOrEmpty();
            Assert.Contains("@", randomEmail);
            
            Assert.InRange(randomInt, 1, 99);
            
            Assert.NotEqual(Guid.Empty, randomGuid);
            
            // Log test output
            LogInfo($"Generated random string: {randomString}");
            LogInfo($"Generated random email: {randomEmail}");
            LogInfo($"Generated random int: {randomInt}");
            LogInfo($"Generated random date: {randomDate}");
            LogInfo($"Generated random GUID: {randomGuid}");
        }
        
        [Fact]
        public async Task ShouldDemonstrateMocking()
        {
            // Arrange
            var mockService = CreateMock<IExampleService>();
            
            var input = "test input";
            var expectedResult = "processed test input";
            
            // Use Moq's built-in ReturnsAsync to avoid ambiguity
            mockService
                .Setup(s => s.ProcessDataAsync(input))
                .Returns(Task.FromResult(expectedResult));
                
            mockService
                .Setup(s => s.DoSomethingElse())
                .Returns(Task.CompletedTask);
            
            // Act
            var result = await mockService.Object.ProcessDataAsync(input);
            await mockService.Object.DoSomethingElse();
            
            // Assert
            result.ShouldEqual(expectedResult);
            
            // Verify the mock was called with the expected parameters
            mockService.VerifyCalledOnce(s => s.ProcessDataAsync(input));
            mockService.VerifyCalledOnce(s => s.DoSomethingElse());
            mockService.VerifyNeverCalled(s => s.ProcessDataAsync("wrong input"));
        }
    }
    
    /// <summary>
    /// Example service interface for demonstration purposes
    /// </summary>
    public interface IExampleService
    {
        /// <summary>
        /// Process the input data asynchronously
        /// </summary>
        /// <param name="input">Input data</param>
        /// <returns>Processed result</returns>
        Task<string> ProcessDataAsync(string input);
        
        /// <summary>
        /// Perform some operation with no result
        /// </summary>
        /// <returns>Task representing the operation</returns>
        Task DoSomethingElse();
    }
} 