# restaurant-backend-app — .NET 10 AWS Lambda backend

REST API and report jobs for the restaurant app. Deployed with AWS Syndicate. Data in DynamoDB, users in Cognito, images in S3, report jobs through SQS and EventBridge.

**There is no local host.** No Docker, no LocalStack, no `dotnet run`. Unit tests cover services with mocked dependencies. Routing, DynamoDB queries, Cognito and IAM behaviour can be checked only after `syndicate deploy` to a real AWS account. Say this when you report a change that depends on them.

## Commands

Run from `backend/restaurant-backend-app`:

| Command | Notes |
|---|---|
| `dotnet build restaurant-backend-app.sln` | Builds all three Lambdas and the tests. Existing warnings: nullable `CS86xx`, obsolete QuestPDF `CS0618`, `xUnit1006`. Do not add new ones. |
| `dotnet test Tests/APIHandler.Tests.csproj` | Baseline: 141 passed, 0 failed. |
| `dotnet test Tests/APIHandler.Tests.csproj --collect:"XPlat Code Coverage"` | Coverage through coverlet. |
| `syndicate build` / `syndicate deploy` | Deploy. Only when the user asks. Needs the Syndicate venv and AWS credentials (`local-setup-guide.md`). |

## Structure

```
dnapp/lambdas/
  api-handler/            one Lambda for the whole REST API (APIHandler.csproj)
    Function.cs           entry point: route lookup, catch-all → ExceptionHandler (500)
    Routing/              RouteRegistry.cs (DI container + route table), RouteHandler.cs
    Handlers/<Domain>/    one IApiRequestHandler per endpoint
    Services/<Domain>/    business rules, I<Name>Service + implementation
    Repositories/<Domain>/ DynamoDB / Cognito / S3 access
    Entities/             DynamoDB table models
    Models/<Domain>/      request/response DTOs
    Mappers/              Entity ↔ DTO
    Exceptions/           domain exceptions (ValidationException, *NotFoundException, …)
    Helpers/              ResponseBuilder, RequestBodyHelper, UserContextHelper, RoleHelper, validators
    Constants/            ResourceNames (env vars), DynamoConfig, HttpMethods, statuses
    lambda_config.json    Lambda settings and env variables (Syndicate)
    deployment_resources.json  Syndicate resources owned by this Lambda
  reports-handler/        builds report data (SQS)
  report-sender/          sends report e-mails (EventBridge schedule)
Tests/                    xUnit + Moq + AutoFixture tests for api-handler services
docs/ApiRoutes.md         API contract. The frontend depends on it.
deployment_resources.json shared Syndicate resources (tables, API Gateway, …)
```

## Ownership boundaries

- **Syndicate owns:** the 3 Lambdas, API Gateway, 13 DynamoDB tables, one S3 bucket and one IAM policy (`deployment_resources.json`), Lambda settings and the SQS trigger of `reports-handler` (`lambda_config.json`).
- **Terraform owns:** S3 buckets, CloudFront, Cognito user pool, SQS reports queue, EventBridge schedule (`infra/terraform`).
- Before you add a resource, decide which side owns it. Ask the user if it is not clear.

## Rules

Coding rules are in the skills `dotnet-best-practices`, `csharp-async`, `csharp-xunit` (see `.claude/rules/backend.md`). The most important ones:

- Handler → Service → Repository. Business rules live in services and have tests.
- Register every new service and repository as a singleton in `RouteRegistry.BuildRoutes`. Keep no per-request state in fields.
- A new or changed route updates `docs/ApiRoutes.md` in the same change.
- Read resource names through `Constants/ResourceNames.cs`. Never hardcode an account ID, ARN or table name.
- Never log request bodies, tokens or personal data.

## Known debt

- 3 blocking `.Result` calls (`UpdateReservationStatusHandler.cs`, `EditUserHandler.cs`).
- Some handlers call `IDynamoDBContext` directly instead of a service.
- `report-sender` and `reports-handler` have no tests.
- `.syndicate-config-restaurant-backend-app/syndicate.yml` is tracked and contains a machine-specific `project_path` and an artifacts bucket name. Do not add credentials to it.
