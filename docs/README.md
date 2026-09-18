# Docs

## What these documents are

Everything in `docs/` describes the **target** — the system as it is meant to become, not as it currently is. The
project is in active development and the code under `src/`
lags behind, and in places contradicts, what is written here.

When the docs and the code disagree, **the code is the thing that's wrong.** These documents are the specification, not
a report.

## Why there is no matching set of "current state" docs

Because the current state already has documentation that can't go stale: the code, the generated OpenAPI document, and
the tests. A hand-written description of how things are today starts rotting the moment someone merges a PR, and it rots
silently — you end up maintaining two documents that are both wrong instead of one that is honestly aspirational.

`AGENTS.md` is the one exception: a short orientation file for whoever (or whatever)
needs to navigate the code as it stands right now. It is expected to be stale between refactors and says so at the top.
It is not a second specification.

## How to tell how far along something is

Each document ends with an **Implementation status** table listing its entities or modules against one of:

| Marker | Meaning                                                               |
|--------|-----------------------------------------------------------------------|
| ⚪     | Planned — described here, nothing in `src/` yet                       |
| 🟡     | Partial — exists in `src/`, but diverges from what this document says |
| 🟢     | Done — `src/` matches this document                                   |

The rules that keep the tables useful:

- **A row goes 🟢 only when the code matches the doc**, not when the code merely exists. A 🟡 row is a promise that
  someone will come back to it.
- **When the doc changes, rows go backwards.** Rewriting a section to describe something better is a normal reason for
  🟢 to become 🟡 — that's the table doing its job, not a regression.
- **The banner comes off a document when its table is all 🟢.** At that point the document stops being aspirational and
  becomes a description, and it is maintained alongside the code from then on.

Status is the only thing in these docs that tracks reality, which is what keeps it cheap enough to stay accurate.
Everything else describes the target and doesn't move when code moves.

## Decisions

Non-obvious choices and their reasoning live inline in the document they affect, under **Open questions** while
undecided. A decision that is settled stops being a question and becomes prose — the reasoning stays, the uncertainty
doesn't.

## Contents

| Document                                     | Covers                                            |
|----------------------------------------------|---------------------------------------------------|
| [domain-model.md](domain-model.md)           | Bounded contexts, aggregates, relationships       |
| [data-model.md](data-model.md)               | Field-level entity and value object reference     |
| [project-structure.md](project-structure.md) | Solution layout, project boundaries, dependencies |
