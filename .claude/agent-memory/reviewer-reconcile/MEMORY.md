# Reviewer-Reconcile memory

Inherited from the retired `reviewer` charter (split into `reviewer-blind` +
`reviewer-reconcile` per spec v2.1 WP-5, 2026-09-06) — `git mv`d wholesale
rather than deleted, since the split changed who reads what, not which review
lessons still hold. Entries that narrate PR #355 events by the agent's
then-current name ("reviewer") are left as-written; that was the correct name
at the time and rewriting it would misrepresent the historical record.

- [NU1605 transitive floor is a ship blocker](nu1605-transitive-floor-is-ship-blocker.md) — a version migration can read correct in the diff and still fail restore.
- [Prerelease pin acceptance criteria](prerelease-pin-acceptance-criteria.md) — the four conditions that make a temporary prerelease pin acceptable.
- [Split PRs when migration scope drifts](split-prs-when-migration-scope-drifts.md) — unrelated concerns dilute the release-risk question; split before reviewing substance.
- [Dependency approval trail is a blocker](dependency-approval-trail-is-a-blocker.md) — missing Security Expert trail blocks even when the package is obviously fine.
- [A disclaimer does not fix a false claim](a-disclaimer-does-not-fix-a-false-claim.md) — directionally wrong claims must be restated, not hedged.
- [TUnit filtering needs tree-node filters](tunit-filtering-needs-tree-node-filters.md) — FullyQualifiedName filters yield zero-test runs that look like passes.
- [Nerdbank CompareFiles is infrastructure noise](nerdbank-comparefiles-is-infrastructure-noise.md) — a known flaky template build failure, not a functional regression.
- [Dynamic imports need served-asset proof](dynamic-imports-need-served-asset-proof.md) — an unserved dynamic import fails silently in best-effort bootstraps.
- [Runtime behaviour coverage belongs in template E2E](runtime-behaviour-coverage-belongs-in-template-e2e.md) — where "prove it wires up at runtime" tests go, and what they assert.
- [Command-text matching is not a gate](command-text-matching-is-not-a-gate.md) — two probes that prove a `git commit` substring hook is both false-positive and false-negative.
- [.last-review-verdict is forgeable](last-review-verdict-is-forgeable.md) — the indent guard counts spaces only; a tab or NBSP still forges a PASS.
- [Doc inventory counts go stale within the same PR](doc-inventory-counts-go-stale-within-the-same-pr.md) — recount "Verified — N workflows" against the tree the PR leaves behind.
- [Line-splitting fixes must start at the open() call](line-splitting-fixes-must-start-at-the-open-call.md) — universal newlines rewrites `\r` before any splitter fix can see it.
- [Guarding a field is not authenticating it](guarding-a-field-is-not-authenticating-it.md) — split parsing-differential impersonation from plain self-declaration, or the blocker reopens forever.
- [Prefix-stripping fixes are unbounded](prefix-stripping-fixes-are-unbounded.md) — enumerate character classes and you get a fifth round; move the check to fence presence.
- [Safety caps must fail closed](safety-caps-must-fail-closed.md) — a cap that returns the permissive answer on cap-hit IS the bypass; probe cap+1.
- [Shape heuristics are defeated by one line](shape-heuristics-are-defeated-by-one-line.md) — attack the reset predicate in both directions before accepting the residual note.
- [Audit the residual ledger in both directions](audit-the-residual-ledger-in-both-directions.md) — over-registration is a failure too; check every `status: open` against the tree.
- [A fix can reintroduce the finding next to it](a-fix-can-reintroduce-the-finding-next-to-it.md) — grade fixes against the round's finding *classes*, not their assigned file.
- [Nested worktrees defeat cwd-relative containment](nested-worktrees-defeat-cwd-relative-containment.md) — a second checkout under cwd re-spells every denied path at a new offset.
- [The round cap is a verdict shape](the-round-cap-is-a-verdict-shape.md) — count the rounds before grading; at round 3 the verdict is the split, not another 🔴.
- [A new trust boundary must name its widest channel](a-new-trust-boundary-must-name-its-widest-channel.md) — diff the boundary's tool list against every governed agent's `tools:` line.
- [Verify a registration pass by scope, not substance](verify-a-registration-pass-by-scope-not-substance.md) — after the split, measure what changed with `find -newer`; don't re-derive findings the cap ruled out.
- [A confirmation pass invalidates its own cache](a-confirmation-pass-invalidates-its-own-cache.md) — committing the confirmation's artifacts moves HEAD and re-blocks the merge; say where to stop.
- [A record is checkable only where its evidence survives](a-record-is-checkable-only-where-its-evidence-survives.md) — design-record PRs fail on provenance; re-run flows overwrite the artifacts governance documents cite.
- [The clock field is a toolchain job](the-clock-field-is-a-toolchain-job.md) — five roles have no Bash to read a clock, and I got `created:` wrong myself while holding one.
- [A blocker list is not the criterion](a-blocker-list-is-not-the-criterion.md) — the author reads your enumeration as the scope; enumerate by rule or (b) comes back unmet.
- [State which grades criterion (b) binds on](state-which-grades-criterion-b-binds-on.md) — read literally, (b) sweeps in nitpicks and never closes; name the grades in the verdict.
