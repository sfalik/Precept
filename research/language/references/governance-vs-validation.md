# Governance vs Validation — Reference

**Date:** 2026-04-10
**Author:** Frank (Lead/Architect & Language Designer)
**Relevance:** Grounds the philosophy's core positioning claim: "This is not validation — it is governance." Reference for design reviews, positioning decisions, and cross-system comparisons.

---

## Purpose

Precept's identity rests on a distinction the philosophy states directly: "Validation checks data at a moment in time, when called. Governance declares what the data is allowed to become and enforces that declaration structurally, on every operation, with no code path that bypasses the contract."

This document defines the distinction precisely, surveys how existing systems fall on the spectrum, catalogs the failure modes validation permits but governance prevents, and identifies where validation alone is sufficient. It is a reference — not a manifesto — intended to be cited when the distinction matters for a design or positioning decision.

---

## The Distinction Defined

### Validation

Validation is a **check invoked at a moment in time**. A validator receives data, evaluates rules against it, and returns pass or fail. The defining characteristics:

- **Invoked, not structural.** The validator runs when called. If no code path calls it, the rules do not apply.
- **Point-in-time.** The data is valid *at the moment of the check*. Between checks, the data can change without re-evaluation.
- **External to the entity.** The validator is a separate object, annotation, or function. The entity itself does not enforce anything — enforcement depends on the calling code.
- **Opt-in.** Every mutation site must remember to invoke the validator. The system's integrity depends on call-site discipline.

### Governance

Governance is a **structural declaration enforced on every operation**. The entity's rules are not checked — they are *compiled into the operational surface*. The defining characteristics:

- **Structural, not invoked.** The rules are enforced by the engine on every operation. There is no call-site to forget.
- **Continuous.** Every operation that can change the entity's state evaluates all applicable rules. There is no window between operations where an invalid configuration can exist.
- **Intrinsic to the entity.** The rules are part of the entity's definition. The entity's operational surface *is* its governance contract.
- **Mandatory.** No code path bypasses enforcement. The governed operations are the *only* way to mutate the entity.

### The Spectrum

The distinction is not binary — systems fall on a spectrum:

| Position | Enforcement | Bypass Possible? | Examples |
|----------|-------------|-------------------|----------|
| **Pure validation** | Invoked at call sites | Yes — any code path that skips the call | FluentValidation, Zod, JSON Schema |
| **Convention-enforced** | Invoked by framework hooks | Yes — by bypassing the framework | EF SaveChanges validation, ActiveRecord callbacks |
| **Layer-enforced** | Enforced at a specific system boundary | Yes — by operating below that boundary | Database CHECK constraints, API gateway rules |
| **Structural governance** | Compiled into the operational surface | No — the governed operations are the only mutation path | Precept |

Database CHECK constraints are the closest existing system to structural governance — they are enforced on every write, by the database engine, regardless of which client issues the write. But they operate only at the storage layer: no lifecycle model, no inspectability, no ability to ask "what would happen if I changed this?" before committing, and no application-layer tooling.

Precept moves the enforcement point to the application layer and adds the full operational surface: lifecycle transitions, inspectability, compile-time structural analysis, and deterministic evaluation.

---

## Cross-System Evidence

### FluentValidation (.NET)

FluentValidation defines validators as C# classes with rule chains (`.NotEmpty()`, `.GreaterThan(0)`, `.Must(predicate)`). Validators are invoked explicitly — typically in a controller, command handler, or pipeline behavior.

**Category:** Pure validation.

- **Invoked at call sites.** A `Validate(entity)` call runs the rules. If no code path calls it, the rules are inert.
- **Bypassed by direct property sets.** `entity.Amount = -1` does not trigger any validator. The entity holds invalid data until someone calls `Validate()`.
- **No structural enforcement.** The validator is a separate class. The entity and the validator are only connected by the calling code's discipline.
- **Comprehensive rule DSL.** FluentValidation's rule language is expressive and composable. The *rules* are not the problem — the *enforcement architecture* is.

### JSON Schema

JSON Schema defines the shape, types, and constraints of JSON documents. Validators (Ajv, Newtonsoft, etc.) check documents against the schema.

**Category:** Pure validation.

- **Checked at parse/validation time.** A JSON document is validated against its schema when `validate(document, schema)` is called. Between validations, the document can be mutated arbitrarily.
- **No structural prevention.** JSON Schema cannot prevent an invalid document from being *constructed*. It can detect one after the fact.
- **Rich constraint vocabulary.** `minimum`, `maximum`, `pattern`, `required`, `if/then/else`, `allOf/anyOf/oneOf`. The vocabulary is expressive, but enforcement is invocation-dependent.

### Entity Framework Validation (.NET)

Entity Framework uses data annotations (`[Required]`, `[Range]`, `[MaxLength]`) and the `IValidatableObject` interface. Validation runs during `SaveChanges()` or on explicit `DbContext.Entry(entity).GetValidationResult()` calls.

