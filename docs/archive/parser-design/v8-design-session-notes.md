# v8 Design Session Notes

**Date:** 2026-04-28
**Author:** Frank (Language Designer / Compiler Architect)

## What Changed from v7

1. **OmitDeclaration split from AccessMode** — v7 combined `modify`/`omit` under a single AccessMode disambiguation entry (`[TokenKind.Modify, TokenKind.Omit]`). v8 gives each its own entry and its own AST node. This is the single largest structural change.

2. **FieldTargetNode promoted to discriminated union** — v7 left this as a flat node. v8 requires abstract base + 3 sealed subtypes per catalog-system.md architectural rules.

3. **`ByLeadingToken[In]` count: 2 → 3** — Adding OmitDeclaration as a separate construct means `In` now dispatches to 3 constructs (StateEnsure, AccessMode, OmitDeclaration). Test data updated throughout.

4. **Total ConstructKinds: 11 → 12** — OmitDeclaration is the 12th kind. BuildNode switch explicitly has 12 arms.

5. **v7's incorrect test replaced** — `InScoped_RoutesToAccessMode_WhenOmitFollowsState` was wrong (asserted AccessModeNode for omit input). Replaced with `InScoped_RoutesToOmitDeclaration_WhenOmitFollowsState`.

6. **Per-slice Soup Nazi test specs** — Every slice now has a dedicated test specification section listing method names, descriptions, and edge cases.

7. **Slice sizing assessment** — Flagged 3 borderline slices (1.4, 2.1, 3.1) for George to confirm.

## Key Decisions Ratified

All decisions from `decisions.md` logged on 2026-04-28 were cross-checked and appear in v8:

| Decision | §1 Rationale | §2 Grammar | §3 AST | §6 Slices |
|----------|:---:|:---:|:---:|:---:|
| F12 vocabulary (modify/readonly/editable) | ✓ | ✓ | ✓ | ✓ (1.4, 4.3, 4.4) |
| OmitDeclaration separate construct | ✓ | ✓ | ✓ | ✓ (1.4, 2.1, 2.4, 4.4) |
| FieldTargetNode DU | ✓ | ✓ | ✓ | ✓ (2.1, 4.3) |
| Separate disambiguation entries | ✓ | ✓ | — | ✓ (1.4, 1.5, 4.4) |
| Slot sequences locked | ✓ | — | ✓ | ✓ (1.4, 2.6) |
| Sync recovery via leading tokens | ✓ | — | — | ✓ (5.4) |
| Token catalog state (George complete) | ✓ | — | — | ✓ (§4) |
| write all removed | ✓ | ✓ | — | ✓ (5.3 removed) |
| omit never guarded | ✓ | ✓ | ✓ | ✓ (1.4, 2.6, 4.4) |
| Proposal C deferred | — | — | — | ✓ (§8) |

## Open Items

- **Proposal C** — `when` as StateAction disambiguation token. Shane has not decided. Noted as DEFERRED in §8.
- **Borderline slices** — 1.4, 3.1 flagged for George's sizing confirmation. (2.1 resolved: formally split into 2.1a/2.1b.)

## George Review — Fixes Applied (2026-04-28)

George's review returned a BLOCKED verdict with 4 targeted fixes. All applied to v8:

1. **Fix 1 (Blocking):** Added `ParseOmit_WithPostFieldGuard_EmitsDiagnostic` test + full implementation to Slice 4.4 Soup Nazi spec. New diagnostic: `DiagnosticCode.OmitDoesNotSupportGuard`.
2. **Fix 2 (Blocking):** Specified stashed-guard + OmitDeclaration behavior in Slice 4.2 — parser emits same diagnostic when pre-consumed guard routes to OmitDeclaration. Added `ParseOmit_WithPreFieldStashedGuard_EmitsDiagnosticAndParses` test. Added `DiagnosticCode.cs` to PR 4 file inventory.
3. **Fix 3 (Clarity):** Replaced misleading sync description in Slice 5.4. `modify`/`omit` are NOT in `LeadingTokens` — recovery works at the enclosing `in` token level. Updated test name from `ErrorSync_SyncSetIncludesModifyAndOmit` to `ErrorSync_InScopedFailure_RecoversByResynchingToNextLeadingToken`.
4. **Fix 4 (Clarity):** Formalized Slice 2.1 split into 2.1a (base types + FieldTargetNode DU, ~80 lines) and 2.1b (declaration nodes, ~120 lines) with explicit dependency ordering. No longer borderline.

Decision verification matrix updated: "Sync tokens include modify/omit" corrected to "Sync recovery via leading tokens" (the mechanism is correct, the prior label was misleading). `OmitDoesNotSupportGuard` is a new implementation detail, not a new design decision — no matrix row needed.
