# DSL / Compiler / Runtime Bugs

Bugs discovered during sample authoring or development. Capture here when
something blocks idiomatic expression; fix as separate engineering work — do
**not** author hidden workarounds into samples or other consumers. If a
workaround is unavoidable, include a comment citing the BUG ID.

The proof engine `ConstraintContext` narrowing gap fixed earlier in the
V2-Radical spike is the precedent for this discipline: without it, the
workaround would have stayed buried in sample 29 and the bug would never have
surfaced for proper fixing.

## Bug template

```markdown
### BUG-NNN: <one-line summary>

- **Discovered**: YYYY-MM-DD during <context>
- **Affected**: <files / samples / features>
- **Symptom**: <what was observed — diagnostic code, error message, behavior>
- **Root cause** (if known): <source file / function / brief description>
- **Workaround used** (if any): <how it's currently sidestepped — and which files cite this BUG ID in a comment>
- **Fix complexity**: trivial / small / large / design-required
- **Priority**: blocks shipping / quality bar / nice-to-have
- **Repro**: <minimal `.precept` snippet or test case that triggers it>
```

## Active

### BUG-015: reassignment-invalidation not applied to index-bounds / key-presence / field-to-field guard strategies (still over-proves)

- **Discovered**: 2026-05-28 (BUG-014 follow-on; spec § 0.6 item 7 clause a).
- **Affected**: `TryIndexBoundsProof`, `TryKeyPresenceProof`, `TryFlowNarrowingProof` (`ProofEngine.Strategies.cs`) — they pull the row guard and discharge via `ExtractGuardBranches` / raw-guard walks but do **not** consult `ProofObligation.ReassignedBefore`. So the BUG-014 class persists there: e.g. `when Idx >= 0 -> set Idx = -1 -> Log.append(Coll.at(Idx))` would still discharge the index lower-bound via the stale `Idx >= 0` guard.
- **Symptom**: a `set`/`clear`-reassigned field's guard fact still discharges index-bounds / key-presence / field-to-field obligations after the reassignment — a live over-prove (unsound) hole, same shape as BUG-014 but in the strategies BUG-014's fix didn't cover.
- **Root cause**: BUG-014 added the `ReassignedBefore` filter only to `TryGuardInPathProof` (numeric + presence — the confirmed/reported path). The other three strategies share the substrate exposure by construction (exposed-by-reasoning; not yet probe-confirmed).
- **Fix complexity**: small-medium — apply the same `ReassignedBefore`-skip to the GuardConstraint-branch matches; the raw-guard walks (index upper-bound `ExtractParamUpperBoundsByBranch`, key-presence `GuardHasContainsCheck`) need the field-filter threaded through too.
- **Priority**: blocks shipping (soundness) — but narrower/rarer than BUG-014's numeric divisor case. Deferred from the BUG-014 slice to avoid an under-tested broad change at session end; do with per-strategy probes + tests.
- **Repro**: not yet probe-confirmed (reasoned from the shared substrate); construct an `Idx >= 0 -> set Idx = -1 -> .at(Idx)` probe when fixing.

### BUG-016: collection-mutation forward-propagation — guard facts not effect-adjusted across grow/shrink

- **Discovered**: 2026-05-28 (BUG-014 follow-on; spec § 0.6 item 7 clause b, "before the new assignment's facts are stored").
- **Affected**: collection-mutation actions (`append`/`insert`/`remove`/`pop`/`dequeue`/`put`) in an action chain under a `count > 0` (or presence) guard.
- **Symptom (two-sided)**: (1) *completeness* — a grow (`insert`/`append`) provably preserves `count > 0`, but BUG-014 deliberately doesn't track collection mutations, so a `set`-style fix can't re-establish the grown fact (no false positive today only because grows aren't treated as invalidating); (2) *latent soundness* — a shrink (`remove`/`pop`) can empty a collection, so `when count > 0 -> remove … -> <mutation needing count>0>` would discharge the second mutation via a stale `count > 0`. (`shopping-cart.precept`'s `insert`-then-`remove` is safe because the remove is last; a remove-then-mutate chain would not be.)
- **Root cause**: spec § 0.6 item 7's forward-propagation clause (re-establish facts from the new value, effect-aware per action: grow preserves / shrink invalidates `count`/presence) is unimplemented. `ActionWriteSemantics` (`Actions.cs`) classifies establish/clear effects and is the catalog hook a real fix would derive from.
- **Fix complexity**: design-required — effect-aware fact propagation, catalog-driven from `ActionWriteSemantics`. Overlaps the D9 design's gap A2 (forward narrowing propagation).
- **Priority**: quality bar (the shrink-then-mutate soundness case is rare; no sample hits it). Pair with the D9 forward-propagation work.

