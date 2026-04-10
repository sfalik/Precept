# Language Research Domain Map

**Maintained by:** Frank (Lead/Architect)
**Created:** 2026-04-08
**Quality bar:** [`computed-fields.md`](./expressiveness/computed-fields.md) — every domain doc should include: background/problem, precedent survey with cross-category table, philosophy fit analysis, semantic contracts to make explicit, dead ends explored, and proposal implications with acceptance criteria hooks.

---

## Purpose

This document maps every durable research domain the Precept language needs — identified from the language surface itself, open proposals, philosophy, existing research, design docs, and 21 sample files. It is organized by domain, not by issue number. Each row says what research exists today, what is missing or stale, and which open proposals the domain informs.

Domains without an open proposal are included when the language surface, sample patterns, or philosophy clearly imply future work. The map is the starting point for prioritized research execution — not a proposal tracker.

---

## How to Read This Map

- **Strength** rates existing research against the `computed-fields.md` bar: precedent survey, philosophy fit, semantic contracts, dead ends, proposal implications.
- **Missing / Upgrade Needed** is the work required to bring the domain to that bar.
- **Proposals Informed** references GitHub issue numbers. A dash means no open proposal exists but the domain is on the horizon.

---

## Domain Map

### 1. Type System Expansion

| Aspect | Detail |
|---|---|
| **Surface covered** | New scalar types beyond `string`, `number`, `boolean`: choice (constrained value sets), date (day-granularity calendar dates), decimal (exact base-10 arithmetic), integer (whole numbers). Collection inner types. Type coercion rules. |
| **Existing research** | Steinbrenner type-system scoping decision (`.squad/decisions/inbox/`). Frank type-system expansion decision (`.squad/decisions/inbox/`). Proposal bodies for #25, #26, #27, #29 (in `temp/issue-body-rewrites/`) contain precedent surveys and locked design decisions — but these are proposal artifacts, not durable research docs. `expression-language-audit.md` §1.5 documents the current type system limits. `zod-valibot.md` and `fluent-validation.md` touch type surface tangentially. |
| **Strength** | **Weak.** Proposal bodies contain good domain surveys (10-industry field counts, 6-system rule engine comparisons) but this evidence lives in ephemeral issue drafts, not in the research library. No standalone research doc exists in `expressiveness/` or `references/`. A referenced `type-system-domain-survey.md` was never created. No philosophy fit analysis against the full 13 design principles. No dead-ends section for rejected type additions (structured types, duration, datetime). No semantic contracts for type coercion, comparison compatibility, or collection inner-type constraints. |
| **Missing / Upgrade needed** | **High priority.** Needs a `type-system-research.md` in `expressiveness/` that: (1) consolidates the 10-domain field survey from the four proposal bodies into one durable table, (2) adds a cross-category precedent survey at `computed-fields.md` quality (databases, DSLs, enterprise platforms, validators, standards — all four type additions), (3) philosophy fit analysis for each type against all 13 principles, (4) dead ends explored (datetime/timezone, money type, structured types, parameterized types), (5) semantic contracts for mixed-type arithmetic, coercion hierarchy, and constraint interaction. Also needs a `type-system-theory.md` in `references/` covering formal type system concepts: sum types, product types, bounded polymorphism, subtyping, and how they map to Precept's flat-field model. |
| **Proposals informed** | #25 (choice), #26 (date), #27 (decimal), #29 (integer) |
| **Why it matters** | Every domain survey shows 40%+ of fields need types Precept doesn't have. Without choice, every enumeration is a typo-vulnerable string. Without date, temporal logic is faked with numbers. Without decimal, financial arithmetic is silently imprecise. This is the single largest capability gap in the language surface. |

---

### 2. Conditional & Value Expressions

