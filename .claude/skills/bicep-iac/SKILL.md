---
name: bicep-iac
description: Bicep reference for the squad's Azure infrastructure — module organisation, parameter file conventions per environment, what-if/deploy patterns, integration with `azd` and Aspire's deployment manifest, secret handling via Key Vault references, the bicep-lint discipline, and the diff-friendly module patterns. Use when authoring or modifying `.bicep` files, reviewing deployment plans, troubleshooting deployment failures, or aligning with `azd` outputs. Do not use for runtime Azure inspection (use az-cli) or for application config (use the relevant project skills).
---

# `bicep` — IaC for the squad

The squad's Azure infrastructure is declared in Bicep, deployed via `azd` (Aspire's deployment surface). Bicep is the squad's IaC because:

1. It's the Microsoft-native ARM authoring language — first-class for new Azure resources, no provider lag.
2. `azd` generates Bicep automatically from Aspire's deployment manifest, then the squad customises it.
3. Diff-friendly — Bicep files read as code, not as JSON noise.

This skill captures the squad's conventions: where files live, how parameters flow, and what `azd`-generated bicep looks like vs. what the squad's hand-rolled bicep looks like.

## Repo layout

```
infra/
├── main.bicep                    # entry point; orchestrates the deployment
├── main.parameters.json          # parameter file template (placeholders)
├── modules/
│   ├── containerapp.bicep        # one Container App with sane defaults
│   ├── postgres.bicep            # Postgres flexible-server with backup config
│   ├── keyvault.bicep            # KV with the squad's standard access policies
│   ├── monitoring.bicep          # Log Analytics + App Insights + diagnostic settings
│   └── network.bicep             # vnet, subnets, NSGs (when private networking is needed)
└── env/
    ├── dev.bicepparam            # dev environment-specific values
    ├── staging.bicepparam
    └── prod.bicepparam
```

`.bicepparam` (typed parameter files) are preferred over `main.parameters.json` (untyped JSON) for new environments. They give compile-time validation against `main.bicep`'s parameters.

## `azd` integration

The squad's deployment flow:
1. Aspire's `Program.cs` declares resources (Container Apps, DBs, KV, etc.).
2. `azd init` (or `azd infra synth`) generates `infra/` from Aspire's manifest.
3. Generated bicep goes into `infra/` and is **committed to the repo**.
4. The squad customises generated files for things Aspire doesn't model (network policies, alerts, diagnostic settings, custom DNS).
5. `azd up` / `azd deploy` applies the bicep + uploads container images.

```bash
# Regenerate from Aspire manifest (after AppHost changes)
azd infra synth

# Diff what azd would change
azd provision --preview

# Just the bicep — without azd's container build/push
az deployment group what-if -g <rg> --template-file infra/main.bicep --parameters infra/env/staging.bicepparam
```

**Squad rule:** never edit `azd`-generated bicep without leaving a comment explaining why. The next `azd infra synth` will overwrite custom changes that aren't preserved by the synth's idempotent regions.

## Authoring patterns

### Module shape

```bicep
// infra/modules/containerapp.bicep

@description('Name of the Container App')
param name string

@description('Container Apps environment resource ID')
param environmentId string

@description('Container image, including registry and tag')
param image string

@description('Min replicas; production should be ≥ 2')
@minValue(0)
@maxValue(30)
param minReplicas int = 1

@description('Max replicas')
@minValue(1)
@maxValue(30)
param maxReplicas int = 10

@description('Environment variables and KV references')
param env array = []

@description('Resource tags applied to the app')
param tags object = {}

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: resourceGroup().location
  tags: tags
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
      }
    }
    template: {
      containers: [{
        name: name
        image: image
        env: env
        resources: {
          cpu: json('0.5')
          memory: '1Gi'
        }
      }]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
        rules: [{
          name: 'http'
          http: { metadata: { concurrentRequests: '50' } }
        }]
      }
    }
  }
}

output fqdn string = app.properties.configuration.ingress.fqdn
output principalId string = app.identity.principalId
```

The squad's module conventions:
- **Every parameter has `@description`.** Without it, `bicep build` warns; the squad treats warnings as PR-blocking.
- **Range/length validators** (`@minValue`, `@maxLength`, `@allowed`) for everything that has a bounded domain. They catch bad parameter files at deploy time, not at runtime.
- **Outputs only what the parent needs.** Don't output every property "in case it's useful"; outputs are part of the contract and changing them breaks consumers.
- **Tags object passed in.** The parent owns the tag scheme; modules receive it.

### Main.bicep shape

```bicep
// infra/main.bicep
targetScope = 'resourceGroup'

@description('Environment name: dev, staging, or prod')
@allowed(['dev', 'staging', 'prod'])
param environmentName string

@description('Standard tags applied to every resource')
param tags object = {
  product: 'articles'
  environment: environmentName
  managedBy: 'azd'
}

// modules
module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: { tags: tags, environmentName: environmentName }
}

module kv 'modules/keyvault.bicep' = {
  name: 'keyvault'
  params: { tags: tags, environmentName: environmentName, logAnalyticsId: monitoring.outputs.logAnalyticsId }
}

module pg 'modules/postgres.bicep' = {
  name: 'postgres'
  params: { tags: tags, environmentName: environmentName, kvName: kv.outputs.name }
}

module api 'modules/containerapp.bicep' = {
  name: 'api'
  params: {
    name: 'api-${environmentName}'
    environmentId: monitoring.outputs.containerAppsEnvironmentId
    image: '${monitoring.outputs.acrLoginServer}/api:latest'
    tags: tags
    env: [
      { name: 'ConnectionStrings__Default', secretRef: 'pg-conn' }
      { name: 'OTEL_EXPORTER_OTLP_ENDPOINT', value: monitoring.outputs.otlpEndpoint }
    ]
  }
}

output apiFqdn string = api.outputs.fqdn
```

