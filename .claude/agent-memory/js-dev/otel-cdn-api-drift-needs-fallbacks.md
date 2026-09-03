---
name: otel-cdn-api-drift-needs-fallbacks
description: The CDN-hosted OpenTelemetry ESM builds change their exported API between versions — feature-detect instead of calling the newest name
metadata:
  type: feedback
---

The browser OTel runtime loads `@opentelemetry/*` from a CDN as ESM, and the
exported surface differs by version. Concretely,
`@opentelemetry/resources@1.30.1/+esm` exports `Resource` but **not**
`resourceFromAttributes`. The code feature-detects:

```js
const resource = typeof resources.resourceFromAttributes === "function"
    ? resources.resourceFromAttributes(resourceAttributes)
    : new Resource(resourceAttributes);
```

**Why:** On 2026-03-27 browser spans vanished entirely after a switch to
`resources.resourceFromAttributes(...)`. Nothing threw visibly — initialization
failed and `service.name` was lost, so traces collapsed into `unknown_service`
in the Aspire UI. A missing export in a CDN build reads exactly like "telemetry
is broken" rather than "one function does not exist".

**How to apply:** When adopting a newer OTel API in `abies-otel.js`, keep the
old call as a fallback rather than replacing it, and pin the API, SDK and
exporter package versions *together* — partial pinning lets one package drift
into an incompatible pair. Six copies of `abies-otel.js` exist across templates
and demo apps, so any such change is a multi-file edit.

Verified 2026-09-02: the `resourceFromAttributes` fallback is still present in
all `abies-otel.js` copies. Related:
[[browser-spans-need-explicit-export-on-end]].