**Category:** Convention-enforced (framework hook).

- **Invoked by `SaveChanges()`.** The framework hook catches violations at the persistence boundary — but only for entities tracked by the `DbContext`.
- **Bypassed by raw SQL.** `context.Database.ExecuteSqlRaw("UPDATE ...")` skips all annotation-based validation. The constraints exist, but the bypass path is standard API.
- **Bypassed by bulk operations.** EF Core bulk extensions, `ExecuteUpdate()`, and direct ADO.NET operations bypass the `SaveChanges()` pipeline entirely.
- **Bypassed by other services.** If another service writes to the same database, any EF-based validation in the original service is irrelevant.
- **Scattered rules.** Annotations on the model, `IValidatableObject.Validate()`, Fluent API configuration in `OnModelCreating()`, interceptors, and service-layer code all contribute fragments of the entity's rules.

### Zod / Valibot (TypeScript)

Zod and Valibot define schemas that parse and validate data. The schema is both the type definition and the validator: `z.object({...}).parse(data)`.

**Category:** Pure validation (parse-time).

- **Checked at `.parse()` time.** The schema validates when `parse()` is called. Between parses, the data object is a plain TypeScript object with no enforcement.
- **The schema is a validator, not a governor.** After `parse()` succeeds, the returned object has no ongoing constraint enforcement. `result.amount = -1` is a valid JavaScript operation.
- **No lifecycle model.** Zod schemas describe data shape. There is no concept of transitions, states, or operation-scoped rules.
- **Strong type inference.** Zod's `z.infer<typeof schema>` produces TypeScript types from schemas — useful for development, but the type system does not prevent runtime field mutations.

### Database CHECK Constraints (SQL)

CHECK constraints are declarative predicates enforced by the database engine on every INSERT and UPDATE. They are the original structural constraint mechanism.

**Category:** Layer-enforced (storage boundary).

- **Structurally enforced at the storage layer.** The database engine evaluates CHECK constraints on every write operation. No client-side code can bypass them — as long as writes go through the database.
- **No lifecycle model.** CHECK constraints apply uniformly. There is no concept of "this constraint applies in state X but not state Y."
- **No inspectability.** There is no way to ask "what would happen if I changed this field?" without attempting the write. The database rejects the write after evaluation — not before.
- **No application-layer tooling.** CHECK constraints are defined in SQL DDL, tested against a live database, and invisible to application code. CI testing requires database provisioning.
- **Database-specific syntax.** `CHECK (amount > 0 AND status IN ('active', 'pending'))` is SQL dialect-specific. Constraints are not portable across database engines without translation.
- **Closest to governance.** Among the systems surveyed, CHECK constraints are nearest to structural governance: mandatory enforcement, no bypass from any client. The missing pieces are lifecycle awareness, inspectability, and application-layer integration.

### Precept

Precept compiles a `.precept` definition into an immutable engine that enforces every declared rule on every operation.

**Category:** Structural governance.

- **Structurally enforced on every operation.** `Fire()` and `Update()` evaluate all applicable invariants and assertions against the resulting entity configuration *before committing*. If any constraint fails, the operation is rejected — the invalid configuration never exists.
- **No bypass path.** The governed operations (`Fire`, `Update`) are the only mutation surface. There is no way to set a field without constraint evaluation. The engine does not expose raw property setters.
- **Lifecycle-aware.** State-scoped assertions, transition guards, and editability declarations make the constraint surface lifecycle-dependent. The rules that apply in `Draft` differ from those in `Approved`, and the engine enforces the correct set automatically.
- **Inspectable before execution.** `Inspect()` previews every possible action and its outcome without mutating anything. The caller can ask "what would happen?" before committing.
- **Compile-time verified.** The compiler catches structural problems — type mismatches, unreachable states, contradictory constraints — before any instance exists.

---

## Failure Mode Taxonomy

Validation architectures permit distinct failure modes that structural governance prevents. Each mode describes a way an invalid entity configuration can come into existence despite rules being defined.

### Bypass

**Definition:** A code path mutates the entity without invoking the validator.

**How it happens:**
- Direct property assignment (`entity.Amount = -1`) with no subsequent `Validate()` call.
- A new service method that forgets to include the validation step.
- A batch job that updates records through a different code path than the interactive flow.
- Raw SQL or bulk ORM operations that skip application-layer hooks.

**Frequency:** Common. In any system where validation is invoked rather than structural, every new mutation site is a potential bypass. The problem scales with codebase size and team turnover.

**Governance prevention:** The mutation surface is the enforcement surface. There is no `entity.Amount = -1` — there is `Update(fields: { Amount: -1 })`, and the engine evaluates invariants on that operation. No operation exists that skips enforcement.

### Timing Gap

**Definition:** The entity is valid at validation time but invalid at commit time — something changed between the check and the write.

