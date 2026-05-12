# Squad Decisions

---

## ACTIVE DECISIONS — Current Sprint

---

### 2026-05-12T23:02:04Z: Field-state guarantees v2 stays approved only after omit-state resolution, broadcast compatibility, and the real test matrix are locked

**By:** Scribe

**Status:** Merged and reconciled from Frank's design review and Soup Nazi's test-plan audit.

**Merged sources:** `frank-v2-design-review.md`, `soup-nazi-v2-test-review.md`.

- Implementation cannot start until omit declarations resolve `StateTarget` before unifying into `AccessModes`, `NameBinder` iterates every field in a multi-field target, and broadcast compatibility keeps a stable `all` identity instead of collapsing `.FieldName` to null.
- The structural/conditional split, pipeline ordering, and D130-D134 envelope remain approved, but the plan's real test surface is much larger than advertised: event handlers, wildcard/self-loop multiplicity, diagnostics stage lists, and broadcast regressions all need explicit anchors.
- Open questions that still block exact tests are now durable team memory: D132 baseline semantics for unmentioned fields, OR-disjunct handling in access conditions, wildcard diagnostic multiplicity, and self-loop D130/D133 double-report behavior.

---

### 2026-05-12T23:02:04Z: Comma-list `StateTarget` spike is architecturally approved, but parser/test closure and count reconciliation remain open

**By:** Scribe

**Status:** Merged and reconciled from Frank's spike review and Soup Nazi's test-gap audit.

**Merged sources:** `frank-spike-review.md`, `soup-nazi-spike-review-gaps.md`.

- Frank approved commit `a63d88b4` on architecture: parser disambiguation stays catalog-derived, `ResolveStateTargets` remains the single normalization path, expansion is pure-copy, and the grammar already accepts comma lists.
- The follow-up gate is test completeness, not design shape: parser AST coverage still needs 2-name, 3+-name, whitespace, and trailing-comma anchors, and expansion coverage must assert cloned guard/action/outcome semantics plus multi-unknown-state diagnostic fan-out.
- Published validation counts must match real output; the durable tester note is that current `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-build --nologo` reports `4962`, so any `4966` claim needs reconciliation before handoff.

---

### 2026-05-12T23:02:04Z: George's B1 hover fix pass landed the compact-card contract and cleared the targeted language-server suites

**By:** Scribe

**Status:** Merged from George's hover-fix closeout note.

**Merged source:** `george-b1-fixes.md`.

- Commit `c2a38a56` replaced verbose proof-gap hover blocks with the compact qualifier/proof cards, locked the shared badge vocabulary to `✅ Proven`, `⚡ Enforced`, and `⚠️ Gap`, and normalized shipped copy from `proved` to `proven`.
- `HoverHandler.cs` required no routing change; the fix stayed inside `RichHoverFactory.cs` and matching `HoverHandlerTests.cs` expectations.
- Validation closed green at `41/41` `HoverHandlerTests` and `269/269` full language-server tests, while repo-wide `dotnet test` still hits the unrelated multi-unknown-state baseline in `TypeCheckerTransitionTests`.

---

### 2026-05-12T23:02:04Z: Hover B1 review locked the compact-card proof-hover contract before the fix pass

**By:** Scribe

**Status:** Merged from Frank's pre-fix blocked review.

**Merged source:** `frank-b1-review.md`.

- Frank's blocker list made the contract explicit: qualifier proof diagnostics, qualifier proof expressions, and qualifier declarations all had to use the compact 3-line card shapes from `docs/Working/hover-design.md` instead of the older forensic sections.
- The review also locked the exact wording surface: `✅ Proven`, `⚡ Enforced`, `⚠️ Gap`, plus `proven` instead of `proved`, with transition hover using `Gap:` rather than `Proof gap:`.
- Routing itself was not the problem: proof-first dispatch in `HoverHandler` and the rich proof/qualifier precedence inside `RichHoverFactory` were already judged structurally correct; the rendered copy and stale red tests were the blocked surface.

---

### 2026-05-12T22:25:28Z: Language docs and README now match shipped comma-list `StateTarget` behavior

**By:** Scribe

**Status:** Merged from Frank's docs-closeout note.

**Merged source:** `frank-s6-docs-done.md`.

- `docs/language/precept-language-spec.md`, `docs/language/precept-grammar.md`, `docs/compiler/parser.md`, `docs/compiler/type-checker.md`, `docs/compiler/name-binder.md`, and `README.md` now describe `StateTarget := Identifier ("," Identifier)* | any` while keeping `EventTarget` single-name.
- The synced docs now record pure-copy expansion semantics plus per-name span/resolution behavior for comma-list state targets, and the parser docs now describe the shipped variable-offset state-scoped disambiguation scan.
- `docs/language/catalog-system.md` and `docs/compiler/diagnostic-system.md` were explicitly verified as already accurate for the shipped comma-list subset and needed no further change.

---

### 2026-05-12T22:39:45Z: Field-state guarantees v2 will enforce structural access violations in the TypeChecker and conditional access through proof obligations

**By:** Scribe

**Status:** Merged from Frank's late inbox note.

**Merged source:** `frank-field-state-v2.md`.

- Enforcement is now explicitly split by certainty: omit/unconditional-readonly violations stay in a new TypeChecker validation pass, while guarded-editability checks become a new `ProofRequirementKind.AccessCondition` path in the ProofEngine.
- `FieldTargetSlot` must become multi-name so access-mode and omit declarations stop dropping fields 2..N from comma-separated targets, and omit declarations should feed `ctx.AccessModes` as `ModifierKind.Omit` rather than living on a disconnected enforcement surface.
- The design also locks the diagnostic envelope for this work: activate existing D42/D43 declaration checks and add D130-D134 for structural write/read failures plus unproved access conditions.

---

### 2026-05-12T22:25:28Z: Language-spec audit locks a targeted cleanup for stale references, overclaimed contracts, and incomplete diagnostic coverage

**By:** Scribe

**Status:** Merged from Frank's audit note.

**Merged source:** `frank-spec-audit.md`.

- Frank confirmed several high-confidence spec defects in `docs/language/precept-language-spec.md`: the dead `docs/PreceptLanguageDesign.md` grounding reference, stale "not yet built" pipeline wording, a vestigial open-questions placeholder, misleading `C48`-style labels, six `ConstructKind` names that do not match code, and an incomplete diagnostic-groups table.
- The audit also preserves the owner-decision boundary: §0.5 and §0.6 currently overclaim shipped behavior if they are meant to describe only implemented guarantees, and ProofEngine Strategy 6 remains undocumented unless the owner wants it promoted to public documentation.
- Frank ranked the immediate cleanup around preamble/status accuracy, replacing `C48`-`C52` notation with real diagnostic names, and explicitly qualifying which design-contract items are future or partial.

---

### 2026-05-12T22:18:18Z: Construct metadata now describes `StateTarget` as a single state, `any`, or a comma-delimited state list

**By:** Scribe

**Status:** Merged from George's manifest closeout; inbox artifact was absent, so the ledger was updated directly from the spawn manifest.

- `src/Precept/Language/ConstructSlot.cs:18` now documents `ConstructSlotKind.StateTarget` as accepting a state name, `any`, or a comma-delimited list of state names.
- `src/Precept/Language/Constructs.cs:29` now gives the shared `SlotStateTarget` description the same list-capable wording, keeping catalog-facing construct metadata aligned with the shipped parser and type-checker behavior.
- George reported the S5 catalog wording pass complete and confirmed the validation build passed.

---

### 2026-05-12T22:18:18Z: User model directive locks claude-opus-4.7 behind explicit permission while keeping claude-opus-4.6 available under normal rules

**By:** Scribe

**Status:** Merged from Shane's directive notes.

**Merged sources:** `copilot-directive-opus47.md`, `copilot-directive-opus46-ok.md`.

- The hard rule is now durable team memory: no one uses `claude-opus-4.7` unless Shane explicitly authorizes it for the session or task.
- The clarification also locks the non-ban boundary: `claude-opus-4.6` remains available for complex work under the existing model-selection policy and is not part of the prohibition.
- Model-selection guidance should therefore treat the directive as a surgical ban on `claude-opus-4.7`, not a general no-opus policy.

---

### 2026-05-12T22:18:18Z: Comma-delimited state-target lists now ship end-to-end across parser, type checker, normalization, and diagnostics

**By:** Scribe

**Status:** Merged, deduplicated, and reconciled across George's S1/S2/S3/S4 notes.

**Merged sources:** `george-s1-parser-done.md`, `george-s2-tc-done.md`, `george-s3s4-done.md`.

- `ParseStateTarget()` now accepts `any` or comma-delimited state-name lists, preserves per-name spans, and parser disambiguation scans past those lists before resolving state-scoped constructs.
- Transition-row normalization expands one typed row per named source state while preserving undeclared names as explicit per-name diagnostics instead of collapsing them into wildcard semantics.
- The remaining state-target normalization passes are now aligned: ensures, access modes, and state hooks each expand per state-list entry, while omit declarations stay unchanged because they do not consume `StateTargetSlot`.
- Two dedicated diagnostics close the authoring contract: `StateListContainsWildcard` rejects mixed named-state plus `any` lists, and `DuplicateStateInList` warns on duplicate names before deduplicated expansion proceeds.

---

### 2026-05-12T22:18:18Z: Canonical samples now demonstrate comma-list state syntax only where semantics stay identical

**By:** Scribe

**Status:** Merged from Frank's sample-closeout note.

**Merged source:** `frank-s7-samples-done.md`.

- `samples/hiring-pipeline.precept`, `samples/it-helpdesk-ticket.precept`, and `samples/utility-outage-report.precept` now use comma-list source states for the pure-copy transitions George's runtime/parser work actually supports.
- Frank deliberately left rows expanded anywhere guards, actions, or outcomes diverged, preserving the spike ruling that comma lists are syntactic sugar for identical rows rather than a semantic broadening of transition behavior.
- Validation for the three edited samples stayed clean through VS Code diagnostics even though MCP `precept_compile` was unavailable during the pass.

---

### 2026-05-12T22:18:18Z: Hover V1 now routes construct cards first, reports only unconditional mutability truth, and exposes graph-edge proof gaps through StateGraph metadata

**By:** Scribe

**Status:** Merged and deduplicated from Kramer's hover-closeout notes.

**Merged sources:** `kramer-b2-b3-routing-and-mutability.md`, `kramer-b4-edge-proof-status.md`, `elaine-b4-design-doc-update.md`.

- `HoverHandler.cs` now lets construct-span cards beat generic operator/function/accessor fallbacks while preserving the existing proof-first behavior and identifier-driven symbol hovers where they are still the honest trigger surface.
- `RichHoverFactory.cs` now limits writable counts and state mutability summaries to unconditional `AccessModes`, omitting guarded access from V1 mutability claims instead of synthesizing misleading read-only complements.
- `StateGraph` now carries edge proof-status metadata projected from unresolved transition proof obligations, allowing state hover to explain unproven graph edges without re-joining proof data inside the language server.
- `docs/Working/hover-design.md` is now synced to the shipped B4 state-proof narrative, including the `📍` / `✅ Proven` / `⚠️ Gap` badge vocabulary and the fact that B4 appends to the rich state hover instead of shipping as a standalone hover kind.
- Kramer's B2/B3/B4 validation stayed green on the full build plus core and language-server test suites (`4966` core tests, `281` LS tests).

---

### 2026-05-12T15:15:10Z: Proof hover spec is now consolidated into `docs/Working/hover-design.md`

**By:** Scribe

**Status:** Merged from Elaine's inbox note.

**Merged source:** `elaine-hover-merge.md`.

- Elaine merged the proof-hover design into `docs/Working/hover-design.md`, replacing the old standalone proof-hover draft with the canonical hover spec.
- Qualifier hover now uses the proof-aware Scenario 4 card, and the working doc carries explicit scenarios for qualified fields, proof-bearing binary expressions, and proof diagnostic squiggles.
- Hover routing, precedence, proof data-shape requirements, and proof-specific open questions now live in one maintained design surface instead of split working docs.

---

### 2026-05-12T15:15:10Z: Remaining `inventory-item.precept` proof fallout splits into shipped G1, BUG-C event-arg qualifiers, and deferred algebraic G2

**By:** Scribe

**Status:** Merged from Frank's inbox note.

**Merged source:** `frank-proof-coverage-expansion.md`.

- Frank closed the root-cause triage: RC1 was the compound-unit qualifier bug in `ResolveQualifierFromInterpolatedConstant`, RC2 is blocked on BUG-C because unqualified `exchangerate` event args cannot carry `in ... to ...` metadata yet, and RC3 is a later algebraic proof-composition problem.
- The decision explicitly reframed F4 as already implemented in runtime semantics; the remaining ReceiveShipment currency fallout is a data-shape gap, not a missing `CurrencyConversion` policy.
- G1 stayed the immediate slice because it clears four diagnostics surgically; G2 remains deferred until BUG-C exposes qualifier data on event args.

---

### 2026-05-12T15:15:10Z: Compound-unit interpolated constants now resolve full `{A}/{B}` qualifiers before denominator fallback

**By:** Scribe

**Status:** Merged from George's inbox note.

**Merged source:** `george-g1-compound-unit-fix.md`.

- George fixed `ResolveQualifierFromInterpolatedConstant` so typed constants carrying both numerator and denominator unit slots build the full compound qualifier string instead of collapsing to the denominator.
- The G1 pass shipped in commit `cb4fbf57`, kept `StaticMagnitude` on the typed interpolated constant node, and reused trusted positive-rule proofs so downstream nonzero obligations can discharge from the cleaned qualifier evidence.
- RC1 fallout is cleared in `samples/inventory-item.precept`: the PRE0114s at plan lines 122/123 and the cascading DivisionByZero diagnostics at lines 137/142 are gone, with docs/history synced in `1ee54bdb`.

---

### 2026-05-12T15:15:10Z: Proof hover ships honest fallback reasons until compile-time proof exposes a stable failure-reason payload

**By:** Scribe

**Status:** Merged from Kramer's inbox note.

**Merged source:** `kramer-hover-gap-proof-reason.md`.

- Kramer recorded that `Compilation.Proof` already gives hover enough truth for verdict, requirement, operands, qualifiers, context, and fix hints, but not a stable unresolved-reason payload for the `Reason:` line.
- The shipped hover design therefore prefers explicit heuristics over invented precision when proof failure reasons are not surfaced directly from the compile-time ledger.
- Elaine v4 hover implementation landed in commits `5ab6030e`, `516aa6ba`, and `7829e9c6`, and `264/264` `Precept.LanguageServer.Tests` passed with the current honest-fallback approach.

---

### 2026-05-12T13:52:04Z: Proof diagnostic root cause is missing same-qualifier operation metadata, not a new proof strategy gap

**By:** Scribe

**Status:** Merged from Frank's inbox note.

**Merged source:** `frank-expression-qualifier-diagnostic.md`.

- Frank isolated the nested `(A - B) - C` PRE0114 failures to a catalog input gap: same-qualifier `+/-` operations lacked `Match: QualifierMatch.Same`, so intermediate `TypedBinaryOp` results did not advertise inherited qualifiers and the existing recursive proof path never activated.
- The approved fix stays metadata-driven and surgical: add `Match: Same` to the six same-qualifier money/quantity/price arithmetic operations and cover nested regressions; no provenance redesign or new proof algorithm is required.
- The message UX follow-up remains independently valuable: `ProofEngine` should describe subexpressions recursively and show resolved qualifier values instead of `<expression>` placeholders so legitimate qualifier failures explain the real mismatch.

---

### 2026-05-12T13:52:04Z: Same-qualifier arithmetic metadata and PRE0114 operand labeling are now fixed together

**By:** Scribe

**Status:** Merged from George's inbox note.

**Merged source:** `george-proof-diagnostic-fix.md`.

- George added `Match: QualifierMatch.Same` to the six same-qualifier money/quantity/price `+/-` operations so nested arithmetic results now retain the proved qualifier contract the checker and proof engine already know how to recurse through.
- `ProofEngine.CreateDiagnostic(...)` now describes full expressions recursively and attaches resolved qualifier values to PRE0114 operand labels, replacing placeholder fallbacks on both the computed-expression and collection-access paths.
- The fix shipped in commit `d187230c` with new proof-engine and operation-catalog regressions, and George reported the full suite green at `5507/5507`.

---

### 2026-05-12T13:52:04Z: Proof-stage diagnostics and hover need operand truth, repair guidance, and dedicated routing

**By:** Scribe

**Status:** Merged from Elaine's inbox note.

**Merged source:** `elaine-proof-ux-audit.md`.

- Elaine's proof UX audit locked the core teachable-moment failures: `<unknown>`-style placeholders are never acceptable, qualifier diagnostics must show the actual conflicting values, and human repair guidance must be paired with structured args rather than baked into sentence fragments.
- The audit also established that proof hover is a routing problem as much as a content problem: generic operator and transition hover frequently wins before authors ever see proof context.
- The durable design split is now explicit: declaration-contract hover, expression-proof hover, and diagnostic-squiggle hover are separate UX jobs and should not be collapsed into one generic card.

---

### 2026-05-12T13:52:04Z: Proof diagnostics now use explicit 'Cannot prove…' wording with structured qualifier payloads

**By:** Scribe

**Status:** Merged from George's inbox note.

**Merged source:** `george-proof-message-rewrites.md`.

- George implemented Elaine Section A's proof-message rewrites in commit `1d8962f7`, including the six-argument PRE0114 shape that carries operand labels, axis, context clause, and left/right qualifier values separately.
- PRE0112, PRE0113, PRE0115, PRE0116, PRE0082, PRE0083, and PRE0084 now use clearer author-facing wording while keeping the structured diagnostic contract intact for tooling and AI consumers.
- The batch updated proof-engine and exact-message regression coverage, synced the proof/diagnostic runtime docs, and stayed green on the targeted core path at `4914/4914`.

---

### 2026-05-12T13:52:04Z: Proof hover working spec is filed for Shane sign-off before implementation

**By:** Scribe

**Status:** Merged from Elaine's inbox note.

**Merged source:** `elaine-hover-design-filed.md`.

- Elaine wrote `docs/working/proof-hover-design.md` as the canonical working spec for proof-hover UX before Kramer starts implementation work.
- The doc covers precedence failures in the current hover stack, scenario-specific card requirements, routing rules, and the proof-evidence data shape the implementation must have available.
- Status is now durable: the hover design is ready for Shane review and annotation, not for silent implementation drift.

---

### 2026-05-12T05:04:03Z: MCP hybrid rollout requires scoped reference tools

**By:** Scribe

**Status:** Merged from Frank's inbox note.

**Merged source:** `frank-mcp-context-window.md`.

- Frank measured the current MCP payloads and found the legacy aggregate `precept_language` surface is about `401 KB`, far beyond a safe routine tool-result budget.
- The approved hybrid direction still stands, but Newman must add `scope` filtering to `precept_types` and `precept_domains` and keep `precept_operations` filter-first.
- The pre-ship rule is explicit: do not preserve `precept_language`, even as a hidden compatibility fallback.

---

### 2026-05-12T05:04:03Z: DTO-free MCP architecture analysis confirms hybrid curated projection

**By:** Scribe

**Status:** Merged from Frank's inbox note.

**Merged source:** `frank-mcp-dto-free-design.md`.

- Frank evaluated multiple DTO-free MCP approaches under the no-codegen constraint and rejected raw core serialization plus attribute-driven converter sprawl.
- The accepted direction keeps the contract curated while removing DTO type maintenance: catalog/reference tools move toward compact rendered output and only genuinely programmatic surfaces keep minimal structured JSON.
- The core ruling remains that transport shape is a deliberate MCP contract concern, not something to leak back into `src/Precept` domain types.

---

### 2026-05-12T05:04:03Z: DTO-free MCP architecture is implementation-ready for Newman

**By:** Scribe

**Status:** Merged from Frank's inbox note.

**Merged source:** `frank-mcp-impl-plan.md`.

- `docs/Working/mcp-dto-free-design.md` now carries an execution-grade implementation plan covering catalog-tool string returns, formatter extraction, `precept_compile` contract reduction, cleanup scope, and test rewrites.
- The dependency ruling is locked: no `src/Precept`, language-server, or VS Code extension work is required for this implementation pass.
- Newman can start coding from the working doc without further architecture elaboration.

---

### 2026-05-12T05:04:03Z: DTO-free MCP working doc is the canonical design surface

**By:** Scribe

**Status:** Merged from Frank's inbox note.

**Merged source:** `frank-mcp-working-doc.md`.

- Frank wrote `docs/Working/mcp-dto-free-design.md` as the durable design record for the approved hybrid DTO-free MCP direction.
- The document explicitly records that Approach 4 (Hybrid) is approved, there are no known programmatic consumers of the current catalog JSON, and implementation may proceed.
- The architecture boundary stays intact: raw core serialization remains rejected and the public MCP surface stays curated.

---

### 2026-05-12T05:04:03Z: E2 and E3 qualifier fixes cut inventory-item PRE0114 to 16

**By:** Scribe

**Status:** Merged from George's inbox note.

**Merged source:** `george-e2-e3-complete.md`.

- George landed E2 in `8785d753` and E3 in `d3f5aa98`, then followed with `f4db093e` for the ReceiveShipment parenthesization fix.
- `samples/inventory-item.precept` PRE0114 count dropped from `66` to `16` after typed interpolated constant qualifier extraction, subexpression qualifier propagation, and compound-unit cancellation improvements.
- The remaining 16 PRE0114 diagnostics are deferred exchange-rate / GrossProfit fallout; two separate `TypeMismatch` sample edits remain outside the committed parenthesization fix.

---

### 2026-05-12T04:29:05Z: Diagnostic Message Fixes Implemented and Validated



**By:** Scribe



**Status:** Merged from George's inbox note.



**Merged source:** `george-diagnostic-message-fixes.md`.



- George implemented all 10 approved diagnostic-message fixes from Elaine's audit in `commit 4535aaa6`.

- Validation stayed green on the targeted path: `818/818` tests passed.

- The batch also synced proof-context formatting, removed the hardcoded `"unknown"` payload from PRE0114, and updated the proof-engine diagnostic documentation.

- The RuleIdentity follow-up remained intentionally skipped because the runtime model still lacks an author-facing label beyond `RuleIndex`.

---

### 2026-05-12T04:29:05Z: Inventory-Item PRE0114 Root Cause Analysis and Resolution Plan



**By:** Scribe



**Status:** Merged from Frank's inbox note.



**Merged source:** `frank-inventory-item-pre0114-plan.md`.



- Frank traced the remaining `inventory-item.precept` PRE0114 failures to four root causes: shared `ParameterMeta` ambiguity, missing typed-interpolated-constant qualifier resolution, missing compound/cross-type propagation, and symbolic comparison gaps between `Dimension("{X.dimension}")` and `Unit("{X}")`.

- The plan adds Part E to `docs/Working/typed-constants-and-proof-coverage-plan.md` with slices E1–E4; the recommended execution order is E1 → E4 → E2 → E3.

- One sample bug is separate from the compiler work: `ReceiveShipment` needs inner parenthesization so the intended scalar-chain grouping survives parsing.

---

### 2026-05-12T04:29:05Z: Diagnostic Message Teachable Moment Audit



**By:** Scribe



**Status:** Merged from Elaine's inbox note.



**Merged source:** `elaine-diagnostic-audit.md`.



