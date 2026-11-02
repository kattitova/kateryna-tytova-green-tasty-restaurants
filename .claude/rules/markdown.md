---
paths:
  - "**/*.md"
---

# Markdown rules

1. Write all Markdown in English.
2. Load the `asd-ste100` skill before you write or change Markdown. Use the mode from its "Use in this repo" table.
3. Change only the text that the task needs. Do not restyle a whole existing document unless the user asks.
4. Keep commands, code blocks, file paths, versions and numbers exact. Check each command against the code or by running it.
5. Run `python .claude/skills/asd-ste100/scripts/ste-lint.py <file>` on the files that you changed.
6. Do not edit files under `.claude/skills/*/references/`. They are upstream content. Put project changes in the skill's `SKILL.md`.
