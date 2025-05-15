using System;
using Moq;
using SmartInsight.Tests.SQL.Common.Utilities;
using Xunit.Abstractions;

namespace SmartInsight.Tests.SQL.Unit
{
    /// <summary>
    /// Base class for unit tests, providing unit test specific functionality
    /// </summary>
    public abstract class UnitTestBase : TestBase
    {
        /// <summary>
        /// Mock repository for creating and tracking mocks
        /// </summary>
        protected MockRepository MockRepository { get; }

        /// <summary>
        /// Initializes a new instance of the UnitTestBase class
        /// </summary>
        /// <param name="outputHelper">XUnit test output helper for logging</param>
        protected UnitTestBase(ITestOutputHelper outputHelper) 
            : base(outputHelper)
        {
            // Create a strict mock repository - will throw for unexpected calls
            MockRepository = new MockRepository(MockBehavior.Strict);
        }

        /// <summary>
        /// Creates a mock object with strict behavior
        /// </summary>
        /// <typeparam name="T">Type to mock</typeparam>
        /// <returns>Mock object</returns>
        protected Mock<T> CreateMock<T>() where T : class
        {
            return MockRepository.Create<T>();
        }

        /// <summary>
        /// Creates a mock object with loose behavior
        /// </summary>
        /// <typeparam name="T">Type to mock</typeparam>
        /// <returns>Mock object</returns>
        protected Mock<T> CreateLoseMock<T>() where T : class
        {
            return new Mock<T>(MockBehavior.Loose);
        }

        /// <summary>
        /// Verify all mocks created through the repository
        /// </summary>
        public override void Dispose()
        {
            try
            {
                // Verify all expectations on mocks were met
                MockRepository.VerifyAll();
            }
            catch (Exception ex)
            {
                LogError("Mock verification failed", ex);
                throw;
            }
            finally
            {
                base.Dispose();
            }
        }
    }
} 