# Access Mode Guard Design — Runtime Feasibility Assessment

**By:** George (Runtime Dev)  
**Date:** 2026-04-28  
**Context:** Design feasibility round for access mode guard extension. No implementation decisions made here — this is a runtime and type-checker impact audit. Frank holds final language design authority.  
**Input:** `.squad/decisions/inbox/george-access-mode-feasibility.md`  
**Questions addressed:** A1 (guarded `read`), A2 (guarded `omit`), A3 (Option B vocabulary `readable`/`writable`)

---

## The Short Version

A1 (guarded `read`) is feasible. The mechanism is genuinely symmetric to guarded `write` — same guard evaluation path, opposite direction. The type checker is greenfield, so there's no migration cost. Two catalog additions are required regardless of this decision.

A2 (guarded `omit`) is categorically infeasible within Precept's philosophy. It does not vary access level — it varies schema membership, which collapses the static-structure guarantee across every pipeline stage.

A3 (Option B vocabulary) is feasible with caveats. The parser is greenfield so there's no migration cost, but `TokenKind.Writable` picks up a three-role burden that needs explicit catalog documentation and careful slot disambiguation design.

One question is unresolved and must be decided before type-checker design begins: should an unguarded `write` + guarded `read` on the same (field, state) pair be treated as a conflict or as a valid refinement?

---

## Pre-Conditions: Catalog Gaps That Exist Regardless

Three catalog gaps exist today independent of any access mode guard decision. They need to be fixed for even the existing guarded `write` to be correctly modeled:

**1. `TypeChecker.cs` is fully greenfield.** The file is a single `throw new NotImplementedException()`. Every validation described in this assessment is net-new work. There is no existing type-checker logic to break or migrate.

**2. `Constructs.cs` `AccessMode` entry has no `SlotGuardClause` slot.** The current slot list is `[SlotOptStateTarget, SlotAccessModeKeyword, SlotFieldTarget]`. The guard slot was never added. Since the parser is also `NotImplementedException`, this is a gap rather than a regression — but it means adding guarded `read`, or correctly modeling the existing guarded `write`, both require adding `SlotGuardClause` (optional) to the `AccessMode` construct entry. This change needs to land regardless of A1's outcome.

**3. `AccessModifierMeta` has `IsPresent` and `IsWritable` booleans, but no `CanHaveGuard` flag.** The metadata-driven architecture requires that "which access modes can have a guard" be catalog metadata, not parser or checker logic. That flag is absent. It needs to exist regardless of whether guarded `read` is accepted — even with guarded `write` alone, the flag should be explicit in the catalog.

---

## A1: Guarded `read` — Feasible with Caveats

### What it means

`in StateTarget read FieldTarget when BoolExpr` on a `writable` field: when the guard is true, the field is downgraded to read-only for the duration of that state. When the guard is false, the field falls back to its baseline (`write`, since the field has the `writable` modifier).

This is the intended authoring pattern: `writable` establishes the field's default mutability; the access mode declaration refines it per state and per condition.

### Catalog changes needed

**`AccessModifierMeta.CanHaveGuard: bool`** (new field, belongs in pre-condition gap fix above)

| Mode | `CanHaveGuard` |
|------|----------------|
| `Write` | `true` |
| `Read` | `true` (with A1) |
| `Omit` | `false` (see A2) |

**`Constructs.cs` `AccessMode` entry** gains `SlotGuardClause` (optional) at position 3. The slot is identical in both `write` and `read` guarded forms — same structural position, same `WhenGuard?` carrier. This change also closes the pre-condition gap.

### Type checker changes needed

The type checker must build a static access mode table: one resolved mode per (field, state) pair. With guarded modes, the resolved entry becomes a discriminated value: either a fixed mode or a guarded pair `(true-mode, false-fallback)`. Four specific behaviors:

1. **Accept guarded `read` on `writable` fields.** Currently rule 4 forbids guards on `read`. Relaxing it requires the checker to verify `field.HasModifier(Writable)` before accepting the guard — a field-level check, not a state-level check.

2. **Reject guarded `read` on non-`writable` fields.** Non-`writable` fields are read-only by the D3 default. Guarding a read mode on them changes nothing and is an author error. Emit `GuardedReadOnReadOnlyField` (see Diagnostics below).

