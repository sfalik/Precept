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

### BUG-010: `now() + '<duration>'` expression crashes the compiler

- **Discovered**: 2026-05-24 during sweep batch 6 refactor of `samples/27-saas-user-provisioning.precept` and `samples/28-saas-license-management.precept`
- **Affected**: any precept that computes a future `instant` by adding a duration literal to `now()` — e.g. `set ExpirationDate = now() + '365 days'`, `now() + '30 days'`, `now() + '1 hour'`. The shape appears in `now() + '<duration literal>'` whether in a transition row body or an event ensure (line 164 of the original `28-saas-license-management.precept`: `on PurchaseLicenses ensure PurchaseLicenses.PurchaseDateAsInstant <= now() + '1 days'`).
- **Symptom**: `precept_compile` returns "An error occurred invoking 'precept_compile'." with no diagnostic, no PRE-code, no message — same shape as BUG-003/005/008/009. The minimal repro is just:
  ```precept
  precept Repro
  field X as instant optional
  state Requested initial
  state Active terminal
  event Create(K as integer) initial
  event Activate
  on Create -> set X = now()
  from Requested on Activate -> set X = now() + '365 days' -> transition Active
  ```
  Replacing `now() + '365 days'` with `Activate.NewDate` (event arg of type `instant`) lets the same definition compile clean.
- **Root cause** (suspected): typed-constant `'<duration>'` literal resolution when used as a `+` operand against an `instant`. Same code-path family as BUG-003 (`period` default) and BUG-008 (`duration` default): the literal parser, normalizer, or operator-overload resolution throws unhandled instead of producing a typed diagnostic. The `'<instant literal>'` form (e.g. `set X = '2026-01-01T00:00:00Z'`) also crashes with the same symptom — likely the same root.
- **Workaround used**: in batch 6, both `saas-user-provisioning.precept` (`ProvisionUser(LicenseExpirationDate as instant)`) and `saas-license-management.precept` (`MarkAsExpiring(ExpirationDate as instant)`, `RenewLicense(NewExpirationDate as instant, ...)`) carry the expiration date as an event argument supplied by the procurement/identity host. Cited inline with `# BUG-010:` comment in the file header. Acceptable for Constructor-pattern entities but blocks any precept that wants to derive a temporal field server-side from `now()`.
- **Fix complexity**: small — likely the same code path as BUG-003 and BUG-008. Fix all temporal-literal evaluation paths in one pass.
- **Priority**: quality bar — workaround is acceptable (push the date computation to the host) but the silent crash blocks idiomatic `set ExpiresAt = now() + '<term>'` shapes, which are natural for license expirations, grace periods, token TTLs, follow-up dates.
- **Repro**: see snippet above.

### BUG-009: `precept_compile` MCP tool has an undocumented payload-size limit

