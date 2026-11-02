---
name: dotnet-best-practices
description: 'Ensure .NET/C# code in backend/restaurant-backend-app (AWS Lambda, .NET 10, DynamoDB, Cognito) meets the best practices of this solution. Use when you write, change, or review backend C# code.'
---

<!--
Source: https://github.com/github/awesome-copilot/tree/main/skills/dotnet-best-practices
(commit 61fa1fa8fcc6), MIT License, Copyright GitHub, Inc.
Adapted for marathon2026. The upstream skill describes a console/Semantic Kernel app.
Removed: Command Handler base classes, ResourceManager localization, Semantic Kernel,
IConfiguration/appsettings.json, MSTest + FluentAssertions, mandatory XML docs on every public member,
ConfigureAwait(false), scoped/transient lifetimes.
Replaced with the patterns that this codebase uses today (measured in dnapp/lambdas/api-handler).
-->

# .NET/C# Best Practices (restaurant-backend-app)

Your task is to make sure that .NET/C# code follows the practices of **this** solution. When the existing code and this file disagree, check the code first. Copy the dominant pattern. Do not "fix" the old code in a change that has a different purpose.

## Solution layout

- `dnapp/lambdas/api-handler` (`APIHandler.csproj`): one Lambda for the whole REST API behind API Gateway.
- `dnapp/lambdas/reports-handler`, `dnapp/lambdas/report-sender`: report Lambdas (SQS / EventBridge).
- `Tests/APIHandler.Tests.csproj`: xUnit tests for `api-handler` services.
- `docs/ApiRoutes.md`: the API contract. The frontend treats it as the source of truth.
- Target framework: `net10.0`. `<Nullable>enable</Nullable>`.

## Request flow and layering

```
Function.FunctionHandler → RouteRegistry (route table) → IApiRequestHandler.HandleRequest
    → I<Domain>Service → I<Domain>Repository → IDynamoDBContext / AWS SDK client
```

- **Handler** (`Handlers/<Domain>/<Verb><Noun>Handler.cs`): parses the `APIGatewayProxyRequest` (path params, body, user claims), calls one service, maps the result to a response with `ResponseBuilder`. No business rules here.
- **Service** (`Services/<Domain>/I<Domain>Service.cs` + implementation): business rules. Unit-tested.
- **Repository** (`Repositories/<Domain>/`): DynamoDB and other AWS calls only.
- **Entities** (`Entities/`) map to DynamoDB tables. **Models** (`Models/<Domain>/`) are request/response DTOs. **Mappers** (`Mappers/`) convert between them.
- Some old handlers call `IDynamoDBContext` directly. New code goes through a service.

## Adding an endpoint (checklist)

1. Add the DTOs in `Models/<Domain>/`.
2. Add the service method to the interface and the implementation. Write the xUnit test first (see the `csharp-xunit` skill).
3. Add the repository method if new data access is necessary.
4. Add the handler that implements `IApiRequestHandler`.
5. Register the route in `Routing/RouteRegistry.cs` with `new RouteHandler(new Route(HttpMethods.X, "/path/{id}"), ActivatorUtilities.CreateInstance<YourHandler>(provider))`. Put more specific paths before generic ones in the same group.
6. Register new services and repositories in `RouteRegistry.BuildRoutes` with `AddSingleton<IFoo, Foo>()`.
7. Document the route, request and response in `docs/ApiRoutes.md`.
8. If the endpoint needs a new table, queue, bucket or permission, change `deployment_resources.json` (Syndicate) or `infra/terraform` (shared infra). Ask which one owns it.

## Dependency injection

- `Microsoft.Extensions.DependencyInjection` is built once per Lambda container in `RouteRegistry.BuildRoutes`.
- **All registrations are singletons.** A Lambda container serves one request at a time but lives across many requests. A service must keep **no per-request state** in fields. Pass request data as method arguments.
- Constructor style: both styles exist (about 67 classes use constructor + `?? throw new ArgumentNullException(nameof(x))`, 33 use primary constructors). New classes may use primary constructors. Do not convert existing classes in unrelated changes.
- Depend on interfaces (`I<Name>`) so tests can mock them with Moq.

## Configuration

- Resource names come from Lambda environment variables through `Constants/ResourceNames.cs`. Add a new property there that throws when the variable is missing. Do not read `Environment.GetEnvironmentVariable` elsewhere.
- Set the variable in the Lambda's `lambda_config.json` / `deployment_resources.json`.
- Never hardcode an account ID, ARN, region, bucket name or table name.

## Errors and responses

- Domain errors are specific exceptions in `Exceptions/` (for example `ValidationException`, `UserNotFoundException`, `DishNotFoundException`). Throw them from services with a message that the client can show.
- Handlers catch the domain exceptions that they expect and map them to 400 / 403 / 404 / 409 with `ResponseBuilder.CreateErrorResponse` or `CreateErrorCodeResponse`.
- Anything uncaught reaches `Services/ExceptionHandler.cs` and becomes a 500. Do not catch `Exception` in a handler only to return 500. That is already done.
- Build all responses with `Helpers/ResponseBuilder` so the CORS headers stay consistent.

## Validation and security

- Validate input at the handler boundary: path params, JSON body (`RequestBodyHelper`, `DataAnnotationsValidator`, `DataValidationHelper`).
- Get the caller identity and role from `UserContextHelper` / `RoleHelper`. Never trust a user ID or role from the request body.
- Check authorization in the service or handler before any write.
- DynamoDB: use `IDynamoDBContext` with `DynamoConfig` or SDK expressions with attribute values. Never build expressions with string concatenation of user input.

## Logging

- Use `context.Logger` (`ILambdaContext`). `ILogger<T>` is not used in this solution.
- Never log request bodies, tokens, passwords or personal data. `Function.cs` keeps the body log commented out on purpose.

## Async

- See the `csharp-async` skill. All AWS SDK calls are async. Use `await` all the way.

## Code quality

- SOLID, meaningful domain names, small focused methods.
- XML doc comments are not required (7 of 242 files have them). Add them only to explain a non-obvious contract.
- Do not add new compiler warnings. The build already has nullable (`CS86xx`) and obsolete-API (`CS0618`) warnings. Do not add to them, and fix the ones in code that you touch.
- Dispose `IDisposable` resources (`using`), for example streams in report generation.

## Verify before you say "done"

```bash
cd backend/restaurant-backend-app
dotnet build restaurant-backend-app.sln
dotnet test Tests/APIHandler.Tests.csproj
```

Report the test totals (all must pass, 0 failed). Handler routing and DynamoDB behaviour cannot be checked locally. Say that a deploy is necessary to check them.
