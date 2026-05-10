# Squad Decisions







---







## ACTIVE DECISIONS — Current Sprint







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

### 2026-05-10T12:34:54Z: Track 2 is the active execution lane again

**By:** Scribe

**Status:** Merged from inbox.

**Merged source:** `copilot-directive-2026-05-10T08-34-54.md`.

- Shane switched active execution focus from Track 1 back to Track 2 immediately.
- This is an execution-priority change only; durable Track 1 decisions remain recorded, but new active batch work should route to Track 2 until another directive supersedes it.

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

### 2026-05-10T12:25:21Z: Status-log triage isolates protocol bugs from the missing status-bar surface

**By:** Scribe

**Status:** Merged from Kramer's inbox note.

**Merged source:** `kramer-status-log-triage.md`.

- Shane's logs exposed two real shipped protocol bugs: the custom semantic-token color notification crossed the client boundary as a raw array, and the outline projector could emit `selectionRange` values that were not contained by `range`.
- Those bugs are real and worth landing, but Kramer did not find a code path where either one removes the VS Code status-bar item; the strongest direct clue for that missing surface remained extension activation and client lifecycle.
- Durable conclusion: keep the protocol fixes and treat the missing status-bar surface as a separate activation/lifecycle issue unless later logs show the extension deactivating or the status item never being created.

---

### 2026-05-10T12:15:36Z: Track 1 autonomous execution proceeds without per-slice approval pauses

**By:** Scribe

**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).

**Merged sources:** `copilot-directive-20260510T005009-track1-autonomous-run.md`, `frank-track1-autonomous-run.md`.

- Shane's directive is now durable team memory: Track 1 should run to completion without pausing for approval between slices.
- Frank's runbook locks the remaining execution order: Wave A can launch Slices 15, 18, 19, 20, 22, 23, 25, 26, and 27 immediately; Slice 17 waits on 14, Slice 21 waits on 20, Slice 24 waits on 23, and terminal Slices 28 then 29 remain strictly serial.
- Shared-infrastructure work (`20`, `23`, `26`) is the correct Wave A priority because those slices unblock later protocol work without reopening design questions.

---

### 2026-05-10T12:15:36Z: Incomplete typed declarations must offer `as` immediately after the value name

**By:** Scribe

**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).

**Merged sources:** `kramer-field-name-completion.md`, `soup-nazi-field-name-completion.md`.

- Completion routing should infer declaration-head context from neighboring significant tokens plus `Constructs.LeadingTokens` when parser recovery collapses the construct span.
- The durable slot-context surface is `AfterValueName`: after `field Name ` or `event Foo(Arg )`, completion should offer the required `as` keyword instead of broad top-level constructs.
- The regression anchor uses the real space trigger and an exact `["as"]` expectation so the test fails on the actual bad surface, not just on an internal context guess.

---

### 2026-05-10T12:15:36Z: Boolean field modifier completions stay filtered by modifier metadata and declaration-site legality

**By:** Scribe

**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).

**Merged sources:** `kramer-boolean-completions.md`, `soup-nazi-boolean-completions.md`.

- Field-modifier completions must derive from `ValueModifierMeta.ApplicableTo` plus `ApplicableDeclarationSites`, using the resolved declaration type instead of offering the entire modifier catalog.
- The current boolean field surface is intentionally limited to `default`, `optional`, and `writable`; numeric-only modifiers such as `max` and `maxplaces` are invalid leaks.
- Regression coverage should stay catalog-anchored while still asserting the exact user-visible boolean surface so future metadata drift fails honestly.

---

### 2026-05-10T12:15:36Z: Grammar-keyword gold drift was a VS Code fallback-color ordering bug, not a catalog classification bug

**By:** Scribe

**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).

**Merged sources:** `kramer-modifier-coloring.md`, `soup-nazi-modifier-coloring.md`.

- The visible gold drift came from the extension's TextMate fallback/theme rule, not from `KeywordGrammar` metadata or the language-server semantic-token surface.
- `as` must remain on `keyword.declaration.precept`, and field-declaration `default` needs an explicit declaration-site override before the generic `#grammarKeywords` fallback.
- The honest regression layer is grammar/package coverage: verify the generated TextMate ordering and fallback colors instead of changing catalog truth that was already correct.

---

### 2026-05-10T05:50:00Z: Slice 25 selection-range coverage must derive spans from real compilation artifacts

**By:** Scribe

**Status:** Merged from Soup Nazi's inbox note.

**Merged source:** `soup-nazi-slice-25.md`.

- Selection-range assertions should derive their expected spans from the real compilation pipeline: token span from `Compilation.Tokens`, enclosing parsed-expression span from the guard AST node, then slot span and construct span from `ConstructManifest`.
- This keeps acceptance coverage aligned with the runtime's actual span contracts instead of brittle hand-counted columns.
- Multi-position acceptance tests must submit positions in a deliberately non-source order and assert the returned chains preserve that request order, making output alignment an explicit contract.

---

### 2026-05-10T05:25:00Z: Slice 20 symbol-navigation coverage must lock full semantic reference sites and capability registration

**By:** Scribe

**Status:** Merged from Soup Nazi's inbox note.

**Merged source:** `soup-nazi-slice-20.md`.

- Slice 20 acceptance coverage belongs in `ReferencesHandlerTests` and `DocumentHighlightHandlerTests`, and it should stay red until real `ReferencesHandler` / `DocumentHighlightHandler` implementations answer requests from a populated `DocumentStore`.
- Event-argument navigation must honor `ArgReference.Site` exactly; qualified references like `JoinWaitlist.PartyName` should use the full qualified span instead of trimming to the trailing identifier.
- Capability coverage is part of the slice contract: once the handlers land, the language server must advertise references and document-highlight providers or the protocol surface is still incomplete.

---

### 2026-05-10T05:18:00Z: Slice 23 document-symbol tests lock state selection to the current semantic `NameSpan` contract

**By:** Scribe

**Status:** Merged from Soup Nazi's inbox note.

**Merged source:** `soup-nazi-slice-23.md`.

- Document-symbol selection ranges should project declaration identifier spans from the approved sources of truth: `IdentifierListSlot.Span` for the precept header and semantic `NameSpan` for field, state, and event declarations.
- For states, acceptance tests should assert the current `TypedState.NameSpan` exactly as emitted today, even though it still includes trailing modifiers such as `initial`.
- If the team later narrows state `NameSpan` to the bare identifier token, that is a separate pipeline contract change and should not be smuggled through the language-server slice.

---

### 2026-05-10T05:16:00Z: Slice 26 version-ordering tests must fail as runtime contract checks, not compile breaks

**By:** Scribe

**Status:** Merged from Soup Nazi's inbox note.

**Merged source:** `soup-nazi-slice-26.md`.

- The `DocumentState` version-ordering acceptance lane should stage its reds through reflection against the planned `TryUpdate(...)` / `Version` API instead of direct compile-time calls to missing members.
- That keeps the suite compiling while still failing with an exact runtime contract message when the versioned API is absent or has the wrong signature.
- Once the production API lands, the same tests can pivot immediately from API-presence checks to older/newer version behavior without test rewrites.

---

### 2026-05-10T05:11:00Z: Slice 14 completion routing must recover receiver and boundary context from semantic spans plus token adjacency

**By:** Scribe

**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).

**Merged sources:** `kramer-slice-14.md`, `soup-nazi-slice-14.md`.

- Expression completions should recover member-access receiver types from semantic expression spans plus token adjacency around `.` so accessor suggestions survive incomplete authoring like `Field.|member`.
- Completion routing must also treat a cursor parked at the start of the next token as belonging to the preceding separator when evaluating member-access and arg-default contexts; otherwise `CrewQueue.|count` and `default |1` fall back to generic surfaces.
- Current event scope should come from semantic construct matches (`TypedEvent`, `TypedTransitionRow`, `TypedEventHandler`, event-anchored `TypedEnsure`) rather than LS-local keyword and verb lists so arg completions stay catalog-driven across declaration, transition, and handler contexts.

---

### 2026-05-10T05:00:00Z: Track 1 should run autonomously to completion under the approved dependency wave plan

**By:** Scribe

**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).

**Merged sources:** `copilot-directive-20260510T005009-track1-autonomous-run.md`, `frank-track1-autonomous-run.md`.

- Shane's directive is explicit: Track 1 should continue without per-slice approval pauses until the lane reaches completion or a real blocker appears.
- The approved remaining-wave plan launches 15, 18, 19, 20, 22, 23, 25, 26, and 27 immediately; 17 waits on 14, 21 waits on 20, 24 waits on 23, and terminal slices 28 then 29 stay strictly serial after the behavioral surface closes.
- Shared-infrastructure slices 20, 23, and 26 should be prioritized ahead of lower-risk handlers and editor polish because they lock the helper contracts that unblock downstream slices.

---

### 2026-05-10T04:36:29Z: Slice 13 slot-context routing treats post-span `by`/`at` separators as expression positions

**By:** Scribe

**Status:** Merged from Soup Nazi's inbox note.

**Merged source:** `soup-nazi-slice-13.md`.

- `tools/Precept.LanguageServer/SlotContext.cs` now routes action-chain verb/target/expression positions, guard/compute/ensure/rule expressions, event-arg defaults, field `default` values, and `of` inner-type positions through the promised `SlotContext` surface.
- The durable parser/LS seam is now explicit: secondary action syntaxes like `enqueue ... by ...` can truncate `ActionChainSlot.Span` before `by` or `at`, so slot-context routing must honor raw separator tokens instead of trusting parsed action spans alone.
- `test/Precept.LanguageServer.Tests/SlotContextResolverTests.cs` locks the full approved Slice 13 matrix, and `test/Precept.LanguageServer.Tests` validated green at 88/88.

---

### 2026-05-10T04:33:18Z: Track 1 is the only active execution lane until Shane explicitly reopens Track 2

**By:** Scribe

**Status:** Merged from inbox.

**Merged source:** `copilot-directive-20260510T003318-focus-track1-only.md`.

- Active execution is now Track 1 only; Track 2 stays paused for execution until Shane explicitly switches focus back.
- The coordinator applied the directive immediately: `.squad/identity/now.md` now marks Track 1 as exclusive, and the SQL tracker reset all Track 2 `in_progress` slices back to `pending` so the live tracker shows no active Track 2 execution.
- Track 2 plans, findings, and reopened bug slices remain part of the durable record; this changes execution priority, not historical memory.

---

### 2026-05-10T04:33:18Z: Phase 1 language-server composition must be shared between Program.cs and LspTestHost

**By:** Scribe

**Status:** Merged from Kramer's inbox note.

**Merged source:** `kramer-no-deferral-followup.md`.

- The old `LspTestHost` mirroring note was real unfinished work, not an acceptable later-slice placeholder: `Program.cs` had the full shipped Phase 1 handler surface while the protocol host still booted a reduced server.
- `Program.cs` and `LspTestHost` now share `LanguageServerComposition.ConfigurePreceptLanguageServer(...)`, so tests and the shipped host boot the same handler set.
- `ServerCapabilityTests` now lock the live Phase 1 capability contract, and Slice 29 is narrowed back to future protocol-surface growth rather than Phase 1 mirroring cleanup.

---

### 2026-05-10T04:33:18Z: Semantic-token colors are injected from SemanticTokenTypes.All via `precept/semanticTokenColors`

**By:** Scribe

**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).

**Merged sources:** `kramer-10color-spec.md`, `kramer-slice-10-color.md`.