### BUG-014: guard facts survive field reassignment — sequential-proof-flow (spec item 7) not implemented for guard narrowing

- **Status**: ✅ **Fixed (numeric + presence, full-replacement) 2026-05-28.** `ProofObligation.ReassignedBefore` now carries the fields written by prior `set`/`clear` actions in the same chain (computed in `WalkActions`, `ProofEngine.cs`); `TryGuardInPathProof` skips guard constraints about those fields, so a `when X != 0` fact no longer discharges `100 / X` after `set X = 0` (and `when X is set` no longer survives `clear X`). Scope deliberately limited to **full-value-replacement** actions (`set`/`clear`): in-place collection mutations (`append`/`insert`/`remove`) transform rather than replace — blanket invalidation false-positived on `shopping-cart.precept`'s insert-then-remove (an `insert` grows the collection, so `count > 0` survives). Tests: `test/Precept.Tests/ProofEngine/ReassignmentInvalidationTests.cs` (4: divisor-reassign, no-reassign control, different-field control, presence-clear). Full suite 6433/6433. **Two follow-ons remain — see BUG-015 / BUG-016.** § 0.6 impl-status drift corrected in the same pass.
- **Discovered**: 2026-05-28 while grounding the D9 qualifier-narrowing design (`docs/Working/d9-qualifier-narrowing-design.md`).
- **Affected**: every guard-narrowing discharge — `TryGuardInPathProof` (numeric + presence), `TryFlowNarrowingProof` (field-to-field), `TryIndexBoundsProof`, `TryKeyPresenceProof`. All pull the row guard from `t.Row.Guard` (`ProofEngine.Strategies.cs`) and apply it to every body obligation regardless of intervening reassignment of the guarded field.
- **Symptom**: a guard fact discharges an obligation *after* the guarded field was reassigned to a guard-violating value. Confirmed unsound divide-by-zero proof:
  ```precept
  precept ReassignProbe
  field X as integer default 1
  field R as integer default 0
  state S initial
  event E
  from S on E when X != 0
      -> set X = 0
      -> set R = 100 / X
      -> no transition
  ```
  `precept_compile` → `success: true`, divisor obligation `Proved` via `GuardInPath`. At runtime `100 / X` divides by zero; the engine declared it structurally safe — a Principle-1 violation.
- **Root cause**: `precept-language-spec.md § 0.6` proof-contract **item 7 "Sequential proof flow"** mandates *"When a field is reassigned, prior proof facts about that field are invalidated before the new assignment's facts are stored."* The engine does not implement this for guard-derived facts — `TransitionRowContext(Row)` carries the whole row, not the obligation's action-chain position, so the discharge cannot tell a guard fact is stale. **Doc-drift**: § 0.6 Implementation status (~line 246) lists "sequential proof flow" among obligations *"implemented and exercised by the proof-engine test suite"* — false.
- **Workaround used**: none (surfaced by a probe; no sample exploits it).
- **Fix complexity**: large — thread action-chain position into the obligation/context (or precompute per-obligation "fields reassigned before me" in `WalkActions`, which already iterates actions in order) and have guard-consuming strategies drop constraints whose subject was reassigned before the obligation's site. Shared guard-substrate change benefiting all strategies.
- **Priority**: blocks shipping (soundness). **Being fixed as the foundational step of the D9 qualifier-narrowing slice** — the new narrowing layer requires the same invalidation, so fixing the substrate fixes both; correct the § 0.6 Implementation-status drift in the same pass.
- **Repro**: the `ReassignProbe` snippet above.

## Fixed

### Post-Phase-5 code-review remediation: interval-algebra soundness + catalog/diagnostic completeness

- **Status**: ✅ **Fixed by post-Phase-5 remediation Slice 1 (2026-05-27).** An extra-high-effort `/code-review` on the Phase 5 spike branch surfaced 15 findings spanning soundness, catalog completeness, and naming. Slice 1 (1a/1b/1c/1d) addressed 11 of the 15 against the locked Phase-5 designs:
  - **Interval-algebra soundness (Slice 1c)**: `BuildSiblingRejectExclusions` forfeits multi-leaf AND-branches (¬(A∧B) = ¬A∨¬B, not ¬A∧¬B); `BuildNarrowedIntervals` cross-branch OR-union back-fills the base interval for fields absent from a branch; `NegateConstraintToInterval` dispatches integer vs. decimal-backed domains — integer uses exact half-step `V ± 1`, decimal closes at `V` (sound superset). The same dispatch + sentinel-safe arithmetic applied to `NarrowByConstraint` in the satisfiability scan.
  - **Catalog completeness (Slice 1b)**: `TryResolveDimensionVector` consults `DimensionCatalog` for bare dimension names (`quantity of 'length'` resolves via the catalog, not UCUM); access-modifier validation now respects `MutuallyExclusiveWith`; `IntegerDivideInteger` and `IntegerDivideNumber` got `IntervalTransfer` functions so narrowed counters propagate through division.
  - **Cleanup (Slice 1a)**: removed dead `NumericInterval.Difference`; collapsed `ProofLedger` ctor to a single 6-arg form.
  - **Diagnostic rename (Slice 1d)**: `AlwaysFalsePeriodComparison` → `DegeneratePeriodComparison` (the code fires for both always-false `==` and always-true `!=`).
