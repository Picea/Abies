---
name: the-merger-eats-the-drop-immediately
description: scribe-decision-merger consumes a decision drop within seconds of writing it — a follow-up check of the inbox path finds nothing; look in .squad/decisions/archive/<YYYY-MM>/ instead
metadata:
  type: project
---

`scribe-decision-merger.sh` does not wait for `SubagentStop` to consume a drop.
A drop written to `.squad/decisions/inbox/` is validated, merged into
`.claude/docs/decisions.md`, and **moved** to
`.squad/decisions/archive/<YYYY-MM>/<UTC-timestamp>-<original-name>.md` within
seconds — between two consecutive `Bash` calls, in the observed case.

**Why:** it matters because the natural next step after writing a drop is to
re-read it for a byte-level check (CR bytes, tabs or NBSP in the front matter,
`commit:` vs `git rev-parse HEAD`). That check hits `No such file or directory`
and reads as *"the write failed"* or *"the drop was rejected"* when in fact it
succeeded. The two outcomes are distinguished by **where** the file went, not by
its absence from the inbox:

- `.squad/decisions/archive/<YYYY-MM>/` — accepted and merged.
- `.squad/decisions/quarantine/` — malformed; the front matter did not validate.

**How to apply:** do the byte-level and `commit:`-vs-`HEAD` checks in the *same*
`Bash` call that writes the drop, before the merger can move it. If you need to
verify afterwards, glob the archive by the drop's own filename rather than the
inbox path, and confirm `.squad/.last-review-verdict` now names your `commit:` —
that pair is the positive proof the drop was accepted, and it is stronger
evidence than re-reading the file would have been.

Note the archive filename is re-stamped with the merger's UTC clock, not your
`created:` field, so the two timestamps differ by however long the drop sat in
the inbox. Do not treat that gap as a discrepancy.

Related: [[the-drop-validator-is-not-a-yaml-parser]] (what gets a drop
quarantined vs. silently merged), [[the-clock-field-is-a-toolchain-job]] (why
`created:` is yours to read live), [[a-confirmation-pass-invalidates-its-own-cache]]
(why the cache the merger just wrote is the thing to protect).