3. **Treat `writable` + guarded `read` as well-formed.** The checker must not treat "field has `writable`, state has guarded `read`" as a conflict — the guard selects between `read` (guard true) and `write` (fallback/guard false). This is the whole point of the feature.

4. **Extend the conflict check for guarded modes.** `ConflictingAccessModes` already exists in `DiagnosticCode` for the unguarded case. With guarded modes, the checker needs to handle two explicit mode declarations on the same (field, state) pair when at least one is guarded (see Edge Cases below for the unresolved question).

### Evaluator changes needed

The evaluator's `ResolveAccessMode(field, state, data)` function handles guarded `write` conceptually — the runtime executes it, just without a parser-built input yet. For guarded `write`: guard true → `write`, guard false → fallback. For guarded `read`: guard true → `read` (downgrade), guard false → fallback (`write`, for `writable` fields).

This is the same evaluation mechanism in the opposite direction. The evaluator already has guard evaluation infrastructure from transition guards and rule guards. No new evaluation infrastructure is needed — only the access mode resolver needs to understand both directions. Evaluation timing is identical to guarded `write`: the guard is evaluated at the moment a write is attempted, reading current field data at that instant.

### New diagnostics needed

**`GuardedReadOnReadOnlyField`** — Type stage, Error  
Template: `"Guarded 'read' on '{0}' has no effect — this field is read-only in all states. Add the 'writable' modifier to the field declaration to use a conditional access mode."`  
FixHint: `"Add 'writable' to the field declaration, or remove the guard"`  
Rationale: Without `writable`, the guard changes nothing. An error (not warning) because a guard on a no-op is almost certainly a misunderstanding of the model.

No new diagnostic code is needed for the conflict case — `ConflictingAccessModes` covers it. The checker should include guard context in the diagnostic span so the author can see which line is conflicting.

### Edge cases

**1. Self-referential guard.** `in S read F when F > 5` — when the evaluator evaluates the guard, it reads the current value of `F` to determine `F`'s own access mode. This is semantically self-referential and valid (the current value of `F` determines whether `F` can be changed). It's a footgun but not a correctness violation. Not proposed to block it — it's spec-coherent — but it should be documented.

**2. Multiple guarded `read` declarations on the same (field, state).** `in S read F when A` and `in S read F when B` — both guarded `read` on the same pair. When A or B is true, the result is `read`; when both are false, fallback to baseline. This is functionally equivalent to a single `in S read F when (A or B)` — the author should consolidate. The conflict checker should catch two explicit `read` declarations on the same (field, state) pair and emit `ConflictingAccessModes` even for guarded ones.

**3. `omit` + guarded `read` on the same pair.** A field `omit`ted in a state with `in S read F when Guard` on the same pair — caught by the existing `ConflictingAccessModes` check. No new case.

**4. Stateless precepts.** State-scoped guarded `read` doesn't apply. For stateless precepts, fields are read-only by default; use the `writable` modifier on field declarations to mark fields writable. No edge case.

### Verdict

**Feasible.** The mechanism is symmetric to guarded `write`. The type checker is greenfield (no migration). Required changes: `CanHaveGuard` flag on `AccessModifierMeta`, `SlotGuardClause` on the `AccessMode` construct entry, one new diagnostic code, and access mode resolver support for both guard directions. Neither change is novel — both extend existing patterns.

---

## A2: Guarded `omit` — Categorically Infeasible

### The structural presence problem

`AccessModifierMeta` for `Omit` has `IsPresent: false`. `omit` does not change access level — it removes the field from the state's data shape entirely. A field with `omit` in state S does not exist in S's schema. It is structurally absent.

Guarded `omit` would mean: *whether this field exists in this state's schema depends on a runtime condition evaluated against the field data.* This is not a change in access level for a field that is always present. It is conditional schema membership.

### What breaks — enumerated

**Type checker.** The type checker builds a static (field, state) access mode table. For `omit`, it enforces rule 6: `set` targeting an `omit`-mode field in the target state is a compile error. With guarded `omit`, the type checker cannot enforce rule 6 statically — it can only say "this field might be omitted at runtime." The guarantee that "set targeting omit is a compile error" collapses to a runtime fault. That is precisely what Precept is built to prevent.

