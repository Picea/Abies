### 2026-03-20T16:00:00Z: Issue #152 kickoff package baseline in-repo
**By:** Maurice Cornelius Gerardus Petrus Peters (via Faramir)
**What:** Start implementation with a new `Picea.Abies.UI` net10.0 class library in the main solution, exposing immutable-options + pure static Node component factories for `button`, `textInput`, `select`, `spinner`, `toast`, `modal`, and `table`, plus a token CSS contract file under `wwwroot`.
**Why:** Provides a compile-ready baseline that matches Phase 1 scope while deferring advanced behaviors (modal focus trap, toast lifecycle, table interaction states) to explicit follow-up phases.
