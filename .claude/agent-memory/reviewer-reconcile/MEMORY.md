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
- [Measure each guard item separately](measure-each-guard-item-separately.md) — a four-item MSBuild Remove group held two no-ops and one that hid the tree from the IDE.
- [Inherited or introduced decides the verdict](inherited-or-introduced-decides-the-verdict.md) — for copy bundles, `git show <pinned-sha>:` separates a faithful copy of a bad source from an error made here.
- [The drop validator is not a YAML parser](the-drop-validator-is-not-a-yaml-parser.md) — invalid front-matter merges instead of quarantining; use quoted heredocs and `>-` block scalars.
- [Squash-merge rewrites the quoted timestamp](squash-merge-rewrites-the-quoted-timestamp.md) — a pinned bundle can faithfully quote the pre-squash commit's author date; check commit metadata separately.
- [Untracked bundles need a check-ignore sweep](untracked-bundles-need-a-check-ignore-sweep.md) — byte-perfect files still ship incomplete; `git add` skips ignored paths silently.
- [State the finding, not the remedy](state-the-finding-not-the-remedy.md) — twice in one changeset the override beat my prescription; a menu of remedies is not an open fix.
- [The merger eats the drop immediately](the-merger-eats-the-drop-immediately.md) — an inbox path that vanishes means merged, not failed; check the archive and the cache.
- [Extract the spec fences and diff](extract-the-spec-fences-and-diff.md) — reassembling `06-spec.md`'s code blocks decides whether a defect is the spec-author's or the transcriber's.
- [A locked file was never type-checked](a-locked-file-was-never-type-checked.md) — an excluded spec is frozen with its typos; compile-probe the assertion shapes before the approval commit closes the lock.
- [A copy outside the deny list defeats blindness](a-copy-outside-the-deny-list-defeats-blindness.md) — the hook denies `.squad/design/**`, not the byte-identical copy someone committed elsewhere.
- [A negative control alone proves nothing](a-negative-control-alone-proves-nothing.md) — my one-sided probe sent an amendment to a shape that can never pass; run both controls, and never read CS0103 as an absent API.
- [A Timeout attribute is not a hang guard](a-timeout-attribute-is-not-a-hang-guard.md) — TUnit0015 on every test, sync spins missed, and the orphaned body corrupts the next test on shared static state.
- [The split can be degenerate](the-split-can-be-degenerate.md) — at the cap, grade by whether the fix is cap-forcing; escalate with numbered routes, don't manufacture a fourth 🔴.
- [Arithmetic adjudicates a missing baseline](arithmetic-adjudicates-a-missing-baseline.md) — untracked files have no baseline; make the counts reconcile, and re-derive rather than quote last round.
- ["Landed as" retro-dates the whole sentence](landed-as-retro-dates-the-whole-sentence.md) — an as-landed anchor converts every neighbouring prediction; run `gh pr checks` against the gate the passage claims.
- [A restated mechanism can arrive inverted](a-restated-mechanism-can-arrive-inverted.md) — the operative instruction survives, the justification clause flips a state qualifier; both controls settle it.
- [Recover the round-1 baseline from unreachable blobs](recover-the-round-1-baseline-from-unreachable-blobs.md) — same-HEAD re-reviews have no diff baseline; `git fsck --unreachable` still holds the previous index.
- [A blocker can be reduced rather than closed](a-blocker-can-be-reduced-rather-than-closed.md) — a scoping fix left a 1-line residue where there were 4; three conditions decide the downgrade.
- [Grade a compound finding by which half survives](grade-a-compound-finding-by-which-half-survives.md) — introduced half fixed, inherited half left; regrade on the criterion's own line and write the argument down.
