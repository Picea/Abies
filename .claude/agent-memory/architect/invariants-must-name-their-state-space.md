---
name: invariants-must-name-their-state-space
description: An INV-n that says "application state" without naming the state space leaks past the Critic; cite the scope by id, never by line, because amendments move it
metadata:
  type: feedback
---

Two lessons from the `undo-redo` pass's third scope amendment (`[R4-nav]`,
2026-09-07), both cheap to apply and both expensive when skipped.

**1. Name the state space in the invariant, not in the reader's head.**
`00-scope.md`'s INV-2 said *"the resulting application state is one that some sequence of
ordinary actions could also have produced"* and its falsifier said **model**. Every phase
read "state" as "the model", so incoming browser navigation — which moves the location
without moving the model through the history — passed the Critic, the spec and close-out
untouched. The user found it after close-out.

**Why:** a falsifier is what the property generator is written from. If the falsifier
names one factor of a product state, the property quantifies over that factor alone, and
the invariant is green for a class of violations. Track A had already named the shape
(`model ⊗ world`) in `01-track-a.md`; the scope had not.

**How to apply:** when writing an `INV-n`, write the sentence *"the state this quantifies
over is ⟨enumeration⟩"* before writing the claim, and make the falsifier mention every
element of it. If the enumeration is long, that is information, not a smell. Test:
could a reviewer construct a violation that the falsifier's wording does not reach?

**2. Cite `00-scope.md` by invariant id and quoted clause, never by line range.**
The amendment lengthened INV-2 and staled four `00-scope.md:NNN` citations across
`04-realist-plan.md` and `06-spec.md` at once. The scope is a file that moves; the ids
exist to make it citable. Ids are stable for the life of a pass by rule — line numbers
are stable only until the first amendment.

Related: [[undo-redo-withhistory-decision]], [[user-chooses-typed-refusal-over-silence]].
