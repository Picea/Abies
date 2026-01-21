# Virtual DOM Diff and Patch Algorithm

This document describes the current diff/patch logic in `Abies.DOM.Operations`.

## Overview

1. `Operations.Diff` walks the old and new virtual DOM trees and produces a list
   of `Patch` values.
2. `Operations.Apply` executes each patch via JavaScript interop.

The diff is recursive and prioritizes simple, ordered updates over keyed
reconciliation.

## Node types

- `Element` nodes: tag + attributes + children
- `Text` nodes: rendered as `<span>` with a stable id
- `RawHtml` nodes: rendered as raw HTML inside a `<span>`

## Diff strategy

- If both nodes are `Text`, it emits `UpdateText` when value or id changed.
- If both nodes are `RawHtml`, it emits `UpdateRaw` when HTML or id changed.
- If both are `Element` and the tag differs, it emits `ReplaceChild` (or
  `AddRoot` if there is no parent).
- Attributes are compared by id using a dictionary for O(n) lookup.
- Children are diffed in order. Extra old children are removed, extra new
  children are appended.

This means Abies does not currently do key-based reordering. Child position is
significant.

## Patch types

Common patches include:

- `AddRoot`
- `ReplaceChild`
- `AddChild` / `RemoveChild`
- `AddAttribute` / `RemoveAttribute` / `UpdateAttribute`
- `AddHandler` / `RemoveHandler` / `UpdateHandler`
- `UpdateText`
- `AddRaw` / `RemoveRaw` / `ReplaceRaw` / `UpdateRaw`

## Patching

`Operations.Apply` forwards each patch to the JavaScript interop layer. Handler
registration is coordinated with DOM updates so event dispatch stays consistent.

See `Abies/DOM/Operations.cs` for the authoritative implementation.