- Elaine ranked PRE0114 (`UnprovedDimensionRequirement`) as the worst diagnostic because the emitted `"unknown"` dimension payload and double-`in` suffix hide the real fix.

- The audit recommends author-facing rewrites for PRE0113/PRE0115 and several lower-priority proof/type messages so the Problems panel names the field, the context, and the fix direction in plain DSL terms.

- The systemic context bug is the same one called out in the proof engine: `FormatContextDescription` should stop producing `in event ... in state ...` chains.

---

### 2026-05-12T04:29:05Z: Diagnostic Message Location Tag Revision



**By:** Scribe



**Status:** Merged from Elaine's inbox note.



**Merged source:** `elaine-diagnostic-message-review.md`.



- Elaine rejected dot-path location tags like `ReceiveShipment.ensure` because authors wrote `on ReceiveShipment ensure` / `in Approved ensure`, not field-access syntax.

- The recommended tag format preserves the DSL preposition and scope name (`[on ... ensure]` / `[in ... ensure]`) so event-vs-state ensures stay distinguishable.

- Structured `Args` remain part of the contract so AI agents can reconstruct the location tag without regex-parsing the rendered message.

---

### 2026-05-12T00:50:06Z: Derivation operations do not infer qualifiers on resulting `price` values



**By:** Scribe



**Status:** Merged from Frank's inbox notes.



**Merged source:** `frank-q2-derivation-no-inference.md`.



- `money ÷ quantity`, `money ÷ period`, and `money ÷ duration` produce bare `price` results; the compiler does not infer denominator qualifiers from the divisor.

- Authors who need temporal-denominated derived prices must assign into fields explicitly declared with `of 'time'` or `of 'date'`.

- The rationale is now locked as D19 in `docs/language/business-domain-types.md`: qualifier inference on derivation would violate Precept's explicit, deterministic, inspectable domain-contract model.

---

### 2026-05-12T00:50:06Z: Temporal proof-plan audit confirms Slice 11B/12 direction and closes G15 as a false gap



**By:** Scribe



**Status:** Merged from Frank's inbox notes.



**Merged sources:** `frank-plan-extension-g15.md`, `frank-spec-coverage-audit.md`, `frank-temporal-canonical-analysis.md`.



- Canonical-doc review confirmed the Slice 11B direction is additive to locked docs: temporal denominators stay on `price of ...`, `price of 'time'` / `price of 'date'` is the right extension, and `quantity of 'time'` remains invalid.

- `ImpliedQualifiers` on `TypeMeta`, temporal routing in `ExtractQualifiers`, comparable temporal values, and Dimension→TemporalDimension fallback remain the accepted infrastructure for Slice 11B.

- G15 is closed as a false gap: derivation-direction operations do not need qualifier-chain proofs because the operands share no qualifier axis, and assignment validation already enforces declared-target compatibility in the practical cases.

- The plan-status correction is durable: Slices 7–11 were confirmed already implemented; only Slice 11B and Slice 12 remain open work.

---

### 2026-05-12T00:01:51Z: Temporal price denominators stay on `price of ...`; `quantity of 'time'` remains invalid



**By:** Scribe



**Status:** Merged from Frank's inbox note.



**Merged source:** `frank-temporal-type-system-design.md`.



- Frank validated the Slice 11B direction without widening the grammar surface: temporal price denomination stays on the existing `of` preposition, so `price of 'time'` and `price of 'date'` are the additive path and no `per` keyword is introduced.

- The earlier UCUM rule remains intact: `quantity of 'time'` stays a type error even though UCUM temporal atoms remain available for compound units; authors should use `duration` or `period` for temporal semantics instead of adding `time` to `DimensionCatalog`.

- `duration` should advertise its temporal meaning through `TypeMeta.ImpliedQualifiers` so the proof/comparison pipeline can consume implied temporal-dimension metadata rather than hardcoded duration cases.

- Existing `quantity × duration` and `quantity × period` arithmetic stays valid without new proof obligations; Slice 12 chain validation should activate only for price fields that actually declare a temporal `of` qualifier.

- Open questions parked for Shane: whether `quantity in 's'` should emit a hint toward `duration`, whether `money ÷ duration -> price` should infer a temporal denominator automatically, and whether `period ×/÷ integer` should ship as a separate follow-up.

---

### 2026-05-11T20:25:57Z: DTO-free MCP catalog exposure stays off the public contract path



**By:** Scribe



**Status:** Merged from Frank's inbox note.



**Merged source:** `frank-dto-free-mcp.md`.



- Raw catalog serialization is not a viable public MCP contract as the code stands; the problem is contract shape, not basic serializability.

- The curated projection layer remains justified because it renders enums as stable strings, preserves polymorphic subtype data that abstract-base serialization drops, and normalizes transport-hostile runtime structures into AI-legible JSON.

- Frank's probe confirmed `ImmutableArray<T>` and `FrozenSet<T>` are not the blocker on .NET 10; the decisive gaps are polymorphism, enum readability, and transport-safe shaping for values like `UcumExactFactor`.

- Durable direction: keep an explicit MCP projection contract, and reduce mapper maintenance only by moving or generating that projection layer rather than exposing raw core types.

---

### 2026-05-11T20:25:57Z: Semantic-token invalidation now clears `_latestResults` so delta requests reseed from a full baseline



**By:** Scribe



**Status:** Merged from Kramer's inbox note.



**Merged source:** `kramer-semantic-tokens-invalidation-fix.md`.



- Typed-constant span changes must invalidate both semantic-token caches for the URI: removing only `_documents` leaves a stale `_latestResults` baseline behind for the next delta request.

- `SemanticTokensHandler.TryInvalidateForTypedConstantSpanChange(...)` now calls `_latestResults.TryRemove(...)` alongside the document-cache removal, so the next request returns a full payload and seeds a fresh baseline instead of diffing against missing state.

- Regression coverage now proves invalidation clears both caches and forces the next delta response down the safe full-response path.

- Kramer reported the fix closed with a successful build and green language-server tests.

---

### 2026-05-11T20:03:33Z: Interpolated typed constants keep compile-time structural guarantees; Slice 6 remains numeric with a whole-value fallback



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (9 files → 1 canonical entry; 1 duplicate source folded into the existing release-only decision).



**Merged sources:** `frank-interpolation-simplification-verdict.md`, `shane-compile-time-guarantees.md`, `frank-string-hole-proof-analysis.md`, `frank-string-hole-proof-revision.md`, `frank-s1-constraint-propagation.md`, `frank-constraint-propagation-scope.md`, `george-interpolation-loe.md`, `frank-slice6-constraint-scope.md`, `frank-slice6-plan-patch.md`.



- Shane rejected the runtime-deferred simplification verdict outright: compile-time validation is non-negotiable for interpolated typed constants, so the type-grammar / slot-classification plan remains the canonical direction and the structural diagnostics stay in scope.

- Frank's string-hole analysis now stands as a prerequisite architectural rule: typed interpolation should compose typed holes, not raw `string` holes, because qualifier resolution and proof power collapse once hole text becomes opaque runtime content.

- Frank also reversed the earlier "downstream enhancement" call on constraint propagation. Slice 6 is in scope as a one-hop proof strategy over existing semantic actions: obligation target field → interpolated assignment RHS → slot source expression → source-field modifiers via existing `SatisfactionCovers()` logic.

- The exhaustive Slice 6 scope pass tightened the boundary: propagation remains **numeric-only**. Qualifier, dimension, modifier, and presence obligations still resolve through existing declaration-driven strategies (S2/S5), so adding provenance-based propagation there would be redundant and architecturally conflicting.

- frank-6 exposed the only real gap in the magnitude-only draft: single-hole whole-value forms like `'{x}'` should inherit numeric constraints from the source field even when no magnitude slot exists. frank-7 patched the plan accordingly by renaming the helper to `GetSlotSource`, lifting the estimate to ~90 LOC / ~10 tests, and documenting why qualifier no-propagation is deliberate.

- George's LOE review remains the standing implementation warning: Slice 2 is still the cost center, and binder plus multiple language-server walkers must be updated alongside the checker so the grammar-driven runtime path and tooling surfaces do not drift.

---

### 2026-05-11T20:03:33Z: `optional` + `notempty` is now a first-class conflicting-modifier error across code, docs, and tests



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (4 files → 1 canonical entry).



**Merged sources:** `frank-optional-notempty.md`, `george-conflicting-modifiers-impl.md`, `soup-nazi-optional-notempty-tests.md`, `frank-optional-notempty-docs.md`.



- Frank established the bug: allowing `optional notempty` is logically inconsistent because `notempty`'s proof satisfactions discharge `.length` / `.count` obligations without a presence guard, which violates the Totality guarantee for values that may be absent.

- George implemented the durable fix path instead of overloading an unrelated diagnostic: `DiagnosticCode.ConflictingModifiers = 120`, a dedicated catalog entry, validator routing to the new code, and symmetric `MutuallyExclusiveWith` metadata on both `Optional` and `Notempty`.

- The symmetry note is now canonical: PRECEPT0011c enforces mutual-exclusion declarations in both directions, so a one-way catalog declaration is not buildable.

- Soup Nazi locked the behavior with four regression anchors covering field, event-arg, and collection conflicts plus the clean `notempty`-alone case.

- Frank synced the spec in the same pass: the modifier reference, modifier-validation guidance, and the validation table now all document C120 and the explicit-rule workaround (`Arg is not set or Arg.length > 0`) for "non-empty if present" semantics.

---

### 2026-05-11T00:00:00Z: Precept skills and Precept Author agent now match the actual 10-tool MCP surface



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (2 files → 1 canonical entry).



**Merged sources:** `newman-skill-mcp-sync.md`, `frank-authoring-skill-architecture.md`.



- Both Precept skills (`.github/skills/precept-authoring/SKILL.md` and `.github/skills/precept-debugging/SKILL.md`) and the Precept Author agent (`.github/agents/precept-author.agent.md`) were rewritten to reference only tools that actually exist: `precept_ping`, `precept_quickstart`, `precept_syntax`, `precept_types`, `precept_patterns`, `precept_proofs`, `precept_operations`, `precept_domains`, `precept_compile`, `precept_diagnostic`. All phantom tool references (`precept_language`, `precept_inspect`, `precept_fire`, `precept_update`) removed.

- Stale DSL syntax corrected in the same pass: `[nullable]` → `optional`, `in <State> write ...` → `in <State> modify ... editable`, `set X = null` → `clear X`.

- Frank established the durable architectural decisions: keep two separate skills (authoring = generative cognitive mode, debugging = diagnostic cognitive mode); authoring tool order is `precept_quickstart` (new session only) → `precept_patterns` (before writing) → conditional domain tools → compile loop; `precept_syntax`/`precept_types` are on-demand reference, not workflow steps; debugging is fully static (compile → `precept_diagnostic` per code → transition-table reasoning); `precept_diagnostic` is reactive in both skills; keep `precept/*` wildcard in the agent definition so new tools are picked up automatically without agent-file edits.

- The debugging workflow now explicitly acknowledges that runtime tracing via MCP does not exist; all diagnosis is static. The debugging skill includes explicit reasoning guidance on guard ordering, unreachable/dead-end states, constraint satisfaction on entry, event ensures, and conditional rules to compensate for the absent runtime tools.



---

---

### 2026-05-11T00:00:00Z: Interpolation plan evolved to type-grammar-driven slot classification with explicit structural validation



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (4 files → 1 canonical entry).



**Merged sources:** `frank-interpolation-plan.md`, `frank-interpolation-position-aware.md`, `frank-interpolation-redesign.md`, `frank-interpolation-redesign-response.md`.



- frank-16 produced the initial 5-slice interpolation plan grounded in `docs/philosophy.md`; all type mismatches caught at compile time (no V2 deferrals). String is the one exception: valid in any hole position because a string field could hold a valid unit code or currency code at runtime.

- frank-18 revised to position-aware slot classification after Shane identified that the flat compatibility table allowed structurally invalid forms (e.g., `'1 {x} kg'` — double magnitude). Hole position within the typed constant (magnitude slot, unit/qualifier slot, whole-value slot) determines valid types, not just the target type. Position detection belongs in the type checker, not the parser.

- frank-23 redesign replaced position-text heuristics with type-grammar matching: each type that supports interpolation defines a finite set of valid `(TextSegment | HoleSegment)*` patterns; the type checker matches the full segment sequence against the target type's pattern table, assigns slot identities to holes on match, and emits `InvalidInterpolatedTypedConstantForm` (code 120) on no match. This covers compound qualifier holes for `price`/`exchangerate`, compound period forms (`'{n} years + {m} months'`), and all 19 typed constant types exhaustively. Formatted temporal types (`date`, `time`, `instant`, `datetime`, `zoneddatetime`, `timezone`) explicitly prohibited from interpolation.

- Three diagnostic codes allocated: 120 (`InvalidInterpolatedTypedConstantForm`), 121 (`InterpolationUnsupportedForType`), 122 (`InterpolatedTypedConstantHoleTypeMismatch`). Slice 1 (Parser) unchanged from frank-16; Slice 2 (TypeChecker) fully redesigned to match-then-check. Plan written to `docs/Working/interpolation-plan.md`. Three open questions filed for Shane review before Slice 1 begins.



---

---

### 2026-05-11T00:00:00Z: B9–B12 slices 2/3/4 shipped — post-resolution type and qualifier checking now enforced



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (3 files → 1 canonical entry).



**Merged sources:** `kramer-slice2-done.md`, `kramer-slice3-done.md`, `kramer-slice4-done.md`.



- Slice 2: Post-resolution `IsAssignable` check added to `ResolveAction`. B9 (bare integer → quantity assignment) now emits `TypeMismatch`.

- Slice 3: `ValidateAssignmentQualifiers` helper added and called in `ResolveAction`. B12 (cross-dimension arg/field assignment) now caught. Money qualifier mismatches caught.

- Slice 4: `UnitDimensionHelper` extracted. `QuantityValidator` now checks dimension against `DeclaredQualifiers`. Qualifiers threaded through resolve chain. Regression coverage added to `TypeCheckerTypedConstantTests` and `TypeCheckerFieldDefaultTests`.

- All three slices closed green. Slice 5 remains.



---

---

### 2026-05-11T00:00:00Z: Release-only build policy is now the repo default; all Debug.Assert sites converted



**By:** Scribe



**Status:** Merged from Frank's inbox note.



**Merged sources:** `frank-debug-release-policy.md`, `copilot-directive-release-only.md`.



- Shane chose Option A (release-only with symbols) over Frank's Option B (keep Debug config, replace asserts): "No debug builds. Production builds can have symbols. I'm a one man show I don't have bandwidth to test two different builds."

- `Directory.Build.props` sets `Release` as the default configuration with `DebugSymbols=true` and `DebugType=portable`.

- All 7 `Debug.Assert` sites in `src/Precept/Pipeline/` and `src/Precept/Language/` converted to unconditional `throw new InvalidOperationException(...)`. No `#if DEBUG` blocks remain.



---

---

### 2026-05-11T00:00:00Z: Fix 7's unconditional throw in GraphAnalyzer stands; PRECEPT019 does not supersede it



**By:** Scribe



**Status:** Merged from Frank's inbox note.



**Merged source:** `frank-fix7-precept019.md`.



- PRECEPT019 enforces method-level catalog exhaustiveness via `[HandlesCatalogExhaustively]` attribute but has zero visibility into an inline switch inside a loop — it can only annotate whole methods.

- Fix 7's unconditional `throw new InvalidOperationException(...)` as the default case in the `GraphAnalysisKind` dispatch loop is minimal, self-enforcing, and proportionate for a two-member enum with a single dispatch site.

- Fix 7 stands as-is. Revisit if `GraphAnalysisKind` grows to 4+ members across multiple dispatch sites — at that scale, PRECEPT019-style annotation becomes the better tool. Defense-in-depth also applies: the runtime throw catches cases where the analyzer is bypassed.



---

---

### 2026-05-11T00:00:00Z: B6 fixed — DeclaredQualifiers threaded through binary peer context in CompletionHandler



**By:** Scribe



**Status:** Merged from Frank's inbox note.



**Merged source:** `frank-b6-fixed.md`.



- `TypedConstantContext` now carries `DeclaredQualifiers` for money/quantity fields, threaded through binary peer context in `CompletionHandler`.

- Commit: `65badacb`. Tests green.



---

---

### 2026-05-11T00:00:00Z: GraphAnalyzer event-modifier dispatch adapted to current semantic model without widening TypedEvent



**By:** Scribe



**Status:** Merged from George's inbox note.



**Merged source:** `george-pipeline-audit.md`.



- The fix plan assumed `TypedEvent.Modifiers`, but the runtime model carries only `TypedEvent.IsInitial` plus syntax back-pointers. Adding a new typed-event modifier collection would have expanded the semantic surface beyond the audit's surgical scope.

- `GraphAnalyzer.Analyze()` now derives the active event modifier set from `evt.IsInitial` (`ModifierKind.InitialEvent` when true, empty otherwise), routes through `Modifiers.GetMeta(modifier)` and `EventModifierMeta.RequiredAnalysis`, and keeps the unconditional `throw` default so future `GraphAnalysisKind` additions still fail loudly.

- `ResolveAction()` remains private; new defensive tests invoke it via reflection instead of widening production visibility for test access.



---

---

### 2026-05-11T05:34:40Z: B4/B5 retriage reopens qualifier-site routing for declaration-side typed literals



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (1 new retriage note plus related prior fix notes).



**Merged sources:** `frank-b4-b5-retriage.md`, `frank-completion-context-fix.md`, `kramer-b1-b4-b5-fix.md`.



- The earlier apostrophe-trigger normalization was a real B1 fix: forcing non-expression quote-trigger contexts to `InExpression` correctly restores typed-literal starters for true expression/default sites such as `field q as quantity default '`.

- Frank's retriage shows that the same coercion is wrong for declaration-side qualifier slots. At `field q as quantity in '` and `field q as quantity of '`, completion now falls through `TryGetEnclosingField(...)`, recovers the outer `Quantity` type, and routes into quantity-literal items instead of the active qualifier slot (`UnitOfMeasure` or `Dimension`).

- Durable fix direction for Kramer's next pass: resolve declaration-side qualifier literal sites before expression fallback using parsed qualifier metadata and qualifier-shape slots, return the active qualifier-slot type rather than the outer field type, avoid routing B4/B5 through `GetQuantityLiteralItems(...)`, and replace weak non-empty assertions with concrete unit/dimension label checks.



---

---

### 2026-05-11T05:34:40Z: Typed-literal autocomplete is now a type-owned, qualifier-aware authoring surface



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (18 files -> 1 canonical entry).



**Merged sources:** `copilot-typed-literal-scope.md`, `copilot-compound-temporal-v1.md`, `copilot-completion-copy-dsl-vocab.md`, `copilot-typed-literal-qualifier-filter.md`, `copilot-qualifier-in-of-all-types.md`, `elaine-typed-literal-autocomplete-ux.md`, `elaine-qualifier-aware-mode.md`, `frank-typed-literal-completion-review.md`, `frank-kramer-typed-literal-review.md`, `frank-triage-completions-bugs.md`, `kramer-typed-literal-completions.md`, `kramer-typed-literal-impl-complete.md`, `kramer-deferred-items-complete.md`, `kramer-b2-fix.md`, `kramer-b7-fix.md`, `soup-nazi-typed-literal-test-battery.md`, `frank-completion-context-fix.md`, `kramer-b1-b4-b5-fix.md`.



- Shane's directives and Elaine's UX spec locked the surface: typed literals behave as a typed mini-mode for all quoted scalar literals, completion copy uses DSL vocabulary, compound temporal literals ship in V1, and qualifier-aware mode hard-filters legal values for both `in` and `of` qualifiers before ranking.

- Kramer shipped the 5-slice completion architecture around `TypedConstantContext`, per-type generators, slot-phase routing, qualifier threading, quote-close insertion, and singular/plural temporal units; Soup Nazi locked the behavior in executable coverage so the completion surface stays type-owned instead of leaking outer grammar items.

- Frank's review approved the architecture but blocked missing Ctrl+Space temporal recovery at `NumberTyping` and `AfterPlus`; his follow-up also clarified that invoked completion inside typed constants must normalize both null and empty trigger characters and must step left past the active literal token when recovering peer-expression context.

- Follow-on fixes replaced the quantity-unit example stub with `UcumCatalog.BrowseTier1()` plus dimension-vector filtering, and closed the OmniSharp semantic-token delta crash by invalidating cached documents when typed-constant token spans change instead of swallowing exceptions or forcing full refresh globally.



---

---

### 2026-05-11T05:34:40Z: Money and quantity modifiers now ship with implementation, regression coverage, and synced docs



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (3 files -> 1 canonical entry).



**Merged sources:** `george-money-modifiers-impl.md`, `soup-nazi-money-modifier-tests.md`, `frank-doc-sync-money-modifiers.md`.



- George extended the modifier metadata surface so `nonnegative`, `positive`, `nonzero`, `min`, and `max` apply to `money` and `quantity`, then mirrored the existing `default` path by resolving `min`/`max` values through `Resolve(mod.Value, ctx, typedField.ResolvedType)` for validation-only diagnostics.

- Soup Nazi confirmed the shipped implementation was complete, added 14 regression anchors in `MoneyQuantityModifierRegressionTests.cs`, and kept the core suite green while explicitly preserving the two known follow-up gaps: qualifier alignment and plain-number acceptance still behave the same as `default`.

- Frank synced the spec and business-domain docs to the real implementation: modifier applicability tables now list `money` and `quantity`, typed-constant bounds are documented as required for domain fields, and the quantity example now uses `min '0 kg' max '1000 kg'` instead of invalid plain integers.



---

---

### 2026-05-11T01:38:51Z: Terminal-state diagnostics now separate structural sinks from lifecycle dead ends



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (3 files -> 1 canonical entry).



**Merged sources:** `elaine-terminal-state-diagnostic-ux.md`, `frank-terminal-state-diagnostic-split.md`, `george-terminal-diagnostic-split.md`.



- Frank established the gating rule: path-to-terminal analysis is only meaningful after at least one terminal state is declared, and the contract wording should name declared terminals explicitly.

- Elaine split the UX into Message A for reachable non-terminal states with zero outgoing transitions and Message B for reachable non-terminal states that cannot reach any declared terminal, with Message B gated on `terminalStates.Length > 0`.

- George shipped the approved design as `StructuralSinkState` (C119) plus gated `DeadEndState` (C108), preserved `DeadEndStateFact` suppression semantics, and recorded implementation commit `482f4b1b`.



---



---

---

### 2026-05-11T01:38:51Z: Parser precedence and typed-constant binary context fixes are durable



**By:** Scribe



**Status:** Merged from Frank's inbox note.



**Merged source:** `frank-parser-precedence-and-ensure-context.md`.



- `Parser.Expressions` now gives non-associative operators `meta.Precedence + 1` as right binding power, so comparisons no longer block higher-precedence arithmetic on the right-hand side.

- `TypeChecker.Expressions.ResolveBinaryOp(...)` now propagates peer operand type context into typed constants before the D13 error short-circuit, closing PRE0052 failures in ensure, rule, and other binary-expression sites.

- The rental sample shed its comparison-workaround parentheses, and Frank recorded the batch as green at 5,073 tests.



---



---

---

### 2026-05-11T01:38:51Z: Declaration-name spans now stay token-precise through parser and binder



**By:** Scribe



**Status:** Merged from Kramer's inbox note.



