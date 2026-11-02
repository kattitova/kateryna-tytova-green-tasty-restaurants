---
paths:
  - "infra/**"
  - "**/*.tf"
---

# Terraform rules

Area guide: `infra/terraform/CLAUDE.md`.

Load these skills before you change Terraform:

| Task | Skills |
|---|---|
| Change `.tf` files | `ub-mode` (Make it Work → Make it Right, small verifiable steps) |
| Change Markdown in `infra/` | `asd-ste100` (STE-flavored mode) |

Hard rules:

1. Run `terraform fmt -recursive` and `terraform validate` after each change. Run `terraform plan` only if the user has AWS credentials configured, and report the add/change/destroy counts.
2. Never run `terraform apply` or `terraform destroy` unless the user asks in this session.
3. A plan that destroys or replaces a stateful resource (S3 bucket, Cognito user pool, SQS queue) is a stop condition. Report it to the user before you continue.
4. Keep names in `locals.tf` in sync with the Syndicate configs in `backend/restaurant-backend-app`.
5. Pin provider versions. Do not change the `~> 5.0` constraint or `.terraform.lock.hcl` in an unrelated change.
6. Ask `infra-reviewer` to review every Terraform diff.
