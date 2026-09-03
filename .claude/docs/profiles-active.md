# Profile Activation Pattern

> **Plaats deze file in:** `.claude/docs/profiles-active.md`

This file declares which optional profiles are active for the current project. The Lead reads this at session start, after `decisions.md` and `tech-stack.md`.

## Active profiles

```
# (uncomment lines for profiles to activate)
# - sovereignty
```

## How profiles work

A profile is a self-contained `.claude/docs/profiles/<name>.md` file that defines:

- Additional principles when active
- Optional additional subagents
- Routing table additions
- Skill router additions
- Statusline indicators (optional)

When activated, the Lead reads the profile file at session start (in addition to `principles-enforcement.md`, `decisions.md`, `tech-stack.md`).

Profiles **add** rules — they never override core principles. The core framework remains generic; profiles layer domain-specific concerns.

## Available profiles

| Profile | When to activate | Defined in |
|---------|------------------|------------|
| `sovereignty` | EU government / regulated industry / CLOUD Act sensitive | `.claude/docs/profiles/sovereignty.md` |

Add custom profiles by creating `.claude/docs/profiles/<name>.md` and listing them above.

## When NOT to activate profiles

For experiments, personal projects, public demos, or anything where the
overhead doesn't match the value — leave all profiles inactive. The core
framework works fine without any.

## Multiple profiles

Profiles can stack. Conflicts between profiles (rare) escalate to the user — the Lead asks which profile's rule wins for the conflicting case, then documents the decision in `decisions.md`.

## Adding a custom profile

1. Create `.claude/docs/profiles/<my-profile>.md`
2. Define: additional principles, subagents, routing, skill router rules
3. Document activation in this file under "Available profiles"
4. (Optional) Commit to your private template for reuse across projects
