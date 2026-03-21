# Instruction Sync Checklist

> Canonical source remains `.github/instructions/*`. Squad files are derived memory and operating context.

## Trigger

Use this checklist when any canonical instruction file is added, updated, renamed, or retired, or when squad behavior drifts from current repo instructions.

## Inputs

Review these canonical instruction files:

- `.github/instructions/abies.conference.brand.palette.instructions.md`
- `.github/instructions/abies.instructions.md`
- `.github/instructions/conduit.instructions.md`
- `.github/instructions/csharp.instructions.md`
- `.github/instructions/ddd.instructions.md`
- `.github/instructions/fluentui.instructions.md`
- `.github/instructions/memory.instructions.md`
- `.github/instructions/playwright-charp.instructions.md`
- `.github/instructions/pr.instructions.md`

## Checklist

- Read the changed canonical instruction file(s) and identify new rules, removed rules, and clarified constraints.
- Update `.squad/decisions.md` with durable team-level rules that affect routing, review, coding, testing, or delivery.
- Update `.squad/identity/wisdom.md` with stable operating guidance that should be available across sessions.
- Update relevant `.squad/agents/*/history.md` entries when a rule changes how a specific role should work.
- Update any impacted charter when behavior boundaries, approvals, or tool constraints changed.
- Preserve existing directives. Do not delete prior user directives unless they were explicitly superseded.
- Keep canonical meaning intact. Summarize or normalize wording only when the squad copy stays faithful to the source rule.
- Prefer additive updates over rewrites so decision history and prior intent stay legible.

## Validation

- Confirm each new or changed rule appears in the right derived squad file and is phrased consistently with the canonical instruction.
- Confirm no prior user directive was removed without an explicit superseding instruction.
- Run a quick grep-based verification for newly synced rules, for example:

```bash
rg -n "<key rule phrase 1>|<key rule phrase 2>" .squad/decisions.md .squad/identity/wisdom.md .squad/agents .squad/agents/*/charter.md
```

- If the grep misses an expected rule, update the derived squad file before closing the sync.

## Commit message suggestion

```text
chore: sync canonical instructions into squad memory
```
