## Core Context

- Owns the core DSL/runtime: parser, type checker, graph analysis, runtime engine, and authoritative language semantics.
- The engine flow is parser to semantic analysis to graph/state validation to runtime execution. Fire behavior must stay consistent with diagnostics, README examples, and MCP output.
- Key runtime areas to protect: constraints, transition guards, event assertions, collection hydration, edit rules, and diagnostic catalogs.
- Documentation should describe the six-stage fire pipeline and implemented semantics accurately, without inventing capabilities.

## Recent Updates

### 2026-04-08 - Sample realism guidance merged
- Scribe merged George's sample-audit guidance into .squad/decisions.md alongside the team directive to use Opus when sample/design judgment is high.
- The standing runtime-facing guidance is to keep realistic domains intact and use aspirational comments rather than flattening workflows to today's weakest primitives.

### 2026-04-05 - Named rule scope and naming converged
- Confirmed field-scoped reuse is sound in when, invariant, and state assert, while on <Event> assert remains incompatible because it is event-arg-only.
- Reweighted the naming decision around Precept's readability goals and aligned the runtime recommendation with rule over predicate.

### 2026-04-04 - DSL pipeline overview
- Consolidated the runtime mental model, major constraint categories, fire stages, and edge cases for downstream agents.
- Key learning: Precept's value is structural integrity across state, data, and rules; every outward-facing description should preserve that unified model.

### 2026-04-06 - README restructure proposal review
- Checked that proposed README/API explanations matched the real runtime surface.
- Key learning: the quickest way to damage trust is to let public examples diverge from actual parser/runtime behavior.

### 2026-05-14 - Named predicate naming analysis

- Re-evaluated whether `guard` is the correct category for a declaration reusable in transition `when`, `invariant`, and state `assert`.
- Key finding: `guard` carries strong transition-gate semantics (UML, xstate) and is misleading when a declaration appears in `invariant` or state `assert` positions. The concept is a **named field-scope boolean predicate**, not a transition guard.
- Compared options: `guard` (misleading), `predicate` (recommended), `rule` (existing informal overloading in docs), `let`/`define` (too generic, blurs computed-field boundary), split `guard`+`predicate` (fragmentation without benefit).
- Verdict: rename from `guard` → `predicate`. Keyword `predicate <Name> when <BoolExpr>`. All prior structural recommendations (reuse sites, scope rules, exclusions) unchanged.
- Amended `docs/research/dsl-expressiveness/expression-feature-proposals.md` (Proposal 3: all keyword and prose references updated) and `expression-language-audit.md` (L3: title, implementation notes, verdict, philosophy fit updated).
- Wrote `.squad/decisions/inbox/george-guard-naming.md` with full options analysis.
- Key pattern: when a DSL construct's name implies narrower semantics than its actual reuse scope, fix the name before the design doc is written — not after.

### 2026-05-01 - Named guard reuse scope analysis

- Researched whether named guard declarations (Proposal 3) can be soundly reused in `invariant` and `in/to/from <State> assert` contexts, not just `when` clauses.
- Key finding: guard body scope (fields + collection accessors) is a subset of invariant/state-assert scope — reuse is sound in both. `on <Event> assert` is explicitly incompatible (event-arg-only scope at Stage 1, disjoint from field scope).
- Verdict: `feasible-with-caveats`. Caveats: (a) guard body must be validated in field-only scope at declaration time, not deferred to use site; (b) `on <Event> assert` must produce a clear diagnostic; (c) cycle risk resolved since guard-to-guard refs are banned.
- Updated `docs/research/dsl-expressiveness/expression-feature-proposals.md` (Proposal 3 scope rules, new invariant/assert examples) and `expression-language-audit.md` (L3 what is missing, implementation notes, verdict).
- Wrote `.squad/decisions/inbox/george-guard-reuse.md` with full analysis for team.

- Audited the full expression surface: parser, type checker, evaluator, all 21 sample files.
- Produced `docs/research/dsl-expressiveness/expression-language-audit.md` with 12 numbered limitations, implementation verdicts, and cross-cutting notes.
- Key findings:
  - No ternary expression (forces row duplication for conditional values)
  - No `string.length` accessor (string length constraints are inexpressible — trivially fixable)
  - No named guard declarations (multi-condition guards must be copy-pasted verbatim)
  - `on <Event> assert` scope excludes data fields (pipeline design constraint, not parser issue)
  - No numeric math functions like `abs()` (requires new function-call AST node)
  - `contains` is collection-only; no substring matching on strings
  - Division by zero produces silent NaN/infinity at runtime — evaluator should return Fail
- Feasibility verdicts: ternary=feasible, string.length=feasible, named-guards=feasible-with-caveats, abs/functions=feasible-with-caveats, collection-any-all predicates=not-recommended.
- The `on <Event> assert` scope limitation is the one item needing a design decision before any code — it touches the fire pipeline stage contract, not just the parser.
- Notified team via `.squad/decisions/inbox/george-expression-limitations.md`. No implementation until Frank's proposal and Shane's approval.

### 2026-04-08 - Current sample corpus audit

- Audited all 21 current sample files against the implemented runtime surface, the language design docs, and open proposal issues affecting sample realism.
- Produced `docs/research/sample-realism/george-current-sample-audit.md`.
- Key findings:
  - The sample set covers the runtime surface reasonably well, especially collection mutation, state asserts/actions, and first-match routing.
  - The corpus still skews toward syntax coverage over believable business policy: many samples flatten dates into counters, categories into strings/numbers, and intake transitions into long argument-copy chains.
  - The biggest realism multipliers are not cosmetic: `choice`, `date`, `decimal`, computed fields, field constraints, declaration guards, named rules, and absorb shorthand.
  - Best existing pressure-test samples for future language work: `insurance-claim`, `loan-application`, `travel-reimbursement`, and `vehicle-service-appointment`.
- Recommendation merged into `.squad/decisions.md`; inbox cleared. Future sample revisions should use aspirational comments instead of hiding domain pressure behind oversimplified current-language surrogates.