**Merged source:** `kramer-diagnostic-span-fix.md`.



- The parser now tracks the last significant consumed token when computing declaration and list spans instead of letting trivia-skipping advance widen the end span.

- `IdentifierListSlot` / `StateEntryListSlot` now preserve per-name spans into binding, so declaration diagnostics and tooling surfaces anchor to the identifier token instead of the whole slot span.

- State-declaration graph warnings now stop at the name token boundary; Kramer validated the span fix against both core and language-server test projects.



---



---

---

### 2026-05-11T01:38:51Z: Semantic-token arg spans now use bare identifiers and exact-range dedup



**By:** Scribe



**Status:** Merged from Kramer's inbox note.



**Merged source:** `kramer-semantic-tokens-crash-fix.md`.



- Event-argument declarations and qualified references now carry bare identifier spans end to end (`ArgumentSyntax.NameSpan` -> binder -> typed args), and qualified arg references resolve from `expr.MemberSpan`.

- Language-server overlay token dedup now collapses only exact duplicate ranges instead of every token sharing a start column, preventing malformed delta streams.

- Kramer closed the OmniSharp delta crash with `test/Precept.LanguageServer.Tests` green at 160/160 and a successful language-server build.



---



---

---

### 2026-05-11T01:38:51Z: Span-refactor fallout fixes restored full-suite green



**By:** Scribe



**Status:** Merged from Soup Nazi's inbox note.



**Merged source:** `soup-nazi-typecheck-function-test-fix.md`.



- `TypeCheckerFunctionTests` now constructs `MemberAccessExpression` with both `MemberSpan` and full expression `Span`, matching the refactored syntax shape.

- Qualified event-argument semantic reference sites stay anchored to the full `Event.Arg` span, and LS symbol navigation now resolves arg references before overlapping event references.

- The graph-warning projector fixture now asserts `StructuralSinkState` for no-terminal flows, and Soup Nazi finished with `dotnet test` green at 5,085 passing / 0 failing.



---



---

---

### 2026-05-10T20:56:45Z: Track 2 Slice 11 makes proof obligations derive from catalog proof-site metadata



**By:** Scribe



**Status:** Merged from George's inbox note.



**Merged source:** `george-t2-11-complete.md`



- Collection non-empty obligations now project by proof-site shape: member access keeps `UnguardedCollectionAccess`, while `pop` / `dequeue` action sites emit `UnguardedCollectionMutation` and map to `CollectionEmptyOnMutation`.

- Unguarded mutation diagnostics now bind the real collection field name, and regression coverage now locks guarded plus `notempty` collection mutations alongside the catalog-driven `sqrt(abs(x))` proof path with 5 new proof tests.

- Implementation commits `004e68be`, `e48c0071`, and `599206b6` closed BUG-008, BUG-013, and BUG-050. The two transient shared-tree NameBinder failures during George-9's concurrent full-run attempt are superseded by George-7's clean 3,925-test run.



---



---



---

---

### 2026-05-10T20:56:44Z: Track 2 Slice 10 finishes catalog-derived name resolution and computed-field binding



**By:** Scribe



**Status:** Merged from George's inbox note.



**Merged source:** `george-t2-10-complete.md`



- `NameBinder` now treats `TokenMeta.IsStateWildcard` and `IsFieldBroadcast` as non-name lookup routes, so `any` and `all` no longer fall through to undeclared-state or undeclared-field diagnostics.

- Computed fields now bind after a declaration-order-stable topological sort: non-cyclic forward references resolve regardless of declaration order, while cyclic groups emit `CircularComputedField`; the coupled TypeChecker state-target normalization pass also now honors wildcard anchors after binder success.

- Implementation commits `def91dbb` and `b08b1fc4` closed BUG-001, BUG-026, BUG-030, and BUG-037 with 3,911 / 3,911 tests passing.



---



---



---

---

### 2026-05-10T20:56:43Z: Track 2 Slice 9 makes operator typing fully catalog-derived in TypeChecker



**By:** Scribe



**Status:** Merged from George's inbox note.



**Merged source:** `george-t2-9-complete.md`



- `TypeChecker.Expressions` now resolves operator result types from `OperatorMeta.ResultType` / `ResultTypePolicy`, including boolean operators, lookup `for`, `contains`, and arithmetic result projection from the operations catalog.

- The same batch tightened adjacent typing surfaces: comparison-position choice literals now contextual-type against `choice` operands, quantifier bindings preserve case-insensitive qualifiers, modifier validation emits `RedundantModifier` for subsumed constraints, and keyed `queue` / `log` field types now resolve to `QueueBy` / `LogBy` so `.peekby` binds correctly.

- Implementation commits `b7868d60` and `2f75c829` closed BUG-002, BUG-003, BUG-007, BUG-009, BUG-010, BUG-028, BUG-029, BUG-038, BUG-040, BUG-046, BUG-052, and BUG-053; validation finished clean at 3,925 / 3,925 tests.



---



---



---

---

### 2026-05-10T20:56:42Z: Track 2 Slice 4 locks operator result typing to catalog metadata



**By:** Scribe



**Status:** Merged from George's inbox note.



**Merged source:** `george-t2-4-operator-meta.md`



- `OperatorMeta.StaticResultType` is now `ResultType`, and the durable policy surface is `ResultTypePolicy { Fixed, LhsType, ElementType, BothOperands, OperationResult }`.

- Catalog assignments are explicit: comparisons/presence/contains stay `Fixed` boolean, `and` / `or` use `BothOperands` with boolean agreement, unary negate uses `LhsType`, lookup `for` uses `ElementType`, and arithmetic operators point at `OperationResult`.

- Durable rule for t2-9: arithmetic result typing must read `OperationMeta.Result` instead of reviving a per-operator promotion switch. George shipped the catalog-only foundation in commit `df874e15` with 3,899 passing tests.



---



---



---

---

### 2026-05-10T17:10:00Z: When-guard redesign rejects slot-index magic and requires explicit guard-policy metadata



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).



**Merged sources:** `copilot-directive-when-slots.md`, `frank-when-guard-design.md`.



- Shane's directive argued that the slot list itself should encode guard position, with `SlotGuardClause` always preceding the verb slot and no separate guard flag or enum.

- Frank rejected that `Slots[1] is GuardClause` convention as positional magic masquerading as metadata; slot order describes post-disambiguation content, not the parser's guard-placement protocol.

- The recommended replacement is an explicit `GuardPolicy` enum on `ConstructMeta` (`None`, `SlotWalk`, `PreVerb`, `PostVerb`) so each construct names where guards parse and whether guards are prohibited.

- AccessMode should move to pre-verb syntax (`in Draft when IsOwner modify Amount editable`), and the follow-through must update spec grammar, catalog/docs/tests, and MCP projections. `SupportsPostActionEnsure` was flagged as the parallel architectural smell, but kept out of this fix's scope.



---



---



---

---

### 2026-05-10T16:15:12Z: When-guard audit locks pre-verb state/event ensures and exposes the remaining spec-sample drift



**By:** Scribe



**Status:** Merged from Frank's inbox note.



**Merged source:** `frank-when-guard-audit.md`.



- The full audit found one real grammar inconsistency: state/event ensures are intended and implemented as pre-verb guards, but spec grammar lines 855–856 and three sample lines still show post-expression `when`.

- Parser, catalog, spec prose, spec examples, toolchain-plan notes, and tests all agree on the pre-verb form; the broken sample lines now stand as durable evidence that `ParserIntegrationTests` must start asserting zero diagnostics, not just "no crash."

- Other guard positions remain structurally consistent: rule stays the deliberate post-expression exception, transition rows keep post-event guards, state actions stay pre-verb, access mode remains post-adjective today, and omit/event-handler constructs still reject `when`.



---



---



---

---

### 2026-05-10T16:02:38Z: Slice 8 parser rewires are approved, but BUG-019 remains partial until binary-comparison typed constants are fixed



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).



**Merged sources:** `frank-slice8-review.md`, `george-slice8-done.md`.



- George's Slice 8 batch landed the parser/catalog rewires for guarded scoped constructs, post-action ensure, event-arg type/default parsing, comma-separated field targets, log ordering modifiers, interpolated strings, computed-field topological ordering, and typed-constant context propagation; build plus `Precept.Tests` closed green at 3869/3869.

- Frank's review approved the implemented Slice 8 items as architecturally sound and catalog-derived, with no merge-blocking issues.

- Durable follow-up: BUG-019 is only partially fixed because typed constants in binary comparison context still hit PRE0052 until `ResolveBinaryOp` retries context before the D13 bailout; stale MCP triage and `FieldTargetSlot`'s single-name data-model limitation remain explicitly tracked as non-blocking follow-ups.



---



---



---

---

### 2026-05-10T15:52:58Z: Track 2 Phase A source audit and D1-D8 doc-sync closeout are now one canonical record



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).



**Merged sources:** `frank-slice-1-7-audit.md`, `frank-catalog-doc-sync.md`.



- Frank's Phase A audit approved the shipped source/catalog work across slices 1–7 and isolated the only remaining closeout debt to eight `catalog-system.md` drift points plus two explicit modifier-test anchors.

- The follow-up doc-sync batch closed all D1–D8 gaps, added the named modifier capability tests, and re-aligned `catalog-system.md` with the live catalog field names, counts, and metadata shapes.

- Durable process rule: when catalog work ships, the owning commit must also close or remove any lingering open-question checklist items so documentation does not trail the metadata-driven source of truth.



---



---



---

---

### 2026-05-10T13:50:12Z: BUG-021 / BUG-048 / BUG-049 share one parser root cause: action shapes need slot/separator metadata



**By:** Scribe



**Status:** Merged from Frank's inbox note.



**Merged source:** `frank-t2-2-plan.md`.



- `ActionSyntaxShape` already tells the parser which structural pattern to use, but it does not describe which separator tokens participate in that pattern, so `ParseActionTarget` and the shape-specific parser methods still hardcode `=`, `into`, `by`, and `at`.

- BUG-021, BUG-048, and BUG-049 are therefore one metadata gap, not three separate parser bugs: the parser needs shape-owned slot metadata that carries per-slot separator tokens plus optionality, then reads those separators instead of a global token union.

- The approved implementation boundary is three vertical slices: catalog enrichment (`ActionSyntaxSlot` / `ActionShapeMeta`), `ParseActionTarget` rewire to shape-specific separator sets, then shape-method rewires that read separator tokens and optional suffix rules from metadata.



---



---



---

---

### 2026-05-10T13:46:52Z: BUG-006 / BUG-051 PRE0009 on `min(A,B)` is a stale extension build, not a live source defect



**By:** Scribe



**Status:** Merged from Frank's inbox note.



**Merged source:** `frank-bug006-051-triage.md`.



- The shipped source fix is already correct: `Parser.Expressions.cs` routes `min(`/`max(` through `IsFunctionCallLeader`, `Tokens.cs` marks `Min` and `Max` with that metadata, and the parser regression test proves `min(Amount, 10)` binds as a `FunctionCallExpression`.

- The live editor symptom came from a stale language-server binary: the running `Precept.dll` predates George's fix commit `6d360231`, so the editor was still executing the old parse path that emitted PRE0009.

- No code change is required for BUG-006 / BUG-051. Shane only needs to rebuild the extension/language-server output (VS Code Build task / `Ctrl+Shift+B`) so the editor picks up the already-correct source fix.



---



---



---

---

### 2026-05-10T12:45:39Z: Track 2 Slice 1 locks token metadata as the routing surface for wildcard, broadcast, and min/max leaders



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).



**Merged sources:** `george-t2-1-outcome.md`, `soup-nazi-t2-1-coverage.md`.



- `TokenMeta` now carries the canonical Track 2 Slice 1 routing fields: `IsStateWildcard`, `IsFieldBroadcast`, and `IsFunctionCallLeader`.

- Parser, binder/type-checker, transition normalization, and the tightly coupled proof-diagnostic mapping now read catalog metadata so `from any`, `modify all` / `omit all`, `.at(...)`, and `min(...)` / `max(...)` stop falling back to undeclared-name or arithmetic-only failure paths; keyword member-name derivation now comes from `Types.All[..].Accessors` instead of a parallel token list.

- Frank's follow-up review kept the flat routing bools and later removed the pure alias shims `IsBroadcastFieldTarget` and `IsAlsoBuiltinFunction`; `IsValidAsMemberName` remains the only derived helper alongside the canonical fields.

- Regression coverage locks BUG-001, BUG-006, BUG-025, BUG-026, BUG-037, BUG-039, and BUG-051 through `Track2PhaseAToolchainRegressionTests` plus exact token-catalog shape/token assertions, and the slice closed green at 3824/3824 `Precept.Tests` after George's commit `6d360231`.



---



---



---

---

### 2026-05-10T12:34:54Z: Track 2 is the active execution lane again



**By:** Scribe



**Status:** Merged from inbox.



**Merged source:** `copilot-directive-2026-05-10T08-34-54.md`.



- Shane switched active execution focus from Track 1 back to Track 2 immediately.

- This is an execution-priority change only; durable Track 1 decisions remain recorded, but new active batch work should route to Track 2 until another directive supersedes it.



---



---



---

---

### 2026-05-10T12:25:21Z: Keep both VS Code activation paths for the Precept extension



**By:** Scribe



**Status:** Merged from Kramer's inbox note.



**Merged sources:** `kramer-status-bar.md`, `soup-nazi-status-bar.md`.



- Keep both VS Code activation paths for the Precept extension: `workspaceContains:**/*.precept` and `onLanguage:precept`.

- The status bar item and language server are created during extension activation, so repo-style workspaces alone are not enough; single-file and no-workspace sessions also need activation coverage.

- `onLanguage:precept` restores the expected editor tooling surface without changing any catalog-driven language behavior.

- The durable regression anchor stays in `test\Precept.LanguageServer.Tests\ExtensionManifestTests.cs` until the repo grows a dedicated `test\Precept.VsCode.Tests` harness; spike mode should not invent a new test project just for this guard.



---



---



---

---

### 2026-05-10T12:25:21Z: Status-log triage isolates protocol bugs from the missing status-bar surface



**By:** Scribe



**Status:** Merged from Kramer's inbox note.



**Merged source:** `kramer-status-log-triage.md`.



- Shane's logs exposed two real shipped protocol bugs: the custom semantic-token color notification crossed the client boundary as a raw array, and the outline projector could emit `selectionRange` values that were not contained by `range`.

- Those bugs are real and worth landing, but Kramer did not find a code path where either one removes the VS Code status-bar item; the strongest direct clue for that missing surface remained extension activation and client lifecycle.

- Durable conclusion: keep the protocol fixes and treat the missing status-bar surface as a separate activation/lifecycle issue unless later logs show the extension deactivating or the status item never being created.



---



---



---

---

### 2026-05-10T12:15:36Z: Track 1 autonomous execution proceeds without per-slice approval pauses



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).



**Merged sources:** `copilot-directive-20260510T005009-track1-autonomous-run.md`, `frank-track1-autonomous-run.md`.



- Shane's directive is now durable team memory: Track 1 should run to completion without pausing for approval between slices.

- Frank's runbook locks the remaining execution order: Wave A can launch Slices 15, 18, 19, 20, 22, 23, 25, 26, and 27 immediately; Slice 17 waits on 14, Slice 21 waits on 20, Slice 24 waits on 23, and terminal Slices 28 then 29 remain strictly serial.

- Shared-infrastructure work (`20`, `23`, `26`) is the correct Wave A priority because those slices unblock later protocol work without reopening design questions.



---



---



---

---

### 2026-05-10T12:15:36Z: Incomplete typed declarations must offer `as` immediately after the value name



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).



**Merged sources:** `kramer-field-name-completion.md`, `soup-nazi-field-name-completion.md`.



- Completion routing should infer declaration-head context from neighboring significant tokens plus `Constructs.LeadingTokens` when parser recovery collapses the construct span.

- The durable slot-context surface is `AfterValueName`: after `field Name ` or `event Foo(Arg )`, completion should offer the required `as` keyword instead of broad top-level constructs.

- The regression anchor uses the real space trigger and an exact `["as"]` expectation so the test fails on the actual bad surface, not just on an internal context guess.



---



---



---

---

### 2026-05-10T12:15:36Z: Boolean field modifier completions stay filtered by modifier metadata and declaration-site legality



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).



**Merged sources:** `kramer-boolean-completions.md`, `soup-nazi-boolean-completions.md`.



- Field-modifier completions must derive from `ValueModifierMeta.ApplicableTo` plus `ApplicableDeclarationSites`, using the resolved declaration type instead of offering the entire modifier catalog.

- The current boolean field surface is intentionally limited to `default`, `optional`, and `writable`; numeric-only modifiers such as `max` and `maxplaces` are invalid leaks.

- Regression coverage should stay catalog-anchored while still asserting the exact user-visible boolean surface so future metadata drift fails honestly.



---



---



---

---

### 2026-05-10T12:15:36Z: Grammar-keyword gold drift was a VS Code fallback-color ordering bug, not a catalog classification bug



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).



**Merged sources:** `kramer-modifier-coloring.md`, `soup-nazi-modifier-coloring.md`.



- The visible gold drift came from the extension's TextMate fallback/theme rule, not from `KeywordGrammar` metadata or the language-server semantic-token surface.

- `as` must remain on `keyword.declaration.precept`, and field-declaration `default` needs an explicit declaration-site override before the generic `#grammarKeywords` fallback.

- The honest regression layer is grammar/package coverage: verify the generated TextMate ordering and fallback colors instead of changing catalog truth that was already correct.



---



---



---

---

### 2026-05-10T05:50:00Z: Slice 25 selection-range coverage must derive spans from real compilation artifacts



**By:** Scribe



**Status:** Merged from Soup Nazi's inbox note.



**Merged source:** `soup-nazi-slice-25.md`.



- Selection-range assertions should derive their expected spans from the real compilation pipeline: token span from `Compilation.Tokens`, enclosing parsed-expression span from the guard AST node, then slot span and construct span from `ConstructManifest`.

- This keeps acceptance coverage aligned with the runtime's actual span contracts instead of brittle hand-counted columns.

- Multi-position acceptance tests must submit positions in a deliberately non-source order and assert the returned chains preserve that request order, making output alignment an explicit contract.



---



---



---

---

### 2026-05-10T05:25:00Z: Slice 20 symbol-navigation coverage must lock full semantic reference sites and capability registration



**By:** Scribe



**Status:** Merged from Soup Nazi's inbox note.



**Merged source:** `soup-nazi-slice-20.md`.



- Slice 20 acceptance coverage belongs in `ReferencesHandlerTests` and `DocumentHighlightHandlerTests`, and it should stay red until real `ReferencesHandler` / `DocumentHighlightHandler` implementations answer requests from a populated `DocumentStore`.

- Event-argument navigation must honor `ArgReference.Site` exactly; qualified references like `JoinWaitlist.PartyName` should use the full qualified span instead of trimming to the trailing identifier.

- Capability coverage is part of the slice contract: once the handlers land, the language server must advertise references and document-highlight providers or the protocol surface is still incomplete.



---



---



---

---

### 2026-05-10T05:18:00Z: Slice 23 document-symbol tests lock state selection to the current semantic `NameSpan` contract



**By:** Scribe



**Status:** Merged from Soup Nazi's inbox note.



**Merged source:** `soup-nazi-slice-23.md`.



- Document-symbol selection ranges should project declaration identifier spans from the approved sources of truth: `IdentifierListSlot.Span` for the precept header and semantic `NameSpan` for field, state, and event declarations.

- For states, acceptance tests should assert the current `TypedState.NameSpan` exactly as emitted today, even though it still includes trailing modifiers such as `initial`.

- If the team later narrows state `NameSpan` to the bare identifier token, that is a separate pipeline contract change and should not be smuggled through the language-server slice.



---



---



---

---

### 2026-05-10T05:16:00Z: Slice 26 version-ordering tests must fail as runtime contract checks, not compile breaks



**By:** Scribe



**Status:** Merged from Soup Nazi's inbox note.



**Merged source:** `soup-nazi-slice-26.md`.



- The `DocumentState` version-ordering acceptance lane should stage its reds through reflection against the planned `TryUpdate(...)` / `Version` API instead of direct compile-time calls to missing members.

- That keeps the suite compiling while still failing with an exact runtime contract message when the versioned API is absent or has the wrong signature.

- Once the production API lands, the same tests can pivot immediately from API-presence checks to older/newer version behavior without test rewrites.



---



---



---

---

### 2026-05-10T05:11:00Z: Slice 14 completion routing must recover receiver and boundary context from semantic spans plus token adjacency



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).



**Merged sources:** `kramer-slice-14.md`, `soup-nazi-slice-14.md`.



- Expression completions should recover member-access receiver types from semantic expression spans plus token adjacency around `.` so accessor suggestions survive incomplete authoring like `Field.|member`.

- Completion routing must also treat a cursor parked at the start of the next token as belonging to the preceding separator when evaluating member-access and arg-default contexts; otherwise `CrewQueue.|count` and `default |1` fall back to generic surfaces.

- Current event scope should come from semantic construct matches (`TypedEvent`, `TypedTransitionRow`, `TypedEventHandler`, event-anchored `TypedEnsure`) rather than LS-local keyword and verb lists so arg completions stay catalog-driven across declaration, transition, and handler contexts.



---



---



---

---

### 2026-05-10T05:00:00Z: Track 1 should run autonomously to completion under the approved dependency wave plan



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).



**Merged sources:** `copilot-directive-20260510T005009-track1-autonomous-run.md`, `frank-track1-autonomous-run.md`.



- Shane's directive is explicit: Track 1 should continue without per-slice approval pauses until the lane reaches completion or a real blocker appears.

- The approved remaining-wave plan launches 15, 18, 19, 20, 22, 23, 25, 26, and 27 immediately; 17 waits on 14, 21 waits on 20, 24 waits on 23, and terminal slices 28 then 29 stay strictly serial after the behavioral surface closes.

- Shared-infrastructure slices 20, 23, and 26 should be prioritized ahead of lower-risk handlers and editor polish because they lock the helper contracts that unblock downstream slices.



---



---



---

---

### 2026-05-10T04:36:29Z: Slice 13 slot-context routing treats post-span `by`/`at` separators as expression positions



**By:** Scribe



**Status:** Merged from Soup Nazi's inbox note.



**Merged source:** `soup-nazi-slice-13.md`.



- `tools/Precept.LanguageServer/SlotContext.cs` now routes action-chain verb/target/expression positions, guard/compute/ensure/rule expressions, event-arg defaults, field `default` values, and `of` inner-type positions through the promised `SlotContext` surface.

- The durable parser/LS seam is now explicit: secondary action syntaxes like `enqueue ... by ...` can truncate `ActionChainSlot.Span` before `by` or `at`, so slot-context routing must honor raw separator tokens instead of trusting parsed action spans alone.

- `test/Precept.LanguageServer.Tests/SlotContextResolverTests.cs` locks the full approved Slice 13 matrix, and `test/Precept.LanguageServer.Tests` validated green at 88/88.



---



---



---

---

### 2026-05-10T04:33:18Z: Track 1 is the only active execution lane until Shane explicitly reopens Track 2



**By:** Scribe



**Status:** Merged from inbox.



**Merged source:** `copilot-directive-20260510T003318-focus-track1-only.md`.



- Active execution is now Track 1 only; Track 2 stays paused for execution until Shane explicitly switches focus back.