- **Discovered**: 2026-05-27 via 9-angle code-review on the Phase 5 spike branch
- **Affected**: any precept whose proof discharge relies on the satisfiability scan or sibling-reject narrowing with multi-field guards, OR-branches, decimal-typed fields; any catalog consumer of bare-dimension qualifiers; any author writing `!=` with disjoint period literals.
- **Tests added**: `test/Precept.Tests/ProofEngine/IntervalAlgebraSoundnessTests.cs` (3), `test/Precept.Tests/Operations/QuantityProductDimensionTests.cs::BareDimensionQualifier_LengthTimesLength_ResolvesViaDimensionCatalog` (1). Suite: 7140/7140 pass.
- **Deferred**: 4 of 15 findings (qualifier-policy wiring for `MoneyDividePrice`, `each * box` dimensionless-product policy, `UnsatisfiableRule` diagnostic split, reachability threading into `FieldNeverSet`) require `/lifecycle-2-design` passes and are pending in Slices 2–5.

### F-LANG-BIZ-08: Discrete equality narrowing for `choice of` fields

- **Status**: ✅ **Fixed by Phase 5 W-D BIZ-08 (2026-05-27, commit `08fae0ef`).** Extended `NumericConstraintSubsumes` in `ProofEngine.Strategies.cs` to recognize `F == V_lit` equality guards: when the guard pins a field to a singleton value, the singleton is checked directly against the obligation's (comparison, threshold) pair via a new `ValueSatisfiesRequirement` helper. Reuses the existing guard-decomposition pipeline (branch-walking discipline preserved — every OR branch must independently discharge). Minimal sound surface per the locked design: direct `F == literal` only; disjunctive equality and field-to-field equality are deferred as separate extensions. Tests in `test/Precept.Tests/ProofEngine/DiscreteEqualityNarrowingTests.cs` (4 new, including soundness negatives for `Severity == 0 ⇒ 1 / Severity` and field-scope checks).
- **Discovered**: 2026-05-25 during Phase 3 Step 3.3f verification
- **Affected**: any precept rule or guard that uses `choice == "literal"` and expects the proof engine to narrow the choice field to that value in the branch body
- **Original symptom**: `when Priority == "High"` did not narrow `Priority` to `"High"` inside the branch. Strategy 3 (GuardInPath) and Strategy 4 (FlowNarrowing) were numeric-only — no `BuildNarrowedDiscreteValues` analog. The equality operator resolved cleanly, but no narrowing strategy consumed the result.
- **Workaround used**: none needed for shipped samples — choice equality worked as a boolean condition; just didn't enable further proof narrowing.
- **Fix complexity**: ended up small — extending the existing subsumption switch with one equality arm + one value-satisfies helper. The "design-required" estimate proved too pessimistic: the reuse path through `GuardConstraint` made a new mechanism unnecessary.

### BUG-006: Proof engine doesn't combine guard narrowing with field-level `max` for arithmetic interval inference

