---
name: e2e-gating-is-not-replaceable-by-microbenchmarks
description: Keep js-framework-benchmark at a 5% threshold and keep mainline E2E benchmark runs — micro-benchmarks are not a substitute for E2E regression gating
metadata:
  type: feedback
---

js-framework-benchmark stays the authoritative performance gate at a 5%
regression threshold, and mainline E2E benchmark runs continue so baselines keep
updating. Micro-benchmarks are additional signal, never a replacement for E2E
gating.

**Why:** Established 2026-04-01. Micro-benchmarks are fast and cheap, which
makes them a tempting swap when someone is trying to shorten the PR path — but
they measure a component in isolation and cannot see the regressions that
actually reach users (layout, patch batching, transport). Dropping E2E gating in
their favour would trade real regression signal for speed, which is the exact
false-confidence trade the lane policy was designed to avoid.

**How to apply:** If a proposal shortens CI by substituting micro-benchmarks for
the E2E benchmark gate, push back and offer
[[required-check-with-conditional-heavy-work]] instead. The 5% threshold is a
tuned number — moving it needs evidence about false-positive rate, not a general
argument that CI is noisy.
