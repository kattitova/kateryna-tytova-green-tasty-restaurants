# Vendored skills

These skills come from external repositories. Each one was reviewed before it was copied, then adapted to this repo. Each adapted `SKILL.md` starts with an HTML comment that names the source commit and lists the changes.

| Skill | Source | Commit | License | Adapted |
|---|---|---|---|---|
| `ub-mode` | https://github.com/unclebob/ub-mode | `57a8bdd28e45` | No license file upstream. The README tells users to copy `SKILL.md` into their agent's skills folder. | Tool sections map to the tools in this repo. "Commit and Push" follows the repo branch rules. |
| `asd-ste100` | https://github.com/danyuchn/asd-ste100-skill | `833810d38666` | MIT (`asd-ste100/LICENSE`) | Description covers Markdown in this repo. Added "Use in this repo". `scripts/`, `references/`, `examples/` unchanged. |
| `dotnet-best-practices` | https://github.com/github/awesome-copilot/tree/main/skills/dotnet-best-practices | `61fa1fa8fcc6` | MIT | Rewritten for the Lambda + DynamoDB architecture. |
| `csharp-xunit` | https://github.com/github/awesome-copilot/tree/main/skills/csharp-xunit | `61fa1fa8fcc6` | MIT | Added AutoFixture and Moq idioms from `Tests/`. |
| `csharp-async` | https://github.com/github/awesome-copilot/tree/main/skills/csharp-async | `61fa1fa8fcc6` | MIT | No `ConfigureAwait(false)`. Fixed the `Task.FromException` rule. Added Lambda rules. |
| `angular-developer` | https://github.com/angular/angular/tree/main/skills/dev-skills/angular-developer | `fdb4517612cf` | MIT (`angular-developer/LICENSE`) | Added "Project overrides". Removed "Creating New Projects". `references/` unchanged. |

## How to update a skill

1. Clone the source repository into a temporary folder. Read the new `SKILL.md` and every script before you copy it.
2. Compare it with the upstream commit in the table. Apply the upstream changes to the adapted file by hand. Keep the project adaptations.
3. Update the commit in this table and in the comment at the top of `SKILL.md`.
4. Do not copy upstream skills that this repo does not use.
