---
name: csharp-xunit
description: 'Best practices for xUnit unit tests in backend/restaurant-backend-app/Tests, including AutoFixture data-driven tests and Moq mocks. Use when you write, fix, or review C# tests.'
---

<!--
Source: https://github.com/github/awesome-copilot/tree/main/skills/csharp-xunit
(commit 61fa1fa8fcc6), MIT License, Copyright GitHub, Inc.
Adapted for marathon2026: added the real project, packages and AutoFixture idioms used in Tests/.
Removed the advice to add FluentAssertions or NSubstitute (not referenced, ask before adding packages).
-->

# xUnit Best Practices (APIHandler.Tests)

Your goal is to write effective unit tests with xUnit that match the existing tests.

## Project Setup

- Test project: `backend/restaurant-backend-app/Tests/APIHandler.Tests.csproj`. It references `dnapp/lambdas/api-handler/APIHandler.csproj`.
- Packages already referenced: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, `Moq`, `AutoFixture.Xunit2`, `coverlet.collector`. Do not add packages (FluentAssertions, NSubstitute, Shouldly and others) without the user's approval.
- Folder layout mirrors the source: `Tests/Services/<Name>ServiceTests.cs` for `Services/<Domain>/<Name>Service.cs`.
- Run: `dotnet test Tests/APIHandler.Tests.csproj` from `backend/restaurant-backend-app`. Run one class with `--filter "FullyQualifiedName~LocationServiceTests"`.
- `report-sender` and `reports-handler` have no test project yet. Ask before you create one.

## What to test

- Services are the main target. They hold the business rules and depend only on interfaces.
- Handlers are thin. Test them only when they contain branching that maps errors to status codes.
- Repositories talk to DynamoDB. Do not unit-test them with a mocked `IDynamoDBContext` that only repeats the implementation.
- A bug fix needs a test that fails before the fix and passes after it.

## Test Structure

- No test class attributes required.
- Follow Arrange-Act-Assert. A blank line between the three parts is enough. Comments are optional.
- Name tests `MethodName_Scenario_ExpectedBehavior`, for example `UpdateLocationAsync_LocationNotFound_ThrowsValidationException`.
- Create mocks as fields and the system under test in the constructor:

```csharp
public class LocationServiceTests
{
    private readonly Mock<ILocationRepository> _mockLocationRepository = new();
    private readonly LocationService _locationService;

    public LocationServiceTests()
    {
        _locationService = new LocationService(_mockLocationRepository.Object /*, ... */);
    }
}
```

- xUnit creates a new class instance for each test, so fields are not shared between tests.
- Use `IClassFixture<T>` only for expensive, read-only setup.

## Data-driven tests (AutoFixture is the default here)

- `[Theory, AutoData]` (or `[Theory]` + `[AutoData]`) generates the arguments. This is the main style in this project (about 100 uses).
- `[InlineAutoData(value1, value2)]` fixes the first arguments and generates the rest. Use it for status/exception tables.
- `[MemberAutoData(nameof(Source))]` takes rows from a static member.
- Use `[Fact]` when the test has no inputs. A `[Theory]` without parameters causes warning `xUnit1006`. Do not add more of them.
- Override only the generated values that matter for the scenario (`existingLocation.Status = ...`). Leave the rest random. This shows which inputs the behaviour depends on.

## Standard Tests

- One behaviour per test.
- Include only the assertions that the scenario needs.
- Tests must be independent and runnable in any order.
- Do not assert on log output.

## Assertions

- `Assert.Equal` for values, `Assert.Same` for references, `Assert.True`/`Assert.False` for conditions.
- `Assert.Contains`/`Assert.DoesNotContain` for collections and substrings.
- `await Assert.ThrowsAsync<T>(() => sut.MethodAsync(...))` for async exceptions. Then check the message if the client sees it.
- `Assert.Throws<T>` only for synchronous code.

## Mocking with Moq

- `Setup(...).ReturnsAsync(value)` for async methods. Cast null explicitly: `.ReturnsAsync((Location?)null)`.
- Use `Verify(..., Times.Once)` / `Times.Never` for side effects that the behaviour requires (saves, deletes, queue messages). Do not verify every call. That pins the implementation, not the behaviour.
- Use `It.Is<T>(x => ...)` to check the important fields of an argument.
- Match nullability in setups (`ISetup<IFoo, Task<Dish?>>`) to avoid warning `CS8620`.

## Test Organization

- Group tests by service. Put one test class in one file.
- Use `[Trait("Category", "...")]` only if the CI starts to filter by it.
- Use `Skip = "reason"` only with a linked issue. Do not commit skipped tests without a reason.
