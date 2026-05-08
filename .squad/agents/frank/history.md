# Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for Precept's DSL and runtime.
- Catalogs remain the language truth; runtime, tooling, and docs must derive behavior from metadata and durable shape rather than enum-identity switch logic or parallel lists.
- Public API surfaces expose stable CLR/JSON interchange contracts; evaluator internals stay internal.
- Investigation docs can be archived once their outcomes are captured in canonical docs, proposals, or the squad decision ledger.

## Learnings

- Closed-vocabulary syntax (types, modifiers, access modes) should be resolved by the parser at recognition time; only open-ended expressions stay deferred as parsed expression trees.
- Symbol binding is a separate concern from type inference: the binder owns declarations and references, then the TypeChecker owns semantic normalization.
- Tooling consumption is layered: TokenStream for lexical work, ConstructManifest for syntax, SymbolTable for name-aware features, and SemanticIndex for semantic features.
- Event coverage remains a structural graph concern; guard-conditioned reachability belongs to the proof engine.
- ProofEngine satisfiability can stay compile-time and bounded: default expressions plus ensure conditions already exist as `TypedExpression` trees, so many initial-state violations can be discharged without evaluator coupling.
- tooling-surface.md updated to reflect shipped grammar generator
- CONTRIBUTING.md reload rules updated with grammar: regenerate step
- Key decision: .NET tool approach confirmed, Constructs-based complex pattern resolution confirmed

## Recent Updates

### 2026-05-08T05:27:37Z — Grammar generator design doc durably recorded
- Scribe merged Frank-18's design-doc note into `.squad/decisions.md`, making `docs/compiler/grammar-generator.md` the canonical generator reference.
- The durable generator gap is now explicit in the ledger: `#messageStrings` is currently unreachable in generated output, and a `TokenMeta.IsMessagePosition` flag on `Because` / `Reject` is the catalog-first fix path.

### 2026-05-08T05:27:37Z — PE Decision 3 deep dive converged on ProofEngine-owned bounded constant folding
- Frank-19 established that the runtime Evaluator cannot be called from stage 6 because it depends on `Compilation`, which the ProofEngine has not produced yet; reuse would create a circular dependency and the current evaluator remains a stub anyway.
- Frank-20 folded the decision into `docs/working/frank-proof-engine-gap-analysis.md` and cleared the transient inbox path, so the active implementation direction is bounded constant folding over `TypedField.DefaultExpression` and `TypedEnsure.Condition` in the `SemanticIndex`.

### 2026-05-08T05:15:57Z — Shane locked PE decisions 1 and 2; evaluator architecture deep dive requested
- Shane approved renaming Strategy 2 to `Declaration Attribute Proof` and locked unqualified periods to permissive `PeriodDimension.Any` behavior.
- The remaining open question was explicitly narrowed to Decision 3's architecture: evaluate whether initial-state satisfiability should be compile-time folding or evaluator reuse.

### 2026-05-08T04:30:00Z — Exhaustive GraphAnalyzer post-implementation audit completed
- Verdict stayed approved: the current GraphAnalyzer implementation is architecturally sound, spec-complete for the implemented surface, and catalog-driven in the required dimensions.
- The only forward-looking gap is future consumption of `EventModifierMeta.RequiredAnalysis` when richer event modifiers ship.

### 2026-05-08T01:00:51Z — ProofEngine PE-G1/G2/G3 design decisions resolved
- Strategy 2 expanded into `Declaration Attribute Proof`, `ProofDischarge` was shaped to cover fixed and parameterized declaration attributes, and the `ProofLedger` output contract was confirmed as the right downstream surface.
- Remaining work shifted from blocker identification to source/spec sync and the bounded satisfiability decision that Frank-19/20 have now resolved.

## Historical summary through 2026-05-08T00:56:00Z

- The R4 review line and advisory follow-through locked the GraphAnalyzer baseline: structural diagnostics 109/110/111 were added, the stale appendix numbering was corrected, required tests landed, and the only deliberate future-touch item is `EventModifierMeta.RequiredAnalysis` for richer modifiers.
- The comprehensive language/compiler review line synchronized `catalog-system.md`, `precept-grammar.md`, and downstream design docs while preserving the catalog-driven architecture rule: metadata lives in catalogs, parser/type checker boundaries stay explicit, and docs must track implementation in the same pass.
- Earlier work established the canonical TypeChecker and parser trajectory: `TransitionRowOutcome` replaced the ambiguous outcome enum name, parse-time handling was split cleanly between closed vocabularies and open expressions, NameBinder took ownership of forward references, and working proposals were required to flow back into canonical docs and the decision ledger.