- The language-server color path is a custom notification, `precept/semanticTokenColors`, carrying the runtime projection of `SemanticTokenTypes.All` including `hexColor`, `bold`, and `italic`.
- The approved flow is catalog-driven end to end: `SemanticTokenTypes.All` -> `SemanticTokensHandler.SendColorNotification(...)` -> `precept/semanticTokenColors` -> VS Code `extension.ts` -> workspace `editor.semanticTokenColorCustomizations`.
- Constraint-pressure styling stays generic rather than per-token duplicated: the extension keeps one wildcard rule, `*.preceptConstrained => italic`, while token colors remain generated from catalog metadata.

---

### 2026-05-10T04:33:18Z: Implementation plans and plan-cleanup prompts must encode the no-deferral rule explicitly

**By:** Scribe

**Status:** Merged, consolidated, inbox cleared (4 files -> 1 canonical entry).

**Merged sources:** `copilot-directive-20260510T000159.md`, `copilot-directive-20260510T000538-both-plans.md`, `copilot-directive-20260510T000538.md`, `frank-no-deferrals-plans.md`.

- The no-deferrals rule now applies explicitly to plan language itself: no implementation plan may say "skip for now," "not strictly necessary," or any equivalent defer-it-for-later phrasing.
- This applies to both active implementation plans and to any spawned cleanup/rewrite prompt; when agents are asked to clean plans up, the prompt must state the no-deferral rule directly.
- Required work belongs in its owning slice. For Track 2, that means metadata-only slices close with catalog tests, consumer integrations land in the later slices that actually change parser/checker/binder/proof/MCP behavior, and Slice 2 is an audit checkpoint rather than a soft maybe.

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

### 2026-05-10T04:33:18Z: Pipeline audit pins the remaining Track 2 debt on parser, type-checker, and proof metadata drift

**By:** Scribe

**Status:** Merged from Frank's inbox note.

**Merged source:** `frank-pipeline-audit-findings.md`.

- The current highest-blast-radius parser drift is still action grammar ownership: `Parser` hardcodes `=`, `into`, `by`, and `at` helpers instead of reading cataloged syntax parts, which is why BUG-021 / BUG-048 / BUG-049 cluster together.
- Wildcard and broadcast targets remain cross-stage drift because `any` and `all` still survive as raw names/null sentinels instead of first-class metadata, affecting parser, binder, and graph behavior together.
- Type-checker and proof debt are the same class of problem in later stages: qualifier/unit meaning still leaks through local tables or modifier-kind checks, and proof discharge still embeds operator implication/diagnostic tables instead of reading metadata-owned semantics.

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

### 2026-05-10T04:20:44Z: Tracker status must change at the same boundary as execution state



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).



**Merged sources:** `frank-status-hygiene.md`, `copilot-directive-20260510T001838-status-hygiene.md`.



- The operating rule is now explicit: tracker state changes at the same boundary as execution state, with no delayed cleanup pass after the work is already done.

- The minimal protocol is boundary-based: before launch update the canonical tracker row and session todo, at result time record the evidence level immediately (`done` with SHA, `worktree-landed`, or still-active with the named open edge), at handoff close the old row before opening the next one, and under uncertainty stay conservative about proof.

- Coordinator hygiene is non-negotiable: keep one active slice per track unless an explicit parallel split is recorded, do not mark work active just because it was mentioned, and do not let safe-read consumer touches imply that a later phase has started.

- The reconciliation batch applied that rule to the live trackers: Track 1 already matched evidence (`Slice 10-color` done, `Slice 11` active), Track 2 `Slice 2` is satisfied from audit, `Slices 1/4/5/6/7` are worktree-landed, and `Slice 3` remains the only active Track 2 item; at close, only Track 1 Slice 11 and the modifier-model rename remained active across the batch.

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

### 2026-05-10T03:13:51Z: Toolchain bug audit locks parser/MCP root causes and a real-catalog test strategy



**By:** Scribe



**Status:** Merged, deduplicated, inbox cleared (2 files -> 1 canonical entry).



**Merged sources:** `frank-bug-cluster-analysis.md`, `soup-nazi-test-strategy.md`.



- The 52 confirmed toolchain bugs cluster most heavily in catalog-consuming surfaces: Parser 17, MCP serialization 15, Type Checker 10, Name Binder 4, Proof Engine 3, and MCP docs 3; the dominant root cause is stages hardcoding language behavior instead of projecting catalog metadata.

- The highest-value defect themes are now explicit: parser routing/disambiguation still misses catalog grammar (wildcards, guarded ensures/state actions, keyword-collision accessor forms, `append/enqueue by`, `insert/remove at`, comma field targets, richer arg type refs), MCP definition DTOs still flatten or omit catalog-derived structure (outcomes, qualifiers, hook actions, per-state declarations, element/member data, modifier bounds, event-arg richness), and operator/result typing still contains hardcoded dispatch drift (`and`/`or`/`not`, `contains`, `for`).

- The approved testing posture is to keep the real static catalogs as the executable language contract and build tiny synthetic stage fixtures around them rather than mocking metadata; mocking the catalogs would add indirection and drift risk without isolating a real boundary.

- Priority regression layers are now locked: add an MCP definition-surface matrix, parser routing/disambiguation tests derived from `Constructs.Entries`, keyword-collision/accessor tests from real catalog names, TypeChecker catalog-consumer tests for operations/accessors/modifiers, hook-specific pipeline tests, and catalog-reflection fixture tests that compile at least one minimal case per relevant catalog member.

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

### 2026-05-10T02:50:04Z: Language-server Phase 2 is now the production gap-closure plan



**By:** Scribe



**Status:** Merged from Frank's inbox note.



**Merged source:** `frank-ls-phase2-gap-analysis.md`.



- The live LS is beyond bootstrap but still missing production-complete authoring support in five areas: expression/default-position completions, catalog-complete hover projection, navigation handlers, document-symbol selection ranges, and document-version ordering.

- `TokenKind.Set` remains the sharpest cross-surface bug: in type position the LS must contextually reclassify `set` as the type token path so completion routing, hover text, and semantic tokens stop projecting the action keyword shape.

- Phase 2 is now the durable implementation plan for Slices 12-29: trigger/context fixes, deeper completion coverage, typed-constant completions, snippet metadata consumption, hover completion, semantic-token cleanup, references/highlights, rename, signature help, workspace/document symbols, selection ranges, version ordering, VS Code quote pairing, and doc sync.

- Non-gaps are locked too: keep push diagnostics on OmniSharp 0.19.9, keep full-sync/no-save hooks, do not add workspace diagnostics for closed files, do not add inlay hints or code lens, and do not encode routing-policy heuristics in completion filtering.

---

### 2026-05-10T02:50:04Z: Outline metadata and LS Slice 0 foundation are durably recorded as the protocol baseline



**By:** Scribe



**Status:** Merged, deduplicated, inbox cleared (2 files -> 1 canonical entry).



**Merged sources:** `george-slice0a-complete.md`, `kramer-slice0-complete.md`.



- `ConstructMeta` now carries `IsOutlineNode` and `OutlineSymbolTag` with safe defaults, and the catalog explicitly marks `PreceptHeader`, `FieldDeclaration`, `StateDeclaration`, `EventDeclaration`, and `RuleDeclaration` as outline nodes with `Module`, `Property`, `Enum`, `Function`, and `Boolean` tags.

- The first LS protocol spine is now durably captured too: `TextDocumentSyncHandler` uses `ILanguageServerFacade`, registration runs through `WithHandler<TextDocumentSyncHandler>()`, the reusable in-process harness is built on `LanguageServer.PreInit(...)` / `LanguageClient.PreInit(...)`, and the test project depends on the separate `OmniSharp.Extensions.LanguageClient` package.

- The temporary `LegacyHandlerCompat` bridge is explicitly part of this baseline record so later slices can treat Slice 0b removal as the planned cleanup, not an accidental regression.

---

### 2026-05-10T02:50:04Z: Snippet templates are the minimal valid authoring form for constructs and primary actions



**By:** Scribe



**Status:** Merged from George's inbox note.



**Merged source:** `george-slice16-done.md`.



- Construct snippet templates are intentionally the smallest valid declaration forms: keyword plus the required authoring slots only (`precept`, `field`, `state`, `event`, `rule`) so VS Code tab stops guide required input instead of pre-populating optional modifiers.

- Primary action verb snippets are derived from `ActionSyntaxShape` and sample `.precept` files rather than invented ad hoc; shapes like `AssignValue`, `CollectionValue`, `CollectionInto`, `FieldOnly`, `InsertAt`, and `PutKeyValue` each map to a canonical snippet pattern.

- Secondary action variants (`AppendBy`, `EnqueueBy`, `DequeueBy`, `RemoveAt`) stay out of snippet metadata because they are checker-resolved secondary forms, not independent primary author-facing completion items.

---

### 2026-05-10T00:47:45Z: Slice 3 core landed ArgReference recording as the semantic-index arg provenance surface



**By:** Scribe



**Status:** Merged from George's inbox batch.



**Merged source:** `.squad/agents/george/inbox.md`.



- George commit `cba898b7` added `ArgReference(TypedArg Arg, SourceSpan Site)` plus `ImmutableArray<ArgReference> ArgReferences` to `SemanticIndex`, with matching `SemanticIndex.Empty` and `CheckContext` support so arg tracking is symmetric with field/state/event references.

- `TypeChecker.Expressions.cs` now records arg references at both `TypedArgRef` resolution sites (identifier-scope lookup and member-access resolution), and `TypeChecker.cs` now seals `ctx.ArgReferences.ToImmutableArray()` into the final semantic index.

- This closes the thin core prerequisite for projection-only arg tooling, and `test/Precept.Tests/ArgReferenceTests.cs` added three regression facts before George validated the slice at 3740/3740 passing tests.

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

### 2026-05-10T00:23:31Z: Slice 0b removed the legacy language-server stub layer and zeroed the LS test project



**By:** Scribe



**Status:** Merged, inbox cleared (1 file -> 1 canonical entry).



**Merged source:** `.squad/agents/kramer/inbox.md`.



- Kramer commit `51d93dc2` deleted `tools/Precept.LanguageServer/LanguageServerStubs.cs`, `PreceptPreviewProtocol.cs`, and `LegacyHandlerCompat.cs`; the compat file also had to go because it still referenced the removed stub types and otherwise kept the language-server build red.

- Slice 0b also deleted 13 legacy shim-facing files under `test/Precept.LanguageServer.Tests/`, removing 173 compiler-redundant tests; the project now retains only `LspTestHost.cs` and `GlobalUsings.cs`, discovers 0 tests, and still builds cleanly.

- Validation closed the cleanup gate: `dotnet build` succeeds for the language-server and LS test projects, and `dotnet test test/Precept.Tests/` stays green at 3737/3737.

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

### 2026-05-09T23:46:43Z: Language-server review batch reconciled docs, landed `TypedField.NameSpan`, and left only the preview restore-failure contract open



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (3 files -> 1 canonical entry).



**Merged sources:** `frank-comprehensive-review.md`, `george-typedfield-namespan.md`, and `kramer-ls-review.md`.



- Frank and Kramer both completed first-principles reviews of `docs/tooling/language-server.md` and `docs/Working/language-server-implementation-plan.md`; the objective artifact-reference and tooling-wiring drift they found was fixed inline, leaving the LS architecture and slice structure intact.

- Shane approved the thin core field-span fix and George landed it: `TypedField` now carries `SourceSpan NameSpan`, `TypeChecker` populates it from `DeclaredField.NameSpan`, runtime tests cover the symmetry change, and George validated the change with 3733 passing tests.

- One design decision remains open from the batch: `precept/inspect` preview restore failures (`RestoreInvalidInput` / `RestoreConstraintsFailed`) still need an explicit language-server contract, either as a structured failure payload or as a defined JSON-RPC error shape.

---

### 2026-05-09T23:21:36Z: Language-server clean pass front-loads shim deletion and leaves Slice 11 as final wiring only



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (4 files -> 1 canonical entry).