**Graph analyzer.** The graph analyzer reasons about reachability, dominator paths, and state machine structure assuming field presence per state is fixed. It does not evaluate guards — it reasons statically. With guarded `omit`, the analyzer would need to track two schema variants per state (field present vs. absent), propagate them through transition edges, and account for both variants in every dominator and reachability proof. The graph analyzer has no architecture for conditional schema variants.

**Evaluator.** `omit` currently clears the field on state entry (rule 5) — a deterministic on-entry action. Guarded `omit` changes this: the evaluator would decide at state entry whether to clear the field, based on evaluating the guard. But the guard condition may reference field data that is about to be cleared — evaluation order becomes circular. Additionally, between events within a state, the guard's truth value can change. If the guard becomes true mid-state, the field retroactively "disappears" from the schema without a clear event. The evaluator has no model for a field transitioning from present to absent within a state.

**Language server.** The LS computes a per-state schema for completions, hover, and inline validation. With guarded `omit`, it cannot say "this field exists in state S" or "this field does not exist in state S." It can only say "this field might exist in state S." Every surface that consumes field presence (completions, hover, schema validation) would need a three-valued presence model: `AlwaysPresent`, `NeverPresent`, `ConditionallyPresent(Guard)`.

**MCP DTOs.** `precept_inspect` and `precept_fire` return the current state's field data as a typed structure. DTOs map to a fixed field list per state. With guarded `omit`, the DTO structure is no longer statically determinable from the state name alone. The response shape becomes dependent on runtime values, breaking the contract tool consumers rely on for schema extraction.

### The categorical distinction

Guarded `write` and guarded `read` vary the access level for a field that is always present. Guarded `omit` varies whether the field is present at all. This is not a difference in degree — it is a difference in kind. The first two are access control problems. The third is a type system problem: the type of the state (its schema) becomes a runtime-dependent function rather than a compile-time constant.

### Is there any technical path?

Yes, in principle: model fields as `Optional<T>` at the presence level (distinct from the `optional` modifier, which marks value optionality). A "schema-optional" field may or may not appear in a state's data, and consumers receive a `MaybePresent<T>` wrapper.

Cost: every pipeline stage that currently operates on a static (field, state) presence table would need a three-valued presence lattice. The type checker, graph analyzer, evaluator, LS, MCP, and integration surfaces all need to understand and propagate `ConditionallyPresent`. The graph analyzer's reachability proofs would need to fork on conditional presence paths.

Philosophy: **No.** Precept's core guarantee is that invalid configurations are structurally impossible — the structure is statically knowable at compile time. Conditional schema membership directly undermines this. The whole point of `omit` is to remove a field from a state's structure so that actions which target it become structurally impossible. Making that removal conditional means "impossible" becomes "possibly impossible depending on runtime data." That is conditional fault prevention — which is what rules and guards already do.

The path exists. The cost is high. The philosophy rejects it.

---

## A3: Option B Vocabulary (`readable`/`writable`/`omit`) — Feasible with Caveats

### Lexer and parser impact

**`readable`:** Does not exist. `TokenKind` has no `Readable` entry; `Tokens.cs` has no `TokenMeta` for it. Adding it requires: new `TokenKind.Readable` enum member, a new `TokenMeta` entry in `Tokens.cs` with string `"readable"` in the `Cat_Acc` category, and updating `VA_AllQuantifier` (currently `[Write, Read, Omit]`) to `[Writable, Readable, Omit]`. Clean addition, no collision.

**`writable`:** Already exists — `TokenKind.Writable`, string `"writable"`, registered in `Tokens.cs` as a field modifier in the `Cat_Decl` category alongside `optional`, `nonnegative`, etc. This token already drives the field-level baseline modifier: `field Amount as decimal writable`.

Reusing `writable` as an access mode keyword creates a **single token kind with three roles** in different syntactic positions:

| Position | Role |
|----------|------|
| `field Amount as decimal writable` | Field modifier in `SlotModifierList` |
| `in Draft writable Amount` | Access mode keyword after state target |
| `writable all` | Root-level access mode keyword (stateless form) |

The lexer cannot disambiguate these — it emits `TokenKind.Writable` in all cases. The parser must determine the role from structural context. All three positions are structurally distinct (after `in StateTarget`, inside field modifier list, or as construct leading token in a stateless precept), so the parser can handle this — but it must be explicitly designed for it.

