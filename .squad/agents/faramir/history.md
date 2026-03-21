# Faramir History

## Project Context
- Project: Abies
- User: Maurice Cornelius Gerardus Petrus Peters
- Team root: /Users/mauricepeters/RiderProjects/Abies
- Added: 2026-03-20
- Focus: C#, .NET 10, functional domain modeling, TUnit, Aspire, OTEL, and production-grade domain/application code.

## Learnings
- Initial assignment created.
- 2026-03-20: Issue #152 Phase 1 boundaries are fixed (7 components, explicit include/defer scope, no hidden mutable state) and merge readiness requires documented accessibility matrix plus mapped CI gates.
- 2026-03-20: Created compile-ready kickoff package `Picea.Abies.UI` as a net10.0 class library with a minimal `Components` API surface (`button`, `textInput`, `select`, `spinner`, `toast`, `modal`, `table`) using immutable records and pure static Node factories.
- 2026-03-20: New UI project must set `<InterceptorsNamespaces>$(InterceptorsNamespaces);Praefixum</InterceptorsNamespaces>` because Abies HTML helpers rely on Praefixum interceptors.