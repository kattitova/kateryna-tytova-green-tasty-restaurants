---
paths:
  - "backend/**"
---

# Backend rules (.NET Lambda)

Area guide: `backend/restaurant-backend-app/CLAUDE.md`.

Load these skills before you write or review backend code:

| Task | Skills |
|---|---|
| Write or change C# code | `ub-mode`, `dotnet-best-practices`, `csharp-async` |
| Write or change tests | `csharp-xunit` |
| Change `docs/*.md`, `README.md`, `local-setup-guide.md` | `asd-ste100` (STE-flavored mode) |

Hard rules:

1. Write the failing xUnit test before you change a service. In the `/orchestrate` pipeline, `test-writer` writes it and `implementer` never edits `Tests/**`.
2. A new or changed endpoint updates `RouteRegistry.cs` and `docs/ApiRoutes.md` in the same change. Tell the user which frontend model must change.
3. Run `dotnet build restaurant-backend-app.sln` and `dotnet test Tests/APIHandler.Tests.csproj` before you report "done". Report the pass/fail totals.
4. Do not add NuGet packages without the user's approval.
5. Do not run `syndicate deploy` or `syndicate clean` unless the user asks.
6. A change to `deployment_resources.json` or `lambda_config.json` changes AWS resources. Ask `infra-reviewer` to review it.
