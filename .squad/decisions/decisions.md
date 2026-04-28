# Decision: README hero PNG fallback

- **Context:** GitHub does not render the styled inline HTML contract block in `README.md` as intended.
- **Decision:** Use `brand/readme-hero-dsl.png` as the GitHub-facing contract sample and keep a collapsed plain-text version immediately below for copyability.
- **Why:** The PNG preserves the intended branded syntax presentation on GitHub, while the collapsed source keeps the sample useful to humans and AI agents without turning the section back into a long raw block.
- **Files:** `README.md`, `brand/readme-hero-dsl.png`, `brand/readme-hero-dsl.precept`

---

## Decision

Treat `docs/HowWeGotHere.md` as a retrospective historical narrative, not as a live trunk-consolidation memo.

## Why

- Shane asked to remove the branch-history section as irrelevant.
- The "worth preserving" material read like an active recommendation set instead of a record of what endured.
- The unresolved/recommendation sections kept pulling the document back into pending-decision framing.

## Applied To

- `docs/HowWeGotHere.md`

---

# Readability Review: combined-design-v2.md (2026-07-17)

**Reviewer:** Elaine (UX Designer)
**Doc:** `docs/working/combined-design-v2.md`
**Verdict:** APPROVED-WITH-CONCERNS

## Top 3 Findings

1. **Parser section needs shape specificity.** The section explains the parser's *philosophy* (source-faithful, recovery-aware) but doesn't give an implementer enough to know what SyntaxTree nodes to define. Missing: error recovery node shape, concrete node inventory or shape sketch, and explicit contract for how malformed input is represented. A parser design doc author would need to invent these from scratch.

2. **Missing navigation guide.** A 486-line doc serving two audiences (human implementers and AI agents) needs a "How to read this document" paragraph after the status block. Three sentences: what §1–§3 cover (commitments and pipeline overview), what §4–§8 cover (per-stage contracts), what §9–§12 cover (runtime and integration). This is the single highest-ROI addition for both audiences.

3. **"How it serves the guarantee" paragraphs become formulaic.** Useful for Lexer through Graph Analyzer. By Proof Engine and Lowering, the pattern is predictable and the content is restating the opening sentence. Recommendation: fold the guarantee connection into the stage's opening paragraph for §8–§10 and drop the separate labeled paragraph.

## Genre Assessment

The rewrite succeeds. §1 opens with a problem statement and architectural commitment, not an inventory. Per-stage sections lead with design decisions. The philosophy-first framing is consistent throughout. This is a design document, not a reference manual.

## Decision

This doc is ready to serve as the architectural foundation for per-stage design docs (starting with the parser). The concerns above are improvements, not blockers — the parser concern is the most urgent because that's the immediate next use case.

---

# Design Review: combined-design-v2.md — Soundness, Completeness, Innovation

**Reviewer:** Frank (Lead Architect)
**Date:** 2026-06-03
**Document:** `docs/working/combined-design-v2.md`
**Context:** Only the Lexer is implemented. All other pipeline stages are stubs.

---

## VERDICT: APPROVED-WITH-CONCERNS

The document is architecturally sound and well-structured. It reads as a unified design explanation rooted in philosophy, not a defense of two separate systems. The pipeline is coherent, the artifact boundaries are clean, and the lowered executable model is concrete enough to implement against. The concerns below are real design gaps that will cost us if we hit them mid-implementation rather than addressing them now.

---

## Soundness Issues

1. **The proof strategy set is closed but its coverage boundary is unstated.** The doc lists four strategies (literal, modifier, guard-in-path, flow narrowing) and says "any obligation outside this set is unresolvable." But it never states what percentage of real-world proof obligations these four strategies can discharge. If most `ProofRequirement` instances in practice require cross-field reasoning (e.g., `ApprovedAmount <= RequestedAmount`), then the four strategies are a beautiful design that rejects most real programs. The doc should include a coverage analysis against the sample corpus — even an informal one — so implementers know whether the strategy set is right-sized or whether a fifth strategy (e.g., relational pair narrowing) is needed before v1.

2. **`Restore` bypasses access-mode but evaluates constraints — the interaction with computed fields is unspecified.** If persisted data includes stale computed-field values, does Restore recompute before constraint evaluation? The recomputation index is listed as a Restore input, but the evaluation order (recompute → validate vs. validate → recompute) is not specified. Getting this wrong means Restore either rejects valid persisted data or accepts invalid computed values.

3. **The `Create` without initial event path evaluates `always` + `in <initial>` — but default values may not satisfy `in <initial>` constraints.** The doc doesn't specify whether this is a compile-time guarantee (the proof engine should catch it) or a runtime domain outcome. The static-reasoning research (C3) says this is a known check, but the combined design doesn't thread it through the proof/fault chain. An author who writes `field X as number default 0` and `in Draft ensure X > 5` gets no compile-time warning in the current design — only a runtime `EventConstraintsFailed` on create. That violates the prevention promise.

4. **`ConstraintActivation` discriminant is described but not typed.** The doc says it "distinguishes whether a constraint binds to the current state, the source state, or the target state." But it doesn't specify whether this is an enum, a DU, or a tag on the descriptor. Given the catalog-driven architecture, this should be cataloged — it's language-surface knowledge that consumers need, not an implementation detail.

---

## Completeness Gaps

1. **No error recovery strategy for the parser.** The doc says `SyntaxTree` preserves "recovery shape for broken programs" and mentions "missing-node representation," but there is no recovery algorithm specified. Panic-mode? Synchronization tokens? The parser recovery strategy directly affects LS quality — a bad recovery model means completions and diagnostics degrade on every keystroke. This is a design decision, not an implementation detail, and it should be locked before the parser is built.

2. **No incremental compilation model.** The doc treats the pipeline as a single-shot transformation: source → tokens → tree → model → graph → proof → CompilationResult. But the language server needs incremental re-analysis on every keystroke. The doc should specify the invalidation boundary — does a keystroke re-lex the whole file? Re-parse? Re-typecheck? For a single-file DSL this may be "just re-run everything" and that's fine — but say so explicitly, with a size-ceiling argument for why that's acceptable (the 64KB source limit helps here).

3. **No serialization contract for `Version`.** The doc specifies `Restore` as the reconstitution path, but never specifies what the caller provides. What is the serialization shape of a `Version`? Is it `(stateName, fieldValues)`? `(stateDescriptor, slotArray)`? The host application needs a defined contract for what to persist and what to hand back to `Restore`. Without it, every host will invent its own serialization and we'll get impedance mismatches.

4. **No definition versioning or migration story.** When a `.precept` file changes (field added, state renamed, constraint tightened), what happens to persisted `Version` instances compiled against the old definition? `Restore` will reject them if they don't satisfy the new constraints. The doc should at least name this as a known gap and specify whether migration is in-scope or explicitly deferred.

5. **No observability hooks.** The doc specifies structured outcomes and inspections, but no tracing, logging, or metric emission points. For a production runtime, host applications need to observe: which events fired, which constraints failed, how long evaluation took, which proof strategies were used. These hooks shape the evaluator's internal architecture — bolting them on later means refactoring the evaluator.

---

## Innovation Opportunities

1. **The proof engine should guarantee initial-state satisfiability at compile time.** The research base (static-reasoning-expansion.md, C3/C4/C5) already describes per-field interval analysis. The combined design should commit to a concrete compile-time guarantee: *if default field values and initial-state constraints are both statically known, the proof engine verifies satisfiability and emits a diagnostic if no valid initial configuration exists.* This is unique among DSL runtimes — no validator, state machine library, or rules engine provides this. It's the proof engine's signature contribution and it's achievable with the bounded strategy set already designed.

2. **Precompute a "constraint influence map" during lowering for AI-native inspection.** Currently the inspection API tells you *what* constraints are active and *whether* they passed. It doesn't tell you *which fields drive which constraints* — the dependency graph exists in the `TypedModel` but is not lowered into an inspectable form. If lowering also produces a `ConstraintInfluenceMap` (constraint → contributing fields, with expression-text excerpts), then an AI agent can answer "why did this constraint fail?" and "which field change would fix it?" without reverse-engineering expressions. This is a structural differentiator for the MCP surface.

3. **The executable model should be a compiled decision table, not a tree walk.** The doc says lowering produces "lowered expression nodes and action plans" but doesn't specify the execution model. For Precept's small, closed expression language, the optimal model is a flat evaluation plan — precomputed slot references, operation opcodes, and result slots — not a recursive tree interpreter. Think of it as a register-based bytecode where "registers" are field slots. This makes evaluation predictable-time, cache-friendly, and trivially serializable for inspection. The doc should commit to "flat evaluation plan" as the executable model shape and explicitly reject tree-walking.

4. **Emit a machine-readable "contract digest" alongside `CompilationResult`.** A deterministic hash of the compiled definition's semantic content (fields, types, constraints, states, transitions — excluding whitespace and comments) would let host applications detect definition changes without diffing source text. Pair it with a structural diff API (`ContractDiff(old, new)` → added/removed/changed fields, states, constraints) and you have the foundation for the migration story (gap #4 above) and a production deployment safety net.

5. **The constraint evaluation matrix should surface "why not" explanations as structured data.** When `Fire` returns `Rejected` or `EventConstraintsFailed`, the outcome carries `ConstraintViolation` objects. But the doc doesn't specify whether violations carry *explanation depth* — just the failing expression text, or also the evaluated field values, the guard that scoped the constraint, and the specific sub-expression that failed. For AI legibility, violations should carry structured explanation: `{ constraint, expression, evaluatedValues: { field: value }, guardContext?, failingSubExpression? }`. This is cheap to compute during evaluation and transforms MCP from "it failed" to "it failed because X was 3 and the constraint requires X > 5."

---

## Right-Sizing Issues

1. **The `SyntaxTree` vs `TypedModel` anti-mirroring rules are over-specified for a doc at this level.** Four numbered rules about what `TypedModel` must not do, plus a seven-item "required inventory" — this is component-level design specification embedded in an architecture document. The architectural decision (they are separate artifacts with separate jobs) is correct and should stay. The implementation contract should move to a parser/type-checker-specific design doc that the implementer reads when building those stages.

2. **The five constraint-plan families and four activation indexes are correctly designed but could be simplified in the implementation.** The `from` and `to` families only activate during `Fire`. The `on` family only activates during `Fire`. The `in` family activates during `Update`, `Create`-without-event, and `Restore`. The `always` family activates everywhere. This means the evaluator really has two modes: "fire mode" (all five families) and "edit mode" (always + in). The doc could name these modes to simplify the mental model without losing the family distinction.

---

## Top 3 Recommended Changes Before This Doc Drives Per-Component Design

### 1. Add a proof coverage analysis against the sample corpus.

Run the four proof strategies against every `ProofRequirement` that would arise from the 20 sample files. Report how many obligations each strategy discharges and how many remain unresolvable. If coverage is below ~90%, design a fifth strategy before implementation begins. This is the highest-risk unknown in the document — the proof engine's value proposition depends on it.

### 2. Specify the parser error recovery strategy.

Lock one of: (a) panic-mode with synchronization at declaration keywords, (b) token-deletion/insertion with cost model, (c) "re-lex everything, re-parse everything" with the 64KB ceiling as the performance argument. The LS team cannot build completion/diagnostic features without knowing what the tree looks like on broken input.

### 3. Commit to a flat evaluation plan as the executable model.

