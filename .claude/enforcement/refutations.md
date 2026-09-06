# Refutation and Registration Ledger

This file holds **three disjoint kinds of entry**, per
`.squad/design/pathless-read-blindness/11-continuous-improvement-criterion.md`
§ 3.1:

| `type:` | Subject | Introduced by |
|---|---|---|
| *(refutation row, no `type:` field)* | an `S ⊋ R` observation for `(role, capability)` | `04-realist-plan.md` / BC-0 |
| `residual` | a finding not fixed in this changeset | `11-continuous-improvement-criterion.md` |
| `bound` | a structural limit no changeset can fix | `11-continuous-improvement-criterion.md` |

**They do not interact.** A `residual` or `bound` entry neither creates nor
clears an `absence-refuted` state, and is not a clearing route for a
refutation row. Routes A and E (`07-handoff.md` BC-1/BC-2) remain the only
ways to clear a refutation ledger entry. If an audit cannot tell these three
kinds apart, that is a defect in the audit, not licence to merge the
semantics.

This file is **append-only**. Extensions are appended, never edited in
place, cite the entry they extend by its position in this file, and **may be
authored only by the user** — an agent that extends its own expiry has built
the laundering shape the fields exist to prevent.

No entry in this file raises a published level. Registration records the
ceiling a rule's current evidence supports; it never claims more than that.

---

## Clarifications (2026-09-05)

Appended text only. Neither item edits an existing entry in place; both
resolve a reference that existing entries left unresolved for a reader of
this repository.

- **The D36 reference in entry 2's `remedy:` field.** That field points at
  the Lead's off-repo decision log by id. The fact it points at, stated
  directly: GitHub branch protection is unavailable on this repository's
  plan tier; the commit-gate hooks are the only merge control (Lead
  measurement, 2026-09-04).
- **`R-gate-classification` and `R-review-before-commit`**, used by entries
  1 and 3 without being defined in this file. Both are named and defined
  once, in `10-architect-ruling-classifier.md` § Q-G, which is their
  origin:
  - `R-gate-classification` — the gate refuses the classified shapes.
  - `R-review-before-commit` — no code-shaped change is committed without
    a PASS.

This section is carried over from the template as reference material for
the terms it defines. It resolves references made by template-originated
entries; this repository's own ledger below carries no entries of its own
yet, so nothing here currently has an entry to resolve against. Kept
because the definitions of `R-gate-classification` and
`R-review-before-commit` are load-bearing vocabulary for any entry this
repository's own agents register in the future.

---

## Refutation rows

None yet.

---

## Residuals and bounds

None yet.

---

## Extensions

Extensions are appended here, never edited in place, per the append-only
rule above. Each extension cites the entry it extends by its position in
this file and **may be authored only by the user** — an agent that extends
its own expiry has built the laundering shape the fields exist to prevent.
No extensions have been authored for this repository's ledger yet.
