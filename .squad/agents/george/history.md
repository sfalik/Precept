## Core Context





- Owns code-level feasibility, runtime implementation detail, and architecture-to-code translation across parser, checker, analyzer, and tooling surfaces.


- Co-owns language research/design grounding with Frank and converts approved language decisions into implementable parser/catalog structures.


- Active durable baseline: parser/type-checker work should stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.





## Learnings





- The checker start point is locked: pre-Slice 0 typed shapes first, existing `Operations` multi-candidate lookup APIs stay canonical, `TypedInputAction` needs an explicit secondary-role discriminator, and `[HandlesCatalogMember]` ownership moves from stub to real handlers slice by slice.


- GAP-060 and GAP-061 are durable parser hygiene rules: variant-action arms hidden inside shape-specific methods must throw when unreachable, and parser-facing catalogs should expose O(1) indexes for the token axis the parser actually queries.


- CI function follow-through is now metadata-native: checker/tooling consumers resolve `~startsWith` and `~endsWith` through real FunctionKind/FunctionMeta entries rather than parser-side naming conventions alone.


- Shared-environment build discipline matters: targeted build/test commands are more reliable than full-solution runs when external file locks interfere with the workspace.


- **GAP-064 rule:** `CIFunctionCallExpression` stores only the bare name string — the TypeChecker will need a `Functions.ByCIVariantOf` reverse index (or a node design change) to resolve CI function calls to `FunctionKind`. Do not implement CI type-checking without first resolving this.


- **GAP-065 rule:** `QualifierMatch.Same` enforcement is a real correctness hole. The catalog declares it; the TypeChecker must honor it. File as Slice 1 TypeChecker work, not a deferred item.

- **BecauseClause catalog defect fix (2026-05-03):** `StateEnsure` and `EventEnsure` were missing `SlotBecauseClause` — a catalog defect inconsistent with `RuleDeclaration`. Fix: added `SlotBecauseClause` to both slot arrays; corrected `EnsureClause = 12` comment to `"ensure expression"` only. Parser is still a stub — when implemented, both parse paths must emit a standalone `BecauseClauseSlot` (not embedded in `EnsureClauseSlot`). LS and MCP need no immediate changes; both derive from catalog automatically. 2316 tests pass.





## Recent Updates

### 2026-05-03T00:51:29Z — Outcomes follow-through narrowed
- Scribe recorded Frank's outcomes batch and cleared the paired inbox notes.
- Carry-forward correction: George's earlier "outcome parsing lacks a catalog path" concern should no longer be interpreted as "add Outcomes.cs"; for this batch, the durable ruling is DU-only outcomes with token-level catalog support and `OutcomeProd()` as the parser path.
- Keep the remaining parser/checker blockers unchanged: `StructuralBoundaryTokens`, split-modifier metadata, variant-action dispatch metadata, and the Slice 4 `TypeMeta.LiteralRange?` / `ContentValidation` prerequisites.

### 2026-05-03T00:15:16Z — Pipeline cross-review corrections recorded
- Scribe merged George's pipeline review into the canonical ledger. Durable correction: `is set` / `is not set` precedence is already catalog-driven; only `.` and `(` remain hardcoded in the Pratt loop.
- `StructuralBoundaryTokens` derivation is P0, uniform action-shape statements must land before checker Slice 5, and `ParseFieldDeclaration` unification stays blocked on split-modifier metadata.
- The open implementation blockers remain explicit: outcome parsing still lacks a catalog, `TypeMeta.LiteralRange?` and `ContentValidation` still gate Slice 4, and variant-action shape parsers still carry inline kind-identity checks.

### 2026-05-02T18:39:02-04:00 — Pipeline cross-review produced