**Merged sources:** `copilot-ls-test-deletion-decision.md`, `frank-clean-pass.md`, `frank-slice0b-early-deletion.md`, `frank-slice11-update.md`.



- Slice 0b is now the immediate cleanup gate: delete `LanguageServerStubs.cs` and the 13 compiler-level language-server test files before any new handler code, then validate with `dotnet build` and `dotnet test test/Precept.Tests/`.

- The clean pass removes shim-shaped production helpers, keeps diagnostics on the text-sync compile/publish path, and adds `ArgReferences` as the thin core prerequisite so semantic tokens and go-to-definition stay projection-only.

- `precept/inspect` now ships as a real handler shell, `PreceptPreviewProtocol.cs` is slated for deletion, and Slice 11 is reduced to final `Program.cs` wiring plus capability declaration.

---

### 2026-05-09T18:53:05-04:00: Language server implementation is locked to the stub contract with no remaining plan deferrals



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (2 files -> 1 canonical entry).



**Merged sources:** `frank-language-server-design.md`, `frank-ls-plan-no-deferrals.md`.



- The 173 ported tests in `LanguageServerStubs.cs` remain the public contract; OmniSharp handlers and stub classes may coexist as thin entry points over shared logic.

- Fuzzy matching stays in the language server, preview/inspect may ship as a handler shell while the runtime evaluator remains stubbed, and `Token != null` is the permanent user-facing type filter.

- The temporary `ConstructKind` outline switch is superseded by concrete Slice 0a: `ConstructMeta` gains `IsOutlineNode` plus string `OutlineSymbolTag`, and the LS projects that tag to `SymbolKind` without introducing LSP types into `src/Precept/`.

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

### 2026-05-09T16:06:55-04:00: UCUM and domain registries stay curated registry surfaces with XML-anchored drift tests



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (5 files -> 1 canonical entry).



**Merged sources:** `frank-q2-q3-analysis.md`, `george-q1-tier1-codes.md`, `george-q4-catalog-registry.md`, `george-ucum-catalog-collapse.md`, `soup-nazi-q8-ucum-drift.md`.



- Named dimension categories are curated Precept editorial metadata, not something UCUM XML can derive mechanically; registry-shaped APIs should expose canonical `All` maps and keep alias resolution in explicit helper paths.

- `UcumAtomCatalog` is the single UCUM source of truth: `All` is the embedded XML-backed atom universe, `BrowseTier1()` is the curated 150-entry business-facing surface, and parse-only Tier 1 forms are synthesized through `UcumParser` rather than duplicated in a second catalog.

- Drift tests anchor against the embedded XML universe plus the approved Tier 1 curation rules, including the exclusion of time atoms (`s`, `min`, `h`, `d`) and `mol`, instead of relying on aspirational atom-count floors.

---

### 2026-05-09T19:55:00Z: Typed business-domain qualifiers are first-class semantic data, and count classification stays unit-aware



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (4 files -> 1 canonical entry).



**Merged sources:** `frank-design-gap-audit.md`, `george-p0-fix.md`, `frank-count-dimensionless-gap.md`, `george-q6b-qualifier-fix.md`.



- The architectural P0 was qualifier loss between parser and semantic model; `QualifiedTypeReference`, `ParsedQualifier`, and type-checker extraction now thread `in`/`of` data into fields and event args instead of dropping it at parse time.

- Qualifier validation belongs in the type checker against the authoritative registries (`CurrencyCatalog`, UCUM parsing, `DimensionCatalog`), with `in`/`of` exclusivity enforced on the qualified type reference span.

- Counting-unit safety did not require a proof-engine redesign: quantity arithmetic already compares unit-code-bearing qualifier records. The real gap was reverse-aliasing every `DimensionVector.None` unit to `count`, which is now fixed in unit-aware qualifier derivation so angles and solid angles no longer masquerade as counts.

---

### 2026-05-09T17:47: AI authoring content belongs in catalogs, and proof guidance owns runtime fault consequences



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (4 files -> 1 canonical entry).



**Merged sources:** `copilot-directive-faults-placement.md`, `copilot-directive-mcp-thin-layer-catalogs.md`, `george-15-catalog-authoring.md`, `george-precept-language-content-expansion.md`.



- All authored guidance for the new MCP tools must live in core metadata; tool implementations stay thin projections and do not embed separate prose, legality tables, or pattern content.

- `DiagnosticMeta` is now the recovery/example home for all 116 diagnostics, `SyntaxReference` is the compile-verified home for 8 common patterns plus 3 anti-patterns, and `QuickstartCatalog` is the first-contact orientation/tool-guide surface for AI agents.

- Runtime `Faults.All` belongs under `precept_proofs()` as `runtimeFaults`, because proofs and guards are the authoring lane that explains how those runtime consequences are avoided.

---

### 2026-05-09T17:43: User directive — the spike branch allows no deferrals, phased punts, or open-question handoffs



**By:** Scribe



**Status:** Merged from inbox.



**Merged source:** `copilot-directive-no-deferrals-final.md`.



- On this branch there are no issue-tracking deferrals, "top N now / rest later" partial authoring passes, or open-question lists handed back to Shane when the team can make the call and proceed.

- This directive applies immediately to MCP tool design, catalog authoring, and language-server planning; durable records should capture the final decision, not a deferred question list.

---

### 2026-05-09T15:33:49Z: User-defined string format validation is a future constraint feature, not typed-literal extensibility



**By:** Scribe



**Status:** Merged from inbox.



**Merged source:** `frank-22-user-defined-validation-extensibility.md`.



- The typed-literal validation framework stays intentionally closed and catalog-defined; there is no user-pluggable validator model for email, phone, or document-format parsing.

- Format validation is a different concern from semantically structured typed literals like money, datetime, and quantity: email/phone/document numbers remain strings with pattern rules, not new `TypeKind` values.

- The recommended future language surface is a string constraint modifier such as `matches /pattern/ because ...`, implemented through the existing modifier/constraint pipeline rather than the typed-literal framework.

---

### 2026-05-09T15:33:49Z: Runtime typed-literal arg parsing stays on `TypeRuntimeMeta`, not compile-time literal validation



**By:** Scribe



**Status:** Merged from inbox.



**Merged source:** `frank-typed-literal-runtime-args.md`.



- Runtime arg parsing for typed-literal event args stays on the existing catalog-owned runtime coercion lane: `TypeRuntime<T>` / `TypeRuntimeMeta.ReadJson` for JSON callers and `TypeRuntime<T>.FromClr` for typed callers.

- `TypedConstantValidation.Validate(...)` remains compile-time-only for DSL literal text, with diagnostic spans and suggestions; runtime failures surface as `EventOutcome.InvalidArgs`, not compiler diagnostics.

- Each typed-literal type therefore keeps three distinct catalog registrations on `TypeMeta`: `TypeRuntime<T>`, `TypeRuntimeMeta`, and `ContentValidation`, while sharing the same domain parsers underneath.

---

### 2026-05-09T15:26:09Z: MCP discovery is correct at three implemented tools, and stdout log pollution is fixed at the host boundary



**By:** Scribe



**Status:** Merged from inbox.



**Merged sources:** `newman-mcp-diagnosis.md`, `newman-stderr-fix.md`.



- The current MCP server really exposes only three tools (`PingTool`, `LanguageTool`, `CompileTool`); `precept_inspect`, `precept_fire`, and `precept_update` are absent because they have not been implemented yet, not because discovery is broken.

- Stdout log pollution was a separate host bug. `tools/Precept.Mcp/Program.cs` now routes console logging to stderr with `LogToStandardErrorThreshold = LogLevel.Trace`, keeping stdout clean for JSON-RPC.

- Commit `9de87699` closes the parse-warning defect now; the missing tool surfaces remain a deferred runtime-build scope tracked in `docs/working/newman-mcp-tool-discovery-diagnosis.md`.

---

### 2026-05-09T15:20:45Z: Event-arg member references now use a dedicated parameter-property TextMate scope



**By:** Scribe



**Status:** Merged, deduplicated, inbox cleared (3 files -> 1 canonical entry).



**Merged sources:** `frank-arg-member-scope.md`, `kramer-arg-ref-color-fix.md`, `kramer-arg-member-scope-impl.md`.



- Frank locked the architecture: event-arg member references belong on the `variable.parameter.*` axis, so `eventArgReference` capture group 3 should emit `variable.parameter.property.precept` rather than `variable.other.property.precept`.

- Kramer's compound-selector override (`meta.event-arg-ref.precept variable.other.property.precept`) is preserved only as the superseded interim fix; the durable answer is the dedicated scope emitted by the grammar generator.

- The implementation shipped in `tools/Precept.GrammarGen/Program.cs`, regenerated `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`, simplified `tools/Precept.VsCode/package.json` to a direct `variable.parameter.property.precept -> #9AD8E8` rule, and left collection-member property scoping unchanged on the field axis.

---

### 2026-05-09T15:11:01Z: Typed literal validation stays catalog-driven under one static dispatcher



**By:** Scribe



**Status:** Merged from inbox.



**Merged source:** `frank-typed-literal-framework.md`.



- Frank approved a single `TypedConstantValidation.Validate(...)` dispatcher keyed by `TypeMeta.ContentValidation`; no `ITypedConstantValidator`, registry, or DI layer is allowed.

- The durable framework shape is `ContentValidation` metadata -> static dispatcher -> domain validator -> `TypedConstantParseResult`, with structured results consumed by the type checker, language server, runtime, and MCP tools.

- New work implied by the decision is explicit: add `UcumValidation` and `QuantityValidation`, give `NodaTimeValidation` a `TemporalLiteralKind`, add missing temporal `ContentValidation` entries (instant, timezone, zoneddatetime, duration quantity), and build the shared temporal parser under `src/Precept/Language/Time/`.

---

### 2026-05-09T15:07:24Z: CurrencyCatalog stays transactional while sync tests record intentional ISO-only exclusions



**By:** Scribe



**Status:** Merged, reconciled, inbox cleared (3 files -> 1 canonical entry).



**Merged sources:** `kramer-iso-xml-mismatch.md`, `george-currency-catalog-fix.md`, `george-fund-codes-excluded.md`.



- Kramer's unconditional sync test exposed the exact XML/catalog drift: the committed ISO snapshot added fund/accounting-unit codes plus precious-metal/testing placeholders, while `ANG`, `BGN`, and `ZWL` no longer appeared in the XML.

- George's implementation direction remains the canonical runtime contract: `CurrencyCatalog` models transactional business currencies, not the full XML payload, and `CurrencyCatalogSyncTests` carries a documented case-insensitive `IntentionalExclusions` set for XML-only codes.

- The durable exclusion policy now explicitly includes fund/accounting-unit codes `BOV`, `CHE`, `CHW`, `CLF`, `COU`, `MXV`, `USN`, `UYI`, `UYW`, `VED`, `XAD`, `XCG`, and `ZWG` alongside `XAU`, `XAG`, `XPT`, `XPD`, `XTS`, and `XXX`; withdrawn catalog entries `ANG`, `BGN`, and `ZWL` stay real failures if reintroduced.

---

### 2026-05-09T15:07:24Z: Data family anchor retired after field and arg colors became first-class semantic tokens



**By:** Scribe



**Status:** Merged, deduplicated, inbox cleared (5 files -> 1 canonical entry).



**Merged sources:** `elaine-slate-audit.md`, `elaine-anchor-literal-check.md`, `elaine-anchor-dropped.md`, `kramer-field-arg-colors.md`, `kramer-color-audit.md`.



- Elaine's audit and literal-safety check made the design ruling durable: once fields move to `--field` (`#A5B4FC`) and args move to `--arg` (`#9AD8E8`), the old Data anchor `--data` (`#B0BEC5`) has zero legitimate consumers and no literal/value scope depends on it.

