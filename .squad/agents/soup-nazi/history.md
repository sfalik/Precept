## Core Context

- Owns test discipline, validation strategy, and compile/runtime verification for the DSL and surrounding tools.
- Samples and hero candidates should be validated against the real compiler/runtime before the team treats them as canonical.
- Coverage work should record meaningful gaps, not just raw counts; behavior claims need executable proof.

## Recent Updates

### 2026-04-10 - Hero candidate DSL validation
- Compile-validated the hero candidate set with precept_compile and separated valid examples from advisory failures.
- Key learning: a hero sample is not real until the engine accepts it without caveat.

### 2026-04-10 - Test refresh coverage analysis
- Reviewed current test/project coverage and documented where follow-up strengthening would matter most.
- Key learning: coverage summaries are useful only when they identify the specific behavioral lanes still at risk.
