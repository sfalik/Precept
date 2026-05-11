## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs must derive behavior from durable catalog shape rather than enum-identity switch logic or parallel lists.
- Public API surfaces expose stable CLR/JSON interchange contracts; evaluator internals stay internal.
- Investigation docs can be archived once their outcomes are captured in canonical docs, proposals, or the squad decision ledger.

## Learnings

- Three shared root causes explain most typed-literal completion bugs: quote-trigger context normalization, typed-constant boundary detection at unterminated end positions, and missing recovery branches for `NumberTyping` / `AfterPlus` slot phases.
- Invoked completion inside a typed constant cannot key solely on `TriggerCharacter == null`; clients may send an empty trigger character, and peer-expression inference must step left past the active typed-constant token.
- For domain-type bounds, qualifier semantics split by qualifier axis: exact unit match for `in 'kg'`, dimension membership for `of 'mass'`; currency remains an exact-match follow-up gap shared with `default`.
- Guard placement should come from slot metadata and parser protocol reality, not helper booleans or enum-identity switches that duplicate the catalog surface.
- Documentation drift often clusters around grammar slot order and guard position; fix the canonical docs the same pass as the source change.
- Typed literals remain on the current architectural boundary: compile-time literal validation through `TypedConstantValidation`, runtime JSON lanes through `TypeRuntime<T>`, and ISO/UCUM as embedded external datasets with Precept-owned metadata.
- Durable rationale belongs in decisions/research, not in ephemeral review comments or ad hoc implementation switches.
- Interpolated typed constants are completely unimplemented: the parser skips hole tokens without creating expression nodes, and the type checker maps `TypedConstantStart` to `TypedErrorExpression` with no validation. The spec (§2.5, §3.6) requires full expression parsing and context-type validation. This is architecturally distinct from B9–B12 (which fix assignment-level post-resolution checks) — I1 means no expressions exist to check in the first place. The `{expr}` syntax also precludes using curly braces for any non-interpolation semantic inside typed constants (killed C2 Approach B).
- The type checker's `expectedType` parameter in expression resolution is advisory only — it hints numeric widening and typed constant context but does NOT enforce assignment compatibility. `ResolveAction` and `ResolveFieldExpressions` both create typed nodes without post-resolution validation, silently accepting type and qualifier mismatches. Three structural gaps exist: (1) no post-resolution type/qualifier check on assignment targets, (2) `QuantityValidator` validates UCUM syntax but is dimension-blind — `TypedConstantContext` exists for qualifier threading but is unused, (3) `TypedArgRef`/`TypedFieldRef` expression nodes strip `DeclaredQualifiers`, making variable-to-field qualifier comparison impossible at the expression tree level. Diagnostic codes `TypeMismatch` (PRE0018), `DimensionCategoryMismatch` (PRE0069), and `QualifierMismatch` (PRE0068) all exist but are never emitted in these paths. The gap also applies to money fields — it is type-agnostic.
- B6 stayed entirely in `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs`: `TryGetBinaryPeerOperandType`, `TryResolveExpressionTypeEndingAtToken`, `TryResolveParenthesizedExpressionType`, `TryResolveIdentifierType`, and `TryResolveMemberExpressionType` now thread `ImmutableArray<DeclaredQualifierMeta>` beside `TypeKind`, and `TryGetTypedConstantContext` assembles binary peer sites with `new TypedConstantContext(peerType, peerQualifiers)`.
- The event-arg path had the same drop: current-event arg resolution in `TryResolveIdentifierType` and event-member lookup in `TryResolveMemberExpressionType` both needed `DeclaredQualifiers` propagation, not just the field path.
- Regression coverage is integration-style in `test/Precept.LanguageServer.Tests/CompletionHandlerTests.cs`: trigger a space after `'100 ` in rule/when binary expressions and assert the returned completion labels hard-filter to `USD` for both field-peer and event-arg-peer cases.

## Historical Summary

- Early May work locked the typed-literal boundary, the external-data posture for ISO/UCUM, and the requirement that durable rationale live in decisions/research instead of scattered implementation branches.
- Recent batches settled the when-guard parser model, grammar/spec doc-sync rules, terminal-state diagnostic gating, and the typed-literal implementation review loop.

