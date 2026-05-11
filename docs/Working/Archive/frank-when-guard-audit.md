# When-Guard Position Audit — Comprehensive Verdict

**Author:** Frank (Lead Architect)
**Date:** 2026-05-10T12:15:12-04:00
**Requested by:** Shane

---

## Audit Summary

Surveyed every construct in the Precept language that accepts an optional `when` guard. Found **one structural inconsistency** — confined to state/event ensures — plus **three broken sample files** that silently produce parse errors.

---

## Complete Construct Table

| Construct | Spec Grammar | Spec Prose | Catalog Attribute | Sample Usage | Parser Implementation | Consistent? |
|-----------|-------------|------------|-------------------|--------------|----------------------|-------------|
| **Rule declaration** | `rule BoolExpr ("when" BoolExpr)? because StringExpr` (line 801) — POST-expression | — | `SlotRuleExpression` terminated by `[When, Because]`; `SlotGuardClause` terminated by `[Because, Arrow]` | `rule Expr when Guard because "reason"` (insurance-claim:16, loan-application:16) | Slot-based: expression stops at `when`, guard parsed as next slot | ✅ |
| **Transition row** | `from StateTarget on Identifier ("when" BoolExpr)?` (line 826) — POST-event-name | Line 821 exempts `from...on`: "guard is inside the transition row after the event name" | `SlotGuardClause` in slots after `SlotEventTarget` | `from X on E when Guard -> ...` (40+ instances across all samples) | Slot-based: guard parsed after event target | ✅ |
| **State ensure** | `(in\|to\|from) StateTarget ensure BoolExpr ("when" BoolExpr)? because StringExpr` (line 855) — POST-expression | Line 821: "optional `when` guard between the state target and the verb" — PRE-VERB | `SupportsPreVerbWhenGuard: true`; no `SlotGuardClause` in slots | POST-expression form in samples (insurance-claim:28, loan-application:25) — **BROKEN** | PRE-VERB: guard parsed between anchor and disambiguation token | ❌ |
| **Event ensure** | `on Identifier ensure BoolExpr ("when" BoolExpr)? because StringExpr` (line 856) — POST-expression | Line 821 applies (pre-verb) | `SupportsPreVerbWhenGuard: true`; no `SlotGuardClause` in slots | POST-expression form in samples (insurance-claim:35) — **BROKEN** | PRE-VERB: guard parsed between anchor and disambiguation token | ❌ |
| **State action** | `(to\|from) StateTarget ("when" BoolExpr)?` (line 872) — POST-target, PRE-arrow | Line 821 applies (pre-verb) | `SupportsPreVerbWhenGuard: true` | No sample uses guarded state action | PRE-VERB: guard parsed between state target and `->` | ✅ |
| **Access mode** | `in StateTarget modify Field readonly ("when" BoolExpr)?` (lines 898–903) — POST-adjective | Line 821 does not apply (AccessMode has its own grammar section) | `SlotGuardClause` in slots after `SlotAccessModeKeyword`; NO `SupportsPreVerbWhenGuard` | `in State modify Field editable when Guard` (insurance-claim:26, loan-application:28) | Slot-based: guard parsed after access mode keyword | ✅ |
| **Omit declaration** | No guard — structural exclusion is unconditional (line 905–908) | — | No guard slot; `OmitDoesNotSupportGuard` diagnostic | — | — | ✅ (N/A) |
| **Event handler** | No `when` guard; supports post-action `ensure` (line 862–864) | — | `SupportsPostActionEnsure: true`; `EventHandlerDoesNotSupportGuard` diagnostic | — | — | ✅ (N/A) |

---

## Two Guard Mechanisms in the Parser

The parser implements two structurally distinct guard-parsing paths:

1. **Slot-based guard** — `GuardClause` appears in the construct's `Slots` array and is parsed in sequence with other slots. Used by: `RuleDeclaration` (post-expression), `TransitionRow` (post-event-name), `AccessMode` (post-adjective).

2. **Pre-verb guard** — Special parser path at `Parser.cs:280` triggered by `SupportsPreVerbWhenGuard: true`. Runs BETWEEN the anchor slot (state/event target) and the disambiguation token (verb). Used by: `StateEnsure`, `EventEnsure`, `StateAction`.

Both are correct implementations of `when Guard`. They differ only in WHERE the guard appears in the construct's surface syntax.

---

## The Inconsistency (State/Event Ensures)

### What agrees (PRE-VERB position):
- **Spec prose** (line 821): "optional `when` guard between the state target and the verb"
- **Spec examples** (lines 1696–1697): `in Open when Escalated ensure Priority >= 3 because "..."`
- **Toolchain plan** (Slice 5, line 615): explicitly cites spec line 813 for pre-verb position
- **Catalog** (`SupportsPreVerbWhenGuard: true`): encodes pre-verb
- **Parser**: implements pre-verb
- **Tests** (ParserSlice8Tests:72): `in Draft when Amount > 0 ensure Amount > 0 because "ok"` — passes

### What disagrees (POST-expression position):
- **Spec grammar** (lines 855–856): `ensure BoolExpr ("when" BoolExpr)? because StringExpr`
- **Sample files** (3 instances): `ensure Expr when Guard because "reason"`

