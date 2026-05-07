# Release Runbook

This runbook describes how to publish Abies NuGet packages using the dedicated release workflow.

## Workflow Reference

- Release workflow: [.github/workflows/release.yml](../../.github/workflows/release.yml)
- CD workflow (main validation only): [.github/workflows/cd.yml](../../.github/workflows/cd.yml)
- PR validation workflow: [.github/workflows/pr-validation.yml](../../.github/workflows/pr-validation.yml)
- Contributing guide: [CONTRIBUTING.md](../../CONTRIBUTING.md)

## Preconditions

1. PR to `main` is merged and required checks are green.
2. `NUGET_API_KEY` repository secret is configured.
3. You have a release tag prepared in the format `v*` (for example `v1.2.3`).

## Option A: Tag-driven release (recommended)

1. Sync local `main`:

```bash
git checkout main
git pull --ff-only
```

2. Create and push version tag:

```bash
git tag vX.Y.Z
git push origin vX.Y.Z
```

3. Confirm release workflow run started:

```bash
gh run list --workflow release.yml --limit 5
```

4. Monitor the run:

```bash
GH_PAGER=cat gh run watch <run-id> --exit-status
```

5. Verify packages on NuGet:

```bash
gh run view <run-id>
```

Then validate package versions on NuGet.org.

## Option B: Manual release

1. Open Actions -> Release workflow.
2. Select Run workflow (`workflow_dispatch`).
3. Run against the intended commit/ref.
4. Monitor run completion and verify package publication.

## What Release Workflow Does

1. Restores dependencies.
2. Builds the solution.
3. Runs key test suites:
   - `Picea.Abies.Tests`
   - `Picea.Abies.Server.Tests`
   - `Picea.Abies.Server.Kestrel.Tests`
   - `Picea.Abies.Conduit.Tests`
   - `Picea.Abies.Conduit.Wasm.Tests`
   - `Picea.Abies.Conduit.Api.Tests`
   - `Picea.Abies.Analyzers.Tests`
   - `Picea.Abies.Templates.Testing` (non-E2E build smoke)
4. Packs core and metapackages.
5. Pushes `.nupkg` files to NuGet using `--skip-duplicate`.

## Troubleshooting

- Authentication failures during publish:
  - Validate `NUGET_API_KEY` in repository secrets.
- Duplicate package version:
  - Expected to be skipped because publish uses `--skip-duplicate`.
- Tag did not trigger release:
  - Ensure tag name starts with `v` and is pushed to origin.

## Rollback Guidance

NuGet packages are immutable. If a bad version was published:

1. Publish a fixed patch version with a new tag (`vX.Y.Z+1`).
2. Update release notes/changelog to mark the bad version as superseded.
