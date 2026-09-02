---
name: correlated-artifacts-share-one-timestamp
description: Artifacts written for the same event must carry the identical ISO 8601 UTC timestamp, not each their own "now"
metadata:
  type: feedback
---

When one event produces several artifacts — a log entry, an orchestration
record, a decision drop — they all carry the *same* ISO 8601 UTC timestamp, not
each their own call to "now".

**Why:** Adopted 2026-03-27. Independently generated timestamps differ by
milliseconds to seconds, which makes correlating "what happened in this run"
across files a fuzzy-matching exercise instead of an exact join. Deterministic
correlation is the whole reason the timestamp is there.

**How to apply:** Compute the timestamp once at the start of the unit of work
and pass it to every artifact. This also applies to hand-authored docs that
reference a dated event: reuse the event's recorded timestamp rather than
writing today's date. Use UTC and the full ISO 8601 form
(`2026-03-27T08:03:52Z`), not a local or date-only rendering.
