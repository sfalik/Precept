# Domain Integrity: Formal Concept vs. Precept's Positioning

**Date:** 2026-04-19
**Author:** Frank (Lead/Architect)
**Research Angle:** Terminological and conceptual grounding for Precept's "domain integrity" positioning
**Purpose:** Determine whether Precept's use of "domain integrity" is aligned with, in tension with, or a deliberate extension of the formal relational and DDD literature — and whether the terminology is coherent enough to stand in public-facing positioning.

---

## Executive Summary

**Verdict: Coherent extension — but the lineage needs to be stated, not assumed.**

Precept's use of "domain integrity" is not a misuse, but it is also not a direct import of any single term from the literature. The formal relational model defines *domain integrity* narrowly: column values must belong to their declared type domain. Precept borrows the phrase but uses "domain" in the DDD sense — the *business domain* — and uses "integrity" to mean the full lifecycle-aware correctness of an entity's data configuration, not merely type conformance for a column. These are different levels of discourse operating at different architectural layers.

The DDD aggregate model is the closer intellectual ancestor. Aggregate invariants — rules that must hold across all state changes within a consistency boundary — are structurally equivalent to what Precept calls `rule` and `ensures` declarations. Precept is, in effect, the aggregate root pattern expressed as a declarative DSL with compile-time proof, not just a runtime enforcement convention.

Three conclusions:

1. **"Domain integrity" as a product label is coherent** when "domain" is read in the DDD/business sense. It is NOT coherent if the reader's reference point is the relational model, where domain integrity is a narrow column-type constraint. The philosophy should acknowledge this explicitly.

2. **"Governed integrity"** — already used in `philosophy.md` — is the better *specific* term for Precept's guarantee. It adds what neither relational domain integrity nor DDD aggregate invariants supplies: lifecycle-awareness, structural prevention (not detection), compile-time proof, and inspectability. The product should use "domain integrity engine" as the category label and "governed integrity" as the precise descriptor of what the engine provides.

3. **"Aggregate integrity" would be incorrect.** Precept is not implementing DDD aggregates (which span multiple objects). Precept governs a single entity type. The aggregate pattern is the implementation pattern developers use when they *don't* have Precept; Precept replaces the need for it.

---

## Survey Results

### 1. Wikipedia — Data Integrity (Relational Model)

**Source:** https://en.wikipedia.org/wiki/Data_integrity
**Retrieved:** 2026-04-19

The article defines three canonical relational integrity types:

- **Entity integrity:** every table has a non-null, unique primary key. Governs identity.
- **Referential integrity:** foreign-key values must reference valid primary-key values. Governs cross-table relationships.
- **Domain integrity:** all column values must belong to the declared domain for that column. A domain is "a set of values of the same type — pools of values from which actual values appearing in the columns of a table are drawn." Governs value type conformance.

A fourth category, *user-defined integrity*, is acknowledged for rules that don't fit the first three. The article's framing is storage-layer: integrity constraints are properties of a relational schema, enforced by the database system.

**Relevance to Precept:** Precept's field declarations and scalar type constraints are the application-layer analogue of relational domain integrity. But Precept extends this in three ways the relational model does not address: (a) cross-field constraints, (b) lifecycle-conditional constraints, and (c) compile-time structural proof. Relational domain integrity says nothing about how values may change over time or what state the entity is in.

---

### 2. Martin Fowler — DDD Aggregate (bliki)

**Source:** https://martinfowler.com/bliki/DDD_Aggregate.html
**Retrieved:** 2026-04-19

Fowler's summary: *"A DDD aggregate is a cluster of domain objects that can be treated as a single unit. An aggregate will have one of its component objects be the aggregate root. Any references from outside the aggregate should only go to the aggregate root. The root can thus ensure the integrity of the aggregate as a whole. Transactions should not cross aggregate boundaries."*

The key phrase: "the root can thus ensure the integrity of the aggregate as a whole." This is integrity enforcement through encapsulation — the aggregate root's methods are the only way to mutate the aggregate, so it can validate invariants before committing changes.

**Relevance to Precept:** Precept's engine *is* the aggregate root — but externalized and declarative. In a hand-rolled DDD aggregate, the root's methods contain guard checks and throw exceptions on invariant violations. Precept externalizes those invariant declarations into a `.precept` file and makes them compile-time verified and runtime-enforced without a single exception in domain method code. The integrity guarantee is the same; the mechanism is fundamentally different.

