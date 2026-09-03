---
name: prefix-stripping-fixes-are-unbounded
description: A parser that falls back to a permissive mode when a leading-junk prefix defeats its fence match cannot be fixed by enumerating character classes — move the check to fence presence.
metadata:
  type: feedback
---

When a validator has a **permissive fallback** (a "legacy mode", a "best effort"
branch, a "couldn't parse, assume old format" path), the interesting bug is never
the parse — it is *what reaching the fallback skips*. Ask that first.

**Why:** `scribe-decision-merger.sh` (PR #355, rounds 1–4). Its `LEGACY` branch
skips the agent whitelist, both enums, and the verdict↔blockers consistency check,
then `cat`s the drop verbatim into `decisions.md`. Round 3 I found a leading BOM
reached it; round 4 devops generalised the strip to *all* leading `Cf` characters
and the comment claimed that "closes the whole class". It did not. Six live
reproductions survived:

- one ASCII **space then** a BOM — a single non-Cf character in front defeats a
  `while category(text[0]) == "Cf"` loop entirely;
- leading `Cc` control characters (NUL, BEL) — not `Cf`, not `\s`;
- a leading `Mn` combining mark;
- BOM / newline / BOM (interleaving again);
- **a file written entirely with CR-only line endings** — a *regression* the same
  round's `newline=""` introduced, because the fence regex's `\r?\n` stopped
  matching. Pre-fix it quarantined correctly.

**How to apply:**
- Every "strip the bad prefix" fix is an enumeration over an open set. Push the
  check one layer out instead. Here, four lines:
  `if not fence_match: if re.search(r"(?m)^---", text): quarantine(); else: LEGACY`.
  A file that *contains* a fence but does not *start* with one is malformed, not
  legacy — that closes all six regardless of what the prefix is made of.
- When a fix changes an `open()`/decode/newline setting, re-test the **fence or
  header match itself**, not just the body parser. `newline=""` and `\r?\n` are
  coupled; changing one silently reroutes CR-only files.
- Encoding-level attacks are the cheap half to verify and usually already fail
  closed: UTF-16 and invalid UTF-8 hit `errors='strict'` and quarantine. Check
  them, then spend the time on the fallback branch.
- Calibrate severity by what the fallback *can* reach. This one cannot write
  `.squad/.last-review-verdict`, so it stayed below the forgery blocker — but it
  can inject unvalidated content into the authoritative conventions file, which
  is enough to block on when the fix is four lines.

**Round 5 correction — my own prescribed fix was wrong.** devops verified live
that `re.search(r"(?m)^---", text)` also matches a `---` horizontal rule in the
BODY of a genuine front-matter-less legacy drop, so it would have hard-rejected
honest legacy content: a worse failure than the hole. I confirmed that
independently. Lessons:
- Before prescribing a fix to a permissive fallback, run it against a **genuine**
  instance of what the fallback exists to tolerate, not only against the exploits.
  A fix verified only on attacks has been tested on half the domain.
- The closure that does work is over category **classes**, not members:
  `unicodedata.category(c)[0] in ("C", "Z", "M")` — "not a Letter, Number,
  Punctuation or Symbol" — instead of `{"Cc","Cf","Cs","Co","Mn","Me"}`, which
  silently omits `Cn` (unassigned, and the home of the Default_Ignorable ranges
  U+2065 / U+FFF0 / U+E0002 / U+E0080 that renderers must not draw) and `Mc`.
  Enumerating six members instead of one is still an enumeration.
- A file with an opening fence and **no closing fence** also falls to the
  permissive branch, needs no exotic bytes at all, and is the most reachable
  route of the lot. Check the fallback's *other* entry points, not just the one
  the current round is fixing.

Related: [[safety-caps-must-fail-closed]],
[[line-splitting-fixes-must-start-at-the-open-call]],
[[guarding-a-field-is-not-authenticating-it]],
[[a-disclaimer-does-not-fix-a-false-claim]].