- **Discovered**: 2026-05-24 during sweep batch 1 refactor of `samples/medical-prior-auth.precept` and `samples/prior-auth-appeal.precept`
- **Affected**: any precept file larger than ~12-15 KB sent to `precept_compile` via the MCP tool. Affects authoring of large samples and especially refactoring of legacy ones (the whole-file operation that's central to a refactor).
- **Symptom**: `precept_compile` returns "An error occurred invoking 'precept_compile'." with no diagnostic, no PRE-code, no message — identical symptom to BUG-003, BUG-005, BUG-008. Files in the 8-10 KB range compile fine; files at ~14 KB crash. The threshold isn't documented anywhere. Agents have to fall back to chunked compile validation (splitting the file into pieces, compiling each piece, reassembling) — which catches structural errors but cannot validate cross-chunk references.
- **Root cause** (suspected): likely a payload-size limit on the MCP tool wrapper or an unhandled exception when the compiler operates on inputs above a certain size. Worth checking `tools/Precept.Mcp/Tools/CompileTool.cs` for buffer / serialization limits and the core compiler for any size-dependent code paths.
- **Workaround used**: in sweep batch 1, the agent validated `medical-prior-auth.precept` (~14 KB) and `prior-auth-appeal.precept` (~14 KB) via chunked compiles — split the file along state-machine boundaries, compile each chunk, infer structural soundness. Less robust than a full-file compile but unblocks the refactor.
- **Fix complexity**: small-to-medium — investigation needed to identify the limit and remove or raise it. May involve buffer sizes in the MCP stdio transport, JSON serialization limits, or compiler memory limits.
- **Priority**: quality bar approaching blocker — directly hampers the corpus-sweep workflow (every large legacy sample takes longer to refactor) and the agent body explicitly says "compile after every edit." That guidance breaks on large files.
- **Note**: this is the *fourth* "MCP-level crash with no PRE-code" symptom (BUGs 3, 5, 8, 9 all share the symptom shape). Worth a coordinated fix pass — the compiler / MCP wrapper has multiple failure modes that all surface identically and indistinguishably from a normal compile error to anything reading the response.

### BUG-008: `duration` field with typed-constant default crashes the compiler

- **Discovered**: 2026-05-24 during refactor of `samples/03-patient-care-plan-coordination.precept`
- **Affected**: any precept that declares `field X as duration default '<literal>'` (e.g. `field ReviewFrequencyDays as duration default '14 days'`, `field GracePeriod as duration default '4 hours'`). Same family as BUG-003 (`period` default) — the duration-default variant was not previously surfaced.
- **Symptom**: `precept_compile` returns an MCP-level error: `"An error occurred invoking 'precept_compile'."` with no diagnostic, no PRE-code, no message. Stripping `default '<literal>'` and leaving the field as `duration optional` lets the same definition compile clean. The crash reproduces on minimal inputs:
  ```precept
  precept Repro
  field A as integer default 0
  field ReviewFrequencyDays as duration default '14 days'
  state Draft initial
  state Done terminal
  event Create(X as integer) initial
  on Create -> set A = Create.X
  event Finish
  from Draft on Finish -> transition Done
  ```
- **Root cause** (suspected): typed-constant resolution for `duration` defaults — same fault class as BUG-003 (`period`). Probably the same code path: the literal parser, normalizer, or default-evaluation throws unhandled instead of producing a diagnostic. Worth fixing both temporal-type defaults in one pass.
- **Workaround used**: in `patient-care-plan-coordination.precept`, dropped the `ReviewFrequencyDays` field entirely. The host can carry review-frequency scheduling outside the precept. Cited inline with `# BUG-008:` comment.
- **Fix complexity**: small — likely the same code path as BUG-003. Convert unhandled exception in duration-default normalization to a typed diagnostic, or fix the underlying literal handling if structurally valid.
- **Priority**: quality bar — silent crash blocks any precept that wants a duration default, which is a natural shape for review frequencies, retry intervals, billing cycles, etc.

### BUG-007: `precept_domains` MCP tool crashes on any scope

- **Discovered**: 2026-05-24 during Phase 4 stateless authoring
- **Affected**: AI agents that follow the agent body's guidance to use `precept_domains` for currency / unit / dimension lookups; user-facing because the MCP tool is documented and surfaced
- **Symptom**: Calling `precept_domains` (with or without a scope argument: `currencies`, `units`, full catalog) returns "An error occurred invoking 'precept_domains'." with no detail or error code. Other MCP tools (`precept_compile`, `precept_patterns`, `precept_types`, etc.) work fine — issue is isolated to `precept_domains`.
- **Root cause** (unknown): server-side fault inside the `precept_domains` tool implementation. Could be a serialization issue, a missing catalog dependency, or a regression from recent catalog work.
- **Workaround used**: agents fall back to inline known values (ISO 4217 codes, UCUM unit categories) or to `precept_types` for type-system metadata. Works but loses the authoritative-source guarantee `precept_domains` was supposed to provide.
- **Fix complexity**: small-to-medium — needs investigation. Likely a bug in `tools/Precept.Mcp/Tools/DomainsTool.cs` or its DTO assembly.
- **Priority**: quality bar — agent body explicitly instructs authors to use `precept_domains`; the tool not working undermines that guidance.

### BUG-006: Proof engine doesn't combine guard narrowing with field-level `max` for arithmetic interval inference

- **Discovered**: 2026-05-24 during authoring of `samples/library-inter-library-loan.precept`
- **Affected**: any precept doing `set Counter = Counter + 1` in a row body where a sibling row above rejects when `Counter >= MaxField`, and `MaxField` carries a field-level `max N` modifier
- **Symptom**: `PRE0078 UnprovedOverflow` (or similar interval-containment failure) on `Counter + 1` even though the guard row above structurally rejects the case where `Counter >= MaxField` AND `MaxField` is bounded by `max N`. The proof engine narrows on each separately but does not combine them transitively.
- **Root cause** (suspected): same architectural class as BUG-001 / BUG-004 — the proof engine's interval-narrowing strategy doesn't compose guard-derived field bounds with field-modifier-derived bounds across rows in a transition table. Probably fixable by extending the same narrowing infrastructure that handles presence to handle numeric intervals.
- **Workaround used**: in `library-inter-library-loan.precept`, dropped the field-level `max 5` on `RenewalCount` and rely on a runtime `rule RenewalCount <= MaxRenewals` invariant. Cited inline with a `# BUG-006:` comment.
- **Fix complexity**: design-required — the narrowing combination is a real interval-inference extension, not a trivial switch.
- **Priority**: quality bar — workaround is a small downgrade (runtime check vs compile-time guarantee) but doesn't block samples.
- **Repro**: a precept with `field Counter as integer default 0 nonnegative` and `field MaxCount as integer max 5`, plus rows `from S on Inc when Counter >= MaxCount -> reject "..."` then `from S on Inc -> set Counter = Counter + 1 -> no transition`. The unguarded second row fails proof on `Counter + 1` containment.

### BUG-005: `lookup of K to money in '<Currency>'` crashes the compiler with no PRE-code

- **Discovered**: 2026-05-24 during authoring of `samples/event-venue-booking.precept`
- **Affected**: any precept declaring a lookup with a qualified-money value type; bisected to specifically the `in '<Currency>'` qualifier on the value side
- **Symptom**: `precept_compile` returns "An error occurred invoking 'precept_compile'." with no PRE-code and no diagnostic. Bisected:
  - `lookup of string to money` — compiles clean (unqualified money is fine)
  - `lookup of string to money in 'USD'` — crashes
  - `lookup of string to quantity of 'mass'` — emits a clean PRE0009 (different code path)
  The qualified-money value type slips through parsing and crashes a later compiler stage.
- **Root cause** (suspected): qualified-money value-type handling in the collection-typecheck pipeline. The clean PRE0009 on the parallel quantity case suggests there's a check site that catches dimension-qualified collection inner types but not currency-qualified ones.
- **Workaround used**: in `event-venue-booking.precept`, dropped the `lookup of K to money in 'USD'` for per-service fees; pass the fee back as an event-arg on the `RemoveService` event, accumulate the running total. Cited inline with `# BUG-005:` comment.
- **Fix complexity**: small-to-medium — investigate where dimension-qualified collection inner types get rejected and extend the check to currency-qualified ones.
- **Priority**: quality bar — workaround is awkward (event-arg fee instead of authoritative lookup) but unblocks samples. A clean PRE-code matching PRE0009 would at least surface the limitation.
- **Repro**:
  ```precept
  precept Repro
  field Fees as lookup of string to money in 'USD'
  state Draft initial terminal
  ```
  Yields the MCP-level crash, not a PRE-code.

### BUG-002: `remove` on a lookup expects the value type instead of the key

- **Discovered**: 2026-05-24 during authoring of `samples/bill-of-materials-management.precept`
- **Affected**: any precept that wants to delete a key from a `lookup of K to V` field; observed in `06-shopping-cart.precept` (worked around with `put k = 0`) and `bill-of-materials-management.precept` (same workaround)
- **Symptom**: `PRE0105 CollectionInnerTypeError — Expected a integer value, but 'Components' holds elements of type string` when the user writes `remove Components RemoveComponent.PartNumber` where `Components` is `lookup of string to integer` and `PartNumber` is `string`. The type checker requires the `remove` argument to match the lookup's **value** type (integer here) rather than the **key** type. Two issues: (1) the only sensible deletion semantics on a lookup is key-removal, so the value-typed argument doesn't even map to a meaningful operation; (2) the diagnostic message confusingly reports the key type as the "element type" of the lookup, masking the real expectation.
- **Root cause** (suspected): the `Remove` action's `CollectionValue` syntax dispatches the same element-type check it uses for set/list/bag. Lookup needs its own `RemoveByKey` shape (mirroring `Put`'s key/value pair) or a dedicated `removekey` action.
- **Workaround used**: shopping-cart sample (line 297-308) uses `put CartPromotions Key = 0.0` to zero-out the entry rather than delete; documents the workaround inline. BOM sample applies the same idiom: pairs the lookup with a `set of string` of part numbers as the source of membership truth, then `put Components PartNumber = 0` to zero out and `remove ComponentPartNumbers PartNumber` to drop membership. Comment cites BUG-002.
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

