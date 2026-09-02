---
name: dependency-approval-trail-is-a-blocker
description: A new dependency without a Security Expert review trail blocks merge even when the package choice is obviously reasonable
metadata:
  type: feedback
---

New dependency additions in code-touching work must carry explicit
dependency-approval evidence. A missing Security Expert review trail is a merge
blocker **even when the package choices are reasonable**.

**Why:** Enforced 2026-05-06. The team's dependency approval policy already
requires review; the reviewer-side lesson is about the tempting exception. When
a package is well-known and obviously fine, waiving the trail feels like
pragmatism — but the policy's value is that it is unconditional, and an
approval trail that only exists for suspicious packages tells you nothing about
the ones nobody looked at.

**How to apply:** Check for the trail, not for the package's reputation. "This
is a fine choice" and "this was approved" are separate findings and the second
one is the blocking one. Route to the Security Expert rather than approving
conditionally. The underlying policy is in the team decisions file under
"Dependency Approval Policy".