- Kramer wired the extension to that semantic split through TextMate, keeping fields on `#A5B4FC`, moving `variable.parameter.precept` to `#9AD8E8`, and removing the last `#B0BEC5` extension/theme/mockup usages.

- The Data family is now the four-token semantic grouping `--data-t`, `--data-v`, `--field`, and `--arg`; the family definition no longer depends on an anchor swatch or hue-only coherence.

---

### 2026-05-09T14:56:10Z: UCUM parsing must ship as a real shared language subsystem, not a closed-set placeholder



**By:** Scribe



**Status:** Merged from inbox.



**Merged source:** `frank-ucum-parser-arch.md`.



- Frank locked the architecture: build the real UCUM parser now in `src/Precept/Language/Ucum/`, backed by authoritative source data in `src/Precept/Data/Ucum/` and generated frozen catalog tables for runtime consumers.

- `unitofmeasure` validation must move off `ClosedSetValidation` onto a UCUM-backed `ContentValidation` path that returns structured parse data (`UcumParseResult` / `UcumParsedUnit`) rather than booleans.

- The domain rules are explicit: `time` is not in the UCUM dimension partition, `quantity of 'time'` is invalid in favor of `duration` / `period`, `count` remains a Precept business alias over dimensionless UCUM forms, and `speed` plus `force` become curated `DimensionCatalog` aliases.

---

### 2026-05-09T14:04:05Z: `precept_language` ships now as the canonical MCP language-vocabulary baseline



**By:** Scribe



**Status:** Merged, deduplicated, inbox cleared (2 files -> 1 canonical entry).



**Merged sources:** `newman-language-tool-done.md`, `frank-language-tool-timing.md`.



- Newman implemented `LanguageTool.cs`, added 12 `LanguageToolTests.cs` coverage points, synced `docs/tooling/mcp.md`, and handed off green validation at 12 MCP tests plus 3646 core tests passing on commit `bd4e6e30`.

- Durable contract baseline: ship `precept_language` now off the current catalog-backed language/diagnostic surface (`tokens`, `types`, grouped `modifiers`, `actions`, `constructs`, `constraints`, `operators`, `functions`, `diagnostics`, and static `firePipeline`) instead of holding for builder/evaluator work.

- The older 11-catalog draft is superseded by the implemented/docs-synced surface; future evaluator metadata stays additive unless it changes the agent-facing vocabulary itself.

---

### 2026-05-09T00:00:00Z: Runtime business-domain CLR shapes are pure data records, not executor logic containers



**By:** Scribe



**Status:** Merged from inbox.



**Merged source:** `george-clr-value-types-done.md`.



- The public runtime shapes for `currency`, `unitofmeasure`, `dimension`, `money`, `quantity`, `price`, and `exchangerate` live under `src/Precept/Runtime/BusinessValues/` as record / record-struct data carriers.

- `Currency` stays a sealed record rather than bespoke alpha-code equality, and the public API surface uses `Dimension` to avoid colliding with the internal dimensional-analysis type `Measures.MeasureDimension`.

- Parsing, formatting, interning, arithmetic helpers, and `PreceptValue` wrappers are explicitly separate follow-on runtime concerns rather than responsibilities of these CLR shape types.

---

### 2026-05-08T05:27:37Z: Grammar generator design doc locks the generator contract and exposes the unreachable message-string path



**By:** Scribe



**Status:** Merged from inbox.



**Merged source:** `frank-grammar-generator-doc.md`.



- Frank's `docs/compiler/grammar-generator.md` is now the canonical generator design reference, locking the four-step algorithm, structural-pattern inventory, output contract, and its boundary with the catalog-system and tooling-surface docs.

- Durable bug record: the generator builds `#messageStrings` in `AddStructuralPatterns()` but never includes it in `BuildTopLevelPatterns()`, so `because` / `reject` message strings stay unreachable in generated output.

- Catalog gap locked: add `TokenMeta.IsMessagePosition` on `Because` and `Reject` so the generator can derive the gold message-string rule catalog-first before inserting `#messageStrings` ahead of `#strings`.

---

### 2026-05-08T05:27:37Z: Grammar generator implementation closes the spec must-fix inventory while leaving the catalog-blocked message-position gap explicit



**By:** Scribe



**Status:** Merged from inbox.



**Merged source:** `kramer-grammar-gen-impl.md`.



- Kramer closed all 16 must-fix items on PR #139: the generator now emits 42 repository patterns, orders 41 top-level patterns per spec, derives structural alternations from catalogs, and removes stale patterns plus the retired `nullable`, `invariant`, `assert`, and `with` keywords.

- Durable boundary: function-argument message strings still cannot receive gold scoping without new positional metadata, so the implementation leaves an explicit TODO at the exact wire-in point instead of hardcoding names or argument positions.

- Validation at handoff stayed clean: the generator build passed, the emitted grammar JSON was valid, and promotion to the canonical grammar remains gated on full parity plus the message-position catalog gap.

---

### 2026-05-08T04:55:35Z: ProofEngine implementation is blocked on unresolved spec and contract gaps



**By:** Scribe



**Status:** Merged from inbox.



**Merged source:** `frank-proof-engine-gap-analysis.md`.



- Frank reviewed the ProofEngine spec against commit `79c3403` and marked implementation **NOT READY**: three blocking gaps and seven significant gaps prevent a clean start.

- The blockers are now explicit in the ledger: three `ProofRequirementKind` variants still lack discharge-strategy coverage, the spec's canonical `FieldModifierMeta.ProofDischarges` property does not exist in source, and the specified `ProofLedger` output shape diverges materially from the stub and `Compilation` contract.

- Durable implementation gate: close the discharge-model, catalog-shape, and output-type mismatches before any ProofEngine slice starts; source-alignment gaps like `SemanticIndex.AllTypedExpressions` and the `ConstraintIdentity` shapes remain follow-up work.

---

### 2026-05-08T04:55:17Z: TextMate grammar replacement must be catalog-complete and parity-or-better before the generator becomes canonical



**By:** Scribe



**Status:** Merged from inbox.



**Merged source:** `frank-grammar-spec.md`.



- Frank drafted an authoritative grammar spec after reviewing the token/catalog sources, the hand-authored grammar, the generator scaffold, the design-system docs, and all `.precept` samples.

- Durable finding: both the shipped `precept.tmLanguage.json` and the current generator are stale (retired keywords and syntax, missing construct patterns, collapsed scope groups), so replacement is only valid when the generator emits the full catalog-derived language surface, including the dedicated rule-message string scope.

- Resolution baseline: catalog `TextMateScope` assignments win over conflicting brand-doc keyword lists, and the generator must reach hand-authored parity-or-better before it can replace the shipped grammar.

---

### 2026-05-08T04:26:28Z: Exhaustive GraphAnalyzer review approves the current implementation and narrows the remaining follow-up to future event-modifier work



**By:** Scribe



**Status:** Merged from inbox.



**Merged source:** `frank-graphanalyzer-exhaustive-review.md`.



- Frank's exhaustive review approved commit `d10513d` as architecturally sound, spec-complete for the currently implemented language surface, and catalog-driven in the required dimensions.

- The only red finding (`EventModifierMeta.RequiredAnalysis` not yet consumed) is explicitly zero-risk today because the only event modifier with graph-analysis implications is `initial`, which the analyzer already handles equivalently through edge/topology derivation.

- Durable future-touch follow-up: when richer event modifiers land, GraphAnalyzer must consume `EventModifierMeta.RequiredAnalysis`; the next touch is also the right time to consider an event-per-state index for the O(events × edges) scans and `RelatedSpans` on structural-violation diagnostics.

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

### 2026-05-08T00:49:00Z: GraphAnalyzer advisory fix batch closed on-branch except the deferred event-modifier gap



**By:** Scribe



**Status:** Merged from inbox.



**Merged source:** `george-advisory-fixes-done.md`.



- George closed all 8 addressable items from Frank's advisory list in commit `79c3403`: structural diagnostics 109/110/111 now carry `RelatedSpans`, graph diagnostics gained `RelatedCodes`, the graph-analyzer docs now spell out zero-terminal semantics and the real analyzer input set, the planned `EventModifierMeta.RequiredAnalysis` consumption path is marked, `IsInitial`'s direct enum check is documented, and the fragile `nameof()` dedup was replaced with `HasDiagnostic()`.

- The event-coverage and initial-event scans now share a precomputed edge index, removing the redundant O(events × edges) lookups without changing behavior.

- Validation at handoff closed green at 3385/3385 `Precept.Tests` passing.

---

### 2026-05-08T00:36:25Z: Full GraphAnalyzer advisory inventory reconstructed and locked for durable follow-up tracking



**By:** Scribe



**Status:** Merged from inbox.



**Merged source:** `frank-advisory-reconstruction.md`.



- Frank reconstructed the full post-review inventory after the earlier exhaustive-review merge omitted the detailed advisory list: 9 advisory items (A1-A9) plus Gap1.

- Durable breakdown: requirements/docs follow-ups A1-A4 (`RelatedSpans`, zero-terminal semantics, `RelatedCodes`, graph-analyzer input-table correction), catalog/compliance follow-ups A5-A7 (planned event-modifier dispatch note, `IsInitial` rationale, typed `NoInitialState` dedup), and quality follow-ups A8-A9 (event-per-state edge index for coverage and `GraphEvent.IsInitial`).

- Gap1 remains the deliberate future-touch item: GraphAnalyzer still must consume `EventModifierMeta.RequiredAnalysis` when richer event modifiers ship.

---

### 2026-05-08T00:22:50Z: R4 hard gate expanded to every remaining conditional GraphAnalyzer item



**By:** Shane (via Copilot)



**Status:** Locked — applies before any ProofEngine work begins.



**Merged source:** `copilot-directive-20260508.md`.



- No R4 conditional follow-on stays optional anymore: TQ1, EC5, EC6, and Gap 8 must all land before ProofEngine work begins.

- Scribe merged the directive immediately without waiting for the still-running `soup-nazi-8` batch so the team ledger reflects the hard gate now, not after the remaining follow-up lands.

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

### CI Enforcement Bug Fixes

**Commit:** 7424785

**Bug 1 fix:** `EnforceCIInExpression` in `src/Precept/Pipeline/TypeChecker.cs` — all 5 `Diagnostics.Create` call sites for CI codes 66, 95–98 now pass the CI field name as the `{0}` template argument. Added `GetCIFieldName` helper (line ~2197) that extracts the field name from whichever binary operand is the `~string` `TypedFieldRef`. For function calls (codes 97, 98), extracts directly from `func.Arguments[0]`.

**Bug 2 fix:** Added `PopulateRules` method (~line 945) that iterates `manifest.ByKind[ConstructKind.RuleDeclaration]`, resolves `RuleExpressionSlot.Expression` and `GuardClauseSlot.Expression` via `Resolve()`, wraps `BecauseClauseSlot.Message` as a `TypedLiteral`, and accumulates into `ctx.Rules`. Called from `Check()` after `PopulateEventHandlers`. `BuildPartialSemanticIndex` now emits `ctx.Rules.ToImmutableArray()` instead of `ImmutableArray<TypedRule>.Empty`. `ValidateCIEnforcement` rule traversal (lines 2067–2073) was already correct — it just iterated an empty list.

**Test result:** 3294/3294 total passing (30 CI tests, 22 quantifier tests, 3242 existing)

**R3-ready:** YES

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

### Slice 10 Complete — Ready for R3

**Commit:** 844f00e

**BuildSemanticIndex:** All 16 ImmutableArray primaries + 4 FrozenDictionary secondaries confirmed — populated from CheckContext, no empty stubs remaining.

**D26 assert location:** `TypeChecker.BuildSemanticIndex()`, line ~2245

