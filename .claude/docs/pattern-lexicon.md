# Pattern lexicon

The list of names the design pass watches for. Two mechanisms read this file:

- **`scope-warden.sh`** (`SubagentStop`, matching `architect`) — reports lexicon
  terms found in `00-scope.md`, the one artifact `dreamer-first-principles`
  reads. A pattern named in the scope is a solution handed to the track whose
  job is to derive one.
- **`lexicon-check.sh`** (`SubagentStop`, matching `dreamer-first-principles`) —
  **blocks** `01-track-a.md` when a term or a recall phrase appears in it, and
  sends the artifact back for a rewrite.

The two use the same vocabulary for opposite reasons. The warden asks *did the
conductor leak a solution in*; the check asks *did the track recall one out*.

> **Owner: `curator`.** Nothing else edits this file. Terms arrive through the
> curation loop — `dreamer-convergence` records which names reached the check,
> recurrence becomes a proposal, the proposal gets a counter-argument, and only
> then does a term land here. A lexicon that grows by anyone adding whatever
> annoyed them last week is decoration within six months.

## Why this is a maintained list and not an enumeration

Track A's deepest limit is that its weights are prior art, and no tool grant
reaches them. Prevention is unavailable; **detection is not.** This file is the
detector's vocabulary, and its calibration data is
`.squad/log/lexicon-hits.md` — every hit and every override, with the sentence
that caused it. The curator reads both columns of that log: recurring true hits
become new terms here, recurring false positives get narrowed or dropped.

An entry that fires often and is overridden every time is a bad entry. Remove
it. The check's value is entirely in being believed.

## How matching works

Terms are matched case-insensitively on **whole words**, with a `-` or a space
treated as interchangeable, so `event sourcing` catches `Event-Sourcing`. A term
containing a regex metacharacter is escaped, not interpreted — write plain
English here, not patterns.

To narrow an entry rather than delete it, make it longer: `state machine`
generates false positives on scopes that are legitimately about state machines;
`the state machine pattern` does not.

---

## Terms

### Architectural and structural patterns

- event sourcing
- CQRS
- command query responsibility segregation
- hexagonal architecture
- ports and adapters
- clean architecture
- onion architecture
- layered architecture
- microservices
- service mesh
- saga pattern
- outbox pattern
- inbox pattern
- circuit breaker
- bulkhead
- sidecar
- strangler fig
- anti-corruption layer
- backend for frontend

### Object and function patterns

- singleton
- factory pattern
- abstract factory
- builder pattern
- prototype pattern
- adapter pattern
- decorator pattern
- facade pattern
- flyweight
- proxy pattern
- chain of responsibility
- command pattern
- interpreter pattern
- iterator pattern
- mediator pattern
- memento pattern
- observer pattern
- state pattern
- strategy pattern
- template method
- visitor pattern
- repository pattern
- unit of work
- specification pattern
- null object pattern
- dependency injection
- inversion of control

### Data and concurrency

- write-ahead log
- two-phase commit
- optimistic concurrency
- pessimistic locking
- actor model
- publish subscribe
- pub/sub
- message queue
- eventual consistency
- read replica
- sharding
- consistent hashing
- bloom filter
- LRU cache
- copy on write
- immutable snapshot

### Named laws, theorems and principles

- CAP theorem
- PACELC
- Conway's law
- Amdahl's law
- Little's law
- Postel's law
- Hyrum's law
- open/closed principle
- single responsibility principle
- Liskov substitution
- interface segregation
- dependency inversion
- SOLID
- DRY
- YAGNI

---

## Recall grammar

Phrases that signal retrieval rather than derivation. These are matched the same
way as terms, and they matter more than the names above: a track that has
recalled a solution usually announces it in this grammar before it names it.

- the standard approach
- the conventional approach
- the usual approach
- the usual way
- commonly
- typically
- traditionally
- conventionally
- well-known
- well known
- the classic
- as everyone knows
- the industry standard
- best practice
- best practices
- widely used
- the canonical
- off the shelf
- textbook

### Removed terms, and why

Kept here rather than deleted, so the same term does not get re-proposed next
quarter by someone who has not seen it fail.

- **`prior art`** — removed on first observation. Track A is *required* to state
  its independence, and the natural sentence for that is *"No prior art, no
  pattern names, no how-this-is-normally-done."* The term fired on an artifact
  doing exactly the right thing. A term that flags correct behaviour is not a
  strict term, it is a broken one, and leaving it in would have spent the
  override budget on the check's own mistake.

---

## Hit log

`.squad/log/lexicon-hits.md`. Never rotated — it is the only calibration input
either mechanism has, and a hit log that ages out takes the evidence for
narrowing a term with it.
