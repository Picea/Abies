# RFC: Abies JS Interop Protocol v2 (Reasoning-First Design)

## Status

Proposed (exploratory).

## Date

2026-04-03.

## Scope

This RFC proposes a next-generation browser interop protocol for Abies from first principles. The baseline assumption is that Abies already uses in-memory binary batching. The objective is lower jank and better tail latency under realistic load, not merely lower serialization cost.

## Problem Statement

After binary in-memory transport is in place, serialization usually stops being the dominant bottleneck. Dominant costs tend to be DOM mutation side effects, layout and paint pressure, main-thread scheduling variance, event storms, and memory churn that amplifies GC spikes. Therefore, an interop protocol must optimize the full loop from runtime to DOM apply, and from browser event capture back to runtime dispatch, while maintaining explicit ownership and acknowledgment semantics.

## Goals

Priority order is to reduce DOM work per frame, keep frame-time deterministic, preserve low-copy transport, enable canonicalization before apply, enforce strict ordering and replay safety, provide event backpressure and prioritization, support protocol negotiation for evolution, and keep security boundaries explicit.

## Non-Goals

This RFC does not replace MVU, does not move DOM work into a worker model, and does not prioritize byte-level encoding tricks that fail to improve frame latency.

## Protocol Overview

The proposed transport is full-duplex with two logical rings. One ring carries runtime-to-JS command batches for UI updates. The other ring carries JS-to-runtime event packets. Both rings are sequence-based and acknowledgment-driven.

### Header Model

A fixed, aligned header should include protocol identity, version, flags, session identity, monotonic batch identity, parent batch lineage for replay, operation count, offsets for string table and typed payload arena, optional checksum in debug mode, and feature bitsets for negotiation.

### Operation Model

Each operation should begin with a compact fixed prefix containing opcode, operation flags, target node identifier, and argument slots. Extended payload should be referenced into a typed arena for large data blobs.

### Payload Model

The payload strategy combines a per-batch UTF-8 string table with optional persistent token dictionaries for repeated symbols such as attribute names, class tokens, and event names. Typed payload blocks are used for text data, attribute/style/class deltas, and event metadata.

## Command Semantics

Apply should be two-phase. The first phase canonicalizes intent by collapsing redundant writes, resolving last-write-wins updates, and identifying fast paths such as append-only, clear-all, and replace-all. The second phase commits in stable layers, with structure-first and scalar/visual updates after structure, so apply behavior remains deterministic.

Reorder semantics should prefer explicit move operations over remove-plus-add for identity preservation. For high-cardinality reorders, an optional compact permutation block may be negotiated, with explicit move fallback always available.

## Event Path

Event capture should stay delegated at root or document level and encode packets with event type identity, target node identity, timestamp, lane, and payload reference. The lane model should distinguish lossless discrete intent from coalescible continuous interaction and low-priority noise. Under pressure, coalescing increases on lower lanes while preserving high-priority intent.

## Scheduling Model

JS apply should commit at requestAnimationFrame boundaries. Any microtask follow-up should remain strictly bounded to avoid render starvation. A per-frame budget policy should split oversized batches by priority and defer non-critical work with anti-starvation aging rules.

## Memory and Ownership

The lifecycle contract should be explicit: produced, published, applied, acknowledged, recyclable. Runtime ownership is held until JS acknowledgment, and JS may not retain references after acknowledgment. Slab pools should back typed arenas and string-table buffers, with optional debug poisoning for recycled memory to expose lifetime bugs.

## Reliability and Compatibility

Ordering should be monotonic and in-order within session scope. Idempotency should be guaranteed at batch granularity via (sessionId, batchId). A bounded replay window should allow gap recovery. Version and feature negotiation in handshake should enable graceful fallback when optional capabilities are unavailable.

## Security Model

Structured operations should be default. Raw HTML insertion should only exist behind explicit trusted mode and policy gate. Dangerous sinks should be allowlisted and validated. Offset and length bounds must be checked at decode boundaries. URL sinks should pass sanitization policy. Protocol handlers should avoid eval-like execution behavior.

## Instrumentation and KPIs

Core spans should cover batch build, publish, apply, acknowledgment, event capture, and managed dispatch. Core metrics should include operation volume, payload size, coalescing ratio, per-opcode cost, frame-over-budget rate, ring depth, drop/coalesce counts by lane, replay/duplicate counters, and GC correlation.

Primary success criteria are lower p95 and p99 frame apply latency, reduced frame-over-budget frequency under interaction load, and no correctness regressions in ordering, replay, or idempotency.

## Phased Rollout Plan

Phase 0 establishes baseline observability and dashboards. Phase 1 adds canonicalization plus lane-aware scheduling without wire changes. Phase 2 introduces explicit event-ring flow control. Phase 3 adds replay and negotiation envelope semantics. Phase 4 adds optional reorder compression blocks.

Each phase should be gated by feature flags, validated by differential correctness checks against the current path, and canaried with A/B telemetry before broad enablement.

## Expected Gains vs Current In-Memory Binary

High-probability gains are improved tail latency, reduced jank under event pressure, and stronger protocol evolution guarantees. Lower-probability gains are further wire-level encoding wins, since binary in-memory transport already captures most serialization benefits.

## Risks and Mitigations

Risks include implementation complexity, semantic regression from incorrect coalescing, and heuristic tuning overhead across devices and browsers. Mitigation strategy is incremental rollout, strict differential testing, and observability-first deployment.

## Recommendation

Treat this proposal as an incremental architecture roadmap, not a big-bang rewrite. Prioritize canonicalization and scheduler discipline first, then event backpressure lanes, then reliability envelope features, then optional reorder compression. Practical principle: once binary in-memory transport exists, the largest gains come from reducing DOM work and stabilizing frame-time behavior.
