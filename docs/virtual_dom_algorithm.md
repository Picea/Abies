# Virtual DOM Diff and Patch Algorithm

This document outlines the algorithm used by **Abies** to compute updates to the
DOM. The implementation is heavily inspired by the Elm `VirtualDom` package but
adapted for C#.

## Overview

1. **Diffing** – The `Operations.Diff` method walks two virtual DOM trees and
   produces a list of `Patch` objects. These describe the minimal changes needed
to transform the old tree into the new tree.
2. **Patching** – Each `Patch` is executed by `Operations.Apply` through
   JavaScript interop calls which update the real DOM.

## Diffing Strategy

The diff uses a stack based traversal to avoid recursion overhead. Attributes
are compared using dictionaries so lookups are O(1). Children are processed in
order, removing extra nodes and appending new ones when necessary.

Patches are generated for:

- Replacing nodes when their tag changes.
- Adding or removing child elements.
- Updating, adding or removing attributes and event handlers.
- Updating text node content.

This approach mirrors Elm's efficient diff while remaining idiomatic C#.

## Patching

`Operations.Apply` interprets a patch and forwards the change to the browser via
JavaScript functions. Because patches are small and interop calls are batched,
the runtime stays responsive.

