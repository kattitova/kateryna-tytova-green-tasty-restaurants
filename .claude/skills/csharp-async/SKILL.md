---
name: csharp-async
description: 'Best practices for C# async programming in the backend Lambdas (AWS SDK, DynamoDB, Cognito, S3, SQS). Use when you write or review async C# code.'
---

<!--
Source: https://github.com/github/awesome-copilot/tree/main/skills/csharp-async
(commit 61fa1fa8fcc6), MIT License, Copyright GitHub, Inc.
Adapted for marathon2026:
- ConfigureAwait(false): Lambda has no SynchronizationContext and the code base uses it 0 times. Do not add it.
- Fixed the upstream line about Task.FromException: it applies only to non-async Task-returning methods.
- Added Lambda-specific rules (ILambdaContext, container reuse, parallel AWS calls).
-->

# C# Async Programming Best Practices (Lambda)

Your goal is to follow best practices for asynchronous programming in C# in this solution.

## Naming Conventions

- Use the `Async` suffix for all async methods (`GetLocationByIdAsync`).
- Match method names with their synchronous counterparts when applicable.

## Return Types

- Return `Task<T>` when the method returns a value.
- Return `Task` when the method returns no value.
- Use `ValueTask<T>` only if a profiler shows that allocations matter. Not by default.
- Never use `async void`. Lambda handlers and `IApiRequestHandler.HandleRequest` return `Task<APIGatewayProxyResponse>`.

## Exception Handling

- An exception thrown in an `async` method goes into the returned task. `await` throws it again. Throw domain exceptions normally.
- In a method that returns `Task` but is **not** `async`, use `Task.FromException(...)` instead of `throw`. This keeps the behaviour the same for callers.
- Do not swallow exceptions. Let unexpected ones reach `ExceptionHandler` (500).
- Do not add `ConfigureAwait(false)`. Lambda has no synchronization context, so it only adds noise.

## Performance

- Use `Task.WhenAll()` for independent AWS calls, for example the capacity and the rating of a location. Do not use it for calls that depend on each other or that write the same item.
- Keep parallel DynamoDB requests small (a few at a time). Large fan-outs hit throttling. Use batch APIs (`BatchGet`, `BatchWrite`) for many items.
- Return the task directly (no `async`/`await`) only for one-line pass-through methods without `using` or `try`.
- Accept a `CancellationToken` in new repository methods that make AWS calls and pass it to the SDK. The current interfaces do not have it. Do not change existing interfaces only to add it.

## Common Pitfalls

- Never use `.Wait()`, `.Result`, or `.GetAwaiter().GetResult()`. The code has 3 such calls. Do not add more, and remove them in code that you touch.
- Do not mix blocking and async code.
- Always await methods that return `Task`. A task that is not awaited can be frozen when the Lambda invocation ends, so its work is lost.
- Do not start background work that must finish after the response is returned. Lambda freezes the container after the handler returns. Use SQS (the existing reports queue pattern) for deferred work.

## Implementation Patterns

- Use `IAsyncEnumerable<T>` for paged DynamoDB queries only when the caller streams the results. The usual pattern is to load a page and return a list.
- Keep handlers async end to end: handler → service → repository → AWS SDK.