**The root-level form concern.** Today the `AccessMode` construct uses `TokenKind.Write` as its `PrimaryLeadingToken` for the root-level form. With Option B, `writable all` uses `TokenKind.Writable`. This creates an asymmetry: the `AccessMode` construct would need two leading tokens registered (or split into `AccessMode` and `RootAccessMode` for the state-scoped vs. standalone forms). The catalog can express this but it adds complexity to the entry.

**`omit` stays unchanged.** No field-level `omit` modifier exists, so there's no collision. The asymmetry of `read → readable`, `write → writable`, `omit → omit` is slightly odd aesthetically but not a technical problem.

### Sample file scope

Every sample using `in State write Field` (approximately 15 files, ~20 lines) needs updating. No sample currently uses explicit `read` mode; no `readable` updates needed. Two samples use guarded write (`insurance-claim.precept`, `loan-application.precept`) — those lines change from `in State write Field when Guard` to `in State writable Field when Guard`. Low cost but non-zero.

### Recommended mitigations if Option B is chosen

1. Add `CanBeAccessModeKeyword: bool` to `TokenMeta` for `Writable` and `Readable`. This makes the three-role property explicit in the catalog — consumers don't hardcode it.

2. Consider whether the root-level form (`writable all`) warrants a separate construct entry to keep the `in`-led and standalone-led forms structurally clean. Not a blocker, but worth deciding before the parser design locks.

### Verdict

**Feasible with caveats.** Mechanically achievable — the parser is greenfield, `readable` is a clean new token, and `writable` disambiguation is solvable. The main caveat is the three-role burden on `TokenKind.Writable`. If Option A (`read`/`write`/`omit`) is chosen instead, there is zero migration cost and no parser complexity, at the cost of less visual connection between the field modifier and the access mode keyword.

---

## Unresolved Question — Requires Decision Before Type-Checker Design

**Should an unguarded `write` + guarded `read` on the same (field, state) pair be treated as a conflict (error) or a valid refinement (accepted)?**

The case for conflict: an explicit unguarded `write` on (field, state) asserts the field is always writable in that state. A guarded `read` on the same pair asserts it is sometimes read-only. These are incompatible assertions about the same pair — the unguarded write overrides the guard, making the guarded read unreachable.

The case for valid refinement: the `writable` field modifier plus the default D3 rule already establish `write` as the baseline. A guarded `read` declaration is an explicit override of that baseline for a specific condition. If an author also writes `in S write F` explicitly alongside `in S read F when Guard`, one could argue they're clarifying the fallback explicitly rather than relying on the default. The conflict interpretation would force authors to omit the explicit `write` declaration and rely on the default instead — which may feel implicit.

**My assessment:** This is a conflict. An explicit `in S write F` alongside `in S read F when Guard` is redundant at best and confusing at worst — it implies the author either forgot the default or is asserting something they believe is non-obvious. The `ConflictingAccessModes` diagnostic is the right response, with a message that explains the baseline handles the fallback. But this was not explicitly resolved in the design round by either me or Frank, and the type checker cannot be designed without a decision here.

This question should be put to Shane before implementation begins on the access mode conflict checker.

---

## Summary of Changes Required

| Item | What | Why |
|------|------|-----|
| `AccessModifierMeta.CanHaveGuard` | New `bool` field | Metadata-driven; replaces hardcoded guard-capable logic |
| `Constructs.cs` `AccessMode` entry | Add `SlotGuardClause` (optional, position 3) | Pre-existing gap; needed for guarded `write` and guarded `read` |
| `DiagnosticCode.GuardedReadOnReadOnlyField` | New error code | Detect no-op guard on non-`writable` field |
| Type checker conflict rules | Handle guarded modes in `ConflictingAccessModes` check | Multiple guarded `read` on same pair should be caught |
| Evaluator `ResolveAccessMode` | Support both guard directions | Same infrastructure, opposite semantic direction |
| Option B only: `TokenKind.Readable` | New token entry | Clean addition, `Cat_Acc` category |
| Option B only: `TokenMeta.CanBeAccessModeKeyword` | New `bool` on `Writable` and `Readable` | Document three-role token explicitly in catalog |
