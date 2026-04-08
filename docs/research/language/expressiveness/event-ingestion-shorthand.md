# Event Ingestion Shorthand

Research grounding for [#11 — Event argument absorb shorthand](https://github.com/sfalik/Precept/issues/11).

This file is durable research, not the proposal body. It captures why event-ingestion shorthand came up, what adjacent systems do with hydration and update shorthand, which contracts must be explicit before implementation, and which directions would push Precept from compact DSL into hidden mapping engine.

## Background and Problem

Precept currently makes intake-style transitions spell out every event-to-field copy as an explicit `set`:

- [subscription-cancellation-retention.precept](../../../../samples/subscription-cancellation-retention.precept) copies `RequestCancellation.Name`, `Plan`, `Price`, and `Reason` into governed fields before moving to `RetentionReview`.
- [event-registration.precept](../../../../samples/event-registration.precept) copies `StartRegistration.Name`, `Email`, and `Seats`, then computes `AmountDue`.
- [loan-application.precept](../../../../samples/loan-application.precept) copies five submit arguments into underwriting fields.
- [travel-reimbursement.precept](../../../../samples/travel-reimbursement.precept) mixes direct copies with derived assignments inside one intake row.
- [refund-request.precept](../../../../samples/refund-request.precept) shows the same pattern with renamed fields such as `OrderNumber -> OrderId`.

The repetition is real. [internal-verbosity-analysis.md](./internal-verbosity-analysis.md) counts **132** `set Field = Event.Arg` assignments across the 21-sample corpus.

But the corpus also exposes the main hazard: this is not mostly exact-name copying. A name-match audit across those same 132 assignments found **zero** exact `Field = Event.Field` pairs. The current samples overwhelmingly do one of three things:

1. **Rename on intake** — `Submit.Name -> ApplicantName`, `RequestCancellation.Price -> MonthlyPrice`, `Submit.OrderNumber -> OrderId`.
2. **Compute on intake** — `AmountDue = Seats * PricePerSeat`, `MileageTotal = Miles * Rate`, `RequestedTotal = Lodging + Meals + ...`.
3. **Mix copy and policy mutation** — copy payload fields, then also reset or derive governed state in the same row.

That matters because it separates two different user needs:

- **Need A:** reduce boilerplate for one-to-one event ingestion;
- **Need B:** express renaming, transforms, defaults, and selective copying during ingestion.

Need A can plausibly fit Precept as narrow sugar. Need B is where compactness turns into a mapping DSL.

## Precedent Survey

| Category | System | Hydration / update model | Precept implication |
|---|---|---|---|
| **Databases** | PostgreSQL `jsonb_populate_record` | Expands a top-level JSON object into a typed row by matching keys to column names. [Docs](https://www.postgresql.org/docs/current/functions-json.html) | Name-matching hydration is real precedent, but it is explicit about target row type and visibly a record-population operation. |
| **Languages** | C# object initializers | Object creation plus explicit property assignment in one expression. [Docs](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/object-and-collection-initializers) | Strong precedent for keeping writes visible; compact does not have to mean implicit. |
| | JavaScript object spread | Copies enumerable properties by key; later properties override earlier ones. [Docs](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Operators/Spread_syntax) | Useful precedence model: broad copy first, explicit override later. Also a reminder that broad copy surfaces are easy to overuse. |
| **End-user tools** | Power Query expansion | Users explicitly expand nested records/tables into selected columns; over-broad expansion has performance and schema-shape consequences. [Docs](https://learn.microsoft.com/en-us/power-query/optimize-expanding-table-columns) | Convenience expansion is acceptable only when the tool makes the expanded surface inspectable. |
| **Enterprise platforms** | Dataverse table and column mappings / `InitializeFrom` | Relationship-scoped mappings copy source values into a new related record before save; later source changes do not propagate. [Docs](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customize-entity-attribute-mappings) | Good precedent for tightly scoped hydration with explicit timing. |
| | Salesforce import / Data Loader mappings | Auto-matching by field name exists, but mappings are reviewed or edited explicitly; unmapped fields are skipped. [Docs](https://help.salesforce.com/s/articleView?id=xcloud.import_with_data_import_wizard.htm&language=en_US&type=5), [Docs](https://developer.salesforce.com/docs/atlas.en-us.dataLoader.meta/dataLoader/defining_field_mappings.htm) | Enterprise tooling treats name matching as import convenience, not hidden business-rule semantics. |
| | ServiceNow Transform Maps | Import-set data maps into target fields through explicit field maps and optional scripts. [Docs](https://www.servicenow.com/docs/r/zurich/integrate-applications/system-import-sets/c_CreatingNewTransformMaps.html) | Clear warning: as soon as renames and transforms matter, a separate mapping subsystem appears. |
| **Pure validators** | Pydantic models | Parse and validate input into typed model fields. [Docs](https://docs.pydantic.dev/latest/concepts/models/) | Useful boundary precedent: payload hydration belongs naturally at parse/instantiation time. |
| | Zod transforms | Validate input and then transform the parsed value. [Docs](https://zod.dev/api#transform) | Transform pipelines exist, but they are boundary-focused and quickly become more than shorthand. |
| | JSON Schema object properties | Declares recognized properties; `properties` alone ignores non-declared names unless other keywords tighten the contract. [Docs](https://json-schema.org/understanding-json-schema/reference/object.html) | Strong caution on silent ignores: permissive object handling is normal at schema boundaries, but risky inside a governed mutation engine. |
| | Joi object schemas | Validates object shape and can coerce or reject values in a single schema. [Docs](https://joi.dev/api/?v=17.12.2) | Another input-boundary precedent, not a lifecycle mutation precedent. |
| | FluentValidation conditions | Controls when validation rules apply, but does not hydrate model state. [Docs](https://docs.fluentvalidation.net/en/latest/conditions.html) | Reinforces that validation and mutation are distinct concerns. |
| **State machines** | XState actions / `assign` | Event-driven context updates are explicit actions; no built-in “copy all matching event fields” action exists in the core model. [Docs](https://stately.ai/docs/actions#assign-action) | Precept's closest category peer chooses visible mutation over implicit hydration. |
| **Policy / decision** | OPA / Rego | Evaluates structured documents and returns decisions or derived documents; no persistent entity writes. [Docs](https://www.openpolicyagent.org/docs/latest/policy-language/) | Policy engines are comfortable with derived values because they do not silently mutate stored entity state. |
| | Cedar | Authorization logic is explicitly separated from application business logic. [Docs](https://docs.cedarpolicy.com/) | Another reminder that decision surfaces and mutation surfaces should stay separate. |
| **Industry standards** | FHIR Mapping Language / StructureMap | Dedicated mapping language with groups, imports, constants, and transformation rules. [Docs](https://build.fhir.org/mapping-language.html) | The clearest non-goal signal: once Precept supports renames and transforms broadly, it starts becoming a mapping language. |
| **MDM** | Reltio crosswalks | Tracks source-system identity and lineage across records. [Docs](https://docs.reltio.com/en/objectives/model-data/data-modeling-at-a-glance/data-modeling-operation/define-crosswalks-for-data-sources/crosswalks) | MDM cares about source traceability, not inline lifecycle-row shorthand. Different problem. |
| **Orchestrators** | Temporal workflows | Workflow logic is written in general-purpose code; inputs, commands, and event history stay explicit. [Docs](https://docs.temporal.io/workflows) | Orchestrators preserve auditability by keeping payload handling explicit in code, not hidden in shorthand. |

### Cross-category pattern

The survey splits cleanly:

- Systems that support broad hydration or expansion do it at an **import / construction / transform boundary** and make that boundary obvious.
- Systems closest to Precept's core role — validators, state machines, decision engines, orchestrators — keep **mutation explicit** or outside the core rule surface.
- Systems that need renames, transforms, fallbacks, nested paths, and source lineage quickly grow a **separate mapping language or mapping UI**.

That is the central design signal for Precept: a narrow shorthand for exact one-to-one intake might fit, but the moment the feature tries to cover real-world renaming and transformation pressure, it stops being syntax sugar and starts becoming a second language.

## Philosophy Fit

Event-ingestion shorthand fits Precept's philosophy only under a narrow contract.

**Prevention, not detection.** Hidden writes are only acceptable if the runtime still validates the fully materialized post-mutation state before commit. `absorb` cannot be “special” in a way that bypasses ordinary `set` semantics or validation.

**Full inspectability.** This is the make-or-break requirement. If authors cannot see which fields were written, which were ignored, and which later explicit `set` statements overrode absorbed values, the shorthand weakens the product.

**Keyword-anchored, flat syntax.** `-> absorb EventName` is still one visible action keyword. `-> absorb Submit rename Applicant as ApplicantName default 0 except Score` is not. The former is sugar; the latter is the start of a mapping DSL.

**First-match routing stays intact.** `absorb` must not affect row selection. It belongs only inside the chosen row's mutation stage, after row selection and before validation, with ordinary declaration-order semantics.

**Configuration-like readability.** A single intake action reads well when it truly means “copy matching payload fields into this entity.” It stops reading like configuration the moment a reviewer needs a separate alias table in their head to know what got written.

**Power without hidden behavior.** The compactness win is legitimate only if the expansion is explainable as deterministic desugaring into ordinary `set` actions that tooling can show back to the author.

The strongest philosophy objection is not abstract. It is sample-backed: because the current corpus has zero exact-name event-to-field copies, a name-match-only `absorb` would often read like a meaningful action while doing nothing. That is worse than verbosity.

## Semantic Contracts To Make Explicit

These contracts must be written directly into the proposal. Leaving them vague would force implementation to make product decisions accidentally.

### 1. Matching Contract

The shorthand needs an exact scope boundary:

- exact event-arg name to field-name matching only;
- no aliases;
- no nested path selection;
- no transforms;
- no wildcard include/exclude vocabulary;
- no cross-event or bare `absorb`.

Without that boundary, the feature stops being shorthand and becomes configurable mapping.

### 2. Expansion and Precedence

The safest model is desugaring:

- `absorb EventName` expands to a concrete ordered list of synthetic `set Field = EventName.Field` actions for every matched pair;
- those synthetic actions live in the row-mutation stage described by the current fire pipeline;
- later authored `set` actions in the same row override earlier absorbed writes naturally.

That keeps precedence simple: explicit later `set` wins because it runs later, not because `absorb` gets a bespoke override rule.

### 3. Zero-Match Behavior

Zero-match behavior cannot be a silent no-op.

That is not theoretical. In the current sample corpus, name-match-only `absorb` would hit zero matches in the showcased examples and, by audit, in all 132 existing event-to-field copies.

The contract therefore needs one of these:

- **compile-time error** when a statically known `absorb` matches zero writable fields; or
- **at minimum**, a highly visible diagnostic plus inspect / fire traces that say the action matched nothing.

Anything quieter invites false confidence and invisible data loss.

### 4. Silent Ignores and Partial Matches

If some event args match fields and others do not, the proposal must say whether non-matching args are:

- silently ignored,
- surfaced as diagnostics,
- or surfaced as structured trace metadata only.

Permissive ignore behavior is common in import tools and schema validators, but Precept is not an import wizard. If partial ignore is allowed for forward compatibility, the ignored-arg set must still be visible in tooling.

### 5. Type Compatibility and Writeability

Every matched arg/field pair must still satisfy ordinary assignment rules:

- type assignability;
- nullability;
- future writeability restrictions on computed or otherwise non-directly-settable fields;
- conflict reporting if a future surface introduces duplicate synthetic writes.

`absorb` should not create a side door around type checking.

### 6. Tooling and Audit Surface

Inspectability requires explicit output surface:

- compile output should show which fields an `absorb` can target;
- preview / inspect output should show resolved absorbed writes before and after later overrides;
- fire traces should show the absorbed field list, ignored args, and final values;
- hover or language-server help should expose the exact matches for the current event.

If the only observable surface is the source text `-> absorb Submit`, the feature is under-instrumented for Precept.

## Dead Ends and Rejected Directions

Several attractive extensions should be rejected up front.

### Bare `absorb`

`absorb` without an event name is unsafe in multi-event precepts and makes action meaning depend on surrounding context in a way Precept usually avoids.

### Silent Zero-Match Success

Given the current corpus, this would make the shorthand look useful while frequently doing nothing. That is an audit failure, not a convenience.

### Rename Clauses or Inline Mapping Tables

Anything like:

- `absorb Submit as ApplicantName from Applicant`
- `absorb Submit rename Applicant -> ApplicantName`
- `absorb Submit { ApplicantName: Applicant, RequestedAmount: Amount }`

pushes the feature into mapping-language territory. The FHIR mapping precedent is the warning here, not the inspiration.

### Implicit Absorb-By-Default

Automatically copying every matching event arg without an authored action would create invisible writes and make rows harder to audit.

### Nested Payload Flattening

If the event surface ever grows nested objects, flattening or path-based absorb would expand the feature's scope dramatically. That belongs in a real mapping system, not in a compact row action.

### “Helpful” Defaulting for Missing Args

Quietly substituting current field values, field defaults, or neutral literals when a payload omits a would-be absorbed name would hide too much behavior. Defaults must remain explicit.

## Proposal Implications

The current issue shape needs a sharper framing.

First, the proposal should stop implying that name-match-only `absorb` cleanly compresses today's samples. It does not. The example in [subscription-cancellation-retention.precept](../../../../samples/subscription-cancellation-retention.precept) is representative: `Name`, `Plan`, `Price`, and `Reason` do **not** match `SubscriberName`, `PlanName`, `MonthlyPrice`, and `CancellationReason`. As written, a strict `absorb RequestCancellation` would map zero fields there.

Second, the real choice is now clearer:

1. **Narrow shorthand option:** keep `absorb` exact-name-only, explicitly visible, and diagnostic-heavy. This helps future precepts that intentionally align event arg names with field names.
2. **Broader ingestion feature:** support renames and transforms. The research says this does **not** fit Precept's philosophy well, because it turns a row action into a mapping surface with hidden behavior pressure.

Third, acceptance criteria should tighten around auditability:

- zero-match must fail loudly;
- inspect and fire must enumerate absorbed writes and ignored args;
- explicit `set` precedence should be defined through ordered desugaring, not prose-only special cases;
- samples should only adopt `absorb` where the names genuinely line up, not by rewriting domain terms solely to force a shorthand demo.

The most defensible proposal shape is therefore **a very narrow, desugared, exact-name ingestion shorthand** — and even that should be treated as a late-wave compactness feature, not as a broad answer to payload-to-entity mapping.
