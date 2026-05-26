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

### F-LANG-COLL-13: `clear` is not valid on `lookup of K to V` per the v3 spec — language gap

- **Discovered**: 2026-05-26 during Phase 4 W-A's `precept-reviewer` audit. The reviewer flagged a catalog widening (adding `Lookup` to `ClearApplicable`) that contradicted the canonical spec. Investigation surfaced that the v3 spec explicitly excludes `clear MyLookup` but offers no idiomatic alternative for "empty the entire lookup."
- **Affected**: any precept that wants to empty a lookup field in one statement. The canonical spec excludes this in v3:
  - `docs/language/collection-types.md:85` — *"`clear` applies to `set`, `queue`, `stack`, `bag`, and `list` only — not log types (append-only) and not `lookup` (has per-key `remove`)"*
  - `docs/language/precept-language-spec.md:1662` — *"Not valid on `log of T`, `log of T by P`, or `lookup of K to V` (v3)"*
- **Symptom**: `clear MyLookup` emits `PRE0048 ScalarOperationOnCollection` per W-A's enforcement (wired 2026-05-26). The spec rationale ("has per-key `remove`") assumed an iteration mechanism that doesn't exist in v3 — Precept lacks a key-iteration primitive, so `remove Lookup Key` can only target known keys. Authors who want "empty the lookup" have no v3 idiom.
- **Affected samples**: `samples/shopping-cart.precept` ClearCart event (which used to call `clear ItemQuantities` and `clear CartPromotions`). Soft-clear workaround landed in W-B: the ClearCart event clears the controlling `LineItems` set and resets totals; the lookup entries become orphaned but harmless (all read paths flow through `LineItems contains` guards, so stale entries are invisible to event handlers). The sample carries an inline comment citing this entry.
- **Workaround used**: soft-clear via the controlling set/list (when one exists). Generalizes only when authors maintain a parallel `set of K` membership tracker alongside the lookup — common pattern but doesn't scale to every shape.
- **Root cause**: deliberate v3 design exclusion. The "per-key remove" rationale didn't account for missing iteration primitives.
- **Fix complexity**: design-required — either (a) lift the exclusion (allow `clear MyLookup` with explicit "drop all keys" semantics) or (b) introduce a lookup-iteration primitive that lets authors express "for each key in lookup, remove it." Both are language-surface decisions requiring `/lifecycle-2-design`.
- **Priority**: quality bar — affects the cart-reset idiom and any similar "empty this lookup" workflow. The soft-clear workaround is semantically incomplete (stale data orphaned but invisible).
- **Target phase**: deferred pending `/lifecycle-2-design` pass; not assigned to Phase 4 or 5.

### BUG-013: `ParserIntegrationTests.TestSample_EventDeclaration_BindsInitialToCreateOnly` references missing `samples/Test.precept`

- **Discovered**: 2026-05-25 during Phase 2 Step 2.7 verification of the test suite
- **Affected**: `test/Precept.Tests/Parser/ParserIntegrationTests.cs:179` reads `Path.Combine(SamplesRoot, "Test.precept")` and parses it; `samples/Test.precept` does not exist in the repo, so `ParseFile` throws `FileNotFoundException`. Single sample-side test failure surfaced by `dotnet test`.
- **Symptom**: `System.IO.FileNotFoundException : Could not find file '/home/sfalik/source/repos/Precept/samples/Test.precept'.` at the test's `ParseFile(path)` call. The 1 failing test out of 6108 in `Precept.Tests` after Phase 2.
- **Root cause** (suspected): the test was authored when `Test.precept` existed (parallel sample-authoring sessions historically added/removed a scratch `Test.precept` fixture); the file was removed without updating the test. Either the test should embed its expected source inline (no filesystem dependency), or the sample file should be re-added under a stable name.
- **Workaround used**: none — the test simply fails. Out of Phase 2 scope per the plan ("Sample-side failures, if any remain, are routed to bugs.md").
- **Fix complexity**: trivial — either rewrite the test with inline `precept ...` source, or re-add `samples/Test.precept` with the minimal multi-event-with-`initial` shape the test asserts (`event create, start, stop, reset` with `create initial`).
- **Priority**: quality bar — exactly 1 test failure noise in an otherwise green baseline. Quick to clear.

