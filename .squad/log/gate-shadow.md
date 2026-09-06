<!--
Lifecycle: this file is durable evidence, not a scratch log. It is TRACKED
(see the `.gitignore` `!.squad/log/gate-shadow.md` negation, alongside
`lexicon-hits.md` and `pass-cost.md`) precisely so a clone or a log rotation
cannot discard it.

`enforce-reviewer-readonly.sh` appends one `SHADOW-ALLOW` line every time the
CI-6 shadow period (`.squad/.gate-shadow`) allows a verdict-cache write that
the enforcing control would refuse. Rows accumulate only while a shadow is
active; once `expires:` in `.squad/.gate-shadow` lapses and the control
starts enforcing, no new rows are written for that control.

Do not delete this file's rows at flip. A shadow with zero rows and a shadow
that was never observed are indistinguishable without them — the accumulated
rows are the record that answers "did shadowing this control actually cost
anything," which is the evidence CI-6 requires before a future shadow period
can cite this one as precedent. Append future shadow periods' rows below any
existing ones; never edit or truncate history already written here.
-->