### BUG-003: `period` field with typed-constant default crashes the compiler

- **Discovered**: 2026-05-24 during authoring of `samples/equipment-lease-agreement.precept`
- **Affected**: any precept that declares `field X as period default '<literal>'` (e.g. `field GracePeriod as period default '10 days'`, `field RenewalPeriod as period default '1 year'`). The existing `samples/13-insurance-renewal-processing.precept` contains the same shape and presumably tripped this bug in earlier runs.
- **Symptom**: `precept_compile` returns an MCP-level error: `"An error occurred invoking 'precept_compile'."` with no diagnostic, no message, no structured payload. Stripping the `default '<literal>'` clause (leaving the field as `period optional`) lets the same definition compile clean. The crash reproduces on minimal inputs:
  ```precept
  precept Repro
  field G as period default '1 year'
  state Draft initial
  state Done terminal
  event E
  from Draft on E -> transition Done
  ```
- **Root cause** (suspected): typed-constant resolution for `period` defaults — the literal parser, normalizer (`NormalizingIso`), or default-evaluation path throws unhandled instead of producing a diagnostic. Other temporal types with defaults (`duration default '4 hours'`, `date default '2024-01-15'`, `instant default '...'`) should be sanity-checked for the same gap.
- **Workaround used**: equipment-lease-agreement sample declares `GracePeriod` as `period optional` (the host supplies it) and cites BUG-003 in a comment.
- **Fix complexity**: small — locate the unhandled exception in period-default normalization and convert it to a typed diagnostic (or fix the underlying parse if the literal is structurally fine).
- **Priority**: quality bar — the workaround is acceptable for the sample but the silent crash blocks any precept that wants a calendar-period default, which is a natural shape for renewal terms, grace periods, billing cycles, etc. Worth fixing before more samples need calendar-period defaults.
- **Repro**: see snippet above.

