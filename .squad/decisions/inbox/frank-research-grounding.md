# Decision Record: Research Grounding for compiler-and-runtime-design.md

**Author:** Frank (Lead/Architect)
**Date:** 2025-07-25

## What was reframed

Three section headings and their framing were changed from GP-compiler-negative to DSL-scale-positive:

| Old heading | New heading | Change |
|---|---|---|
| "Anti-Roslyn guidance" | "Right-sized parser patterns" | Replaced "don't do what Roslyn does" framing with "here's what works at DSL scale and why" — grounded in CEL, OPA, Dhall, Jsonnet, Pkl evidence |
| "Anti-pattern: per-construct check methods" | "Right-sized type checking: generic resolution passes" | Added CEL checker and OPA `ast/check.go` as surveyed precedent for single-pass catalog-driven type resolution |
| "Anti-pattern: serialized TypedModel" | "Lowering is restructuring, not renaming" | Added CEL `Program`, OPA rule indexes, and XState v5 as surveyed precedent for restructuring transformations in lowering |

The catalog-driven section (§2) still mentions Roslyn/GCC/TypeScript as a contrast point, but now frames them as "general-purpose compilers" and immediately pivots to what DSL-scale systems do instead — with CEL, OPA, and CUE as named examples.

## What research was used

All grounding draws from the 15-survey compiler corpus (`research/architecture/compiler/`) and the runtime evaluator survey (`research/architecture/runtime/`):

| Doc section | Surveys referenced | Systems cited |
|---|---|---|
| Catalog-driven design (§2) | compiler-pipeline-architecture-survey | CEL, OPA/Rego, CUE |
| Purpose-built (§2) | compiler-pipeline-architecture-survey, language-server-integration-survey | CEL, OPA/Rego, Dhall, Pkl, CUE |
| Parser patterns (§5) | compiler-pipeline-architecture-survey, language-server-integration-survey | CEL, OPA/Rego, Dhall, Jsonnet, Pkl |
| Error recovery (§5) | compiler-pipeline-architecture-survey | Roslyn (adapted pattern), OPA, Pkl |
| Type checking (§6) | compiler-pipeline-architecture-survey | CEL, OPA/Rego |
| Graph analysis (§7) | state-graph-analysis-survey | SPIN/Promela, Alloy, NuSMV/nuXmv, XState `@xstate/graph` |
| Proof engine (§8) | proof-engine-interval-arithmetic-survey | SPARK Ada/GNATprove, Dafny, Liquid Haskell, CBMC |
| CompilationResult (§9) | compiler-pipeline-architecture-survey | Roslyn, OPA, CEL, Dhall |
| Lowering / flat eval (§10) | runtime-evaluator-architecture-survey | CEL, OPA/Rego, Dhall, Pkl, XState v5 |
| Structured outcomes (§11) | runtime-evaluator-architecture-survey | CEL, OPA, Eiffel/DbC |
| Inspection (§11) | dry-run-preview-inspect-api-survey | Terraform, XState v5, OPA, Temporal |
| Incremental compilation (§12) | language-server-integration-survey | OPA/Regal, Dhall, Jsonnet, CEL |
| LS single-process (§12) | language-server-integration-survey | Regal/OPA, Dhall, Jsonnet, CUE |

## Gaps found

1. **Flat evaluation plans vs tree-walking.** The surveyed DSL-scale systems (CEL, OPA, Dhall, Pkl) all use tree-walk evaluation and succeed at their scale. Precept's choice of flat slot-addressed evaluation plans is a design decision for inspectability and determinism, not a pattern validated by DSL-scale precedent. The doc now explicitly flags this as a design decision rather than a researched conclusion.

2. **Proof engine bounded strategy set.** No surveyed DSL-scale system has a comparable bounded proof engine — the verified systems (SPARK, Dafny, Liquid Haskell) all use SMT solvers for general proof. Precept's four-strategy bounded approach is novel in this space. The doc now flags the tradeoff (no solver dependency, reduced coverage breadth) and anchors it in the verification survey evidence.

3. **Grammar generation from catalogs.** No surveyed system generates its TextMate grammar from the same metadata that drives parsing and type checking. This remains an ungrounded innovation claim — it is Precept-specific and has no external precedent to anchor.
