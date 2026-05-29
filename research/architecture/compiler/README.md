# Compiler Architecture Survey Corpus

External research surveys assembled as the foundation for Precept's clean-room compiler redesign. All files are raw research — no Precept-specific interpretations, no design conclusions, no implementation recommendations. The surveys document what real systems do, not what Precept should do.

## Purpose

This corpus was commissioned to fill research gaps identified before any new compiler design work begins. The gap analysis (conducted April 2026) identified that the existing prototype implementation should not be used as a reference — all design decisions must be grounded in external precedent.

## Reading order by priority

### Critical (resolve before designing any compiler component)

| Survey | Research Question | Systems Covered |
|--------|------------------|-----------------|
| [proof-engine-interval-arithmetic-survey.md](proof-engine-interval-arithmetic-survey.md) | How do real systems perform interval/range analysis, prove safety properties, and report proof obligations with attribution? | SPARK Ada / GNATprove, Frama-C EVA, Astrée, Liquid Haskell, Dafny, CBMC, Infer (InferBO) |
| [units-of-measure-dimensional-analysis-survey.md](units-of-measure-dimensional-analysis-survey.md) | How do real type systems represent compound units, check unit compatibility in arithmetic, compute result units from operator application, and handle unit cancellation? | F# Units of Measure, Boost.Units (C++), Frink, Haskell `dimensional`/`units`, Rust `uom`, JSR 354 (Java Money), Kennedy 1997/2009 |

### High (resolve before committing to type checker or evaluator design)

| Survey | Research Question | Systems Covered |
|--------|------------------|-----------------|
| [context-sensitive-literal-typing-survey.md](context-sensitive-literal-typing-survey.md) | How do compilers resolve the type of numeric literals from expression context? What happens when context is insufficient? | Haskell (GHC), Kotlin, Swift, Rust, Ada, Pierce & Turner 2000 (bidirectional), TypeScript |
| [flow-sensitive-check-placement-survey.md](flow-sensitive-check-placement-survey.md) | Where do comparable systems place flow-sensitive, value-narrowing-dependent compatibility checks relative to the base type checker, and how do they stage the resulting diagnostics? Grounds the assignment-qualifier-discharge-placement (PRE0141 keep-vs-relocate) design pass. | Roslyn nullable, Kotlin K2/FIR, Rust NLL borrowck, TypeScript CFA, occurrence typing (Tobin-Hochstadt & Felleisen ICFP 2010) |
| [exact-decimal-arithmetic-survey.md](exact-decimal-arithmetic-survey.md) | How do runtimes implement exact base-10 decimal arithmetic, handle precision propagation, and make overflow a deterministic error? | .NET System.Decimal, Java BigDecimal, IEEE 754-2008 decimal, SQL DECIMAL/NUMERIC, Python `decimal`, Rust `rust_decimal`, checked arithmetic patterns |
| [currency-precision-coupling-survey.md](currency-precision-coupling-survey.md) | Does construction or arithmetic of a money value bind precision to currency identity at the type-system level? Surveyed because F-LANG-BIZ-02 needed precedent before locking D10 doctrine (Position 1/2/3). Grounded the Position 3 (decoupled) decision in `docs/Working/compiler-readiness-plan-2026-05-24.md`. Open question #1 (boundary enforcement) promoted to **F-LANG-BIZ-09** for Phase 4+. | Joda-Money, JSR-354, Fowler PoEAA, NodaMoney, Stripe/PayPal/Square/Adyen, COBOL `PIC`, IFRS IAS 21 / US GAAP ASC 830, SQL `DECIMAL`, Hibernate/JPA |
| [proof-attribution-witness-design-survey.md](proof-attribution-witness-design-survey.md) | How do verification tools structure proof results, attribute obligations to source, and present witnesses as structured data for tooling consumption? | SPARK Ada / GNATprove, Dafny, Liquid Haskell, Infer, CBMC, Rust borrow checker, Frama-C WP |
| [type-proof-stage-contract-survey.md](type-proof-stage-contract-survey.md) | In separate-proof-stage systems, how is the type-checker↔verifier contract structured — who generates proof obligations (front-end VC-gen vs back-end self-derivation), how they cross the stage boundary, who emits diagnostics + when, patterns beyond VC-gen, and IDE/incremental consequences? Grounds the assignment-qualifier-discharge-placement (B1 self-derive vs B2 VC-handoff) design pass and the broader type↔proof-contract direction. Extends proof-attribution + flow-sensitive-check-placement + pipeline-architecture surveys. | Whiley, Dafny→Boogie, Frama-C/WP→Why3, SPARK, F*, Viper/IVL, Liquid Haskell, Rust borrowck, Roslyn nullable, Kotlin K2, GHC OutsideIn(X) |
| [state-graph-analysis-survey.md](state-graph-analysis-survey.md) | How do formal tools and compiler frameworks perform structural analysis on state graphs at compile time? What algorithms are used for reachability, dominator computation, and dead-state detection? | SPIN / Promela, Alloy Analyzer, NuSMV / nuXmv, UPPAAL, Lengauer-Tarjan / LLVM DominatorTree, XState `@xstate/graph`, SCXML validators |
| [temporal-type-hierarchy-survey.md](temporal-type-hierarchy-survey.md) | How do temporal libraries distinguish between instants, zoned datetimes, local times, dates, durations, and periods? What operations are type-safe vs. type errors? | NodaTime, java.time (JSR-310), Chrono (Rust), Python `datetime` + `pytz`/`zoneinfo`, PostgreSQL temporal types, ISO 8601 / RFC 3339 |

