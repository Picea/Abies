# Abies Debugger UX Review And Proposal

Date: 2026-03-27
Owner: Squad

## 1. Problem Framing

### Users
- Framework developers debugging MVU update paths.
- App developers debugging behavior regressions in browser and server render modes.

### Jobs To Be Done
- Rewind to a meaningful state quickly.
- Understand what changed and why.
- Replay forward without losing context.
- Share reproducible debugger sessions.

### Success Metrics
- Time to find bug origin reduced by 40 percent from current baseline.
- First-time user can complete rewind and replay task in under 60 seconds.
- Keyboard-only completion of core flow in under 45 seconds.
- No failed attempts caused by unclear control semantics.

## 2. Current UX Review

Current debugger implementation is in [Picea.Abies.Browser/wwwroot/debugger.js](Picea.Abies.Browser/wwwroot/debugger.js) with runtime integration in [Picea.Abies.Browser/Interop.cs](Picea.Abies.Browser/Interop.cs), [Picea.Abies.Browser/Runtime.cs](Picea.Abies.Browser/Runtime.cs), [Picea.Abies/Runtime.cs](Picea.Abies/Runtime.cs), and [Picea.Abies/Debugger/DebuggerMachine.cs](Picea.Abies/Debugger/DebuggerMachine.cs).

### Highest-Impact Issues
- High: No visual timeline scrubber. Users must parse text logs to reason about cursor position.
- High: Jump input has weak affordance. It is not obvious what range or target semantics are valid.
- High: Bridge unavailable states are not explicit enough for quick diagnosis.
- Medium: Transport state language is terse and color semantics are underspecified.
- Medium: Action log is useful for diagnostics but weak for navigation.
- Medium: Panel-only layout constrains complex sessions and larger histories.

### Accessibility And Interaction Risks
- No explicit focus order map for panel controls.
- State transitions rely on subtle visual cues.
- Timeline navigation is not represented as a semantic control for assistive tech.

## 3. Comparable Debugger Research

### What Comparable Tools Do Well
- Redux DevTools: Action list plus jump-to-state and clear temporal model.
- Elm Debugger: Deterministic time-travel and simple mental model.
- Chrome DevTools Timeline: Strong scrubber and zoom patterns for temporal navigation.
- Playwright Trace Viewer: Action list + details split that improves scanning and drill-down.
- Flipper-style plugin panels: Progressive disclosure for advanced diagnostics.

### Common Gaps In Comparable Tools
- Weak default discoverability of advanced controls.
- Poor handling of very large histories.
- Insufficiently integrated sharing and reproducibility workflows.
- Too much information density without a guided first-run path.

### Opportunity For Abies
- Differentiate with an MVU-native causality view: Message -> Model Diff -> View Update -> Patch Summary.
- Keep base experience simple, then reveal advanced diagnostics progressively.

## 4. Proposed Debugger UX

## 4.1 Information Architecture

### Primary Regions
- Header: Mode badge, session summary, quick actions.
- Timeline rail: Scrubber, cursor, event markers.
- Event list: Filterable list synchronized with scrubber.
- Details panel: Before/after model diff, message payload, patch summary.

### Navigation Model
- Default compact panel remains for quick use.
- Expand into full inspector mode for deep sessions.
- Preserve current shell button behavior to avoid workflow disruption.

## 4.2 Annotated Wireframe (Textual)

```
+---------------------------------------------------------------+
| Abies Debugger | Recording | Step 42/184 | Export | Expand   |
+---------------------------------------------------------------+
| Timeline: [----o---x--x---o------x--------------------------] |
|           0         42                               184      |
+------------------------------+--------------------------------+
| Events                       | Details                        |
| Filter: [message type / text]| Message: Increment             |
|                              | Payload: { ... }               |
| > #42 Increment              | Model diff (Before | After)    |
|   #41 SelectRow              | Count: 1 -> 2                  |
|   #40 LoadCompleted          | Patch summary: 1 text update   |
|   ...                        |                                 |
+------------------------------+--------------------------------+
| [Back] [Play/Pause] [Step] [Jump To] [Clear] [Bookmark]      |
+---------------------------------------------------------------+
```

