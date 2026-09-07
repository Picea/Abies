---
name: extract-the-spec-fences-and-diff
description: For spec-by-example reviews, reassemble the code fences from the approved spec and diff against the committed file — it decides who owns the fix
metadata:
  type: feedback
---

When a changeset transcribes an approved spec (`06-spec.md`) into a real test
file, do not eyeball the correspondence. **Reassemble every `csharp` fence from
the spec in order and `diff -u` it against the committed file.**

```
grep -n '^```' 06-spec.md          # fence line numbers
python3 -c "...slice the ranges, concatenate, write /tmp/spec_extract.cs"
diff -u /tmp/spec_extract.cs <the committed file>
```

**Why:** the diff answers, mechanically, the one question that decides *who is
allowed to fix a defect*. A defect that appears in the extract is in the
**approved text** → `spec-author` amendment plus user re-approval. A defect that
appears only in the committed file is the **transcriber's** → `csharp-dev` fixes
it now. Getting that backwards either sends a blocker to an agent forbidden to
act on it, or lets a specialist quietly edit text the user approved.

On `undo-redo-pr0` this settled two compile-verified findings in one command
(both were `06-spec.md:1186`, `:505`, `:509` — approved text, not transcription)
and separately proved a nine-line file header was an *addition* (`grep -c` for
its first phrase in the spec returned `0`), so its false claim blocked on the
changeset's own account. Same command also showed the only semantic omission was
a bodiless `file static class Gen` stub, which was correct to drop.

**How to apply:** run it before writing any finding about a transcribed spec.
Expect three benign delta classes and treat anything else as a finding: (1) an
added file header; (2) the spec's markdown prose folded in as comments; (3)
whitespace from the format-on-save hook — check `.editorconfig` for the rule
that explains it (`csharp_preserve_single_line_statements = false` explained
every one of mine) rather than attributing it to the author. Related:
[[a-locked-file-was-never-type-checked]], [[inherited-or-introduced-decides-the-verdict]].
