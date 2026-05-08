### Slice 5 Regression Report — 2026-05-07

**Reporter:** Soup Nazi (Tester)
**Severity:** CRITICAL — Slice 5 implementation was entirely overwritten by Slice 6

#### Triage Result: George's 7 "Pre-Existing Failures" Were Stale Binaries

George reported 3163/3170 with "7 pre-existing failures, zero regressions." **These were NOT real failures.** They were caused by stale DLLs from a prior build state (the `--no-build` flag used a binary that didn't match HEAD). After a fresh `dotnet build` + `dotnet test`, the baseline is **3177/3177 clean** (26 new tests from this file bring total to 3196).

#### Root Cause: Slice 6 Overwrote Slice 5

Commit `fe358ef` ("feat: TypeChecker Slice 6 — structural validation") **completely replaced** commit `687d364`'s Slice 5 implementation instead of merging with it. Specifically:

| What Slice 5 added (`687d364`) | Slice 6 removed (`fe358ef`) |
|------|-------|
| `PopulateTransitionRows(manifest, ctx)` call in `Check()` | Replaced with `ValidateStructural(ctx)` |
| `PopulateEventHandlers(manifest, ctx)` call in `Check()` | Removed entirely |
| `ValidateModifiers(ctx)` call in `Check()` | Removed entirely |
| `NormalizeTransitionRow()` — ~120 lines | Deleted |
| `NormalizeEventHandler()` — ~50 lines | Deleted |
| `ResolveAction()` — ~150 lines | Deleted |
| `ResolveActionTarget()` — ~25 lines | Deleted |
| `ContainsErrorExpression/InAction()` — ~20 lines | Deleted |
| `TransitionRows: ctx.TransitionRows.ToImmutableArray()` | Reset to `ImmutableArray<TypedTransitionRow>.Empty` |
| `EventHandlers: ctx.EventHandlers.ToImmutableArray()` | Reset to `ImmutableArray<TypedEventHandler>.Empty` |
| `FieldReferences/StateReferences/EventReferences` from ctx | Reset to empty arrays |
| `System.Diagnostics` import | Removed |

This is a classic concurrent-agent overwrite — the Slice 6 agent worked from the Slice 4 base instead of the Slice 5 base.

#### Impact on Tests

- **19 of 26 new Slice 5 tests fail** — all because TransitionRows/EventHandlers are empty (stubs). These are TYPE B (known red, not suppressed).
- **7 of 26 pass** — these verify behavior handled by NameBinder or other stages (not dependent on PopulateTransitionRows).
- The tests are CORRECT — they document the Slice 5 contract per George's implementation notes. When Slice 5 code is restored, they should pass.

#### Fix Required

George (or the coordinator) must merge Slice 5's implementation back into the current HEAD. The Slice 5 code from `687d364` needs to coexist with Slice 6's additions (choice validation, postfix ops, structural validation). This is a merge task, not a rewrite.

#### Additionally: EventName.ArgName Bug

Even in `687d364`, the Slice 5 implementation has a bug where `EventName.ArgName` accessor expressions (e.g. `set Name = Submit.Label`) emit `UndeclaredField` for the event name. This is because `ResolveIdentifier` doesn't recognize event names as valid receivers. This bug will need fixing AFTER the Slice 5 code is restored.
