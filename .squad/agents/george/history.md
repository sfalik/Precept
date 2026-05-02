## Core Context

- Owns code-level feasibility, runtime implementation detail, and architecture-to-code translation across parser, checker, analyzer, and tooling surfaces.
- Co-owns language research/design grounding with Frank and converts approved language decisions into implementable parser/catalog structures.
- Historical summary: closed catalog extensibility hardening, PRECEPT0018 analyzer enforcement, parser whitespace-insensitivity, typed constants, event-handler ensure guards, presence-operator Pratt support, the expression-form catalog/annotation bridge, list literals, method calls, the sample/coverage regression layer, and Phase 2a+2b of the parser-gap fix plan (GAP-A/B/C + OperatorMeta DU restructure).
- Current ownership: GAP-019a (`InvalidCallTarget`) and GAP-019b (`UnexpectedKeyword`) diagnostic emission now implemented and tested.

## Learnings

### 2026-05-02 — Type Checker Design Review (Frank's analysis)

- **`Operations.BinaryIndex` / `UnaryIndex` / `FindCandidates` / `FindUnary` already exist.** Frank proposed `BinaryBySignature`/`UnaryBySignature` as new additions — they are already implemented under these names. Critical difference: `BinaryIndex` returns `BinaryOperationMeta[]` (array), not a single entry. Money/money and quantity/quantity disambiguation requires multi-candidate handling in the checker.
- **SemanticIndex record type definitions must be committed BEFORE any slice implementation begins ("pre-Slice 0").** Without the full record shape in place, no slice test can compile.
- **7 AST expression node types missing from Frank's Resolve pseudocode:** `IsSetExpression`, `IsNotSetExpression`, `CIFunctionCallExpression`, `MethodCallExpression`, `InterpolatedStringExpression`, `InterpolatedTypedConstantExpression`, `TypedConstantExpression`. All exist in `SyntaxNodes/Expressions/`. The core Resolve function is ~250-350 lines, not ~100.
- **GAP-032 (`pow` ProofRequirement) was fixed 2026-05-02** — Frank flagged it as an open gap but it is already closed.
- **Field ordering in SemanticIndex:** `ImmutableDictionary<string, TypedField>` loses declaration order. Preferred pattern: `ImmutableArray<TypedField>` primary + derived `FrozenDictionary` secondary (same pattern as `Functions.ByName`).
- **`[HandlesCatalogMember]` stub migration:** Each slice implementing a real expression form must REMOVE the annotation from the stub `CheckExpression()` and re-apply it to the real handler. Omitting this causes PRECEPT0019 duplicate-coverage diagnostics.
- **`TypedInputAction.SecondaryExpression` needs a role discriminator** (Index / Key / Priority) — a single nullable with a comment is insufficient for the Evaluator.
- **`InterpolatedStringExpression` / `InterpolatedTypedConstantExpression` have no slice in Frank's plan.** Assign to Slice 8 or define as Slice 8a.
- **`ContentValidation` shape for Gap 1 needs a DU** — a flat `string Pattern` hides a per-type switch between regex, NodaTime parse-delegate, and closed-set membership validation strategies.

- Spec grammar, parser enforcement, docs, tests, and samples must all agree before a slice is considered complete.
- Durable language truth belongs in catalogs/metadata; avoid hardcoded parallel tables in parser or tooling code.
- Shared record or catalog signature changes require a full construction-site and call-site audit, including dead-code exhaustive switch arms.
- Pratt handlers for tokens that are not in `OperatorPrecedence` must be inserted before the `TryGetValue` guard or they are silently swallowed.
- Use compiler/analyzer exhaustiveness intentionally: CS8509 for switch coverage, PRECEPT0007 for catalog metadata, PRECEPT0019 for handler coverage.
- `HandlesCatalogExhaustivelyAttribute` must allow `Struct` targets for `ref struct` handlers, and `ref struct` types still cannot own static fields.
- Verify real `TokenKind` names before writing metadata or tests; `dotnet build -q` can hide actionable diagnostics during failure analysis.
- Multi-token presence operators are a proposal-scale catalog completeness gap, while keyword member names like `.min` / `.max` need dedicated parser handling in the `Dot` path.
- **`TypeChecker` and `GraphAnalyzer` are both `public static class`** — all stub methods must be `private static`. Reflection test must include `BindingFlags.Static` to find them, and the existing `ContainSingle` assertion must expand to `HaveCount(3)` once all three pipeline stages are annotated.
- **ExpressionFormCoverageTests split**: the existing `test/Precept.Tests/ExpressionFormCoverageTests.cs` is the Layer 3 reflection+round-trip file (Slice 13); the new `test/Precept.Tests/Language/ExpressionFormCoverageTests.cs` is the Layer 2 per-kind catalog assertions (Slice 25) — different namespaces, no class name conflict.
- **PRECEPT0019 promotion sequence**: annotate all consuming pipeline classes FIRST (verify zero PRECEPT0019 fires), THEN flip severity to Error and remove WarningsNotAsErrors. The pre-condition check is non-negotiable.
- **PRECEPT0023c analyzer invariant**: `ByTokenSequence` is keyed by the full `(TokenKind, TokenKind?, TokenKind?)` tuple — NOT by the lead token alone. `IsSet=[Is,Set]` and `IsNotSet=[Is,Not,Set]` share the lead token `Is` but are structurally valid because their full sequences differ. An analyzer checking "no two MultiTokenOps share the same lead token" will fire false positives on this real catalog pattern. Always key the uniqueness check on the full sequence, not the first element.