### 2026-05-11T08:26:04Z — X1/X2 crash triage: D26 assertion + extension JS TypeError
- Shane reported two crashes triggered by opening/editing a file with `'1 {s}'` (interpolated typed constant assigned to a quantity field).
- **X1 (P0):** `ResolveLiteral` in `TypeChecker.Expressions.cs:141` has a deferred stub for `TypedConstantStart` that returns `TypedErrorExpression` without emitting a diagnostic. D26 assertion fires at `PopulateEventHandlers` (TypeChecker.cs:592), `Debug.Assert` → `Environment.FailFast` → exit code 0x80131163. Crashes on both `DidOpen` and `DidChange`, creating a crash-restart loop.
- **X2 (P2):** Inside `vscode-languageclient` library's debounced document-sync `Delayer` — `this.task` is `undefined` when the LS process dies because the connection teardown clears the callback before the pending promise resolves. Secondary to X1; no independent fix needed.
- Fix for X1: emit an Error-severity diagnostic before returning `TypedErrorExpression` in the `TypedConstantStart` branch, following the `ResolveMissing` pattern.
- X1 is related to frank-12's interpolation triage (C2 design conflict). The deferred stub exists because interpolated typed constants aren't implemented yet, but D26's contract wasn't satisfied.
- Decision filed: `.squad/decisions/inbox/frank-crash-triage.md`. Tracker entries: X1, X2 in `docs/Working/completions-bugs.md`.

## Recent Updates

### 2026-05-11T08:32:57.386-04:00 — I1/X1 safe-stub fix shipped
- `ResolveLiteral` no longer returns a bare `TypedErrorExpression` for `TypedConstantStart`; the stub now emits an Error-severity TC diagnostic first, mirroring the `ResolveMissing` self-containment pattern.
- Reused existing `DiagnosticCode.TypeMismatch` instead of inventing a new code; the message text explicitly names the unsupported interpolated typed constant so users see a clear error and D26 is satisfied.
- Added regression coverage in `TypeCheckerTypedConstantTests` for the event-handler path and a full `Compiler.Compile(...)` non-crash path using `set q = '1 {s}'`; this closes X1 and leaves X2 closed-by-elimination.


### 2026-05-11 — C2 Approach B invalidated + interpolation bugs I1/I2/I3 triaged
- **C2 conflict confirmed:** `{expr}` interpolation syntax inside typed constants (`'...'`) fatally conflicts with UCUM arbitrary-unit curly-brace syntax (`'{widget}'`). The lexer parses `{widget}` as an interpolation hole, not a unit code. Approach B is dead. Replacement candidates noted: Approach E (square-bracket), F (prefix marker), or C-revisited (explicit declaration).
- **I1 (P1):** `ParseInterpolatedTypedConstant()` in `Parser.Expressions.cs` (line 444) skips all hole tokens without parsing expressions — unlike `ParseInterpolatedString()` which properly calls `ParseExpression()` per hole. The type checker (line 141) then maps `TypedConstantStart` to `TypedErrorExpression` with "deferred" comment. Result: no type checking of interpolated typed constant expressions at all. The spec (§2.5, §3.6) requires full expression parsing and validation. This is a type-safety hole.
- **I2 (P3):** No completions inside `{...}` holes of typed constants — the `CompletionHandler` doesn't detect interpolation-hole context inside typed constants.
- **I3 (P3):** No semantic tokens inside `{...}` holes — the `SemanticTokensHandler` has no expression nodes to walk (downstream of I1).
- Fix order: I1 → B9–B12 → I2 → I3. I1 is orthogonal to B9–B12 (different layers).
- Decision filed: `.squad/decisions/inbox/frank-interpolation-triage.md`.
- Tracker updated: `docs/Working/completions-bugs.md` — C2 updated with invalidation + alternative candidates; I1/I2/I3 filed as new entries.

### 2026-05-11T01:58:19Z — B9–B12 triage: type checker is qualifier-blind on assignments and defaults
- All four bugs share a common architectural gap: `ResolveAction` and `ResolveFieldExpressions` pass `expectedType` as an advisory hint but never validate the resolved expression's result type or qualifiers against the target field.
- B9 (bare integer → quantity): `IsAssignable(Integer, Quantity)` correctly returns false, but the result is never checked.
- B10/B11 (dimension mismatch in literals/defaults): `QuantityValidator` validates UCUM syntax only — dimension-blind. `TypedConstantContext` exists for qualifier threading but is unused.
- B12 (arg dimension mismatch): `TypedArgRef` strips `DeclaredQualifiers` from the expression tree — qualifier comparison is structurally impossible.
- Fix is three layers: (1) post-resolution type check on assignments, (2) dimension-aware quantity validation, (3) qualifier metadata on expression nodes.
- All diagnostic codes already exist (PRE0018, PRE0068, PRE0069). The gap also applies to money fields.
- Decision filed: `.squad/decisions/inbox/frank-b9-b12-triage.md`.