- The coordinator applied the directive immediately: `.squad/identity/now.md` now marks Track 1 as exclusive, and the SQL tracker reset all Track 2 `in_progress` slices back to `pending` so the live tracker shows no active Track 2 execution.

- Track 2 plans, findings, and reopened bug slices remain part of the durable record; this changes execution priority, not historical memory.



---



---



---

---

### 2026-05-10T04:33:18Z: Phase 1 language-server composition must be shared between Program.cs and LspTestHost



**By:** Scribe



**Status:** Merged from Kramer's inbox note.



**Merged source:** `kramer-no-deferral-followup.md`.



- The old `LspTestHost` mirroring note was real unfinished work, not an acceptable later-slice placeholder: `Program.cs` had the full shipped Phase 1 handler surface while the protocol host still booted a reduced server.

- `Program.cs` and `LspTestHost` now share `LanguageServerComposition.ConfigurePreceptLanguageServer(...)`, so tests and the shipped host boot the same handler set.

- `ServerCapabilityTests` now lock the live Phase 1 capability contract, and Slice 29 is narrowed back to future protocol-surface growth rather than Phase 1 mirroring cleanup.



---



---



---

---

### 2026-05-10T04:33:18Z: Semantic-token colors are injected from SemanticTokenTypes.All via `precept/semanticTokenColors`



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).



**Merged sources:** `kramer-10color-spec.md`, `kramer-slice-10-color.md`.



- The language-server color path is a custom notification, `precept/semanticTokenColors`, carrying the runtime projection of `SemanticTokenTypes.All` including `hexColor`, `bold`, and `italic`.

- The approved flow is catalog-driven end to end: `SemanticTokenTypes.All` -> `SemanticTokensHandler.SendColorNotification(...)` -> `precept/semanticTokenColors` -> VS Code `extension.ts` -> workspace `editor.semanticTokenColorCustomizations`.

- Constraint-pressure styling stays generic rather than per-token duplicated: the extension keeps one wildcard rule, `*.preceptConstrained => italic`, while token colors remain generated from catalog metadata.



---



---



---

---

### 2026-05-10T04:33:18Z: Implementation plans and plan-cleanup prompts must encode the no-deferral rule explicitly



**By:** Scribe



**Status:** Merged, consolidated, inbox cleared (4 files -> 1 canonical entry).



**Merged sources:** `copilot-directive-20260510T000159.md`, `copilot-directive-20260510T000538-both-plans.md`, `copilot-directive-20260510T000538.md`, `frank-no-deferrals-plans.md`.



- The no-deferrals rule now applies explicitly to plan language itself: no implementation plan may say "skip for now," "not strictly necessary," or any equivalent defer-it-for-later phrasing.

- This applies to both active implementation plans and to any spawned cleanup/rewrite prompt; when agents are asked to clean plans up, the prompt must state the no-deferral rule directly.

- Required work belongs in its owning slice. For Track 2, that means metadata-only slices close with catalog tests, consumer integrations land in the later slices that actually change parser/checker/binder/proof/MCP behavior, and Slice 2 is an audit checkpoint rather than a soft maybe.



---



---



---

---

### 2026-05-10T04:33:18Z: Track 2 has a written master plan, and Phase A stays metadata-first with catalog tests first



**By:** Scribe



**Status:** Merged, consolidated, inbox cleared (4 files -> 1 canonical entry).



**Merged sources:** `frank-track2-plan-written.md`, `frank-track2-phase-a-guardrails.md`, `george-track2-phase-a.md`, `soup-nazi-track2-phase-a-tests.md`.



- `docs/Working/track2-implementation-plan.md` is now the single execution plan for Track 2, covering 15 slices and mapping BUG-001 through BUG-054 to their owning work.

- Phase A is a metadata lane, not a general compiler-rewire lane: Slice 2 is audit-and-lock only because the required `ActionSyntaxShape` assignments already exist, while the real Phase A slices are 1, 3, 4, 5, 6, and 7.

- Small consumer reads are acceptable only when they are direct derivations from newly added metadata and do not require new parser/model/runtime shape; deeper parser, type-checker, proof, and MCP rewires stay in the later slices.

- Phase A proof closes first at the catalog layer: `test/Precept.Tests` should lock the new metadata fields, while behavior/integration coverage becomes mandatory in the later consumer slices, with outcome serialization closing end to end when the MCP slice wires it.



---



---



---

---

### 2026-05-10T04:33:18Z: Pipeline audit pins the remaining Track 2 debt on parser, type-checker, and proof metadata drift



**By:** Scribe



**Status:** Merged from Frank's inbox note.



**Merged source:** `frank-pipeline-audit-findings.md`.



- The current highest-blast-radius parser drift is still action grammar ownership: `Parser` hardcodes `=`, `into`, `by`, and `at` helpers instead of reading cataloged syntax parts, which is why BUG-021 / BUG-048 / BUG-049 cluster together.

- Wildcard and broadcast targets remain cross-stage drift because `any` and `all` still survive as raw names/null sentinels instead of first-class metadata, affecting parser, binder, and graph behavior together.

- Type-checker and proof debt are the same class of problem in later stages: qualifier/unit meaning still leaks through local tables or modifier-kind checks, and proof discharge still embeds operator implication/diagnostic tables instead of reading metadata-owned semantics.



---



---



---

---

### 2026-05-10T04:20:44Z: Slice 11 final wiring keeps Program.cs thin and leaves capabilities registration-driven







**By:** Scribe







**Status:** Merged from Kramer's inbox note.







**Merged source:** `kramer-slice-11-wiring.md`.







- `Program.cs` now completes the Phase 1 language-server surface by registering the full handler set over a shared singleton `DocumentStore`: text sync, semantic tokens, completion, hover, definition, document symbols, code actions, and folding.



- Semantic-token color bootstrapping is now part of startup wiring: `SemanticTokensHandler.SendColorNotification(server)` runs after server initialization, while tests keep the delegate-based overload so color publication stays unit-testable without a live server.



- Capability advertisement remains registration-driven rather than hand-authored in `Program.cs`; the OmniSharp handler base classes own their `ServerCapabilities` fragments, so final wiring adds handlers without creating a parallel manual capability block.



- The slice also locked its follow-through boundaries: the Track 2 rename surfaced a required `CompletionHandler` update to `ValueModifierMeta`, `LspTestHost` intentionally stays partial until Slice 29 expands protocol-surface coverage, and `dotnet test test/Precept.LanguageServer.Tests/` closed green at 74/74.



---



---



---

---

### 2026-05-10T04:20:44Z: Tracker status must change at the same boundary as execution state







**By:** Scribe







**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).







**Merged sources:** `frank-status-hygiene.md`, `copilot-directive-20260510T001838-status-hygiene.md`.







- The operating rule is now explicit: tracker state changes at the same boundary as execution state, with no delayed cleanup pass after the work is already done.



- The minimal protocol is boundary-based: before launch update the canonical tracker row and session todo, at result time record the evidence level immediately (`done` with SHA, `worktree-landed`, or still-active with the named open edge), at handoff close the old row before opening the next one, and under uncertainty stay conservative about proof.



- Coordinator hygiene is non-negotiable: keep one active slice per track unless an explicit parallel split is recorded, do not mark work active just because it was mentioned, and do not let safe-read consumer touches imply that a later phase has started.



- The reconciliation batch applied that rule to the live trackers: Track 1 already matched evidence (`Slice 10-color` done, `Slice 11` active), Track 2 `Slice 2` is satisfied from audit, `Slices 1/4/5/6/7` are worktree-landed, and `Slice 3` remains the only active Track 2 item; at close, only Track 1 Slice 11 and the modifier-model rename remained active across the batch.



---



---



---

---

### 2026-05-10T04:20:44Z: Value modifiers are the canonical cross-surface family, and declaration-site legality lives on core metadata







**By:** Scribe







**Status:** Merged, consolidated, inbox cleared (6 files -> 1 canonical entry).







**Merged sources:** `george-value-modifier-core.md`, `frank-slice-3-applicabletoeventargs.md`, `frank-slice-3-modifier-naming.md`, `newman-value-modifier-sync.md`, `j-peterman-value-modifier-doc-sync.md`, `soup-nazi-value-modifier-tests.md`.







- The canonical modifier family for typed value declarations is now `ValueModifierMeta`; the old `FieldModifierMeta` framing is retired, and supporting names move with it so parser/tooling/test surfaces stop encoding a false field-only claim.



- Declaration-site legality remains modifier-owned metadata, but the durable shape is the flags enum `ValueModifierDeclarationSite` projected through `ApplicableDeclarationSites`; the narrow `ApplicableToEventArgs` boolean is gone, `writable` is `FieldDeclaration`-only, and the other value modifiers stay legal on both fields and event args.



- Core and downstream consumers now read the same source-of-truth shape directly: parser routing uses the value-modifier surface, checker/proof consumers validate against declaration-site metadata instead of adapters, and the MCP/public language contract exposes `modifiers.value` plus declaration-site applicability from the core metadata.



- The batch closes the surrounding sync work too: Frank's architectural rulings are now implemented rather than deferred, Newman and J. Peterman's contract/doc updates align to the landed core names, and Soup Nazi's earlier red rename/applicability coverage is satisfied by George's validated Precept build plus green `Precept.Tests` and `Precept.Mcp.Tests`.



---



---



---

---

### 2026-05-10T03:13:51Z: Toolchain bug audit locks parser/MCP root causes and a real-catalog test strategy







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (2 files -> 1 canonical entry).







**Merged sources:** `frank-bug-cluster-analysis.md`, `soup-nazi-test-strategy.md`.







- The 52 confirmed toolchain bugs cluster most heavily in catalog-consuming surfaces: Parser 17, MCP serialization 15, Type Checker 10, Name Binder 4, Proof Engine 3, and MCP docs 3; the dominant root cause is stages hardcoding language behavior instead of projecting catalog metadata.



- The highest-value defect themes are now explicit: parser routing/disambiguation still misses catalog grammar (wildcards, guarded ensures/state actions, keyword-collision accessor forms, `append/enqueue by`, `insert/remove at`, comma field targets, richer arg type refs), MCP definition DTOs still flatten or omit catalog-derived structure (outcomes, qualifiers, hook actions, per-state declarations, element/member data, modifier bounds, event-arg richness), and operator/result typing still contains hardcoded dispatch drift (`and`/`or`/`not`, `contains`, `for`).



- The approved testing posture is to keep the real static catalogs as the executable language contract and build tiny synthetic stage fixtures around them rather than mocking metadata; mocking the catalogs would add indirection and drift risk without isolating a real boundary.



- Priority regression layers are now locked: add an MCP definition-surface matrix, parser routing/disambiguation tests derived from `Constructs.Entries`, keyword-collision/accessor tests from real catalog names, TypeChecker catalog-consumer tests for operations/accessors/modifiers, hook-specific pipeline tests, and catalog-reflection fixture tests that compile at least one minimal case per relevant catalog member.



---



---



---

---

### 2026-05-10T02:50:04Z: SemanticTokenTypes is the single visual-category source of truth, and constrained events stay in the italic system







**By:** Scribe







**Status:** Merged, consolidated, inbox cleared (5 files -> 1 canonical entry).







**Merged sources:** `frank-semantic-token-field-consolidation.md` (withdrawn), `frank-semantic-token-field-revision.md`, `frank-visual-catalog-design.md`, `frank-event-italic-clarification.md`, `frank-event-italic-resolved.md`.







- The approved direction is now singular: `SemanticTokenTypes` is the 14th catalog, `TokenMeta` keeps one `VisualCategory` field, and token-surface projections (custom semantic-token type, TextMate scope, base style metadata, and constrained-modifier capability) derive from that catalog instead of parallel token fields or hand-maintained manifest copies.



- Frank's earlier single-field rejection is superseded by the revised analysis: TextMate scopes and custom semantic-token types are two format projections of the same visual-category concept, so the catalog owns both projections and downstream tooling reads metadata rather than maintaining duplicate mappings.



- Shane resolved the event-italic conflict in the visual-system HTML: constrained events keep `SupportsConstrainedModifier = true`, italic is the universal constraint-pressure signal for states/fields/args/events, and constraint-blocked events stack italic plus dim rather than choosing one signal over the other.



- The visual taxonomy also stays explicit at the token level: args remain their own `ArgName` category, message strings remain the only gold token lane, comments keep base italic outside the five construct colors, and generated `package.json` semantic-token sections are the deployment projection of the catalog metadata rather than an independent source of truth.



---



---



---

---

### 2026-05-10T02:50:04Z: Language-server Phase 2 is now the production gap-closure plan







**By:** Scribe







**Status:** Merged from Frank's inbox note.







**Merged source:** `frank-ls-phase2-gap-analysis.md`.







- The live LS is beyond bootstrap but still missing production-complete authoring support in five areas: expression/default-position completions, catalog-complete hover projection, navigation handlers, document-symbol selection ranges, and document-version ordering.



- `TokenKind.Set` remains the sharpest cross-surface bug: in type position the LS must contextually reclassify `set` as the type token path so completion routing, hover text, and semantic tokens stop projecting the action keyword shape.



- Phase 2 is now the durable implementation plan for Slices 12-29: trigger/context fixes, deeper completion coverage, typed-constant completions, snippet metadata consumption, hover completion, semantic-token cleanup, references/highlights, rename, signature help, workspace/document symbols, selection ranges, version ordering, VS Code quote pairing, and doc sync.



- Non-gaps are locked too: keep push diagnostics on OmniSharp 0.19.9, keep full-sync/no-save hooks, do not add workspace diagnostics for closed files, do not add inlay hints or code lens, and do not encode routing-policy heuristics in completion filtering.



---



---



---

---

### 2026-05-10T02:50:04Z: Outline metadata and LS Slice 0 foundation are durably recorded as the protocol baseline







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (2 files -> 1 canonical entry).







**Merged sources:** `george-slice0a-complete.md`, `kramer-slice0-complete.md`.







- `ConstructMeta` now carries `IsOutlineNode` and `OutlineSymbolTag` with safe defaults, and the catalog explicitly marks `PreceptHeader`, `FieldDeclaration`, `StateDeclaration`, `EventDeclaration`, and `RuleDeclaration` as outline nodes with `Module`, `Property`, `Enum`, `Function`, and `Boolean` tags.



- The first LS protocol spine is now durably captured too: `TextDocumentSyncHandler` uses `ILanguageServerFacade`, registration runs through `WithHandler<TextDocumentSyncHandler>()`, the reusable in-process harness is built on `LanguageServer.PreInit(...)` / `LanguageClient.PreInit(...)`, and the test project depends on the separate `OmniSharp.Extensions.LanguageClient` package.



- The temporary `LegacyHandlerCompat` bridge is explicitly part of this baseline record so later slices can treat Slice 0b removal as the planned cleanup, not an accidental regression.



---



---



---

---

### 2026-05-10T02:50:04Z: Snippet templates are the minimal valid authoring form for constructs and primary actions







**By:** Scribe







**Status:** Merged from George's inbox note.







**Merged source:** `george-slice16-done.md`.







- Construct snippet templates are intentionally the smallest valid declaration forms: keyword plus the required authoring slots only (`precept`, `field`, `state`, `event`, `rule`) so VS Code tab stops guide required input instead of pre-populating optional modifiers.



- Primary action verb snippets are derived from `ActionSyntaxShape` and sample `.precept` files rather than invented ad hoc; shapes like `AssignValue`, `CollectionValue`, `CollectionInto`, `FieldOnly`, `InsertAt`, and `PutKeyValue` each map to a canonical snippet pattern.



- Secondary action variants (`AppendBy`, `EnqueueBy`, `DequeueBy`, `RemoveAt`) stay out of snippet metadata because they are checker-resolved secondary forms, not independent primary author-facing completion items.



---



---



---

---

### 2026-05-10T00:47:45Z: Slice 3 core landed ArgReference recording as the semantic-index arg provenance surface







**By:** Scribe







**Status:** Merged from George's inbox batch.







**Merged source:** `.squad/agents/george/inbox.md`.







- George commit `cba898b7` added `ArgReference(TypedArg Arg, SourceSpan Site)` plus `ImmutableArray<ArgReference> ArgReferences` to `SemanticIndex`, with matching `SemanticIndex.Empty` and `CheckContext` support so arg tracking is symmetric with field/state/event references.



- `TypeChecker.Expressions.cs` now records arg references at both `TypedArgRef` resolution sites (identifier-scope lookup and member-access resolution), and `TypeChecker.cs` now seals `ctx.ArgReferences.ToImmutableArray()` into the final semantic index.



- This closes the thin core prerequisite for projection-only arg tooling, and `test/Precept.Tests/ArgReferenceTests.cs` added three regression facts before George validated the slice at 3740/3740 passing tests.



---



---



---

---

### 2026-05-10T00:41:09Z: Language-server handler batch established the first real post-sync editor surface







**By:** Scribe







**Status:** Merged from Kramer's inbox batch.







**Merged source:** `.squad/agents/kramer/inbox.md`.







- Kramer commits `568ab5cc`, `9e679ceb`, `1ec3c7d5`, `1fbecf36`, and `453e690a` landed Slices 1, 2, 4, 5, and 9 respectively, moving the language server from text-sync-only infrastructure to concrete diagnostics, semantic tokens, completion, hover, and folding handlers.



- Slice 1 locked the diagnostic publication contract in tests: `DiagnosticProjectorTests` and `DiagnosticPublishIntegrationTests` now verify 0-based range projection, severity mapping, `Source = "precept"`, and publish-on-open capture through `LspTestHost.WhenPublishDiagnosticsAsync(...)`.



- The durable handler shapes are now explicit: semantic tokens stay a lexical projection from `Compilation.Tokens.Tokens` through `TokenMeta.SemanticTokenType`; completions are catalog-driven through `SlotContextResolver` plus `SemanticIndex` target lookup; hover composes markdown from `TokenMeta.Description` and semantic symbols; folding is construct-span-based only for multi-line regions.



- Validation closed most of the batch cleanly: Slice 1 and the Slice 2/4 work passed LS build/test runs at 20/20, Slice 5 passed isolated-worktree LS build/tests at 7/7 plus 3737 core tests, and Slice 9 confirmed clean IDE diagnostics plus 3737 core tests. The only remaining repo-baseline blocker called out by Kramer is the pre-existing `SemanticTokensHandler.CreateRegistrationOptions` access-modifier mismatch that can stop shared-tree LS build/test execution before the new folding tests run.



---



---



---

---

### 2026-05-10T00:23:31Z: Slice 0b removed the legacy language-server stub layer and zeroed the LS test project







**By:** Scribe







**Status:** Merged, inbox cleared (1 file -> 1 canonical entry).







**Merged source:** `.squad/agents/kramer/inbox.md`.







- Kramer commit `51d93dc2` deleted `tools/Precept.LanguageServer/LanguageServerStubs.cs`, `PreceptPreviewProtocol.cs`, and `LegacyHandlerCompat.cs`; the compat file also had to go because it still referenced the removed stub types and otherwise kept the language-server build red.



- Slice 0b also deleted 13 legacy shim-facing files under `test/Precept.LanguageServer.Tests/`, removing 173 compiler-redundant tests; the project now retains only `LspTestHost.cs` and `GlobalUsings.cs`, discovers 0 tests, and still builds cleanly.



- Validation closed the cleanup gate: `dotnet build` succeeds for the language-server and LS test projects, and `dotnet test test/Precept.Tests/` stays green at 3737/3737.



---



---



---

---

### 2026-05-10T00:11:05Z: Slice 0a outline metadata and Slice 0 language-server infrastructure are now the durable baseline







**By:** Scribe







**Status:** Recorded from completed work summaries; both agent inbox files were absent, so no inbox merge was required.







**Merged sources:** none — `.squad/agents/george/inbox.md` and `.squad/agents/kramer/inbox.md` were not present.







- George commit `d85449ea` extended `ConstructMeta` with `bool IsOutlineNode = false` and `string? OutlineSymbolTag = null`, then marked `PreceptHeader`, `FieldDeclaration`, `StateDeclaration`, `EventDeclaration`, and `RuleDeclaration` as outline nodes with `Module`, `Property`, `Enum`, `Function`, and `Boolean` tags in `src/Precept/Language/Constructs.cs`.



- George also added four catalog tests under `test/Precept.Tests/` and validated the branch at 3737 passing tests, closing the planned outline-metadata prerequisite with concrete coverage.



- Kramer commit `9f6b1fd7` landed the language-server text-sync/diagnostic spine: `DocumentState`, `DocumentStore`, `DiagnosticProjector`, `Handlers/TextDocumentSyncHandler`, `test/Precept.LanguageServer.Tests/LspTestHost.cs`, and `Program.cs` registration for `DocumentStore` plus `TextDocumentSyncHandler`.



- Durable caveat: `DocumentState` uses a volatile `Compilation` field plus `Interlocked.Exchange`, `DocumentStore` is keyed by `ConcurrentDictionary<DocumentUri, DocumentState>`, the language server builds, and the remaining legacy stub test failures stay expected until Slice 0b deletes the old stub layer.



---



---



---

---

### 2026-05-09T23:46:43Z: Language-server review batch reconciled docs, landed `TypedField.NameSpan`, and left only the preview restore-failure contract open







**By:** Scribe







**Status:** Merged, reconciled, inbox cleared (3 files -> 1 canonical entry).







**Merged sources:** `frank-comprehensive-review.md`, `george-typedfield-namespan.md`, and `kramer-ls-review.md`.







- Frank and Kramer both completed first-principles reviews of `docs/tooling/language-server.md` and `docs/Working/language-server-implementation-plan.md`; the objective artifact-reference and tooling-wiring drift they found was fixed inline, leaving the LS architecture and slice structure intact.



- Shane approved the thin core field-span fix and George landed it: `TypedField` now carries `SourceSpan NameSpan`, `TypeChecker` populates it from `DeclaredField.NameSpan`, runtime tests cover the symmetry change, and George validated the change with 3733 passing tests.



- One design decision remains open from the batch: `precept/inspect` preview restore failures (`RestoreInvalidInput` / `RestoreConstraintsFailed`) still need an explicit language-server contract, either as a structured failure payload or as a defined JSON-RPC error shape.



---



---



---

---

### 2026-05-09T23:21:36Z: Language-server clean pass front-loads shim deletion and leaves Slice 11 as final wiring only







**By:** Scribe







**Status:** Merged, reconciled, inbox cleared (4 files -> 1 canonical entry).







**Merged sources:** `copilot-ls-test-deletion-decision.md`, `frank-clean-pass.md`, `frank-slice0b-early-deletion.md`, `frank-slice11-update.md`.







- Slice 0b is now the immediate cleanup gate: delete `LanguageServerStubs.cs` and the 13 compiler-level language-server test files before any new handler code, then validate with `dotnet build` and `dotnet test test/Precept.Tests/`.



- The clean pass removes shim-shaped production helpers, keeps diagnostics on the text-sync compile/publish path, and adds `ArgReferences` as the thin core prerequisite so semantic tokens and go-to-definition stay projection-only.



- `precept/inspect` now ships as a real handler shell, `PreceptPreviewProtocol.cs` is slated for deletion, and Slice 11 is reduced to final `Program.cs` wiring plus capability declaration.



---



---



---

---

### 2026-05-09T18:53:05-04:00: Language server implementation is locked to the stub contract with no remaining plan deferrals







**By:** Scribe







**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).







**Merged sources:** `frank-language-server-design.md`, `frank-ls-plan-no-deferrals.md`.







