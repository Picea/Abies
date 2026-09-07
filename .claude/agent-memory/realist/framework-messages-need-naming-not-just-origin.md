---
name: framework-messages-need-naming-not-just-origin
description: A classification rule that partitions messages by how they arrive will miss framework-defined messages that carry their own semantics — enumerate the named ones (UrlChanged) as well as the routes
metadata:
  type: feedback
---

When a plan classifies messages by **how they arrive** (enveloped vs bare, decided vs
interpreter-fed), also enumerate the **framework-defined messages by name** and say what each one
classifies as. A route-based partition is total over routes and silently wrong about any message
whose semantics the framework, not the application, defines.

**Why:** in the `undo-redo` pass the classification rule was made total over the two questions
"came through the envelope?" and "moved the projection?", and survived three Critic passes that
way. `UrlChanged` — delivered by the browser on a back/forward press — answers *yes/yes*, so it
`record`ed like a click. Application undo then restored a model without its URL and the browser's
own stack diverged from the application's history. The user found it after close-out. The plan had
already reasoned about `UrlChanged` once (origin re-basing, pre-record) and that partial treatment
made it *look* handled; the post-record case was never asked.

**And then check whether the name is actually the framework's.** The fix above was a rule keyed on
`origin is UrlChanged` — and `Navigation.UrlChanges(Func<Url, Message> toMessage)` lets the
*application* supply the constructor, so on the WASM head the framework never guarantees the type.
This repo's own `README.md` front-page example passes `url => new UrlChangedTo(url)` while Conduit
and every tutorial pass `url => new UrlChanged(url)`. A rule keyed on the framework type is green on
the fixture and silent for the adopter who followed the README. **Grep for who constructs the type
before keying a rule on it**; if the answer is "the application, through a `Func<…, Message>`", the
plan has an *application obligation*, not a mechanism, and it belongs stated beside the other
obligations the framework cannot discharge (here: the lens laws L1–L6 and the reachability
assumption), not buried in the classification rule.

**How to apply:** when the design wraps or intercepts a message path, list the messages the
framework itself dispatches (`UrlChanged`, lifecycle/bootstrap messages, interpreter feedback) and
give each a row. If one already appears in the plan under a *conditional* rule, that is a tell, not
a reassurance — ask what happens when the condition is false. See
[[message-provenance-has-no-seam]] for the origin-side half of the same failure, and
[[one-route-mitigations-repeat]] for why one treated site predicts a second untreated one.
