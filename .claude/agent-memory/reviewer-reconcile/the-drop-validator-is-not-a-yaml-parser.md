---
name: the-drop-validator-is-not-a-yaml-parser
description: scribe-decision-merger.sh validates decision drops with a hand-rolled line parser, so structurally invalid YAML front-matter merges instead of quarantining — validate your own drop before writing it
metadata:
  type: feedback
---

`scribe-decision-merger.sh`'s `validate()` is a hand-rolled Python line parser
(`import sys, re, unicodedata` — **no `yaml`**). It checks the fields it knows about
(`agent`, `verdict`, `scope`, `commit`, verdict↔blockers consistency) and does not check
that the front-matter is well-formed YAML at all.

**Why:** on `presentation-demo` (2026-09-07) I wrote a drop through an unquoted bash heredoc.
The shell collapsed `\\**` to `\*`, producing `found unknown escape character '*'` inside a
double-quoted scalar — `yaml.safe_load` rejects it outright. The merger **archived it as
valid and appended it to `.claude/docs/decisions.md`**; `.squad/decisions/quarantine/` stayed
empty. I then wrote a corrected drop, and the register ended up with two entries for one
review, one of them a malformed draft. I cannot clean that up — the readonly hook confines me
to my own outputs — so it had to be escalated as manual cleanup.

**How to apply:**

1. **Write drops with a quoted heredoc** (`<<'EOF'`), never an unquoted one, and substitute
   `commit`/`created` afterwards with `sed` on placeholders. An unquoted heredoc eats
   backslashes and `$`.
2. **Use YAML block scalars (`>-`) for every prose field.** Backslashes, quotes and colons are
   all literal inside them, so `reason:` text containing `<None Remove="content\**" />` or an
   apostrophe needs no escaping. Double-quoted scalars are where this bites.
3. **Parse it yourself before you stop:** `python3 -c "import yaml; yaml.safe_load(open(p).read().split('---',2)[1])"`.
   The hook will not catch it for you, and it fires on `SubagentStop` — by then the drop is
   already merged.

The gap itself is worth reporting when observed, but it is a hook defect, not a finding
against whatever change is under review. Related: [[the-clock-field-is-a-toolchain-job]].