### BUG-004: Proof engine ignores event ensures for transition-row body narrowing

- **Discovered**: 2026-05-24 during authoring of `samples/equipment-lease-agreement.precept`
- **Affected**: any precept whose transition row body reads an optional field that an `on Event ensure Field is set` declaration has already proven present. Observed on the `from Draft on Quote` row reading `MonthlyPayment` after `on Quote ensure MonthlyPayment is set`.
- **Symptom**: `PRE0116 UnprovedPresenceRequirement — Cannot prove that 'MonthlyPayment' is present (used on event 'Quote' from state 'Draft') — guard with 'when MonthlyPayment is set', initialize it earlier, or make it required` even though the event ensure structurally rejects the event before the row body runs if the field is absent. This is the same shape as BUG-001 (which fixed body narrowing for rule/ensure `when` guards) but for event ensures narrowing transition-row bodies.
- **Root cause** (suspected): `ProofEngine.Strategies.cs` Strategy 3 / 4 enumerate guard sources for narrowing — the rule/ensure narrowing was added in BUG-001's fix, but event ensures are still not consulted as a narrowing source when evaluating transition-row body expressions for the same event.
- **Workaround used**: equipment-lease-agreement sample adds a redundant `when MonthlyPayment is set` guard to the row (line 135 area), with a comment citing BUG-004. The row remains the only Quote transition from Draft, so the guard does not shadow anything.
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

### BUG-005: `lookup of K to money in '<Currency>'` crashes the compiler