### 2026-05-11 — B9-B12 implementation plan authored
- Confirmed all four bugs share the same three structural deficits: (1) no post-resolution type/qualifier check in `ResolveAction` or `ResolveFieldExpressions`, (2) `QuantityValidator` is dimension-blind despite `TypedConstantContext` parameter existing, (3) `TypedArgRef`/`TypedFieldRef` expression nodes strip `DeclaredQualifiers`.
- Key line numbers confirmed: `ResolveAction` AssignAction case at lines 810–822, `ResolveIdentifier` arg-ref creation at line 549, field-ref at lines 571–572, `ResolveMemberAccess` arg-ref at line 1334, `ResolveFieldExpressions` default resolve at line 452.
- `TypedArgRef` and `TypedFieldRef` are positional records in `SemanticIndex.cs` (lines 20–33) — adding `DeclaredQualifiers` changes positional construction at ~15 call sites. Must audit all before committing.
- `DeriveUnitDimensionName` (TypeChecker.cs line 188) is `private static` and depends on two `FrozenSet<string>` constants — extraction required for `QuantityValidator` to use it. Cleanest path: `internal static` or shared utility.
- `TypedConstantContext` (TypedConstantParseResult.cs line 19) has only `PeerType` and `Operator` — no qualifier data. Extension to add `DeclaredQualifiers?` is backward-compatible via optional parameter.
- `IsAssignable` (TypeChecker.Expressions.cs line 1162) correctly returns `false` for `Integer → Quantity` — the check exists, it's just never called post-resolution in the assignment paths.
- Diagnostic codes PRE0018 (`TypeMismatch`), PRE0068 (`QualifierMismatch`), PRE0069 (`DimensionCategoryMismatch`) all exist with full metadata including examples and fault codes — zero new diagnostic infrastructure needed.
- `MoneyQuantityModifierRegressionTests` (line 138) has explicit gap-documenting tests that assert "no diagnostic" — these flip to assert diagnostics after the fix.

### 2026-05-11T05:34:40Z — B4/B5 retriage corrected the prior completion-bug closure
- Kramer's apostrophe-trigger coercion was a legitimate B1 fix for true expression/default sites, but it does not repair declaration-side qualifier literals.
- B4 (`quantity in '`) and B5 (`quantity of '`) still misroute through `TryGetEnclosingField(...)`, recover the outer `Quantity` type, and surface quantity-literal items instead of the active qualifier slot.
- Durable fix direction: qualifier-site resolution must happen before expression fallback, driven by parsed qualifier metadata / qualifier-shape slots, with concrete unit/dimension assertions replacing weak non-empty tests.

### 2026-05-11 — Empty typed-constant invocation diagnosis tightened
- The lexer/span layer was already correct for empty `''`; the real regressions were client-shape variance on invoked completion and token walks that failed to skip the active typed-constant token while recovering surrounding expression context.
- Record both null and empty trigger characters as invoked completion, and make peer-operand inference walk left past the current literal token.

### 2026-05-11T01:38:51Z — Terminal-state gating and parser follow-through are now durable
- Path-to-terminal warnings only make sense once at least one terminal state is declared, and lifecycle wording should name declared terminals explicitly.
- The paired parser/type-checker closeout also landed: non-associative operators use `meta.Precedence + 1` on the RHS, and typed constants inherit peer operand type context before the D13 bailout.

### 2026-05-10T23:31:04-04:00 — Typed-literal UX review approved the architecture and wrote Kramer's plan
- Elaine's typed-literal UX was approved as the behavioral contract: type-owned routing, qualifier-aware hard filtering, compound temporal in V1, and quiet free-form text.
- The implementation plan locked the 5-slice execution order: type branching, slot detection, qualifier threading, compound temporal continuation, and integration coverage.

### 2026-05-10T19:47:35Z — Grammar doc-fix batch durably recorded
- The comprehensive `precept-grammar.md` audit, the EventHandler trailing-ensure cleanup, and the final doc-alignment pass now live in the squad ledger.
- Durable guidance: document pre-verb `when` coverage everywhere StateEnsure / StateAction / EventEnsure / AccessMode appear, keep computed-field modifiers before `<-`, and remove dead construct-metadata claims once the source deletes them.
