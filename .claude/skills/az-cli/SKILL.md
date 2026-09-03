---
name: az-cli
description: Azure CLI (`az`) reference for the squad's Aspire-deployed stack — subscription/resource-group hygiene, Container Apps and App Service inspection (logs, scale, env), Key Vault read patterns, Postgres flexible-server diagnostics, OIDC federation for GitHub Actions, and the production-vs-staging discipline. Use when inspecting deployed resources, pulling logs, rotating secrets, or scripting deployment state. Do not use for `azd up`/`azd deploy` (that's the Aspire deployment surface, separate workflow) or for IaC authoring (use the bicep-iac skill).
---

# `az` — Azure CLI for the squad

The squad's deployment target is Azure via `azd` (Aspire's deployment surface). `azd` handles the deployment lifecycle; `az` is what you reach for once things are deployed and you need to inspect, diagnose, or surgically intervene.

Generic `az` documentation is exhaustive at `az <group> --help` and Claude knows the syntax. This skill captures the squad's conventions and the commands worth memorising.

## Subscription and tenant hygiene

The single most common failure mode is "ran a command in the wrong subscription". Always confirm before destructive operations.

```bash
az account show --query "{name:name, id:id, tenant:tenantId}" -o table
az account list --query "[].{name:name, id:id, default:isDefault}" -o table

# Switch subscription
az account set --subscription "<sub-name-or-id>"
```

The squad's subscriptions follow the convention `<product>-<env>` — e.g. `articles-prod`, `articles-staging`, `articles-dev`. Treat anything ending in `-prod` as no-experimentation.

## Resource groups

The squad's resource groups follow `rg-<product>-<env>-<region>` — e.g. `rg-articles-prod-westeu`. The `azd` deployment creates them; `az group create` is rare in day-to-day work.

```bash
az group list --query "[].{name:name, location:location}" -o table
az resource list -g <rg> --query "[].{name:name, type:type}" -o table

# Cost (last 30 days) — useful when something is unexpectedly expensive
az consumption usage list --start-date $(date -u -d '30 days ago' +%Y-%m-%d) \
                          --end-date $(date -u +%Y-%m-%d) \
  | jq '[.[] | select(.instanceName != null)] | group_by(.instanceName) | map({name:.[0].instanceName, cost: ([.[].pretaxCost | tonumber] | add)})'
```

## Container Apps (Aspire's default target)

Aspire deploys most workloads to Azure Container Apps. The diagnostic flow is: list → inspect → logs → revisions.

```bash
# List apps in the env
az containerapp list -g <rg> --query "[].{name:name, fqdn:properties.configuration.ingress.fqdn, replicas:properties.template.scale}" -o table

# Single app detail
az containerapp show -g <rg> -n <app> --query "properties.template.containers[0].{image:image, env:env, resources:resources}"

# Live logs (streaming)
az containerapp logs show -g <rg> -n <app> --follow --tail 100

# System logs (platform events: scale, restart, image pull failures)
az containerapp logs show -g <rg> -n <app> --type system --tail 50

# Revisions — useful when "it worked before"
az containerapp revision list -g <rg> -n <app> -o table
az containerapp revision show -g <rg> -n <app> --revision <revision>
az containerapp revision activate -g <rg> -n <app> --revision <previous-rev>  # rollback

# Scale rules
az containerapp show -g <rg> -n <app> --query "properties.template.scale"
```

**Scaling.** Aspire ships sane defaults (HTTP-rule, min 1, max 10). When you need to tune:

```bash
az containerapp update -g <rg> -n <app> \
  --min-replicas 2 \
  --max-replicas 20 \
  --scale-rule-name http \
  --scale-rule-type http \
  --scale-rule-metadata concurrentRequests=50
```

**Don't** edit env vars via `az containerapp update --set-env-vars` for production. Those should come from `azd` + bicep + Key Vault refs, not be drift-set imperatively. For ephemeral debugging in dev/staging, fine.

## App Service (legacy / non-Aspire workloads)

Some squad services still run on App Service Linux. The same shape, different commands:

```bash
az webapp list -g <rg> -o table
az webapp log tail -g <rg> -n <app>                  # streaming
az webapp config show -g <rg> -n <app>
az webapp config appsettings list -g <rg> -n <app>
az webapp restart -g <rg> -n <app>                   # last resort
az webapp deployment slot list -g <rg> -n <app>      # check slot config
az webapp deployment slot swap -g <rg> -n <app> --slot staging --target-slot production
```

Slot swap is the squad's preferred zero-downtime deploy for App Service workloads. The convention: deploy to `staging` slot, smoke test, swap.

## Key Vault

```bash
# List secrets (names only — never the values in transit logs)
az keyvault secret list --vault-name <kv> --query "[].name" -o tsv

# Get a secret value (use sparingly; prefer KV references in app config)
az keyvault secret show --vault-name <kv> -n <secret> --query value -o tsv

# Set a secret (rotation; production usually goes via the rotation pipeline)
az keyvault secret set --vault-name <kv> -n <secret> --value "<value>"

# Soft-delete recovery
az keyvault secret list-deleted --vault-name <kv>
az keyvault secret recover --vault-name <kv> -n <secret>
```