### BUG-012: Ordinal comparison between an ordered-choice field and a choice-literal cannot be proved

- **Discovered**: 2026-05-25 during refactor of `samples/it-helpdesk-ticket.precept` to give Severity, Urgency, and Priority an idiomatic ordered-choice shape.
- **Affected**: any precept that uses `<`, `<=`, `>`, or `>=` between an `ordered` `choice of …` field and a bare literal from the same choice set. Both flavors reproduce: `choice of integer(...)` (e.g. `Severity <= 2`) and `choice of string(...)` (e.g. `Tier <= "Medium"`). Field-vs-field comparisons of two same-set ordered choice fields prove cleanly via `DeclarationAttribute`.
- **Symptom**: `precept_compile` emits `PRE0112` UnprovedModifierRequirement: `Cannot prove that '<literal>' satisfies the required modifier 'Ordered' (used in the computed expression for field '<F>')`. The proof obligation `Both choice operands must be declared ordered` lists as `Unresolved`. The proof engine appears to require both operands to carry the `Ordered` modifier directly on their declaration; a literal that lexically belongs to a same-set ordered choice declaration is not lifted to "ordered" by virtue of the field on the other side of the operator.
- **Minimal repro**:
  ```precept
  precept Repro
  field Severity as choice of integer(1, 2, 3, 4, 5) ordered default 3
  field IsCritical as boolean <- Severity <= 2
  ```
  Equivalent string-set repro:
  ```precept
  precept Repro
  field Tier as choice of string("Low", "Medium", "High") ordered default "Low"
  field IsLow as boolean <- Tier <= "Low"
  ```
  Field-vs-field (proves clean — shows the gap is literal-side, not the operator):
  ```precept
  precept Repro
  field Severity as choice of integer(1, 2, 3, 4, 5) ordered default 3
  field Threshold as choice of integer(1, 2, 3, 4, 5) ordered default 1
  field R as boolean <- Severity <= Threshold
  ```
- **Root cause** (suspected): the proof obligation `Both choice operands must be declared ordered` (defined on `ChoiceLessThanChoice` etc. in the operations catalog) is resolved by `DeclarationAttribute` strategy only — it walks operand declarations looking for the `ordered` modifier. A choice literal has no field declaration to inspect, so the obligation falls through to `Unresolved` and reports against the literal text. The fix is either to (a) lift the `Ordered` modifier from the contextual choice-set type when one operand is a literal and the other a typed ordered-choice field, or (b) add a typed-literal strategy that infers the modifier from the operand's expected type.
- **Workaround used**: in `samples/it-helpdesk-ticket.precept`, the Priority computed field was rewritten as an equality-based cascade (`Severity == 1 or Severity == 2 or …`) instead of the more natural ordinal form (`Severity <= 2 or …`). The original mapping shape (`if Severity <= 1 and Urgency <= 1 then …`) is recorded in the file header docstring; equivalence to the cascade form is verified by case analysis. No BUG-012 inline citation was added because the cascade is correct as written — but it is more verbose than the ordinal form would be.
- **Post-fix cleanup**: in `samples/it-helpdesk-ticket.precept`, replace the equality-based cascade with the ordinal form documented in the original refactor brief — `if Severity <= 1 and Urgency <= 1 then "Critical" / else if Severity <= 2 or Urgency <= 2 then "High" / else if Severity <= 4 and Urgency <= 4 then "Medium" / else "Low"`. Update the field-level comment to drop the reference to this bug entry.
- **Fix complexity**: small to medium — extends one proof strategy or adds a new typed-literal strategy. Self-contained to the proof engine; no language surface change.
- **Priority**: quality bar — the workaround is correct but verbose, and the limitation forecloses the natural idiom for tier/rank fields where ordinal comparisons against thresholds are the obvious shape. Documented in `docs/language/primitive-types.md` § Type Operator Surface Summary that the `choice` row supports ordinal comparison; consumers will reach for it.

### BUG-006: Proof engine doesn't combine guard narrowing with field-level `max` for arithmetic interval inference