- **Status**: ✅ **Fixed by Phase 5 W-C (2026-05-27, commit `c16be77b`).** `BuildNarrowedIntervals` in `ProofEngine.Intervals.cs` now composes sibling reject-row guards into the current row's per-field narrowing. New `BuildSiblingRejectExclusions` helper walks every reject row on the same `(state, event)` pair; for each leaf constraint, `NegateConstraintToInterval` produces the negated interval (integer-style half-open: `>= V` ⇒ `<= V-1`; conservative-on-decimal noted in the doc-comment). Required collateral: integer arithmetic ops (`IntegerPlusInteger`, `IntegerMinusInteger`, `IntegerTimesInteger`) now carry `IntervalTransfer` functions so the narrowed interval propagates through `Counter + 1`. Tests in `test/Precept.Tests/ProofEngine/CrossRowIntervalCompositionTests.cs` (3 new, including the verbatim repro). **Fix scope (post-Phase-5 sample-restore audit, 2026-05-28)**: the W-C cross-row narrowing discharges only **literal-comparison sibling rejects** (`Counter >= 5`). It does NOT compose for field-vs-field guards (`Counter >= MaxField`), narrowed-through-division (`(a+b+c)/3`), or presence narrowing (`when Field is not set -> reject`). Six of nine post-Phase-5 BUG-006 cites fell into these unfixable patterns; the runtime rule workaround was retained in those samples as the legitimate enforcement. Future extension of the W-C narrowing to field-vs-field guards is tracked as a separate follow-up.
- **Discovered**: 2026-05-24 during authoring of `samples/library-inter-library-loan.precept`
- **Affected**: any precept doing `set Counter = Counter + 1` in a row body where a sibling row above rejects when `Counter >= MaxField`, and `MaxField` carries a field-level `max N` modifier
- **Original symptom**: `PRE0078 UnprovedOverflow` on `Counter + 1` even though the guard row above structurally rejected the case where `Counter >= MaxField` AND `MaxField` was bounded by `max N`. The proof engine narrowed on each separately but did not combine them transitively.
- **Workaround used**: in `samples/library-inter-library-loan.precept`, dropped the field-level `max 5` on `RenewalCount` and relied on a runtime `rule RenewalCount <= MaxRenewals` invariant. Cited inline with a `# BUG-006:` comment.
- **Post-fix cleanup completed** (2026-05-28, commit `332f76ac`): `max N` restored on counters where the literal-comparison sibling-reject narrowing now discharges (`saas-trial-to-paid` ExtensionCount, `global-meeting-scheduler` ParticipantCount); `# BUG-006` cites stripped from the remaining 6 samples where the runtime rule is the proper enforcement (field-vs-field / division / presence-narrowing patterns the W-C fix doesn't reach).

### BUG-004: Proof engine ignores event ensures for transition-row body narrowing

- **Status**: ✅ **Fixed by Phase 5 W-A (2026-05-27, commit `034a5976`).** Extended the guard-extraction switch in `ProofEngine.Strategies.cs:TryGuardInPathProof` so event ensures on the row's event contribute their predicates to the row body's narrowing context. AND-combined with the row's own explicit guard via the new `CombineAndBranches` helper. Mirrors BUG-001's fix shape. Tests in `test/Precept.Tests/ProofEngine/EventEnsureNarrowingTests.cs` (4 new). Sample-restore pending: `samples/equipment-lease-agreement.precept` can drop the redundant `when MonthlyPayment is set` guard.
- **Discovered**: 2026-05-24 during authoring of `samples/equipment-lease-agreement.precept`
- **Affected**: any precept whose transition row body reads an optional field that an `on Event ensure Field is set` declaration has already proven present.
- **Original symptom**: `PRE0116 UnprovedPresenceRequirement` on `MonthlyPayment` (after `on Quote ensure MonthlyPayment is set`) even though the event ensure structurally rejects the event before the row body runs if the field is absent. Same shape as BUG-001 (rule/ensure `when` guard body narrowing) but for event ensures narrowing transition-row bodies.
- **Workaround used**: equipment-lease-agreement sample added a redundant `when MonthlyPayment is set` guard to the row with a `# BUG-004:` comment.
- **Post-fix cleanup pending**: remove the redundant `when X is set` guard from every row carrying a `# BUG-004` cite. **Scope-check**: sweep `grep -rn "# BUG-004" samples/`.

### BUG-013: `ParserIntegrationTests.TestSample_EventDeclaration_BindsInitialToCreateOnly` references missing `samples/Test.precept`

- **Status**: ✅ **Fixed by Phase 5 W-F (2026-05-27, commit `766637c0`).** `samples/Test.precept` restored with the minimal multi-event-with-`initial` shape the test asserts: `event create initial`, `event start`, `event stop`, `event reset`, three states (Idle/Running/Stopped), four transition rows. Test now passes.
- **Discovered**: 2026-05-25 during Phase 2 Step 2.7 verification of the test suite
- **Affected**: `test/Precept.Tests/Parser/ParserIntegrationTests.cs:179` read `Path.Combine(SamplesRoot, "Test.precept")` and parsed it; `samples/Test.precept` did not exist in the repo, so `ParseFile` threw `FileNotFoundException`.
- **Original symptom**: `System.IO.FileNotFoundException : Could not find file '/home/sfalik/source/repos/Precept/samples/Test.precept'.` at the test's `ParseFile(path)` call.

### F-LANG-COLL-13: `clear` and `notempty` lifted on `lookup of K to V`

- **Status**: ✅ **Fixed by Phase 4 W-J (2026-05-26)** after a `/lifecycle-2-design` pass surveyed comparator languages (Java `Map.clear`, C# `IDictionary.Clear`, Python `dict.clear`, Rust `HashMap::clear`, Swift `Dictionary.removeAll`, Kotlin `MutableMap.clear`, F# `Dictionary.Clear`, Go `clear(map)` added in 1.21 specifically to avoid forcing iteration). No surveyed language with per-key remove forbids the bulk operation; Precept's exclusion was anomalous. Bundled lift: `notempty` on lookup also lifted (was a parallel synonym restriction without independent rationale once `clear` lifted). Spec docs (`collection-types.md:85`, `:756`, `:902`, `precept-language-spec.md:1624`, `:1632`, `:1662`) updated. `Actions.cs ClearApplicable` adds `TypeKind.Lookup`; `Types.cs` Lookup TypeMeta drops the explicit `NotemptyApplicable: false` (default is `true`). Sample restore: `samples/shopping-cart.precept` ClearCart + Cancel events revert to canonical `clear LineItems / clear ItemQuantities / clear CartPromotions / clear GiftMessages`.
- **Discovered**: 2026-05-26 during Phase 4 W-A's precept-reviewer audit.
- **Affected**: any precept that wanted to empty a lookup field in one statement.
- **Pre-fix symptom**: `clear MyLookup` emitted `PRE0048 ScalarOperationOnCollection`. `notempty MyLookup` emitted `InvalidModifierForType`.

### BUG-012: Ordinal comparison between an ordered-choice field and a choice-literal cannot be proved

- **Status**: ✅ **Fixed by Phase 4 W-G (2026-05-26).** The proof engine's `TryDeclarationAttributeProof` ModifierRequirement arm now lifts a literal-side modifier from the sibling operand when the obligation site is a binary op (`ProofEngine.Strategies.cs`). When `Severity <= 2` emits `ModifierRequirement(PChoice, Ordered)` resolved against the literal `2`, the engine consults the sibling `Severity` operand and discharges from its declared `ordered` choice. The fix is symmetric: `Tier <= "Low"` discharges identically. Field-vs-field comparisons continue to discharge via the original DeclarationAttribute path. Unordered choice fields with literal comparisons still emit `PRE0112` (no order to inherit). Sample restore: `samples/it-helpdesk-ticket.precept` reverts the equality-cascade Priority computation to the canonical ordinal form (`if Severity <= 1 and Urgency <= 1 then "Critical" / else if Severity <= 2 or Urgency <= 2 then "High" / else if Severity <= 4 and Urgency <= 4 then "Medium" / else "Low"`). Tests in `test/Precept.Tests/ProofEngine/OrderedChoiceLiteralTests.cs` (4 new).
- **Discovered**: 2026-05-25 during refactor of `samples/it-helpdesk-ticket.precept` to give Severity, Urgency, and Priority an idiomatic ordered-choice shape.
- **Affected**: any precept that used `<`, `<=`, `>`, or `>=` between an `ordered` `choice of …` field and a bare literal from the same choice set.
- **Pre-fix symptom**: `precept_compile` emitted `PRE0112` UnprovedModifierRequirement on the literal operand. Field-vs-field comparisons of two same-set ordered choice fields proved cleanly via `DeclarationAttribute`.

### BUG-005: `lookup of K to money in '<Currency>'` and other qualified inner types

- **Status**: ✅ **Fully fixed by Phase 4 W-C (2026-05-26, F-LANG-COLL-06).** Qualified inner types in collections (`set of money in 'USD'`, `lookup of K to money in 'USD'`, `bag of quantity of 'mass'`, etc.) parse, type-check, and propagate qualifier metadata through to the proof engine. New `TypedElementType` DU in `SemanticIndex.cs` (variants: `TypedScalarElement`, `TypedChoiceElement` for W-G, `TypedQualifiedElement` with declared-qualifier metadata). Parser extended at `Parser.Types.cs ParseInnerTypeReference` to route inner types through `TryParseQualifiers`. Proof engine extended at `ProofEngine.Qualifiers.cs ResolveQualifierFromExpression` to inherit lookup-access result qualifiers from the lookup's element-type metadata. Sample restore: `event-venue-booking.precept` reverted from event-arg-carried-fee workaround back to canonical `set of string AddOnServices + lookup of string to money in 'USD' AddOnFees` paired pattern. Tests in `test/Precept.Tests/Parser/QualifiedInnerTypeTests.cs` (13 new).
- **Discovered**: 2026-05-24 during authoring of `samples/event-venue-booking.precept`
- **Affected**: any precept declaring a lookup with a qualified-money value type; bisected to specifically the `in '<Currency>'` qualifier on the value side
- **Original symptom** (before Phase 2): `precept_compile` returned "An error occurred invoking 'precept_compile'." with no PRE-code and no diagnostic. Phase 2 added a symmetric currency-qualifier rejection (clean PRE0105); Phase 4 added full support.

### BUG-002: `remove` on a lookup expects the value type instead of the key

- **Status**: ✅ **Fixed by Phase 4 W-B (2026-05-26)** — `TypeChecker.Expressions.Callables.cs` `CollectionValueAction` arm now branches on target type: when target is Lookup and action is Remove, the expected operand type is the lookup's `KeyType` rather than `ElementType`. `remove Items "specific-key"` and `remove Items Drop.Key` now type-check correctly. Sample cleanup landed in the same commit: `bill-of-materials-management.precept` and `shopping-cart.precept` reverted from the `put F K = 0` workaround to direct `remove F K`. Tests in `test/Precept.Tests/TypeChecker/LookupRemoveTests.cs`.
- **Discovered**: 2026-05-24 during authoring of `samples/bill-of-materials-management.precept`
- **Affected**: any precept that wants to delete a key from a `lookup of K to V` field
- **Pre-fix symptom**: `PRE0105 CollectionInnerTypeError — Expected a integer value, but 'Components' holds elements of type string` when the user wrote `remove Components RemoveComponent.PartNumber` where `Components` is `lookup of string to integer`. The type checker required the `remove` argument to match the lookup's value type rather than the key type.

### BUG-011: `timezone` and `time` fields with typed-constant default crash the compiler

- **Status**: ✅ **Fixed by Phase 2 (2026-05-25, commit `38712543`)** — already passing per the Phase 2 Step 2.2 verification (no crash; valid temporal defaults compile clean). Defence-in-depth wrapper added to `tools/Precept.Mcp/Tools/McpToolSafeInvoke.cs` ensures any future regression surfaces as a structured `McpToolInternalError` diagnostic instead of the raw `"An error occurred invoking ..."` MCP response. Scenario test in `test/Precept.Mcp.Tests/CompileTool_BugReproTests.cs`. **Post-fix sample cleanup pending**: restore the `timezone default` / `time default` declarations in `samples/global-meeting-scheduler.precept`.
- **Discovered**: 2026-05-24 during feature-gap-fill batch authoring `samples/global-meeting-scheduler.precept`
- **Affected**: any precept that declares `field X as timezone default '<literal>'` (e.g. `field DefaultTz as timezone default 'America/New_York'`) or `field X as time default '<literal>'` (e.g. `field DefaultStart as time default '09:00'`). Same fault family as BUG-003 (`period`), BUG-008 (`duration`), BUG-010 (`now() + duration`) — typed-constant temporal-literal handling.
- **Original symptom**: `precept_compile` returned "An error occurred invoking 'precept_compile'." with no diagnostic, no PRE-code, no message — identical to BUG-003/008/010. Stripping `default '<literal>'` (leaving the field as `optional`) let the same definition compile clean.
- **Workaround used**: in `samples/global-meeting-scheduler.precept`, `DefaultTimezone` is declared `timezone optional` and the host supplies it at construction via `Create(DefaultTimezone as timezone, ...)`; `DefaultStartTime` is similarly `time optional`. Cited inline with a `# BUG-011:` comment in the file header.
- **Post-fix cleanup**: in `samples/global-meeting-scheduler.precept`, restore `field DefaultTimezone as timezone default 'America/New_York'` and `field DefaultStartTime as time default '09:00'`; drop the `DefaultTimezone` and `DefaultStartTime` arguments from `Create(...)`; remove the `# BUG-011:` header comment.

### BUG-010: `now() + '<duration>'` expression crashes the compiler

- **Status**: ✅ **Fully fixed by Phase 3 (F-LANG-TEMP-01/02, context-aware temporal classification).** Phase 2 fixed the crash (commit `38712543`); Phase 3 fixed the type-inference quirk — `TemporalQuantityParser.Parse` now accepts an optional `TypeKind? expectedType` parameter, so `now() + '365 days'` in a duration arithmetic context correctly classifies the literal as a duration and compiles clean. Defense-in-depth wrapper in `McpToolSafeInvoke` remains for any future regression. Scenario test in `test/Precept.Mcp.Tests/CompileTool_BugReproTests.cs`.
- **Discovered**: 2026-05-24 during sweep batch 6 refactor of `samples/saas-user-provisioning.precept` and `samples/saas-license-management.precept`
- **Affected**: any precept that computes a future `instant` by adding a duration literal to `now()` — e.g. `set ExpirationDate = now() + '365 days'`, `now() + '30 days'`, `now() + '1 hour'`. The shape appears in `now() + '<duration literal>'` whether in a transition row body or an event ensure.
- **Original symptom** (before Phase 2): `precept_compile` returned "An error occurred invoking 'precept_compile'." with no diagnostic, no PRE-code, no message. Now returns structured PRE0058 (type-inference quirk; no crash).
- **Workaround used**: in batch 6, both `samples/saas-user-provisioning.precept` and `samples/saas-license-management.precept` carry the expiration date as an event argument supplied by the procurement/identity host. Cited inline with `# BUG-010:` comment in the file header.
- **Post-fix cleanup** (Phase 4 sample-restore): restore server-side temporal derivation in `samples/saas-user-provisioning.precept` and `samples/saas-license-management.precept`: replace the `LicenseExpirationDate`/`ExpirationDate`/`NewExpirationDate` event-arg path with `set ExpirationDate = now() + '<term>'` row bodies (e.g. `'365 days'` for annual license, `'30 days'` for grace). **Scope-check**: sweep `grep -rn "# BUG-010" samples/` to find every cite site. Remove `# BUG-010` comments.

### BUG-009: `precept_compile` MCP tool has an undocumented payload-size limit

- **Status**: ✅ **Fixed by Phase 2 (2026-05-25, commit `38712543`)** — not reproducible in-process. Synthetic ~20 KB precept compiles cleanly via direct `CompileTool.Compile()` call; if a stdio-framing limit exists in the MCP SDK, the new `McpToolSafeInvoke` wrapper catches it cleanly and returns a structured `McpToolInternalError` diagnostic rather than the raw `"An error occurred invoking ..."` MCP response. Scenario test in `test/Precept.Mcp.Tests/CompileTool_LargePayloadTests.cs`. **Wire-level re-verification needed** once the MCP server is rebuilt and the new wrapper is loaded — current session's MCP server is the pre-Phase-2 build.
- **Discovered**: 2026-05-24 during sweep batch 1 refactor of `samples/medical-prior-auth.precept` and `samples/prior-auth-appeal.precept`
- **Affected**: any precept file larger than ~12-15 KB sent to `precept_compile` via the MCP tool (originally reported).
- **Original symptom**: `precept_compile` returned "An error occurred invoking 'precept_compile'." with no diagnostic, no PRE-code, no message — identical symptom to BUG-003, BUG-005, BUG-008. Files in the 8-10 KB range compiled fine; files at ~14 KB crashed.
- **Workaround used**: in sweep batch 1, the agent validated `samples/medical-prior-auth.precept` (~14 KB) and `samples/prior-auth-appeal.precept` (~14 KB) via chunked compiles.
- **Post-fix cleanup**: no sample changes required — this is a tooling bug, not a DSL workaround. Once the MCP server is rebuilt with the Phase 2 wrapper, agents and `/precept-author` workflows revert to full-file `precept_compile` instead of chunked validation. No in-corpus `# BUG-009` citations exist to remove.

### BUG-008: `duration` field with typed-constant default crashes the compiler

- **Status**: ✅ **Fixed by Phase 2 (2026-05-25, commit `38712543`)** — already passing per the Phase 2 Step 2.2 verification (valid duration defaults like `'14 days'` compile clean). Defense-in-depth wrapper in `McpToolSafeInvoke` covers regression. Scenario test in `test/Precept.Mcp.Tests/CompileTool_BugReproTests.cs`. **Post-fix sample cleanup pending**: restore `duration default` in `samples/patient-care-plan-coordination.precept`.
- **Discovered**: 2026-05-24 during refactor of `samples/patient-care-plan-coordination.precept`
- **Affected**: any precept that declares `field X as duration default '<literal>'` (e.g. `field ReviewFrequencyDays as duration default '14 days'`, `field GracePeriod as duration default '4 hours'`). Same family as BUG-003 (`period` default).
- **Original symptom**: `precept_compile` returned an MCP-level error: `"An error occurred invoking 'precept_compile'."` with no diagnostic, no PRE-code, no message. Stripping `default '<literal>'` and leaving the field as `duration optional` let the same definition compile clean.
- **Workaround used**: in `samples/patient-care-plan-coordination.precept`, dropped the `ReviewFrequencyDays` field entirely. The host can carry review-frequency scheduling outside the precept. Cited inline with `# BUG-008:` comment.
- **Post-fix cleanup**: restore `field ReviewFrequencyDays as duration default '14 days'` (or whatever frequency the domain calls for) in `samples/patient-care-plan-coordination.precept`; if the field had rules / ensures that referenced it, restore those too. **Scope-check**: sweep `grep -rn "# BUG-008" samples/` to find every cite site. Remove `# BUG-008` comments.

### BUG-007: `precept_domains` MCP tool crashes on any scope

- **Status**: ✅ **Fixed by Phase 2 (2026-05-25, commit `38712543`)** — verified working for all 5 scopes (`currencies`, `units`, `dimensions`, `prefixes`, `temporal`, and no-arg) via direct call to `DomainsTool.Domains(scope)`. The earlier-reported crash appears to have been resolved by intervening work before Phase 2; Phase 2's `McpToolSafeInvoke` wrapper provides defense-in-depth against regression. Scenario test in `test/Precept.Mcp.Tests/DomainsTool_AllScopesTests.cs`. **Wire-level re-verification needed** once the MCP server is rebuilt.
- **Discovered**: 2026-05-24 during Phase 4 stateless authoring
- **Affected**: AI agents that follow the agent body's guidance to use `precept_domains` for currency / unit / dimension lookups; user-facing because the MCP tool is documented and surfaced.
- **Original symptom**: Calling `precept_domains` (with or without a scope argument) returned "An error occurred invoking 'precept_domains'." with no detail or error code. Other MCP tools worked fine.
- **Workaround used**: agents fell back to inline known values or to `precept_types` for type-system metadata. No in-corpus `# BUG-007` citations exist to remove.

### BUG-003: `period` field with typed-constant default crashes the compiler

- **Status**: ✅ **Fixed by Phase 2 (2026-05-25, commit `38712543`)** — already passing per the Phase 2 Step 2.2 verification (valid period defaults like `'1 year'` compile clean; invalid defaults like `'1 bogus'` emit a structured `InvalidTypedConstantContent` diagnostic). Defense-in-depth wrapper in `McpToolSafeInvoke` covers regression. Scenario test in `test/Precept.Mcp.Tests/CompileTool_BugReproTests.cs`. **Post-fix sample cleanup pending**: restore `period default` in `samples/equipment-lease-agreement.precept` and `samples/insurance-renewal-processing.precept`.
- **Discovered**: 2026-05-24 during authoring of `samples/equipment-lease-agreement.precept`
- **Affected**: any precept that declares `field X as period default '<literal>'` (e.g. `field GracePeriod as period default '10 days'`, `field RenewalPeriod as period default '1 year'`).
- **Original symptom**: `precept_compile` returned an MCP-level error: `"An error occurred invoking 'precept_compile'."` with no diagnostic, no message, no structured payload. Stripping the `default '<literal>'` clause (leaving the field as `period optional`) let the same definition compile clean.
- **Workaround used**: equipment-lease-agreement sample declares `GracePeriod` as `period optional` (the host supplies it) and cites BUG-003 in a comment.
- **Post-fix cleanup**: restore `field GracePeriod as period default '<literal>'` in `samples/equipment-lease-agreement.precept`; drop the constructor / event-arg path that supplies it from the host; restore equivalent `period default '<literal>'` form in `samples/insurance-renewal-processing.precept` if it was workarounded too. **Scope-check**: sweep `grep -rn "# BUG-003" samples/` to find every cite site. Remove `# BUG-003` comments.

### BUG-001: Proof engine ignored rule/ensure `when` guards for body narrowing

- **Discovered**: 2026-05-23 during authoring of `samples/patient-referral-management.precept`
- **Affected**: any precept using `rule X when Y is set because "..."` or `ensure X when Y is set because "..."` with optional-field narrowing in the body
- **Symptom**: `UnprovedPresenceRequirement` (PRE0116) on the body's field references even though the `when` guard provably established presence
- **Root cause**: `ProofEngine.Strategies.cs` — Strategy 3 (GuardInPath) and Strategy 4 (FlowNarrowing) enumerated `TransitionRowContext`, `StateHookContext`, `EventHandlerContext` in their guard-extraction switch, but treated `ConstraintContext` (rules + ensures) as the discard arm
- **Workaround used**: sample 29 originally scattered the invariant across an event ensure + per-state transition reject rows. Refactored to a single rule after the fix landed.
- **Fix complexity**: trivial (two switch extensions)
- **Priority**: was blocking sample quality — fixed before shipping any sample with a workaround comment
- **Fixed by**: `ProofEngine.Strategies.cs:275, :522` — added `ConstraintContext c => c.Constraint switch { RuleIdentity ri => semantics.Rules[ri.RuleIndex].Guard, EnsureIdentity ei => semantics.Ensures[ei.EnsureIndex].Guard, _ => null }`; tests in `test/Precept.Tests/ProofEnginePresenceTests.cs`; doc updates in `docs/compiler/proof-engine.md`
