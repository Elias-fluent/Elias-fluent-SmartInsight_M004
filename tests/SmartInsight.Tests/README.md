# SmartInsight Test Framework

This document provides an overview of the SmartInsight test framework and guidelines for writing effective tests.

## Test Project Structure

```
tests/SmartInsight.Tests/
├── SQL/
│   ├── Common/               # Common test utilities and helpers
│   │   ├── Mocks/            # Mock helpers and utilities
│   │   ├── TestData/         # Test data generators
│   │   └── Utilities/        # General test utilities (base classes, assertions)
│   ├── Integration/          # Integration tests
│   ├── Performance/          # Performance tests
│   ├── Regression/           # Regression tests
│   ├── Security/             # Security tests
│   └── Unit/                 # Unit tests
│       ├── Generators/       # Tests for generator components
│       ├── Logging/          # Tests for logging components
│       ├── Optimizers/       # Tests for optimizer components
│       └── Validators/       # Tests for validator components
└── TestResults/             # Generated test results and coverage reports
    └── Coverage/            # Code coverage reports
```

## Testing Framework Components

### 1. Base Test Classes

- **TestBase**: Base class for all tests providing common functionality like logging
- **UnitTestBase**: Specialized base class for unit tests with mocking capabilities
- **IntegrationTestBase**: Specialized base class for integration tests with async setup/teardown

### 2. Utility Classes

- **AssertionExtensions**: Fluent assertion methods to make tests more readable
- **TestDataGenerator**: Utility for generating random test data
- **MockHelper**: Helper methods for common mocking scenarios

## Writing Tests

### Unit Tests

Unit tests should inherit from `UnitTestBase` and follow this general structure:

```csharp
public class MyServiceTests : UnitTestBase
{
    public MyServiceTests(ITestOutputHelper outputHelper) 
        : base(outputHelper)
    {
    }

    [Fact]
    public void MyMethod_Scenario_ExpectedBehavior()
    {
        // Arrange
        var mockDependency = CreateMock<IDependency>();
        mockDependency.Setup(d => d.SomeMethod()).Returns("result");
        
        var sut = new MyService(mockDependency.Object);
        
        // Act
        var result = sut.MyMethod();
        
        // Assert
        result.ShouldEqual("expected result");
        mockDependency.VerifyCalledOnce(d => d.SomeMethod());
    }
}
```

### Integration Tests

Integration tests should inherit from `IntegrationTestBase` and follow this general structure:

```csharp
public class MyIntegrationTests : IntegrationTestBase
{
    public MyIntegrationTests(ITestOutputHelper outputHelper) 
        : base(outputHelper)
    {
    }

    [Fact]
    public async Task MyIntegration_Scenario_ExpectedBehavior()
    {
        // Arrange
        var service = new RealService();
        
        // Act
        var result = await service.DoSomethingAsync();
        
        // Assert
        result.ShouldNotBeNull();
        // Additional assertions...
    }
    
    protected override async Task SetupIntegrationTestAsync()
    {
        await base.SetupIntegrationTestAsync();
        
        // Initialize resources for integration test
        // For example: database connections, test servers, etc.
    }
    
    protected override async Task CleanupIntegrationTestAsync()
    {
        // Clean up resources used in integration tests
        
        await base.CleanupIntegrationTestAsync();
    }
}
```

## Test Naming Convention

Follow this pattern for test method names:

```
[MethodUnderTest]_[Scenario]_[ExpectedBehavior]
```

Examples:
- `GetUser_UserExists_ReturnsUser`
- `ProcessData_NullInput_ThrowsArgumentNullException`
- `CalculateTotal_EmptyCart_ReturnsZero`

## Assertion Best Practices

Use the fluent assertion extensions to make your tests more readable:

```csharp
// Instead of:
Assert.NotNull(result);
Assert.Equal("expected", result);

// Use:
result.ShouldNotBeNull().ShouldEqual("expected");
```

Common assertion methods:
- `ShouldBeNull()`
- `ShouldNotBeNull()`
- `ShouldEqual(expected)`
- `ShouldBeTrue()`
- `ShouldBeFalse()`
- `ShouldNotBeNullOrEmpty()` (for strings and collections)
- `ShouldHaveCount(count)` (for collections)

## Mocking

Use the `CreateMock<T>()` method from `UnitTestBase` to create strict mocks:

```csharp
var mockService = CreateMock<IService>();
mockService.Setup(s => s.GetData()).Returns("test data");
```

For asynchronous methods:

```csharp
mockService.Setup(s => s.GetDataAsync()).Returns(Task.FromResult("test data"));
mockService.Setup(s => s.ProcessAsync()).Returns(Task.CompletedTask);
```

## Running Tests

### Run All Tests

```bash
dotnet test
```

### Run Specific Tests

```bash
dotnet test --filter "FullyQualifiedName~MyServiceTests"
```

### Run Tests with Code Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

Code coverage reports are generated in `TestResults/Coverage/html/`.

## Continuous Integration

Tests are automatically run as part of the CI pipeline on pull requests and pushes to main branches.

## Additional Resources

- [xUnit Documentation](https://xunit.net/docs/getting-started/netcore/cmdline)
- [Moq Documentation](https://github.com/moq/moq4)
- [Code Coverage with Coverlet](https://github.com/coverlet-coverage/coverlet) 