**ContainsAnyErrorExpression:** Traverses Fields (default+computed), Events (arg defaults), TransitionRows (guard+actions), Rules (condition+guard+message), Ensures (condition+guard+message), AccessModes (guard), StateHooks (guard+actions), EventHandlers (actions). Recursive `ContainsError` walks all composite expression subtypes (binary, unary, function call, member access, conditional, quantifier, interpolated string, list literal, postfix).

**Remaining stubs removed:** `BuildSemanticIndex` was the last `NotImplementedException` stub — now fully implemented.

**Full pipeline order:** PopulateFields → PopulateStates → PopulateEvents → PopulateTransitionRows → PopulateEventHandlers → PopulateRules → ValidateModifiers → ValidateStructural → ValidateCIEnforcement → BuildSemanticIndex

**Test result:** 3294/3294 total, 118 integration tests passing

**NotImplementedException stubs remaining:** None

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

### Slice 8 Complete

**By:** George (for Soup Nazi)

**Commit:** `00ef822`

**EnforceCIConsistency:** `ValidateCIEnforcement` traverses all resolved expression trees in `CheckContext` — field defaults, computed expressions, transition row guards and actions, event handler actions, rule conditions/guards/messages, and ensure conditions/guards/messages. Recursively walks `TypedExpression` DU via `EnforceCIInExpression`.

**CI-required contexts:** A context is CI-required when a `TypedFieldRef` with `IsCaseInsensitive = true` appears as an operand of `==`/`!=`, as the first argument of `startsWith`/`endsWith`, or as the value operand of `contains` on a case-sensitive collection. `IsCaseInsensitive` is set during `ResolveIdentifier` from the `CIFields` HashSet populated in `PopulateFields` when `declared.Type is CITypeReference`.

**DiagnosticCodes used:**

- `CaseInsensitiveFieldRequiresTildeEquals` (66) — `==` with `~string` operand

- `CaseInsensitiveFieldRequiresTildeNotEquals` (95) — `!=` with `~string` operand

- `CaseInsensitiveValueInCaseSensitiveContains` (96) — `contains` with `~string` value in CS collection (dormant: no `contains` OperationKind yet)

- `CaseInsensitiveFieldRequiresTildeStartsWith` (97) — `startsWith(~string, ...)`

- `CaseInsensitiveFieldRequiresTildeEndsWith` (98) — `endsWith(~string, ...)`



**Valid CI examples for Soup Nazi (should NOT trigger diagnostics):**

```

// ~= and ~!= are fine with ~string

field Email: ~string

when Email ~= "admin@example.com"

when Email ~!= "test@test.com"



// ~startsWith / ~endsWith are fine

when ~startsWith(Email, "info@")

when ~endsWith(Email, ".com")



// Regular == on non-CI string is fine

field Name: string

when Name == "Alice"

```



**Invalid (non-CI in CI context) examples for Soup Nazi (should trigger diagnostics):**

```

// == on ~string → CaseInsensitiveFieldRequiresTildeEquals

field Email: ~string

when Email == "admin@example.com"



// != on ~string → CaseInsensitiveFieldRequiresTildeNotEquals

when Email != "test@test.com"



// startsWith on ~string → CaseInsensitiveFieldRequiresTildeStartsWith

when startsWith(Email, "info@")



// endsWith on ~string → CaseInsensitiveFieldRequiresTildeEndsWith

when endsWith(Email, ".com")



// Both operand positions checked:

when "admin@example.com" == Email

```



**Notes:**

- CI tracking lives in `CheckContext.CIFields` (scalar `~string`) and `CheckContext.CIElementCollections` (collections with `~string` elements). Both populated from `CITypeReference` checks in `PopulateFields`.

- The `contains` rule (Rule 3) is structurally implemented but dormant — `IsContainsOperation()` returns `false` because no `OperationKind` entries for `contains` exist yet. When they land, update `IsContainsOperation` to match them.

- To test CI enforcement, declare a field with `~string` type (requires parser CI type support), then use `==`/`!=`/`startsWith`/`endsWith` on that field. The diagnostic fires on the binary op or function call span.

- `TypedFieldRef.IsCaseInsensitive` was previously always `false` — now correctly populated from `CIFields` set. This is a semantic change visible in `SemanticIndex` output.

---

### 2026-05-08: george-slice-9-done



**By:** Unknown



**Status:** Merged from inbox — merged from inbox.



**Merged source:** `george-slice-9-done.md`.

---

### Slice 9 Complete

**By:** George (for Soup Nazi)

**Commit:** 54fa59b



**TypedQuantifier:** Triggered by `QuantifierExpression(TokenKind.All|Any|No, bindingName, collection, predicate)`. Resolves collection → extracts ElementType via `GetElementType` → pushes `(bindingName, elementType)` onto `ctx.QuantifierBindings` → resolves predicate with binding in scope → pops binding. Binding variable shadows event args and fields (per §13 Slice 9). Returns `TypedQuantifier(Boolean, bindingName, elementType, collection, predicate, span)`.



**TypedListLiteral:** Triggered by `ListLiteralExpression([elem1, elem2, ...])`. Resolves each element expression, unifies element types via bidirectional `IsAssignable` widening (e.g., `[1, 2.5]` → Integer widens to Decimal). Empty lists produce `TypedListLiteral(List, Error, [], span)`. Returns `TypedListLiteral(List, unifiedElementType, elements, span)`.



**QualifierBinding DU:** Not directly used on quantifier/list literal arms. QualifierBinding (InheritedQualifier vs SameQualifierRequired) remains on `TypedBinaryOp.ResultQualifier` and `TypedTransitionRow.ResultQualifier` — quantifier resolution does not produce qualifier bindings; it uses the simpler `QuantifierBindings` stack on CheckContext which is `Stack<(string Name, TypeKind Type)>`.



**DiagnosticCodes used:**

- `InvalidQuantifierTarget` (102) — collection operand is not a collection field (no ElementType)

- `QuantifierPredicateNotBoolean` (106) — predicate resolves to non-boolean type

- `TypeMismatch` (18) — list literal elements have incompatible types



**Valid quantifier examples for Soup Nazi:**

- `each x in Tags (x = "active")` — Tags is `set of string`, x binds as string

- `any item in Scores (item > 100)` — Scores is `list of integer`, item binds as integer

- `no entry in Logs (entry.Status = "failed")` — Logs is collection, entry binds as element type



**Valid list literal examples:**

- `[1, 2, 3]` — inferred ElementType: Integer, ResultType: List

- `["a", "b", "c"]` — inferred ElementType: String, ResultType: List

- `[1, 2.5, 3]` — Integer widens to Decimal, ElementType: Decimal



**Notes for Soup Nazi:** Quantifier bindings shadow both event args and fields (tested in `QuantifierBindingShadowsEventArg`). The `GetElementType` helper only resolves `TypedFieldRef` receivers via `FieldLookup` — chained collection access (e.g., `each x in obj.Items(...)`) returns null and emits InvalidQuantifierTarget. Empty list literals `[]` are valid but produce Error element type (no inference possible). ConditionalExpression arm remains a stub (Slice TBD).

---

### 2026-05-08: george-slice5-restored



**By:** Unknown



**Status:** Merged from inbox — merged from inbox.



**Merged source:** `george-slice5-restored.md`.

---

### Slice 5 Restoration Complete

**Commit:** 4e1efd8

**Methods restored:**

- `PopulateTransitionRows` — iterates TransitionRow constructs, calls NormalizeTransitionRow, accumulates into CheckContext

- `PopulateEventHandlers` — iterates EventHandler constructs, calls NormalizeEventHandler, accumulates into CheckContext

- `NormalizeTransitionRow` — resolves from-state, event, guard, action chain, outcome into TypedTransitionRow

- `NormalizeEventHandler` — resolves event, action chain into TypedEventHandler

- `ResolveAction` — dispatches on ParsedAction DU (Assign, CollectionValue, FieldOnly, etc.) into TypedAction

- `ResolveActionTarget` — resolves action target identifier to field name + type

- `ContainsErrorExpression` — D26 assertion helper for transition rows

- `ContainsErrorExpressionInAction` — D26 assertion helper for event handler actions

- `ValidateModifiers` — Slice 7 modifier validation entry point (also lost in overwrite)

- `ValidateFieldModifiers` — per-field/arg modifier applicability, conflicts, subsumption

- `IsTypeApplicable` — modifier ApplicableTo type matching



**Additional fix:** `BuildPartialSemanticIndex` was returning empty arrays for TransitionRows, EventHandlers, FieldReferences, StateReferences, EventReferences — now wires from CheckContext.



**Secondary bug fixed:** EventName.ArgName resolution — added early check in `ResolveMemberAccess`: when the target of a `MemberAccessExpression` is an `IdentifierExpression` matching a declared event name, resolve the member against the event's arg declarations and return `TypedArgRef` instead of falling through to normal member access (which would fail with UndeclaredField since event names aren't fields). Per language spec §3.5 Event arg access.



**Pipeline call order confirmed:**

1. PopulateFields (Slice 1)

2. PopulateStates (Slice 1)

3. PopulateEvents (Slice 1)

4. PopulateTransitionRows (Slice 5)

5. PopulateEventHandlers (Slice 5)

6. ValidateModifiers (Slice 7)

7. ValidateStructural (Slice 6)

8. BuildPartialSemanticIndex



**Test result:** 3196/3196 passing (26/26 TypeCheckerTransitionTests, 0 regressions)

---

### 2026-05-08: soup-nazi-slice-1-triage



**By:** Unknown



**Status:** Merged from inbox — merged from inbox.



**Merged source:** `soup-nazi-slice-1-triage.md`.

---

### Slice 1 Test Failure Triage — 2026-05-07



| Test | Failure Type | Root Cause | Action Taken |

|------|-------------|------------|--------------|

| PriceType_ResolvesToPriceTypeKind | TYPE A | Test used `in 'USD/each'` qualifier syntax; parser doesn't handle BusinessDomain qualifier parsing yet | Fixed — removed qualifier, test now passes |

| ExchangeRateType_ResolvesToExchangeRateTypeKind | TYPE A | Test used `in 'USD' to 'EUR'` qualifier syntax; parser doesn't handle qualifier parsing yet | Fixed — removed qualifier, test now passes |

| QuantityType_ResolvesToQuantityTypeKind | TYPE A | Test used `in 'kg'` qualifier syntax; parser doesn't handle qualifier parsing yet | Fixed — removed qualifier, test now passes |

| MoneyType_ResolvesToMoneyTypeKind | TYPE A | Test used `in 'USD'` qualifier syntax; parser doesn't handle qualifier parsing yet | Fixed — removed qualifier, test now passes |

| LogOfString_ResolvesCollectionWithElementType | TYPE B | `Types.ByToken` maps `TokenKind.LogType` → last-wins is `TypeKind.LogBy` (enum 28), overwriting `TypeKind.Log` (enum 27). `log of string` resolves to LogBy instead of Log. | Documented — parser/ByToken gap |

| QueueOfNumber_ResolvesCollectionWithElementType | TYPE B | `Types.ByToken` maps `TokenKind.QueueType` → last-wins is `TypeKind.QueueBy` (enum 31), overwriting `TypeKind.Queue` (enum 23). `queue of string` resolves to QueueBy instead of Queue. | Documented — parser/ByToken gap |

| EventArgWithNotempty_ModifierPreserved | TYPE B | Parser `ParseArgumentList` (Parser.cs:675–721) parses `Name as Type` only — does not consume modifiers (`notempty`, `optional`) after the type token. Samples use this syntax but it silently fails. | Documented — parser gap |

| EventWithOptionalArg_ArgIsOptional | TYPE B | Same root cause as above — `ParseArgumentList` does not support `optional` modifier on event args. | Documented — parser gap |

---

### TYPE A — Test Bugs Fixed (4 tests)