- The 173 ported tests in `LanguageServerStubs.cs` remain the public contract; OmniSharp handlers and stub classes may coexist as thin entry points over shared logic.



- Fuzzy matching stays in the language server, preview/inspect may ship as a handler shell while the runtime evaluator remains stubbed, and `Token != null` is the permanent user-facing type filter.



- The temporary `ConstructKind` outline switch is superseded by concrete Slice 0a: `ConstructMeta` gains `IsOutlineNode` plus string `OutlineSymbolTag`, and the LS projects that tag to `SymbolKind` without introducing LSP types into `src/Precept/`.



---



---



---

---

### 2026-05-09T21:29:00Z: AI authoring MCP discovery now centers on focused named tools, not `precept_language`







**By:** Scribe







**Status:** Merged, reconciled, inbox cleared (6 files -> 1 canonical entry).







**Merged sources:** `copilot-directive-mcp-tool-arch.md`, `copilot-directive-tool-suite-decisions.md`, `frank-ai-authoring-tool-suite.md`, `frank-mcp-language-audit.md`, `newman-8-mcp-tools-implementation.md`, `newman-mcp-tool-audit.md`.







- The AI authoring surface is a focused named-tool suite: `precept_quickstart`, `precept_syntax`, `precept_types`, `precept_operations`, `precept_proofs`, `precept_patterns`, `precept_domains`, `precept_diagnostic`, plus `precept_compile`; tool names are the discoverability surface, not section parameters.



- Owner answers closed Frank's open questions: `precept_operations()` returns all 198 operations by default (with an optional category nicety), `precept_diagnostic` must cover all 116 codes, and v1 pattern scope is 8 compile-verified patterns plus 3 anti-patterns.



- `precept_language` may remain as an internal/testing fallback, but it is removed from MCP discovery and from skill/agent guidance because the focused suite is the public authoring contract.



- The focused tool implementations stay thin by projecting from `LanguageTool.Language()` internally; `precept_language` remains an internal fallback with its discoverable attribute removed, `precept_operations(category?)` filters on case-insensitive `LhsType`, and `precept_domains` layers in `UcumPrefixCatalog`.



---



---



---

---

### 2026-05-09T16:06:55-04:00: UCUM and domain registries stay curated registry surfaces with XML-anchored drift tests







**By:** Scribe







**Status:** Merged, reconciled, inbox cleared (5 files -> 1 canonical entry).







**Merged sources:** `frank-q2-q3-analysis.md`, `george-q1-tier1-codes.md`, `george-q4-catalog-registry.md`, `george-ucum-catalog-collapse.md`, `soup-nazi-q8-ucum-drift.md`.







- Named dimension categories are curated Precept editorial metadata, not something UCUM XML can derive mechanically; registry-shaped APIs should expose canonical `All` maps and keep alias resolution in explicit helper paths.



- `UcumAtomCatalog` is the single UCUM source of truth: `All` is the embedded XML-backed atom universe, `BrowseTier1()` is the curated 150-entry business-facing surface, and parse-only Tier 1 forms are synthesized through `UcumParser` rather than duplicated in a second catalog.



- Drift tests anchor against the embedded XML universe plus the approved Tier 1 curation rules, including the exclusion of time atoms (`s`, `min`, `h`, `d`) and `mol`, instead of relying on aspirational atom-count floors.



---



---



---

---

### 2026-05-09T19:55:00Z: Typed business-domain qualifiers are first-class semantic data, and count classification stays unit-aware







**By:** Scribe







**Status:** Merged, reconciled, inbox cleared (4 files -> 1 canonical entry).







**Merged sources:** `frank-design-gap-audit.md`, `george-p0-fix.md`, `frank-count-dimensionless-gap.md`, `george-q6b-qualifier-fix.md`.







- The architectural P0 was qualifier loss between parser and semantic model; `QualifiedTypeReference`, `ParsedQualifier`, and type-checker extraction now thread `in`/`of` data into fields and event args instead of dropping it at parse time.



- Qualifier validation belongs in the type checker against the authoritative registries (`CurrencyCatalog`, UCUM parsing, `DimensionCatalog`), with `in`/`of` exclusivity enforced on the qualified type reference span.



- Counting-unit safety did not require a proof-engine redesign: quantity arithmetic already compares unit-code-bearing qualifier records. The real gap was reverse-aliasing every `DimensionVector.None` unit to `count`, which is now fixed in unit-aware qualifier derivation so angles and solid angles no longer masquerade as counts.



---



---



---

---

### 2026-05-09T17:47: AI authoring content belongs in catalogs, and proof guidance owns runtime fault consequences







**By:** Scribe







**Status:** Merged, reconciled, inbox cleared (4 files -> 1 canonical entry).







**Merged sources:** `copilot-directive-faults-placement.md`, `copilot-directive-mcp-thin-layer-catalogs.md`, `george-15-catalog-authoring.md`, `george-precept-language-content-expansion.md`.







- All authored guidance for the new MCP tools must live in core metadata; tool implementations stay thin projections and do not embed separate prose, legality tables, or pattern content.



- `DiagnosticMeta` is now the recovery/example home for all 116 diagnostics, `SyntaxReference` is the compile-verified home for 8 common patterns plus 3 anti-patterns, and `QuickstartCatalog` is the first-contact orientation/tool-guide surface for AI agents.



- Runtime `Faults.All` belongs under `precept_proofs()` as `runtimeFaults`, because proofs and guards are the authoring lane that explains how those runtime consequences are avoided.



---



---



---

---

### 2026-05-09T17:43: User directive — the spike branch allows no deferrals, phased punts, or open-question handoffs







**By:** Scribe







**Status:** Merged from inbox.







**Merged source:** `copilot-directive-no-deferrals-final.md`.







- On this branch there are no issue-tracking deferrals, "top N now / rest later" partial authoring passes, or open-question lists handed back to Shane when the team can make the call and proceed.



- This directive applies immediately to MCP tool design, catalog authoring, and language-server planning; durable records should capture the final decision, not a deferred question list.



---



---



---

---

### 2026-05-09T15:33:49Z: User-defined string format validation is a future constraint feature, not typed-literal extensibility







**By:** Scribe







**Status:** Merged from inbox.







**Merged source:** `frank-22-user-defined-validation-extensibility.md`.







- The typed-literal validation framework stays intentionally closed and catalog-defined; there is no user-pluggable validator model for email, phone, or document-format parsing.



- Format validation is a different concern from semantically structured typed literals like money, datetime, and quantity: email/phone/document numbers remain strings with pattern rules, not new `TypeKind` values.



- The recommended future language surface is a string constraint modifier such as `matches /pattern/ because ...`, implemented through the existing modifier/constraint pipeline rather than the typed-literal framework.



---



---



---

---

### 2026-05-09T15:33:49Z: Runtime typed-literal arg parsing stays on `TypeRuntimeMeta`, not compile-time literal validation







**By:** Scribe







**Status:** Merged from inbox.







**Merged source:** `frank-typed-literal-runtime-args.md`.







- Runtime arg parsing for typed-literal event args stays on the existing catalog-owned runtime coercion lane: `TypeRuntime<T>` / `TypeRuntimeMeta.ReadJson` for JSON callers and `TypeRuntime<T>.FromClr` for typed callers.



- `TypedConstantValidation.Validate(...)` remains compile-time-only for DSL literal text, with diagnostic spans and suggestions; runtime failures surface as `EventOutcome.InvalidArgs`, not compiler diagnostics.



- Each typed-literal type therefore keeps three distinct catalog registrations on `TypeMeta`: `TypeRuntime<T>`, `TypeRuntimeMeta`, and `ContentValidation`, while sharing the same domain parsers underneath.



---



---



---

---

### 2026-05-09T15:26:09Z: MCP discovery is correct at three implemented tools, and stdout log pollution is fixed at the host boundary







**By:** Scribe







**Status:** Merged from inbox.







**Merged sources:** `newman-mcp-diagnosis.md`, `newman-stderr-fix.md`.







- The current MCP server really exposes only three tools (`PingTool`, `LanguageTool`, `CompileTool`); `precept_inspect`, `precept_fire`, and `precept_update` are absent because they have not been implemented yet, not because discovery is broken.



- Stdout log pollution was a separate host bug. `tools/Precept.Mcp/Program.cs` now routes console logging to stderr with `LogToStandardErrorThreshold = LogLevel.Trace`, keeping stdout clean for JSON-RPC.



- Commit `9de87699` closes the parse-warning defect now; the missing tool surfaces remain a deferred runtime-build scope tracked in `docs/working/newman-mcp-tool-discovery-diagnosis.md`.



---



---



---

---

### 2026-05-09T15:20:45Z: Event-arg member references now use a dedicated parameter-property TextMate scope







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (3 files -> 1 canonical entry).







**Merged sources:** `frank-arg-member-scope.md`, `kramer-arg-ref-color-fix.md`, `kramer-arg-member-scope-impl.md`.







- Frank locked the architecture: event-arg member references belong on the `variable.parameter.*` axis, so `eventArgReference` capture group 3 should emit `variable.parameter.property.precept` rather than `variable.other.property.precept`.



- Kramer's compound-selector override (`meta.event-arg-ref.precept variable.other.property.precept`) is preserved only as the superseded interim fix; the durable answer is the dedicated scope emitted by the grammar generator.



- The implementation shipped in `tools/Precept.GrammarGen/Program.cs`, regenerated `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`, simplified `tools/Precept.VsCode/package.json` to a direct `variable.parameter.property.precept -> #9AD8E8` rule, and left collection-member property scoping unchanged on the field axis.



---



---



---

---

### 2026-05-09T15:11:01Z: Typed literal validation stays catalog-driven under one static dispatcher







**By:** Scribe







**Status:** Merged from inbox.







**Merged source:** `frank-typed-literal-framework.md`.







- Frank approved a single `TypedConstantValidation.Validate(...)` dispatcher keyed by `TypeMeta.ContentValidation`; no `ITypedConstantValidator`, registry, or DI layer is allowed.



- The durable framework shape is `ContentValidation` metadata -> static dispatcher -> domain validator -> `TypedConstantParseResult`, with structured results consumed by the type checker, language server, runtime, and MCP tools.



- New work implied by the decision is explicit: add `UcumValidation` and `QuantityValidation`, give `NodaTimeValidation` a `TemporalLiteralKind`, add missing temporal `ContentValidation` entries (instant, timezone, zoneddatetime, duration quantity), and build the shared temporal parser under `src/Precept/Language/Time/`.



---



---



---

---

### 2026-05-09T15:07:24Z: CurrencyCatalog stays transactional while sync tests record intentional ISO-only exclusions







**By:** Scribe







**Status:** Merged, reconciled, inbox cleared (3 files -> 1 canonical entry).







**Merged sources:** `kramer-iso-xml-mismatch.md`, `george-currency-catalog-fix.md`, `george-fund-codes-excluded.md`.







- Kramer's unconditional sync test exposed the exact XML/catalog drift: the committed ISO snapshot added fund/accounting-unit codes plus precious-metal/testing placeholders, while `ANG`, `BGN`, and `ZWL` no longer appeared in the XML.



- George's implementation direction remains the canonical runtime contract: `CurrencyCatalog` models transactional business currencies, not the full XML payload, and `CurrencyCatalogSyncTests` carries a documented case-insensitive `IntentionalExclusions` set for XML-only codes.



- The durable exclusion policy now explicitly includes fund/accounting-unit codes `BOV`, `CHE`, `CHW`, `CLF`, `COU`, `MXV`, `USN`, `UYI`, `UYW`, `VED`, `XAD`, `XCG`, and `ZWG` alongside `XAU`, `XAG`, `XPT`, `XPD`, `XTS`, and `XXX`; withdrawn catalog entries `ANG`, `BGN`, and `ZWL` stay real failures if reintroduced.



---



---



---

---

### 2026-05-09T15:07:24Z: Data family anchor retired after field and arg colors became first-class semantic tokens







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (5 files -> 1 canonical entry).







**Merged sources:** `elaine-slate-audit.md`, `elaine-anchor-literal-check.md`, `elaine-anchor-dropped.md`, `kramer-field-arg-colors.md`, `kramer-color-audit.md`.







- Elaine's audit and literal-safety check made the design ruling durable: once fields move to `--field` (`#A5B4FC`) and args move to `--arg` (`#9AD8E8`), the old Data anchor `--data` (`#B0BEC5`) has zero legitimate consumers and no literal/value scope depends on it.



- Kramer wired the extension to that semantic split through TextMate, keeping fields on `#A5B4FC`, moving `variable.parameter.precept` to `#9AD8E8`, and removing the last `#B0BEC5` extension/theme/mockup usages.



- The Data family is now the four-token semantic grouping `--data-t`, `--data-v`, `--field`, and `--arg`; the family definition no longer depends on an anchor swatch or hue-only coherence.



---



---



---

---

### 2026-05-09T14:56:10Z: UCUM parsing must ship as a real shared language subsystem, not a closed-set placeholder







**By:** Scribe







**Status:** Merged from inbox.







**Merged source:** `frank-ucum-parser-arch.md`.







- Frank locked the architecture: build the real UCUM parser now in `src/Precept/Language/Ucum/`, backed by authoritative source data in `src/Precept/Data/Ucum/` and generated frozen catalog tables for runtime consumers.



- `unitofmeasure` validation must move off `ClosedSetValidation` onto a UCUM-backed `ContentValidation` path that returns structured parse data (`UcumParseResult` / `UcumParsedUnit`) rather than booleans.



- The domain rules are explicit: `time` is not in the UCUM dimension partition, `quantity of 'time'` is invalid in favor of `duration` / `period`, `count` remains a Precept business alias over dimensionless UCUM forms, and `speed` plus `force` become curated `DimensionCatalog` aliases.



---



---



---

---

### 2026-05-09T14:04:05Z: `precept_language` ships now as the canonical MCP language-vocabulary baseline







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (2 files -> 1 canonical entry).







**Merged sources:** `newman-language-tool-done.md`, `frank-language-tool-timing.md`.







- Newman implemented `LanguageTool.cs`, added 12 `LanguageToolTests.cs` coverage points, synced `docs/tooling/mcp.md`, and handed off green validation at 12 MCP tests plus 3646 core tests passing on commit `bd4e6e30`.



- Durable contract baseline: ship `precept_language` now off the current catalog-backed language/diagnostic surface (`tokens`, `types`, grouped `modifiers`, `actions`, `constructs`, `constraints`, `operators`, `functions`, `diagnostics`, and static `firePipeline`) instead of holding for builder/evaluator work.



- The older 11-catalog draft is superseded by the implemented/docs-synced surface; future evaluator metadata stays additive unless it changes the agent-facing vocabulary itself.



---



---



---

---

### 2026-05-09T00:00:00Z: Runtime business-domain CLR shapes are pure data records, not executor logic containers







**By:** Scribe







**Status:** Merged from inbox.







**Merged source:** `george-clr-value-types-done.md`.







- The public runtime shapes for `currency`, `unitofmeasure`, `dimension`, `money`, `quantity`, `price`, and `exchangerate` live under `src/Precept/Runtime/BusinessValues/` as record / record-struct data carriers.



- `Currency` stays a sealed record rather than bespoke alpha-code equality, and the public API surface uses `Dimension` to avoid colliding with the internal dimensional-analysis type `Measures.MeasureDimension`.



- Parsing, formatting, interning, arithmetic helpers, and `PreceptValue` wrappers are explicitly separate follow-on runtime concerns rather than responsibilities of these CLR shape types.



---



---



---

---

### 2026-05-08T05:27:37Z: Grammar generator design doc locks the generator contract and exposes the unreachable message-string path







**By:** Scribe







**Status:** Merged from inbox.







**Merged source:** `frank-grammar-generator-doc.md`.







- Frank's `docs/compiler/grammar-generator.md` is now the canonical generator design reference, locking the four-step algorithm, structural-pattern inventory, output contract, and its boundary with the catalog-system and tooling-surface docs.



- Durable bug record: the generator builds `#messageStrings` in `AddStructuralPatterns()` but never includes it in `BuildTopLevelPatterns()`, so `because` / `reject` message strings stay unreachable in generated output.



- Catalog gap locked: add `TokenMeta.IsMessagePosition` on `Because` and `Reject` so the generator can derive the gold message-string rule catalog-first before inserting `#messageStrings` ahead of `#strings`.



---



---



---

---

### 2026-05-08T05:27:37Z: Grammar generator implementation closes the spec must-fix inventory while leaving the catalog-blocked message-position gap explicit







**By:** Scribe







**Status:** Merged from inbox.







**Merged source:** `kramer-grammar-gen-impl.md`.







- Kramer closed all 16 must-fix items on PR #139: the generator now emits 42 repository patterns, orders 41 top-level patterns per spec, derives structural alternations from catalogs, and removes stale patterns plus the retired `nullable`, `invariant`, `assert`, and `with` keywords.



- Durable boundary: function-argument message strings still cannot receive gold scoping without new positional metadata, so the implementation leaves an explicit TODO at the exact wire-in point instead of hardcoding names or argument positions.



- Validation at handoff stayed clean: the generator build passed, the emitted grammar JSON was valid, and promotion to the canonical grammar remains gated on full parity plus the message-position catalog gap.



---



---



---

---

### 2026-05-08T04:55:35Z: ProofEngine implementation is blocked on unresolved spec and contract gaps







**By:** Scribe







**Status:** Merged from inbox.







**Merged source:** `frank-proof-engine-gap-analysis.md`.







- Frank reviewed the ProofEngine spec against commit `79c3403` and marked implementation **NOT READY**: three blocking gaps and seven significant gaps prevent a clean start.



- The blockers are now explicit in the ledger: three `ProofRequirementKind` variants still lack discharge-strategy coverage, the spec's canonical `FieldModifierMeta.ProofDischarges` property does not exist in source, and the specified `ProofLedger` output shape diverges materially from the stub and `Compilation` contract.



- Durable implementation gate: close the discharge-model, catalog-shape, and output-type mismatches before any ProofEngine slice starts; source-alignment gaps like `SemanticIndex.AllTypedExpressions` and the `ConstraintIdentity` shapes remain follow-up work.



---



---



---

---

### 2026-05-08T04:55:17Z: TextMate grammar replacement must be catalog-complete and parity-or-better before the generator becomes canonical







**By:** Scribe







**Status:** Merged from inbox.







**Merged source:** `frank-grammar-spec.md`.







- Frank drafted an authoritative grammar spec after reviewing the token/catalog sources, the hand-authored grammar, the generator scaffold, the design-system docs, and all `.precept` samples.



- Durable finding: both the shipped `precept.tmLanguage.json` and the current generator are stale (retired keywords and syntax, missing construct patterns, collapsed scope groups), so replacement is only valid when the generator emits the full catalog-derived language surface, including the dedicated rule-message string scope.



- Resolution baseline: catalog `TextMateScope` assignments win over conflicting brand-doc keyword lists, and the generator must reach hand-authored parity-or-better before it can replace the shipped grammar.



---



---



---

---

### 2026-05-08T04:26:28Z: Exhaustive GraphAnalyzer review approves the current implementation and narrows the remaining follow-up to future event-modifier work







**By:** Scribe







**Status:** Merged from inbox.







**Merged source:** `frank-graphanalyzer-exhaustive-review.md`.







- Frank's exhaustive review approved commit `d10513d` as architecturally sound, spec-complete for the currently implemented language surface, and catalog-driven in the required dimensions.



- The only red finding (`EventModifierMeta.RequiredAnalysis` not yet consumed) is explicitly zero-risk today because the only event modifier with graph-analysis implications is `initial`, which the analyzer already handles equivalently through edge/topology derivation.



- Durable future-touch follow-up: when richer event modifiers land, GraphAnalyzer must consume `EventModifierMeta.RequiredAnalysis`; the next touch is also the right time to consider an event-per-state index for the O(events × edges) scans and `RelatedSpans` on structural-violation diagnostics.



---



---



---

---

### 2026-05-08T04:26:28Z: GraphAnalyzer structural blockers and both R4 test batches are durably recorded







**By:** Scribe







**Status:** Merged from inbox; George's blocker fixes plus Soup-Nazi's primary and late-arriving Round 2 test batches are now all durably recorded.







**Merged sources:** `frank-r4-review.md`, `soup-nazi-r4-review.md`, `george-graph-analyzer-done.md`, `george-r4-fixes-done.md`, `soup-nazi-r4-tests-done.md`, `soup-nazi-r4-round2-done.md`.







- George's GraphAnalyzer implementation baseline is now durably recorded: declaration spans stay hoisted on typed inputs, missing-initial recovery keeps analysis total, wildcard expansion remains deterministic with explicit-row suppression, event coverage stays event-level, and terminal-completeness vs. dead-end facts remain separate proof artifacts.



- Frank's R4 architectural review is now canon: the real blockers were the three missing structural diagnostics plus the stale appendix code collision; the `Reject`/`NoTransition` self-edge nuance and the indentation defect were explicitly carried forward as cleanup items.



- George closed B1/B2/F10/F11 in commit `5398435` by registering and emitting `TerminalStateHasOutgoingEdges` (109), `IrreversibleStateHasBackEdge` (110), and `RequiredStateDoesNotDominateTerminal` (111), filtering terminal self-edges, and correcting the doc appendix / indentation drift.



- Soup-Nazi-7 closed the required GraphAnalyzer test matrix in commit `7c674bd` for wildcard behavior, missing-initial recovery, stateless precepts, structural violations, positive terminal completeness, and the single-state / cycle / diamond / multi-dead-end edges, with 3381 tests passing at handoff.



- During the same Scribe pass, the late-arriving `soup-nazi-r4-round2-done.md` inbox note was merged mechanically without waiting for orchestration: TQ1 was renamed to match its actual assertions, EC5 was split into zero-handler vs. partial-coverage tests, EC6 added explicit `reject` self-edge coverage, Gap 8 added explicit `no transition` self-edge coverage, and validation closed green at 3385/3385.



- The locked 2026-05-08 directive is therefore preserved together with the evidence that its remaining conditional GraphAnalyzer test items have now landed in the ledger.



---



---



---

---

### 2026-05-08T00:49:00Z: GraphAnalyzer advisory fix batch closed on-branch except the deferred event-modifier gap







**By:** Scribe







**Status:** Merged from inbox.







**Merged source:** `george-advisory-fixes-done.md`.







- George closed all 8 addressable items from Frank's advisory list in commit `79c3403`: structural diagnostics 109/110/111 now carry `RelatedSpans`, graph diagnostics gained `RelatedCodes`, the graph-analyzer docs now spell out zero-terminal semantics and the real analyzer input set, the planned `EventModifierMeta.RequiredAnalysis` consumption path is marked, `IsInitial`'s direct enum check is documented, and the fragile `nameof()` dedup was replaced with `HasDiagnostic()`.



- The event-coverage and initial-event scans now share a precomputed edge index, removing the redundant O(events × edges) lookups without changing behavior.



- Validation at handoff closed green at 3385/3385 `Precept.Tests` passing.



---



---



---

---

### 2026-05-08T00:36:25Z: Full GraphAnalyzer advisory inventory reconstructed and locked for durable follow-up tracking







**By:** Scribe







**Status:** Merged from inbox.







**Merged source:** `frank-advisory-reconstruction.md`.







- Frank reconstructed the full post-review inventory after the earlier exhaustive-review merge omitted the detailed advisory list: 9 advisory items (A1-A9) plus Gap1.



