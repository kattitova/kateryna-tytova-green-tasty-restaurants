# infra/terraform — shared AWS infrastructure

Terraform config for the resources that the frontend and backend share. One root module, no child modules. Region `us-west-2` (`var.aws_region`). State is in the S3 backend (`backend.tf`) with S3-native locking (`use_lockfile = true`).

| File | Resources |
|---|---|
| `s3.tf` | `images`, `profile_images`, `swagger_ui`, `frontend` buckets with encryption, public access blocks, policies |
| `cloudfront.tf` | frontend distribution + origin access control |
| `cognito.tf` | backend user pool + API client |
| `sqs.tf` | reports queue + DLQ |
| `eventbridge.tf` | weekly reports schedule |
| `iam.tf` | EventBridge Scheduler role and policy |
| `locals.tf` | names of Syndicate-managed resources (Lambdas, API Gateway) |
| `providers.tf` | AWS provider `~> 5.0`, plus a `us_east_1` alias kept for future ACM certificates |

## Commands

Run from `infra/terraform`:

| Command | Safe to run | Notes |
|---|---|---|
| `terraform fmt -check -recursive` | yes | Passes today. |
| `terraform validate` | yes | Passes today. Needs `terraform init` once. |
| `terraform plan` | yes, read-only | Needs AWS credentials and access to the state bucket. |
| `terraform apply` / `destroy` | **only when the user asks** | `.claude/settings.json` asks for confirmation. |

## Rules

Details are in `.claude/rules/terraform.md`. Key points:

- Syndicate owns the Lambdas, API Gateway, DynamoDB tables and the resources in `backend/restaurant-backend-app/deployment_resources.json`. Do not create them here. Refer to them by the derived names in `locals.tf`.
- Every S3 bucket has server-side encryption and a public access block resource.
- Public access today: `frontend` and `profile_images` are private (`frontend` is read through CloudFront OAC). `images` and `swagger_ui` are public on purpose, with `s3:GetObject` only. Do not make another bucket public, and do not widen these policies, without the user's approval.
- IAM policies use specific actions and resource ARNs. No `"*"` actions. No `"*"` resources unless the AWS action does not support resource-level permissions, and then add a comment.
- No secrets, account-specific values or e-mail addresses in committed defaults. Use gitignored `terraform.tfvars`.
- Keep `.terraform.lock.hcl` committed. Never commit `*.tfstate` or `*.tfvars`.

## Known debt

- `variables.tf` and `backend.tf` contain a real account ID as a default and as the state bucket name. `README_HOMEWORK.md` tells each user to replace them. Do not add more account-specific defaults.