- **Discovered**: 2026-05-24 during authoring of `samples/event-venue-booking.precept`
- **Affected**: any precept that declares a lookup whose value type is a currency-qualified `money` (e.g. `field AddOnFees as lookup of string to money in 'USD'`, `field Prices as lookup of integer to money in 'EUR'`). Unqualified `money` as the value type compiles clean (`lookup of string to money`), and qualified-money outside lookups compiles clean — the crash is specific to the combination.
- **Symptom**: `precept_compile` returns an MCP-level error: `"An error occurred invoking 'precept_compile'."` with no diagnostic, no message, no structured payload. The same shape with `to integer`, `to decimal`, `to string`, `to boolean`, or `to money` (unqualified) all compile. The parallel case `lookup of string to quantity of 'mass'` produces a clean `PRE0009 Expected declaration keyword here, but found 'of'` parse error rather than crashing — suggesting the parser already rejects qualified quantity values in lookups but qualified money values slip through parsing and crash a downstream stage.
- **Root cause** (suspected): the parser accepts `lookup of K to money` and then mis-handles the trailing `in '<Currency>'` qualifier — either treating it as the start of a new declaration, throwing in qualifier resolution, or producing a malformed type the type-checker / proof engine cannot normalize. A clean diagnostic for unsupported qualified-money-in-lookup (or full support for it) would resolve.
- **Workaround used**: event-venue-booking sample drops the per-service-fee lookup entirely. Add-on services live only in `AddOnServices` (a `set of string`); `AddOnTotal` accumulates the running total. The `RemoveService` event requires `Fee as money in 'USD'` as an argument so the row can decrement `AddOnTotal` directly without reading per-service fees back out of a lookup. Comment cites BUG-005. The sample retains the lookup-with-membership *pattern shape* on `AddOnServices` (set membership guards every action) but skips the per-key value table because of this crash.
- **Fix complexity**: small-to-medium — either propagate the qualified-value type properly through lookup declaration, or emit a clean diagnostic if qualified-value lookups are intentionally unsupported. The matching `quantity of '...'` case already produces a clean PRE0009, so the parser change for money should mirror that.
- **Priority**: quality bar — silent crash blocks idiomatic per-key money/price lookups (fee tables, line-item pricing, per-currency balances). Worth fixing before pricing-heavy samples are written.
- **Repro**:
  ```precept
  precept Repro
  field Fees as lookup of string to money in 'USD'
  state Draft initial
  state Done terminal
  event Finish
  from Draft on Finish -> transition Done
  ```
  Yields MCP-level crash with no PRE-code.

## Fixed

### BUG-001: Proof engine ignored rule/ensure `when` guards for body narrowing

- **Discovered**: 2026-05-23 during authoring of `samples/29-patient-referral-management.precept`
- **Affected**: any precept using `rule X when Y is set because "..."` or `ensure X when Y is set because "..."` with optional-field narrowing in the body
- **Symptom**: `UnprovedPresenceRequirement` (PRE0116) on the body's field references even though the `when` guard provably established presence
- **Root cause**: `ProofEngine.Strategies.cs` — Strategy 3 (GuardInPath) and Strategy 4 (FlowNarrowing) enumerated `TransitionRowContext`, `StateHookContext`, `EventHandlerContext` in their guard-extraction switch, but treated `ConstraintContext` (rules + ensures) as the discard arm
- **Workaround used**: sample 29 originally scattered the invariant across an event ensure + per-state transition reject rows. Refactored to a single rule after the fix landed.
- **Fix complexity**: trivial (two switch extensions)
- **Priority**: was blocking sample quality — fixed before shipping any sample with a workaround comment
- **Fixed by**: `ProofEngine.Strategies.cs:275, :522` — added `ConstraintContext c => c.Constraint switch { RuleIdentity ri => semantics.Rules[ri.RuleIndex].Guard, EnsureIdentity ei => semantics.Ensures[ei.EnsureIndex].Guard, _ => null }`; tests in `test/Precept.Tests/ProofEnginePresenceTests.cs`; doc updates in `docs/compiler/proof-engine.md`