- Completed implementer's review of Frank's two catalog-bias analyses (type checker and parser) in `docs/working/george-catalog-driven-pipeline-review.md`.
- **Key factual correction on Pratt loop:** Frank claimed `is set`/`is not set` hardcode precedence. Source (Parser.Expressions.cs:60) reads from `Operators.ByTokenSequence(...)` — already catalog-driven. Only `.` = 80 and `(` = 90 are real hardcodes.
- **Split-modifier blocker confirmed:** `ParseFieldDeclaration` pre/post compute-expression modifiers require new catalog metadata before unification through `ParseConstructSlots` is safe. Frank P1 → my P2 until design settled.
- **Inline kind-identity checks missed by Frank:** `ParseCollectionValueStatement` lines 355/365/373 and `ParseCollectionIntoStatement` line 415 do `meta.Kind == ActionKind.X` mid-parse. Requires `ActionMeta.VariantTriggerToken?` or equivalent catalog field.
- **Ordering dependency table produced:** 8 explicit sequencing constraints: `TypeMeta.LiteralRange?` before Slice 4, `ActionMeta.TypedActionShape` before Slice 5, `ScopeRule` before Slice 3, uniform action-shape nodes before Slice 5 checker implementation.
- **Cross-cutting connections:** precomputed-table pattern bridges parser and checker; action-statement unification connects parser P1 and checker Slice 5; TypeParseShape DU benefits checker Pass 1; outcomes gap (GAP-062) is shared blind spot in both of Frank's documents.
- `TypeMeta.LiteralRange?` and `ContentValidation DU` remain unaddressed by Frank — still outstanding Slice 4 blockers.



### 2026-05-02T22:22:24Z — Iteration 11 audit session recorded


- Scribe merged George's iteration-11 findings into the canonical ledger, cleared all current decision inbox files, and wrote the audit closeout logs.


- Cross-agent context to retain: the durable batch now bundles the spike type-checker directive, both catalog-driven type-checker reviews, GAP-047 closure, Frank's GAP-048–056 doc/catalog gaps, and George's GAP-062–067 catalog-impl gaps.


- Health gate result: decisions archive ran under the 7-day rule before merge; no history summarization was required after propagation.





### 2026-05-02 — Iteration 11 catalog-impl audit (GAP-062–067)


- Filed 6 new gaps covering outcome dispatch, QueueBy sort direction, CIVariantOf dead metadata, QualifierMatch.Same enforcement missing, AllowedIn never enforced, and variant-action By/At dispatch using per-member identity.


- Most significant: GAP-065 (QualifierMatch.Same is a real correctness gap — qualifier mismatch on min/max/abs/clamp/round silently passes), GAP-064 (no ByCIVariantOf reverse index — TypeChecker has no catalog path to resolve CIFunctionCallExpression), GAP-062 (no Outcomes catalog — ParseOutcomeNode hardcodes all three outcome shapes).


- Three design questions need Shane input: (a) Outcomes catalog vs. inline parser, (b) ByCIVariantOf index vs. parser-level FunctionKind resolution, (c) QualifierMatch enforcement slice timing.


- Full findings in `.squad/decisions/inbox/george-iter11-findings.md`.





### 2026-05-02T21:58:21Z — Canonical review accepted and compacted


- Frank accepted George's checker review, resolved the open pre-requisites, and left transitive widening rejected; the canonical checker plan is now implementation-ready.


- Kramer and Soup-Nazi constraints are now part of the active baseline: keep typed models derivation-first and treat the 450-550 test envelope plus 3 non-negotiable gates as required follow-through.





### 2026-05-02 — Active implementation snapshot


- Keep the parser baseline from Iteration 10 live: no fake variant-action constructors in nested switches, and no parser-side linear scans where a catalog index belongs.


- The checker/evaluator next step remains explicit: partial-result error recovery, qualifier propagation, event-arg scope in event handlers, and explicit slice ownership for method/interpolated forms are no longer open design questions.





### 2026-05-02 — Historical Summary (fully compacted)


- Older active-history detail was moved to history-archive.md during Scribe closeout to keep George under the 15 KB gate.


- Use the archive for the Phase 2 closeout trail, parser-gap implementation sequence, and earlier analyzer-shipping notes.