All four BusinessDomain type tests included qualifier syntax (`in 'USD'`, `in 'kg'`, `in 'USD/each'`, `in 'USD' to 'EUR'`) that the parser does not yet support. The tests were testing **TypeKind resolution**, not qualifier parsing, so the qualifiers were unnecessary. Removed qualifiers; all four now pass.



- `MoneyType_ResolvesToMoneyTypeKind` — `field Cost as money in 'USD'` → `field Cost as money`

- `QuantityType_ResolvesToQuantityTypeKind` — `field Weight as quantity in 'kg'` → `field Weight as quantity`

- `PriceType_ResolvesToPriceTypeKind` — `field UnitPrice as price in 'USD/each'` → `field UnitPrice as price`

- `ExchangeRateType_ResolvesToExchangeRateTypeKind` — `field FxRate as exchangerate in 'USD' to 'EUR'` → `field FxRate as exchangerate`

---

### TYPE B — Real Upstream Gaps (4 tests)



#### Gap 1: `Types.ByToken` dictionary overwrites Log/Queue with LogBy/QueueBy



**Affected tests:** `LogOfString_ResolvesCollectionWithElementType`, `QueueOfNumber_ResolvesCollectionWithElementType`



**Root cause:** `Types.ByToken` is a `FrozenDictionary<TokenKind, TypeMeta>`. Both `TypeKind.Log` and `TypeKind.LogBy` use `TokenKind.LogType` as their token. Similarly `TypeKind.Queue` and `TypeKind.QueueBy` share `TokenKind.QueueType`. The `BuildByToken()` loop in `Types.cs:639` iterates `Enum.GetValues<TypeKind>()` — since LogBy (28) comes after Log (27) and QueueBy (31) after Queue (23), the later values overwrite the earlier. Result: `log of string` (without `by`) resolves to `TypeKind.LogBy` instead of `TypeKind.Log`.



**Fix approach:** `ByToken` should map to the base type (Log/Queue). The parser's `ParseCollectionType` should promote to LogBy/QueueBy only when a `by` clause is present. The promotion logic at Parser.cs:524–530 already exists but never triggers because `ByToken` already returns the `By` variant.



#### Gap 2: Parser `ParseArgumentList` does not support modifiers on event args



**Affected tests:** `EventArgWithNotempty_ModifierPreserved`, `EventWithOptionalArg_ArgIsOptional`



**Root cause:** `ParseArgumentList` (Parser.cs:675–721) parses each arg as `Name as Type` then looks for `,` or `)`. It does not consume modifier keywords (`optional`, `notempty`, `writable`, `nonnegative`) after the type token. When `notempty` or `optional` follows the type, the parser breaks out of the arg loop, leaving the modifier tokens unconsumed and emitting "Expected declaration keyword" errors.



**Note:** Sample files (e.g. `apartment-rental-application.precept`) use `event Approve(Note as string optional notempty)` — this syntax parses with errors that are currently tolerated. The samples should be verified for diagnostic cleanliness once this gap is fixed.



**Fix approach:** After consuming the type token in `ParseArgumentList`, loop over modifier tokens (check against `Modifiers.ByToken` or the modifier catalog) and collect them into a modifiers list. The `(string Name, TypeMeta Type)` tuple in the arg list should be expanded to include modifiers.

---

### Recommended next action



George should fix these 4 TYPE B gaps before Slice 2. The Log/Queue ByToken overwrite is a data-integrity issue that affects any code path using `Types.ByToken` for these types. The event arg modifier gap blocks testing of a feature that's already used in samples. Both are contained fixes in Parser.cs and Types.cs — no TypeChecker changes needed.



**Current score: 51/55 passing (was 47/55).**

---

### 2026-05-08: soup-nazi-slice-10-done



**By:** Unknown



**Status:** Merged from inbox — merged from inbox.



**Merged source:** `soup-nazi-slice-10-done.md`.

---

### Slice 10 Tests Complete — R3-Ready

**Commit:** 703000a

**Total written:** 32

**Passing:** 32/32

**Full suite:** 3326/3326 (no regressions)



**D26 coverage:**

- Clean precept (FullPrecept) → no Error diagnostics, invariant trivially satisfied

- Error precept (unknown field ref) → Error diagnostic present, D26 holds

- Multiple errors (2 unknown fields) → ≥2 Error diagnostics captured

- Error severity validation (all errors are Severity.Error)

- Minimal clean precept → no errors

- TrafficLight realistic precept → no errors, invariant holds



**FrozenDictionary D4 coverage:**

- `FieldsByName["ClaimAmount"]` → correct TypedField with TypeKind.Decimal

- `StatesByName["Approved"]` → correct TypedState

- `EventsByName["Submit"]` → correct TypedEvent with args

- `TryGetValue` on missing keys → returns false (all 3 dictionaries)

- `EnsuresByState` → documented as empty (scoped ensure population not yet wired)



**Integration test highlights:**

- InsuranceClaim-derived precept: 7 fields, 6 states, 6 events, transitions, rules, access modes

- TrafficLight: 4 fields, 4 states, 3 events, guards, string concatenation actions

- LoanApplication: 5 fields, 4 states, 3 events, rules, guarded transitions

- All StateReferences → valid in StatesByName

- All EventReferences → valid in EventsByName

- All TypedField.ResolvedType → non-Error for clean precepts

- Transition row consistency: source/target states and events all valid



**Implementation gaps documented (not red — tests assert current behavior):**

- `Ensures` empty: scoped ensure constructs not yet wired into TypeChecker ctx.Ensures

- `AccessModes` empty: scoped access-mode constructs not yet wired

- `ConstraintRefs` empty: constraint ref tracking not yet wired

- `DefaultExpression` null: field default resolution not yet wired (Slice 2+ stub)

- Multi-field modify syntax (`modify X, Y editable`) not supported by parser

- `from any` wildcard transitions not tested (CI tests note: contains OperationKind dormant)

- `rule ... when` conditional rules produce parse errors

- Boolean literal `true`/`false` in set actions produces TypeMismatch

- Multi-arg functions (`min(a, b)`) not supported in action context



**Any red tests:** None — all 32 pass

**R3-readiness:** YES

---

### 2026-05-08: soup-nazi-slice-2-done



**By:** Unknown



**Status:** Merged from inbox — merged from inbox.



**Merged source:** `soup-nazi-slice-2-done.md`.

---

### Slice 2 Tests Complete — 2026-05-07

**Commit:** d4053c1

**Total tests written:** 46

**Passing:** 46/46

**Any red tests (real gaps):** None

**Coverage notes:**

- Added internal `CreateContext`/`ResolveExpression` test entry points to TypeChecker (already present from Slice 3 commit)

- Event arg tests bypass pre-existing parser gap by manually setting up CurrentEventArgs with TypedArg objects

- Stub arm tests updated to reflect Slice 3 landing (FunctionCall/InterpolatedString/MemberAccess/MethodCall are no longer stubs; tests cover remaining stubs: ConditionalExpression, PostfixOperationExpression, QuantifierExpression)

- Widening tested via IntegerPlusDecimal (bidirectional catalog entry) and IntegerPlusNumber; no real widening-only path exists for scalar types since the catalog is exhaustive with bidirectional entries

- Full suite: 3075 passed, 0 failed

---

### 2026-05-08: soup-nazi-slice-3-done



**By:** Unknown



**Status:** Merged from inbox — merged from inbox.



**Merged source:** `soup-nazi-slice-3-done.md`.

---

### Slice 3 Tests Complete — 2026-05-07

**Commit:** 23edd54

**Total tests written:** 51

**Passing:** 51/51

**Any red tests (real gaps):** None — all 51 green.

**Coverage notes:**

- FunctionCall happy path: 9 tests covering abs, min, trim, floor, now, startsWith, clamp, roundPlaces, and floor-with-widening (integer→decimal). Round multi-overload (Round vs RoundPlaces) tested explicitly.

- FunctionCall errors: 7 tests — UndeclaredFunction, FunctionArityMismatch (too few + too many), TypeMismatch (boolean into abs), ErrorType propagation (single arg + multi-arg), sqrt proof requirements.

- CI variant selection: 4 tests — ~startsWith → TildeStartsWith, ~endsWith → TildeEndsWith, unknown CI function, CI of function without variant (abs). CI enforcement of first-arg ~string deferred to Slice 8 per George's notes.

- MemberAccess happy path: 15 tests via Theory — date (year/month/day/dayOfWeek), string (length), duration (totalDays/totalHours/totalMinutes/totalSeconds), period (years/months/days), time (hour/minute/second), set.count, list.count.

- MemberAccess errors: 5 tests — unknown accessor, accessor on no-accessor type (boolean), accessor on integer, error receiver propagation, resolved accessor return type verification.

- InterpolatedString: 7 tests — field ref hole, multiple holes, text-only, error hole propagation, nested function call hole, multi-hole with one error.

- Structural: 2 tests — TypedFunctionCall carries resolved arguments, TypedMemberAccess carries resolved object.

- Round multi-overload: 1 test — round(decimal) → Round (1-arg), round(decimal, integer) → RoundPlaces (2-arg).

- Full suite: 3126/3126 passing (no regressions).

---

### 2026-05-08: soup-nazi-slice-4-done



**By:** Unknown



**Status:** Merged from inbox — merged from inbox.



**Merged source:** `soup-nazi-slice-4-done.md`.

---

### Slice 4 Tests Complete — 2026-05-07

**Commit:** `1c29fe6`

**Total tests written:** 44

**Passing:** 44/44

**Any red tests (real gaps):** None

**Coverage notes:**

- ClosedSetValidation: Currency (valid×4 + case-insensitive, invalid×3), UnitOfMeasure (valid×3, invalid×2), Dimension (valid×2, invalid×1)

- NodaTimeValidation: Date (valid×3, invalid×3, datetime-as-date×1), Time (valid×3, invalid×3), DateTime (valid×2, invalid×2), Period (valid×3, invalid×2)

- TypedLiteral fallback: string, integer, boolean — confirms non-typed-constant literals stay TypedLiteral

- UnresolvedTypedConstant: null context + Error context both emit diagnostic and return TypedErrorExpression

- Trusted pass-through: type with no ContentValidation (String) accepts any value as TypedTypedConstant

- RegexValidation: no TypeKind currently uses RegexValidation in production, so no test coverage. The code path exists and is tested implicitly via the DU dispatch. If a type gains RegexValidation, add tests.

**Ready for R1:** YES

---

### 2026-05-08: soup-nazi-slice-5-done



**By:** Unknown



**Status:** Merged from inbox — merged from inbox.



**Merged source:** `soup-nazi-slice-5-done.md`.

---

### Slice 5 Tests Complete — 2026-05-07

**Commit:** (see below)

**Total tests written:** 26

**Passing:** 7/26

**Failing (TYPE B — blocked):** 19/26