---

### 3. Wikipedia — Domain-Driven Design

**Source:** https://en.wikipedia.org/wiki/Domain-driven_design
**Retrieved:** 2026-04-19

DDD defines *domain* as "the subject area to which the user applies a program" — the business problem space. Aggregate roots "check the consistency of changes in the aggregate." Entities are defined by identity; value objects by attributes. The bounded context defines where a domain model is consistent and valid.

The word "domain" in DDD has *nothing to do* with the relational model's domain (a set of valid column values). This is the fundamental terminological split: relational theory uses "domain" for a value type; DDD uses "domain" for a business subject area.

**Relevance to Precept:** Precept uses "domain" in the DDD sense — the business domain. "Domain integrity" in Precept means "the integrity of entities in the business domain," not "column values belong to their type domains." This reading is coherent; it just needs to be stated rather than assumed.

---

### 4. Microsoft — Domain Model Layer Validations (.NET Microservices)

**Source:** https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-model-layer-validations
**Retrieved:** 2026-04-19

The authoritative Microsoft guidance on DDD validation in .NET: *"In DDD, validation rules can be thought as invariants. The main responsibility of an aggregate is to enforce invariants across state changes for all the entities within that aggregate. Domain entities should always be valid entities... Invariant rules are simply expressed as contracts, and exceptions or notifications are raised when they are violated."*

The guidance also acknowledges the failure mode of detection-first approaches: objects end up in states they should never have been in. Services downstream check for conditions they shouldn't have to check because they can't trust the upstream entity was validated. The proposed solutions (Specification pattern, Notification pattern) are all detection-first at runtime, not structural prevention.

**Relevance to Precept:** This document exactly describes the problem Precept solves, using the vocabulary of the DDD community. Precept's `rule` declarations *are* invariants. The engine *is* the aggregate-enforcing invariants on state changes. What Precept adds beyond this document's recommended patterns: (a) the invariants are declared in a single file, not scattered in entity methods; (b) they are compile-time verified for consistency and completeness; (c) they are structurally enforced — no bypass path exists — rather than checked by convention when entity methods are called.

---

### 5. InfoQ — DDD Aggregates and Sharing

**Source:** https://www.infoq.com/articles/ddd-aggregates-sharing/
**Status:** 404 — page not found.

**From knowledge:** Greg Young's writing on aggregates (including his influential 2010 CQRS essays) distinguishes between *invariants* (rules that must always hold within a consistency boundary) and *business rules* (which may span boundaries and require sagas or process managers). Young's contribution: the aggregate boundary is a *correctness boundary*, not just a transactional one. If a rule can only be enforced by holding two aggregates in a lock, the rule is probably wrong — it should be relaxed to an eventual consistency guarantee, or the aggregate boundary should be redrawn.

**Relevance to Precept:** Precept explicitly governs a single entity type — the correctness boundary is the entity itself. It does not attempt cross-entity invariants. This aligns precisely with Young's position: the right scope for structural invariants is the entity/aggregate, not a distributed lock across two aggregates.

---

### 6. Wikipedia — Domain-Driven Design (Supplementary — Aggregate Consistency)

See source 3. The DDD aggregate model was already covered. The additional point relevant here: DDD's bounded context defines where a domain model is "consistent and valid." Precept's `.precept` file is a micro-level bounded context — a single-entity domain model with declared consistency rules.

---

## The Relational Definition of Domain Integrity

In C.J. Date's relational theory, a **domain** is a named, typed set of permissible scalar values. The domain `Age` might be "non-negative integers"; the domain `OrderStatus` might be the enumeration `{Draft, Submitted, Approved, Rejected}`. **Domain integrity** means that every column in every table draws its values from its declared domain — no value outside the declared set may appear.

In SQL, domain integrity is enforced by:
- Data types (`INT`, `VARCHAR(100)`, `DATE`)
- `NOT NULL` constraints
- `CHECK` constraints
- `ENUM` types or lookup-table foreign keys

