# Constraint Composition — Domain Research

Research grounding for the constraint composition domain: named rules ([#8](https://github.com/sfalik/Precept/issues/8)), field-level constraints ([#13](https://github.com/sfalik/Precept/issues/13)), and conditional (guarded) declarations ([#14](https://github.com/sfalik/Precept/issues/14)).

This file is durable research, not a proposal body. It captures why these three features belong to one domain, what adjacent systems do, which contracts must be explicit before implementation, and which directions look attractive but weaken Precept's semantics.

---

## Background and Problem

Constraint composition supports governed integrity by enabling authors to express complex data rules — unconditionally or under declared guard conditions — that the runtime enforces structurally on every operation.

Precept's constraint surface currently has three interconnected pressure points. Each is visible independently, but they share a root cause: the language treats every constraint as a standalone flat statement with no composition, no co-location, and no conditional applicability. Together they account for the largest authoring-friction category in the sample corpus.

### Pressure 1 — Basic shape constraints inflate every file

Across 21 sample precepts, **46 of 55 invariant statements** (84%) enforce basic data-shape bounds: `>= 0`, `> 0`, `!= null`, `!= ""`. These are structurally identical across files — "amount cannot be negative" appears in 10+ samples with only the field name varying. They are not business rules; they are type-shape enforcement that belongs near the field declaration, not scattered across a file as separate `invariant` lines.

The loan-application sample illustrates the pattern:

```precept
field RequestedAmount as number default 0
field ApprovedAmount as number default 0
field AnnualIncome as number default 0
field ExistingDebt as number default 0

invariant RequestedAmount >= 0 because "Requested amount cannot be negative"
invariant ApprovedAmount >= 0 because "Approved amount cannot be negative"
invariant AnnualIncome >= 0 because "Annual income cannot be negative"
invariant ExistingDebt >= 0 because "Existing debt cannot be negative"
```

Four fields, four invariants, four `because` strings, all saying the same thing: non-negative number. In Zod this would be four `.nonnegative()` suffixes on the field declarations. In FluentValidation, four `.GreaterThanOrEqualTo(0)` calls chained to the property selector. The current Precept surface separates these from the field, inflating the file and obscuring the business invariants (`ApprovedAmount <= RequestedAmount`) that represent real domain logic.

### Pressure 2 — Multi-clause guards cannot be named or reused

The loan-application sample contains a 93-character eligibility guard:

```precept
from UnderReview on Approve
    when DocumentsVerified && CreditScore >= 680 && AnnualIncome >= ExistingDebt * 2
        && RequestedAmount < AnnualIncome / 2 && Approve.Amount <= RequestedAmount
```

If the same policy applies to multiple events (Approve, Reconsider, ExtendCredit), the expression must be duplicated verbatim. A policy change — say the credit threshold moves from 680 to 700 — requires editing every copy. There is no mechanism to name, define once, and reference by name.

The apartment-rental sample shows the same pattern at a smaller scale:

```precept
from Submitted on Approve
    when MonthlyIncome >= RequestedRent * 3 && CreditScore >= 650 && HouseholdSize < 8
```

The `3x rent rule` plus credit and household checks is a named business concept ("rental eligibility") that has no linguistic representation as a named thing.

### Pressure 3 — Conditional constraints require state explosion or no enforcement

When a constraint applies only under certain field conditions — "premium customers must have a credit card," "high-deductible plans require a minimum deductible of $500" — the current workaround is to model each condition as a separate state. A three-tier customer (Standard / Premium / VIP) with two independent feature flags produces 12+ states instead of one state with conditional invariants. The conditional-logic-strategy research documented this as the "Cartesian explosion" problem.

### Why these are one domain

All three pressures stem from the same architectural gap: Precept's constraint surface has no composition layer between individual boolean expressions and the flat statement model. Field constraints address vertical composition (co-locating simple bounds with their field). Named rules address horizontal composition (reusing a predicate across sites). Conditional guards address conditional composition (declaring when a constraint applies). They interact:

- A named rule might itself carry a `when` guard at its definition site
- A field-level constraint desugars to an invariant that might someday be conditionally applied
- A `when`-guarded invariant references the same field scope as a named rule

Designing them independently risks inconsistent scope rules, competing suffix positions in the grammar, and three different answers to "where does this constraint live?"

### Adjacent internal evidence

- [internal-verbosity-analysis.md](./internal-verbosity-analysis.md) §3: 19 of 21 samples contain non-negative boilerplate, the single largest verbosity category.
- [conditional-logic-strategy.md](./conditional-logic-strategy.md): `when`/`if` teaching model, `unless` rejection, consistency audit of the structural-guard pattern across 10 systems.
- [conditional-invariant-survey.md](../references/conditional-invariant-survey.md): 10 production constraint systems all use positive guards, not formal implication.
- [constraint-composition.md](../references/constraint-composition.md): PLT grounding — predicate combinators, Boolean lattices, Alloy/Zod scope segregation.
- [fluent-validation.md](./fluent-validation.md): FluentValidation's `When`/`Unless` conditional blocks and `.GreaterThan(0)` co-located constraints — the most commercially important comparison.
- [zod-valibot.md](./zod-valibot.md): Zod's `.min()`, `.nonnegative()` chaining and `.refine()` cross-field rules — the dominant TypeScript pattern.
- [fluent-assertions.md](./fluent-assertions.md): FluentAssertions' `AssertionScope` collect-all semantics — equivalent to Precept's invariant model.

---

## Precedent Survey

### Databases

| System | Field constraints | Named rules | Conditional constraints | Source |
|--------|------------------|-------------|------------------------|--------|
| **PostgreSQL** | `CHECK (amount >= 0)` on column; `NOT NULL`, `UNIQUE` as inline suffixes. Column-level and table-level CHECK. | Named constraints via `CONSTRAINT name CHECK (expr)`. Referenceable in ALTER/DROP. | No conditional CHECK. Separate `EXCLUDE` for complex cases. | [Docs](https://www.postgresql.org/docs/current/ddl-constraints.html) |
| **SQL Server** | `CHECK`, `DEFAULT`, `NOT NULL` on column definition. Named constraints standard. | Named constraints universal. `sp_rename` for constraint names. | No conditional CHECK. Filtered indexes serve a tangential role. | [Docs](https://learn.microsoft.com/en-us/sql/relational-databases/tables/unique-constraints-and-check-constraints) |
| **MySQL** | `CHECK (expr)` on column (enforced since 8.0.16). `NOT NULL`, `DEFAULT` inline. | Named constraints via `CONSTRAINT name`. | No conditional CHECK. | [Docs](https://dev.mysql.com/doc/refman/8.0/en/create-table-check-constraints.html) |

**Cross-database pattern:** Databases universally support (a) inline column-level constraints as declaration suffixes, (b) named constraints referenceable by name, and (c) no conditional constraints — conditions are handled by separate rows or application logic. This maps directly to Precept's field constraints (#13) and named rules (#8), while confirming that conditional invariants (#14) are a genuine language extension beyond what SQL offers.

### Programming Languages

| System | Field constraints | Named predicates | Conditional constraints | Source |
|--------|------------------|-----------------|------------------------|--------|
| **Kotlin** | `require(x > 0) { "..." }` in init blocks. `@JvmField` annotations. No declaration-site constraints. | Named functions as predicates. `val isEligible: Boolean get() = ...`. | `require` with `if` guards in init/factory. | [Docs](https://kotlinlang.org/docs/coding-conventions.html) |
| **C#** | Data annotations: `[Range(0, 100)]`, `[Required]`, `[MaxLength(200)]`. Attribute-based, declaration-co-located. | No named constraint reuse beyond shared attributes. | `[Required(AllowEmptyStrings = false)]` with custom `ValidationAttribute.IsValid()` override for conditional logic. | [Docs](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations) |
| **Python / Pydantic** | `Field(ge=0, le=100)`, `Field(min_length=1, max_length=200)`. Declaration-inline. `@field_validator` for custom. | `@model_validator` for cross-field. Named validators are methods. | `@field_validator` with `if` in body. No declarative conditional. | [Docs](https://docs.pydantic.dev/latest/concepts/validators/) |
| **TypeScript** | No runtime constraints. Type narrowing only. | Type predicates (`x is Foo`). No runtime enforcement. | Discriminated unions for conditional shapes. | — |

**Cross-language pattern:** Languages that support field-level constraints universally co-locate them with the field declaration (C# attributes, Pydantic `Field()`, Kotlin `require`). Named predicates exist but are general-purpose functions, not constraint-specific reuse mechanisms. Conditional constraints require imperative code in all surveyed languages — none have a declarative conditional constraint form.

### Pure Validators

| System | Field constraints | Named / reusable rules | Conditional rules | Source |
|--------|------------------|----------------------|-------------------|--------|
| **FluentValidation** | `.NotNull()`, `.NotEmpty()`, `.GreaterThan(0)`, `.LessThanOrEqualTo(max)`, `.MaximumLength(200)`. All chained to `RuleFor(x => x.Field)`. Default error messages. | `.Include(new SharedValidator())` for cross-validator reuse. Rule sets (`RuleSet("name", () => { ... })`). | `When(x => x.IsActive, () => { ... })` / `Unless(...)`. One condition gates a block of rules. Flagship pattern. | [Docs](https://docs.fluentvalidation.net/en/latest/) |
| **Zod** | `.min(n)`, `.max(n)`, `.nonnegative()`, `.positive()`, `.nonempty()`, `.email()`. Chained on `z.string()` / `z.number()`. | `.refine()` for custom cross-field. `.superRefine()` for complex multi-field. No named reuse mechanism. | `.refine().when()` pattern. `.discriminatedUnion()` for shape-conditional schemas. | [Docs](https://zod.dev/) |
| **Valibot** | `minValue()`, `maxValue()`, `nonEmpty()`, `maxLength()`. Pipe-based: `pipe(string(), minLength(1), maxLength(100))`. | Composable via function composition — any predicate is a reusable function. | Conditional via `union()` and custom pipe steps. | [Docs](https://valibot.dev/) |
| **Joi** | `.min(n)`, `.max(n)`, `.required()`, `.allow('')`. Chained schema declarations. | `.shared(schema)` for cross-schema reuse. | `.when('field', { is: condition, then: schema, otherwise: schema })`. Conditional validation based on sibling fields. | [Docs](https://joi.dev/api/) |
| **JSON Schema** | `minimum`, `maximum`, `minLength`, `maxLength`, `pattern`, `required` as inline properties. | `$ref` to named `$defs` for schema reuse. | `if`/`then`/`else` (Draft 7+) for conditional subschemas. Added specifically to avoid negation-based workarounds. | [Docs](https://json-schema.org/understanding-json-schema/) |

**Cross-validator pattern:** Every mainstream validator (a) co-locates basic constraints with the field/property declaration, (b) has some form of named/reusable constraint composition, and (c) supports conditional rule application via guards (`When`), discriminants, or `if/then/else`. Precept currently lacks all three. FluentValidation's `When` block is the strongest commercial precedent for Precept's `when` guard design — same .NET audience, same conditional constraint need.

### Policy and Decision Systems

| System | Constraint vocabulary | Named rules | Conditional application | Source |
|--------|----------------------|-------------|------------------------|--------|
| **Drools** | Pattern matching in `when` clause. No inline field constraints. | Named rules with explicit `rule "name"`. Rules composed via fact insertion and agenda groups. | `when` structural guard on every rule. Positive framing. Salience for priority. | [Docs](https://docs.drools.org/latest/drools-docs/docs-website/drools/language-reference/) |
| **Cedar** | Static attributes only. No declared constraints on policy attributes. | Named policies (`permit`/`forbid`). Reusable via policy sets. | `when { condition }` and `unless { condition }` guards on every policy. Positive framing. | [Docs](https://docs.cedarpolicy.com/policies/syntax-policy.html) |
| **DMN / FEEL** | FEEL expressions in decision tables. Type constraints via input data definitions. | Named decisions referenceable by name. Business knowledge models for reuse. | Row-based — each row is a positive condition set. First-match or collect-all configurable. No explicit conditional construct. | [Docs](https://www.omg.org/spec/DMN/) |
| **OPA / Rego** | No field constraints. Rules are policies over presented data. | Named rules and packages. Partial rules compose via conjunction. | `default` values and rule ordering for conditional logic. | [Docs](https://www.openpolicyagent.org/docs/latest/policy-language/) |

**Cross-policy pattern:** Rule and policy systems universally use named rules as the primary unit of composition. Guards (`when`, `unless`) are the standard conditional application mechanism — never formal implication. DMN's row-based approach maps to Precept's multi-row transition model. The key insight: named rules in policy systems are *the* composition mechanism, not a convenience feature.

### Enterprise Platforms

| System | Field constraints | Named rules | Conditional constraints | Source |
|--------|------------------|-------------|------------------------|--------|
| **Salesforce** | Validation rules on fields. `ISBLANK()`, `REGEX()`, `LEN()`, comparison operators. Each rule has a name, error message, and optional condition. | Named validation rules referenceable in setup. Cross-object formula fields. | `IF()` and `CASE()` in validation-rule formulas. Conditional applicability via record type or profile-based rule activation. | [Docs](https://help.salesforce.com/s/articleView?id=sf.fields_defining_field_validation_rules.htm) |
| **ServiceNow** | Data policies with field-level conditions. `Mandatory`, `Read only`, value constraints. | Named business rules (`gr.name`). Script includes for reuse. | Business rules with `When to run` conditions and `Filter conditions` for conditional applicability. | [Docs](https://docs.servicenow.com/bundle/latest/page/administer/business-rules/concept/c_BusinessRules.html) |
| **Dynamics 365 / Dataverse** | Column validation rules. `Business required`, value ranges. | Business rules with names and descriptions. Reusable across forms and entities. | `IF/THEN/ELSE` logic in business rule designer. Scope-based activation (entity, all forms, specific form). | [Docs](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule) |
| **Guidewire** | Validation rules on entity fields. Range, required, pattern. | Named validation rules. Rule sets for grouping. | Conditional rules via `availabilityExpression` and context-based activation. | [Docs](https://docs.guidewire.com/) |

**Cross-platform pattern:** Every major enterprise platform studied supports all three composition forms: (a) field-level constraint vocabulary with named constraints, (b) named reusable rules as a first-class concept, and (c) conditional rule applicability based on record state, type, or field values. This is the strongest external signal that constraint composition is not language sugar — it is standard enterprise infrastructure.

### End-User Tools

| System | Field constraints | Named rules | Conditional rules | Source |
|--------|------------------|-------------|-------------------|--------|
| **Excel / Google Sheets** | Data Validation: dropdown lists, whole number ranges, text length limits. All co-located with the cell. | Named ranges as indirect constraint references. No explicit named rules. | Data Validation's `Custom` formula can reference other cells conditionally. Conditional Formatting as visual constraint feedback. | [Excel Docs](https://support.microsoft.com/en-us/office/apply-data-validation-to-cells-29fecbcc-d1b9-42c1-9d76-eff3ce5f7249) |
| **Google Forms** | Response validation: number ranges, text length, regex patterns. Per-question. | No named rules. | Conditional sections based on prior answers (branching). Not constraint composition — form routing. | [Docs](https://support.google.com/docs/answer/3378864) |
| **Notion** | Property constraints: `Number` with no negative, `Select` with enumerated values. Inline per property. | No named rules. Relation constraints. | Rollup formulas for conditional display. No conditional constraints. | — |
| **Airtable** | Field-level validation: required, unique. Linked-record constraints. | No named rules. | Automations for conditional validation logic. Not declarative. | — |

**Cross-end-user pattern:** End-user tools universally co-locate constraints with the field/cell declaration. Named rules are absent — the audience doesn't need that abstraction level. Conditional constraints are handled through form routing or custom formulas, not a declarative conditional mechanism. This confirms that field-level constraints are the *universal* composition form — present in every category studied — while named rules and conditional guards are the features that separate a professional constraint language from a form builder.

### State Machines and Orchestrators

| System | Constraint vocabulary | Named guards | Conditional constraints | Source |
|--------|----------------------|--------------|------------------------|--------|
| **XState** | No field constraints. Context is untyped. | Named guards in machine config: `guards: { isEligible: (ctx) => ... }`. Referenceable by string name in transitions. | Guards on transitions only. No conditional invariants — no invariant concept. | [Docs](https://xstate.js.org/docs/guides/guards.html) |
| **Stateless (.NET)** | No field model. States and triggers only. | Named guard functions via `.PermitIf(trigger, state, guard)`. | Guard-based conditional transitions. No data constraints. | [Docs](https://github.com/dotnet-state-machine/stateless) |
| **Temporal / MassTransit** | No field constraints. Saga state is opaque to the framework. | Named activities/signals. No constraint reuse. | Workflow branching via code. No declarative constraints. | — |

**Cross-state-machine pattern:** State machines support named guards as reusable transition conditions — XState's `guards` config is the closest precedent for Precept's named rules in a transition context. But no state machine has field-level constraints or invariants — the gap Precept fills by combining lifecycle governance with data integrity enforcement.

### Formal Specification and Industry Standards

| System | Constraint composition | Source |
|--------|----------------------|--------|
| **Alloy** | Named predicates (`pred name[args] { ... }`) compose via conjunction. Facts are implicit collect-all. `=>` for implication. `not` keyword for negation. | [Docs](https://alloytools.org/documentation.html) |
| **OCL** | `inv:` constraints on classifiers. `implies` operator. `let` for named sub-expressions. `context`-scoped. | [Docs](https://www.omg.org/spec/OCL/) |
| **FHIR** | Constraint elements in StructureDefinitions: `expression` (FHIRPath), `severity`, `human` (reason text). Named via `key`. | [Docs](https://hl7.org/fhir/conformance-rules.html) |
| **ACORD** | Schema-level constraints via XML Schema facets. Named complex types for reuse. | [Docs](https://www.acord.org/standards-architecture) |
| **ISO 20022** | Message constraints via XML Schema restrictions. Named component reuse. | [Docs](https://www.iso20022.org/catalogue-messages) |

**Cross-formal pattern:** Formal systems use named predicates as the primary composition mechanism (Alloy `pred`, OCL `let`, FHIR `key`). Industry standards use named types/components for structural reuse. The universal pattern: give the constraint a name, make it referenceable.

---

## Philosophy Fit

### Prevention, not detection

**Field constraints strengthen prevention.** Moving `>= 0` from a separate `invariant` statement to the field declaration makes the constraint structurally co-located with the thing it protects. The runtime still enforces it — the prevention guarantee is identical — but the declaration now reads as "this field is a non-negative number," not "this field is a number, and oh by the way, it can't be negative." The constraint is part of the field's identity, not an afterthought.

**Named rules strengthen prevention.** When a 93-character eligibility expression must be duplicated across three transition rows, drift is inevitable. A named rule eliminates the duplication — one source of truth for the business predicate. Prevention is only as strong as the accuracy of the constraint; named rules reduce the surface area where a stale copy can silently weaken the guarantee.

**Conditional guards strengthen prevention.** Without `when` guards on invariants, the workaround is state explosion (12 states instead of 1) or no enforcement at all. State explosion makes the model harder to reason about, increasing the chance of a missing constraint. Conditional guards make the enforcement surface explicit without multiplicative state growth.

### One file, complete rules

Field constraints keep bounds near the field they protect — one fewer place to look. Named rules keep the definition in one place and the references anywhere — one fewer place to update. Conditional guards keep conditional logic in the constraint declaration — one fewer construct to model. All three reduce the distance between "where the constraint is defined" and "where it applies."

### Data truth over movement truth

All three features are data-integrity mechanisms, not routing mechanisms. Field constraints declare what values a field can hold. Named rules declare what combinations of field values constitute a valid business predicate. Conditional guards declare when a data constraint applies. None of these change transition routing — `when` guards on invariants are evaluated post-mutation, not during row selection.

### Determinism and inspectability

**Field constraints** desugar to invariants — same evaluation timing, same collect-all semantics, same inspect output. The inspector shows the desugared invariant; the author sees the co-located suffix.

**Named rules** must be transparent to inspection. When inspect reports what would happen if an event fired, every named rule referenced in a guard must be expanded — the consumer sees the full boolean expression, not an opaque name. The name is an authoring convenience; the inspected result is the expanded truth. This is a non-negotiable contract for Precept's inspectability promise.

**Conditional guards** add one evaluation to the pipeline: "is this invariant applicable?" The guard evaluation itself is inspectable — the tool shows whether the guard was true/false and therefore whether the invariant was checked or skipped.

### AI-readable authoring

Named rules are *more* AI-readable than duplicated multi-clause guards. An AI authoring a precept can define `rule LoanEligible when ...` once and reference it by name — reducing the chance of copy-paste drift. The AI can also verify via MCP inspect that the rule evaluates correctly across states.

Field constraints give the AI a vocabulary: `nonnegative` is more semantically precise than the expression `>= 0`. The AI can generate field declarations with constraint suffixes without reasoning about separate invariant placement.

Conditional guards eliminate the AI's need to model state explosion for categorical conditions.

### Flat, keyword-anchored statements

**Field constraints** extend the existing field-declaration grammar — suffixes after `default`, same line, keyword-led. No new block structure. No indentation requirement. This is the critical design constraint: constraint suffixes must work within the existing flat, keyword-anchored statement model.

**Named rules** are top-level keyword-led statements: `rule <Name> when <Expr>`. Flat. References are identifiers in expression positions. No nesting, no blocks.

**Conditional guards** are suffix keywords on existing statements: `invariant <Expr> when <Guard> because "..."`. The `when` keyword is already established for transition-row guards. Using it on invariants is an extension of the same flat pattern.

### Collect-all validation

All three features must preserve collect-all semantics. Field constraints desugar to invariants — all invariants evaluate, all failures report. Named rules are expanded at each call site — each invariant that references a named rule evaluates independently. Conditional guards skip inapplicable invariants — but all *applicable* invariants evaluate and all failures report.

---

## Locality Boundaries

This is the most design-sensitive dimension. Each of the three features introduces a different scope relationship, and the interactions between them must be explicit.

### Field-local scope (field constraints, #13)

A field constraint references only the field it decorates. `field Amount as number default 0 nonnegative` cannot reference `MaxAmount` or any other field. This is the same scope restriction as field-indented `rule` statements in the current language (per RulesDesign.md): the constraint is *about* this field, so it can only *see* this field.

**What's in scope:** The declared field, literal constants.

**What's excluded:** Other fields, event arguments, collection accessors on other fields.

**Why:** A suffix on a field declaration implies it is a property of that field. Referencing other fields from that position is misleading — the reader assumes locality. Cross-field constraints belong in `invariant` (or a named rule that is itself cross-field).

**Desugaring:** `field Amount as number default 0 nonnegative` → `invariant Amount >= 0 because "Amount must be non-negative"`. The `because` reason is auto-generated from the constraint keyword and field name.

### Cross-field scope (named rules, #8)

A named rule references all persistent entity fields, the same scope as top-level `invariant` and `when` guards. It cannot reference event arguments — that would create a transient dependency in a persistent predicate.

**What's in scope:** All declared fields, collection accessors (`.count`, `contains`), literals.

**What's excluded:** Event arguments (`Event.Arg`), state names, external references.

**Why:** Named rules serve as reusable data predicates. If a rule could reference `Approve.Amount`, it would only be valid when the `Approve` event is in scope — defeating reuse across events. The scope boundary is the same as `invariant`: persistent entity data only.

**Interaction with `on <Event> assert`:** Named rules are explicitly invalid in `on <Event> assert` context. Event asserts validate event arguments, not entity data. A compile-time error rejects the reference: "Field-scoped rule cannot be used in event assert."

### Conditional scope (guarded declarations, #14)

A `when` guard on an invariant or edit declaration references the same scope as the base declaration — field-only for invariants, field-only for edit guards.

**What's in scope:** All declared fields, collection accessors, literals.

**What's excluded:** Event arguments, state names.

**Why:** The guard determines *when* the constraint applies, not *where*. The scope must match the constraint's own scope to avoid temporal coupling between the "applies when" condition and transient event data.

### Locality interaction matrix

| Composition form | Can reference fields? | Can reference event args? | Can reference named rules? | Can be conditional (`when`)? |
|-----------------|----------------------|--------------------------|---------------------------|------------------------------|
| Field constraint suffix | Own field only | No | No | No (future consideration) |
| `invariant` statement | All fields | No | Yes (as identifier) | Yes (`when` guard) |
| `in/to/from <State> assert` | All fields | No | Yes | Yes (state already serves as condition; `when` adds field-condition) |
| `on <Event> assert` | No | Yes (that event's args) | No (scope mismatch) | No (event args are transient; `when` guard scope is field-only) |
| Named rule definition | All fields | No | No (flat — no rule-to-rule) | N/A (the definition is the predicate itself) |
| `when` guard in transition row | All fields + event args | Yes | Yes | N/A (the guard *is* the condition) |

All three composition forms (field constraints, named rules, conditional guards) work identically in stateless precepts. The absence of states does not reduce the composition vocabulary — field-local and cross-field constraints remain available.

### How much suffix syntax is acceptable?

The grammar already has a suffix chain on field declarations: `as <Type> [nullable] [default <Value>]`. Adding constraint suffixes extends this chain: `as <Type> [nullable] [default <Value>] [<constraint>...]`.

**Design limit:** The constraint zone must remain self-identifying — every constraint keyword is unambiguous at the token level. No separator keyword is needed because constraint keywords (`nonnegative`, `positive`, `min`, `max`, `notempty`, `minlength`, `maxlength`, `mincount`, `maxcount`) don't collide with identifiers or existing keywords. The parser's `.Many()` combinator handles arbitrary ordering within the zone.

**Line-length concern:** A field with type, nullable, default, and three constraints approaches comfortable line length:

```precept
field CreditScore as number default 0 nonnegative min 300 max 850
```

This is 60 characters — within the comfortable range. But adding a fourth or fifth constraint would push toward wrapping. The design mitigates this by limiting constraint keywords to basic shape bounds; complex constraints (cross-field, pattern matching) remain as `invariant` statements.

### How named predicates interact with flat statements and inspectability

Named rules are defined as flat, top-level statements and referenced by name in expression positions. The critical interaction is with the inspect/fire pipeline:

1. **At authoring time:** The named rule reads as a concept name. `when LoanEligible` is more readable than the 93-character expansion.

2. **At inspection time:** The MCP `inspect` tool and language-server hover must expand the rule to show the full boolean expression. The name is an authoring convenience; the inspected output is the deterministic truth. If `LoanEligible` is false, inspect should show *which sub-clause* failed — not just "LoanEligible is false."

3. **At error-attribution time:** When a named rule is referenced in an `invariant LoanEligible because "..."`, and the invariant fails, the constraint violation must attribute to the *invariant call site*, not the rule definition site. The violation targets the fields referenced in the expanded expression, and the `because` reason comes from the invariant.

This means named rules are **transparent abstractions** — they reduce duplication at the author level but are fully expanded at every consumer level (inspector, error reporter, AI tools). This is the correct trade for Precept's inspectability guarantee.

---

## Semantic Contracts to Make Explicit

### 1. Desugaring model for field constraints

Field constraint suffixes desugar to `invariant` statements (for field declarations) and `on <Event> assert` statements (for event argument declarations). The desugared constraints:

- Use the same expression grammar as hand-written invariants
- Participate in collect-all evaluation
- Produce the same `ConstraintViolation` structure
- Auto-generate `because` reasons from the constraint keyword and field/arg name

**Nullable interaction:** A nullable field with a constraint desugars with a null guard: `field Amount as number nullable nonnegative` → `invariant Amount == null || Amount >= 0 because "Amount must be non-negative"`. Null is always allowed on nullable fields; constraints apply to the non-null value.

**Default-constraint validation:** The default value must satisfy all declared constraints at compile time. `field Amount as number default -5 nonnegative` is a compile error.

### 2. Named rule scope and resolution

Named rules are resolved at type-check time. The type checker:

- Verifies the rule body is a boolean expression
- Verifies all identifiers resolve in field scope (no event args)
- Rejects rule-to-rule references (flat — no stacking)
- Rejects cycles (trivially satisfied by the no-stacking rule)
- Records the rule as an available identifier in `when`, `invariant`, and state-assert expression positions

**Resolution timing:** Rules are resolved before guard evaluation. A transition row `when LoanEligible && Approve.Amount <= RequestedAmount` is resolved to the full expression at compile time — no runtime symbol lookup.

### 3. Guard evaluation timing

`invariant <Expr> when <Guard> because "..."` evaluates the guard in the same pipeline stage as invariant evaluation (Stage 6 in the fire pipeline). The guard is evaluated first; if false, the invariant is skipped (not counted as a violation, not counted as a pass — it's inapplicable). If true, the invariant expression is evaluated normally.

This means:

- Guards see post-mutation field values (same as invariant expressions)
- Guards participate in the same read-your-writes semantics
- The Update pipeline evaluates guards against post-edit field values
- Inspect preview shows guard evaluation for each conditional invariant

### 4. Error attribution for composed constraints

**Field constraints:** Violations target the field and carry the auto-generated reason. The `ConstraintTarget` is `Field(fieldName)` + `Definition()`, identical to a hand-written invariant targeting the same field.

**Named rules in invariants:** Violations target all fields referenced in the *expanded* expression, not the rule name. The reason is the `because` from the invariant statement, not from the rule definition.

**Conditional invariants:** When a guarded invariant fails, the violation includes the guard expression in diagnostic metadata ("applicable when: `IsPremium`"). The guard condition is part of the violation's context, not its reason.

### 5. Constraint keyword vocabulary

The constraint vocabulary is type-specific and closed:

| Type | Keywords | Desugaring |
|------|----------|-----------|
| `number` | `nonnegative`, `positive`, `min <N>`, `max <N>` | `>= 0`, `> 0`, `>= N`, `<= N` |
| `string` | `notempty`, `minlength <N>`, `maxlength <N>` | `!= ""`, `.length >= N`, `.length <= N` |
| `set`/`queue`/`stack` | `notempty`, `mincount <N>`, `maxcount <N>` | `.count > 0`, `.count >= N`, `.count <= N` |

The vocabulary is deliberately small — only constraints that are (a) single-field, (b) common across domains, and (c) expressible as simple boolean desugaring. Regex, format validators, and cross-field constraints are excluded.

---

## Dead Ends and Rejected Directions

### Parameterized rules

Allowing `rule Check(threshold) when Amount > threshold` would transform named rules from named predicates to parameterized functions — a major semantic expansion. Parameterization introduces:

- A new scoping concept (parameter scope vs field scope)
- Call-site syntax that looks like function calls
- Potential for recursive parameterization

This crosses the line from "named predicate" to "user-defined function," which conflicts with Precept's declarative-only model. Named rules are flat, closed over fields, and resolved at compile time. No parameters.

### Rule-to-rule composition

Allowing `rule Combined when RuleA && RuleB` creates a dependency graph among rules, requiring cycle detection, resolution ordering, and potentially deep expansion chains. The value is low — combining two named predicates in a `when` guard (`when RuleA && RuleB`) achieves the same result without the definitional complexity. Rules are flat: each stands alone, readable by a domain expert or AI without forward references.

### `unless` keyword for negation

The conditional-logic-strategy research surveyed 10 systems and found 7:3 precedent against a dedicated negation keyword. `unless` breaks down on compound conditions (`unless A and B` is ambiguous — De Morgan confusion). Precept uses `when not` as the single canonical negative form. This was decided during #14 research and remains locked.

### Formal `implies` operator

OCL's `inv: isPremium implies minAmount > 5000` is powerful for formal verification but the wrong register for business DSLs. The negated case (`not isPremium implies minAmount > 0` ≡ `isPremium or minAmount > 0`) creates double negatives. All surveyed production systems avoid formal implication in surface syntax. `when` guards achieve the same semantics with positive framing.

### `else true` conditional invariants

`invariant if IsPremium then Amount > 100 else true` was proposed and rejected — invariants that "sometimes don't apply" aren't invariants. The `when` guard is the correct mechanism: `invariant Amount > 100 when IsPremium because "..."`. The guard makes inapplicability explicit rather than hiding it in a trivially-true else branch.

### Regex and format constraints

Adding `pattern`, `email`, `url` as constraint suffixes would be culturally dependent (email format varies by region), frequently changing (URL standards evolve), and impossible to verify at compile time. These are host-application concerns, not entity-integrity concerns. Precept governs data shape, not data format.

### Custom constraint predicates

Allowing `constraint IsValid(field) { ... }` would introduce user-defined constraint functions — a category shift from declarative constraint vocabulary to a general-purpose predicate language. The constraint zone stays closed and vocabulary-driven.

### Conditional field constraints

`field Amount as number nonnegative when IsPremium` would make a field declaration's type shape dependent on runtime field values. This blurs the line between structural declaration and behavioral constraint. Field constraints declare the field's inherent shape; conditional applicability belongs in `invariant ... when`.

### Mutable named rules

Named rules that change their definition based on state or field values would defeat compile-time resolution and transparency. Named rules are static — defined once, expanded everywhere.

### Named rules in event-arg scope

Allowing a named rule to reference `Submit.Amount` would create a persistent predicate with a transient dependency. The rule would be valid only when the `Submit` event is in scope, defeating cross-event reuse. Event-arg validation belongs in `on <Event> assert`, not in named rules.

---

## Current Proposal Implications

### Field-level constraints (#13) — Wave 2

The strongest case of the three. Field constraints address the single largest verbosity category (84% of invariant statements) with a well-understood desugaring model and universal external precedent. The implementation is primarily parser and type-checker work; no new runtime behavior.

Key implementation decisions:
- Constraint zone architecture: suffix position in grammar after `default`, parsed via `.Many()` combinator
- Type-specific vocabularies: constraints validate at parse time against field type
- Desugaring: produces standard `invariant` / `on Event assert` nodes — existing pipeline unchanged
- Diagnostic attribution: violations identify the source as a constraint suffix, not a hand-written invariant

### Guarded declarations (#14) — Wave 2

The natural companion to field constraints. Together they cover the full field-integrity surface: constraints declare shape, guards declare applicability. The `when` keyword is already established in the language; extending it to invariants and edit declarations is a consistent surface extension.

Key implementation decisions:
- Guard evaluation timing: same stage as invariant evaluation (post-mutation)
- Scope restriction: field-only — no event args in guards
- Skip semantics: inapplicable invariants are not violations, not passes — they are skipped
- `when not` form requires #31 (`not` keyword) as a prerequisite

### Named rules (#8) — Wave 4

The highest-risk and highest-reward feature. Named rules are the only composition mechanism that reduces guard duplication across transition rows. But they also introduce the most new concepts: a new declaration keyword, name resolution in expression positions, and transparency requirements for inspection.

Key implementation decisions:
- Scope: field-only, no event args, no rule-to-rule references
- Resolution: compile-time, fully expanded at each call site
- Inspection: MCP tools expand named rules — no opaque references in inspect output
- Error attribution: violations target the call site, not the definition site
- Grammar: `rule <Name> when <Expr>` at top level; name usable as identifier in boolean positions

### Sequencing recommendation

**Wave 2 first:** #13 (field constraints) and #14 (guarded declarations) together. They share the suffix-grammar architecture and address the two most common constraint-authoring patterns. Together they eliminate ~70% of invariant boilerplate while introducing no new scoping concepts.

**Wave 4 second:** #8 (named rules) after the expression surface (#9 conditional expressions, #31 logical keywords) has settled. Named rules are most valuable when the expressions they contain are maximally readable — `and`/`or`/`not` instead of `&&`/`||`/`!`.

---

## Open Research Threads

These are not blockers but areas where additional evidence would strengthen future proposal revisions:

1. **Constraint-zone interaction with future types:** When choice (#25), date (#26), decimal (#27), and integer (#29) ship, each will bring type-specific constraints (e.g., `ordered` on choice, `maxplaces` on decimal). The constraint-zone architecture must accommodate type-specific keyword sets without grammar explosion.

2. **Named rule discoverability in large precepts:** When a precept has 10+ named rules, how does the language server help authors discover which rules exist and what they contain? Completion suggestions, hover expansion, and go-to-definition are the minimum; the question is whether a rules summary or outline view is needed.

3. **Interaction with computed fields:** When a computed field's formula changes, do field constraints on the computed field still make sense? (Likely yes — `computed TotalCost = BasePrice * Quantity` with `nonnegative` asserts the derived value, not the formula.) But the interaction should be documented explicitly.