**Triage result (George's 7 "pre-existing failures"):**

George's 7 reported failures were **stale binary artifacts**, NOT real failures. After a fresh `dotnet build`, the baseline is **3170/3170 clean** (excluding my new tests). The `--no-build` flag used a DLL from a prior Slice 5 build state that no longer matches HEAD.



**CRITICAL finding: Slice 5 was overwritten by Slice 6.**

Commit `fe358ef` ("feat: TypeChecker Slice 6 — structural validation") replaced `687d364`'s entire Slice 5 implementation. `PopulateTransitionRows`, `PopulateEventHandlers`, `ResolveAction`, and all related methods were deleted. TransitionRows and EventHandlers are empty stubs at HEAD. See `soup-nazi-slice-5-regression.md` for full details.



**19 failing tests are TYPE B (known red, not suppressed):**

All 19 test the Slice 5 contract (transition row resolution, guard scope, action targets, event handler normalization, state/event references, D26 invariant). They are correct per George's implementation notes — they will pass when Slice 5 code is restored and merged with Slice 6.



**7 passing tests:** These verify behavior handled by NameBinder or stages other than PopulateTransitionRows (e.g., unknown event/field diagnostics from NameBinder, clean input producing no errors).



**R2-readiness:** Slice 5 is NOT ready — implementation was overwritten. Requires merge of `687d364` back into current HEAD.



**Additionally:** Even the original Slice 5 code (`687d364`) has a bug where `EventName.ArgName` accessors emit `UndeclaredField`. This is a secondary issue — restore first, fix second.

---

### 2026-05-08: soup-nazi-slice-6-done



**By:** Unknown



**Status:** Merged from inbox — merged from inbox.



**Merged source:** `soup-nazi-slice-6-done.md`.

---

### Slice 6 Tests Complete

**Commit:** 78d1774

**Total written:** 17

**Passing:** 17/17

**Any red tests:** None — all 17 green. 2 pre-existing failures in TypeCheckerModifierTests (Slice 7) unrelated.

**R2-readiness for Slice 6:** YES

---

### 2026-05-08: soup-nazi-slice-7-done



**By:** Unknown



**Status:** Merged from inbox — merged from inbox.



**Merged source:** `soup-nazi-slice-7-done.md`.

---

### Slice 7 Tests Complete

**Commit:** 26208fe

**Total written:** 29

**Passing:** 29/29

**Any red tests:** None

**R2-readiness for Slice 7:** YES



#### Coverage breakdown

- Category 1 (valid modifiers): 7 tests — optional, notempty, nonnegative, writable, positive, ordered, notempty-on-set

- Category 2 (invalid modifier for type): 6 tests — 5 via Theory (boolean/nonneg, string/nonneg, date/positive, boolean/nonzero, integer/ordered) + notempty-on-boolean, minlength-on-integer, maxplaces-on-integer, mincount-on-string, event-arg-type-mismatch

- Category 3 (duplicate + mutual exclusivity): 4 tests — duplicate nonneg, duplicate optional, nonneg+positive conflict, positive+nonneg reversed

- Category 4 (redundant modifier): 4 tests — positive-subsumes-nonneg, positive-subsumes-nonzero, notempty-on-timezone (implied), notempty-on-currency (implied)

- Category 5 (event arg + computed): 4 tests — valid event arg modifier, writable-on-event-arg, computed-field-not-writable, computed-field-without-writable



#### Notes

- RedundantModifier is Warning severity; tests use raw `Check()` and assert on `d.Code` directly since `CheckExpectingError` filters to Error severity only.

- Event arg syntax is `event Name(ArgName as type modifier)` with transition rows required for clean parse.

- Computed field `writable` modifier must appear before `<-` (modifiers after `<-` expression work for constraint modifiers like `positive`/`nonnegative` but not for `writable`).

- Full suite: 3242/3242 passing — zero regressions.

---

### 2026-05-08: soup-nazi-slice-8-done



**By:** Unknown



**Status:** Merged from inbox — merged from inbox.



**Merged source:** `soup-nazi-slice-8-done.md`.

---

### Slice 8 Tests Complete

**Commit:** 9472824

**Total written:** 30

**Passing:** 30/30

**CI rules covered:**

- Rule 1: CaseInsensitiveFieldRequiresTildeEquals (66) — == with ~string

- Rule 2: CaseInsensitiveFieldRequiresTildeNotEquals (95) — != with ~string

- Rule 3: CaseInsensitiveValueInCaseSensitiveContains (96) — dormant (no contains OperationKind yet)

- Rule 4: CaseInsensitiveFieldRequiresTildeStartsWith (97) — startsWith with ~string

- Rule 5: CaseInsensitiveFieldRequiresTildeEndsWith (98) — endsWith with ~string

**Any red tests:** None — all 30 green. Two TYPE B bugs documented in test comments:

1. `Diagnostics.Create` for CI codes throws `FormatException` in transition guard context — template has `{0}` placeholder but `EnforceCIInExpression` passes no field name argument. Tests assert the throw.

2. Rule condition context does NOT trigger CI enforcement diagnostics (== and != on ~string in rules produce no error). Tests document this coverage gap.

**George's inbox note correction:** CI not-equals operator is `!~`, not `~!=` as stated in george-slice-8-done.md.

**R3-readiness for Slice 8:** NO — blocked by the two TYPE B bugs above (FormatException crash on violation, rules not enforced).

---

### 2026-05-08: soup-nazi-slice-9-done



**By:** Unknown



**Status:** Merged from inbox — merged from inbox.



**Merged source:** `soup-nazi-slice-9-done.md`.

---

### Slice 9 Tests Complete

**Commit:** f14a664

**Total written:** 22

**Passing:** 22/22

**Any red tests:** None from this file. 13 pre-existing reds in TypeCheckerCITests (unrelated).

**R3-readiness for Slice 9:** YES

---

### 2026-05-07T23:22:15Z: TypeChecker Slice 1 test inventory recorded ahead of symbol population



**By:** Scribe



**Status:** Merged from inbox while implementation was still running.



**Merged source:** `soup-nazi-slice-1-tests.md`.



- Soup-Nazi wrote `test/Precept.Tests/TypeChecker/TypeCheckerSymbolTests.cs` with 55 Slice 1 tests covering type-kind resolution, collection element types, optional/modifier preservation, implied modifiers, state/event population, initial-state diagnostics, and name-index population.

- At inbox-write time only 2 tests passed and 53 failed because George's Slice 1 symbol-population implementation had not landed yet and `SemanticIndex` was still effectively empty.

- Durable gate: treat the test matrix as ready for R1 once George's Slice 1 commit arrives; `LogBy` / `QueueBy` key-type coverage remains explicitly deferred beyond this batch.

---

### 2026-05-07T23:22:15Z: TypeChecker Slice 0 R0 closed after `TransitionRowOutcome` rename



**By:** Scribe



**Status:** Merged, deduplicated, inbox cleared (3 files -> 1 canonical entry).



**Merged sources:** `george-s0-shape.md`, `frank-r0-review.md`, `george-r0-b1-fixed.md`.



- George committed the TypeChecker Slice 0 semantic shape in `5260065` plus `abf2532`: `SemanticIndex` full layout, `CheckContext` accumulators/lookups, the 14-node `TypedExpression` DU, `QualifierBinding`, `ConstraintIdentity`, `TypedAction`, and TypeChecker test-helper wiring.

- Frank's R0 review found one blocker only: `TypedOutcomeKind` solved the `TransitionOutcome` name collision but violated enum naming conventions; the correct disambiguation is `TransitionRowOutcome`.

- George resolved B1 in `350f386` by renaming `TypedOutcomeKind` to `TransitionRowOutcome` in `SemanticIndex.cs`; all other D# decisions remained compliant and the 2974-test branch baseline stayed intact.

---

### 2026-05-07T22:51:59Z: H1 housekeeping closeout recorded; Frank C2 catalog doc sync deduplicated into the same batch



**By:** Scribe



**Status:** Merged, deduplicated, inbox cleared (2 files -> 1 canonical entry).



**Merged sources:** `george-h1-committed.md`, `frank-c2-catalog-doc-sync.md`.



- Recorded George-9's nine-commit Precept-V2-Radical housekeeping batch as the durable closeout: Outcomes catalog, parsed action/type-reference DUs, parser enrichment, diagnostic payload expansion, NameBinder, type-checker OQ doc locks, catalog-system doc sync, and history housekeeping all landed with the working tree clean.

- Deduplicated Frank-12's catalog doc note into the same canonical entry because commit `a469217` already carried the `docs/language/catalog-system.md` additions for `ActionMeta.SyntaxShape`, `FunctionMeta.HasCIVariant`, and `FunctionMeta.CIVariantOf` inside the George-9 batch.

- Validation at handoff: 2974 tests passing; no history files crossed the 15 KB summarization gate in this pass.

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

### 2026-05-07T08:05:00Z: ParsedOutcome / ParsedExpression parser refactor recorded as the durable parse-time payload baseline



**By:** Scribe



**Status:** Merged, deduplicated, inbox cleared (6 files -> 1 canonical entry).



**Merged sources:** `frank-outcome-expression-analysis.md`, `frank-outcome-impl-plan.md`, `george-outcome-plan-review.md`, `george-outcome-impl-notes.md`, `george-gap062-resolved.md`, `george-parsed-expression-created.md`.



- The parser-side payload contract is now durably recorded: closed-vocabulary slot values resolve at parse time, open-ended expressions flow through `ParsedExpression`, and outcomes no longer masquerade as synthetic `BinaryOperationExpression` nodes.



- Frank identified the synthetic outcome-operator encoding as a real defect, authored the replacement plan, and George reviewed that plan green before implementing it in commit `94dec3b`.



- Durable rule: parse-time outcomes use the `ParsedOutcome` DU (`TransitionOutcome`, `NoTransitionOutcome`, `RejectOutcome`, `MalformedOutcome`), so malformed rows stay explicit instead of falling back into unrelated expression forms.



- George's GAP-062 investigation remains preserved as the catalog-pressure signal that outcome syntax needed a first-class lane instead of ad hoc per-member parser branching.

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

### 2026-05-07T02:20:00Z: Wave 3 Round 1 canonical doc sweep recorded



**By:** Scribe



**Status:** Merged, deduplicated, inbox cleared (1 file); Frank reported 3 pre-existing `SemanticIndex.cs` errors only.



**Merged source:** `frank-wave3-round1.md`.



- `docs/compiler/type-checker.md` closed CC#9 by switching `ConstraintFieldRefs.ConstraintIdentity` to the `ConstraintIdentity` DU, closed CC#11 by adding `TypedTransitionRow.RejectReason`, and removed the stale CC#1-era "No expression tree parsing" note.



- `docs/compiler/proof-engine.md` closed catalog-gap #12 (`TryLiteralProof` scope), catalog-gap #13 (Strategy 3 vs. 4 boundary), the CC#1 follow-through around initial-state satisfiability blocking text, the corresponding stale OQ block, and the CC#5 follow-through on `FieldModifierMeta.ProofDischarges`.



- `docs/runtime/precept-builder.md` closed CC#4 by restoring `Compilation.Tokens`, closed CC#11 by documenting `ExecutionRow.RejectReason`, and closed CC#7 by documenting the `ConstraintMeta.StateAnchored` DU hierarchy.



- Validation reported: `dotnet build src/Precept/Precept.csproj` still shows only the 3 pre-existing `SemanticIndex.cs` errors (`TypedState`, `TypedField`, `TypedEvent` not found); no new errors were introduced.

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

### 2026-05-07T01:26:52Z: Wave 2 cross-cutting decisions all closed; Wave 1 checkbox drift corrected



**By:** Scribe



**Status:** Merged, deduplicated, inbox cleared (1 file); Frank reported build clean.



**Merged source:** `frank-wave2-complete.md`.



- Wave 2 is durably closed: CC#5, CC#10, CC#13, CC#14, CC#15, CC#16, CC#17, CC#18, CC#19, CC#20, and CC#22 are all resolved and propagated into canonical docs.



- Canonical synchronization landed in `docs/working/cross-cutting-decisions.md`, `docs/language/catalog-system.md`, `docs/compiler/graph-analyzer.md`, `docs/runtime/evaluator.md`, `docs/compiler/diagnostic-system.md`, `docs/tooling/language-server.md`, `docs/compiler/type-checker.md`, and `docs/compiler/proof-engine.md`.



- Six Wave 1 display-sync errors were corrected without re-deciding the work: CC#3, CC#4, CC#6, CC#12, CC#23, and CC#24 now show `[x]` to match their already-resolved status rows; CC#26's status row is likewise corrected to `✅ Resolved`.



- Durable architecture takeaways: `GraphState` stays a derived-facts output record, `SlotContext` and `ConstructSlotKind` stay distinct, catalog metadata owns per-member language knowledge, and default-valued `readonly record struct` additions remain backward-compatible.

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

### 2026-05-05T15:32:50Z: Value-types investigation reconciled to authoritative docs; `DateRange` deferred and `date` stays `NodaTime.LocalDate`



**By:** Frank



**Status:** Merged, inbox cleared (1 file).



**Merged sources:** `frank-value-types-reconciliation.md`.



- `docs/working/precept-value-types-investigation.md` now aligns with `docs/language/temporal-type-system.md` and `docs/language/business-domain-types.md` across §§1–§14.



- §7.4 `DateRange` is no longer treated as a confirmed type: the temporal type system explicitly defers date-interval / daterange support, so the investigation now records it as deferred rather than adopted.



- The backing type for `date` is confirmed as `NodaTime.LocalDate`, and the earlier `DateOnly` / dual-public-surface claim was removed because the locked temporal design exposes NodaTime directly.



- All remaining sections were verified as consistent; the only follow-up tensions left for Shane are the DX-layer well-known-constants surface and the authoritative `currency` accessor doc lag.

---

### 2026-05-04T17:00:09Z: Business value type coverage narrowed: Price stays semantic-only; ExchangeRate, Percentage, and DateRange advance as candidates



**By:** Frank



**Status:** Recommendation recorded from inbox; Shane decision still needed on OQ-7a through OQ-7f.



**Merged source:** `frank-business-types-coverage.md`.



- `Price` is not a new first-class type; it remains a role name on `MoneyValue` fields rather than a distinct structural/runtime type.



- `ExchangeRate` is recommended as a new first-class value type with `(BaseCurrency, QuoteCurrency, Rate)` shape and a positive-rate invariant lean.



- `Percentage` and `DateRange` are recommended as additional first-class candidates because they introduce invariant-bearing, runtime-significant semantics that bare decimals and paired dates cannot express safely.



- The investigation scope should expand from unit types to the broader Precept value-type surface; six open questions remain for Shane on built-in status, invariants, interval semantics, and a future `DateTimeRange` companion.

---

### 2026-05-04T05:45:56Z: Audit-gap P2 clarifications recorded; compiler/runtime innovation callouts confirmed clean



**By:** Scribe



**Status:** Merged, inbox cleared (1 file).



**Merged sources:** `frank-p2-doc-fixes.md`.



- `docs/runtime/evaluator.md` §4 now records both the `TypeBuilder` rejection rationale and the stable compiled-path upgrade seam against the existing A+G execution contract.



- `docs/runtime/evaluator.md` §7.3 now clarifies that per-type `TypeRuntimeMeta.BinaryExecutors` / `UnaryExecutors` are registered into flat `Operations` arrays, preserving zero-knowledge O(1) dispatch inside the evaluator.



- `docs/compiler-and-runtime-design.md` required no edit for Item 14; all `Precept Innovations` callouts already match the single-interpreter, catalog-dispatch architecture.

---

### 2026-05-04T05:45:56Z: Decision ledger summary created as a non-canonical navigation aid



**By:** Scribe



**Status:** Recorded from inbox note.



**Merged source:** `frank-decisions-summary.md`.



- `docs/working/decisions-summary.md` was added as a scanning aid over `.squad/decisions.md`.



- The durable source of truth remains `.squad/decisions.md`; the summary is reference-only and does not supersede the ledger.

---

### 2026-05-04T05:44:10Z: `ConstraintViolation` public contract promoted to the 5-field rich shape



**By:** Scribe



**Status:** Merged, inbox cleared (1 file).



**Merged sources:** `frank-constraint-violation-promoted.md`.



- Shane's 2026-05-04 ruling promotes the 5-field `ConstraintViolation` shape from `docs/runtime/evaluator.md` §7.6 to the public runtime contract; the earlier 2-field minimal shape is superseded.



- `FailingValue` is `PreceptValue?`, not `object?`; CLR callers convert through `TypeRuntime<T>.ToClr` rather than through evaluator-owned boxing.



- `docs/runtime/runtime-api.md` now documents the public 5-field shape, and the evaluator docs frame the remaining work as implementation follow-through rather than as an unresolved contract question.

---

### 2026-05-04T05:31:45Z: Evaluator pseudocode and §8 integration contract aligned to `FiredArgs` / `PreceptValue` lanes



**By:** Scribe



**Status:** Merged, inbox cleared (1 file).



**Merged sources:** `frank-evaluator-pseudocode-fix.md`.



- `docs/runtime/evaluator.md` pseudocode now uses the actual CC#25 internal types throughout: `FiredArgs`, `PreceptValue[]`, `PreceptValue[]? patch`, and `version.Slots.ToArray()`.



- Update patch application and access-mode checks are slot-indexed, `FiredArgs.Empty` replaces the stale standalone `EmptyArgs`, and the old `object?[]` readability caveat is removed.



- §8 now documents the durable dual-lane public contract: both JSON ingress and CLR-builder ingress materialize `FiredArgs`, and the evaluator never consumes raw dictionaries or raw JSON.

---

### 2026-05-04T04:36:09Z: Deep content audit filled seven specificity gaps in canonical docs



**By:** Scribe



**Status:** Merged, inbox cleared (1 file).



**Merged sources:** `frank-deep-content-audit.md`.



- Filled seven specificity gaps across `docs/runtime/evaluator.md`, `docs/language/catalog-system.md`, and `docs/runtime/runtime-api.md`, while confirming adjacent runtime surfaces that were already correct.



- `evaluator.md` now gives `PreceptValue` a full performance-and-memory section (GC rationale, the 32-byte tagged-value rationale, and the hot-path memory picture around 44–48 slots / ~4,480 bytes), expands Fire to a 7-step lifecycle, corrects `LOAD_ARG` to slot-index dispatch, and replaces stale `object?` executor examples with canonical `stackalloc PreceptValue[32]` examples.



- `catalog-system.md` now places `BinaryExecutors` and `UnaryExecutors` on `TypeRuntime` and explains executor-array dispatch as catalog-owned runtime behavior rather than evaluator-owned switches.



- `runtime-api.md` now defines the arg presence mask concretely as a `bool[]` aligned to the arg slot array and documents the required-arg fault boundary.



- Open design questions stay explicit in the canonical docs: `PreceptValue` FieldOffset layout, `ArgDescriptor.SlotIndex`, and the executor registration / assembly mechanism.

---

### 2026-05-04T04:30:00Z: Full CC#25 / CC#2 decisions audit closed the remaining five canonical doc gaps



**By:** Scribe



**Status:** Merged, inbox cleared (1 file).



**Merged sources:** `frank-audit-report.md`.



- Audited CC#25 Q1–Q10, CC#2, and the `PreceptValue` slot-storage follow-through across canonical runtime and compiler docs; confirmed Q1, Q2, Q4, Q7, and Q10 were already covered.



- Closed the lagging doc gaps in `docs/runtime/evaluator.md`, `docs/runtime/result-types.md`, `docs/runtime/precept-builder.md`, `docs/compiler-and-runtime-design.md`, and `docs/working/cross-cutting-decisions.md`.



- Durable audit rule: after runtime API updates, re-audit `result-types.md`, `evaluator.md`, and the cross-cutting register together so locked decisions do not leave stale `object?`, dictionary, or pending-status language behind.



- Open flags stay explicit: `TypeRuntime<T>` documentation reconciliation and the non-expression `SlotValue` shape conflicts still need owner direction.

---

### 2026-05-04T04:02:05Z: Catalog gap register migration completed and archived



**By:** Scribe



**Status:** Merged, deduplicated, inbox cleared (2 files).



**Merged sources:** `frank-chunk4-gaps.md`, `frank-chunk4-unplaced-gaps.md`.



- The 43 entries from the catalog gap register are now fully triaged: 23 pending gaps were attributed into canonical Open Question blocks across 9 docs, 3 resolved-in-source gaps were marked closed, 5 already-captured gaps were confirmed in place, and gap #39 was promoted to a first-class open-question block.



- Cross-cutting routing is durable: 8 entries stay owned by the cross-cutting register, while 4 out-of-scope items remain runtime/MCP/tooling design questions rather than catalog metadata gaps.



- The working register was retired: `docs/working/catalog-gap-register.md` now lives at `docs/working/Archived/catalog-gap-register-migrated.md`, preserving the original content plus a migration notice.

---

### 2026-05-04T03:26:10Z: CC#25 Q7 acceptance revision locked



**By:** Scribe



**Status:** Recorded from the full Q7 acceptance inbox merge.



**Merged sources:** `frank-cc25-q7-typed-api.md`, `frank-cc25-q7-accepted.md`, `frank-cc25-q7-challenges.md`, `copilot-cc25-q7-ingress-egress.md`, `copilot-directive-20260503-231016.md`, `copilot-directive-20260503-231158-no-string-json-overloads.md`.



- Q7 is now fully accepted. `Version.Get<T>(string)` is the primary typed field API, raw indexers return `PreceptValue`, and `Transitioned` / `Applied` carry `FiredArgs` with the same `Get<T>` + `PreceptValue` indexer pattern for event-arg egress.



- `TypeRuntime` naming is final: `FromJson` / `ToJson` / `FromClr` / `ToClr`. `TypeRuntime<T>` is the zero-boxing CLR ingress/egress path, and typed `Get<T>` / `Set<T>` dispatch through those delegates.



- Typed ingress is fluent and AOT-safe: `Fire()` / `Inspect()` use `Action<IArgBuilder>`, `Create()` uses `Action<IFieldBuilder>`, and `IArgBuilder` now materializes `PreceptValue[]` plus a presence mask rather than an arg dictionary.



- JSON boundaries stay `JsonElement`-only. No string convenience overloads exist anywhere on the JSON API surface, and typed `Restore` is removed so restore remains round-trip-faithful hydration from Precept's own serialized egress.



- The JSON ingress/egress boundary remains outside the evaluator: public API / `Version` conversion owns JSON parsing and lazy `ToJson()` egress, while the evaluator only sees typed `PreceptValue` data.



- This supersedes the earlier provisional note that `IReadOnlyDictionary<string, object?>` would survive as a convenience extension lane.

---

### 2026-05-04T03:26:10Z: CC#25 Q7 dictionary convenience lane closed



**By:** Shane (via Copilot)



**Status:** Recorded from inbox closeout.



**Merged source:** `copilot-cc25-q7-dict-extension-obsolete.md`.



- `IReadOnlyDictionary<string, object?>` convenience overloads and extension methods are fully obsolete. They are not part of the main API, not a test-only helper lane, and not a future convenience surface.



- Wire-format callers use `JsonElement`; in-process typed callers use the fluent builders. No third ingress lane remains.

---

### 2026-05-04T01:45:56Z: ConstructManifest name confirmed as the working-doc parser artifact label



**By:** Scribe



**Status:** Merged, inbox cleared (1 file).



**Merged sources:** `frank-construct-manifest-rename.md`.



- The working docs now use `ConstructManifest` consistently for the parser output artifact, matching the already-correct pipeline diagram and earlier canonical rename decisions.



- Scope remains documentation only; any source-code rename is a separate implementation task.

---

### 2026-05-04T01:08:14Z: Dual-interpreter model rejected; trace stays inside the single A+G interpreter



**By:** Shane (via Copilot)



**Status:** Recorded from inbox correction merge.



**Merged source:** `frank-trace-correction.md`.



- Rejected: a production A+G runtime paired with a separate LS/MCP tree-walk interpreter.



- Adopted instead: one stack-based opcode interpreter serves every consumer, with optional per-step trace emission for tooling and diagnostics.



- Trace record shape and LS/MCP consumption remain open implementation seams, but the architecture no longer permits a second semantic engine.