## 4.3 Core Flows

### Flow A: Rewind Then Replay
1. User opens debugger shell.
2. User sees clear current position and total entries.
3. User drags timeline scrubber or clicks Back.
4. Details panel updates immediately with before and after model diff.
5. User presses Step or Play to move forward.
6. Cursor and view update in lockstep.

### Flow B: Find Specific Event
1. User filters event list by message type or text.
2. User selects matching event.
3. Cursor jumps to event and details panel focuses this state transition.

### Flow C: Share Repro Session
1. User clicks Export.
2. Session file is downloaded with timeline and snapshot metadata.
3. Team member can import and replay identically.

## 4.4 Interaction Details And States

### Required States
- Loading: Runtime bridge connecting with explicit status text.
- Ready: Controls active with visible cursor and timeline count.
- Empty: No timeline entries yet, guided message with next action.
- Error: Bridge unavailable with retry action.
- Replay boundary: At first and last entry, directional controls disabled with explanation.

### Keyboard Model
- Space: Play and pause.
- Left arrow: Step back.
- Right arrow: Step forward.
- Slash: Focus filter input.
- Home and End: Jump to first and last timeline entry.

## 4.5 Accessibility Requirements
- Full keyboard-only operation for all core controls.
- Visible focus ring on each interactive element.
- Announce cursor changes through aria-live status text.
- Semantic roles for timeline, event list, and transport controls.
- Do not rely on color alone for mode and boundary states.

## 4.6 Visual Direction (Abies Brand-Compatible)
- Keep dark panel baseline and monospaced diagnostics feel.
- Use explicit status tokens from existing Abies palette guidance.
- Reserve accent color for active cursor and selected event.
- Use neutral hierarchy for scaffolding and non-critical metadata.

## 5. Acceptance Checklist

- User can identify current cursor position at a glance.
- User can scrub timeline directly without typing an index.
- User can find an event using search or filter in under 10 seconds on 200-entry timeline.
- Rewind and replay update both visible app state and debugger state consistently.
- First and last entry boundaries are obvious and non-confusing.
- Keyboard-only core flow passes.
- Screen reader announces cursor movement and active event.

## 6. Visual Verification Plan

### Manual Verification Script
1. Run app in debug mode and open debugger shell.
2. Generate at least 10 timeline entries through normal interaction.
3. Confirm timeline rail shows total count and active cursor.
4. Click Back three times. Confirm app state rewinds each step.
5. Click Step twice. Confirm app state replays forward.
6. Use filter to find a known message type and jump to it.
7. Validate details panel shows before and after model values.
8. Use keyboard-only controls for Back, Step, Play and filter focus.
9. Confirm boundary behavior at entry 0 and final entry.

### E2E Test Additions
- Add browser and server E2E tests for scrubber jump behavior.
- Add keyboard navigation test coverage for transport controls.
- Add accessibility smoke checks for focus visibility and live region updates.

Existing replay tests for reference:
- [Picea.Abies.Templates.Testing.E2E/BrowserTemplateTests.cs](Picea.Abies.Templates.Testing.E2E/BrowserTemplateTests.cs)
- [Picea.Abies.Templates.Testing.E2E/ServerTemplateTests.cs](Picea.Abies.Templates.Testing.E2E/ServerTemplateTests.cs)
- [Picea.Abies.Tests/DebuggerRuntimeReplayApplicationTests.cs](Picea.Abies.Tests/DebuggerRuntimeReplayApplicationTests.cs)

## 7. Incremental Rollout Plan

### Phase 1 (Small)
- Explicit runtime state messaging.
- Cursor and timeline summary improvements.
- Boundary-aware disabled states and labels.

### Phase 2 (Medium)
- Timeline scrubber with click and drag.
- Search and filterable event list.
- Details panel with structured before/after diff.

### Phase 3 (Large)
- Export and import session workflow.
- Bookmarks and quick navigation markers.
- Optional expanded full inspector mode.

## 8. Recommended Next Step

Implement Phase 1 and Phase 2 behind a debug feature flag, then run the visual verification script with browser and server modes before default-on rollout.