| Aspect | Detail |
|---|---|
| **Surface covered** | Conditional value expressions (`if...then...else` in `set` RHS), null-coalescing (`??`), conditional defaults. The ability to select between values based on conditions within expressions. |
| **Existing research** | `conditional-logic-strategy.md` — **strong** for the `when`/`if` split and the `else reject` (#12) decline decision. Covers the teaching model, precedent survey (10 systems), consistency audit, dead ends (`implies`, `else true`). `expression-language-audit.md` L1 (ternary) and L7 (null-coalescing) — severity-ranked with implementation notes and sample evidence. `expression-evaluation.md` — formal grounding on decidable fragments. |
| **Strength** | **Moderate-to-good.** The `when`/`if` split is well-documented. But the conditional expression (#9) itself has no dedicated research doc — its precedent survey, philosophy fit, and dead ends live only in `conditional-logic-strategy.md` as a subsection. Null-coalescing has no research at all beyond the audit entry. |
| **Missing / Upgrade needed** | Conditional expressions (#9) are close to adequate coverage but need: (1) philosophy fit against Principle 8 (compile-time-first) and Principle 12 (AI legibility) — does ternary in `set` RHS hurt or help AI authoring? (2) Scope-restriction contracts: which expression positions accept `if...then...else`? The audit says "set RHS only initially" but the research doesn't explore why or what unlocking invariant/assert positions later would cost. Null-coalescing needs a standalone evaluation: precedent survey (C#, TypeScript, SQL COALESCE, Kotlin `?:`, Swift `??`), interaction with nullable type narrowing model, scope restrictions. |
| **Proposals informed** | #9 (conditional expressions). Null-coalescing is an audit-identified gap (L7) without a proposal. |
| **Why it matters** | Conditional expressions eliminate the most frequent row-duplication pattern in the corpus (14+ samples per the audit). Null-coalescing reduces nullable field handling boilerplate. Together they address the two most common reasons authors must write multiple transition rows for what is logically one operation. |

---

### 3. Named Rules & Constraint Reuse

| Aspect | Detail |
|---|---|
| **Surface covered** | Named boolean predicates (`rule LoanEligible when <expr>`), referenceable in `when` guards, `invariant` expressions, and state assert expressions. Constraint composition patterns. |
| **Existing research** | `expression-language-audit.md` L3 — detailed implementation notes, scope analysis, philosophy fit assessment. `constraint-composition.md` — formal PLT grounding (predicate combinators, Boolean lattices, Alloy/Zod/JML precedent). `xstate.md`, `polly.md`, `fluent-validation.md` — tangential precedent. |
| **Strength** | **Moderate.** The audit's L3 section is thorough on implementation feasibility and scope restrictions (data-only, no event args, no `on Event assert`). `constraint-composition.md` provides excellent formal grounding. But there is no standalone named-rules research doc that meets the `computed-fields.md` bar — no cross-category precedent table, no explicit philosophy fit analysis per principle, no dead-ends section (what about parameterized rules? rule hierarchies? rule groups?), no semantic contracts for cycle detection, scope inheritance, or tooling surface. |
| **Missing / Upgrade needed** | Needs a `named-rules-research.md` in `expressiveness/` that: (1) cross-category precedent survey (Drools named rules, Alloy predicates, FluentValidation rule sets, OPA partial rules, Cedar policy groups), (2) philosophy fit per principle (especially Principle 7 — self-contained rows — and Principle 4 — locality of reference), (3) dead ends (parameterized rules, rule-of-rules, rule inheritance), (4) semantic contracts (cycle detection, scope resolution, error attribution through rule expansion, tooling visibility). |
| **Proposals informed** | #8 (named rule declarations) |
| **Why it matters** | Guard reuse is the #3 maintainability concern in the corpus. The 93-character loan eligibility condition appears once but would need verbatim duplication in any additional event that requires the same check. Named rules are the prerequisite for scaling precepts beyond 5-6 guarded transitions without copy-paste drift. |

---

### 4. Field-Level Constraints

| Aspect | Detail |
|---|---|
| **Surface covered** | Inline constraint annotations on field declarations: `min`, `max`, `nonnegative`, `positive`, `maxplaces`. Replaces the `invariant Field >= 0 because "..."` boilerplate pattern. The "constraint zone" architecture. |
| **Existing research** | `internal-verbosity-analysis.md` #3 — identifies non-negative invariant boilerplate as the #3 verbosity smell (19/21 samples). `zod-valibot.md` — shows inline `.min()/.max()` patterns. `fluent-validation.md` — shows `.GreaterThan(0)` co-located constraints. `expression-language-audit.md` — notes field-level constraint gap implicitly. Proposal #13 body (in `temp/issue-body-rewrites/13.md`) contains constraint-zone architecture. Proposal #27 references `maxplaces` as a constraint-zone extension. |
| **Strength** | **Weak.** The verbosity analysis establishes the problem clearly. Library comparisons show the pattern. But there is no standalone research doc. The constraint-zone architecture — which is the key design innovation (constraints as keyword suffixes in the declaration line, not separate invariant statements) — lives only in the proposal body. No precedent survey across the full positioning spectrum (databases, enterprise platforms, validators, IaC). No philosophy fit analysis. No dead ends (what about regex patterns? custom constraint functions? constraint inheritance?). No semantic contracts for interaction between field-level constraints and invariants, or between field constraints and `edit` declarations. |
| **Missing / Upgrade needed** | Needs a `field-constraints-research.md` in `expressiveness/` that: (1) cross-category precedent survey for inline field constraints (SQL CHECK, Zod schema chaining, FluentValidation built-ins, Protobuf field options, Terraform attribute constraints, Salesforce field validation rules), (2) constraint-zone architecture rationale as durable research (how keyword suffixes stay parser-friendly), (3) philosophy fit against Principle 3 (minimal ceremony) and Principle 1 (keyword-anchored — does `min 0 max 100` stay on one flat line?), (4) interaction semantics: how do field constraints compose with invariants? Are they sugar or a distinct enforcement mechanism? (5) dead ends (regex, custom constraints, constraint DSL-within-DSL). |
| **Proposals informed** | #13 (field-level range/basic constraints). Also prerequisite architecture for #25 (`ordered` constraint), #27 (`maxplaces`), #29 (integer constraints). |
| **Why it matters** | This single feature eliminates 60-70% of all `invariant` statements in the corpus. It is also the architectural foundation for type-specific constraints (`ordered` on choice, `maxplaces` on decimal), making it a prerequisite for the type system expansion. |

---

### 5. String Operations

| Aspect | Detail |
|---|---|
| **Surface covered** | String `.length` accessor, string `.contains()` membership test, string comparison operators (`<`, `>`, `<=`, `>=`). Substring operations, pattern matching (future). |
| **Existing research** | `expression-language-audit.md` L2 (`.length` — Critical), L6 (`.contains()` — Significant), L10 (string comparison — Moderate). All three include implementation notes, sample evidence, and feasibility verdicts. `zod-valibot.md` — shows `.min(1).max(100)` on strings. `fluent-validation.md` — shows `.MaximumLength(100)`. `expression-evaluation.md` — formal category (regular language predicates, decidable). |
| **Strength** | **Moderate.** The audit provides good severity classification and implementation guidance. But no standalone research doc exists. The audit entries are implementation-focused, not research-focused — they lack cross-category precedent tables, philosophy fit analysis, and dead-ends exploration (what about regex? startsWith/endsWith? trim? toUpper/toLower?). |
| **Missing / Upgrade needed** | Needs a `string-operations-research.md` in `expressiveness/` that: (1) cross-category precedent survey for string accessors/predicates in constrained DSLs (FEEL string functions, Cedar string operations, SQL string functions, Zod/FluentValidation string validators, Alloy string operations), (2) philosophy fit — especially Principle 8 (decidability risk of regex vs safe string predicates), (3) dead ends (regex, interpolation, template strings, case conversion), (4) semantic contracts for `.length` on nullable strings and `.contains()` on nullable strings, (5) the full string accessor surface: which operations to ship first vs defer. |
| **Proposals informed** | #10 (string `.length`), #15 (string `.contains()`). String comparison is audit-identified (L10) but has no proposal. |
| **Why it matters** | String length constraints are table-stakes for any data-quality system. All 21 samples have string fields that lack length validation because the accessor doesn't exist. `.contains()` enables basic format validation (email has `@`, serial number has `-`). These are the lowest-cost, highest-impact expression gaps. |

---

### 6. Built-in Functions & Numeric Operations

| Aspect | Detail |
|---|---|
| **Surface covered** | Built-in expression functions: `abs()`, `min()`, `max()`, `floor()`, `ceil()`, `round()`. The function-call AST node and static function registry architecture. Future: string functions, date functions, type conversion functions. |
| **Existing research** | `expression-language-audit.md` L5 — detailed implementation notes for function-call syntax and static registry. `expression-evaluation.md` — formal decidability analysis for bounded built-ins. Frank `round-function` decision (`.squad/decisions/inbox/`). Proposal #16 body (in `temp/issue-body-rewrites/16.md`) — comprehensive built-in function proposal. Proposal #27 introduces `round(decimal, N)` as the first expression function. |
| **Strength** | **Weak-to-moderate.** The audit identifies the gap and implementation path. The expression-evaluation reference provides formal footing. But there is no standalone research doc. No cross-category precedent survey for built-in function surfaces in constrained DSLs. No architecture research for the function-call expression node (which is the most invasive parser change in the entire proposal set). No philosophy fit analysis for introducing function-call syntax into a keyword-anchored language. No dead ends (user-defined functions, higher-order functions, aggregate functions). |
| **Missing / Upgrade needed** | Needs a `built-in-functions-research.md` in `expressiveness/` that: (1) cross-category precedent survey for built-in function surfaces (FEEL, Cedar, DMN, SQL, Alloy, OPA/Rego, Drools), (2) function-call syntax architecture: how `Name(args)` integrates with Superpower's atom combinator without ambiguity, (3) philosophy fit — does function-call syntax shift the language toward a general-purpose feel? How to keep it configuration-like? (4) dead ends (user-defined functions, lambda expressions, aggregate functions over collections), (5) the registry architecture: static dispatch, arity/type checking, extensibility. Also needs a `function-call-semantics.md` in `references/` covering formal function-call semantics in constrained expression languages. |
| **Proposals informed** | #16 (comprehensive built-in functions). Also prerequisite architecture for #27 (`round()`). |
| **Why it matters** | Function calls are the most architecturally significant expression expansion. Once the function-call AST node and registry exist, every future accessor, converter, and predicate function can be added incrementally. This is infrastructure, not just a feature. |

---

### 7. Computed / Derived Fields

| Aspect | Detail |
|---|---|
| **Surface covered** | Read-only fields whose value is a declared expression over other fields. Recomputation timing in Fire, Update, and Inspect pipelines. Dependency ordering and cycles. Writeability restrictions. Tooling/serialization surface. |
| **Existing research** | `computed-fields.md` — **THE QUALITY BAR.** Full cross-category precedent survey (17 systems across 9 categories), philosophy fit analysis, 6 semantic contracts explicitly stated, 5 dead ends explored, proposal implications with acceptance criteria hooks. `expression-language-audit.md` — identifies the formula-drift problem. `expression-evaluation.md` — confirms decidability of field-local derivation. |
| **Strength** | **Excellent.** This is the reference document for what comprehensive domain research looks like. |
| **Missing / Upgrade needed** | None for the research itself. The proposal (#17) needs updating to reflect the research contracts — that's a proposal task, not a research task. |
| **Proposals informed** | #17 (computed/derived fields) |
| **Why it matters** | Eliminates manual synchronization across transition rows for derived values. Removes the formula-drift class of bugs entirely. Travel-reimbursement, event-registration, and loan-application samples all exhibit the pattern. |

---

### 8. Event Argument Ingestion (Absorb)

| Aspect | Detail |
|---|---|
| **Surface covered** | Bulk field assignment from event arguments. The `absorb Event` or name-matched auto-mapping shorthand. Reduces N `set Field = Event.Arg` statements to 1. |
| **Existing research** | `internal-verbosity-analysis.md` #1 — identifies event-argument ingestion as the #1 verbosity smell (21/21 samples, 4-7 SET statements per intake transition). Corpus examples from 15+ samples. Hypothetical syntax sketched. |
| **Strength** | **Weak.** The verbosity analysis establishes the problem definitively — it's the most pervasive pattern in the corpus. But there is no standalone research doc. No cross-category precedent survey for bulk assignment patterns (destructuring in JS/TS, C# records, Kotlin data class copy, actor message handling). No philosophy fit analysis (does `absorb` hide individual assignments, violating Principle 7 self-contained rows?). No semantic contracts (what happens with name mismatches? computed fields during absorb? nullable handling?). No dead ends (field-name-matching vs explicit mapping, partial absorb, absorb with overrides). |
| **Missing / Upgrade needed** | Needs an `absorb-research.md` in `expressiveness/` at `computed-fields.md` quality. Key sections: (1) cross-category precedent survey for bulk assignment / destructuring in constrained systems, (2) philosophy fit — the tension between Principle 7 (self-contained rows: absorb hides individual assignments) and Principle 3 (minimal ceremony: absorb eliminates 4-7 lines), (3) semantic contracts (name matching rules, partial absorb, computed overrides, interaction with nullable fields, interaction with computed fields), (4) dead ends (declaration-time binding, runtime reflection, implicit absorb). |
| **Proposals informed** | #11 (event argument absorb shorthand) |
| **Why it matters** | The single most impactful compactness improvement. Every sample in the corpus exhibits this pattern. A 5-argument intake event currently costs 5 SET statements + 1 transition = 6 actions for one logical operation. Absorb collapses this to 1-2 statements. |

---

### 9. Conditional Declarations (Guards on Invariants & Edit)

| Aspect | Detail |
|---|---|
| **Surface covered** | `when` guards on `invariant` declarations. `when` guards on `in <State> edit` declarations. The `when not` form for negative conditions. Interaction with choice types for categorical modeling. |
| **Existing research** | `conditional-logic-strategy.md` — **strong.** Covers the `when`/`if` teaching model, the `else true` dead end, the `implies` dead end, the negation problem root cause, the `when not` resolution, the `unless` rejection (7-to-3 precedent against). Full consistency audit of all `when`/`if` usage. 10-system precedent survey. `conditional-invariant-survey.md` — detailed system-by-system capture (FluentValidation, Drools, Cedar, DMN, JSON Schema, Zod, OCL, Alloy, CSS, SQL CHECK). |
| **Strength** | **Good-to-excellent.** This is the second-strongest domain in the research library after computed fields. The only gap: no explicit semantic contracts section (when does a guarded invariant vs a state assert vs a guarded edit declaration differ in enforcement timing?). |
| **Missing / Upgrade needed** | Minor: add a semantic contracts section to `conditional-logic-strategy.md` that explicitly states enforcement timing for `invariant ... when` vs `in State assert` vs `on Event assert`. Currently the enforcement model is implicit across ConstraintViolationDesign.md and PreceptLanguageDesign.md. One section consolidating the enforcement timeline would complete the research. |
| **Proposals informed** | #14 (when guards on declarations), #31 (not keyword — `when not` form). Also #25 (choice types eliminate the negation problem at modeling level). |
| **Why it matters** | Conditional invariants address the class of constraints that are sometimes-true: "premium customers must have a credit card" should not apply to standard customers. Without conditional guards, authors must either over-constrain (invariant applies always) or under-constrain (no invariant at all, rely on guard logic). |

---

### 10. Data-Only Precepts (Stateless Definitions)

| Aspect | Detail |
|---|---|
| **Surface covered** | Precept definitions without state machines. Fields, invariants, and edit declarations only. `edit all` / `edit Field1, Field2` root-level syntax. Nullable CurrentState API. |
| **Existing research** | `data-only-precepts-research.md` — covers the mixed-tooling problem, DDD entity vs value object precedent, progressive disclosure principle, `edit all` vs `edit any` decision, API decisions, dead ends explored (`close #22`, `edit any` syntax). |
| **Strength** | **Moderate-to-good.** Covers the problem, precedent, and key decisions well. But the precedent survey (5 systems) is narrower than `computed-fields.md` (17 systems across 9 categories). No cross-category table. No philosophy fit analysis against all 13 principles. No semantic contracts for API behavior (what does Inspect return for a stateless precept? What does Fire return?). |
| **Missing / Upgrade needed** | Moderate: (1) expand the precedent survey to `computed-fields.md` breadth — add enterprise platforms (Salesforce objects vs custom objects, ServiceNow CMDB vs workflow tasks), industry standards (FHIR resources vs workflows), and IaC (Terraform data sources), (2) philosophy fit per principle, (3) semantic contracts for Inspect/Fire/Update on stateless instances. |
| **Proposals informed** | #22 (data-only precepts) |
| **Why it matters** | In every real domain, data entities outnumber workflow entities. Insurance has Claims (stateful) but also Adjusters, Rate Tables, Policy Templates (stateless). A language that only governs the workflow side covers the minority. Data-only precepts complete the domain coverage. |

---

### 11. Multi-Event & Multi-State Shorthand

| Aspect | Detail |
|---|---|
| **Surface covered** | Multi-event `on` clause in transition rows (`from any on Cancel, Withdraw -> ...`). Existing multi-state `from` shorthand. Multi-name declarations. Catch-all `on any` row. Event-set abstraction. |
| **Existing research** | `multi-event-shorthand.md` — solid PLT reference (symbolic finite automata, CSP event sets, UML multi-event arcs). Documents existing shorthand inventory. `state-machine-expressiveness.md` — covers hierarchical states and parallel regions as higher-cost alternatives. `expression-compactness.md` — formal desugaring framework. `internal-verbosity-analysis.md` — documents guard-pair duplication (#2 verbosity smell). |
| **Strength** | **Moderate.** The PLT grounding is good. But multi-event `on` has no dedicated research beyond the reference doc — no precedent survey for DSL-specific implementations, no philosophy fit analysis, no semantic contracts (what happens when events in the set have different arg shapes? How does tooling show expanded rows?). Catch-all `on any` is identified as a gap in `references/README.md` but has no research. |
| **Missing / Upgrade needed** | Moderate: (1) multi-event `on` needs implementation-focused research: arg-shape compatibility rules, desugaring error attribution, tooling impact (does Inspect show the compacted form or the expanded form?), (2) catch-all `on any` needs a dedicated research section: masking risk for `Undefined` outcomes, interaction with reachability analysis (C48-C52), precedent survey. |
| **Proposals informed** | No open proposal for multi-event `on` (identified as highest-value compactness candidate in `references/README.md`). No proposal for catch-all. |
| **Why it matters** | Multi-event `on` is the highest-value-for-lowest-cost compactness candidate identified across all research. The references README ranks it #1 for future investigation. Catch-all rows would eliminate repeated rejection boilerplate. |

---

### 12. Keyword vs Symbol Surface

| Aspect | Detail |
|---|---|
| **Surface covered** | The keyword-dominant, symbol-for-math design framework. Logical operator migration (`&&`→`and`, `||`→`or`, `!`→`not`). The `!=` exception. Decision matrix for new syntax. |
| **Existing research** | `conditional-logic-strategy.md` §Keyword vs Symbol Decision — covers the full spectrum analysis, 3 justification reasons, the `!=` asymmetry argument, 50+ years of SQL/Python precedent. `PreceptLanguageDesign.md` §Keyword vs Symbol Design Framework (Locked) — the design decision itself with decision matrix. |
| **Strength** | **Good.** Well-documented and locked. The research and the design doc are in sync. |
| **Missing / Upgrade needed** | Minor: The research justification lives in `conditional-logic-strategy.md`, which is about conditional logic — not keywords. A better home would be a standalone `keyword-symbol-research.md` in `references/` that captures the spectrum analysis, the cognitive readability evidence, and the Fowler DSL design principles as a reusable reference for future keyword decisions. Currently, anyone evaluating a new keyword/symbol choice must dig into the conditional logic file. |
| **Proposals informed** | #31 (replace `!`, `&&`, `||` with `not`, `and`, `or`) |
| **Why it matters** | Every new language construct requires a keyword-or-symbol decision. The framework is locked but the research justification should be findable by domain, not buried in a conditional-logic file. |

---

### 13. Null Safety & Type Narrowing

| Aspect | Detail |
|---|---|
| **Surface covered** | Nullable type modifier. Null-narrowing via `&&` short-circuit. Null-coalescing operator (`??`). Nullable string concatenation. Nullable accessor safety (`.length` on nullable string, `.peek` on empty collection). |
| **Existing research** | `expression-language-audit.md` L7 (null-coalescing — Significant), L12 (nullable string concatenation — Moderate). `expression-evaluation.md` §Kotlin smart casts, §TypeScript discriminated unions — formal type narrowing patterns. Type checker implementation documents nullable flag propagation (`StaticValueKind.Null`). |
| **Strength** | **Weak.** The audit identifies two specific gaps. The expression-evaluation reference provides formal patterns. But there is no standalone null-safety research. No precedent survey for null-handling strategies in constrained DSLs (Kotlin null safety, TypeScript strict null checks, Rust Option, Swift optionals, SQL NULL semantics). No philosophy fit analysis for Precept's specific nullable model. No semantic contracts for how null interacts with new types (choice nullable? date nullable? decimal nullable?). |
| **Missing / Upgrade needed** | Needs a `null-safety-research.md` in `references/` that: (1) surveys null-handling strategies across languages and DSLs, (2) maps Precept's current model (nullable modifier + `&&` narrowing) against the landscape, (3) evaluates future options (null-coalescing, safe navigation `?.`, exhaustive null checking), (4) defines interaction contracts between nullability and the type system expansion (every new type needs nullable semantics). |
| **Proposals informed** | No dedicated proposal. Null-coalescing is audit-identified (L7). Intersects with every type-system proposal (#25-#29) via nullable variants. Intersects with #17 (computed fields) via nullable input handling. |
| **Why it matters** | Nullable handling is a cross-cutting concern that touches every type, every expression position, and every constraint form. As the type system expands (choice, date, decimal, integer), each new type inherits nullable semantics — and the contracts for how nullable interacts with new operators, accessors, and constraints must be defined explicitly. Deferring this research means each type proposal reinvents nullable handling independently. |

---

### 14. Collection Operations & Quantifiers

| Aspect | Detail |
|---|---|
| **Surface covered** | Current: `.count`, `.min`, `.max`, `.peek`, `contains`. Future: quantified predicates (`any(element => condition)`, `all(element => condition)`), extended accessors (`.first`, `.last`), filtering. |
| **Existing research** | `expression-language-audit.md` L9 (`any`/`all` — Moderate, verdict: "not recommended" for now due to lambda scoping complexity). Current accessor surface documented in §1.3. |
| **Strength** | **Weak.** The audit correctly identifies the gap and the implementation cost. But the "not recommended" verdict deserves research backing — no precedent survey for collection predicates in constrained DSLs, no analysis of whether simpler forms (without lambda) could work, no philosophy fit analysis. |
| **Missing / Upgrade needed** | Low priority for now (audit's deferral is reasonable). When revisited: needs research on collection predicates without lambda syntax (Alloy set comprehensions, SQL WHERE clauses, FEEL list filters). The question is whether a non-lambda form exists that stays within Precept's flat expression model. |
| **Proposals informed** | No open proposal. Horizon domain — revisit after L1-L5 expression gaps close. |
| **Why it matters** | 11 of 21 samples use collections. Current accessors cover "how many" and "does it contain X" but not "do all elements satisfy Y" or "does any element satisfy Y." The gap limits collection-based business rule expressiveness. |

---

### 15. Set-Membership Tests (Inline Constant Sets)

| Aspect | Detail |
|---|---|
| **Surface covered** | Testing whether a scalar value equals any member of an inline constant set: `Priority in ["Low", "Medium", "High"]`. The `in` keyword conflict. Alternative syntax (`one of`, `any of`). |
| **Existing research** | `expression-language-audit.md` L8 — identifies the gap, the `in` keyword lexical conflict, and alternative syntax mitigations. |
| **Strength** | **Weak.** Single audit entry. No precedent survey, no philosophy fit, no semantic contracts. |
| **Missing / Upgrade needed** | Moderate priority. This domain partially overlaps with #25 (choice types) — once choice types exist, the most common inline-set pattern (`Priority in ["Low", "Medium", "High"]`) becomes a choice field constraint instead. Research should evaluate: (1) does choice (#25) eliminate most use cases? (2) what remains after choice? (3) precedent for set-membership in constrained DSLs (SQL IN, FEEL, DMN). |
| **Proposals informed** | No open proposal. Intersects with #25 (choice types may subsume most use cases). |
| **Why it matters** | The pattern appears in helpdesk ticket, insurance claim, and travel reimbursement samples. However, if choice types ship first, the remaining use cases are narrow. Priority depends on #25 sequencing. |

---

### 16. Constraint Violation & Error Attribution

| Aspect | Detail |
|---|---|
| **Surface covered** | Structured violation reporting: which constraint was violated, what it targets (field, event arg, state, definition), where it came from (invariant, state assert, event assert, rejection). Consumer-facing attribution for UI rendering (inline vs banner). |
| **Existing research** | `ConstraintViolationDesign.md` — **design doc, not research.** Documents the runtime model (ConstraintTarget, ConstraintSource), four constraint kinds, and the runtime API. `EditableFieldsDesign.md` — covers field-level error vs form-level error attribution for the preview inspector. |
| **Strength** | **Moderate as design, weak as research.** The design doc is thorough for what it covers. But there is no research grounding — no precedent survey for how other systems report violations (FluentValidation error chains, Zod ZodIssue arrays, JSON Schema error paths, Salesforce validation error rendering). No analysis of AI-consumer attribution needs (how does an MCP tool surface violations in a structured, machine-readable way?). |
| **Missing / Upgrade needed** | Low-medium priority. When the design doc moves to implementation: needs a research doc covering (1) precedent survey for structured error attribution in validators and platforms, (2) AI-consumer attribution contracts (how Inspect and Fire results expose violation targets for MCP tool consumers), (3) localization and reason-template patterns. |
| **Proposals informed** | No open proposal. Design doc exists. Implementation blocked on other work. |
| **Why it matters** | Error attribution quality directly affects both human UX (inline field errors vs form banners) and AI legibility (structured violation objects vs string lists). Every constraint feature (#8, #13, #14, #17) produces violations that need correct attribution. |

---

### 17. Division Safety & Runtime Expression Guards

| Aspect | Detail |
|---|---|
| **Surface covered** | Division-by-zero behavior (currently silent NaN/infinity). Runtime expression evaluation safety. Potential compile-time warnings for provably-unsafe expressions. |
| **Existing research** | `expression-language-audit.md` L11 — identifies the problem, two implementation approaches, sample evidence from `travel-reimbursement.precept` and `maintenance-work-order.precept`. |
| **Strength** | **Weak.** Single audit entry with implementation options. No precedent survey, no philosophy fit analysis (Principle 1 — deterministic model: is NaN deterministic?). |
| **Missing / Upgrade needed** | Low priority. Feasible as an evaluator fix (option b in audit) with no grammar change. Minimal research needed — primarily a runtime behavior decision. |
| **Proposals informed** | No open proposal. Identified as audit gap L11. |
| **Why it matters** | Silent NaN propagation through comparisons produces unpredictable guard behavior. A `travel-reimbursement` guard that divides by zero will evaluate to `false` (NaN comparison), silently skipping the row. This is a correctness risk, not just a quality-of-life issue. |

---

### 18. State Machine Expressiveness (Advanced)

| Aspect | Detail |
|---|---|
| **Surface covered** | Hierarchical (nested) states. Parallel (orthogonal) regions. History pseudostates. Broadcast events. Generation 3 statechart features. Catch-all `on any` row. |
| **Existing research** | `state-machine-expressiveness.md` — thorough PLT reference covering all Generation 3 features, with explicit cost assessments. Hierarchical states marked "HIGH cost — architecturally invasive." Parallel regions marked as high cost. XState comparison in `xstate.md`. |
| **Strength** | **Good as a "what not to do" reference.** The research correctly establishes that Generation 3 features are too costly for Precept's flat, self-contained-row model. The catch-all `on any` analysis is thinner — identified as a gap in `references/README.md` but not fully explored. |
| **Missing / Upgrade needed** | Low priority for hierarchy/parallelism (deliberately excluded). Catch-all `on any` deserves dedicated research: (1) masking semantics (does `on any` suppress `Undefined` outcomes?), (2) precedent (xstate wildcard transitions, UML completion transitions), (3) interaction with reachability analysis. |
| **Proposals informed** | No open proposal. Hierarchy explicitly excluded from the roadmap. Catch-all is a horizon candidate. |
| **Why it matters** | The research serves as a durable "why not" reference that prevents future proposals from re-litigating hierarchy/parallelism. The catch-all question matters for samples with repeated rejection boilerplate across many events. |

---

### 19. Temporal & Ordering Assertions

| Aspect | Detail |
|---|---|
| **Surface covered** | Event-ordering constraints: "event A must occur before event B." Temporal logic operators analogous to TLA+ temporal assertions. Sequence constraints. |
| **Existing research** | `references/README.md` §Notes for Future Research — identified as a gap. One sentence: "Temporal assertions — `before`/`after` event ordering constraints, analogous to TLA+ temporal operators. High semantic cost; not currently in scope." |
| **Strength** | **None.** Identified but unexplored. |
| **Missing / Upgrade needed** | Future priority. When demand arises: needs a `temporal-assertions.md` in `references/` covering (1) TLA+ temporal logic, (2) BPMN sequence constraints, (3) Alloy ordering predicates, (4) cost analysis for Precept's flat model (temporal logic requires history awareness, which Precept's stateless-per-operation model doesn't have). |
| **Proposals informed** | None. Horizon domain. |
| **Why it matters** | Some business domains have implicit ordering requirements ("document verification must complete before approval"). Today these are enforced structurally through the state graph (verification is a prerequisite state). Temporal assertions would make the ordering *declared* rather than *structural* — but at high semantic cost. The research should determine whether the state graph already serves this need adequately. |

---

### 20. Cross-Entity / Multi-Precept References

| Aspect | Detail |
|---|---|
| **Surface covered** | References between precept definitions. Cross-entity constraints. Entity composition. Multi-precept systems. |
| **Existing research** | `computed-fields.md` §Dead Ends — "Cross-Precept Derivations" explicitly rejected: "the more a computed field reaches across entity boundaries, the more Precept stops being a one-file integrity contract and starts becoming a distributed query language." `philosophy.md` — "one file, complete rules" is a core differentiator. |
| **Strength** | **Weak but deliberately bounded.** The computed-fields dead-ends section correctly identifies the category risk. But there is no research exploring the *legitimate* forms of cross-entity reference that don't violate one-file integrity (shared choice type definitions, shared rule libraries, entity-relationship declarations for documentation/tooling). |
| **Missing / Upgrade needed** | Low priority. When demand arises: needs research distinguishing (1) cross-entity *constraints* (rejected — violates one-file integrity), (2) cross-entity *references* (entity A has a field whose type is entity B's identifier — relationship declaration for tooling, not enforcement), (3) shared vocabularies (choice type definitions reused across precepts — DRY without coupling enforcement). |
| **Proposals informed** | None. Horizon domain. |
| **Why it matters** | Real domain systems have entity relationships. Insurance has Claims ↔ Policies ↔ Adjusters. Precept's philosophy is one-file integrity, but tooling benefits (cross-entity Inspect, relationship diagrams) don't require cross-entity *enforcement*. The research should draw the line between declaration and enforcement. |

---

### 21. Schema Evolution & Versioning

| Aspect | Detail |
|---|---|
| **Surface covered** | How precept definitions evolve over time. Field additions/removals. State graph changes. Migration of existing instances. Backward compatibility guarantees. |
| **Existing research** | None. |
| **Strength** | **None.** Completely unexplored. |
| **Missing / Upgrade needed** | Future priority. Becomes critical when Precept is used in production with persisted instances. Needs research on: (1) how database schema migration tools handle evolution (EF migrations, Flyway, Liquibase), (2) how state machine tools handle version changes (xstate version interop), (3) Precept-specific migration semantics (what happens when a state is removed but instances exist in that state? when a field is added with no default?), (4) compatibility rules (additive changes that are always safe vs breaking changes that require migration). |
| **Proposals informed** | None. Horizon domain. |
| **Why it matters** | Any production system with persisted entities must handle definition evolution. Without migration semantics, every definition change risks orphaning existing instances. This is not urgent during language design but becomes non-negotiable at production adoption. |

---

### 22. AI Legibility & Tooling Surface

| Aspect | Detail |
|---|---|
| **Surface covered** | How the language surface, diagnostics, and tool APIs serve AI agent consumers. MCP tool contracts. Diagnostic structure for AI consumption. Language reference as queryable data (`precept_language`). |
| **Existing research** | `McpServerDesign.md` — MCP tool contracts. `CliDesign.md` — CLI design (not implemented). `philosophy.md` — "AI is a first-class consumer." `PreceptLanguageDesign.md` Principle 12 — "AI is a first-class consumer." `expression-language-audit.md` §5 "On AI legibility" — notes on enumerable/declarative representations. |
| **Strength** | **Moderate as design philosophy, weak as research.** The principle is stated clearly. The MCP tools are shipped. But there is no *research* evaluating how effectively AI agents use the current surface — no AI authoring study, no diagnostic legibility evaluation, no comparison with how AI agents interact with other DSLs. |
| **Missing / Upgrade needed** | Low-medium priority. As the AI-first philosophy matures: needs research on (1) AI authoring success rates with the current surface (empirical, from Copilot plugin usage), (2) diagnostic legibility for AI consumers (are constraint codes sufficient? do AI agents need more structured error objects?), (3) precedent survey for AI-native DSL design (Copilot-optimized languages, AI-friendly API design patterns). |
| **Proposals informed** | No language-specific proposal. Cross-cuts every language feature through Principle 12. |
| **Why it matters** | Precept's philosophy positions AI as a first-class consumer. Every language change is evaluated against AI legibility. But without research grounding, "AI legibility" remains an intuition, not a measured property. The research would give the team evidence-based criteria for evaluating AI impact of future proposals. |

---

## Comparative Library Studies — Status Assessment

The `expressiveness/` folder contains six library comparison studies. These informed early proposals but vary in quality and coverage:

| Study | Quality | Coverage | Staleness Risk | Upgrade Needed |
|---|---|---|---|---|
| `xstate.md` | Good | Guards, transitions, hierarchical states, parallel regions, actions, context | Low — xstate v5 is stable | Minor: add choice/type comparison if xstate adds TypeScript-native enums in context |
| `fluent-validation.md` | Good | Property chains, conditional rules, rule sets, error reporting | Low | Minor: add comparison for field-level constraint patterns when #13 progresses |
| `zod-valibot.md` | Good | Schema declarations, refinements, discriminated unions, error paths | Low | Minor: add comparison for choice types when #25 progresses |
| `polly.md` | Moderate | Pipeline composition, named policies, DI integration | Low | No update needed — Polly's overlap with Precept is narrow (action pipeline only) |
| `linq.md` | Moderate | Method chains, conditional extension, deferred execution | Low | No update needed — LINQ's overlap with Precept is tangential |
| `fluent-assertions.md` | Moderate | Assertion chains, collect-all failure, custom assertions | Low | No update needed — FluentAssertions' overlap is narrow (assertion pattern only) |

**Missing from the library set:**
- **FEEL (DMN)** — the strongest external comparator for business-rule DSL expression design. Has ternary, string functions, numeric functions, date operations, range membership. Referenced in `expression-language-audit.md` §5 and `references/README.md` but no dedicated study exists.
- **Cedar (AWS)** — strongest counter-precedent for deliberate constraint. Omits division and most math for formal analyzability. Referenced repeatedly but no dedicated study.
- **Drools** — foundational rule engine with named rules, production systems, and forward chaining. Referenced in conditional-invariant-survey but no dedicated study.
- **OPA/Rego** — policy-as-code DSL with virtual documents and partial evaluation. Closest model for computed/derived values. No dedicated study.

---

## PLT Reference Documents — Status Assessment

| Reference | Quality | Scope | Staleness Risk | Upgrade Needed |
|---|---|---|---|---|
| `expression-evaluation.md` | Good | Many-sorted FOL, decidability, type narrowing | Low | Add function-call semantics when #16 progresses |
| `expression-compactness.md` | Good | Syntactic sugar, derived forms, desugaring | Low | Add error-resugaring when multi-event shorthand progresses |
| `constraint-composition.md` | Good | Predicate combinators, Boolean lattices, collect-all | Low | Add named-rule composition when #8 progresses |
| `state-machine-expressiveness.md` | Good | Statecharts, hierarchy, parallelism, CSP | Low | No update needed — deliberate exclusion is stable |
| `multi-event-shorthand.md` | Good | SFAs, CSP event sets, UML multi-event arcs | Low | Add implementation-focused section when proposal materializes |
| `conditional-invariant-survey.md` | Good | 10-system survey for conditional constraints | Low | No update needed |

**Missing from the reference set:**
- **Type system theory** — sum types, product types, subtyping, bounded polymorphism. Needed before type system expansion (#25-#29).
- **Function-call semantics** — formal evaluation of function calls in constrained expression languages. Needed before #16.
- **Null-safety theory** — nullable type systems, option types, null-coalescing semantics. Needed for cross-cutting nullable decisions.
- **Error recovery & resugaring** — how to preserve diagnostic quality through desugaring. Identified in `references/README.md` as a gap.

---

## Sample Pattern Implications

Recurring authoring patterns in `samples/` (21 files) that imply research domains even without current proposals:

| Pattern | Frequency | Implied Domain | Strongest Evidence |
|---|---|---|---|
| `set Field = Event.Arg` × N | 21/21 samples | Absorb shorthand (#11) | `travel-reimbursement`: 7 SET statements for one intake |
| Non-negative invariant boilerplate | 19/21 samples | Field-level constraints (#13) | `loan-application`: 3 of 5 invariants are pure `>= 0` |
| Guard expression duplication | 15/21 samples | Named rules (#8) | `loan-application`: 93-char eligibility guard |
| String fields without length validation | 21/21 samples | String `.length` (#10) | All 21 samples have unconstrained strings |
| Date-like fields as numbers | 5/21 samples | Date type (#26) | `library-book-checkout`: CurrentDay, DueDay as numbers |
| Money amounts as floats | 10/21 samples | Decimal type (#27) | `travel-reimbursement`: MileageRate = 0.67 |
| String fields as implicit enums | 6/21 samples | Choice type (#25) | `clinic-appointment`: ConfirmationCode = "CONFIRMED" |
| Derived values recomputed in multiple rows | 8/21 samples | Computed fields (#17) | `event-registration`: AmountDue formula repeated |
| Guard-pair duplication (when + else reject) | 21/21 samples | Conditional expressions (#9) or multi-event shorthand | 20-35% of all row-header count |
| Count-as-day offset arithmetic | 5/21 samples | Date type (#26) | `library-hold-request`: PickupExpiryDay as number |

---

## Priority Assessment

Domains ranked by research urgency — how badly we need the research to exist before proposals can advance:

| Priority | Domain | Reason |
|---|---|---|
| **P0 — Blocking** | Type System Expansion (#1) | Four open proposals (#25-#29) with no durable research doc. Proposal bodies contain good evidence but it's not in the research library. |
| **P0 — Blocking** | Field-Level Constraints (#4) | Prerequisite architecture for type-specific constraints. No research doc exists. |
| **P1 — High** | Named Rules (#3) | Open proposal #8. Research exists across multiple files but no consolidated, quality-bar doc. |
| **P1 — High** | Built-in Functions (#6) | Most architecturally invasive parser change in the proposal set. Needs architecture research before implementation. |
| **P1 — High** | Event Argument Ingestion (#8) | #1 verbosity smell with no research doc at all. |
| **P2 — Medium** | String Operations (#5) | Two open proposals (#10, #15). Audit coverage is adequate but not at quality bar. |
| **P2 — Medium** | Null Safety (#13) | Cross-cutting concern that touches every type expansion. No research exists. |
| **P2 — Medium** | Conditional & Value Expressions (#2) | Open proposal #9. Conditional logic strategy covers most ground but scope questions remain. |
| **P3 — Low** | Multi-Event Shorthand (#11) | No open proposal. Existing research is solid PLT reference. Implementation-focused research needed when proposal materializes. |
| **P3 — Low** | Data-Only Precepts (#10) | Open proposal #22. Existing research is adequate. Could be expanded. |
| **P3 — Low** | Constraint Violation (#16) | Design doc exists. Research needed at implementation time. |
| **P4 — Horizon** | Collection Quantifiers (#14) | Deferred per audit recommendation. Research when simpler gaps close. |
| **P4 — Horizon** | Set-Membership Tests (#15) | May be subsumed by choice types. Evaluate after #25. |
| **P4 — Horizon** | Temporal Assertions (#19) | Identified but high semantic cost. Research when demand arises. |
| **P4 — Horizon** | Cross-Entity References (#20) | Deliberately bounded. Research when domain systems require it. |
| **P4 — Horizon** | Schema Evolution (#21) | No research. Critical at production adoption, not during language design. |
| **P4 — Horizon** | AI Legibility Research (#22) | Empirical research when plugin usage data exists. |
| **Low** | Division Safety (#17) | Evaluator fix, minimal research needed. |
| **Low** | Keyword Surface (#12) | Well-covered. Minor reorganization. |
| **Low** | State Machine Advanced (#18) | Deliberately excluded. Serves as "why not" reference. |
| **Complete** | Computed Fields (#7) | Quality bar met. |
| **Complete** | Conditional Declarations (#9) | Nearly complete. Minor semantic-contracts addition. |

---

## Cross-References

- Issue map: `research/language/README.md`
- Expressiveness studies: `research/language/expressiveness/README.md`
- PLT references: `research/language/references/README.md`
- Quality bar: `research/language/expressiveness/computed-fields.md`
- Philosophy: `docs/philosophy.md`
- Language spec: `docs/PreceptLanguageDesign.md`
- Design docs: `docs/RulesDesign.md`, `docs/EditableFieldsDesign.md`, `docs/ConstraintViolationDesign.md`
- Open proposals: GitHub issues #8, #9, #10, #11, #13, #14, #15, #16, #17, #22, #25, #26, #27, #29, #31
