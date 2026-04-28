# Runtime Evaluator Architecture Survey Corpus

External research surveys assembled as the foundation for Precept's runtime evaluator design. All files are raw research — no Precept-specific interpretations, no design conclusions, no implementation recommendations. The surveys document what real systems do, not what Precept should do.

## Surveys

| Survey | Research Question | Systems Covered |
|--------|------------------|-----------------|
| [runtime-evaluator-architecture-survey.md](runtime-evaluator-architecture-survey.md) | How do rule engines, expression evaluators, configuration languages, and state machine runtimes structure their runtime object model, evaluation strategy, fault representation, versioning, preview/inspect, result types, and constraint evaluation? | CEL (cel-go), OPA/Rego, XState v5, Temporal (.NET SDK), Dhall, CUE, Eiffel/DbC, Pkl, Drools, SPARK Ada |

## Dimensions covered

1. **Runtime object architecture** — Primary runtime unit, state model, mutability
2. **Evaluator design** — Evaluation strategy, key mechanisms
3. **Fault/error representation** — Error model, error-as-value vs. exception-based
4. **Compile-time to runtime fault correspondence** — How tightly compile-time diagnostics map to runtime faults
5. **Entity/activation versioning** — How entities or activations are versioned across time
6. **Inspect/preview architecture** — Preview/dry-run mechanisms, side-effect freedom
7. **Result type design** — Shape of evaluation results, error channels
8. **Constraint evaluation model** — Constraint style, violation collection, blame assignment

## Coverage summary

1 survey. 10 external systems documented across:

- Expression evaluators (CEL, OPA/Rego)
- State machine / workflow runtimes (XState v5, Temporal)
- Configuration languages (Dhall, CUE, Pkl)
- Rule engines (Drools)
- Contract-based systems (Eiffel/DbC)
- Formal verification (SPARK Ada)