Replace "lowered expression nodes and action plans" with a concrete specification: slot-addressed evaluation plans with operation opcodes, field-slot references, literal constants, and result slots. This prevents the implementation from defaulting to a recursive AST interpreter — which would be correct but would sacrifice the performance and inspectability properties that make Precept's runtime distinctive.

---

*This review is direct because the timing demands it. Addressing these three items now — before the parser, type checker, and evaluator are built — is nearly free. Addressing them after implementation begins is expensive. The architecture is sound. These are the gaps that would bite us.*

---

# Decision: Combined Design v2 Comprehensive Revision Pass

**By:** Frank
**Date:** 2026-07-17
**Status:** Applied

## Summary

Applied all team review feedback (Frank design review, George technical accuracy, Elaine readability) to `docs/working/combined-design-v2.md` in a single revision pass. Added Precept Innovations callouts to every major section. Added two new sections: §12 TextMate Grammar Generation and §13 MCP Integration.

## What Changed

### Review feedback applied (all three reviewers)
- Navigation guide ("How to read this document") after status block
- Parser: error recovery shape, node inventory, catalog-to-grammar mapping, anti-Roslyn guidance, ActionKind dual-use, parser/TypeChecker contract boundary
- TypeChecker: anti-pattern for per-construct check methods
- Proof engine: coverage boundary, flow narrowing clarification, initial-state satisfiability
- Compilation snapshot: no-incremental-compilation model, contract digest hash, definition versioning gap
- Lowering: fixed "catalogs not re-read" claim, descriptor shapes, flat evaluation plan, anti-pattern warnings, ConstraintActivation cataloging, Version serialization contract
- Runtime: Restore recomputation order, structured "why not" violations

### New content
- **Precept Innovations callouts** in every major section (§2–§14), 2–4 bullets each
- **§12 TextMate grammar generation** — catalog contributions table, anti-pattern, zero-drift guarantee
- **§13 MCP integration** — tool inventory, thin-wrapper principle, AI-first design, catalog-derived vocabulary

### Structural changes
- Former §12 (LS integration) renumbered to §14
- Doc grew from 486 to 694 lines
- Formulaic guarantee paragraphs folded into stage openings for §8–§10

## Decisions Locked
- Parser error recovery: construct-level panic mode with `MissingNode` + `SkippedTokens`
- Expression evaluation: flat slot-addressed evaluation plans, tree-walk explicitly rejected
- Incremental compilation: "re-run everything" is the intended model (64KB ceiling)
- Definition versioning: known gap, deferred beyond v1
- `ConstraintActivation`: should be cataloged (language-surface knowledge)

---

## Proposal Summary

Invert D3: make `write` the universal default for (field, state) pairs. Add a `readonly` modifier on field declarations to permanently lock fields from ever being written in any state. Eliminate root-level `write` declarations entirely.

---

## Question 1: Does inverting D3 weaken the conservative guarantee?

**Yes. Fundamentally.**

D3 as specified (§2.2 Access Mode, composition rule 1) states: "D3 is the universal per-pair baseline — undeclared (field, state) pairs default to `read`." The design principle behind this is explicit: "Authors declare only exceptions to readonly — `write` opens a field for editing in that state."

This is a **closed-world access model**. Nothing is writable unless explicitly opened. The omission failure mode is safe: if an author forgets to declare a `write`, the field is locked in that state. The author must take a deliberate action — writing the `write` keyword — to open the attack surface.

The proposal inverts this to an **open-world access model**. Everything is writable unless explicitly restricted. The omission failure mode is unsafe: if an author forgets to mark a field `readonly`, it is exposed in every state to direct mutation via `Update`.

This is the firewall-rule principle. Good security defaults to DENY; you add ALLOW exceptions. D3 defaults to DENY (read-only) and authors add ALLOW exceptions (`write`). The proposal defaults to ALLOW (writable) and authors add DENY exceptions (`readonly`). In a **governance** language — one whose entire identity is built on "invalid configurations are structurally impossible" (Principle 1: Prevention, not detection) — the conservative default is non-negotiable.

### Corpus evidence

The sample set confirms that the conservative default reflects real domain proportions:

- **Stateful precepts with zero write declarations:** `hiring-pipeline`, `loan-application` (except one guarded write), `apartment-rental-application`, `restaurant-waitlist`, `library-hold-request`, `travel-reimbursement`, `warranty-repair-request`. These precepts rely entirely on event-driven mutation. The D3 default silently protects all fields from direct editing. Under the proposal, all those fields would be writable by default — an enormous, invisible expansion of the attack surface.

- **Stateful precepts with 1–2 write declarations:** `crosswalk-signal` (1), `clinic-appointment-scheduling` (1), `building-access-badge-request` (1), `insurance-claim` (2), `maintenance-work-order` (1), `refund-request` (1), `subscription-cancellation-retention` (1), `event-registration` (2), `it-helpdesk-ticket` (1), `utility-outage-report` (1), `vehicle-service-appointment` (1). The typical pattern is opening 1–3 fields in 1–2 states. The remaining (field, state) pairs — the overwhelming majority — stay protected by D3.

- **Stateless precepts:** `fee-schedule` (3 of 5 writable), `payment-method` (2 of 6 writable), `computed-tax-net` (2 of 4 writable), `invoice-line-item` (4 of N writable). Even in stateless precepts, the typical pattern is that some fields are intentionally locked. `customer-profile` is the only sample using `write all`.

The verbosity cost of the current model is 1–2 lines per precept. The safety cost of the proposed model is an invisible, unbounded expansion of the mutation surface whenever an author omits a `readonly` marker.

### Principle citations

- **Principle 1 (Prevention, not detection):** The proposal turns field-level access control from structurally prevented to structurally permitted. An author who omits `readonly` on a field that should be locked has created a governance gap. Under D3, the same omission creates no gap.
- **Principle 4 (Full inspectability):** Auditability is stronger when the declared surface is the exception set (small, explicit) rather than the restriction set (requiring mental subtraction from a universal default). "What can a user directly edit here?" is answered by scanning for `write` keywords under D3. Under the proposal, the answer is "everything, minus what's marked `readonly`" — which requires reading every field declaration to check for the absence of a modifier.

---

## Question 2: Does `readonly` on a field cleanly complement or conflict with computed fields?

**It creates a semantic inconsistency.**

Computed fields (`field Tax as number -> Subtotal * TaxRate`) are already implicitly readonly. The spec enforces this structurally — `ComputedFieldNotWritable` is a type-checker diagnostic (§3.8). A computed field's readonly nature arises from its derivation: it has an expression, so it cannot be directly assigned. This is not a modifier; it is a structural consequence of the field's kind.

Under the proposal, the access defaults would be:

| Field kind | Proposed default | Actual access |
|---|---|---|
| Stored field (no `readonly`) | write | write |
| Stored field (with `readonly`) | write → overridden to read | read |
| Computed field | write (in theory) | read (structurally) |

The computed field's access mode would be inconsistent with the declared default. A stored field and a computed field would have different effective defaults despite the language claiming "write is the default." The author would need to understand that computed fields are a hidden exception to the stated default — undermining Principle 4 (inspectability) and Principle 5 (keyword-anchored readability).

Under D3, the picture is consistent:

| Field kind | D3 default | Actual access |
|---|---|---|
| Stored field (no `write`) | read | read |
| Stored field (with `write`) | read → overridden to write | write |
| Computed field | read | read |

All fields default to read. Computed fields are naturally aligned with the default. Stored fields that need to be writable are explicitly opened. There is no inconsistency to explain.

Adding `readonly` as a modifier also creates a redundancy question: should `readonly` on a computed field be a warning (redundant modifier), an error (modifier conflicts with structural readonly), or silently accepted? Each answer has downsides. Under D3, the question never arises — there is no `readonly` keyword, and computed fields simply match the default.

---

## Question 3: Does "write default, restrict per state" change the auditability story?

**Yes. It weakens it materially.**

In a stateful precept under D3, the audit question "which fields can a user directly edit in state S?" is answered by reading the `in S write` declarations. If there are none, the answer is "nothing — all mutation happens through events." This is a **closed-world audit**: the write declarations ARE the complete answer.

Under the proposal, the same question is answered by: "every field, minus those marked `readonly` on the field declaration, minus those restricted by `in S read` or `in S omit` declarations." This is an **open-world audit** requiring cross-referencing the field declarations (for `readonly` markers), the state-scoped access declarations (for per-state restrictions), and computing the difference. The mental model is subtraction from a universal set rather than enumeration of an explicit set.

For a governance language — one where the point is to make the access contract **explicit and visible** — the open-world model is the wrong posture. The current model's strength is that the write declarations positively assert what is open. The proposed model requires the reader to infer what is open from what is not restricted.

This matters especially for AI consumers. Precept's Principle 3 (deterministic semantics) and Principle 5 (keyword-anchored readability) are designed partly for AI legibility. A closed-world access model is easier for AI agents to reason about: "find all `write` declarations" is a simple, complete query. "Find all fields, subtract `readonly` fields, subtract per-state restrictions" is a compositional query with a higher error surface.

---

## Additional Concerns

### The `readonly` keyword itself is misaligned

`readonly` is a **programming-language concept** from C#, Java, Rust, TypeScript. It carries connotations of compile-time immutability, final binding, memory-model guarantees. Precept's access model is about **editability** — which fields can the host application directly mutate via the `Update` operation. These are different concepts. A field that is `read` in a given state is not immutable — events can still `set` it during transitions. It is merely not directly editable by the external caller. Introducing `readonly` would import programming-language semantics into a domain-configuration language, violating the philosophy's positioning of Precept as a language for domain experts and business analysts, not software developers (§ Who authors a precept in philosophy.md).

### Root-level `write` elimination is a false economy

The proposal motivates itself partly by eliminating root-level `write` declarations. But the current model already makes these declarations do useful work:

- `write BaseFee, DiscountPercent, MinimumCharge` in `fee-schedule` — the `write` keyword positively documents the author's intent. Reading it, you know immediately which fields are editable. The comment above it ("Only pricing levers are editable; TaxRate and CurrencyCode are locked") is restating what the `write` declaration already says.
- `write all` in `customer-profile` — a deliberate, visible assertion that everything is open. Under the proposal, this becomes the invisible default, and the author's deliberate intent vanishes from the surface.

The `write` keyword carries semantic weight as a positive assertion. Replacing it with the absence of `readonly` loses that signal.

---

## Verdict: **Reject**

The proposal inverts Precept's conservative access posture from closed-world (safe by default, explicitly opened) to open-world (exposed by default, explicitly restricted). This:

1. **Weakens the omission failure mode** from safe (field locked) to unsafe (field exposed).
2. **Creates an access-default inconsistency** between stored and computed fields.
3. **Degrades auditability** from positive enumeration to negative subtraction.
4. **Imports programming-language semantics** (`readonly`) into a domain-configuration language.
5. **Eliminates the positive-assertion value** of `write` declarations for marginal verbosity savings (1–2 lines per precept).

D3 is philosophically correct, empirically well-calibrated to real domain proportions, and consistent with the governance identity. It should not be inverted.

### What would need to change for reconsideration

If the underlying concern is verbosity in stateless precepts that happen to have mostly-writable fields, there are narrower solutions that preserve D3:

- A `write all` shorthand already exists and handles the fully-open case.
- If a `write all except F1, F2` syntax were needed, it could be evaluated without inverting the default. The exception list would still be a positive declaration against a positively-declared baseline.

Neither of these requires abandoning the conservative default. The proposal conflates "reduce boilerplate" with "invert the safety model." Only the former is a real problem; the latter is the wrong solution.

---

# George — Technical Review: combined-design-v2.md