- **Discovered**: 2026-05-24 during authoring of `samples/library-inter-library-loan.precept`
- **Affected**: any precept doing `set Counter = Counter + 1` in a row body where a sibling row above rejects when `Counter >= MaxField`, and `MaxField` carries a field-level `max N` modifier
- **Symptom**: `PRE0078 UnprovedOverflow` (or similar interval-containment failure) on `Counter + 1` even though the guard row above structurally rejects the case where `Counter >= MaxField` AND `MaxField` is bounded by `max N`. The proof engine narrows on each separately but does not combine them transitively.
- **Root cause** (suspected): same architectural class as BUG-001 / BUG-004 — the proof engine's interval-narrowing strategy doesn't compose guard-derived field bounds with field-modifier-derived bounds across rows in a transition table. Probably fixable by extending the same narrowing infrastructure that handles presence to handle numeric intervals.
- **Workaround used**: in `samples/library-inter-library-loan.precept`, dropped the field-level `max 5` on `RenewalCount` and rely on a runtime `rule RenewalCount <= MaxRenewals` invariant. Cited inline with a `# BUG-006:` comment.
- **Post-fix cleanup**: restore `max 5` on `RenewalCount` in `samples/library-inter-library-loan.precept` (or whatever the structural bound should be); decide whether to keep the runtime `rule RenewalCount <= MaxRenewals` (defense-in-depth) or drop it (the field bound + reject row are now provably sufficient). **Scope-check**: sweep `grep -rn "# BUG-006" samples/` to find every cite site. Remove `# BUG-006` comments.
- **Fix complexity**: design-required — the narrowing combination is a real interval-inference extension, not a trivial switch.
- **Priority**: quality bar — workaround is a small downgrade (runtime check vs compile-time guarantee) but doesn't block samples.
- **Repro**: a precept with `field Counter as integer default 0 nonnegative` and `field MaxCount as integer max 5`, plus rows `from S on Inc when Counter >= MaxCount -> reject "..."` then `from S on Inc -> set Counter = Counter + 1 -> no transition`. The unguarded second row fails proof on `Counter + 1` containment.

### BUG-005: `lookup of K to money in '<Currency>'` — full qualified-inner-type support pending Phase 4

- **Status**: ⚠️ **Symptom fixed by Phase 2 (2026-05-25); full support deferred to Phase 4 (F-LANG-COLL-06).** The crash is gone — `lookup of K to money in 'USD'` now emits a clean `CollectionInnerTypeError` (PRE0105) diagnostic instead of crashing the MCP server. The fix in `src/Precept/Pipeline/Parser.Types.cs` extends the dimension-qualifier rejection (which already handled `lookup of K to quantity of 'mass'`) to also catch currency-qualified money. **Full support for qualified inner types in lookups** — making `lookup of string to money in 'USD'` actually work as documented — lands in Phase 4 alongside the other collection completeness work. Scenario test in `test/Precept.Mcp.Tests/CompileTool_BugReproTests.cs`. **This entry stays Active** until Phase 4 ships the full feature.
- **Discovered**: 2026-05-24 during authoring of `samples/event-venue-booking.precept`
- **Affected**: any precept declaring a lookup with a qualified-money value type; bisected to specifically the `in '<Currency>'` qualifier on the value side
- **Original symptom** (before Phase 2): `precept_compile` returned "An error occurred invoking 'precept_compile'." with no PRE-code and no diagnostic. Bisected:
  - `lookup of string to money` — compiles clean (unqualified money is fine)
  - `lookup of string to money in 'USD'` — crashed (now emits clean PRE0105)
  - `lookup of string to quantity of 'mass'` — emits a clean PRE0105 (was already covered)
- **Root cause** (now understood): qualified inner types are unsupported in lookup value position. Phase 2 added the symmetric currency-qualifier rejection; Phase 4 will add full support (F-LANG-COLL-06).
- **Workaround used**: in `samples/event-venue-booking.precept`, dropped the `lookup of K to money in 'USD'` for per-service fees; per-add-on services live only in `AddOnServices` (a `set of string`); `AddOnTotal` accumulates the running total. The `RemoveService` event carries `Fee as money in 'USD'` as an arg so the row can decrement `AddOnTotal` directly without reading per-service fees back out of a lookup. The sample retains the lookup-with-membership *pattern shape* on `AddOnServices` (set membership guards every action) but skips the per-key value table because of this restriction. Cited inline with `# BUG-005:` comment.
- **Post-fix cleanup**: restore `field AddOnFees as lookup of string to money in 'USD'` in `samples/event-venue-booking.precept`; drop the `Fee as money in 'USD'` arg from `RemoveService`; have the row read the fee back from `AddOnFees` instead. Remove `# BUG-005` comment.
- **Fix complexity**: small-to-medium — Phase 4 work; extend the qualified-inner-type plumbing through the type checker, proof engine, and runtime evaluator.
- **Priority**: quality bar — workaround is awkward (event-arg fee instead of authoritative lookup) but unblocks samples.

