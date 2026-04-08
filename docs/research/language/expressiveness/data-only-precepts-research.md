# Data-Only Precepts: Research & Rationale

Research grounding for [#22 — Data-only precepts](https://github.com/sfalik/Precept/issues/22).

## The Mixed-Tooling Problem

Precept's original design required every precept to have a state machine. This means entities without workflow needs (reference data, configuration, simple domain objects) must be modeled in a separate tool (Zod, FluentValidation, JSON Schema). For domains where some entities have workflows and others don't, this creates:

- Two languages, two runtimes, two mental models
- No single source of truth for the domain
- Adoption barrier: users evaluate Precept for their complex entities, then realize they need a second tool for everything else

This problem was identified by Shane during backlog grooming (April 7, 2026).

## Precedent Survey

### DSL Precedents for Optional Complexity

| System | Simple Form | Complex Form | Optional? |
|--------|-------------|--------------|-----------|
| **Terraform** | Data sources (read-only) | Resources + lifecycle | Yes — coexist in same config |
| **Protobuf** | Message definitions (pure data) | Message + external behavior | Yes — same language |
| **GraphQL** | Queries (read schema) | Mutations/subscriptions | Yes — optional |
| **SQL DDL** | Table + CHECK constraints | Triggers, stored procedures | Yes — same DDL |
| **DDD** | Value objects (no identity) | Entities (identity + behavior) | Yes — same bounded context |

**Key finding:** None of these systems require the complex form. All let users start simple and add complexity as needed. Precept's "everything is a state machine" requirement was the outlier.

### Progressive Disclosure Principle

Established as a UX/design principle since 1985 (Apple HIG). Successful languages follow it:

- HTML: `<p>Hello</p>` → `<form>` + JS event handlers
- CSS: `color: red` → `@keyframes` + `calc()` + grid
- Python: scripts → classes → async

Precept violates this principle by requiring state machines from the start.

### DDD Pattern Match

Evans (2003) explicitly recognizes that a single domain contains both:
- **Value objects:** No identity, no behavior, immutable or simply-constrained data
- **Entities:** Identity, state, business rules, lifecycle

A domain modeling language that only supports entities forces value objects into a different tool.

## Design Decisions

### Editability: `edit all` / `edit Field1, Field2`

**Decision:** Root-level `edit` for stateless precepts. `all` as field quantifier (parallel to `any` as state quantifier).

**Alternatives rejected:**

| Option | Why Rejected |
|--------|-------------|
| All fields editable by default | Violates Principle #2 (no silent defaults). Breaks "locked by default." |
| `readonly` keyword | Inverts philosophy — stateful = locked, stateless = open. Inconsistent. |
| `edit any` | Overloads `any` across state and field domains. Fails glance test. `any` is the state quantifier. |
| No editability (invariants only) | Can't express read-only fields (IDs, timestamps shouldn't be user-editable). |

**Why `all` not `any`:** `any` is established as the state-domain quantifier (`in any edit`, `from any assert`). Using it for the field domain creates semantic collision. `all` is a distinct word for a distinct domain. Two quantifiers: `any` for states, `all` for fields. They compose: `in any edit all`.

**Graduation path:** Root-level `edit` is a compile error when states are declared. Compiler tells the user to use `in any edit all` or `in <State> edit ...`. No silent behavioral change.

### API: Nullable CurrentState, Undefined Results

**Decision:** `CurrentState` = null for stateless instances. `Fire()`/`Inspect(event)` return `Undefined` (not throw).

**Why Undefined over throw:** Consistent with how `Fire()` already handles "event not valid from this state." Callers already handle this outcome — no new error-handling pattern needed.

### Preview: Deferred

**Decision:** No stateless preview rendering in current panel. Deferred to the full preview panel redesign.

## Dead Ends Explored

### "Close #22 — Out of Scope"

The team initially recommended closing #22 (April 7, 2026). Arguments:
- "Precept's identity is state machines" — but this framed Precept as a state machine tool, not a domain integrity platform
- "All 20 samples are stateful" — circular: Precept doesn't support stateless, so no stateless samples can exist
- "Better served by Zod/FluentValidation" — ignores the mixed-tooling adoption barrier

Reversed after Shane's pushback on the mixed-tooling argument and the circular evidence problem.

### `edit any` Syntax

Proposed by Shane, evaluated and replaced with `edit all` after team analysis showed `any` was already claimed as a state quantifier. The semantic collision would cause confusion in documentation, autocomplete, and first-time learning.

## Related Issues

- **#22** — The proposal itself
- **#31** — Keyword logical operators (`and`, `or`, `not`) — same wave, no dependency
- **#9** — Conditional expressions (`if...then...else`) — same wave, no dependency