**Date:** 2026-04-28
**Verdict:** APPROVED-WITH-CONCERNS

## Summary

The doc is architecturally sound, internally consistent with the catalog system, and faithful to the philosophy. The pipeline stages, artifact boundaries, lowering contract, and runtime operation surfaces are accurately described. The constraint evaluation matrix and activation indexes are precise and correct. The proof/fault chain is well-specified.

However, the doc has **critical implementation-readiness gaps for the Parser** (our immediate next step) and **Roslyn-bias risks** that would cause an implementer to default to conventional compiler patterns in at least four places. These must be patched before the parser design doc is written.

## Top 3 Recommended Changes Before Parser Design

1. **Add a concrete SyntaxTree node inventory.** The doc says what `SyntaxTree` is for but never enumerates its node types. An implementer has no guidance on whether to produce `PreceptSyntax` as a single flat root with child declarations, a Roslyn-style red/green tree, or something else. The Constructs catalog defines 11 `ConstructKind` values with typed slots — the doc should state explicitly that the parser produces one syntax node type per `ConstructKind`, with child nodes corresponding to `ConstructSlot` entries. This is the single most critical gap.

2. **Specify the parser/TypeChecker contract boundary.** The doc says the parser stamps `ConstructKind`, `ActionKind`, `OperatorKind`, `TypeKind` on `TypeRef` nodes, and `ModifierKind` — but doesn't say what the parser guarantees to the TypeChecker and what it doesn't. Does the parser guarantee structurally well-formed declarations (every required slot filled, or represented as a missing-node)? Does the parser guarantee that all identifiers in keyword positions actually resolved to catalog keywords? What can the TypeChecker assume without re-checking? This contract is the #1 source of bugs in multi-pass compilers when it's implicit.

3. **Add an explicit anti-Roslyn section for the parser.** The doc should state: (a) Precept's grammar is line-oriented and flat — there is no block nesting, no brace-delimited scopes, no expression statements; (b) error recovery is construct-level — if a line doesn't parse, emit a diagnostic and skip to the next newline-anchored declaration keyword; (c) the parser does NOT need a general-purpose recursive-descent expression parser for the full language — expressions only appear in specific slots (guards, action RHS, ensure clauses, computed fields, because clauses); (d) operator precedence comes from `Operators.GetMeta()`, not a hardcoded precedence table.

## Technical Accuracy Issues

1. **§5 Parser: "Catalog entry" claim is underspecified.** The doc says "The parser stamps syntax-level identities as soon as syntax alone can know them: construct kind, anchor keyword, action keyword, operator token, literal segment form." This is accurate in principle but dangerous in practice. The parser CAN stamp `ConstructKind` because construct dispatch is keyword-anchored (`field`, `state`, `event`, `rule`, `from`, `in`, `to`, `on`). The parser CAN stamp `OperatorKind` because operators are token-level. But `ActionKind` is ambiguous in syntax — `set` is both an action keyword (TokenCategory.Action) and a type keyword (TokenCategory.Type). The doc should note that `ActionKind` stamping requires the parser to use position context (after `->` = action; after `as`/`of` = type), and this disambiguation is a parser responsibility, not a catalog lookup.

2. **§6 TypeChecker: "projection, not in-place annotation" is correct but needs the mechanism.** The doc says `TypedModel` is a projection of `SyntaxTree`, which is architecturally right. But it doesn't say whether the TypeChecker walks the `SyntaxTree` nodes and produces parallel `TypedModel` nodes (Roslyn's approach), or whether it reads the `SyntaxTree` and populates semantic tables/inventories (more like a symbol-table-driven approach). For Precept's flat, declaration-oriented grammar, the latter is correct — the TypeChecker should build declaration registries and expression bindings, not a parallel tree. The doc should say this explicitly to prevent a Roslyn-pattern default.

3. **§8 ProofEngine: Strategy descriptions are accurate but "straightforward flow narrowing" is vague.** Literal proof, modifier proof, and guard-in-path proof are well-scoped. "Straightforward flow narrowing" could mean anything from SSA-based type narrowing to full dataflow analysis. In Precept's context, this should be scoped to: "if a guard clause in the same transition row establishes a constraint on a field, that constraint is available as evidence for proof obligations on expressions within that row's action chain." The doc should narrow this.

4. **§10 Lowering: "Catalogs are not re-read here" is a design assertion, not an implementation constraint.** Lowering receives `TypedModel` and `GraphResult` which already carry catalog-resolved identities. But the lowering step may still need to read catalog metadata for things like default-value computation, constraint text extraction, or fault-site descriptor construction. The doc should clarify: lowering reads catalog metadata transitively through already-resolved model identities, but does not perform fresh catalog lookups for classification purposes.

5. **Descriptor types are described by name but not by shape.** The doc references `FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, `ArgDescriptor`, `ConstraintDescriptor` — and `ConstraintDescriptor` actually exists in code with a concrete shape. But the other four are named without any shape specification. The implementation action items (§Appendix A) correctly flag this, but the main doc body treats them as if they're defined. An implementer reading §10–§11 would assume these types exist.

## Implementation Readiness Gaps

### Parser (immediate next step)
- **No node inventory.** The doc never says what `PreceptSyntax Root` contains. Is it a list of declaration nodes? What are the node types? How do expression nodes nest? This is the gap that will cause the most re-derivation.
- **No error recovery strategy.** The doc says "recovery shape" and "missing-node representation" but doesn't specify the pattern. Does the parser use marker nodes for missing required slots? Does it use `Option<T>` for optional slots? Does it skip-to-newline on error?
- **No expression grammar specification.** Expressions appear in guards, action RHS values, ensure clauses, computed fields, if/then/else, and because clauses. The doc doesn't specify the expression grammar or how it relates to the Operators/Operations catalogs. The parser needs to know: what are the expression forms? (binary, unary, literal, field-ref, event-arg-ref, function-call, if-then-else, is-set/is-not-set, contains, member-access). This is a significant omission.
- **No statement about whether Precept's grammar is LL(1), LL(k), or requires backtracking.** Given the line-oriented, keyword-anchored design, it should be LL(1) with single-token lookahead in most positions, but this should be stated.
- **Catalog lookup timing for the parser is unclear.** The doc says the parser consults `Constructs`, `Tokens`, `Operators`, `Diagnostics`. It should specify: the parser uses `Tokens.Keywords` (already built into the lexer) for keyword recognition; it uses `Constructs.GetMeta()` for construct shape validation; it uses `Operators.GetMeta()` for precedence during expression parsing. When does each lookup happen?

### TypeChecker
- Adequate for a per-component design doc, but needs the "semantic tables, not parallel tree" clarification (issue #2 above).
- The typed action family (§6) is well-specified — this is a strong point.
- Missing: how does the TypeChecker handle multi-name declarations (`field a, b, c as number`)? Does it expand them into separate symbols? This matters for the TypedModel shape.

### GraphAnalyzer
- Adequate. The `GraphResult` facts list is concrete enough. The key risk is an implementer building a general graph library instead of the four specific fact categories listed.
- Missing: does the graph operate over `StateDescriptor` and `EventDescriptor` identities from the TypedModel, or does it build its own node identities? The doc says it consumes `TypedModel` — this should be explicit.

### ProofEngine
- The bounded strategy set is the right design. The proof/fault chain is well-specified.
- Missing: what does "unresolvable by the compiler" mean for the author? Is it always a hard error? Or can the author annotate intent? (I suspect hard error is correct for Precept's philosophy — but the doc should state it.)

### Evaluator/Runtime
- The constraint evaluation matrix is precise and correct. The operation-facing plan selection table is accurate.
- Missing: the doc never describes the expression evaluation model. Are lowered expressions interpreted (tree-walk)? Compiled to IL? Compiled to delegates? For Precept's scale, tree-walk interpretation is the right answer — but the doc should say so to prevent an implementer from building a JIT compiler.

## Roslyn-Bias Risks

1. **§5 Parser — An implementer will build a Roslyn-style recursive-descent parser with red/green trees.** The doc says nothing about Precept's grammar being flat and line-oriented. Roslyn's design is for a language with deep nesting (classes → methods → statements → expressions). Precept has NO nesting beyond expression-within-declaration. An implementer needs to know: one pass, keyword-dispatched, line-scoped declarations, with expression sub-parsing only in specific slots. The red/green tree pattern is massive overkill.

2. **§6 TypeChecker — An implementer will write per-construct-kind check methods.** The doc says the TypeChecker resolves `OperationKind`, `FunctionKind`, etc. from catalogs, but doesn't explicitly say: "the type checker should NOT have a `CheckFieldDeclaration()`, `CheckTransitionRow()`, etc. method per construct kind — it should have generic resolution passes that read construct metadata." In practice, some per-construct logic is unavoidable (field declarations and transition rows have genuinely different type-checking needs), but the doc should draw the line: catalog-resolvable checks are generic; construct-specific structural validation is the only place for per-kind methods.

3. **§8 ProofEngine — An implementer will reach for Z3 or an SMT solver.** The doc's "four strategies only, no general SMT solver" is the right call and IS stated. But the strategies are described abstractly enough that an implementer might still build a general obligation-discharge framework. The doc should add: "Each strategy is a simple predicate function, not a solver. Literal proof checks whether the operand is a compile-time constant. Modifier proof checks whether the field carries a relevant modifier. Guard-in-path proof checks whether the enclosing guard expression subsumes the obligation. Flow narrowing checks type state through the immediately enclosing control path."

4. **§10 Lowering — An implementer will serialize the TypedModel.** The doc says lowering "selectively transforms" — but an implementer's default is to map TypedModel types 1:1 to runtime types. The doc should state: the runtime model's shape is organized for execution, not for semantic analysis. Constraint plans are grouped by activation anchor, not by source declaration order. Action plans are grouped by transition row, not by field. The runtime model is a dispatch-optimized index, not a renamed analysis model.

5. **Expression parsing — An implementer will build a Pratt parser or ANTLR grammar.** Neither is stated as required or prohibited. For Precept's expression grammar (binary ops with catalog-defined precedence, unary negation/not, field refs, function calls, if/then/else, member access, is-set, contains), a simple precedence-climbing parser reading precedence from `Operators.GetMeta()` is the right tool. The doc should name this.

## Right-Sizing Issues

1. **The `TypedModel` inventory (§5) is comprehensive but may be over-specified for initial implementation.** "Dependency facts — computed-field dependencies, arg dependencies, referenced-field sets, and semantic edge data" is real and needed, but it's a second-pass concern. The initial TypedModel needs declaration symbols, reference bindings, and typed expressions. Dependency extraction can be a sub-pass within the TypeChecker or a separate lightweight analysis. The doc should distinguish "required for correctness" from "required for optimization."

2. **The proof engine's four strategies are right-sized for the current language.** No over-generalization detected. This is a strong point.

3. **The constraint activation indexes (§10) are correctly scoped.** Four indexes for five anchor families, with the activation discriminant — this is exactly what the evaluator needs, no more.

4. **The three-tier constraint query contract is well-designed but the doc should note that Tier 2 (ApplicableConstraints) is a runtime convenience, not an evaluation necessity.** The evaluator always uses the activation indexes directly. Tier 2 is for API consumers who want to know what constraints are relevant before firing an event.

---

### 2026-04-27: User directive — greenfield parser, no consumer concerns
**By:** Shane (via Copilot)
**What:** The parser is being written from scratch. The stub throws NotImplementedException. There are zero existing consumers of the parser output. Concerns about "breaking existing consumers" or "sequenced PR plans to protect consumers" are invalid in this context. Read the actual code before raising compatibility concerns.
**Why:** User correction — George raised a backward-compatibility concern that does not apply to a not-yet-implemented component.

### 2026-04-27: Design question — calculated field operator
**By:** Shane (via Copilot)
**What:** Would switching calculated fields from -> to <- help the catalog-driven parser design? Specifically: does having -> serve dual duty (transition arrow + computed field assignment) create token ambiguity that complicates slot catalog design? Would <- as a dedicated "computed from" operator simplify things?
**Why:** Shane raised this mid-design-iteration — fold into the parser design loop.

---

### Read the docs — language vision and language design
**By:** Shane (via Copilot)
**What:** Before producing any design output, ALL agents MUST read the relevant documentation — especially language vision and language design docs. Do not design in a vacuum. Do not assume. Read first.
**Why:** Agents have been designing without grounding in the existing language vision and language design documentation. This produces proposals that may contradict or ignore already-decided direction.
**Specific docs to read (minimum):**
- docs/philosophy.md
- docs/language/precept-language-vision.md
- docs/language/precept-language-spec.md
- docs/language/catalog-system.md
- Any other docs/language/* files
- Sample files in samples/ to see actual syntax in use
**Rule:** If a design decision contradicts an existing doc, call that tension out explicitly rather than silently overriding it.

---

### Roslyn source generators for test generation
**By:** Shane (via Copilot)
**What:** Agents working on the catalog-driven parser design MUST consider Roslyn source generators as part of the solution — specifically for test generation. If the catalog describes constructs, slots, and grammar, a source generator could emit test scaffolding (or even test cases themselves) directly from catalog metadata, keeping tests in sync with language surface changes automatically.
**Why:** A truly catalog-driven design means the catalog is the single source of truth — test coverage should derive from it, not be hand-maintained alongside it. Source generators close that loop.
**Questions to address in design:**
- Could a Roslyn generator read ConstructCatalog/ConstructMeta and emit parser test stubs?
- Could it emit round-trip parse tests (text -> AST -> text) per construct automatically?
- How does this interact with the generic AST proposal — if AST nodes are generic/generated, do tests follow?
- What is the boundary between generated test scaffolding and hand-authored test logic?

---

# Calculated Field Arrow Direction: `<-` vs `->` Analysis

**Author:** Frank (Lead/Architect & Language Designer)
**Date:** 2026-04-27
**Status:** VERDICT — REJECT
**Requested by:** Shane

---

## 1. Current State

### How `->` is used today

The `->` token (`TokenKind.Arrow`) serves **two distinct grammatical roles** in the Precept DSL:

**Role A — Computed field expression introducer:**
```
field Tax as number nonnegative -> Subtotal * TaxRate
field Net as number positive -> Subtotal - Tax
field LineTotal as number -> TaxableAmount + TaxAmount nonnegative
```

Grammar production (spec §2.2, line 576):
```
field Identifier ("," Identifier)* as TypeRef FieldModifier* ("->" Expr)?
```

The `->` appears at the end of a field declaration, after modifiers, to introduce a computed expression. The field is read-only by contract: no `set`, no `edit`, no `writable`. The arrow says "this field's value is derived from this expression."

**Role B — Action chain / outcome separator:**
```
from Draft on Submit
    -> set ApplicantName = Submit.Applicant
    -> set RequestedAmount = Submit.Amount
    -> transition Approved