## Parameters per environment

```bicep
// infra/env/staging.bicepparam
using '../main.bicep'

param environmentName = 'staging'
param tags = {
  product: 'articles'
  environment: 'staging'
  managedBy: 'azd'
  costCenter: 'engineering'
}
```

`.bicepparam` is typed against `main.bicep`. If `main.bicep` adds a new required parameter, `.bicepparam` files fail to compile until they're updated. Catch at PR time, not deploy time.

## Deploy and what-if

```bash
# What-if (no changes applied) — always run before a real deploy
az deployment group what-if \
  -g rg-articles-staging-westeu \
  --template-file infra/main.bicep \
  --parameters infra/env/staging.bicepparam

# Deploy
az deployment group create \
  -g rg-articles-staging-westeu \
  --template-file infra/main.bicep \
  --parameters infra/env/staging.bicepparam \
  --name "deploy-$(date -u +%Y%m%d%H%M%S)"

# Inspect a deployment
az deployment group show -g <rg> -n <deploy-name>
az deployment group list -g <rg> --query "[].{name:name, state:properties.provisioningState, time:properties.timestamp}" -o table
```

`what-if` output uses colour-coded markers:
- `+ Create` — new resource
- `- Delete` — destroyed resource (read carefully)
- `~ Modify` — in-place update (potentially restart-inducing)
- `=` — unchanged

**Squad rule:** any `Delete` in `what-if` against staging or prod requires a `/decide` artifact explaining why before it ships. Most accidental deletions are caused by parameter typos; the `what-if` step catches them.

## Linting

```bash
# Built into the Bicep compiler
bicep build infra/main.bicep              # produces ARM JSON; warns about issues
bicep lint infra/main.bicep               # static analysis only

# Common findings the linter catches:
#   - secure-parameter-default (no @secure on a password param with a default)
#   - max-outputs (too many outputs)
#   - no-unused-params
#   - no-hardcoded-env-urls
#   - prefer-interpolation
```

This template ships no IaC CI workflow by default. If you add one, run `bicep build` in it so lint findings surface on every PR; the reviewer subagent flags any warnings introduced by the change during local review either way.

`.bicepconfig.json` at the repo root tunes lint severities. The squad's defaults:

```json
{
  "analyzers": {
    "core": {
      "rules": {
        "no-hardcoded-env-urls": { "level": "error" },
        "secure-parameter-default": { "level": "error" },
        "no-unused-params": { "level": "warning" },
        "prefer-interpolation": { "level": "warning" }
      }
    }
  }
}
```

## Secret handling

Never put secrets in parameter files. The squad's pattern:

1. Secrets live in Key Vault (managed by `kv.bicep`).
2. Container Apps reference KV via `secretRef`:

```bicep
configuration: {
  secrets: [
    {
      name: 'pg-conn'
      keyVaultUrl: '${kv.outputs.uri}/secrets/postgres-connection-string'
      identity: 'system'
    }
  ]
}
```

3. The Container App's system-assigned identity has `Key Vault Secrets User` role on the KV (granted via RBAC in `kv.bicep`).

Parameter files contain references (KV names, secret names), not values. If `git secrets` ever flags a `.bicepparam` file, that's a stop-everything moment.

## Failure modes and footguns

- **`azd up` overwrites manual `az` changes.** The squad's principle: infra is in bicep, not in imperative `az` commands. If you `az containerapp update` something, bicep deploys will revert it.
- **Modules not re-deployed after parameter changes.** Bicep's `module` block is idempotent; ARM only re-deploys what changed. If a module's parameters change but the module itself doesn't redeploy, check the parameter is actually wired through (a stale `defaultValue` is the usual culprit).
- **`@secure() param ... = 'default'`** — secure parameters with defaults defeat the purpose. Linter catches it; don't suppress.
- **Deploying without `what-if` in production.** Squad rule: never. The `what-if` is cheap, the unintended deletion is expensive.
- **Long-running deployments timing out.** `az deployment group create --no-wait` returns the deployment name; poll with `az deployment group show`. Useful for deployments that spin up Postgres or other slow resources.
- **`azd infra synth` overwrites custom edits.** Use comment markers `// AZD-PRESERVE-START` / `// AZD-PRESERVE-END` (azd 2024.x+) around custom blocks; outside markers, expect overwrites.
- **Resource name collisions.** Container App names are globally unique within an environment; KV names are globally unique across Azure. Use `uniqueString(resourceGroup().id)` suffixes for KV and other globally-scoped resources.
- **Cross-region deployments.** A single bicep deployment is one region. Multi-region requires either separate resource groups + bicep runs, or a top-level subscription-scoped bicep that orchestrates them.
