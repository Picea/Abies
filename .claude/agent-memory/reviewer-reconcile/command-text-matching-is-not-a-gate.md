---
name: command-text-matching-is-not-a-gate
description: How to prove a PreToolUse hook that substring-matches command text is both false-positive and false-negative, in two probes
metadata:
  type: feedback
---

A hook that decides "is this a commit?" with `case "$command" in *"git commit"*)`
is not a gate. Prove it with two probes rather than arguing about it:

- **False negative:** payload command `git -C /other/repo commit -m "x"`. The
  literal substring `git commit` never appears, so the hook short-circuits to
  exit 0 and the gate is skipped entirely. Same for `git --git-dir=... commit`
  and `git -c user.name=x commit`.
- **False positive:** payload command `echo "see: git commit -m msg"` (or any
  heredoc/doc-writing command containing that text). The hook fires and
  evaluates the *project directory's* state, not the command's target.

**Why:** verified end-to-end against all four PreToolUse hooks in PR #355
(`enforce-conventional-commits`, `enforce-gpg-signing`, `enforce-no-secrets`,
`block-large-files`). The PR description named this exact defect class as its
reason for withholding two *other* hooks, without noticing the four it shipped
share it. I also tripped the false positive accidentally on my own tool call
while writing the test harness — the probability is demonstrably not
theoretical.

**How to apply:** any time a hook script gates on `$command` text, run both
probes before accepting it. The fix is to parse the invocation (argv, honouring
`-C`/`--git-dir`) rather than to add more substring cases. Related:
[[a-disclaimer-does-not-fix-a-false-claim]] — a PR that names a defect class it
also commits is a false claim, not a caveat.