```

Grammar productions (spec §2.2, lines 626–628, 656–658, 665–667):
```
from StateTarget on Identifier ("when" BoolExpr)?
    ("->" ActionStatement)*
    "->" Outcome
```

The `->` here introduces each step in a transition pipeline — actions and the final outcome. The parser loops consuming `-> ActionKeyword` pairs, breaking when the token after `->` is an outcome keyword.

**Role C — State action (entry/exit hook):**
```
to Active -> set LastLogin = now()
from Expired -> clear Cache
```

The `->` introduces the action chain after a state target in `to`/`from` scoped constructs.

### How the lexer handles it

The lexer is catalog-driven. `->` lives in the `Tokens` catalog as:

- **TokenKind:** `Arrow` (line 145 of `TokenKind.cs`)
- **Text:** `"->"` 
- **Category:** `TokenCategory.Structural` (not `Operator` — this is a deliberate classification; `Cat_Str`, line 328 of `Tokens.cs`)
- **TextMateScope:** `keyword.operator.arrow.precept`
- **SemanticTokenType:** `operator`

The lexer resolves `->` through the `TwoCharOperators` frozen dictionary (line 411 of `Tokens.cs`). The scan order is: try two-char operators first (`->` before `-`), then fall back to single-char operators (`-` as `Minus`). This is the maximal-munch guarantee documented in spec §1.5, line 209:

> `->` before `-`

The `TwoCharOperators` table is built generically from `Tokens.All` entries with length-2 text and `Operator` or `Structural` categories. The lexer's `TryScanOperator()` method (Lexer.cs lines 733–757) does a single `TwoCharOperatorStarters.Contains(c)` guard, then a `TwoCharOperators.TryGetValue((c, PeekNext), ...)` lookup. No special-case code for `->`.

### What complexity exists

**Disambiguation cost: zero at the lexer level.** The lexer emits `Arrow` tokens identically regardless of context. All disambiguation is in the parser:

- In a `field` declaration: `->` after modifiers means "computed expression follows."
- In a transition row: `->` at line start means "action or outcome follows."
- In a state action: `->` after state target means "action chain follows."

The parser knows which role `->` plays purely from its position in the grammar production — not from any lookahead past the arrow itself. The token after `->` tells the parser whether it's an action keyword (continue loop), outcome keyword (break loop), or expression start (computed field). This is straightforward recursive descent: the parser is already inside the right production when it encounters `->`.

**There is no ambiguity.** The two roles occupy different grammar productions that are entered through different leading tokens (`field` vs `from`/`to`/`on`). The parser never faces a choice between "is this `->` a computed expression arrow or an action chain arrow?" because by the time it sees `->`, it already knows which production it's in.

---

## 2. The `<-` Proposal

The proposal: use `<-` (left-pointing arrow) for computed field expressions instead of `->` (right-pointing arrow). The action chain would retain `->`.

### What would change

#### Lexer impact

A new token kind `LeftArrow` (or `ComputedArrow`) would be needed:

- **TokenKind.cs:** Add `LeftArrow` to the `Operators` section (line ~145).
- **Tokens.cs GetMeta:** Add a new entry: `TokenKind.LeftArrow => new(kind, "<-", Cat_Str, "Computed field expression", ...)`.
- **TwoCharOperators table:** Automatically picks up `('<', '-')` from the new catalog entry.

**Critical conflict: `<-` collides with `<` followed by `-` (unary negation).** The `TwoCharOperatorStarters` set already contains `<` (because of `<=`). Adding `<-` means the lexer would try to match `('<', '-')` as a `LeftArrow` token. But consider this existing legal expression:

```
rule Score < -5 because "..."
```

The lexer would see `<` followed by `-` and greedily consume `<-` as a `LeftArrow` token. This **breaks all existing programs** that use `< -` (less-than, followed by negative number or negation). The maximal-munch rule that currently works cleanly would now create a genuine ambiguity that requires context-sensitive lexing.

Possible mitigations:
1. **Require whitespace between `<` and `-` when they're separate tokens.** This changes the lexer from context-free to whitespace-sensitive in operator scanning. The current lexer has zero whitespace sensitivity in operator scanning — this would be a first.
2. **Don't use the maximal-munch table; add special-case code.** This breaks the catalog-driven scan model. Every other two-char operator goes through the generic `TwoCharOperators` lookup. `<-` would need a carve-out.
3. **Abandon `<-` as a two-char operator and parse it as two tokens (`<` + `-`).** This means the parser must compose two separate tokens into a computed-arrow concept. That's a regression in token-level clarity.

None of these are clean. The current system has **zero special-case code** in operator scanning.

#### Parser impact

If the lexer successfully emits `LeftArrow` tokens:

- Field declaration production changes from `("->" Expr)?` to `("<-" Expr)?`.
- The parser checks for `TokenKind.LeftArrow` instead of `TokenKind.Arrow` when parsing field tails.
- Action chains continue checking for `TokenKind.Arrow`.

The parse grammar change is trivial — a single token kind swap. No disambiguation change, because `->` and `<-` never competed in the same parse context anyway.

#### Type checker impact

None. The type checker operates on the AST, not tokens. Whether the tree says "computed expression" regardless of which arrow introduced it, the semantic analysis is identical.

#### Catalog impact

- `TokenKind.cs`: Add `LeftArrow` (1 enum member, update count).
- `Tokens.cs`: Add `GetMeta` entry for `LeftArrow`.
- The `TwoCharOperators` table auto-derives from `All`.
- The `Operators` catalog is **not affected** — `->` is not an expression-level operator (it's `Structural` category), and `<-` wouldn't be either.
- Spec §1.1 operator table: add `LeftArrow` / `<-` row.
- Spec §1.5 scan priority: add `<-` scan rule and document the `< -` conflict.

#### Grammar generation impact

The TextMate grammar generator reads catalog metadata and emits `tmLanguage.json`. A new `LeftArrow` token with `keyword.operator.arrow.precept` scope would be auto-picked up. Completions and hover would derive from the catalog entry. Impact is minimal — one new derived entry — **assuming the lexer conflict is solved.**

---

## 3. Compiler Simplification Assessment

### Does `<-` reduce parser lookahead requirements?

**No.** The current `->` requires zero lookahead to disambiguate between its two roles. The parser is already inside the correct production (field declaration vs. transition row) before encountering `->`. There is no lookahead cost to reduce.

### Does it eliminate any ambiguity that `->` currently creates?

**No.** There is no ambiguity to eliminate. `->` is unambiguous in both parse contexts because the contexts are entered through different leading tokens (`field` vs `from`/`to`/`on`). The parser never needs to decide "which kind of arrow is this?"

### Does it make the parse grammar more regular or less regular?

**Less regular.** Currently `->` is the universal "pipeline step" glyph — it means "what follows derives from / is produced by what precedes." Using `<-` for one role and `->` for the other introduces a directional split that the grammar must track. The parser now has two arrow token kinds where one sufficed.

More concretely: the scan-priority rule becomes harder. The current rule is clean — `->` before `-`. Adding `<-` requires `<-` before `<` and a resolution for the `<` + `-` collision. The scan priority table gains a new conflict pair.

### Does it affect operator overloading or multi-use disambiguation?

**Yes — negatively.** The `<` character is currently a starter for three tokens: `<=`, `<`, and (hypothetically) `<-`. The `-` character is currently a starter for two tokens: `->` and `-`. With `<-`, both characters become starters for three tokens each, and the `<-` vs `< -` collision is a genuine new ambiguity. The current system has **zero** collisions between its two-char operators and their single-char prefixes in isolation.

### Net assessment

**`<-` adds complexity.** It introduces a lexer conflict that doesn't exist today, requires either whitespace sensitivity or special-case scanning to resolve, and provides zero disambiguation benefit at the parser level because the parser never had an ambiguity to begin with.

---

## 4. Language Surface Assessment

### Does `<-` read better or worse than `->`?

Compare the two:

```
# Current (->)
field Tax as number nonnegative -> Subtotal * TaxRate
field Net as number positive -> Subtotal - Tax

