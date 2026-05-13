## Core Context



- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.

- Catalog metadata remains the language truth; runtime, tooling, and docs should derive from durable metadata rather than enum-identity switches or parallel lists.

- Proof, qualifier, and field-state design work must stay grounded in the shipped spec and evaluator/runtime surfaces, not inferred intent.



## Live Guidance



- `readonly` / `editable` / guarded access modes govern the Update (patch) surface; they do not restrict event-driven `set` actions unless the spec changes.

- Business-domain magnitude modifier legality is a catalog contract: fix drift in metadata and docs, not with checker-only special cases.

- Required-field constructor enforcement is now an active implementation surface: D93 and D94 are real checker obligations, while stateless construction stays outside the state-entry slice.



## Historical Summary



- 2026-05-12 through 2026-05-13 concentrated Frank's work around hover contract reviews, comma-list `StateTarget` closure, field-state-guarantees design, modifier applicability drift, constructor-gap analysis, and diagnostic coverage enforcement planning.

- Durable batch-by-batch detail now lives in `.squad/decisions.md` and `history-archive.md`; this live file keeps the research posture plus the newest conclusions other agents need immediately.



## Recent Updates

### 2026-05-13T04:32:04Z — Diagnostic coverage enforcement now has two enforced gates

- `docs/working/diagnostic-coverage-enforcement.md` now records Gate 1 emission-site coverage plus Gate 2 emitted-code test coverage as separate convention-test allow-list checks.
- Frank confirmed the current baseline is clean for Gate 2: all 83 emitted diagnostic codes are referenced in tests, so the emitted-but-untested allow-list starts empty while 7 codes remain neither emitted nor tested.




### 2026-05-13 — Field-state guarantees and constructor enforcement are the active baseline



- The v3 field-state design is now canonically D130/D131/D132, with author-language follow-up still needed for D132's shipped name.

- Frank's gap audit established that declaration metadata is not enough: D93/D94 were specified but unenforced, so prerequisite audits must verify real `DiagnosticCode` emission sites before downstream design work assumes behavior exists.

- George's Slice 9 and Slice 10/11 follow-through closed the immediate construction-risk loop: disjunction support landed, D93 now blocks no-initial-event required-field holes, and D94 now enforces required assignments on initial-event construction paths while keeping stateless precepts exempt.

- The diagnostic coverage enforcement recommendation remains a convention-test allow-list model, using literal `DiagnosticCode.{Member}` references in pipeline and catalog-emission files as the minimum coverage bar.



## Learnings



- If a design depends on an existing diagnostic, confirm the emitting pipeline stage instead of trusting `DiagnosticMeta` declarations or spec prose.

- `set` into an `omit` target-state field is the decisive field-state rule; Update access modes do not constrain Fire semantics.

- D132 is the structural complement to D94: both exist to prevent required fields from becoming present without a valid value.

- Catalog static initialization may not reach downstream catalog statics during cctor execution; reverse references must defer through `Lazy<T>`.

- **Two-gate enforcement is the right model for diagnostic coverage.** Gate 1 (emission-site exists) and Gate 2 (test reference exists for every emitted code) enforce orthogonal properties and need separate allow-lists. Gate 1 catches "declared but never emitted" gaps; Gate 2 catches "emitted but never tested" gaps — the more dangerous class because the code ships to users untested.
- **Current test suite is in excellent shape.** Source scan found zero emitted-but-untested diagnostic codes — all 83 codes with emission sites are referenced in test files. Gate 2 starts with an empty allow-list. The 7 codes with neither emission nor test are: `AmbiguousTypedConstant`, `EventHandlerDoesNotSupportGuard`, `EventHandlerInStatefulPrecept`, `OmitDoesNotSupportGuard`, `OutOfRange`, `PreEventGuardNotAllowed`, `RedundantAccessMode`.
- **D94 false positive was a row-scoping bug, not a design gap.** The construction guarantee (D94) applies only to transition rows whose `FromState` is an initial state — lifecycle rows for the initial event are not construction paths. The fix (`4c567cdc`) is correct and matches the v3 design spec. Two defense layers exist: (1) `NeedsInitialEvent` excludes fields omitted in all initial states, (2) the row filter excludes non-initial-state rows. The D94/D132 boundary is clean: D94 covers construction-time field population; D132 covers lifecycle omit→non-omit crossings.
- **Stateless precepts with initial events are a latent D94 gap.** `ValidateConstructionGuarantees` bails out when `ctx.States.Count == 0`, leaving stateless precepts with `event X initial` and required fields unchecked by both D93 and D94. Low priority (rare pattern) but should be tracked.
- **MCP "stale build" is a restart issue, not behavioral divergence.** The MCP launch script (`start-precept-mcp.js`) rebuilds from source on launch. Apparent discrepancies between MCP and LS are always stale cached runtimes — restarting resolves them. No code-level divergence exists.
- **Gate 2 is a tripwire, not a proof of correctness.** It verifies `DiagnosticCode.X` literal presence in test files but cannot distinguish behavioral assertions (`CheckExpectingError`) from catalog-only references (`DiagnosticsTests` metadata checks) or enum iterations. All 83 emitted codes pass Gate 2 today, but the limitation must be documented so implementers know the minimum bar is higher than what the gate enforces.
- **Negative tests are non-negotiable for gap closure.** A diagnostic that fires on valid input is a false positive — worse than a missing diagnostic in a domain-integrity product. Every gap closure test must include at least one `CheckExpectingClean` negative case alongside the `CheckExpectingError` positive case. The CI tests (`TypeCheckerCITests.cs`) set the standard: every violation test has a companion no-diagnostic test.
- **Test file placement follows domain clustering, not pipeline stage.** New gap tests go in domain-specific files (`TypeCheckerCurrencyUnitTests.cs`, `TypeCheckerCollectionSafetyTests.cs`) or extend existing domain files (`TypeCheckerTypedConstantTests.cs` for temporal, `TypeCheckerStructuralTests.cs` for choice). The naming convention is `{Condition}_{InputDescription}_{ExpectedOutcome}` — PascalCase, no `Test_` or `Should_` prefix.

### 2026-05-13T04:52:18Z — Diagnostic gap docs now lock the negative-test quality bar

- `docs/Working/diagnostic-gap-analysis.md` now records the D94 row-scoping review, targeted regression coverage recommendations, and the low-priority stateless-initial-event enforcement gap.
- `docs/Working/diagnostic-coverage-enforcement.md` now adds the team quality bar: each gap closure needs both `CheckExpectingError` and `CheckExpectingClean`, because Gate 2 alone cannot prove behavioral coverage.