### Impact of the inconsistency:
The 3 sample file lines that use post-expression `when` **do not parse correctly**. The parser emits `PRE0009: Expected declaration keyword here, but found 'when'`. This is silently masked because `ParserIntegrationTests` only checks that samples parse without crashing — it does **not** assert zero diagnostics. Verified: all 29 sample files pass the integration test, but the when-guard lines produce orphaned tokens.

---

## VERDICT

**Are all `when` guard positions consistent?** NO — but the inconsistency is narrow and well-understood.

The **parser, catalog, spec prose, spec examples, toolchain plan, and tests** all agree on PRE-VERB position for state/event ensures. The spec grammar rules at lines 855–856 and three sample files are the outliers.

The PRE-VERB position is correct. The spec grammar and samples need to be fixed.

---

## Inconsistencies Found

1. **Spec grammar lines 855–856** show POST-expression position (`ensure BoolExpr ("when" BoolExpr)?`) — contradicts spec prose, spec examples, catalog, parser, and tests.

2. **Sample files** use POST-expression position (3 lines) — produces silent parse errors:
   - `insurance-claim.precept:28`: `in Approved ensure DecisionNote is set when FraudFlag because "..."`
   - `insurance-claim.precept:35`: `on Submit ensure Submit.Amount <= 100000 when Submit.RequiresPoliceReport because "..."`
   - `loan-application.precept:25`: `in UnderReview ensure CreditScore >= 300 when DocumentsVerified because "..."`

3. **Integration test gap**: `ParserIntegrationTests` does not assert zero diagnostics on sample files — silently masks broken syntax.

4. **MCP tool stale build**: `precept_compile` via MCP rejects valid pre-verb `when` syntax that the test suite accepts — same stale-build class as BUG-006/BUG-051.

---

## Spec Line 821 Analysis

> "All three support an optional `when` guard between the state target and the verb (except `from ... on`, where the guard is inside the transition row after the event name)."

**Reconciliation with grammar at lines 855–856:**

Line 821 is correct. Lines 855–856 are wrong. The grammar rules should read:

```
(in|to|from) StateTarget ("when" BoolExpr)? ensure BoolExpr because StringExpr
on Identifier ("when" BoolExpr)? ensure BoolExpr because StringExpr
```

This matches the implemented behavior, the spec examples, and the natural reading of line 821.

Line 821's scope: "All three" refers to the `in`/`to`/`from` prepositions in the context of ensures and state actions — NOT access modes. AccessMode has its own grammar section (lines 890–909) with its own guard position (post-adjective), which is correctly implemented.

---

## Recommended Fixes

### 1. Fix spec grammar (lines 855–856)
Change from:
```
(in|to|from) StateTarget ensure BoolExpr ("when" BoolExpr)? because StringExpr
on Identifier ensure BoolExpr ("when" BoolExpr)? because StringExpr
```
To:
```
(in|to|from) StateTarget ("when" BoolExpr)? ensure BoolExpr because StringExpr
on Identifier ("when" BoolExpr)? ensure BoolExpr because StringExpr
```

### 2. Fix sample files (3 lines)
- `insurance-claim.precept:28`: `in Approved when FraudFlag ensure DecisionNote is set because "..."`
- `insurance-claim.precept:35`: `on Submit when Submit.RequiresPoliceReport ensure Submit.Amount <= 100000 because "..."`
- `loan-application.precept:25`: `in UnderReview when DocumentsVerified ensure CreditScore >= 300 because "..."`

### 3. Add diagnostic assertion to integration tests
`ParserIntegrationTests.SampleFile_ParsesWithoutException_AndReturnsManifest` should also assert `manifest.Diagnostics.Should().BeEmpty()` to prevent future silent breakage.

### 4. `SupportsPreVerbWhenGuard` naming
The name is **correct**. It accurately describes what it does: enables a `when` guard that appears before the verb (ensure, ->) in the construct's surface syntax. No rename needed.

### 5. Rebuild MCP server
The MCP tool is running a stale build that predates the pre-verb `when` guard parser support. Same class of issue as BUG-006/BUG-051.

---

## Summary of Guard Positions by Construct

| Position Pattern | Constructs | Mechanism |
|-----------------|------------|-----------|
| POST-expression (`Expr when Guard`) | Rule declaration | Slot-based (`SlotRuleExpression` terminated by `When`) |
| POST-event-name (`on Event when Guard`) | Transition row | Slot-based (`SlotGuardClause` after `SlotEventTarget`) |
| PRE-VERB (`StateTarget when Guard ensure/->`) | State ensure, Event ensure, State action | `SupportsPreVerbWhenGuard` special path |
| POST-adjective (`modify Field readonly when Guard`) | Access mode | Slot-based (`SlotGuardClause` after `SlotAccessModeKeyword`) |
| No guard | Omit declaration, Event handler | Explicit diagnostics if attempted |

All five positions are grammatically motivated and internally consistent. The only defect is the spec grammar text at lines 855–856 and three sample files that contradict the implemented and intended behavior.