# Proposed (<-)
field Tax as number nonnegative <- Subtotal * TaxRate
field Net as number positive <- Subtotal - Tax
```

Reading left to right: `field Tax as number nonnegative -> Subtotal * TaxRate` reads as "field Tax, a nonnegative number, **derived as** Subtotal times TaxRate." The arrow flows in the reading direction. The declaration on the left, the derivation on the right, and the arrow connects them in reading order.

With `<-`: "field Tax, a nonnegative number, **receives from** Subtotal times TaxRate." The arrow points backwards — from right to left, against reading direction. The semantic is "the expression on the right flows into the name on the left," which is assignment semantics (like Erlang's `X = 5` or R's `x <- 5`).

This is a philosophical misfit. Precept's `->` isn't assignment — it's derivation declaration. The field doesn't *receive* a value; the field *is defined as* the expression. The right-pointing arrow says "this is what you get" — a definition, not an imperative assignment. The left-pointing arrow imports imperative assignment semantics from general-purpose programming languages, which is precisely the direction Precept's philosophy says not to go.

### Philosophy filter

| Criterion | `->` | `<-` |
|-----------|------|------|
| Domain integrity vs. enforcement-later | Neutral | Neutral |
| Deterministic and inspectable | ✓ Both | ✓ Both |
| Keyword-anchored, flat statements | ✓ Preserved | ✓ Preserved |
| First-match routing / collect-all validation | N/A | N/A |
| AI legibility | ✓ Consistent arrow glyph across all pipeline contexts | ✗ Two arrows, need context to know which is which |
| Configuration vs. scripting | ✓ Declarative derivation feel | ✗ Assignment feel (imperative) |
| Power without hidden behavior | ✓ Same glyph for "this step produces" everywhere | ✗ Different glyph for same "produces" concept in field vs. transition |

**AI legibility specifically:** LLMs reading Precept files benefit from having one universal "produces" operator. A single arrow glyph in two grammatical contexts (field derivation and action pipeline) is less cognitive surface than two glyphs that must be mapped to their respective contexts. Models that have seen `->` in action chains will correctly predict it in computed fields. Introducing `<-` creates a second pattern to learn with no semantic payoff.

### Convention alignment

| Language/System | Arrow | Meaning |
|----------------|-------|---------|
| Haskell | `->` | Function type / pattern match result |
| Erlang | `->` | Clause body |
| R | `<-` | Assignment (imperative) |
| Elm | `->` | Function type / case branch result |
| OCaml | `->` | Pattern match / function type |
| PostgreSQL | `GENERATED ALWAYS AS (expr)` | Keyword-based, no arrow |
| Kotlin | `get() =` | Property getter |
| C# | `=>` | Expression body |

The `<-` convention is associated almost exclusively with **imperative assignment** (R, some reactive frameworks). The `->` convention is associated with **derivation, pattern matching, and "produces"** semantics. Precept's computed fields are derivations, not assignments. `->` is the correct convention.

---

## 5. Verdict: REJECT

### What's wrong with the proposal

1. **The compiler simplification claim is false.** There is no ambiguity in the current `->` usage that `<-` would resolve. The parser never needs to disambiguate between computed-field `->` and action-chain `->` because these are in different grammar productions entered through different leading tokens. The disambiguation cost is zero today.

2. **`<-` introduces a real lexer conflict.** The character pair `< -` currently and correctly scans as two separate tokens (`LessThan`, `Minus`). Adding `<-` as a two-char token breaks maximal-munch for expressions like `Score < -5`. Resolving this requires whitespace sensitivity or special-case scanning — both of which are regressions from the current zero-special-case operator scanner.

3. **`<-` carries wrong semantics.** The left-pointing arrow evokes assignment ("value flows into variable"), which is an imperative concept. Precept's computed fields are declarative derivations ("field is defined as expression"). The right-pointing arrow correctly conveys "this produces" / "this derives as." This is not a style preference — it's a semantic alignment question that affects how readers (human and AI) interpret the construct.

4. **It breaks the universal pipeline glyph.** `->` currently serves as a consistent "what follows is produced" glyph in both field derivations and action chains. Splitting this into two directional arrows doubles the visual vocabulary without adding expressiveness. The consistency of a single arrow glyph is a feature, not a coincidence — it was a deliberate design choice (spec §2.2, line 644: "The `->` arrow is deliberately overloaded to create a visual pipeline that reads top-to-bottom").

### Tradeoff accepted

We accept that `->` is a multi-use token (field derivation + action chain), which means the parser must know its context to interpret it. This is a trivially cheap disambiguation — the parser is already inside the correct production — and the readability benefit of a single universal "produces" glyph outweighs the theoretical elegance of one-token-one-meaning.

---

## 6. Alternatives Considered

### `=>` (fat arrow)

Used in C#, JavaScript, TypeScript for expression bodies. Would avoid the `< -` collision. However:
- Introduces a third arrow-like glyph alongside `->` and `=`.
- `=>` is strongly associated with lambda/function bodies in mainstream languages. Precept doesn't have lambdas. Importing the glyph without the semantics creates false familiarity.
- Doesn't improve over `->` in any dimension that matters.

**Verdict:** No advantage.

### `=` (plain equals)

```
field Tax as number nonnegative = Subtotal * TaxRate
```

This is how spreadsheets and some config languages work. Problems:
- `=` is already `TokenKind.Assign` — used in `set Balance = 0` action statements. Adding a second role creates a real ambiguity: in `field X as number = 5`, is `5` a computed expression or a default value? Defaults use `default 5`, so there's a syntactic distinction, but the visual similarity is a readability trap.
- `=` in an expression context evokes imperative assignment even more strongly than `<-`.

**Verdict:** Worse than the status quo.

### Keyword-based (`computed`, `derives`, `defined as`)

```
field Tax as number nonnegative computed Subtotal * TaxRate
field Tax as number nonnegative derives Subtotal * TaxRate
```

This follows the PostgreSQL `GENERATED ALWAYS AS` model. However:
- Precept's field declaration line is already dense: `field Name as Type Modifier* -> Expr`. Adding a multi-word keyword increases line width without adding clarity over `->`.
- A keyword would need to be reserved, increasing the keyword table and reducing the identifier namespace.
- The `->` glyph is compact and visually distinct — it immediately signals "derivation follows" without consuming identifier-like tokens.

**Verdict:** Inferior to `->` for Precept's compact line-oriented grammar.

### Status quo (`->`)

The right-pointing arrow:
- Has zero lexer conflicts.
- Requires zero parser disambiguation (context is always unambiguous from the enclosing production).
- Reads as "derives as" / "produces" — correct semantics for both computed fields and action chains.
- Is consistent with functional/DSL precedent for "this produces that."
- Is already implemented, documented, tested, and understood.

**Verdict:** The correct choice. No change needed.

---

## Source References

| Artifact | Location | Relevance |
|----------|----------|-----------|
| `TokenKind.Arrow` | `src/Precept/Language/TokenKind.cs:145` | Token enum definition |
| Arrow catalog entry | `src/Precept/Language/Tokens.cs:328–329` | `Cat_Str` category, `keyword.operator.arrow.precept` scope |
| `TwoCharOperators` table | `src/Precept/Language/Tokens.cs:411–415` | Generic two-char scan table derivation |
| `TryScanOperator()` | `src/Precept/Pipeline/Lexer.cs:733–757` | Maximal-munch scan — zero special cases |
| `LessThan` token | `src/Precept/Language/Tokens.cs:314` | `<` is single-char operator, `<` is also in `TwoCharOperatorStarters` via `<=` |
| `Minus` token | `src/Precept/Language/Tokens.cs:320–321` | `-` is single-char operator, `-` is also in `TwoCharOperatorStarters` via `->` |
| Field declaration grammar | `docs/language/precept-language-spec.md:576` | `("->" Expr)?` at end of field production |
| Transition row grammar | `docs/language/precept-language-spec.md:626–628` | `("->" ActionStatement)* "->" Outcome` |
| Deliberate overload rationale | `docs/language/precept-language-spec.md:644` | "deliberately overloaded to create a visual pipeline" |
| Scan order spec | `docs/language/precept-language-spec.md:209` | `->` before `-` in maximal-munch priority |
| Computed field research | `research/language/expressiveness/computed-fields.md` | Read-only derivation contract, precedent survey |
| `computed-tax-net.precept` | `samples/computed-tax-net.precept:10–11` | Canonical computed field usage |
| `invoice-line-item.precept` | `samples/invoice-line-item.precept:16–20` | Multi-step computed field chains |
| `travel-reimbursement.precept` | `samples/travel-reimbursement.precept:14` | Computed field with modifier interleaving |

---

# Design Session Round 1: Catalog-Driven Parser Full Vision

**By:** Frank
**Date:** 2026-04-27
**Status:** Round 1 complete — awaiting George's challenge (Round 2)

## What This Is

Round 1 of a 3-round design session requested by Shane. The prior analysis walked back Layer D (slot-driven productions) and rejected Layer C (disambiguation metadata). Shane explicitly rejected those walkbacks and asked for a full-vision design with no compromise.

## Key Decisions in This Round

1. **`DisambiguationEntry` replaces `LeadingToken` on `ConstructMeta`.** The single `LeadingToken` field cannot express constructs with multiple leading tokens (`StateEnsure` has 3, `AccessMode` has 2 with different disambiguation per entry). The new `Entries: ImmutableArray<DisambiguationEntry>` field carries per-leading-token disambiguation metadata.

2. **Generic disambiguation replaces 4 hand-written methods.** `ParseDisambiguated` handles the `when` guard uniformly, then matches disambiguation tokens from catalog metadata. Zero per-construct disambiguation code.

3. **Generic slot iteration drives all 11 constructs.** `ParseConstructSlots` reads `meta.Slots` and dispatches to slot parsers via a `FrozenDictionary<ConstructSlotKind, Func<SyntaxNode?>>`. No per-construct parse methods.

4. **Node factory dictionary instead of exhaustive switch.** Trades CS8509 compile-time enforcement for runtime testability via factory completeness tests. Flagged for George's challenge.

5. **Source generation rejected at current scale.** Design is generator-ready but 11 constructs don't justify the infrastructure investment.

## Design Artifact

`docs/working/catalog-parser-design-v1.md` — full design with C# sketches, catalog changes, migration path, and questions for George.

## What Round 2 Should Challenge

See `## For George` section in the design doc. Key areas: `Entries` replacing `LeadingToken` (breaking catalog change), `when` guard uniformity assumption, slot parser `SyntaxNode?` return type fragility, factory dictionary vs. switch, anchor/guard injection coupling, and clean-slate re-estimate.

---

# Decision: Catalog-Driven Parser Design — Round 3 Resolutions

**By:** Frank
**Date:** 2026-04-27
**Status:** Design decisions locked pending Shane review

## Context

Round 3 of the catalog-driven parser design collaboration. George (Round 2) found two bugs in the v1 design and flagged six decisions for Frank's disposition. Shane added a new question about language extensibility and generic AST options.

## Decisions Made

### George's Six Flagged Items

1. **F1 (LeadingTokenSlot): ACCEPTED.** `LeadingTokenSlot: ConstructSlotKind?` on `DisambiguationEntry` correctly handles the `write all` bug where the leading token doubles as slot content.

2. **F2 (BuildNode shape): GEORGE WINS — exhaustive switch.** CS8509 compile-time enforcement is the correct invariant shape for BuildNode. `_slotParsers` stays as dictionary (registry pattern). Split by purpose, not unified.

3. **F3 (ActionChain peek-before-consume): ACCEPTED.** Verified against all three outcome forms and the no-action case. Fix is correct and complete.