Domain integrity is fundamentally a *type constraint at the storage layer*. It is static (the domain doesn't change based on the entity's lifecycle position), single-column (it governs one column's values independently), and storage-level (it is the database's responsibility, not the application's).

**Where Precept relates:**

Precept's field type declarations are the closest analogue:

```precept
field status: string
field quantity: number
field approvalDate: string
```

Declaring `quantity: number` is analogous to assigning the `quantity` column to the `Number` domain in relational theory. Precept extends this in ways the relational model does not support:

1. **Cross-field constraints** — `rule "total = quantity * unitPrice"` spans multiple fields. The relational model handles this only in CHECK constraints (limited and inconsistently supported) or triggers (bypassing the domain integrity model entirely).

2. **Lifecycle-conditional constraints** — `ensures "approvalDate is set" when Approved` activates a constraint only in a specific lifecycle state. The relational model has no lifecycle model; it treats all rows as having the same constraint set regardless of any "status" field value.

3. **Compile-time structural proof** — Precept's proof engine verifies at compile time that constraints are satisfiable and internally consistent. Relational databases verify at runtime, against actual data. Precept moves the detection boundary leftward: definition time, not operation time.

4. **Inspectability** — Precept's `Inspect` operation shows what every event would do without committing. The relational model has no equivalent — you cannot ask a database "what would happen if I ran this update?" without running it in a transaction and rolling back.

**Summary of the gap:** Precept's field-type declarations *subsume* relational domain integrity (they enforce the same column-type guarantees). But Precept's full rule surface extends far beyond what the relational model calls domain integrity. Precept is not "relational domain integrity at the application layer" — it is something broader that includes relational domain integrity as a subset of its enforcement.

---

## The DDD Aggregate Model

Evans's aggregate pattern addresses a different level of discourse: application design, not storage schema. The aggregate is a cluster of objects governed by a root that enforces invariants across the cluster. The invariant is the application-layer equivalent of the database's domain integrity constraint — but it applies to object graphs, not columns.

From Evans (*Domain-Driven Design*, 2003): *"Invariants are consistency rules that must be maintained whenever data changes."* The aggregate root is the enforcement point: external code can only mutate the aggregate through the root's interface, so the root can check invariants before every mutation commits.

**Aggregate invariants in practice (the pre-Precept pattern):**

```csharp
public class Order // aggregate root
{
    private List<OrderLine> _lines;
    private OrderStatus _status;

    public void AddLine(Product product, int quantity)
    {
        if (_status != OrderStatus.Draft)
            throw new InvalidOperationException("Cannot add lines to a non-Draft order.");
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.");
        _lines.Add(new OrderLine(product, quantity));
    }

    public void Submit()
    {
        if (!_lines.Any())
            throw new InvalidOperationException("Cannot submit an order with no lines.");
        _status = OrderStatus.Submitted;
    }
}
```

This is the aggregate invariant pattern. Every state mutation checks rules; every rule violation throws. The invariants are scattered across methods. No compile-time verification that the rules are complete or non-contradicting. No way to ask "what would happen if I called Submit?" without calling it in a transaction. No single source of truth for the entity's complete rule set.

**Where Precept relates:**

Precept takes the *concept* of aggregate invariants and provides it as a DSL with structural properties the OO pattern lacks:

| DDD Aggregate Pattern | Precept Equivalent |
|---|---|
| Aggregate root methods check invariants | `rule` declarations checked by engine on every operation |
| Exceptions thrown when invariant violated | `Rejected` or `ConstraintFailure` outcome returned |
| Method-per-mutation with ad hoc guard checks | `event` declarations with typed `transition` rows and guards |
| No compile-time verification | Type checker + Proof Engine verify structural soundness at compile time |
| No inspectability (must execute to know) | `Inspect` operation — read-only preview of all events |
| Invariants scattered across methods | All rules in one `.precept` file |
| Lifecycle position tracked by `_status` field | `state` declarations — first-class lifecycle model |

Greg Young's framing strengthens this further: the aggregate correctness boundary is the entity itself, not a distributed lock. Precept's single-entity scope is exactly right for structural invariants. Cross-entity rules that cannot be enforced in a single `.precept` file are, by Young's reasoning, either candidates for eventual consistency or evidence of a wrong aggregate boundary.

**What DDD aggregate invariants do NOT address that Precept does:**

1. **Editability rules.** DDD doesn't formalize which fields may be directly edited at which lifecycle positions. Precept's `edit` declarations do.
2. **Compile-time completeness.** No DDD tool verifies at compile time that all invariants are satisfiable or that no two transitions leave the entity in a contradicting rule state.
3. **Inspectability.** DDD has no concept of "preview all possible operations without executing." Precept's `Inspect` provides this.
4. **Stateless entities.** DDD aggregates are defined by their lifecycle and identity. Precept governs stateless entities (data records, configuration objects) with the same rule language.

---

## Synthesis: Precept's Position in the Integrity Hierarchy

The relational model and DDD describe integrity at different architectural layers. Precept spans both and adds a third:

```
Layer 1 — Storage Integrity (Relational Model)
  Entity integrity:     primary keys are unique and non-null
  Referential integrity: foreign keys reference valid primary keys
  Domain integrity:     column values belong to declared type domains
  → Enforced by the database. Schema-level. Static (no lifecycle awareness).

Layer 2 — Application Domain Integrity (DDD Aggregate Invariants)
  Invariants:           rules that must hold across all state changes within an entity
  Consistency boundary: the aggregate root is the sole mutation point
  → Enforced by aggregate root methods. OO-level. Runtime only. Detection-first.

Layer 3 — Governed Integrity (Precept)
  Field integrity:      field values conform to declared types and constraints (⊇ Layer 1)
  Cross-field rules:    relationships between field values that must hold (extends Layer 1)
  Lifecycle rules:      state-conditional constraints and transition guards (absent in L1)
  Structural prevention: no bypass path — invalid configurations cannot exist (extends L2)
  Compile-time proof:   structural soundness verified before any instance exists (new)
  Inspectability:       all possible operations visible without execution (new)
  One-file completeness: entire rule set in a single declaration (new)
```

**Position statement:** Precept's "domain integrity" subsumes relational domain integrity (Layer 1) and formalizes DDD aggregate invariants (Layer 2), then adds a third dimension — governed integrity — that neither layer provides: lifecycle-awareness, compile-time proof, structural prevention, and inspectability.

Precept is not merely a Layer 2 tool that replaces hand-rolled aggregates. It is a Layer 3 tool that enforces integrity guarantees neither the storage layer nor the application layer pattern can provide on their own.

**The scope boundary:** Precept governs integrity *within* a single entity type. Referential integrity *across* entities (Layer 1's second category) and cross-aggregate invariants (the DDD pattern that Greg Young argues should be relaxed) are explicitly out of scope. This is a deliberate, coherent boundary — not a limitation. It matches the ownership boundary of the entity.

---

## Is the Terminology Coherent?

### The problem with "domain integrity" as a label

The phrase "domain integrity" has a prior art problem. To a database practitioner, it means "column values belong to their type domain" — a narrow, static, storage-layer concept. Using the same phrase for a runtime enforcement engine that does lifecycle-aware, cross-field, compile-time-verified constraint enforcement is a stretch. A database DBA reading "domain integrity engine" will underestimate Precept's scope.

To a DDD practitioner, "domain" means "business domain" — so "domain integrity engine" reads as "the thing that keeps business domain entities valid." This is the correct reading for Precept. But practitioners who haven't internalized DDD vocabulary will still land on the relational definition.

### Is "aggregate integrity" better?

No. "Aggregate integrity" would imply Precept implements the DDD aggregate pattern — multi-object clusters with a root. Precept governs a single entity type, not a cluster. And the aggregate pattern is the implementation workaround that developers use when they don't have Precept; conflating the two would muddy the positioning.

### Is "governed integrity" better?

For the *specific guarantee* Precept provides, yes. "Governed integrity" precisely names the property: the entity's data is governed by its declared contract, on every operation, structurally, not by convention. This is not the same as "checked integrity" (validation) or "type integrity" (relational domain integrity) or "aggregate integrity" (DDD aggregate invariants). "Governed integrity" is Precept's specific contribution.

**Recommendation:**

Use the two terms at different levels:

- **"Domain integrity engine"** — the *category label* for the product. It correctly positions Precept in the broad space of "things that enforce integrity on business domain entities." The DDD reading of "domain" is correct. The relational reading is too narrow but the phrase still parses sensibly. This is the right label for the tagline.

- **"Governed integrity"** — the *precise term* for the guarantee the engine provides. Use this when describing the specific properties: prevention, completeness, lifecycle-awareness, determinism, inspectability. This is already present in `philosophy.md` and should be the load-bearing term when the philosophy makes claims about what the engine guarantees.

The two terms are complementary: Precept is a domain integrity engine because it governs integrity for business domain entities; it provides governed integrity because the contract governs what the entity's data is allowed to become, structurally, on every operation.

The philosophy should not abandon "domain integrity" as the label — it is correct and intuitive in context. But it should explicitly position "governed integrity" as the name for the *specific property* that distinguishes Precept from validation, from state machines, from aggregate patterns, and from database constraints. The distinction between the category label and the specific guarantee is worth one sentence of explicit framing.

---

## Implications for philosophy.md

1. **Add a note on the terminological lineage of "domain integrity."** The philosophy currently uses "domain integrity engine" as if the phrase is self-evident. It is not, to practitioners whose reference point is the relational model. One sentence: "The word 'domain' is used in the DDD sense — the business domain, not the relational model's typed value domain — and 'integrity' extends across the entity's full lifecycle, not just individual field types."

2. **Elevate "governed integrity" as the technical term for the guarantee.** The philosophy already uses this phrase ("The unifying principle is governed integrity"). It should be stated that "governed integrity" is the specific term for Precept's contribution — what distinguishes it from relational domain integrity (narrower), from DDD aggregate invariants (no compile-time proof, no inspectability), and from validation (detection-first, not structural prevention).

3. **Name the DDD aggregate pattern as the implementation analog.** The current positioning section compares Precept to "pure state machines" and "pure validators" but does not name DDD aggregate roots explicitly. Adding one sentence that positions Precept relative to the aggregate pattern would be useful: "The aggregate root pattern in DDD provides invariant enforcement through encapsulation; Precept provides the same guarantee through a declarative contract that is compile-time verified, structurally enforced, and fully inspectable — without requiring aggregate root boilerplate in domain code."

4. **Keep the relational database comparison accurate.** The current philosophy says: "Database constraints and triggers: SQL CHECK constraints and triggers are the original declarative constraint mechanism — and they work. But they operate at the storage layer: no lifecycle model, no inspectability..." This is accurate. It should be read alongside the above note on "domain integrity" lineage so readers understand Precept is extending, not competing with, the relational integrity model at the storage layer.

5. **No changes needed to core claims.** The prevention, one-file completeness, inspectability, and determinism claims are all supported by the literature. These are not borrowed from any prior term — they are Precept's specific contributions beyond the prior art. The philosophy's framing of "governed integrity" as the unifying principle is the right anchor.

---

## References

1. Wikipedia contributors. "Data integrity." Wikipedia, The Free Encyclopedia. Retrieved 2026-04-19. https://en.wikipedia.org/wiki/Data_integrity
   - Canonical summary of relational integrity types: entity, referential, domain, user-defined.

2. Fowler, Martin. "DDD_Aggregate." martinfowler.com, 23 April 2013. Retrieved 2026-04-19. https://martinfowler.com/bliki/DDD_Aggregate.html
   - Aggregate root as integrity enforcer for a cluster of domain objects.

3. Wikipedia contributors. "Domain-driven design." Wikipedia, The Free Encyclopedia. Retrieved 2026-04-19. https://en.wikipedia.org/wiki/Domain-driven_design
   - DDD vocabulary: aggregate, bounded context, entity, value object, invariants.

4. Microsoft. "Design validations in the domain model layer." .NET Microservices Architecture for Containerized .NET Applications. Retrieved 2026-04-19. https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-model-layer-validations
   - "In DDD, validation rules can be thought as invariants. The main responsibility of an aggregate is to enforce invariants across state changes."

5. Evans, Eric. *Domain-Driven Design: Tackling Complexity in the Heart of Software.* Addison-Wesley, 2003.
   - Primary source for aggregate invariants, bounded context, and the meaning of "domain" in DDD.

6. Date, C.J. *An Introduction to Database Systems*, 8th ed. Addison-Wesley, 2003.
   - Primary source for relational domain integrity as typed column value constraints.

7. Young, Greg. "CQRS and Event Sourcing" (various essays, 2010–2015).
   - Aggregate correctness boundaries and why cross-aggregate invariants should be relaxed to eventual consistency.

8. Precept project. `docs/philosophy.md`. Retrieved 2026-04-19.
   - Primary source for Precept's "domain integrity engine" and "governed integrity" claims.
