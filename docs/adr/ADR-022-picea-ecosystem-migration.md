# ADR-022: Migration from Automaton to Picea Ecosystem

**Status:** Accepted  
**Date:** 2026-03-11  
**Decision Makers:** Maurice Peters  
**Related:** ADR-023 (Package Rename), ADR-024 (Four Render Modes)

## Context

The Abies MVU framework grew inside the `MCGPPeters/Automaton` monorepo alongside the Automaton kernel, resilience patterns, event sourcing patterns, and actor model patterns. As the project matured, several architectural forces demanded separation:

1. **Independent release cycles** — The MVU framework, kernel, resilience library, event sourcing, and actor model all evolve at different speeds. Coupling them in one repo means a change to resilience patterns blocks an Abies release (and vice versa).

2. **Bounded context clarity** — Domain-Driven Design teaches that bounded contexts should be explicit. Having `Automaton`, `Automaton.Resilience`, `Automaton.Patterns`, and `Abies` in one repo blurs the boundaries between fundamentally different domains.

3. **Dependency direction** — All higher-level libraries depend on the kernel (`Automaton<>`, `Result<>`, `Option<>`, `Decider<>`), but they should not depend on each other. A monorepo makes it too easy to accidentally introduce cross-cutting dependencies.

4. **Community discovery** — Individual repos with focused READMEs and NuGet packages are more discoverable than a monorepo where the MVU framework is buried alongside unrelated patterns.

5. **CI/CD isolation** — A change to actor model tests should not trigger Abies E2E tests. Separate repos give each library its own CI pipeline.

## Decision

Migrate the Automaton monorepo into the **Picea GitHub organization**, splitting it into five focused repositories:

| Repository | Source | Domain |
|---|---|---|
| `picea/picea` | `Automaton/` (kernel) | Core types: `Automaton<>`, `Result<>`, `Option<>`, `Decider<>`, Runtime, Diagnostics |
| `picea/abies` | `Abies/`, `Abies.Browser/`, `Abies.Server/`, etc. | MVU framework for .NET |
| `picea/mariana` | `Automaton.Resilience/` | Resilience patterns |
| `picea/glauca` | `Automaton.Patterns/EventSourcing/` | Event sourcing patterns |
| `picea/rubens` | `Automaton/Actor/` | Actor model patterns |

The naming follows Picea (spruce) taxonomy — each repo is named after a species in or related to the *Picea* genus, reflecting their relationship to the kernel.

### Git History Strategy

The migration preserves git history via rebasing rather than starting fresh:

- **picea/picea**: Cherry-picked kernel commits from Automaton
- **picea/abies**: Rebased Automaton MVU commits on top of existing Abies history (branch `archive/pre-automaton` preserves the original)
- **picea/glauca, picea/rubens, picea/mariana**: Extracted via `git filter-repo`

This ensures `git blame` and `git log` remain useful across the migration boundary.

### Version Continuity

| Repo | Starting Version | Rationale |
|---|---|---|
| picea/picea | `1.0-rc.1` | New package, first release |
| picea/abies | `1.0-rc.2` → `2.0-rc.1` | Continues from existing Abies versioning; major bump for breaking namespace changes |
| picea/mariana | `0.1` | Experimental extraction |
| picea/glauca | `0.1` | Experimental extraction |
| picea/rubens | `0.1` | Experimental extraction |

## Consequences

### Positive

- Each library has an independent release cycle — shipping a fix to Abies no longer requires coordinating with resilience or event sourcing changes
- Bounded contexts are explicit in the repository structure — the dependency graph is visible at the organization level
- CI/CD pipelines are isolated — Abies E2E tests don't run when resilience code changes
- NuGet packages map 1:1 to repositories, improving discoverability
- Contributors can focus on a single domain without cloning unrelated code

### Negative

- Cross-cutting changes (e.g., updating the Picea kernel API) require coordinated PRs across repos
- Local development of the full stack requires cloning multiple repos
- Dependency versioning must be managed explicitly via NuGet rather than project references

### Neutral

- All repos share the same .NET 10 target, Apache 2.0 license, and Nerdbank.GitVersioning setup
- The `MCGPPeters/Automaton` repo will be archived with a deprecation notice pointing to the Picea organization

## Alternatives Considered

### Alternative 1: Stay in Monorepo

Keep everything in `MCGPPeters/Automaton` and use project-level isolation.

**Rejected** because: release cycles are fundamentally different (Abies ships weekly, resilience is stable for months), and the monorepo structure obscures bounded contexts. The cost of maintaining a monorepo increases as more patterns are added.

### Alternative 2: GitHub Organization Without Renaming

Move repos to a GitHub org but keep the `Automaton` / `Abies` package names.

**Rejected** because: the package names should reflect the organizational structure. `Picea.Abies` clearly communicates that Abies is part of the Picea ecosystem. See ADR-023.

### Alternative 3: NuGet-Only Split (Same Repo)

Keep one repo but publish separate NuGet packages with different version tracks.

**Rejected** because: this doesn't solve the CI/CD isolation or discovery problems, and managing multiple version tracks in one repo is error-prone with Nerdbank.GitVersioning.

## Related Decisions

- [ADR-023: Package Rename](./ADR-023-package-rename.md) — The naming convention for migrated packages
- [ADR-024: Four Render Modes](./ADR-024-four-render-modes.md) — New capability enabled by the SSR work that motivated the migration

## References

- [Picea Migration Plan](../migration/picea-migration-plan.md)
- [Domain-Driven Design — Bounded Contexts (Eric Evans, 2003)](https://www.domainlanguage.com/ddd/)
- [Monorepo vs Multi-repo (Atlassian)](https://www.atlassian.com/git/tutorials/monorepos)