- Durable breakdown: requirements/docs follow-ups A1-A4 (`RelatedSpans`, zero-terminal semantics, `RelatedCodes`, graph-analyzer input-table correction), catalog/compliance follow-ups A5-A7 (planned event-modifier dispatch note, `IsInitial` rationale, typed `NoInitialState` dedup), and quality follow-ups A8-A9 (event-per-state edge index for coverage and `GraphEvent.IsInitial`).



- Gap1 remains the deliberate future-touch item: GraphAnalyzer still must consume `EventModifierMeta.RequiredAnalysis` when richer event modifiers ship.



---



---



---

---

### 2026-05-08T00:22:50Z: R4 hard gate expanded to every remaining conditional GraphAnalyzer item







**By:** Shane (via Copilot)







**Status:** Locked — applies before any ProofEngine work begins.







**Merged source:** `copilot-directive-20260508.md`.







- No R4 conditional follow-on stays optional anymore: TQ1, EC5, EC6, and Gap 8 must all land before ProofEngine work begins.



- Scribe merged the directive immediately without waiting for the still-running `soup-nazi-8` batch so the team ledger reflects the hard gate now, not after the remaining follow-up lands.



---



---



---

---

### 2026-05-08: Parser remediation design decisions Q5–Q8 locked (OutcomesCatalog, NameBinder, quantifier scoping, forward references)







**By:** Coordinator (Shane decisions)







**Status:** Locked — recorded from design session. Implementation deferred to NameBinder sprint.







- **Q5 — OutcomesCatalog position:** `OutcomesCatalog` is **catalog #14**, a peer-level catalog alongside Constructs, Actions, Modifiers, Types, etc. It is not a sub-catalog grouped under grammar/structure. `docs/language/catalog-system.md` must add it to the catalog table when implemented.







- **Q6 — Quantifier binding vs. field name shadowing:** When a quantifier binding variable (e.g., `item` in `for item in items`) has the same identifier as a declared field name, the NameBinder emits a **hard error** (`BindingShadowsField` or similar). Silent shadowing is rejected because it cuts off access to the field inside the predicate with no escape hatch in current DSL syntax.







- **Q7 — Forward-reference detection ownership:** Forward-reference detection (a field expression references a field declared later in the precept) moves to the **NameBinder**, not the TypeChecker. Name resolution — including detecting that a name does not exist at all, or exists only later — is a name-resolution concern. The TypeChecker receives a fully resolved `SymbolTable` and should not re-implement reference existence checks.







- **Q8 — NameBinder diagnostic code range:** Implementation detail; the implementer assigns the next available codes from `DiagnosticCatalog.cs` at implementation time. Reserve codes for: `DuplicateFieldName`, `DuplicateStateName`, `DuplicateEventName`, `UndeclaredField`, `UndeclaredState`, `UndeclaredEvent`, `UndeclaredArg`, `BindingShadowsField`.

---

### 2026-05-08: R2 Gate Verdict — Slices 5–7







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `frank-r2-verdict.md`.







**Reviewer:** Frank (Lead Architect)



**Scope:** Slices 5 (TransitionRow + EventHandler), 6 (Structural Validation), 7 (Modifier Validation)



**Test baseline:** 3242/3242 passing







---







## Verdict: APPROVED







Slices 8–9 may proceed.







---







## Summary







The Slices 5–7 implementation is **sound, catalog-compliant, and correctly scoped.** All three slices follow the design authority faithfully. The pipeline call order is correct and matches the intended dependency chain. Key locked decisions are enforced:







- **D5 (ActionSecondaryRole invariant):** `ResolveAction` correctly pairs `SecondaryRole` with `SecondaryExpression` — `null/null` for no-secondary cases, `HasValue/non-null` for `CollectionValueByAction`, `InsertAtAction`, and `PutKeyValueAction`, with `Debug.Assert` enforcing the non-null side. The tests validate the null/null case; the positive case is enforced by assert and will get end-to-end coverage when collection action tests expand.







- **D9 (QualifierBinding DU):** `QualifierBinding` is used on `TypedBinaryOp.ResultQualifier` and `TypedTransitionRow.ResultQualifier` — no raw qualifier strings anywhere.







- **D10 (FromState == null for wildcard):** `TypedTransitionRow.FromState` is `string?` with comprehensive XML doc explaining null = any-state wildcard. The null case is handled correctly in the implementation (line 938–952). No test asserts `FromState == null` because the parser's wildcard syntax isn't exercised yet — this is a parser-surface gap, not a type-checker gap.







- **D26 (ErrorExpression → ≥1 Error diagnostic):** `Debug.Assert` in both `PopulateTransitionRows` and `PopulateEventHandlers` via `ContainsErrorExpression` / `ContainsErrorExpressionInAction` helpers. Tests at lines 225–241 and 445–462 exercise both the guard and action-value error paths.







- **D3/Modifier catalog compliance:** `ValidateFieldModifiers` reads `FieldModifierMeta.ApplicableTo`, `MutuallyExclusiveWith`, `Subsumes` entirely from the Modifiers catalog. Zero per-modifier switches. `IsTypeApplicable` handles both `TypeTarget` and `ModifiedTypeTarget` correctly.







- **§13/§14 boundary:** `ValidateStructural` contains only computed-field cycle detection (DFS), forward-reference belt-and-suspenders, and is set/choice validation. No reachability, dead-end, or unreachable-state logic — those are correctly left to GraphAnalyzer.







- **Restoration integrity:** Slice 5 methods are complete. Pipeline call order confirmed: PopulateFields → PopulateStates → PopulateEvents → PopulateTransitionRows → PopulateEventHandlers → ValidateModifiers → ValidateStructural.







- **EventName.ArgName fix:** `ResolveMemberAccess` (line 1487–1498) correctly produces `TypedArgRef` when LHS is a known event name and RHS is a declared arg. Does NOT fall through to `TypedMemberAccess`. End-to-end validated by `TypeCheckerModifierTests.EventArg_WithValidModifier_NoDiagnostic` (`Submit.Label` resolves cleanly).







## Test Quality Notes







- **Transition tests (26):** Good breadth — FromState/ToState resolution, undeclared state/event, guard resolution with field refs, D26 guard/action error paths, multi-action chains, clear action shape, event handler resolution and reference recording.



- **Structural tests (17):** IsSet/IsNotSet on optional and non-optional fields well covered. Cycle detection infrastructure is correct; positive cycle tests are structurally blocked until computed expression resolution populates `ComputedDeps` (documented in test comments — acceptable).



- **Modifier tests (29):** Strong catalog-driven coverage. Applicability uses real `ApplicableTo` from catalog. Subsumption uses real catalog relationships (positive→nonnegative, positive→nonzero). Implied modifier redundancy uses real type metadata (timezone→notempty, currency→notempty). Writable-on-event-arg and writable-on-computed both validated.







## Observations (non-blocking)







1. **Stale regression note in TransitionTests header:** The `<remarks>` block references the `EventName.ArgName` regression as "TYPE B (known red)" — but the fix is already shipped and `Submit.Label` resolves cleanly. The note should be removed in a future cleanup pass.







2. **D10 wildcard test gap:** No test asserts `FromState == null` for an any-state wildcard. Low risk — the implementation is trivially correct (if `StateName == null`, `fromState` stays `null`). Coverage should be added when parser wildcard syntax is available.







3. **D5 positive-case test gap:** No end-to-end test exercises `SecondaryRole.HasValue == true` with `SecondaryExpression != null` (insert-at, append-by, put). The Debug.Assert covers correctness; expand test coverage when collection actions get dedicated integration tests.

---

### 2026-05-08: george-ci-fix-done







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `george-ci-fix-done.md`.



---



---



---

---

### 2026-05-08: george-parser-fix-done







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `george-parser-fix-done.md`.

---

### 2026-05-08: george-slice-1-done







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `george-slice-1-done.md`.

---

### 2026-05-08: george-slice-10-done







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `george-slice-10-done.md`.



---



---



---

---

### 2026-05-08: george-slice-2-done







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `george-slice-2-done.md`.

---

### 2026-05-08: george-slice-3-done







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `george-slice-3-done.md`.

---

### 2026-05-08: george-slice-4-done







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `george-slice-4-done.md`.

---

### 2026-05-08: george-slice-5-done







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `george-slice-5-done.md`.

---

### 2026-05-08: george-slice-6-done







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `george-slice-6-done.md`.

---

### 2026-05-08: george-slice-7-done







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `george-slice-7-done.md`.

---

### 2026-05-08: george-slice-8-done







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `george-slice-8-done.md`.



---



---



---

---

### 2026-05-08: george-slice-9-done







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `george-slice-9-done.md`.



---



---



---

---

### 2026-05-08: george-slice5-restored







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `george-slice5-restored.md`.



---



---



---

---

### 2026-05-08: soup-nazi-slice-1-triage







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `soup-nazi-slice-1-triage.md`.



---



---



---

---

### 2026-05-08: soup-nazi-slice-10-done







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `soup-nazi-slice-10-done.md`.



---



---



---

---

### 2026-05-08: soup-nazi-slice-2-done







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `soup-nazi-slice-2-done.md`.



---



---



---

---

### 2026-05-08: soup-nazi-slice-3-done







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `soup-nazi-slice-3-done.md`.



---



---



---

---

### 2026-05-08: soup-nazi-slice-4-done







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `soup-nazi-slice-4-done.md`.



---



---



---

---

### 2026-05-08: soup-nazi-slice-5-done







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `soup-nazi-slice-5-done.md`.



---



---



---

---

### 2026-05-08: soup-nazi-slice-6-done







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `soup-nazi-slice-6-done.md`.



---



---



---

---

### 2026-05-08: soup-nazi-slice-7-done







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `soup-nazi-slice-7-done.md`.



---



---



---

---

### 2026-05-08: soup-nazi-slice-8-done







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `soup-nazi-slice-8-done.md`.



---



---



---

---

### 2026-05-08: soup-nazi-slice-9-done







**By:** Unknown







**Status:** Merged from inbox — merged from inbox.







**Merged source:** `soup-nazi-slice-9-done.md`.



---



---



---

---

### 2026-05-07T23:22:15Z: TypeChecker Slice 1 test inventory recorded ahead of symbol population







**By:** Scribe







**Status:** Merged from inbox while implementation was still running.







**Merged source:** `soup-nazi-slice-1-tests.md`.







- Soup-Nazi wrote `test/Precept.Tests/TypeChecker/TypeCheckerSymbolTests.cs` with 55 Slice 1 tests covering type-kind resolution, collection element types, optional/modifier preservation, implied modifiers, state/event population, initial-state diagnostics, and name-index population.



- At inbox-write time only 2 tests passed and 53 failed because George's Slice 1 symbol-population implementation had not landed yet and `SemanticIndex` was still effectively empty.



- Durable gate: treat the test matrix as ready for R1 once George's Slice 1 commit arrives; `LogBy` / `QueueBy` key-type coverage remains explicitly deferred beyond this batch.



---



---



---

---

### 2026-05-07T23:22:15Z: TypeChecker Slice 0 R0 closed after `TransitionRowOutcome` rename







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (3 files -> 1 canonical entry).







**Merged sources:** `george-s0-shape.md`, `frank-r0-review.md`, `george-r0-b1-fixed.md`.







- George committed the TypeChecker Slice 0 semantic shape in `5260065` plus `abf2532`: `SemanticIndex` full layout, `CheckContext` accumulators/lookups, the 14-node `TypedExpression` DU, `QualifierBinding`, `ConstraintIdentity`, `TypedAction`, and TypeChecker test-helper wiring.



- Frank's R0 review found one blocker only: `TypedOutcomeKind` solved the `TransitionOutcome` name collision but violated enum naming conventions; the correct disambiguation is `TransitionRowOutcome`.



- George resolved B1 in `350f386` by renaming `TypedOutcomeKind` to `TransitionRowOutcome` in `SemanticIndex.cs`; all other D# decisions remained compliant and the 2974-test branch baseline stayed intact.



---



---



---

---

### 2026-05-07T22:51:59Z: H1 housekeeping closeout recorded; Frank C2 catalog doc sync deduplicated into the same batch







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (2 files -> 1 canonical entry).







**Merged sources:** `george-h1-committed.md`, `frank-c2-catalog-doc-sync.md`.







- Recorded George-9's nine-commit Precept-V2-Radical housekeeping batch as the durable closeout: Outcomes catalog, parsed action/type-reference DUs, parser enrichment, diagnostic payload expansion, NameBinder, type-checker OQ doc locks, catalog-system doc sync, and history housekeeping all landed with the working tree clean.



- Deduplicated Frank-12's catalog doc note into the same canonical entry because commit `a469217` already carried the `docs/language/catalog-system.md` additions for `ActionMeta.SyntaxShape`, `FunctionMeta.HasCIVariant`, and `FunctionMeta.CIVariantOf` inside the George-9 batch.



- Validation at handoff: 2974 tests passing; no history files crossed the 15 KB summarization gate in this pass.



---



---



---

---

### 2026-05-07T08:40:33Z: BackArrow (`<-`) syntax batch closed; parser exhaustiveness stays on `ParserState`







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (10 files -> 1 canonical entry).







**Merged sources:** `elaine-backarrow-ux.md`, `elaine-equals-ux.md`, `frank-backarrow-analysis.md`, `frank-equals-analysis.md`, `frank-backarrow-decision.md`, `frank-backarrow-impl-plan.md`, `george-backarrow-impl-notes.md`, `soup-nazi-backarrow-tests.md`, `frank-consume-exhaustively-analysis.md`, `george-backarrow-fix-and-exhaustive.md`.







- Shane approved replacing computed-field `->` with `<-`; Elaine's UX read stays durable: the value flows into the field, while `=` collides with ubiquitous `set X = expr` syntax and `->` stays overloaded for outcomes/action chains.







- Frank's plan kept the rollout narrow and mechanical: add `BackArrow`, propagate the token through parser/docs/samples/tooling, and preserve `->` for transition outcomes plus action chains.







- George shipped the main implementation in commit `266ee5a`, Soup-Nazi added 11 focused parser tests and surfaced the one honest red case (`field X as number <-` silently accepted), and George closed that defect plus the exhaustiveness follow-through in commit `5212c9d`.







- Final validation for the batch is 2810/2810 passing.







- PRECEPT0019 scope is now explicitly narrow: `ExpressionFormKind` exhaustiveness belongs on `ParserState` and the expression-form handlers it owns, not as a wider parser-wide promotion.



---



---



---

---

### 2026-05-07T08:05:00Z: ParsedOutcome / ParsedExpression parser refactor recorded as the durable parse-time payload baseline







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (6 files -> 1 canonical entry).







**Merged sources:** `frank-outcome-expression-analysis.md`, `frank-outcome-impl-plan.md`, `george-outcome-plan-review.md`, `george-outcome-impl-notes.md`, `george-gap062-resolved.md`, `george-parsed-expression-created.md`.







- The parser-side payload contract is now durably recorded: closed-vocabulary slot values resolve at parse time, open-ended expressions flow through `ParsedExpression`, and outcomes no longer masquerade as synthetic `BinaryOperationExpression` nodes.







- Frank identified the synthetic outcome-operator encoding as a real defect, authored the replacement plan, and George reviewed that plan green before implementing it in commit `94dec3b`.







- Durable rule: parse-time outcomes use the `ParsedOutcome` DU (`TransitionOutcome`, `NoTransitionOutcome`, `RejectOutcome`, `MalformedOutcome`), so malformed rows stay explicit instead of falling back into unrelated expression forms.







- George's GAP-062 investigation remains preserved as the catalog-pressure signal that outcome syntax needed a first-class lane instead of ad hoc per-member parser branching.



---



---



---

---

### 2026-05-07T08:05:00Z: Catalog-driven parser slices 1-4 recorded with review corrections and status-quo rulings







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (11 files -> 1 canonical entry).







**Merged sources:** `george-parser-s1-findings.md`, `george-slice-1-notes.md`, `george-slice2-findings.md`, `george-slice-2-notes.md`, `george-slice-3-notes.md`, `george-slice-4-notes.md`, `frank-arrow-analysis.md`, `frank-eventhandler-analysis.md`, `frank-set-settype-analysis.md`, `frank-slice-3-review.md`, `frank-slice-4-review.md`.







- George's parser baseline is now durably recorded: catalog-driven construct parsing, sentinel slot values for absent optionals, `peek(2)` disambiguation, and a `ParserState`-hosted Pratt parser.







- Frank confirmed three suspected parser bugs were actually correct by design: Arrow's dual role is an intentional grammar split, EventHandler correctly has no Outcome slot, and `set` / `set type` disambiguation is a healthy three-layer collaboration rather than a defect.







- Frank's Slice 3 review surfaced one real parser defect (`is not <non-set>` could loop forever) while Slice 4 otherwise approved the direction and flagged only a stale parser-entry-point line in docs.







- Carry-forward constraint: parser helpers may dispatch on metadata shape, but they should not reintroduce duplicated per-member language knowledge outside the catalogs.



---



---



---

---

### 2026-05-07T04:02:01Z: Parser prerequisite decisions locked; `peek(2)` kept as a structural invariant







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (2 files -> 1 canonical entry).







**Merged sources:** `frank-parser-prereqs-b2-b3.md`, `frank-disambiguation-catalog.md`.







- Shane approved Frank's B2 + B3 parser-prerequisite decisions as the durable baseline for the parser/type-checker handoff.







- Closed-vocabulary slot values stay parser-resolved: `TypeExpressionSlot` carries `TypeMeta`, `ModifierListSlot` carries `ImmutableArray<ModifierKind>`, `BecauseClauseSlot` carries extracted string text, and `AccessModeSlot` must carry `TokenKind AccessMode` rather than span-only data.







- `docs/compiler/type-checker.md`'s `SlotValue` subtype table is stale for those slot contracts and remains the follow-up doc-sync target.







- The disambiguation rule is locked as `peek(2).Kind ∈ DisambiguationEntry.DisambiguationTokens`; no `Offset` field belongs on `DisambiguationEntry`.







- Rationale: state/event anchors are grammar-level single-token productions, so the offset never varies by construct kind; it is universal parser geometry, not per-member metadata.







- George is unblocked to implement `ParsedExpression.cs` (B1) and the paired `AccessModeSlot` fix against the approved slot-value contracts and invariant disambiguation rule.



---



---



---

---

### 2026-05-07T03:00:00Z: Wave 3 Round 2 canonical doc sweep recorded







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (1 file); Frank reported 3 pre-existing `SemanticIndex.cs` errors only.







**Merged source:** `frank-wave3-round2.md`.







- Closed 20 Wave 3 Round 2 markers across `docs/runtime/evaluator.md`, `docs/tooling/language-server.md`, `docs/tooling/mcp.md`, `docs/language/catalog-system.md`, and `docs/compiler/graph-analyzer.md`; `docs/compiler/diagnostic-system.md` CC#13 / CC#20 were verified complete with no doc edits needed.







- `evaluator.md` closed 7 items by finalizing the `EventOutcome` DU (`Faulted`, `Mutations`, enriched `Unmatched`), confirming `RejectReason`, locking `AmbiguousDispatch`, and updating the fire pseudocode plus the in-domain failures table.







- `language-server.md` closed 6 items by confirming `Compilation.Tokens`, `SemanticIndex.References`, `TypeMeta.IsUserFacing`, and `ActionMeta.Description` as the hover source, and by converting §13 open questions into decided notes.







- `mcp.md` closed 5 items by documenting null-data bootstrap, keeping `firePipeline` out of catalog scope, confirming `EnsuresByState`, carrying the mutations payload, and aligning unmatched output to `evaluatedRows` / `TransitionInspection`.







- `catalog-system.md` closed the `ConstraintMeta` five-subtype hierarchy marker, and `graph-analyzer.md` closed wildcard expansion ordering by locking declaration order inline.