**How it happens:**
- Optimistic concurrency: another request modifies the entity between this request's validation and its save.
- Multi-step operations: early validation passes, but later mutations in the same transaction introduce violations.
- Async workflows: validation runs on data that becomes stale before the final commit.

**Frequency:** Escalates with system concurrency and operation complexity. Particularly insidious because the validation *did run* and *did pass* — the failure is temporal, not structural.

**Governance prevention:** Constraint evaluation happens at commit time, on the *resulting* configuration — not on a previously-checked snapshot. `Fire()` and `Update()` evaluate constraints after all mutations are applied, atomically. There is no gap between evaluation and commitment.

### Scattered Rules

**Definition:** The entity's rules are distributed across multiple layers, files, or systems, and not all are applied in every context.

**How it happens:**
- Data annotations on the model enforce one set of rules. FluentValidation enforces another. Service-layer code enforces a third. Database constraints enforce a fourth.
- A rule added to the service layer is not replicated in the API gateway validator.
- A constraint exists in the database CHECK but not in the application code, so the application produces entities the database rejects.

**Frequency:** Nearly universal in layered architectures. Rule scattering is the default outcome of any system without a single-artifact constraint model.

**Governance prevention:** One `.precept` file contains every field, constraint, state, and transition. There is no second artifact to keep in sync. The compiler verifies the definition's structural soundness. One file, complete rules.

### Silent Mutation

**Definition:** A field changes without re-validation of the rules that depend on it.

**How it happens:**
- A field is updated through a code path that doesn't trigger the validator (see Bypass above).
- A related field changes, invalidating a cross-field rule, but validation only runs on the changed field, not the affected rule set.
- An event handler mutates a field as a side effect, and the handler doesn't invoke validation.

**Frequency:** Common in systems where validation is field-scoped rather than entity-scoped. Cross-field invariants are particularly vulnerable.

**Governance prevention:** Every operation evaluates *all applicable constraints* against the resulting entity configuration — not just the constraints related to the changed field. An `Update` that changes `Amount` triggers evaluation of every invariant that references `Amount`, every invariant that references fields correlated with `Amount`, and every unconditional invariant. Nothing is scoped to the mutation — everything is scoped to the result.

---

## The Precept Guarantee

| Failure Mode | How Governance Closes It |
|--------------|--------------------------|
| **Bypass** | No raw mutation surface. `Fire()` and `Update()` are the only operations, and both enforce all applicable constraints. |
| **Timing gap** | Constraints evaluate on the *resulting* configuration at commit time, not on a previously-checked snapshot. |
| **Scattered rules** | One `.precept` file. No second artifact. Compiler verifies structural soundness. |
| **Silent mutation** | Every operation evaluates all applicable constraints against the full resulting configuration. |

The guarantee is not "we check thoroughly." It is "the invalid configuration cannot exist." The enforcement is structural — compiled into the operational surface, not invoked by discipline.

---

## Where Validation Is Sufficient

Governance is not always warranted. Validation is the right tool when:

- **Ephemeral input.** HTTP request bodies, form submissions, CLI arguments — data that is checked once, used, and discarded. No ongoing integrity to maintain.
- **Display formatting.** Checking that a date string matches a display format. The concern is presentation, not entity integrity.
- **Stateless transformations.** ETL jobs that transform data between formats. The output is validated against a schema and written; there is no persistent entity whose integrity must be maintained across operations.
- **External data ingestion.** Parsing untrusted data from third-party APIs. The schema check at the boundary is validation — what happens *after* the data enters the domain is where governance applies.
- **Performance-critical hot paths.** When nanosecond-level latency matters (e.g., inner loops of financial calculations), the overhead of a governance engine may be inappropriate. Validation at boundaries is the pragmatic choice.

The test from the philosophy applies here too: "Does it need governed integrity?" If the answer is no — if the data is transient, the rules are simple shape checks, or there is no persistent entity to protect — validation is sufficient. Governance is for entities whose data must satisfy declared rules at every moment, through every operation.

---

## Summary

| System | Category | Structural? | Lifecycle? | Inspectable? | Bypass path? |
|--------|----------|-------------|------------|--------------|-------------|
| FluentValidation | Pure validation | No | No | No | Direct property set |
| JSON Schema | Pure validation | No | No | No | Post-parse mutation |
| EF Validation | Convention-enforced | No | No | No | Raw SQL, bulk ops, other services |
| Zod / Valibot | Pure validation (parse-time) | No | No | No | Post-parse mutation |
| DB CHECK constraints | Layer-enforced | Yes (at storage layer) | No | No | None (from DB perspective) |
| **Precept** | **Structural governance** | **Yes** | **Yes** | **Yes** | **None** |

The distinction is not about rule *expressiveness* — most validators have expressive rule languages. It is about enforcement *architecture*: does the system structurally prevent invalid entity configurations from existing, or does it check for them when asked?