- **Catalog-derived set for keyword-as-function-name tokens**: The correct pattern for `ParseAtom` keyword-function ambiguity is a pre-switch check against a `FrozenSet<TokenKind>` derived at startup from `Functions.All` ∩ `Tokens.Keywords`. This avoids hardcoded switch arms and auto-extends to future functions whose names collide with keywords. The check must precede the switch entirely (not use goto-case or fallthrough) to avoid code duplication.
- **Edit no-ops in shared worktrees**: When working on a shared branch, a file may already carry the expected changes from a parallel commit. My `edit` to `Parser.Expressions.cs` was a no-op because `ea18430` (GAP-031) already included the ParseAtom restructure. Only `Parser.cs` was actually new. Verify with `git diff HEAD` before committing to confirm the actual change scope.



- **RS1030 pattern**: When an analyzer follows a field reference to its declaring syntax to get operations (e.g., checking if a shared array is empty), use `ctx.SemanticModel` if `syntaxNode.SyntaxTree == ctx.SemanticModel.SyntaxTree`, and return early (assume non-empty) if not — never call `compilation.GetSemanticModel()` inside an analyzer. The `CompilationStartAction` wrapper is only needed when per-compilation state must be built up; if the only use was to capture `compilation` for `GetSemanticModel`, remove it entirely and register the `OperationAction` directly.
- **RS1030 + CatalogEnumNames**: `ConstraintKind` and `ProofRequirementKind` cannot be added to `CatalogEnumNames` until their `GetMeta` switches drop the discard arm; adding them prematurely causes PRECEPT0007 to fire on those fallback arms. Track this as a Phase 3 prerequisite via a TODO comment.

## Learnings (continued)

- **Dapr Actor model maps tightly to Precept's execution semantics.** Dapr's turn-based single-threaded access per actor ID is exactly what Precept's immutable-Version, single-event-at-a-time model requires. No concurrent writes to the same instance are possible by construction.
- **The compiled `Precept` definition must be cached per pod, not per actor.** The `Precept` object is "thread-safe, shareable, cacheable" by design — mirroring CEL's Program. In a Dapr deployment, a static `ConcurrentDictionary<string, Precept>` keyed by definition hash per pod is the correct caching strategy. Recompiling per actor activation would be expensive and pointless.
- **JSON deserialization into `object?` is a silent correctness trap.** Precept field values travel as `IReadOnlyDictionary<string, object?>`. When Dapr state stores roundtrip these through JSON, System.Text.Json returns `JsonElement` for number and boolean fields — not `int`, `bool`, `decimal`. Guard expressions evaluated against these will silently fail or produce wrong results. A typed deserialization shim keyed from `FieldDescriptor.Type` is required before guards can be trusted.
- **Dapr Workflow is the wrong model for Precept instances.** The replay/event-sourcing model is designed for orchestration workflows that span multiple services and need unbounded history. Precept instances need a compact current-state snapshot, not a growing event log. The mismatch is fundamental: Precept evaluates against a point-in-time state; Dapr Workflow reconstructs state by replaying the full event sequence from the beginning.
- **State store model is viable but requires external concurrency protection.** OCC via ETags catches concurrent writes but doesn't prevent the race — it just forces a retry. For semantically correct single-event-at-a-time processing, actor placement (which pins an instance to one node at a time) is strictly better than hoping OCC retries are rare.
- **Actor reminders can drive time-based Precept transitions**, because reminders persist across actor deactivation and fire even on cold-start. A reminder registered with the event name as its identifier can call `Fire(eventName)` on reactivation.
- **The `Restore()` contract is the Dapr-state boundary.** Every actor activation that pulls state from the store should call `Restore(stateName, fieldDictionary)` before any `Fire()` or `Update()`. `Restore` validates the persisted data against the current definition, handles schema evolution, and recomputes computed fields. Skipping it is a correctness defect.

- **Named `ParameterMeta` constants are required for proof obligations** — the same instance must appear in both the overload's `Parameters` list and the `NumericProofRequirement`'s `ParamSubject` to satisfy PRECEPT0005 reference-equality. Inline `new(...)` construction at call sites will silently create two distinct instances and break the proof engine's subject-resolution. Always define a `private static readonly ParameterMeta P<Name>` constant and share it.

## Recent Updates

### 2026-05-02 — Historical Summary (compacted)
- Archived older update entries to `history-archive.md` until `george` history returned to the active-size target.
- Durable themes retained in active history: current ownership, catalog-driven parser/runtime rules, and the newest implementation/audit outcomes.
- Use the archive for the full slice-by-slice closeout trail and earlier review context.

### 2026-05-02 — Active implementation snapshot
- Detailed GAP-032, Iteration 8 audit, GAP-019 implementation, and rename-shipping notes were moved to `history-archive.md` to bring active context back under the size gate.
- Active durable baseline: `pow(integer, integer)` proof requirements are fixed, GAP-019a/019b are shipped, and the Iteration 8 audit still defines the remaining parser catalog-derivation follow-ons before real TypeChecker/Evaluator implementation begins.
- The newest type-checker-design review adds the current checker guardrails: pre-Slice 0 `SemanticIndex` shapes, array-primary field ordering, existing `Operations` multi-candidate lookup usage, `SecondaryRole` stamping, and disciplined `[HandlesCatalogMember]` stub migration.
