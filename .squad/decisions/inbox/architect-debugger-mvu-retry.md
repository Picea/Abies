### 2026-03-24: Debugger MVU boundary retry verification (Issue 160 follow-up)
**By:** Architect
**Decision:** PASS — ADR-025 Phase 1 boundary remains satisfied.

**Evidence:**
- Debugger UI surface is implemented in C# MVU (`DebuggerUiModel`, `Transition`, `View`) and rendered via Abies document pipeline.
- JavaScript adapter (`wwwroot/debugger.js`) performs mount + intent transport only (listener wiring + CustomEvent dispatch), with no debugger state machine/replay ownership.
- Runtime replay gates are active in `Picea.Abies/Runtime.cs` for interpreter, navigation, and subscription side effects when replay mode is enabled.
- Release-strip is enforced by excluding `wwwroot/debugger.js` from Release configuration in `Picea.Abies.Browser.csproj`.

**Changes made:**
- Added this decision note only. No code changes were required.