4. **F4 (Two-position when guard): ACCEPTED.** Both pre-disambiguation and post-EventTarget guard positions are valid Precept syntax. The generic disambiguator handles both uniformly. Spec must document both.

5. **F5 (DisambiguationTokens derivation): REJECTED.** Routing and grammar are separable concerns. Declare disambiguation tokens explicitly. No `IntroductionTokens` field on ConstructSlot.

6. **F6 (Migration PR sequence): ACCEPTED with bridge property.** Catalog shape change in PR 1 with `PrimaryLeadingToken` backward-compatible bridge. Parser work in subsequent PRs. Bridge removed when last consumer migrates.

### Extensibility Analysis Outcomes

- Generic AST (ConstructNode with Slots[]): **REJECTED.** Catastrophic type-safety loss for TypeChecker and Evaluator.
- AST-as-catalog-tree: **REJECTED.** Confuses syntax with semantics.
- Source-generated AST nodes: **DEFERRED.** Break-even at ~25-30 constructs. Design is generator-ready.
- Irreducible per-construct code identified: ConstructKind, GetMeta, AST node, BuildNode arm, TypeChecker rules, Evaluator semantics.

## Artifacts

- `docs/working/catalog-parser-design-v3.md` — supersedes v1 and v2 as the living design document.

---

## Summary

Audited all 32 files in `docs/` for references to field access modes, field modifiers, token vocabulary, grammar, diagnostics, and runtime enforcement. 7 files required updates; 25 required no change.

---

## Design Confirmed (Locked)

1. **Two-layer access mode model:**
   - Layer 1: field-level `writable` modifier sets baseline (`write` default across states)
   - Layer 2: `in <State> write|read|omit` overrides per-(field, state) pair
   - State-level ALWAYS wins over field-level for a specific pair
   - Fields without `writable` default to `read` in all states (D3 baseline preserved)

2. **Root-level `write <Field>` eliminated.** Use `writable` modifier on the field declaration instead.

3. **Root-level `write all` preserved** as sugar for stateless precepts — marks all non-computed fields writable.

4. **`writable` on computed field** → existing `ComputedFieldNotWritable` diagnostic fires.

5. **`writable` on event arg** → new `WritableOnEventArg` diagnostic (compile-time only; no runtime backstop path).

6. **`TokenKind.Writable`**: `Text = "writable"`, category `Declaration`, `ValidAfter = VA_FieldModifier`.

7. **`ModifierKind.Writable`**: `ModifierShape.Flag`, `FieldModifierMeta` subtype. Count: 14 → 15.

---

## Files Updated

| File | Changes |
|------|---------|
| `docs/language/precept-language-spec.md` | §1.1 token vocabulary, §1.2 keywords, §2.2 grammar/composition rules, §2.4 modifiers, §3.8 validation, §3.10 diagnostics |
| `docs/language/precept-language-vision.md` | Editability form table, declaration keywords, Field Access Modes section, composition rules, parser/typechecker responsibilities |
| `docs/compiler/parser.md` | Flag modifiers list (added `writable`), dispatch note (write all only), AccessMode grammar node |
| `docs/compiler/type-checker.md` | Processing model — `writable` modifier validation and `WritableOnEventArg` |
| `docs/compiler/diagnostic-system.md` | `WritableOnEventArg` added to `DiagnosticCode` enum and exhaustive switch |
| `docs/language/catalog-system.md` | `Writable` in TokenKind enum, token count 90+ → 91+, `FieldModifierMeta` 14 → 15 members |
| `docs/runtime/evaluator.md` | Access-Mode Enforcement note updated — resolved mode from two-layer composition |

## Files Confirmed No Change

All `docs/working/` files (historical records — must not be updated per audit policy), `docs/philosophy.md`, lexer, graph-analyzer, proof-engine, literal-system, tooling-surface, precept-builder, fault-system, result-types, primitive-types, temporal-type-system, business-domain-types, extension, mcp (stub), language-server (stub), and all READMEs.

**Key rationale for result-types.md:** The runtime `FieldAccessMode { Read, Write }` enum represents the *resolved* per-(field, state) mode after both layers are applied. Correct as-is — `writable` is a compile-time declaration modifier; its resolution into runtime access mode happens in the Precept Builder.

---

## Open Questions / Escalations

None. All decisions locked and documented above.

---

# Frank — `writable` Field Modifier Review

**Date:** 2026-04-27  
**Branch:** `precept-architecture`  
**Commits reviewed:** 28535e4 (catalog + docs), 54672c8 (samples + tests)  
**Verdict:** BLOCKED

---

## Verdict: BLOCKED

One blocker. Three minor doc defects. Everything else is well-executed. Fix B1, M1, and M2 and this clears.

---

## B1 — `Constructs.AccessMode.LeadingToken` incorrectly changed to `TokenKind.In`
**Severity:** Blocker  
**File:** `src/Precept/Language/Constructs.cs`, line 107

`AccessMode.LeadingToken` was changed from `TokenKind.Write` to `TokenKind.In`. This is wrong and the impact is non-trivial: `Parser.cs` is a stub throwing `NotImplementedException`. The real parser has not been written yet. `parser.md` line 138 states: "The dispatch table is a direct map from `ConstructMeta.LeadingToken` to parse methods." A Parser.cs implementer following the catalog will build:

- `In → ParseAccessMode()` — **wrong**
- `In` at the top level must route to `ParseInScoped()` for preposition disambiguation (StateEnsure, AccessMode state-scoped form, StateAction all share `In` as leading token). Direct `In → ParseAccessMode()` would skip that disambiguation entirely.
- The `Write → ParseAccessMode()` path for `write all` (parser.md line 130) has no catalog entry to back it — it would be a dangling dispatch table entry with no corresponding `LeadingToken`.

The correct `LeadingToken` for `AccessMode` is `TokenKind.Write`. This is the one token that maps DIRECTLY to `ParseAccessMode()` at the top level. The state-scoped form (`in State write|read|omit`) enters `AccessMode` indirectly through `ParseInScoped()` — it is a secondary production routed by the preposition disambiguation method, not a first-class `LeadingToken` dispatch.

The `UsageExample = "in Draft write Amount"` and the description are accurate and should be kept. The `LeadingToken` field alone is wrong.

**Required fix:** `TokenKind.In` → `TokenKind.Write` in `Constructs.cs` `AccessMode` entry.

---

## M1 — Stale `edit` terminology in spec §1.1 token table
**Severity:** Minor  
**File:** `docs/language/precept-language-spec.md`, lines 47 and 111

Two entries in the §1.1 token vocabulary table carry v1 `edit` terminology:

- Line 47: `| In | in | State-scoped ensure/edit (in State ensure ...) |` — "edit" is a v1 keyword removed in v2 (§1.2 explicitly states this). Should reference write/read/omit.
- Line 111: `| All | all | Universal quantifier / edit all |` — "edit all" is the v1 form. The v2 form is `write all`.

§1.2 says "`edit` is not reserved in v2. `write` replaces `edit`." Having the token table say "edit all" is a direct contradiction in the same document.

**Required fix:**
- Line 47: `State-scoped ensure/edit (in State ensure ...)` → `State-scoped ensure/write/read/omit scope preposition`
- Line 111: `Universal quantifier / edit all` → `Universal quantifier / write all (stateless precepts), read all / omit all (state-scoped)`

---

## M2 — catalog-system.md field modifier count comment stale
**Severity:** Minor  
**File:** `docs/language/catalog-system.md`, line 740

The code sample comment reads `// ── Field modifiers (14) ─────────────────────────────────`. There are now 15 field modifiers. The summary table at line 716 correctly says 15. The `ModifierKind.cs` group comment correctly says `field (15)`. The inline doc comment is the only laggard.

**Required fix:** `(14)` → `(15)` on line 740.

---

## Good Findings

**G1 — Token group placement correct.** `TokenKind.Writable` lands in the Declaration group between `Optional` and `Because`. Consistent with `Optional` as a field-declaration modifier; Declaration group is the right category.

**G2 — `VA_FieldModifier` bidirectional setup correct.** `TokenKind.Writable` appears in the `VA_FieldModifier` array (other modifiers can precede `writable`) and `writable`'s own `ValidAfter: VA_FieldModifier` (other modifiers can follow `writable`). Position-agnostic within the modifier list. Samples confirm both orderings work: `positive writable`, `optional writable`.

**G3 — `FieldModifierMeta(AnyType)` split is architecturally correct.** `ApplicableTo = AnyType` (empty) is right because computed-field exclusion is a semantic rule (field has a `->` expression), not a type-compatibility rule. `ApplicableTo` encodes type-based restrictions; the computed-field restriction belongs to the type checker. The test comment explicitly documents this: "computed-field restriction is enforced by the type checker, not the modifier catalog." Clean separation.

**G4 — `WritableOnEventArg` diagnostic is correctly placed and specified.** Stage=Type, Severity=Error, Category=Structure, positioned after `CircularComputedField` and before `ConflictingAccessModes` in the enum. The message "The 'writable' modifier cannot appear on event argument '{0}'" is accurate. The fix hint "Remove 'writable' — event arguments are always read-only within the transition body" is precise and actionable.

**G5 — Sample migration is clean.** All 6 migrated samples (`computed-tax-net`, `fee-schedule`, `invoice-line-item`, `payment-method`, `sum-on-rhs-rule`, `transitive-ordering`) are stateless precepts. `writable` appears only on non-computed fields in every case. All 22 other samples are untouched. State-scoped `in State write` forms are preserved across the full sample set. `customer-profile.precept:write all` is correctly untouched. No stale `write <FieldName>` patterns remain.

