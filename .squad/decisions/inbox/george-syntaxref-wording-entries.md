# SyntaxReference Wording + New Entries

**Author:** George  
**Date:** 2026-05-17  
**Requested by:** Shane  
**Status:** Decision Record

---

## Decision

Update `src/Precept/Language/SyntaxReference.cs` and `src/Precept/Language/Quickstart.cs` as follows:

1. Fix the `Chaining comparisons` anti-pattern to describe the actual current compiler behavior: `precept_compile` emits `PRE0018`, while `PRE0010 NonAssociativeComparison` remains the intended future parser diagnostic.
2. Apply **Option B** for the construction-pattern gap note: keep a temporary PRE0092 note in the Constructor Pattern description and in the Hollow Draft anti-pattern description.
3. Add two new `CommonPattern` entries:
   - `Lifecycle-absent fields (omit)`
   - `Entry ensures as construction gate`
4. Add one new `AntiPattern` entry:
   - `Reading uninitialized field in construction row`
5. Update `QuickstartCatalog.ToolGuide` to report `19` common patterns and `7` anti-patterns.

---

## Verification

- `git log --oneline -5` on the local tip did not show a landed George-7 PRE0092 fix, and both constructor-shaped snippets still produce PRE0092/PRE0094 in `precept_compile`.
- `precept_compile` verifies the new `Lifecycle-absent fields (omit)` snippet cleanly once the field is cleared via a state-exit action (`from Investigating, WaitingOnCustomer -> clear CurrentHandler`) instead of a transition action into an omitted target state.
- `precept_compile` verifies the new `Entry ensures as construction gate` snippet cleanly.
- `precept_compile` accepts the bad `Counter = Counter + 1` construction-row snippet with **no PRE-code today**, so the anti-pattern wording must document a semantic trap plus current compiler gap rather than cite PRE0142/PRE0144 as live behavior for that exact example.
- `dotnet build test\Precept.Tests\Precept.Tests.csproj --nologo` succeeds after the string-table edits.
- `dotnet test test\Precept.Tests\ --no-build -q` still fails on the pre-existing `F5TempVerify` `UnsatisfiableInitialState` cases for `samples\parcel-locker-pickup.precept` and `samples\clinic-appointment-scheduling.precept`.

---

## Durable Guidance

- SyntaxReference wording must describe **current shipped behavior first**, then note the intended future diagnostic if the implementation has not caught up yet.
- For `omit` examples, use a state-exit clear when the target state omits the field; writing or clearing the field as part of the transition into the omitted state trips `PRE0131`.
- Pattern/anti-pattern guidance in `SyntaxReference` must stay honest about compiler gaps because MCP consumers treat these entries as authoritative authoring advice.
