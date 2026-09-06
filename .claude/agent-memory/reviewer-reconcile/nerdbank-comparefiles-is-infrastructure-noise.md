---
name: nerdbank-comparefiles-is-infrastructure-noise
description: A template build-test failure with Nerdbank.GitVersioning CompareFiles EndOfStreamException is infrastructure flake, not a functional assertion failure
metadata:
  type: feedback
---

A template build-test failure surfacing as a `Nerdbank.GitVersioning`
`CompareFiles` `EndOfStreamException` is an infrastructure/tooling failure, not
a functional assertion failure in the code under review.

**Why:** Diagnosed 2026-03-26. It presents as a red template build test, which
reads like the reviewed change broke template generation — so the default
reaction is to block. It does not indicate anything about the change.

**How to apply:** Do not raise it as a 🔴 against the author, and do not let it
mask the real result — re-run and get a clean signal before issuing a verdict,
rather than approving over a red check. If it recurs frequently, that is a
DevOps item about build determinism, not a code-review item.

Verified 2026-09-02: `Nerdbank.GitVersioning` 3.9.50 is still referenced from
`Directory.Build.props`, so this failure mode remains reachable.