- Preserved 6 follow-up gaps for owner attention in the canonical record: `TokenMeta.SemanticTokenModifiers` (#41), `EventCoverageEntry` granularity, back-edge definition, `GraphEvent.IsInitial` derivation, TBD structural diagnostic codes, and `ActionMeta` LS/MCP property alignment (#43).







- Validation reported: `dotnet build src/Precept/Precept.csproj` still shows only the 3 pre-existing `SemanticIndex.cs` errors (`TypedState`, `TypedField`, `TypedEvent` not found); no new errors were introduced.



---



---



---

---

### 2026-05-07T02:24:36Z: Wave 5 archive and cleanup recorded







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (1 file); Frank reported 3 pre-existing `SemanticIndex.cs` errors only.







**Merged source:** `frank-wave5-complete.md`.







- Retired `docs/working/` entirely (67 files) after a pre-deletion scan confirmed every surviving open question already lived in canonical docs; no rescued items were needed.







- Deleted the superseded radical proposals `docs/compiler/parser-radical.md` and `docs/compiler/type-checker-radical.md` because their design content had been absorbed into the canonical stage docs.







- Repaired broken references across 8 canonical docs: `docs/compiler/README.md`, `docs/compiler/parser.md`, `docs/compiler/proof-engine.md`, `docs/compiler/type-checker.md`, `docs/compiler/tooling-surface.md`, `docs/language/catalog-system.md`, `docs/language/precept-grammar.md`, and `docs/tooling/mcp.md`.







- `docs/language/catalog-system.md` now records the ActionMeta question as settled inline: `Description` is canonical, `SyntaxShape` stays internal, and `SnippetTemplate` remains deferred.







- Validation reported: `dotnet build src/Precept/Precept.csproj` still shows only the 3 pre-existing `SemanticIndex.cs` errors (`TypedState`, `TypedField`, `TypedEvent` not found); no new errors were introduced.







- Frank's cleanup landed in commit `421605a`.



---



---



---

---

### 2026-05-07T02:20:00Z: Wave 3 Round 1 canonical doc sweep recorded







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (1 file); Frank reported 3 pre-existing `SemanticIndex.cs` errors only.







**Merged source:** `frank-wave3-round1.md`.







- `docs/compiler/type-checker.md` closed CC#9 by switching `ConstraintFieldRefs.ConstraintIdentity` to the `ConstraintIdentity` DU, closed CC#11 by adding `TypedTransitionRow.RejectReason`, and removed the stale CC#1-era "No expression tree parsing" note.







- `docs/compiler/proof-engine.md` closed catalog-gap #12 (`TryLiteralProof` scope), catalog-gap #13 (Strategy 3 vs. 4 boundary), the CC#1 follow-through around initial-state satisfiability blocking text, the corresponding stale OQ block, and the CC#5 follow-through on `FieldModifierMeta.ProofDischarges`.







- `docs/runtime/precept-builder.md` closed CC#4 by restoring `Compilation.Tokens`, closed CC#11 by documenting `ExecutionRow.RejectReason`, and closed CC#7 by documenting the `ConstraintMeta.StateAnchored` DU hierarchy.







- Validation reported: `dotnet build src/Precept/Precept.csproj` still shows only the 3 pre-existing `SemanticIndex.cs` errors (`TypedState`, `TypedField`, `TypedEvent` not found); no new errors were introduced.



---



---



---

---

### 2026-05-07T02:13:50Z: Wave 4 final consistency pass recorded; 6 gaps closed and terminology sweep completed







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (1 file); Frank reported 3 pre-existing `SemanticIndex.cs` errors only.







**Merged source:** `frank-wave4-pass.md`.







- All 6 preserved follow-up gaps from Wave 3 Round 2 were resolved as team-autonomous and propagated into canonical docs with no owner-required items remaining.







- Closed the SemanticTokenModifiers question by documenting that Precept tokens carry zero LSP modifier bits, leaving `TokenMeta` unchanged and the language server hardcoding `tokenModifiers: 0`.







- Locked graph-analyzer semantics on three fronts: `EventCoverageEntry` remains event-level only, back-edges are BFS-tree ancestors, and `GraphEvent.IsInitial` is structurally derived from edges whose source state is initial.







- Assigned structural diagnostic codes 82-85 (`TerminalStateHasOutgoingEdges`, `IrreversibleStateHasBackEdge`, `RequiredStateDoesNotDominateTerminal`, `NoInitialState`) and confirmed proof-engine codes begin at 86.







- Settled the `ActionMeta` tooling pattern: `Description` surfaces in LS hover and MCP vocabulary, `SyntaxShape` stays internal, and `SnippetTemplate` remains a deferred catalog addition.







- Cleaned stale language across `docs/compiler/graph-analyzer.md`, `docs/language/catalog-system.md`, `docs/tooling/language-server.md`, and `docs/compiler/proof-engine.md`; corrected 6 `precept/preview` → `precept/inspect` terminology drifts in `docs/compiler/tooling-surface.md`; updated `docs/compiler/README.md` to mark `parser-radical.md` and `type-checker-radical.md` as superseded; and marked Waves 3 and 4 `✅ COMPLETE` in `docs/working/cross-cutting-decisions.md`.







- Validation reported: `dotnet build src/Precept/Precept.csproj` still shows only the 3 pre-existing `SemanticIndex.cs` errors (`TypedState`, `TypedField`, `TypedEvent` not found); no new errors were introduced.



---



---



---

---

### 2026-05-07T01:26:52Z: Wave 2 cross-cutting decisions all closed; Wave 1 checkbox drift corrected







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (1 file); Frank reported build clean.







**Merged source:** `frank-wave2-complete.md`.







- Wave 2 is durably closed: CC#5, CC#10, CC#13, CC#14, CC#15, CC#16, CC#17, CC#18, CC#19, CC#20, and CC#22 are all resolved and propagated into canonical docs.







- Canonical synchronization landed in `docs/working/cross-cutting-decisions.md`, `docs/language/catalog-system.md`, `docs/compiler/graph-analyzer.md`, `docs/runtime/evaluator.md`, `docs/compiler/diagnostic-system.md`, `docs/tooling/language-server.md`, `docs/compiler/type-checker.md`, and `docs/compiler/proof-engine.md`.







- Six Wave 1 display-sync errors were corrected without re-deciding the work: CC#3, CC#4, CC#6, CC#12, CC#23, and CC#24 now show `[x]` to match their already-resolved status rows; CC#26's status row is likewise corrected to `✅ Resolved`.







- Durable architecture takeaways: `GraphState` stays a derived-facts output record, `SlotContext` and `ConstructSlotKind` stay distinct, catalog metadata owns per-member language knowledge, and default-valued `readonly record struct` additions remain backward-compatible.



---



---



---

---

### 2026-05-07T01:26:35Z: Implementation-note discipline locked for active parser work







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (1 file).







**Merged source:** `copilot-directive-2026-05-07T01-26-35.md`.







- Shane directed all active implementation agents to keep high-quality running notes covering design decisions, tradeoffs, and anything non-obvious.







- Treat George, Frank, and Soup-Nazi note-taking as an execution requirement, not optional cleanup, so end-of-batch review can inspect the real reasoning trail.

---

### 2026-05-07: R4 gate stays separate from the comprehensive audit, and the grammar generator cannot replace the hand-authored grammar until parity exists







**By:** Shane







**Status:** Locked — recorded from the audit follow-up.







**Merged sources:** `frank-comprehensive-audit.md`, `shane-d7-d8-decisions.md`.







- D7 locked the process ruling: Frank's comprehensive audit did **not** count as the dedicated R4 final review, so George's GraphAnalyzer work stayed held in the inbox until the separate Frank + Soup-Nazi review path completed.



- D8 locked the tooling ruling: the grammar generator must reach hand-authored `tmLanguage.json` quality before it becomes canonical; no generated base + manual-edit hybrid workflow is allowed.



- Durable implication: the catalog-driven architecture still demands a single generated source of truth, but the current generator output remains scaffold-quality and must not overwrite production grammar assets yet.

---

### 2026-05-07: OQ1 anti-mirroring enforcement locks to a Roslyn analyzer







**By:** Shane Falik (via Copilot)







**Status:** LOCKED







**Merged source:** `copilot-oq1-anti-mirroring.md`.







- Anti-mirroring enforcement lands as a custom Roslyn analyzer that reports when `.Syntax` is accessed on a `SemanticIndex`-typed record outside the TypeChecker assembly or test assemblies.



- The architectural invariant is compile-time enforced because GraphAnalyzer, ProofEngine, and Builder must not read semantic data through syntax back-pointers.



- Tradeoff accepted: analyzer maintenance is heavier than a reflection/xUnit guard, but the guarantee is automatic and structurally stronger.

---

### 2026-05-07: OQ2 ContentValidation DU must land before Slice 4







**By:** Shane Falik (via Copilot)







**Status:** LOCKED







**Merged source:** `copilot-oq2-content-validation-timeline.md`.







- `ContentValidation` ships as its own DU commit before Typed Constants / Slice 4; Slice 4 must not carry a temporary hardcoded dispatch table.



- The chosen shape remains `RegexValidation`, `NodaTimeValidation`, and `ClosedSetValidation` sealed subtypes in `Types.cs`.



- Durable rule: avoid knowingly shipping short-lived metadata debt when the final catalog shape is already designed and small enough to land cleanly first.

---

### 2026-05-07: OQ3 CI enforcement remains TypeChecker logic until the rule surface grows







**By:** Shane Falik (via Copilot)







**Status:** LOCKED







**Merged source:** `copilot-oq3-ci-enforcement-cataloging.md`.







- The 5 stable `~string` CI enforcement rules stay in TypeChecker logic; no `CIEnforcementDiagnostic?` field is added to `BinaryOperationMeta` right now.



- The current rule set is considered stable and too small to justify new catalog metadata infrastructure.



- Revisit cataloging only if the rule surface expands again (explicitly, if a sixth CI rule appears).

---

### 2026-05-07: Diagnostic multi-location payload lands as `RelatedSpans` on `Diagnostic`







**By:** George







**Status:** Shipped







**Merged source:** `george-related-spans.md`.







- `RelatedSpan` lands as a `readonly record struct` alongside `Diagnostic`, and `Diagnostic.RelatedSpans` lands as an init-only property with an empty immutable-array default.



- Constructor stability was preserved deliberately so existing `Diagnostics.Create(...)` and positional-constructor call sites do not churn.



- Usage rule is now explicit: only attach `RelatedSpans` when a real secondary source location exists; absence cases remain single-span diagnostics with explanatory primary text.



- Validation closed green for `src\Precept\Precept.csproj` plus `test\Precept.Tests\Precept.Tests.csproj` (2931 tests at handoff).

---

### 2026-05-07: Parser metadata promotion lands `ExpressionFormMeta.BindingPower` and `ConstructSlot.TerminationTokens`







**By:** George







**Status:** Shipped







**Merged source:** `george-catalog-p4p5.md`.







- Member-access precedence is now catalog-owned through optional `ExpressionFormMeta.BindingPower`; the Pratt loop reads led-form metadata before falling back to operator metadata.



- Expression-bearing construct slots can now own their stop-token metadata through optional `ConstructSlot.TerminationTokens`, replacing bespoke parser termination lambdas with shared slot-driven logic.



- Binary operator precedence stays on the `Operators` catalog, and `is set` / `is not set` sequence validation remains parser-owned token-sequence checking.



- Regression coverage plus `docs/language/catalog-system.md` now document both metadata additions; validation closed green at 2949 tests.

---

### 2026-05-07: OutcomesCatalog coverage closes and missing required outcomes now emit `ExpectedOutcome`







**By:** Soup-Nazi







**Status:** Shipped







**Merged source:** `soup-nazi-outcomes-coverage.md`.







- Added `OutcomesCatalogTests.cs` to lock full-precept outcome dispatch across `transition`, `no transition`, and `reject`, including malformed-path, drift, and recovery anchors.



- The parser now treats a wholly missing required outcome as `ExpectedOutcome` instead of returning only a `MalformedOutcome` sentinel.



- Durable test rule: outcome work needs full-precept anchors, malformed/partial-form coverage, and recovery checks so later rows remain parsable.



- The manifest baseline supersedes the inbox note about excluded files: coordinator confirmed `ExpressionFormCatalogTests.cs` and `ParserExpressionTests.cs` both passed inside the 2949-test run.

---

### 2026-05-07: Parser gap fixes complete



**Commit:** 514f82f



**Bug 1 (Token collision):** `src/Precept/Language/Types.cs` line 644 — changed `dict[meta.Token.Kind] = meta` to `dict.TryAdd(meta.Token.Kind, meta)` in `BuildByToken()`. Base types (Log/Queue) now win over By-variants (LogBy/QueueBy) since they appear first in enum iteration order.



**Bug 2 (Event arg modifiers):** `src/Precept/Pipeline/Parser.cs` lines 697–703 in `ParseArgumentList` — after consuming the type token, now loops over `Modifiers.ByFieldToken` to collect any trailing field modifiers (optional, notempty, writable, nonnegative, etc.). Expanded `ArgumentListSlot` tuple to `(Name, Type, Modifiers)`, added `Modifiers` to `DeclaredArg` in `SymbolTable.cs`, and wired through `NameBinder` → `TypeChecker.PopulateEvents` so `TypedArg.Modifiers` and `IsOptional` are populated from parsed data.



**Test results:** 4/4 previously-failing tests now pass, 3029/3029 total passing.



**Any issues:** Two existing parser tests (`TypeExpression_QueueOfNumber_ProducesCollectionTypeReference`, `QueueOfNumber_TypeExpressionSlot_PreservesCollectionAndElementTypes`) were asserting the buggy `QueueBy` behavior — updated them to assert `Queue`. No other regressions.

---

### 2026-05-07: Slice 1 — Typed Symbol Population Complete



**By:** George (for Soup Nazi)



**Commit:** e882396



**What's implemented:**



- `ResolveTypeKind(ParsedTypeReference)` — pattern-matches on ParsedTypeReference DU subtypes (Simple, Collection, Choice, CI, Missing); resolves to `(TypeKind, TypeKind? ElementType, TypeKind? KeyType)`. Collection element types resolved recursively per D2.



- `PopulateFields(SymbolTable, CheckContext)` — iterates `symbols.Fields`, resolves TypeKind, extracts declared modifier kinds, reads `Types.GetMeta(resolvedType).ImpliedModifiers` for implied modifiers (D3 catalog-driven), computes IsOptional/IsWritable from modifier presence, builds TypedField records. Emits `TypeMismatch` for `MissingTypeReference` → `TypeKind.Error`.



- `PopulateStates(SymbolTable, CheckContext)` — iterates `symbols.States`, builds TypedState records with modifiers verbatim from DeclaredState. Tracks initial state count: first initial state recorded, second triggers `MultipleInitialStates` diagnostic. If states exist but none is initial → `NoInitialState` diagnostic. Zero terminal states is allowed (open lifecycle per D7).



- `PopulateEvents(SymbolTable, CheckContext)` — iterates `symbols.Events`, builds TypedArg from DeclaredArg (TypeKind from `arg.Type.Kind`), builds TypedEvent records.



- `Check()` wired — creates CheckContext, calls PopulateFields/States/Events, returns partial SemanticIndex via `BuildPartialSemanticIndex` (symbol tables + derived FrozenDictionary lookups + diagnostics populated; all normalized declaration arrays empty).







**DiagnosticCodes used:** `TypeMismatch` (18), `NoInitialState` (32), `MultipleInitialStates` (31)







**Known edge cases for Soup Nazi:**



- **DeclaredArg missing modifiers:** NameBinder's DeclaredArg doesn't carry `ImmutableArray<ParsedModifier>` or `IsOptional`. TypedArg.Modifiers is always empty and IsOptional is always false. 8 of 55 TypeCheckerSymbolTests fail because of this and upstream parser gaps (qualified types emit parse errors, queue/log type ambiguity). All 2974 baseline tests pass.



- **Qualified types (money in 'USD', etc.):** Parser emits parse-stage errors for these; CheckExpectingClean fails. The type resolution itself works correctly — SimpleTypeReference carries the right TypeMeta.



- **Queue vs QueueBy / Log vs LogBy:** Both share TokenKind.QueueType / LogType. Parser's CollectionTypeReference may carry wrong TypeMeta. Not a Slice 1 issue.



- **MissingTypeReference:** Emits TypeMismatch diagnostic to surface field-level impact. Parser already emits its own diagnostic for the missing token, so this is a belt-and-suspenders diagnostic.







**CheckContext fields populated in Slice 1:** Fields, FieldLookup, States, StateLookup, Events, EventLookup, Diagnostics







**SemanticIndex fields populated by Slice 1:** Fields ✓, States ✓, Events ✓, FieldsByName ✓, StatesByName ✓, EventsByName ✓, Diagnostics ✓ (type-checker diagnostics only; does not include parser/binder diagnostics). All other arrays are ImmutableArray.Empty / FrozenDictionary.Empty.

---

### 2026-05-07: Slice 2 — Scalar Expression Resolution Complete



**By:** George (for Soup Nazi)



**Commit:** 1111da4



**Resolve() arms implemented:** TypedLiteral, TypedFieldRef, TypedArgRef, TypedBinaryOp, TypedUnaryOp



**Stub arms (return TypedErrorExpression, no diagnostic):** FunctionCallExpression, CIFunctionCallExpression, MemberAccessExpression, MethodCallExpression, ConditionalExpression, QuantifierExpression, InterpolatedStringExpression, ListLiteralExpression, PostfixOperationExpression



**DiagnosticCodes used:**



- `UndeclaredField` (17): unknown identifier in expression — name not found in quantifier bindings, event args, or fields



- `DefaultForwardReference` (54): field referenced before its declaration when FieldScopeMode is PriorFieldsOnly



- `TypeMismatch` (18): no matching binary or unary operation for the given operand types (reusing existing diagnostic — fits the "expected X, got Y" pattern for operand type mismatches)







**Widening algorithm:** 4-level deterministic priority (§7.3/D16):



1. Exact: FindCandidates(op, lhs, rhs) — no widening



2. Left widen: for each l in lhs.WidensTo → FindCandidates(op, l, rhs)



3. Right widen: for each r in rhs.WidensTo → FindCandidates(op, lhs, r)



4. Both widen: for each l, r in cross product → FindCandidates(op, l, r)



First match wins. WidensTo array order is the tiebreaker (narrowest-first per catalog convention). Single-hop only (D15).







**Qualifier disambiguation:** When FindCandidates returns >1 entry (money/money, quantity/quantity divisions), DisambiguateCandidates selects QualifierMatch.Same by default — the structurally safe assumption. SameQualifierRequired is set on the TypedBinaryOp.ResultQualifier. ProofEngine adds deeper obligations. QualifierMatch.Different and Any produce null ResultQualifier.







**Known edge cases for Soup Nazi:**



- MissingExpression sentinel → TypedErrorExpression immediately, no diagnostic (parser already emitted one)



- Numeric literals: text containing `.` → Decimal, otherwise → Integer. Bottom-up only; context retry (amount > 100 where amount is money) is Slice 4.



- TypedConstant/TypedConstantStart literal kinds → TypedErrorExpression stub (Slice 4)



- Quantifier binding resolution returns TypedFieldRef (not a dedicated TypedQuantifierRef) — reuses the field ref shape



- GroupedExpression unwraps transparently (resolves inner)



- TokenKind → OperatorKind mapping goes through Operators.ByToken[(token, arity)]







**FieldScopeMode:**



- Set to PriorFieldsOnly when resolving default value or computed-field expressions (caller responsibility — Slice 5+ sets this)



- AllFields is the default for guards, actions, rules



- When PriorFieldsOnly: identifier resolution checks field's index against CurrentFieldIndex; >= triggers DefaultForwardReference diagnostic







**Test results:** 3021 passed, 8 failed (same 8 pre-existing DeclaredArg/qualified-type parser gaps from Slice 1). All 2974 baseline tests pass.

---

### 2026-05-07: Slice 3 Complete



**By:** George (for Soup Nazi)



**Commit:** fa87df9



**Arms implemented:** FunctionCall, CIFunctionCall, MemberAccess, MethodCall, InterpolatedString







**DiagnosticCodes used:**



- `UndeclaredFunction` (30) — function name not in `Functions.ByName`



- `InvalidMemberAccess` (20) — accessor name not found in `TypeMeta.Accessors` for receiver type



- `FunctionArityMismatch` (21) — no overload matches arg count (also used for method call param count)



- `TypeMismatch` (18) — no overload matches arg types after arity filter; also method call arg type mismatch







**FunctionCall edge cases:**



- Arity mismatch: collects all valid arities across FunctionMeta entries, reports "takes X or Y inputs"



- Type mismatch: reports after arity filter passes but no exact/widened match found



- Multi-FunctionMeta names (e.g., "round" → Round + RoundPlaces): all overloads scored across both entries



- Widened match scoring: score = count of widened args; lowest score wins; exact (0) short-circuits



- Context retry for literal args deferred to Slice 4







**CIFunctionCall:**



- Prepends `~` to parser-provided name and looks up `Functions.FindByName("~" + name)`



- If `~name` not in catalog → UndeclaredFunction diagnostic with the `~`-prefixed name



- CI enforcement (verifying first arg is ~string field) deferred to Slice 8







**MemberAccess edge cases:**



- Accessor lookup fails on types with no accessors (e.g., boolean, integer without accessors)



- Return type resolution via accessor DU: FixedReturnAccessor → .Returns, ElementParameterAccessor → Integer, base TypeAccessor → owning field's ElementType



- Element type extracted from TypedFieldRef via FieldLookup; returns Error if receiver isn't a field ref







**MethodCall:**



- Same accessor lookup as MemberAccess plus argument validation



- If accessor has ParameterType: expects exactly 1 arg with IsAssignable check



- If accessor has no ParameterType: expects 0 args







**InterpolatedString:**



- Each HoleSegment expression resolved recursively



- TextSegment → TypedTextSegment pass-through



- ErrorType propagation: ANY hole error → entire string becomes TypedErrorExpression



- Result TypeKind is always String (hardcoded in TypedInterpolatedString record)







**Helpers added:**



- `IsAssignable(source, target)` — identity + single-hop widening via TypeMeta.WidensTo



- `SelectOverload(candidates, args, name, span, ctx)` — overload scoring across multiple FunctionMeta entries



- `ResolveAccessorReturnType(accessor, receiver, ctx)` — accessor DU dispatch for return type



- `GetElementType(receiver, ctx)` — extracts element type from TypedFieldRef via FieldLookup







**Stub arms still returning TypedErrorExpression (no diagnostic):**



- ConditionalExpression (Slice 6)



- QuantifierExpression (Slice 9)



- ListLiteralExpression (Slice 9)



- PostfixOperationExpression (Slice 6)



- TypedConstant/TypedConstantStart literals (Slice 4)







**Notes for Soup Nazi:**



- Untracked WIP file `test/Precept.Tests/TypeChecker/TypeCheckerExpressionTests.cs` exists and won't compile (references `TypeChecker.CreateContext`/`ResolveExpression` which don't exist as public API). Move aside before running tests.



- Element type resolution only works for direct TypedFieldRef receivers. Chained collection access (e.g., `field.first.accessor`) won't resolve element type — acceptable for current language surface.



- No context retry for numeric literals in function args (deferred to Slice 4).



- ProofRequirements from overloads/accessors are propagated to TypedFunctionCall/TypedMemberAccess.

---

### 2026-05-07: Slice 4 Complete



**By:** George (for Soup Nazi)



**Commit:** `ac95de2`







**TypedTypedConstant triggers:** A string literal becomes `TypedTypedConstant` (instead of `TypedLiteral`) when:



1. The literal's `LiteralKind` is `TokenKind.TypedConstant` (single-quoted string in DSL), AND



2. An `expectedType` context is provided (non-null, non-Error), AND



3. The target type's `TypeMeta.ContentValidation` is non-null (Date, Time, DateTime, Period, Currency, UnitOfMeasure, Dimension).







Without `expectedType` context, a typed constant emits `UnresolvedTypedConstant` and returns `TypedErrorExpression`.







**ContentValidation dispatch:**



- `NodaTimeValidation` → Date (`LocalDatePattern.Iso`), Time (`LocalTimePattern.ExtendedIso`), DateTime (`LocalDateTimePattern.ExtendedIso`), Period (`PeriodPattern.NormalizingIso`)



- `ClosedSetValidation` → Currency (ISO 4217 codes, case-insensitive), UnitOfMeasure (recognized units, case-insensitive), Dimension (recognized families, case-insensitive)



- `RegexValidation` → general pattern match via `System.Text.RegularExpressions.Regex.IsMatch`







On validation failure → `InvalidTypedConstantContent` diagnostic + `TypedErrorExpression`.







**DiagnosticCodes used:**



- `UnresolvedTypedConstant` (52) — typed constant with no type context



- `InvalidTypedConstantContent` (53) — typed constant content fails validation







**Context threading:** `expectedType` is passed as an optional `TypeKind?` parameter to `Resolve(expr, ctx, expectedType)`. Callers set it:



- Field defaults: caller passes `field.ResolvedType` (wiring deferred to when default resolution is implemented)



- Binary op context retry: when bottom-up fails and one operand is a literal, re-resolve with the other side's type



- Function call context retry: when overload resolution fails, re-resolve literal args with each candidate parameter type







For Soup Nazi test setup: call `TypeChecker.ResolveExpression(expr, ctx, expectedType: TypeKind.Date)` to test typed constant resolution with context. Without the expectedType, typed constants will emit `UnresolvedTypedConstant`.







**Valid typed constant examples:**



- `'2026-01-15'` with expectedType=Date → `TypedTypedConstant(Date, "2026-01-15", LocalDate(2026,1,15))`



- `'USD'` with expectedType=Currency → `TypedTypedConstant(Currency, "USD", "USD")`



- `'09:30:00'` with expectedType=Time → `TypedTypedConstant(Time, "09:30:00", LocalTime(9,30,0))`







**Invalid typed constant examples:**



- `'2026-13-01'` with expectedType=Date → `InvalidTypedConstantContent` (invalid month)



- `'XYZ'` with expectedType=Currency → `InvalidTypedConstantContent` (not in ISO 4217)



- `'not-a-time'` with expectedType=Time → `InvalidTypedConstantContent` (NodaTime parse failure)







**NodaTime parsers per type:**



- Date → `LocalDatePattern.Iso.Parse()` (pattern: `uuuu'-'MM'-'dd`)



- Time → `LocalTimePattern.ExtendedIso.Parse()` (pattern: `HH':'mm':'ss`)



- DateTime → `LocalDateTimePattern.ExtendedIso.Parse()` (pattern: `uuuu'-'MM'-'dd'T'HH':'mm':'ss`)



- Period → `PeriodPattern.NormalizingIso.Parse()` (normalizing ISO 8601)

---

### 2026-05-07: Slice 5 Complete



**By:** George (for Soup Nazi)



**Commit:** `687d364`



**What's now populated:** TransitionRows, EventHandlers, StateReferences, EventReferences (in BuildPartialSemanticIndex — no longer empty arrays)







