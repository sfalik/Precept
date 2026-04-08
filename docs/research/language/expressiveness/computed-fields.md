# Computed Fields

Research grounding for [#17 — Computed / derived fields](https://github.com/sfalik/Precept/issues/17).

This file is durable research, not the proposal body. It captures why computed fields came up, what adjacent systems do, which contracts must be explicit before implementation, and which directions look attractive but weaken Precept's semantics.

## Background and Problem

Precept currently has a manual-synchronization gap: when one field is just a function of other fields, the author must either:

- recompute that value in every transition row that mutates an input, or
- avoid storing the value and repeat the underlying formula everywhere it is needed.

That creates three recurring problems:

- **Formula drift.** Change the business formula once, then update multiple rows by hand.
- **Single-source-of-truth loss.** A stored total can diverge from its components if one row forgets to refresh it.
- **Review friction.** Readers must scan action chains to determine whether a displayed total is authoritative or stale.

The pain is visible in the sample set. [travel-reimbursement.precept](../../../../samples/travel-reimbursement.precept) has totals assembled from multiple component amounts. [event-registration.precept](../../../../samples/event-registration.precept) and [loan-application.precept](../../../../samples/loan-application.precept) similarly depend on arithmetic that is meaningful to later guards and invariants. The current workaround is valid, but it forces authors to encode derived data as repeated transition mechanics.

The adjacent internal research supports that diagnosis:

- [expression-language-audit.md](./expression-language-audit.md) shows that Precept can already express the arithmetic needed for many derivations; the gap is not arithmetic power, it is where formulas live.
- [expression-evaluation.md](../references/expression-evaluation.md) shows that field-local derivation can stay inside Precept's current decidable fragment if it remains pure, row-local, and dependency-bounded.

## Precedent Survey

| Category | System | Derivation model | Precept implication |
|---|---|---|---|
| **Databases** | PostgreSQL generated columns | Always computed from other columns; cannot be written; distinct from defaults. [Docs](https://www.postgresql.org/docs/current/ddl-generated-columns.html) | Derivation ≠ initialization; recompute on base change. |
| | SQL Server computed columns | Virtual unless persisted; blocked from INSERT/UPDATE. [Docs](https://learn.microsoft.com/en-us/sql/relational-databases/tables/specify-computed-columns-in-a-table) | Read-only contract; persistence separate from authoring. |
| | MySQL generated columns | Inline expression; rejects direct writes; deterministic only; may reference earlier generated columns. [Docs](https://dev.mysql.com/doc/refman/8.0/en/create-table-generated-columns.html) | Dependency ordering and determinism as explicit contracts. |
| **Languages** | Kotlin read-only properties | Custom getters compute from other properties; single source of truth via backing-property pattern. | Derived value safest as read-only view over underlying state. |
| | C# expression-bodied properties | Read-only, no backing field; presented as computed properties. | Familiar .NET model: observable but not independently mutable. |
| **IaC** | Terraform `Computed` attributes | Provider-owned; practitioner config for computed-only attribute rejected. [Docs](https://developer.hashicorp.com/terraform/plugin/framework/handling-data/attributes/string) | External callers should not set computed fields — reject, don't ignore. |
| **End-user** | Spreadsheet formulas | Formula is canonical cell definition; auto-produces result from referenced cells. [Excel](https://support.microsoft.com/en-us/office/overview-of-formulas-in-excel-ecfdc708-9162-49e8-b993-c311f47ca173), [Sheets](https://support.google.com/docs/answer/46977?hl=en) | Users understand formula-bearing fields as declared derivations. |
| **Enterprise platforms** | Salesforce formula fields | Read-only, post-save recompute, 200+ functions; no direct assignment. | Full platform precedent: read-only declared derivation with auto-recompute. |
| | Dynamics 365 calculated columns | Real-time recompute; parent-record refs; depth-5 dependency chains. | Bounded dependency depth as explicit contract; cross-entity reach limited. |
| | ServiceNow calculated fields | Server-side calculation on display/update; not directly editable. | Enterprise precedent: calculated = derived, not user-writable. |
| **Pure validators** | Pydantic `@computed_field` | Read-only, serializable, evaluated at access time. Closest validator match. | Access-time evaluation suits recompute-before-check model. |
| | Zod `.transform()` | Pipeline step transforming input to different type. Not schema-level derivation. | Pipeline mechanics, not field-level derivation — wrong model for Precept. |
| | FluentValidation | No computed concept; rules reference properties but don't derive values. | Confirms derivation is a distinct concern from validation. |
| | JSON Schema | No derivation; describes shape only. | Pure shape validation; derivation out of scope. |
| **State machines** | XState | No derived context; all values set via explicit `assign` actions. | State machines don't model derived data — the gap computed fields fill. |
| | Stateless (.NET) | No field model; states and triggers only. | State machines govern transitions, not data derivation. |
| **Policy / decision** | OPA/Rego virtual documents | Query-time derivations via variable binding; read-only, no mutation. | Strong read-only derivation precedent; scope broader than entity-local. |
| | Cedar | Static attributes only; no computed attributes or derivation. | Authorization-focused; derivation not part of policy model. |
| | DMN decision tables | Table outputs derived from inputs; stateless and deterministic. | Deterministic derivation precedent, but decision-table shaped, not field-local. |
| **Industry standards** | FHIR, ACORD, ISO 20022 | No computed elements; all delegate derivation to implementation. | Standards define vocabulary, not derivation. Precept fills enforcement gap. |
| **MDM** | Informatica MDM | Attributes + enrichment rules; derivation is post-definition, not inline. | MDM treats derivation as separate processing, not structural field property. |
| **Orchestrators** | Temporal, MassTransit | No computed state; explicit snapshots / saga properties. Derived state avoided deliberately. | Orchestrators manage process state, not entity data derivation. Complementary. |

The precedent pattern is unusually consistent across systems that support derivation:

- derived values are declared once,
- direct assignment is restricted or forbidden,
- defaults and derivations are treated as different concepts,
- dependency and determinism rules are explicit.

That is a strong signal that computed fields are not exotic language sugar. They are a standard way to preserve one source of truth when a system wants derived data to be observable.

### Cross-category pattern

The full sweep across all seven philosophy positioning categories reveals a structural gap. Systems that *have* computed fields — databases, enterprise platforms, spreadsheets, and Pydantic — converge on the same contract: read-only, declared once, automatically recomputed. Systems that *don't* — state machines, validators, orchestrators, policy engines, industry standards, and MDM — either delegate derivation to implementation code or avoid it entirely. No system in the survey combines field-level derivation with lifecycle-aware constraint enforcement. That is precisely the intersection Precept occupies, which means computed fields would not duplicate an existing capability from any adjacent category — they would fill a gap that every category leaves open.

## Philosophy Fit

Computed fields fit Precept's philosophy only under a narrow contract.

**Prevention, not detection.** A derived total that can drift from its inputs weakens the product's central guarantee. A declared derivation improves that guarantee only if the runtime recomputes it before constraint evaluation rather than trusting manual `set` discipline.

**One file, complete rules.** A field-local derivation keeps the business formula near the field it names. That is more aligned with Precept than hiding the same formula across multiple transition rows.

**Data truth over movement truth.** Derived fields are about persistent data shape, not routing logic. They should remove repeated data calculations, not replace first-match row selection or introduce branchy outcome logic.

**Determinism and inspectability.** Computed fields are a fit only if their inputs, ordering, and evaluation timing are explicit. If their value depends on hidden caching, lazy reads, or transient event scope, they cut against the engine's inspectable model.

**AI-readable authoring.** A field-local derivation is easier to audit than duplicated action chains, but only if the surface stays obvious and the toolchain exposes both the formula and the resulting value consistently.

## Semantic Contracts To Make Explicit

These are the contracts the proposal must state directly. Leaving them implicit would shift product design into implementation guesswork.

### 1. Scope Boundary

Computed expressions should be scoped to persistent entity data, not transient event arguments, unless the product deliberately wants "event input becomes field definition" coupling. The safer default is:

- fields,
- safe collection accessors,
- no event arguments,
- no cross-precept references.

That keeps derivations tied to the entity's long-lived state rather than to a single fire operation.

### 2. Recomputation Timing

The contract must say exactly when recomputation happens in **both** pipelines:

- **Fire**: after all mutations in the chosen path, including exit actions, row actions, and entry actions, then before invariants and state assertions evaluate.
- **Update**: after direct field edits are applied, then before invariants and `in <State> assert` rules evaluate.
- **Inspect**: preview output should reflect post-recomputation values, or the preview becomes misleading.

This is the most important semantic contract. Without it, computed fields are just nicer syntax for stale caches.

### 3. Nullability and Accessor Safety

The proposal direction may be "computed fields always produce a value," but that is only meaningful if nullable inputs are handled explicitly. The proposal still needs to choose and document:

- whether expressions that reference nullable fields are rejected conservatively,
- whether a future null-coalescing feature is required before broader adoption,
- what happens when `.peek`, `.min`, or `.max` touch an empty collection,
- whether those accessor cases are compile-time errors, runtime rejections, or default-bearing semantics.

This should be framed as a proposal decision, not as already-set runtime behavior.

### 4. Dependency Ordering and Cycles

If computed fields can depend on other computed fields, the language needs a declared dependency model:

- dependency graph built at compile time,
- topological evaluation order,
- compile-time cycle diagnostics with a readable cycle path.

MySQL's rule that generated columns may reference earlier generated columns is useful precedent here: dependency ordering is part of the contract, not an implementation afterthought.

### 5. Writeability and External Input

The proposal should explicitly state how derived fields behave across all mutation surfaces:

- may they appear in `edit` declarations,
- may they be the target of `set`,
- may callers provide them in `CreateInstance`, `Update`, hydration, or MCP payloads,
- if callers do provide them, is that ignored or rejected?

Terraform's `Computed` contract is a good reminder that a read-only field should usually reject user configuration rather than silently competing with provider-owned state.

### 6. Tooling and Serialization Surface

The proposal should say what tools expose:

- whether compile output shows the derivation expression,
- whether inspect/update output shows the current computed value,
- whether hover/completions surface the formula, the result, or both.

This is not a minor tooling detail. Precept's product promise includes inspectability, and inspectability is only real if computed fields are visible as both rule and data.

## Dead Ends and Rejected Directions

Several directions look convenient but weaken the model.

### Event-Argument-Derived Fields

Letting a computed field depend directly on event args would make a persistent field definition depend on transient operation scope. That creates hidden temporal coupling and blurs the line between instance state and call input.

### Lazy or Cached Evaluation

Database systems can differentiate stored and virtual generated columns because they own the storage model. Precept's stronger requirement is inspectable determinism. Lazy evaluation or transparent caching would make value freshness depend on read order or cache invalidation, which is the wrong trade for this product.

### Writable Computed Fields

Allowing a derived field to appear in `edit` or be the target of `set` defeats the single-source-of-truth benefit. The system would be accepting both a formula and an override for the same fact.

### Silent Default Fallbacks

If a computed expression is total only because the runtime quietly substitutes `0`, `""`, or `false` for null or empty-source cases, the language becomes harder to reason about. Defaults belong in explicit declarations or future explicit operators, not as hidden derivation behavior.

### Cross-Precept Derivations

The more a computed field reaches across entity boundaries, the more Precept stops being a one-file integrity contract and starts becoming a distributed query language. That is a category shift, not a small extension.

## Proposal Implications

Issue #17 should be framed as more than compact syntax. The real benefit is eliminating manual synchronization while preserving a single authoritative formula.

Concretely, the proposal should:

- make the Fire, Update, and Inspect timing contract explicit,
- decide how nullable inputs and empty-collection accessors interact with a "derived value is always defined" story,
- define the external-input and serialization contract for computed fields,
- add acceptance criteria for cycle diagnostics, inspect output, update-path recomputation, and tooling visibility.

The strongest proposal shape is a **read-only, field-local derivation contract** that reduces duplicated row logic without weakening first-match routing or introducing hidden evaluation state. The remaining open choices are not reasons to reject the idea; they are the places where the issue must be precise before implementation starts.