### BUG-002: `remove` on a lookup expects the value type instead of the key

- **Status**: ✅ **Fixed by Phase 4 W-B (2026-05-26)** — `TypeChecker.Expressions.Callables.cs` `CollectionValueAction` arm now branches on target type: when target is Lookup and action is Remove, the expected operand type is the lookup's `KeyType` rather than `ElementType`. `remove Items "specific-key"` and `remove Items Drop.Key` now type-check correctly. Sample cleanup landed in the same commit: `bill-of-materials-management.precept` and `shopping-cart.precept` reverted from the `put F K = 0` workaround to direct `remove F K`. Tests in `test/Precept.Tests/TypeChecker/LookupRemoveTests.cs`.
- **Discovered**: 2026-05-24 during authoring of `samples/bill-of-materials-management.precept`
- **Affected**: any precept that wants to delete a key from a `lookup of K to V` field; observed in `samples/shopping-cart.precept` (worked around with `put Key = 0`) and `samples/bill-of-materials-management.precept` (same workaround)
- **Symptom**: `PRE0105 CollectionInnerTypeError — Expected a integer value, but 'Components' holds elements of type string` when the user writes `remove Components RemoveComponent.PartNumber` where `Components` is `lookup of string to integer` and `PartNumber` is `string`. The type checker requires the `remove` argument to match the lookup's **value** type (integer here) rather than the **key** type. Two issues: (1) the only sensible deletion semantics on a lookup is key-removal, so the value-typed argument doesn't even map to a meaningful operation; (2) the diagnostic message confusingly reports the key type as the "element type" of the lookup, masking the real expectation.
- **Root cause** (suspected): the `Remove` action's `CollectionValue` syntax dispatches the same element-type check it uses for set/list/bag. Lookup needs its own `RemoveByKey` shape (mirroring `Put`'s key/value pair) or a dedicated `removekey` action.
- **Workaround used**: shopping-cart sample uses `put CartPromotions Key = 0.0` to zero-out the entry rather than delete; documents the workaround inline. BOM sample applies the same idiom: pairs the lookup with a `set of string` of part numbers as the source of membership truth, then `put Components PartNumber = 0` to zero out and `remove ComponentPartNumbers PartNumber` to drop membership. Comment cites BUG-002.
- **Post-fix cleanup**: in `samples/shopping-cart.precept` and `samples/bill-of-materials-management.precept`, replace each `put Lookup Key = 0` (or `= 0.0`, or `= ""`) workaround with `remove Lookup Key`; drop the parallel `set of K` field (e.g. `ComponentPartNumbers`) where it was added purely as a membership tracker for the workaround. **Scope-check**: sweep `grep -rn "# BUG-002" samples/` to find every cite site before declaring the cleanup done. Remove `# BUG-002` comments.
- **Fix complexity**: small — extend the action catalog with a key-removal shape for lookups, or change `remove` dispatch on lookup to expect the key type
- **Priority**: quality bar — the workaround is awkward (orphan zero-valued entries) but doesn't block samples from compiling. Worth fixing before lookup becomes more visible in tutorials.
- **Repro**:
  ```precept
  precept Repro
  field Items as lookup of string to integer
  state Draft initial
  state Done terminal
  event Drop(Key as string notempty)
  from Draft on Drop -> remove Items Drop.Key -> no transition
  event Finish
  from Draft on Finish -> transition Done
  ```
  Yields `PRE0105 Expected a integer value, but 'Items' holds elements of type string`.

### BUG-004: Proof engine ignores event ensures for transition-row body narrowing

