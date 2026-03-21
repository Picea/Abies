### 2026-03-20T15:30:00Z: Issue #152 design iteration 2 — execution contract locked
**By:** Gandalf (Architect)
**What:**
- Locked a four-phase implementation plan for `Picea.Abies.UI` + `Picea.Abies.UI.Demo` with explicit owners and reviewer gates.
- Defined v1 component boundaries for all Phase-1 components (`button`, `textInput`, `select`, `modal`, `table`, `spinner`, `toast`) including explicit deferred items to prevent scope creep.
- Operationalized accessibility verification as a required matrix with automated and manual checks, with fail conditions tied to merge gates.
- Mapped merge and release gates to concrete GitHub Actions jobs and pass conditions.
- Froze explicit non-goals for Issue #152.

**Why:**
Elrond returned `changes-requested` and required execution-ready boundaries, measurable accessibility verification, and explicit CI/release mapping before implementation starts.

**Completion contract for Issue #152:**
1. Every Phase-1 component has a documented v1 include/defer boundary.
2. Accessibility matrix is implemented in CI and manually verified before release.
3. Merge requires green statuses for: `PR Validation`, `CD` (`build` job), `E2E` (`e2e` job), `CodeQL Security Analysis` (`analyze` job), and `Benchmark` (`benchmark-check` / `Benchmark (js-framework-benchmark)`).
4. Release requires merge-gate pass plus successful `CD` run on `main` including package pack/push steps.
5. Non-goals remain frozen unless a separate follow-up issue is opened and approved.