### Architectural skeleton (original 8 surveys — commissioned earlier)

These surveys cover pipeline architecture, diagnostic design, LS integration, compile-to-runtime handoff, dry-run APIs, outcome taxonomy, and state machine runtime APIs. Read alongside the critical/high surveys above.

| Survey | Research Question |
|--------|------------------|
| [compilation-result-type-survey.md](compilation-result-type-survey.md) | How do compilers represent compilation results — immutability, partial success, caching? |
| [compiler-pipeline-architecture-survey.md](compiler-pipeline-architecture-survey.md) | How do production compilers structure pipeline stages, IRs, and public/internal API boundaries? |
| [compiler-result-to-runtime-survey.md](compiler-result-to-runtime-survey.md) | How do compiled artifacts carry type information into evaluation at runtime? |
| [diagnostic-and-output-design-survey.md](diagnostic-and-output-design-survey.md) | How do compilers structure diagnostics — severity models, positions, fix/suggestion systems, serialization? |
| [dry-run-preview-inspect-api-survey.md](dry-run-preview-inspect-api-survey.md) | How do systems expose preview/dry-run/inspect APIs before committing state changes? |
| [language-server-integration-survey.md](language-server-integration-survey.md) | How do language servers integrate with compilers — shared-code patterns, incremental recompilation, cancellation? |
| [outcome-type-taxonomy-survey.md](outcome-type-taxonomy-survey.md) | How do systems distinguish outcome failure modes — rejection vs. constraint vs. undefined? |
| [state-machine-runtime-api-survey.md](state-machine-runtime-api-survey.md) | How do state machine runtimes expose fire, query, inspect, and direct-update APIs? |

## Coverage summary

17 surveys total. 60+ external systems documented across:

- Compiler pipeline and IR design (Roslyn, TypeScript, Rust, Kotlin K2, Swift, Go, CEL, Rego)
- Proof and verification systems (SPARK, Frama-C, Astrée, Liquid Haskell, Dafny, CBMC, Infer, GNATprove)
- Type systems (F#, Haskell, Kotlin, Swift, Rust, Ada, TypeScript)
- Numeric and decimal arithmetic (.NET, Java, Python, SQL, IEEE 754-2008)
- Money / currency / precision coupling (Joda-Money, JSR-354, Fowler PoEAA, NodaMoney, Stripe/PayPal/Square/Adyen, COBOL, IFRS/GAAP)
- Unit/dimension type systems (F# UoM, Boost.Units, Frink, Rust uom, JSR 354)
- Temporal type libraries (NodaTime, java.time, Chrono, Python datetime, PostgreSQL)
- State graph analysis (SPIN, Alloy, NuSMV, UPPAAL, LLVM, XState, SCXML)
- Language server integration (14 systems across three tiers)
- Dry-run and preview APIs (Terraform, Kubernetes, XState, Temporal, OPA, Dhall)

## Constraint

These files are permanently raw. Conclusions drawn from this research belong in design documents, not here. If a survey entry is found to be factually incorrect, it should be corrected in place with a note; it should not be replaced with Precept-specific reasoning.
