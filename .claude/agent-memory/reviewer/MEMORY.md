# Reviewer memory

- [NU1605 transitive floor is a ship blocker](nu1605-transitive-floor-is-ship-blocker.md) — a version migration can read correct in the diff and still fail restore.
- [Prerelease pin acceptance criteria](prerelease-pin-acceptance-criteria.md) — the four conditions that make a temporary prerelease pin acceptable.
- [Split PRs when migration scope drifts](split-prs-when-migration-scope-drifts.md) — unrelated concerns dilute the release-risk question; split before reviewing substance.
- [Dependency approval trail is a blocker](dependency-approval-trail-is-a-blocker.md) — missing Security Expert trail blocks even when the package is obviously fine.
- [A disclaimer does not fix a false claim](a-disclaimer-does-not-fix-a-false-claim.md) — directionally wrong claims must be restated, not hedged.
- [TUnit filtering needs tree-node filters](tunit-filtering-needs-tree-node-filters.md) — FullyQualifiedName filters yield zero-test runs that look like passes.
- [Nerdbank CompareFiles is infrastructure noise](nerdbank-comparefiles-is-infrastructure-noise.md) — a known flaky template build failure, not a functional regression.
- [Dynamic imports need served-asset proof](dynamic-imports-need-served-asset-proof.md) — an unserved dynamic import fails silently in best-effort bootstraps.
- [Runtime behaviour coverage belongs in template E2E](runtime-behaviour-coverage-belongs-in-template-e2e.md) — where "prove it wires up at runtime" tests go, and what they assert.