- **Discovered**: 2026-05-24 during authoring of `samples/equipment-lease-agreement.precept`
- **Affected**: any precept whose transition row body reads an optional field that an `on Event ensure Field is set` declaration has already proven present. Observed on the `from Draft on Quote` row reading `MonthlyPayment` after `on Quote ensure MonthlyPayment is set`.
- **Symptom**: `PRE0116 UnprovedPresenceRequirement — Cannot prove that 'MonthlyPayment' is present (used on event 'Quote' from state 'Draft') — guard with 'when MonthlyPayment is set', initialize it earlier, or make it required` even though the event ensure structurally rejects the event before the row body runs if the field is absent. This is the same shape as BUG-001 (which fixed body narrowing for rule/ensure `when` guards) but for event ensures narrowing transition-row bodies.
- **Root cause** (suspected): `ProofEngine.Strategies.cs` Strategy 3 / 4 enumerate guard sources for narrowing — the rule/ensure narrowing was added in BUG-001's fix, but event ensures are still not consulted as a narrowing source when evaluating transition-row body expressions for the same event.
- **Workaround used**: equipment-lease-agreement sample adds a redundant `when MonthlyPayment is set` guard to the row (line 135 area), with a comment citing BUG-004. The row remains the only Quote transition from Draft, so the guard does not shadow anything.
- **Post-fix cleanup**: remove the redundant `when X is set` guard from every row carrying a `# BUG-004` cite — start with `samples/equipment-lease-agreement.precept`. **Scope-check**: sweep `grep -rn "# BUG-004" samples/` to find every cite site. After deletion, sweep more broadly for the *pattern* (`when X is set` guards on rows whose event has a matching `on Event ensure X is set`) — those become removable noise once the fix lands, even if they weren't cited.
- **Fix complexity**: trivial-to-small — extend the guard-extraction switch in `ProofEngine.Strategies.cs` so event ensures on the row's event contribute their `is set` predicates to the row body's narrowing context. Mirrors the BUG-001 fix shape.
- **Priority**: quality bar — workaround is mechanical (one extra guard per affected row), but it adds noise to every Free-Construction row that reads an event-ensure-guaranteed field. Worth fixing because Free-Construction is becoming the canonical pattern.
- **Repro**:
  ```precept
  precept Repro
  field Amount as money in 'USD' optional
  state Draft initial
  state Done terminal
  event Submit
  on Submit ensure Amount is set because "Required"
  from Draft on Submit -> set Amount = Amount + '1.00 USD' -> transition Done
  ```
  Yields PRE0116 on the right-hand `Amount` read even though `on Submit ensure Amount is set` provably establishes presence before the row body runs.

### F-LANG-BIZ-08: Discrete equality narrowing for `choice of` fields is not implemented

- **Discovered**: 2026-05-25 during Phase 3 Step 3.3f verification
- **Affected**: any precept rule or guard that uses `choice == "literal"` and expects the proof engine to narrow the choice field to that value in the branch body
- **Symptom**: `when Priority == "High"` does not narrow `Priority` to `"High"` inside the branch. The proof engine's narrowing strategies (Strategy 3 GuardInPath, Strategy 4 FlowNarrowing in `ProofEngine.Strategies.cs`) are numeric-only — no `BuildNarrowedDiscreteValues` analog exists for choice literals. The equality operator (`ChoiceEqualsChoice → Boolean`) resolves cleanly, but no narrowing strategy consumes the result.
- **Root cause**: `ProofEngine.Strategies.cs` narrowing infrastructure handles numeric interval narrowing via `BuildNarrowedIntervals`. There is no corresponding mechanism to recognize `when X == "literal"` for a choice field and narrow `X`'s set of possible values to `{"literal"}` in the guard-true branch.
- **Workaround used**: none needed for current samples — choice equality guards work as boolean conditions, they just don't enable further proof narrowing (e.g., proving that a subsequent use of the same field satisfies a constraint).
- **Fix complexity**: design-required — requires (a) a new proof strategy that recognizes `choiceField == literal` guards and narrows the field's discrete value set, and (b) a discrete-value interval representation (analogous to `BuildNarrowedIntervals` for scalar types). Non-trivial proof-engine extension.
- **Priority**: quality bar — narrowing for choice fields improves proof-engine completeness and reduces false-positive `UnprovedModifierRequirement` diagnostics in complex guard chains.
- **Target phase**: Phase 5 (proof engine satisfiability)

## Fixed

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