**G6 — D3 guarantee correctly preserved.** Fields without `writable` default to `read`. The language spec §2.2 "D3 default" rule (composition rule #2) is accurate. The evaluator.md correctly describes the two-layer composition model being pre-resolved at Precept Builder time, with the evaluator reading descriptors, not re-computing access modes.

**G7 — Two-layer composition model documentation is a significant improvement.** The Layer 1 / Layer 2 model in §2.2 replaces the previous flat "read-by-default" description with a structured account of how field baselines and state overrides compose. The nine composition rules are complete, ordered correctly (baseline → D3 default → state override → guarded write → omit clear → set restriction → conflicts → computed → event arg), and accurate.

**G8 — All 1783 tests pass.** No regressions.

**G9 — Language spec completeness is thorough.** `writable` appears in §1.1 (Declaration keyword table), §1.2 (reserved keyword set), §2.2 (access mode grammar with full two-layer model), §2.4 (field modifiers table), §3.8 (modifier validation table), §3.10 (diagnostic table with `WritableOnEventArg`). No section missing.

**G10 — MCP gap is pre-existing, not introduced by this change.** `LanguageTool.cs` does not exist — MCP only has `PingTool.cs`. `writable` will flow through automatically when `LanguageTool` is implemented from the catalog. No gap introduced here.

---

## Minor Observation (Not a Blocker)

**`ComputedFieldNotWritable.RelatedCodes` does not include `WritableOnEventArg`.** Both diagnose misuse of `writable`. This is a weak cross-navigation gap in the LS diagnostic experience. Not a blocker — the two codes diagnose distinct surfaces (computed fields vs. event args) — but worth considering when the LS diagnostic UI ships.

---

## Fixes Required Before Re-Review

1. `src/Precept/Language/Constructs.cs` — `TokenKind.In` → `TokenKind.Write` on the `AccessMode` `LeadingToken` argument. Description and example may stay as-is.
2. `docs/language/precept-language-spec.md` line 47 — remove "edit", add write/read/omit.
3. `docs/language/precept-language-spec.md` line 111 — `edit all` → `write all`.
4. `docs/language/catalog-system.md` line 740 — `(14)` → `(15)`.

Items 2–4 are doc-only and may land in the same commit as item 1.

---

# Decision Note: Catalog-Driven Parser Design Round 2

**By:** George
**Date:** 2026-04-27
**Related doc:** `docs/working/catalog-parser-design-v2.md`
**Status:** Pending Frank's Round 3 response

---

## Context

Shane asked for the full vision of a catalog-driven parser with no compromise. Frank wrote Round 1
(`docs/working/catalog-parser-design-v1.md`). This note records George's Round 2 findings for
the decisions log.

---

## Findings Requiring Team Decision

### Finding G1 — Two Implementation Bugs in Frank's v1 (Both Are Real, Both Need Fixes)

**Bug 1 (ActionChain/Outcome boundary):** Frank's `ParseActionChain()` consumes `->` then breaks
when an outcome keyword follows, leaving the outcome keyword as the current token. `ParseOutcome()`
then expects `->` first and returns null. This fires on every `TransitionRow` with actions — the
`Outcome` required slot gets `ExpectedSlot` diagnostic on valid input.

Fix: `ParseActionChain()` must peek at `Peek(1)` before consuming `->` when an outcome keyword
follows. Leave `->` for `ParseOutcome()` to consume as its introduction token.

**Bug 2 (AccessMode `Write`-leading path):** `write all` dispatches with `Write` consumed as the
leading token. The generic slot iterator then calls `_slotParsers[AccessModeKeyword]()` but current
token is now the field target (`all`). The `Write` token is gone.

Fix: Add `LeadingTokenSlot: ConstructSlotKind?` to `DisambiguationEntry`. When set, the parser
injects a synthetic node from the already-consumed leading token into that slot rather than parsing
fresh.

**Disposition needed:** Frank should confirm both fixes are accepted before any implementation work
begins on the parser.

---

### Finding G2 — `DisambiguationEntry` Is a Breaking Catalog Shape Change

`ConstructMeta.LeadingToken` must become `Entries: ImmutableArray<DisambiguationEntry>`. This
breaks:
- LS completions that read `ConstructMeta.LeadingToken`
- MCP vocabulary output that uses it
- Any tests referencing it

Mitigation: add `PrimaryLeadingToken` computed property = `Entries[0].LeadingToken` as
backward-compat bridge. Consumers migrate to `Entries` incrementally.

**The "no migration" claim in Frank's v1 is correct for the parser stub, not for catalog consumers.**
The catalog PR must be sequenced before parser implementation begins.

---

### Finding G3 — Implicit Contract Must Become Explicit

The generic disambiguator leaves disambiguation tokens unconsumed. This only works if each
slot parser consumes its own introduction token as its first action. This is an invariant that
holds for all current slot parsers but is undocumented. If a future slot parser omits this,
the stream is corrupted silently.

Mitigation: document the contract in the `_slotParsers` dictionary code comment and in the
parser design doc. This is not a blocker, but it must be done before implementation.

---

### Finding G4 — Pre-Disambiguation `when` Guard Position in TransitionRow

The `when` guard can appear at two syntactic positions in TransitionRow:
1. Before the `on` disambiguation token (`from X when expr on Y → ...`)
2. After the event target (`from X on Y when expr → ...`, the standard form)

Frank's generic disambiguator handles both correctly: pre-disambiguation guards are injected
into slot[2] after disambiguation; post-EventTarget guards are parsed naturally during slot
iteration. Both produce the same `TransitionRowNode.Guard` field.

**Decision needed from Frank:** Is `from X when expr on Y → ...` (pre-disambiguation guard)
actually valid Precept syntax? The parser.md tables list `When` as a re-check option for all
preposition methods, but no samples demonstrate this form. If it is NOT valid, the disambiguator
should either skip the guard-consumption step or constrain it to constructs that declare
a `GuardClause` slot at the construct level.

---

## Revised Estimate

| Option | Scope | Estimate |
|--------|-------|---------|
| Option 1 (A+B+C+E only) | Catalog + vocabulary tables + disambiguation metadata + sync set | 1 week |
| Option 3 (A+B+D+E, Frank's full vision) | Full catalog-driven parser | **3–3.5 weeks** (was 2.5–3; +18h correctness hardening) |

The corrections do not change the architectural recommendation — they harden it. Option 3 is
still viable on a clean-slate build. The bugs are correctness issues that exist regardless of
which option is chosen once parser implementation begins.

---

## No Code Before These Are Resolved

Per George's charter: no implementation work until Frank's sign-off on the design. These bugs
are discovered in design review — the right time. The parser is still a stub. These are zero-cost
fixes at design time.

---

# Soup Nazi — Writable Coverage Review
**Date:** 2026-04-27  
**Reviewer:** Soup Nazi (Tester)  
**Scope:** `writable` field modifier — test coverage audit  
**Test run:** 1783 tests, 0 failed, 0 skipped ✅

---

## Verdict: BLOCKED

Two blockers. No soup until they are fixed.

---

## Blockers

### B1: `AccessMode.LeadingToken` change is untested

`Constructs.AccessMode` had its `LeadingToken` changed from `TokenKind.Write` to `TokenKind.In`. No test anywhere in `ConstructsTests.cs` asserts `LeadingToken` on any construct — the field is completely invisible to the test suite.

If someone reverts this to `Write` (or any other value), no test will catch it.

**Required fix:** Add a test to `ConstructsTests.cs`:

```csharp
[Theory]
[InlineData(ConstructKind.AccessMode,       TokenKind.In)]
[InlineData(ConstructKind.StateEnsure,      TokenKind.In)]
[InlineData(ConstructKind.StateAction,      TokenKind.To)]
[InlineData(ConstructKind.EventEnsure,      TokenKind.Ensure)]
[InlineData(ConstructKind.FieldDeclaration, TokenKind.Field)]
[InlineData(ConstructKind.StateDeclaration, TokenKind.State)]
[InlineData(ConstructKind.EventDeclaration, TokenKind.Event)]
public void LeadingToken_IsCorrect(ConstructKind kind, TokenKind expectedToken)
{
    Constructs.GetMeta(kind).LeadingToken.Should().Be(expectedToken,
        $"{kind} must begin with {expectedToken}");
}
```

`AccessMode → In` is the regression anchor for this change. The rest are bonus coverage.

---

### B2: `WritableOnEventArg` missing from `DiagnosticsTests.TypeCodes` static list

`TypeCodes` in `DiagnosticsTests.cs` (line 153) is a hardcoded TheoryData used by `TypeStageCodes_AllHaveTypeStage`. It includes `ComputedFieldNotWritable` but does **not** include `WritableOnEventArg`. The two diagnostic codes were added in the same implementation — one is present, one is absent.

The three dynamic `Create_*` theories (using `AllDiagnosticCodes()`) DO exercise `WritableOnEventArg` and would catch a missing `GetMeta` entry or factory crash. But `TypeStageCodes_AllHaveTypeStage` will not catch a future stage miscategorization for `WritableOnEventArg`.

**Required fix:** Add `DiagnosticCode.WritableOnEventArg` to the `TypeCodes` TheoryData in `DiagnosticsTests.cs`:

```csharp
// near ComputedFieldNotWritable
DiagnosticCode.WritableOnEventArg,
```

---

## Good Observations

**G1: ModifiersTests — 7 new/updated tests are correct.**  
Count invariants updated (28→29 total, 14→15 field, 25→26 structural), `Writable_AppliesToAnyType`, `Writable_IsStructuralFlag`, `Writable_TokenTextIsWritable`, and `FlagModifiers_HasValueIsFalse` theory updated — all well-formed. The "empty = any type" semantics are correctly documented in the assertion message.

**G2: Dynamic exhaustiveness net is solid.**  
`GetMeta_ReturnsForEveryModifierKind`, `All_ContainsEveryKindExactlyOnce`, and the three `Create_*` theories all use `Enum.GetValues<>()` — new entries are covered without code changes. `WritableOnEventArg` is not orphaned; it passes the Create factory tests today.

**G3: TokensTests uses dynamic count — no hardcoded token count to update.**  
`All_ContainsExactlyAsManyEntries_AsEnumValues` derives its expected count from the enum. `AllKeywords_HaveTextMateScope` and `AllKeywords_HaveSemanticTokenType` cover `TokenKind.Writable` automatically (it is `Cat_Decl`, which is included in both token-property checks). No action needed.

**G4: Sample files are clean.**  
All 6 migrated samples place `writable` only on non-computed fields:
- `computed-tax-net.precept`: Subtotal, TaxRate writable; Tax, Net (computed) — no `writable`. ✅  
- `fee-schedule.precept`: BaseFee, DiscountPercent, MinimumCharge writable; TaxRate, CurrencyCode locked. ✅  
- `invoice-line-item.precept`: Description, UnitPrice, Quantity, DiscountPercent writable; Subtotal through LineTotal (all computed) — no `writable`. ✅  
- `sum-on-rhs-rule.precept`: Total, Tax, Fee writable; Net (computed) — no `writable`. ✅  
- `transitive-ordering.precept`: High, Mid, Low writable; Spread (computed) — no `writable`. ✅  
- `payment-method.precept`: IsDefault, Nickname writable; no computed fields. ✅

**G5: Hover description coverage is implicit.**  
`GetMeta_ReturnsForEveryModifierKind` asserts `Description.NotBeNullOrEmpty` for every `ModifierKind` — `Writable` is covered without a dedicated test.

---

## Deferred Tests (Parser/TypeChecker not yet implemented)

These tests MUST exist before the implementation is marked complete. They are not optional.

| ID | Test | Trigger |
|----|------|---------|
| D1 | `field X as money writable` → zero diagnostics | Parser + TypeChecker |
| D2 | `field X as money` (no modifier) → field is read-only baseline | TypeChecker semantic model |
| D3 | Computed field + `writable` → `ComputedFieldNotWritable` | TypeChecker |
| D4 | Event arg + `writable` → `WritableOnEventArg` | TypeChecker |
| D5 | Field `writable` baseline + `in State write|read|omit` → correct composed mode | TypeChecker + evaluator |
| D6 | State-scoped `in State write Field` still works (regression) | TypeChecker |
| D7 | Stateless `write all` still works (regression) | TypeChecker |
| D8 | `writable` on computed field that also has `default` → both `ComputedFieldNotWritable` and `ComputedFieldWithDefault` fire | TypeChecker |

---

## Summary

Fix B1 and B2, then resubmit. The catalog-level work is solid. The sample migration is correct. The blocking gaps are small and surgical: one new `[Theory]` in `ConstructsTests.cs` and one line added to `DiagnosticsTests.TypeCodes`.

No soup until then.

---

## Verdict

**NEEDS MORE TESTS**

Two blockers. The catalog work is solid. The lexer surface is now covered. The gaps are surgical and documented below with exact fixes.

---

## Test Coverage Assessment

### What's covered

| Area | Test | Status |
|------|------|--------|
| `Writable_AppliesToAnyType` — `ApplicableTo.Should().BeEmpty()` | `ModifiersTests.cs` | ✅ |
| `Writable_IsStructuralFlag` — `Category == Structural`, `HasValue == false` | `ModifiersTests.cs` | ✅ |
| `Writable_TokenTextIsWritable` — `Token.Text == "writable"`, `Token.Kind == Writable` | `ModifiersTests.cs` | ✅ |
| `FlagModifiers_HasValueIsFalse` includes `Writable` | `ModifiersTests.cs` | ✅ |
| Count invariants updated: 29 total, 15 field, 26 structural | `ModifiersTests.cs` | ✅ |
| `GetMeta_ReturnsForEveryModifierKind` covers `Writable` via enum exhaustion | `ModifiersTests.cs` | ✅ |
| `AllFieldModifiers_AreStructural` covers `Writable` structurally | `ModifiersTests.cs` | ✅ |
| `TokenKind.Writable` in `Keywords`, TextMateScope, SemanticTokenType (via exhaustiveness) | `TokensTests.cs` | ✅ |
| `Create_*` factory theories cover `WritableOnEventArg` via `Enum.GetValues<DiagnosticCode>()` | `DiagnosticsTests.cs` | ✅ |
| `WritableOnEventArg` meta not-null, severity + stage returned by `Create` | `DiagnosticsTests.cs` | ✅ (via AllDiagnosticCodes dynamic) |
| Lexer emits `Writable` token after type keywords (all 5 surface cases) | `WritableSurfaceTests.cs` | ✅ (added during investigation) |
| `in Draft write Amount` emits `Write` not `Writable` (correct distinction) | `WritableSurfaceTests.cs` | ✅ |
| `write all` preserved — lexes as `Write + All` | `WritableSurfaceTests.cs` | ✅ |
| Root-level `write Amount` — lexer doesn't reject (Parser's job) | `WritableSurfaceTests.cs` | ✅ |

### What is NOT covered — blockers

See Findings section.

---

## Findings

### [GAP] WritableOnEventArg Missing from TypeCodes Stage Group

**Severity:** Major

**File:** `test/Precept.Tests/DiagnosticsTests.cs`, `TypeCodes` member data (~line 153)

**Finding:** `DiagnosticsTests.TypeCodes` is the hardcoded list used by `TypeStageCodes_AllHaveTypeStage`. It includes `DiagnosticCode.ComputedFieldNotWritable` (added in the same PR) but does **not** include `DiagnosticCode.WritableOnEventArg` (also added in the same PR). The two codes were introduced together; one made it into the stage-group list, one did not.

The three dynamic `Create_*` theories iterate `Enum.GetValues<DiagnosticCode>()` and DO exercise `WritableOnEventArg` — the `GetMeta` entry exists, the factory doesn't crash, and the severity/stage round-trips correctly via the generic path. But `TypeStageCodes_AllHaveTypeStage` will not catch a future miscategorization (e.g., accidentally setting `DiagnosticStage.Parse` instead of `DiagnosticStage.Type`).

Additionally, no severity spot-check exists for `WritableOnEventArg` the way one exists for `DivisionByZero_HasErrorSeverity`. This is a minor but real gap — it's `Severity.Error` and that contract should be pinned.

**Required action:**

1. Add `DiagnosticCode.WritableOnEventArg` to `TypeCodes` in `DiagnosticsTests.cs` between `CircularComputedField` and `ConflictingAccessModes`:

```csharp
// existing entries
DiagnosticCode.ComputedFieldNotWritable,
DiagnosticCode.ComputedFieldWithDefault,
DiagnosticCode.CircularComputedField,
DiagnosticCode.WritableOnEventArg,   // ← ADD THIS
DiagnosticCode.ConflictingAccessModes,
```

2. Add a severity spot-check:

```csharp
[Fact]
public void WritableOnEventArg_HasErrorSeverity()
{
    Diagnostics.GetMeta(DiagnosticCode.WritableOnEventArg).Severity.Should().Be(Severity.Error);
}
```

---

### [GAP] AccessMode.LeadingToken Change Has No Regression Test

**Severity:** Major

**File:** `test/Precept.Tests/ConstructsTests.cs`

**Finding:** `ConstructKind.AccessMode.LeadingToken` was changed from `TokenKind.Write` to `TokenKind.In` as part of this PR. This is a behavioral change to the catalog's public contract — `LeadingToken` drives LS completions, MCP vocabulary output, and semantic token classification. No test in `ConstructsTests.cs` asserts `LeadingToken` on any construct — the property is completely invisible to the test suite. A regression back to `TokenKind.Write` would not be caught by any test.

The `GetMeta_ReturnsForEveryConstructKind` exhaustiveness test checks `Kind`, `Name`, `Description`, and `UsageExample`. It does not check `LeadingToken`.

**Required action:** Add a `[Theory]` to `ConstructsTests.cs` pinning `LeadingToken` for key constructs. `AccessMode → In` is the regression anchor for this PR change; the others are bonus coverage that should also have been tested:

```csharp
[Theory]
[InlineData(ConstructKind.AccessMode,       TokenKind.In)]
[InlineData(ConstructKind.StateEnsure,      TokenKind.In)]
[InlineData(ConstructKind.StateAction,      TokenKind.To)]
[InlineData(ConstructKind.EventEnsure,      TokenKind.On)]
[InlineData(ConstructKind.FieldDeclaration, TokenKind.Field)]
[InlineData(ConstructKind.StateDeclaration, TokenKind.State)]
[InlineData(ConstructKind.EventDeclaration, TokenKind.Event)]
[InlineData(ConstructKind.TransitionRow,    TokenKind.From)]
[InlineData(ConstructKind.PreceptHeader,    TokenKind.Precept)]
public void LeadingToken_IsCorrect(ConstructKind kind, TokenKind expectedToken)
{
    Constructs.GetMeta(kind).LeadingToken.Should().Be(expectedToken,
        $"{kind} must begin with {expectedToken}");
}
```

---

### [CONFIRMED] ModifiersTests — 4 New Writable Tests Are Correct

**Severity:** N/A (confirmed)

**File:** `test/Precept.Tests/ModifiersTests.cs`

**Finding:** All 4 new Writable tests are well-formed and assert the right catalog properties:
- `Writable_AppliesToAnyType` — asserts empty `ApplicableTo` with the correct semantic comment ("empty = applies to all types; computed-field restriction is enforced by the type checker")
- `Writable_IsStructuralFlag` — asserts `Category == Structural` and `HasValue == false`
- `Writable_TokenTextIsWritable` — asserts `Token.Text == "writable"` and `Token.Kind == TokenKind.Writable`
- `FlagModifiers_HasValueIsFalse` updated to include `ModifierKind.Writable`

Count invariants (29 total / 15 field / 26 structural) are correct.

**Required action:** None.

---

### [CONFIRMED] TokensTests — TokenKind.Writable Covered by Exhaustiveness

**Severity:** N/A (confirmed)

**File:** `test/Precept.Tests/TokensTests.cs`

**Finding:** No direct spot-check test for `TokenKind.Writable` exists in `TokensTests.cs`. However, the existing exhaustiveness tests adequately cover it:
- `GetMeta_ReturnsWithoutThrowing_ForEveryTokenKind` — runs over every `TokenKind` including `Writable`
- `All_ContainsExactlyAsManyEntries_AsEnumValues` — count-invariant catches missing entries
- `AllKeywords_HaveTextMateScope` — `Writable` has `Cat_Decl` and non-null text, so it's included
- `AllKeywords_HaveSemanticTokenType` — same reason
- `Keywords_ContainsAllKeywordCategoryTokensWithNonNullText` — `Writable` will be in both `expectedKeys` and `Keywords.Keys`

The indirect coverage via `Writable_TokenTextIsWritable` in `ModifiersTests.cs` also pins the token text. No spot-check gap that needs to be filled.

**Required action:** None. Pre-existing pattern; `ValidAfter` membership is not tested for any token — that's a broader gap outside this PR's scope.

---

### [CONFIRMED] No Old `write Field` Syntax in Test Data

**Severity:** N/A (confirmed)

**File:** All `test/Precept.Tests/*.cs`

**Finding:** Grep for `write\s+\w` across all test files found zero matches in test data strings. The only occurrence is in a comment string in `ConstructsTests.cs` ("root-level write"). No regression from eliminated `write Field` syntax exists in the catalog test suite.

**Required action:** None. Note: the eliminated syntax is not rejected at lex time (lexer is context-free; `write Amount` emits `Write + Identifier` without error). Parser-level rejection must be tested once Parser is implemented.

---

### [CONFIRMED] MCP Regression — Lexer Correctly Handles writable

**Severity:** N/A (confirmed)

**Finding:** MCP server is live (`precept_ping` = ok). All lexer-surface probes pass:

| Probe | Result |
|-------|--------|
| `field Amount as money writable` | `Writable` token emitted after `MoneyType` ✅ |
| `field Amount as money` (no modifier) | No `Writable` token emitted ✅ |
| `write all` on stateless precept | `Write + All` tokens; no `Writable` ✅ |
| `in Draft write Amount` | `In + Write` tokens; `Writable` token absent (correct: `write` is the access-mode keyword, `writable` is the field modifier) ✅ |
| `write Amount` (eliminated syntax) | Lexes as `Write + Identifier`; no lex diagnostic. Rejection is Parser/TypeChecker work ✅ |

All compile paths uniformly throw `NotImplementedException` at `Parser.Parse()` — consistent with the known stub state.

**Required action:** None for current state.

---

### [CONFIRMED] WritableSurfaceTests.cs Created During Investigation

**Severity:** N/A (informational)

**File:** `test/Precept.Tests/WritableSurfaceTests.cs` (new, created during investigation)

**Finding:** 10 new tests were created during the MCP regression phase. They cover:
- 5 `*_LexesCorrectly` tests — verify token stream shapes for each writable surface case
- 5 `*_CompileThrowsNotImplemented` tests — anchor the current stub state

**Caution:** The `CompileThrowsNotImplemented` tests are asserting stub behavior. When Parser is implemented, they will turn red. That is correct and honest — they will be visible failures requiring update. They should NOT be deleted or skipped before the implementation lands; they should be converted to positive-case assertions at that time.

All 10 new tests pass. Total count is now 1793.

**Required action:** None. Keep the file. Update `*_CompileThrowsNotImplemented` tests when Parser is implemented.

---

## Deferred Tests (Parser/TypeChecker stubs — required before implementation is complete)

These tests MUST exist before the `writable` implementation is marked done. Red is acceptable. Skip is not.

| ID | Test | Gate |
|----|------|------|
| D1 | `field X as money writable` → compiles clean, zero diagnostics | Parser + TypeChecker |
| D2 | `field X as money` (no modifier) → field is read-only baseline | TypeChecker semantic model |
| D3 | Computed field + `writable` → `ComputedFieldNotWritable` diagnostic | TypeChecker |
| D4 | Event arg + `writable` → `WritableOnEventArg` diagnostic | TypeChecker |
| D5 | `writable` baseline + `in State write Field` override → correct composed access mode | TypeChecker + evaluator |
| D6 | `in State write Field` still works on non-writable field (regression) | TypeChecker |
| D7 | Stateless `write all` still works (regression) | TypeChecker |
| D8 | `writable` on computed field with `default` → both `ComputedFieldNotWritable` + `ComputedFieldWithDefault` fire | TypeChecker |

---

## Summary

The catalog-level work is solid. The `Writable` modifier entry in `Modifiers.cs` is correct and well-tested. The 1793 tests pass cleanly.

**Fix these two gaps, then resubmit:**

1. **`WritableOnEventArg` → add to `TypeCodes`** in `DiagnosticsTests.cs` + add `WritableOnEventArg_HasErrorSeverity` spot-check.
2. **`AccessMode.LeadingToken → In`** → add `LeadingToken_IsCorrect` theory to `ConstructsTests.cs`.

Both fixes are one-liners or near-one-liners. No soup until then.

