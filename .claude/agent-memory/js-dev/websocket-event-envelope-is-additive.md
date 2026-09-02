---
name: websocket-event-envelope-is-additive
description: The InteractiveServer WebSocket event envelope tolerates additive top-level properties, which is how traceparent was introduced without a version bump
metadata:
  type: project
---

The InteractiveServer DOM-event envelope is forward-compatible by construction:
`WebSocketTransport` deserializes `commandId`, `eventName` and `eventData` and
ignores unknown top-level properties. Optional `traceparent` and `tracestate`
fields were added on 2026-03-26 on exactly this basis —
`Session.RunEventLoop` restores the W3C parent onto a per-event activity so
runtime spans stay on the browser event trace.

**Why:** It means a new optional field can ship browser-side and server-side
independently, with old clients and old servers both continuing to work. That
property is not obvious from the type and is easy to destroy by adding strict
deserialization options or a required member.

**How to apply:** New optional metadata on the envelope is cheap — add it
top-level and default it. But do not add `JsonSerializerOptions` with
`UnmappedMemberHandling.Disallow`, and do not make a new field required; either
turns a rolling upgrade into a breaking change. Anything that must be
*required* deserves an explicit envelope version instead.

Verified 2026-09-02: `Picea.Abies.Server/Transport.cs` documents `traceparent`
and `tracestate` as optional; `Session.cs` reads them onto the event activity.
