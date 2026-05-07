# Binary Patch Protocol Maintenance Guide

This guide is the maintenance contract for the Abies binary patch protocol between C# and JavaScript.

Canonical implementation files:

- C# writer and opcode enum: `Picea.Abies/RenderBatchWriter.cs`
- JS reader and patch applier: `Picea.Abies.Browser/wwwroot/abies.js`
- Interop bridge declaration: `Picea.Abies.Browser/Interop.cs`

Important repository rule:

- The canonical browser runtime source is `Picea.Abies.Browser/wwwroot/abies.js`.
- Do not patch copied `abies.js` files in template/sample projects.

## Why this protocol exists

Abies serializes a render-cycle patch list into one compact binary payload and applies it in one JSImport call.

Design goals:

- no JSON serialization
- low allocation transport (`Span<byte>` -> `JSType.MemoryView`)
- deterministic, fixed-size patch entry decoding
- string deduplication for repeated IDs and attribute names

## Wire Format (Current)

All integers are little-endian `int32`.

### Header (8 bytes)

| Offset | Field | Type |
| --- | --- | --- |
| 0 | `patchCount` | int32 |
| 4 | `stringTableOffset` | int32 |

### Patch Entry (20 bytes each)

| Offset within entry | Field | Meaning |
| --- | --- | --- |
| 0 | `type` | opcode (`BinaryPatchType`) |
| 4 | `field1` | string index or -1 |
| 8 | `field2` | string index or -1 |
| 12 | `field3` | string index or -1 |
| 16 | `field4` | string index or -1 |

Null sentinel: `-1`.

### String Table

Starts at `stringTableOffset`, continues to end of payload.

Encoding per string:

1. unsigned LEB128 byte length
2. UTF-8 bytes

String table is deduplicated by writer-side interning.

## Opcode Map (must remain synchronized)

Source of truth: enum `BinaryPatchType` in `Picea.Abies/RenderBatchWriter.cs`.

| Value | Opcode | C# patch shape (writer) | JS apply semantics |
| --- | --- | --- | --- |
| 0 | `AddRoot` | `f1=rootId`, `f2=html` | `document.body.innerHTML = f2` |
| 1 | `ReplaceChild` | `f1=oldId`, `f2=newId`, `f3=html` | Replace old element with parsed HTML |
| 2 | `AddChild` | `f1=parentId`, `f2=childId`, `f3=html` | Append parsed child to parent |
| 3 | `RemoveChild` | `f1=parentId`, `f2=childId` | Remove child element by id |
| 4 | `ClearChildren` | `f1=parentId` | `parent.innerHTML = ""` |
| 5 | `SetChildrenHtml` | `f1=parentId`, `f2=childrenHtml` | Replace all children with concatenated HTML |
| 6 | `MoveChild` | `f1=parentId`, `f2=childId`, `f3=beforeId?` | Insert before `beforeId` or append |
| 7 | `UpdateAttribute` | `f1=elementId`, `f2=name`, `f3=value` | Set attribute (+ value/checked property sync) |
| 8 | `AddAttribute` | `f1=elementId`, `f2=name`, `f3=value` | Set attribute |
| 9 | `RemoveAttribute` | `f1=elementId`, `f2=name` | Remove attribute |
| 10 | `AddHandler` | `f1=elementId`, `f2=attrName`, `f3=commandId` | Set `data-event-*` attribute |
| 11 | `RemoveHandler` | `f1=elementId`, `f2=attrName`, `f3=commandId` | Remove `data-event-*` attribute |
| 12 | `UpdateHandler` | `f1=elementId`, `f2=attrName`, `f3=newCommandId` | Update `data-event-*` attribute |
| 13 | `UpdateText` | `f1=parentId`, `f2=oldTextId`, `f3=text`, `f4=newTextId` | Update managed text marker/text node |
| 14 | `AddText` | `f1=parentId`, `f2=text`, `f3=textId` | Append managed text fragment |
| 15 | `RemoveText` | `f1=parentId`, `f2=textId` | Remove managed text |
| 16 | `AddRaw` | `f1=parentId`, `f2=html`, `f3=rawId` | Append wrapped raw HTML |
| 17 | `RemoveRaw` | `f1=parentId`, `f2=rawId` | Remove raw wrapper element |
| 18 | `ReplaceRaw` | `f1=oldId`, `f2=newId`, `f3=html` | Replace raw wrapper |
| 19 | `UpdateRaw` | `f1=nodeId`, `f2=html`, `f3=newId` | Replace innerHTML of raw wrapper |
| 20 | `AddHeadElement` | `f1=key`, `f2=html` | Append managed head element |
| 21 | `UpdateHeadElement` | `f1=key`, `f2=html` | Replace managed head element by key (or add fallback) |
| 22 | `RemoveHeadElement` | `f1=key` | Remove managed head element by key |
| 23 | `AppendChildrenHtml` | `f1=parentId`, `f2=childrenHtml` | Append via `insertAdjacentHTML("beforeend", ...)` |

## C# -> JS Contract Boundaries

### Writer responsibilities (C#)

- canonicalize patch list before writing
- emit header with accurate `patchCount` and `stringTableOffset`
- encode every patch entry as 5 int32 values (`type` + 4 fields)
- intern all non-null strings and reference by index

### Reader responsibilities (JS)

- copy `MemoryView` to stable bytes using `batchData.slice()`
- decode header and string table before applying patches
- interpret `-1` as null field
- apply patches in original order

## Synchronized-Change Checklist

Use this checklist whenever changing the protocol.

1. Update enum values and ordering in `Picea.Abies/RenderBatchWriter.cs`.
2. Update opcode constants in `Picea.Abies.Browser/wwwroot/abies.js`.
3. Update JS `applyPatch` switch behavior for any changed/new opcode.
4. Update C# patch writing branch in `RenderBatchWriter.WritePatch`.
5. If entry width changes, update both writer constants and JS reader offsets/entry size.
6. If field meaning changes, update this guide and [JavaScript Interop Architecture](./js-interop.md).
7. Validate at least one end-to-end flow that exercises changed opcodes.
8. Confirm no stale copies of `abies.js` were edited directly.

## Common Failure Modes

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| No DOM updates after a render | Entry size mismatch between writer and JS reader | Verify `PatchEntrySize` and JS `entrySize`/offsets |
| Wrong elements mutate | Opcode values out of sync | Compare enum values with JS constants |
| Random null/garbled fields | String table offset bug or LEB128 decode mismatch | Validate header writes and string table reader |
| Event handlers stop firing | Handler field mapping changed (`data-event-*`) | Re-check `AddHandler`/`UpdateHandler` field mapping |
| Works in one project but not another | Edited copied `abies.js` instead of canonical file | Re-apply change in canonical file and rebuild sync |

## Validation Suggestions

After protocol edits, run at minimum:

1. Build affected projects that consume `abies.js`.
2. Run focused E2E tests for create/update/remove and navigation flows.
3. Verify no stale docs still describe old entry size/field count.

## Related References

- [Browser Runtime API Reference](./browser-runtime-api.md)
- [JavaScript Interop Architecture](./js-interop.md)
- [Virtual DOM Diff Algorithm](./virtual-dom-algorithm.md)