**Squad rule:** never echo secrets to terminal where the line is logged. `az keyvault secret show ... -o tsv` is fine for one-shot capture into env, e.g. `export FOO=$(az keyvault secret show ... -o tsv)`. Avoid `echo $FOO`.

The squad's KV access policies are set via bicep, not `az keyvault set-policy`. If a developer needs read access to a vault, that's a bicep change + PR, not a CLI imperative grant.

## Postgres Flexible Server

The squad's primary OLTP store. EF Core runs migrations via `dotnet ef`; `az` is for ops:

```bash
az postgres flexible-server list -g <rg> -o table
az postgres flexible-server show -g <rg> -n <server>

# Logs
az postgres flexible-server server-logs list -g <rg> -n <server>
az postgres flexible-server server-logs download -g <rg> -n <server> -f <log-file> -d ./

# Configuration (timeouts, log levels)
az postgres flexible-server parameter list -g <rg> -s <server> --query "[?source=='user-override']" -o table
az postgres flexible-server parameter set -g <rg> -s <server> -n log_min_duration_statement -v 1000

# Firewall (for ad-hoc dev access)
az postgres flexible-server firewall-rule create -g <rg> -n <server> \
  --rule-name temp-laptop --start-ip-address $(curl -s ifconfig.me) --end-ip-address $(curl -s ifconfig.me)

# Backup / restore
az postgres flexible-server backup list -g <rg> -n <server> -o table
az postgres flexible-server restore -g <rg> -n <new-name> --source-server <source-name> --restore-time "2026-05-06T10:00:00Z"
```

For schema migrations, **EF Core is the source of truth** — `dotnet ef database update` against a connection string from KV. Don't apply DDL manually via `psql` in production.

## OIDC federation (GitHub Actions ↔ Azure)

The squad uses OIDC for GitHub Actions auth — no long-lived secrets in repo settings. When setting this up for a new repo:

```bash
# Create the AAD app + service principal
az ad sp create-for-rbac --name "<repo>-gh-oidc" --role contributor \
  --scopes /subscriptions/<sub>/resourceGroups/<rg> \
  --json-auth

# Add the federated credential
az ad app federated-credential create --id <app-id> --parameters '{
  "name": "<repo>-main",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:<owner>/<repo>:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'

# For PR runs (e.g. a CI workflow that deploys an Azure preview environment)
az ad app federated-credential create --id <app-id> --parameters '{
  "name": "<repo>-pr",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:<owner>/<repo>:pull_request",
  "audiences": ["api://AzureADTokenExchange"]
}'
```

In GitHub Actions, set `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` as repo secrets and use `azure/login@v2` with `enable-AzPSSession: true`.

## Diagnostic patterns

**"My app is returning 500s in prod, where do I start?"**

```bash
# 1. Is the container app healthy?
az containerapp show -g <rg> -n <app> --query "properties.{provisioning:provisioningState, latest:latestRevisionName, latestActive:latestReadyRevisionName}"

# 2. Recent system events (image pull, restart, scale)
az containerapp logs show -g <rg> -n <app> --type system --tail 100 | grep -iE "error|fail|restart"

# 3. App logs (streaming)
az containerapp logs show -g <rg> -n <app> --follow --tail 50

# 4. Application Insights / Log Analytics (the squad's OTLP collector lands here)
az monitor log-analytics query \
  -w <workspace-id> \
  --analytics-query 'AppRequests | where TimeGenerated > ago(1h) | where ResultCode startswith "5" | summarize count() by bin(TimeGenerated, 5m), Url'
```

**"Costs spiked yesterday."**

```bash
az consumption usage list \
  --start-date $(date -u -d '7 days ago' +%Y-%m-%d) \
  --end-date $(date -u +%Y-%m-%d) \
  | jq -r '[.[] | {date:.date, name:.instanceName, cost:.pretaxCost}] | group_by(.name) | map({name:.[0].name, total:([.[].cost | tonumber] | add)}) | sort_by(.total) | reverse | .[0:10] | .[] | "\(.total | tostring | .[0:6])  \(.name)"'
```

## Failure modes and footguns

- **`az group delete`** — refuses, even when explicitly asked. RG deletion is irreversible and can wipe months of state. Direct the user to the portal where the typed-confirmation step matches the consequence.
- **`az resource delete`** without `--no-wait` — usually fine; with `--no-wait` you don't see whether it actually worked.
- **`az login` in CI** — never. Use OIDC (above). If you see `az login` in a workflow file, that's a finding, file it as `/decide` material.
- **Setting subscription via `--subscription` flag vs. `az account set`** — `--subscription` is per-command and safer when scripting; `az account set` mutates the default for the whole shell session. Prefer per-command for scripts.
- **Wrong tenant.** `az login --tenant <id>` if you have access to multiple tenants. Symptom: `az account show` shows the right name but `az ad ...` returns "principal not found".