**FromState wildcard:** `StateTargetSlot.StateName == null` triggers `FromState == null` (any-state wildcard, D10). The parser emits `StateName = null` when the `*` wildcard syntax or missing state target is used. No error diagnostic — this is intentional "fires in any state" semantics.







**DiagnosticCodes used:**



- `UndeclaredState` (28) — FromState or ToState name not found in StateLookup



- `UndeclaredEvent` (29) — Event name not found in EventLookup



- `UndeclaredField` (17) — Action target field not found in FieldLookup (via ResolveActionTarget)



- Plus any codes from `Resolve()` for guard/action expression resolution







**ActionSecondaryRole (D5):**



- `null` — AssignAction, CollectionValueAction, FieldOnlyAction, RemoveAtAction, CollectionIntoAction



- `ActionSecondaryRole.Key` — CollectionValueByAction (appendBy/enqueueBy ordering key), PutKeyValueAction (lookup key)



- `ActionSecondaryRole.Index` — InsertAtAction (insertion index)



- Invariant enforced: `SecondaryRole.HasValue == (SecondaryExpression != null)` — structurally guaranteed by construction







**Action DU mapping:**



- `AssignAction` → `TypedInputAction` (no secondary)



- `CollectionValueAction` → `TypedInputAction` (no secondary)



- `CollectionIntoAction` → `TypedBindingAction` (optional into target)



- `FieldOnlyAction` → `TypedAction` base (clear)



- `CollectionValueByAction` → `TypedInputAction` (SecondaryRole.Key)



- `InsertAtAction` → `TypedInputAction` (SecondaryRole.Index)



- `RemoveAtAction` → `TypedInputAction` (index as primary InputExpression)



- `PutKeyValueAction` → `TypedInputAction` (SecondaryRole.Key)



- `CollectionIntoByAction` → `TypedBindingAction` (optional into target)



- `MalformedAction` → `TypedAction` base (error sentinel)







**Guard resolution context:** AllFields scope (not PriorFieldsOnly). Event args in scope via `CurrentEventArgs` set from resolved event. Guards can reference any field and all event args.







**EventHandler body scope:** Event args in scope via `CurrentEventArgs` (same pattern as transition rows). AllFields scope for action expressions.







**D26 assert location:** End of `PopulateTransitionRows()` and `PopulateEventHandlers()` — Debug.Assert checks that if any TypedErrorExpression exists in resolved rows/handlers, at least one Error-severity diagnostic was emitted.







**Notes for Soup Nazi:**



- `CurrentEventArgs` is saved/restored via try/finally to ensure scope cleanup even on exceptions.



- `ResolveActionTarget` is a new helper that resolves IdentifierExpression targets to (fieldName, fieldType) and records FieldReferences.



- ProofRequirements on actions come from `Actions.GetMeta(kind).ProofRequirements` — they flow through to TypedAction without additional checking (ProofEngine responsibility).



- `ContainsErrorExpression` / `ContainsErrorExpressionInAction` are intentionally shallow checks for D26 — they don't walk nested expression trees. Full deep traversal is Slice 10's responsibility.

---

### 2026-05-07: Slice 6 Complete



**By:** George (for Soup Nazi)



**Commit:** fe358ef



**Structural checks implemented:** IsSet/IsNotSet expression resolution, choice domain validation (empty + duplicate), computed-field cycle detection (DFS), forward-reference belt-and-suspenders on ComputedDeps.



**DiagnosticCodes used:** IsSetOnNonOptional (49), EmptyChoice (46), DuplicateChoiceValue (45), CircularComputedField (40), DefaultForwardReference (54). No new codes — all pre-existing.



**Reachability algorithm:** N/A — reachability is GraphAnalyzer's responsibility per §14. Slice 6 per §13 is IsSet/IsNotSet + computed deps + choice validation + forward-ref belt-and-suspenders.



**Cycle detection:** Three-color DFS on ComputedDeps adjacency graph. O(n) construction + O(n) traversal. Currently a no-op because ComputedDeps is empty until computed expression resolution is wired.



**Choice validation:** During PopulateFields, when type is ChoiceTypeReference: empty domain → EmptyChoice, duplicate values → DuplicateChoiceValue. Uses HashSet for O(n) duplicate detection.



**IsSet/IsNotSet:** ResolvePostfixOp validates operand is an optional field or optional arg. Non-optional → IsSetOnNonOptional. Non-field/arg operand → IsSetOnNonOptional.



**Forward-ref belt-and-suspenders:** Post-hoc validation that ComputedDeps entries don't reference fields at or after their own declaration index. Redundant with D8 enforcement in ResolveIdentifier.



**Edge cases for Soup Nazi:** Stateless precepts (no states) pass through — no structural checks depend on state presence. Single-state precepts pass. Precepts with no transitions pass. ComputedDeps being empty causes cycle detection and forward-ref check to be no-ops (correct — no computed expressions resolved yet). PostfixOp test updated from stub assertion to IsSetOnNonOptional assertion.



**Test delta:** 3177 passing (up from 3170 baseline). 19 pre-existing TypeCheckerTransitionTests failures from Slice 5 revert — not introduced by this slice.

---

### 2026-05-07: Slice 7 Complete



**By:** George (for Soup Nazi)



**Commit:** `687d364` (co-committed with Slice 5 due to parallel file edits)



**ValidateModifiers scope:** Both TypedFields and TypedEventArgs



**DiagnosticCodes used:** `InvalidModifierForType` (33), `DuplicateModifier` (36), `RedundantModifier` (37), `WritableOnEventArg` (41), `ComputedFieldNotWritable` (38)



**New DiagnosticCodes added:** None — all codes already existed



**Catalog API used:** `Modifiers.GetMeta(kind)` → `FieldModifierMeta.ApplicableTo` (TypeTarget[]), `ModifierMeta.MutuallyExclusiveWith` (ModifierKind[]), `FieldModifierMeta.Subsumes` (ModifierKind[]), `Types.GetMeta(resolvedType).ImpliedModifiers`, `Types.GetMeta(resolvedType).DisplayName`



**Conflict detection:** Iterates `MutuallyExclusiveWith` array on each modifier; if any conflict member is already in the `seen` set → emits `InvalidModifierForType` with conflict description



**Redundant modifier detection:** Two sources: (1) `FieldModifierMeta.Subsumes` — if another explicit modifier subsumes this one → `RedundantModifier` warning; (2) `TypeMeta.ImpliedModifiers` — if the type already implies this modifier → `RedundantModifier` warning



**Notes for Soup Nazi:** `IsTypeApplicable` handles both simple `TypeTarget` (kind match) and `ModifiedTypeTarget` (kind + required modifiers). Empty `ApplicableTo` array means "any type" — no validation needed. Writable checks are the only non-catalog-driven dispatch (`kind == ModifierKind.Writable`) — these are structural constraints on the modifier's semantics, not type applicability. 7 pre-existing test failures from Slice 5 transition row processing (UndeclaredField on event arg member access); no new failures from Slice 7.

---

### 2026-05-07: Decision: PRECEPT0024 Anti-Mirroring Enforcement Implemented







**By:** Newman (MCP/AI Dev)







**Status:** Done — merged from inbox.







**Merged source:** `newman-precept0024-implemented.md`.







## Context







OQ1 in `docs/compiler/type-checker.md` §13 locked the decision that `.Syntax` back-pointers on `Typed*` records must only be accessed inside `TypeChecker`. GraphAnalyzer, ProofEngine, and Builder must consume typed semantic data — never parse-tree back-pointers. The enforcement mechanism was specified as a Roslyn analyzer.







## Decision







Implemented `PRECEPT0024` as a Roslyn analyzer in `src/Precept.Analyzers/Precept0024AntiMirroringEnforcement.cs`.







- **Diagnostic ID:** PRECEPT0024



- **Severity:** Error



- **Mechanism:** `RegisterOperationAction` on `OperationKind.PropertyReference`



- **Guard:** Fires when `.Syntax` is accessed on any of 10 guarded `Typed*` record types (`TypedField`, `TypedState`, `TypedEvent`, `TypedTransitionRow`, `TypedRule`, `TypedEnsure`, `TypedAccessMode`, `TypedStateHook`, `TypedEventHandler`, `TypedEditDeclaration`) outside the `TypeChecker` class in `Precept.Pipeline` namespace.



- **Allowed:** Access inside `TypeChecker` (including nested types). Test code uses `#pragma warning disable PRECEPT0024` where needed.



- **Type resolution:** Uses `IPropertyReferenceOperation` with namespace-qualified type checks to avoid false positives on unrelated types.







## Tests







8 tests in `test/Precept.Analyzers.Tests/Precept0024Tests.cs`:



- 4 true positives: GraphAnalyzer, ProofEngine, Builder, lambda-in-non-TypeChecker



- 4 true negatives: inside TypeChecker, non-guarded type, non-Syntax property, nested class in TypeChecker







## Impact







- Closes OQ1 from type-checker.md §13.



- No MCP surface changes required — this is a compile-time enforcement mechanism only.

---

### 2026-05-07: GraphAnalyzer OQ1 — DeadEndStateFact is a separate fact from TerminalCompletenessFact



**By:** Frank (frank-graphanalyzer-oqs)



**What:** Dead-end states get a new, separate `DeadEndStateFact` rather than being an expansion of `TerminalCompletenessFact`. New `DiagnosticCode.DeadEndState = 108` (Warning) added. Detection uses reverse-reachability BFS from terminal states in Phase 2.



**Why:** Clean separation of concerns — TerminalCompletenessFact assesses reachability of terminal states; DeadEndStateFact identifies states with no outbound transitions to terminals. Mixing them would conflate two distinct structural properties.

---

### 2026-05-07: GraphAnalyzer OQ2 — EventHandlers structurally excluded from EventCoverage



**By:** Frank (frank-graphanalyzer-oqs)



**What:** TypedEventHandler entries do NOT count toward event coverage and cannot coexist with the graph analyzer in any valid precept. EventHandlers are only valid in stateless precepts (PRECEPT0092 `EventHandlerInStatefulPrecept` blocks them in stateful precepts). The graph analyzer only runs on stateful precepts. The coexistence scenario is structurally impossible. Corrected graph-analyzer.md §4 which incorrectly claimed event handlers were consumed for coverage.



**Why:** This was a doc error, not a policy question. The language semantics make it impossible.



---



---



---

---

### 2026-05-06T23:51:33Z: Event-interaction UXR closes OQ-8 and OQ-9; document is now complete







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (2 files).







**Merged sources:** `elaine-oq8-closed.md`, `elaine-oq9-closed.md`.







- OQ-8 is durably closed: the Data Form supports both commit modes, with per-field blur commit for fields outside multi-field constraints and buffered Save/Cancel for fields participating in multi-field constraints.







- The commit mode is derived from `FieldAccessInfo` constraint metadata; the UI does not introduce manual per-field configuration.







- OQ-9 is durably closed: the Event Timeline reflects only the current committed state, and fire actions remain disabled while buffered edits are pending.







- The preview surface does not make a hypothetical inspect/fire call against uncommitted edits; the user must save or discard before interacting with event firing.







- With OQ-6 already closed in the prior merged entry, all event-interaction UXR open questions are now resolved and `docs/working/elaine-ux-requirements-event-interaction.md` is complete.



---



---



---

---

### 2026-05-06T23:45:58Z: Event-interaction UXR closes OQ-1, OQ-3, and OQ-5; rule-failure descriptions are universal







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (7 files).







**Merged sources:** `copilot-directive-rule-failure-descriptions.md`, `elaine-33-corrections.md`, `elaine-oq1-closed.md`, `elaine-oq3-closed.md`, `elaine-oq5-closed.md`, `elaine-oq6-closed.md`, `frank-guard-summary-added.md`.







- OQ-1 is durably closed: user-facing surfaces normalize certain-reject outcomes to **Blocked**, per the semantic visual-system spec.







- OQ-3 is durably closed in V1: collection event args (`set of T`, `list of T`) use pill/tag input rather than a deferred follow-up control.







- OQ-5 is durably closed on the runtime contract: `TransitionInspection` provides `GuardSummary: string?`, and the UI renders that summary directly instead of parsing DSL source or inventing a fallback.







- OQ-6 is durably closed in V1: event cards use the event name as the sole label, with no authored description, generated transition-summary copy, or hover help text beyond that name.







- Rule-failure descriptions are a universal runtime contract: every rule-failure surface must provide a human-readable reason, not just guard summaries or constraint failures already carrying `because`.







- Elaine-33's API accuracy pass is folded forward: `InspectUpdate` references `ConstraintResult`, fire-outcome prose uses `EventOutcome.ConstraintsFailed`, `TransitionInspection` references align to the `RowEffect` DU, and `datetime` remains a valid Precept type.



---



---



---

---

### 2026-05-06T19:11:15Z: CC#8 EventInspection shape resolved; `ArgErrorKind` rejected and `RowEffect` DU adopted







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (4 files).







**Merged sources:** `frank-cc8-fit-assessment.md`, `frank-cc8-resolved.md`, `copilot-directive-cc8-resolutions-20260506.md`, `elaine-32-corrections.md`.







- OQ-2 is closed: `ArgError` stays `(ArgName, Reason)` only. No `ArgErrorKind` discriminator is added.







- Arg input error display now mirrors field-edit error display: show the reason string inline; do not branch UI or agent behavior on error kind.







- OQ-3 is closed in favor of the `RowEffect` DU (`TransitionTo`, `NoTransition`, `Rejection`) instead of an enum-plus-nullables shape.







- Frank's fit assessment remains the durable acceptance bar: once those two blockers closed, the proposal fit Elaine's UX spec and the remaining source/proposal drift became implementation follow-through rather than a design blocker.







- `event-inspection-proposal.md` was updated, CC#8 is resolved in the cross-cutting register, and CC#12 is now unblocked.



---



---



---

---

### 2026-05-06T18:41:27Z: Event card taxonomy locks to four visible states and dialog-based arg firing







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (7 files; review findings folded into the corrected final model).







**Merged sources:** `copilot-directive-event-undefined-hide-20260506.md`, `copilot-directive-no-args-possible-impossible-20260506.md`, `copilot-directive-event-firing-interaction-20260506.md`, `elaine-30-corrections.md`, `elaine-31-corrections.md`, `frank-elaine-ux-review.md`, `george-elaine-ux-review.md`.







- Undefined events are absent from the `Event Timeline`; only events that are defined for the current state can render as unavailable/disabled.







- `DeclaredArgs.Length == 0 && OverallProspect == Possible` is structurally impossible, so the phantom zero-arg Ready-Uncertain state is removed.







- The durable event-card taxonomy is four visible states: Unavailable, Blocked, Needs Input, and Ready-Certain.







- Events with declared args open a dialog and commit through the dialog OK action; zero-arg events fire directly from the event card.







- The canonical semantic-visual-system HTML, not legacy inline-expansion prose, owns disabled-token treatment, direct-fire card affordance, and warning-color reject styling.







- Frank and George's review pass is preserved as a durable warning that proposal-only inspection fields and names must be called out as CC#8 dependencies until the implementation ships.



---



---



---

---

### 2026-05-06T18:25:02Z: Event-interaction personas, surface model, and create/edit/fire mental model corrected







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (10 files).







**Merged sources:** `copilot-directive-persona-20260506.md`, `copilot-directive-persona-nuance-20260506.md`, `copilot-directive-authoring-spectrum-20260506.md`, `copilot-directive-persona-1-3-enduser-20260506.md`, `copilot-directive-conflict-a-ruling-20260506.md`, `copilot-directive-conflict-b-ruling-20260506.md`, `copilot-directive-conflict-c-ruling-20260506.md`, `copilot-directive-three-path-model-20260506.md`, `copilot-directive-constructor-event-20260506.md`, `elaine-29-corrections.md`.







- Persona 1.1 is the Business Analyst / Domain Expert, not a software developer; Persona 1.3 is the End-User who operates a Precept-governed product with no DSL awareness.







- DSL authoring is a full-human ↔ AI-assisted spectrum. AI help is first-class, but never required.







- `Event Timeline` is the canonical surface name; `event landscape` is a legacy error everywhere it appears.







- `Data Form` and `Event Timeline` are peer surfaces. When a constrained layout forces priority, `Data Form` wins.







- The panel supports three user interactions: instance creation via the constructor event, lifecycle event firing, and direct data editing. Only firing and editing are change paths on an existing instance.







- `InspectUpdate` must return the hypothetical post-patch access modes on its existing response so conditional field unlock UX stays on one runtime surface.



---



---



---

---

### 2026-05-06T10:41:33Z: Event-interaction UX baseline established under current-architecture rules







**By:** Scribe







**Status:** Merged, deduplicated, inbox cleared (3 files; later same-day corrections folded forward).







**Merged sources:** `copilot-directive-20260506.md`, `elaine-event-ux-requirements.md`, `elaine-ux-research-pass.md`.







- Durable integration rule: when legacy prototype visual-system assumptions conflict with the current compiler/runtime direction, the current architecture is canonical.







- Requirement and workflow conflicts are not silently normalized; they are surfaced to Shane for ruling.







- Elaine's requirements baseline now explicitly covers both event firing and direct data editing, uses canonical semantic tokens and surface vocabulary, and treats stateless precepts as first-class.







- Same-day review follow-through superseded the original inline-expansion and five-state assumptions; the corrected durable state is captured by the later 18:25, 18:41, and 19:11 entries.

---

### 2026-05-06: Wave 1 cross-cutting facilitation started with CC#7 first







**By:** Frank







**Status:** Recommendation recorded from inbox; Shane decision still needed on CC#7.







**Merged source:** `frank-wave1-start.md`.







- Wave 0 is treated as complete (CC#1, CC#2, CC#25), and Wave 1 sequencing starts with CC#7, then CC#9, CC#8, CC#12, CC#3/CC#4/CC#6/CC#23/CC#24, then CC#11.







- Frank recommends keeping the hierarchical `ConstraintMeta.StateAnchored` intermediate: builder routing still matches all five concrete leaves, while other consumers retain a structural "is state-scoped" grouping node.







- Once Shane rules on CC#7, the CC#9 follow-through and the catalog-system example cleanup are mechanically unblocked.



---



---



---

---

### 2026-05-11T22:41:49Z: Proof Engine Qualifier Coverage — Part B (Slices 7+8+9)



**By:** Scribe



**Status:** Merged from George's inbox note.



**Merged source:** `george-proof-qualifier-coverage-partb.md`.



- Added `QualifierCompatibilityProofRequirement` on `QualifierAxis.Currency` to all 8 money operations so same-currency enforcement is no longer implicit-only.

- Introduced `QualifierChainProofRequirement` for cross-type qualifier validation on `ExchangeRateTimesMoney` and `PriceTimesQuantity`, with dual-axis comparison support.

- Added Unit→Dimension fallback in `ResolveQualifierOnAxis()` so dimension-only fields can satisfy unit-axis proof obligations.

- Validation landed with 19 new ProofEngine tests and 193/193 proof tests passing.

---

### 2026-05-11T22:41:49Z: TypeChecker Slices 10 + 11 Complete



**By:** Scribe



**Status:** Merged from George's inbox note.



**Merged source:** `george-typechecker-slices-done.md`.



- Extended `ValidateAssignmentQualifiers` to recursively extract leaf operands from binary/unary expression trees before applying the existing qualifier checks.

- Added `FromCurrency` and `ToCurrency` switch arms so exchange-rate assignments now validate both currency sides.

- Preserved the existing proof-engine boundary for bare-expression assignment gaps that still need structural provenance.

- Shipped with 10 new assignment-qualifier tests.

---

### 2026-05-11T22:41:49Z: Slice 2 Complete: Full Type-Grammar Matching for Interpolated Typed Constants



**By:** Scribe



**Status:** Merged from George's inbox note.



**Merged source:** `george-slice2-done.md`.



- Replaced the stubs with segment-aware matching against per-type interpolated typed-constant forms.

- Added temporal compound matching plus slot compatibility checks for magnitude, currency, unit, whole-value, and compound slots.

- Introduced diagnostics for invalid forms, unsupported types, hole-type mismatches, and dimension/unit mismatches.

- Closed with 39 new tests and 129 typed-constant tests passing.

---

### 2026-05-11T22:41:49Z: Slice 1 (Parser) Complete



**By:** Scribe



**Status:** Merged from George's inbox note.



**Merged source:** `george-slice1-done.md`.



- Added the `InterpolatedTypedConstantExpression` AST and rewrote parsing to emit the full segment structure.

- Updated NameBinder and TypeChecker routing so holes still bind correctly while the type checker owns slot classification.

- Added the interpolated typed-constant expression form to the catalog and moved `TypedConstantStart` to that form.

- Parser coverage landed with 10 round-trip tests.

---

### 2026-05-11T22:41:49Z: string Excluded from Typed Constant Interpolation Holes



**By:** Scribe



**Status:** Merged from Frank's inbox note.



**Merged source:** `frank-string-excluded-from-interpolation.md`.



- `string` stays invalid in every typed-constant interpolation hole position; compile-time rejection is the canonical behavior.

- The decision restores the prior compile-time guarantee and rejects runtime-deferral as a structural escape hatch.

- No new diagnostic code is needed because `InterpolatedTypedConstantHoleTypeMismatch` already covers the failure.

---

### 2026-05-12T01:05:25Z: inventory-item coverage audit confirms compound-unit interpolation follow-up



**By:** Scribe



**Status:** Merged from Frank's inbox note.



**Merged source:** `frank-inventory-item-coverage.md`.



- `samples/inventory-item.precept` still needs a typed-constant follow-up for compound-unit interpolation: forms like `'{StockingUnit}/{PurchaseUnit}'` and `'0 {StockingUnit}/{PurchaseUnit}'` are outside the current `unitofmeasure` and `quantity` interpolation grammars, so Slice 2 needs a Slice 2B-style extension for compound-unit patterns plus dimensional validation.

- BUG-B remains covered once interpolated typed constants and Slice 9's Unit→Dimension fallback land; the Rate × money path is already covered by existing operation/catalog work.

- BUG-A still looks like an interpolation-driven cascade rather than a separate proof defect, but explicit regressions for event-arg qualifier use in `ensure` comparisons and arithmetic expressions should ship before calling that path closed.

- Remaining sample fallout after the plan lands is design-level, not compiler-level: `SupplierUnitCost` is modeled as `money` where `price` semantics are needed, `Sku is set` still targets a non-optional field, and the average-cost calculation still needs a division-by-zero guard.

---

### 2026-05-12T01:05:25Z: Slice 11B shipped and unblocked temporal price-chain validation



**By:** Scribe



**Status:** Merged from George's inbox note.



**Merged source:** `george-slice11b-complete.md`.



- `price of 'time'` and `price of 'date'` now stay on the existing `of` qualifier surface: `ExtractQualifiers` routes those price qualifiers to temporal dimensions while `quantity of 'time'` remains invalid.

- The proof pipeline now carries the required temporal comparison infrastructure: `ExtractComparableValue` understands `TemporalUnit` / `TemporalDimension`, `ResolveQualifierOnAxis` falls back `Dimension → TemporalDimension`, and `TypeMeta.ImpliedQualifiers` lets `duration` contribute implied temporal-denominator metadata.

- MCP type-catalog output now serializes implied qualifiers, and George locked the implementation with 13 new Slice 11B tests. Slice 12 can proceed on top of the completed temporal denominator substrate.

- Tooling follow-up remains open for Kramer: `price of ...` completions should offer `'time'` and `'date'` alongside physical dimensions.

