# Grammar Doc Comprehensive Review — 2026-05-10

**Reviewer:** Frank (Lead/Architect)  
**Document:** `docs/language/precept-grammar.md`  
**Ground truth:** `src/Precept/Language/Constructs.cs`, `ConstructKind.cs`, `ConstructSlot.cs`, `ExpressionForms.cs`, `DisambiguationEntry.cs`, `samples/`  
**Date:** 2026-05-10T15:38:30-04:00

---

## § Executive Summary

| Severity | Count |
|----------|-------|
| **Error** (factually wrong, misleads implementors) | 8 |
| **Warning** (incomplete/inconsistent, causes confusion) | 6 |
| **Minor** (cosmetic, terminology, non-blocking) | 3 |
| **Total** | 17 |

**Carry-forward from previous audit:** 6 issues (the `when` guard gaps on StateEnsure anatomy, family details, and StateAction anatomy — all confirmed here as Issues 1–6 below).

**New issues found:** 11 additional issues not in the previous audit.

**Overall assessment:** The grammar doc is structurally sound in its design-explanation sections (§1, §7, §8 body) but has **significant factual errors** in its construct anatomy diagrams and family detail sections. The errors cluster around:
1. Missing pre-verb `when` guard slots (6 locations — known from prior audit)
2. Incorrect computed-field anatomy diagram (slot order and annotation errors)
3. Stale counts (13 vs 14 expression forms)
4. Inconsistent BecauseClause optionality claims
5. Stale invariant quick-reference text
6. Incorrect CIFunctionCall syntax example

Anyone using the anatomy diagrams or family detail sections as a reference will get the slot sequences wrong. The invariants and design philosophy sections are accurate.

---

## § Complete Issue List

### §3 — Construct Anatomy

---

**Issue 1** [Error]
- **Location:** Lines 258–266 — StateEnsure anatomy diagram
- **Current text:**
  ```
  in  Approved  ensure  ApprovedAmount > 0  because  "…"
   │     │        ↑           │               ↑        │
  [1]   [2]   disambig.      [3]             slot     [4]
              token                          marker
  ```
- **Problem:** The diagram shows NO optional `when` guard slot between StateTarget and `ensure`. The actual slot sequence from `Constructs.cs` is: `[SlotStateTarget, SlotPreVerbGuardEnsure, SlotEnsureClause, SlotOptBecauseClause]`. The `when` guard IS present in samples (e.g., `in UnderReview when DocumentsVerified ensure CreditScore >= 300 because "..."`).
- **Correct text:** Diagram should show `[when Guard]` as optional slot between `[2] StateTarget` and `ensure` disambiguation token, numbered as slot [3], shifting EnsureClause to [4] and BecauseClause to [5].
- **Fix type:** Doc-only

---

**Issue 2** [Error]
- **Location:** Line 265 — StateEnsure BecauseClause description
- **Current text:** `[4] BecauseClause slot — the mandatory explanatory reason`
- **Problem:** In StateEnsure, the BecauseClause uses `SlotOptBecauseClause` (`IsRequired: false`). It is OPTIONAL, not mandatory. Only in RuleDeclaration is it mandatory (`SlotBecauseClause`).
- **Correct text:** `[5] BecauseClause slot — optional explanatory reason`
- **Fix type:** Doc-only

---

**Issue 3** [Error]
- **Location:** Lines 325–331 — StateAction anatomy diagram
- **Current text:**
  ```
  to  Approved  ->  set ApprovedAmount = ClaimAmount
   │     │       ↑                │
  [1]   [2]  disambig.           [3]
              token
  ```
- **Problem:** Missing optional `when` guard slot between StateTarget and `->`. The actual slot sequence is `[SlotStateTarget, SlotPreVerbGuardArrow, SlotActionChain]`. Sample evidence: `to Confirmed when AmountDue > 0 -> set AmountDue = 0`.
- **Correct text:** Show `[when Guard]` as optional slot [3], shifting ActionChain to slot [4].
- **Fix type:** Doc-only

---

**Issue 4** [Error]
- **Location:** Lines 408–410 — `in` family detail
- **Current text:**
  ```
  in  [AnchorState]  ensure  Expr  because  "..."                    → StateEnsure
  ```
- **Problem:** Missing optional `[when Guard]` between StateTarget and `ensure`. AccessMode (line 409) correctly shows `[when Guard]` but StateEnsure does not.
- **Correct text:**
  ```
  in  [AnchorState]  [when Guard]  ensure  Expr  [because  "..."]   → StateEnsure
  ```
  (Also note: `because` should be shown as optional `[because "..."]` per Issue 2.)
- **Fix type:** Doc-only

---

**Issue 5** [Error]
- **Location:** Lines 427–429 — `from` family detail
- **Current text:**
  ```
  from  [AnchorState]  ensure  Expr  because  "..."                             → StateEnsure
  from  [AnchorState]  ->  actions                                              → StateAction
  ```
- **Problem:** Both StateEnsure and StateAction are missing optional `[when Guard]` before the disambiguation token. From the code: StateEnsure has `SlotPreVerbGuardEnsure`, StateAction has `SlotPreVerbGuardArrow`.
- **Correct text:**
  ```
  from  [AnchorState]  [when Guard]  ensure  Expr  [because  "..."]             → StateEnsure
  from  [AnchorState]  [when Guard]  ->  actions                                → StateAction
  ```
- **Fix type:** Doc-only

---

**Issue 6** [Error]
- **Location:** Lines 437–438 — `to` family detail
- **Current text:**
  ```
  to  [AnchorState]  ensure  Expr  because  "..."    → StateEnsure
  to  [AnchorState]  ->  actions                     → StateAction
  ```
- **Problem:** Same as Issue 5 — both missing optional `[when Guard]`.
- **Correct text:**
  ```
  to  [AnchorState]  [when Guard]  ensure  Expr  [because  "..."]   → StateEnsure
  to  [AnchorState]  [when Guard]  ->  actions                      → StateAction
  ```
- **Fix type:** Doc-only

---

**Issue 7** [Error]
- **Location:** Lines 415–419 — `on` family detail
- **Current text:**
  ```
  on  EventName  ensure  Expr  because  "..."        → EventEnsure
  on  EventName  ->  actions                         → EventHandler
  ```
- **Problem:** EventEnsure is missing optional `[when Guard]` before `ensure`. From the code: EventEnsure has `SlotPreVerbGuardEnsure`. Sample evidence: `on Submit when Submit.RequiresPoliceReport ensure Submit.Amount <= 100000 because "..."`. The main EventEnsure anatomy (lines 269–277) correctly shows this, but the family detail section does not.
- **Correct text:**
  ```
  on  EventName  [when Guard]  ensure  Expr  [because  "..."]  → EventEnsure
  on  EventName  ->  actions                                   → EventHandler
  ```
- **Fix type:** Doc-only

---

### §3 — Computed Field Anatomy

---

**Issue 8** [Error]
- **Location:** Lines 227–235 — Computed field anatomy diagram
- **Current text:**
  ```
  field  LineTotal  as  number  nonnegative  <-  TaxableAmount  *  TaxRate  /  100
    │       │         ↑    │      ↑              │               │
   [1]     [2]      slot  [3]   slot            [4]             [5]
                   marker      marker
  [4] ComputeExpression slot — `<-` is the slot marker; the expression computes the field value
  [5] ModifierList slot — optional trailing modifiers still apply to computed fields
  ```
- **Problem:** Multiple annotation errors:
  1. The `↑` under `nonnegative` is labeled "slot marker" — `nonnegative` is not a slot marker; it's a value within the ModifierList slot. The actual slot marker for ModifierList is implicit (the first modifier keyword signals entry).
  2. `[5]` is labeled "ModifierList slot — optional trailing modifiers" but points to `TaxRate / 100` which is part of the expression, not a modifier.
  3. The actual slot order from `Constructs.cs` is `[IdentifierList, TypeExpression, ModifierList, ComputeExpression]` — modifiers come BEFORE `<-`, not after. There are no trailing modifiers after the compute expression (ComputeExpression has `TerminationTokens: []` meaning it consumes to end-of-construct).
  4. All sample files confirm: `field Tax as number nonnegative <- Subtotal * TaxRate` — modifiers before `<-`.
- **Correct text:** Renumber: [3] TypeExpression (`number`), [4] ModifierList (`nonnegative` — between type and compute marker), [5] ComputeExpression (`<- TaxableAmount + TaxAmount` — everything after `<-`). Remove the "slot marker" annotation under `nonnegative` — it's a modifier value, not a marker. Remove the claim about "trailing modifiers."
- **Fix type:** Doc-only

---

### §6 — Expressions

---

**Issue 9** [Error]
- **Location:** Line 563 — CIFunctionCall example
- **Current text:** `startswith~("val")`
- **Problem:** The actual syntax from the ExpressionForms catalog description is `~startsWith(subject, prefix)` — the tilde goes BEFORE the function name, and two arguments are required. The doc's example has the tilde after the name and only one argument.
- **Correct text:** `~startsWith(Name, "val")`
- **Fix type:** Doc-only

---

**Issue 10** [Warning]
- **Location:** Lines 559–566 — Expression kinds table
- **Current text:** Table shows 6 categories: Atom, Composite, Invocation, Collection, Quantifier, Interpolated
- **Problem:** The `ExpressionCategory` enum has only 4 values: `Atom`, `Composite`, `Invocation`, `Collection`. The doc's "Quantifier" row maps to `ExpressionCategory.Composite` in the code, and "Interpolated" maps to `ExpressionCategory.Atom`. The table invents two categories that don't exist in the type system.
- **Correct text:** Either: (a) restructure the table to use the actual 4 categories and show which forms belong to each, OR (b) keep the presentation grouping for readability but add a note: "Note: the table groups forms by surface structure for documentation purposes. The code uses 4 ExpressionCategory values (Atom, Composite, Invocation, Collection) which classify forms differently."
- **Fix type:** Doc-only

---

### §9 — Catalogs

---

**Issue 11** [Error]
- **Location:** Line 722 — ExpressionForms catalog description
- **Current text:** `ExpressionForms         The expression grammar — 13 node kinds covering atoms,`
- **Problem:** There are 14 `ExpressionFormKind` enum members, not 13. The doc at line 557 correctly says 14.
- **Correct text:** `ExpressionForms         The expression grammar — 14 node kinds covering atoms,`
- **Fix type:** Doc-only

---

### §8 / Appendix — Invariants

---

**Issue 12** [Warning]
- **Location:** Line 839 — Quick-reference invariant 2
- **Current text:** `2. Disambiguation ≤ 2 lookahead tokens`
- **Problem:** This is the OLD wording of Invariant 2, prior to BUG-020. The body text at line 652 correctly says "Family disambiguation must remain linear and guard-bounded." The quick reference is stale and contradicts the body.
- **Correct text:** `2. Family disambiguation must remain linear and guard-bounded`
- **Fix type:** Doc-only

---

### §5 — Slots

---

**Issue 13** [Warning]
- **Location:** Line 485 — BecauseClause row in slot-kind table
- **Current text:** `| \`BecauseClause\` | Mandatory reason string literal | \`because "Approved amount must be positive"\` |`
- **Problem:** The word "Mandatory" is incorrect as a blanket claim. BecauseClause is mandatory in `RuleDeclaration` but optional in `StateEnsure` and `EventEnsure` (both use `SlotOptBecauseClause`). The table should describe what the slot holds, not its optionality — optionality varies by construct.
- **Correct text:** `| \`BecauseClause\` | Reason string literal | \`because "Approved amount must be positive"\` |`
- **Fix type:** Doc-only

---

**Issue 14** [Warning]
- **Location:** Lines 496–509 — TransitionRow slot sequence table
- **Current text:** Shows `[3]` as `(slot marker)` with description "`on` keyword — slot delimiter" and `[5]` as `(slot marker)` for `when`
- **Problem:** In the TransitionRow, `on` is NOT a slot marker — it's the **disambiguation token** that routes to `TransitionRow` within the `from` family. The doc's own disambiguation section correctly identifies `on` as the disambiguation token for TransitionRow (line 367). Calling it a "slot delimiter" here is misleading — it's the family verb.
- **Correct text:** Change [3] description from "(slot marker)" to "(disambiguation token)" and from "slot delimiter" to "family verb / event slot boundary".
- **Fix type:** Doc-only

---

**Issue 15** [Warning]
- **Location:** Lines 800–809 — Quick-reference construct-to-family mapping
- **Current text:** Shows all StateScoped/EventScoped entries without `[when Guard]` markers
- **Problem:** Unlike the full family-detail sections, the quick reference doesn't note that StateEnsure, StateAction, EventEnsure, and AccessMode can have optional pre-verb guards. While a compressed summary may legitimately omit optionals, there's no footnote indicating this. A reader using only the quick reference will not know guards exist.
- **Correct text:** Add a footnote: "Note: StateEnsure, StateAction, EventEnsure, and AccessMode support optional `[when Guard]` before the disambiguation token. See §4 family details."
- **Fix type:** Doc-only

---

**Issue 16** [Warning]
- **Location:** Line 33 — §1 "Context-dependent" row
- **Current text:** `Every construct opens with a keyword that unambiguously identifies its kind (or a shared-family disambiguation token reached after an optional pre-verb guard).`
- **Problem:** Minor imprecision — the parenthetical says "a shared-family disambiguation token reached after an optional pre-verb guard." This is slightly misleading because it suggests the disambiguation token itself follows the guard. In reality the construct opens with a LEADING TOKEN, then has an optional state/event target, THEN an optional pre-verb guard, THEN the disambiguation token. The leading token doesn't "unambiguously identify its kind" for family-routed constructs — it identifies the family. This row is trying to say both things at once and is a bit muddled.
- **Correct text:** `Every construct opens with a keyword that identifies its family. Direct constructs route immediately; shared families resolve at a disambiguation token after the anchor target (and optional pre-verb guard).`
- **Fix type:** Doc-only (minor reword for accuracy)

---

### §3 — Anatomy listing completeness

---

**Issue 17** [Minor]
- **Location:** Line 212 — OmitDeclaration omission note
- **Current text:** `OmitDeclaration is omitted — its slot shape is covered by AccessMode.`
- **Problem:** This claim is not accurate. OmitDeclaration slots are `[SlotStateTarget, SlotFieldTarget]` while AccessMode slots are `[SlotStateTarget, SlotPreVerbGuardModify, SlotFieldTarget, SlotAccessModeKeyword]`. AccessMode has two additional slots (guard and keyword). They share StateTarget + FieldTarget but OmitDeclaration is SIMPLER, not "covered by" AccessMode.
- **Correct text:** `OmitDeclaration is omitted — its slot shape (StateTarget + FieldTarget) is a strict subset of AccessMode's shape, and its omit-specific disambiguation token adequately conveys the difference.`
- **Fix type:** Doc-only (minor wording)

---

**Issue 18** [Minor]  
- **Location:** Line 19 — Status table "Updated" date
- **Current text:** `| Updated | 2026-05-03 |`
- **Problem:** The doc was revised as part of BUG-020 after this date. Should reflect actual last-edit date.
- **Correct text:** Update to actual last edit date when fixes are applied.
- **Fix type:** Doc-only (housekeeping)

---

**Issue 19** [Minor]
- **Location:** Line 163 — construct count claim
- **Current text:** `There are 12 construct kinds`
- **Problem:** This is currently correct (enum values 1–12). No issue — noting for future-proofing only.
- **Correct text:** N/A — accurate.
- **Fix type:** None (confirmed correct)

---

## § Confirmed-Correct Sections

The following sections are **accurate and need no changes**:

1. **§1 "Three Core Design Choices"** (lines 37–94) — flatness, keyword anchoring, named slots. All descriptions are accurate.

2. **§2 "Grammar Hierarchy"** (lines 109–155) — the four-level hierarchy (File → Constructs → Slots → Expressions) is correctly described.

3. **§3 Construct table** (lines 163–178) — all 12 construct kinds listed correctly with accurate leading keywords and descriptions.

4. **§3 PreceptHeader anatomy** (lines 293–298) — correct: `[precept, IdentifierList]`.

5. **§3 Stored FieldDeclaration anatomy** (lines 214–224) — correct slot labels for the stored form.

6. **§3 StateDeclaration anatomy** (lines 238–244) — correct: single StateEntryList slot.

7. **§3 EventDeclaration anatomy** (lines 247–254) — correct: IdentifierList, ArgumentList (optional), InitialMarker (optional).

8. **§3 RuleDeclaration anatomy** (lines 301–309) — correct: RuleExpression, GuardClause (optional), BecauseClause (mandatory for rules).

9. **§3 AccessMode anatomy** (lines 313–321) — correct: StateTarget, GuardClause (optional `when`), FieldTarget, AccessModeKeyword.

10. **§3 EventEnsure anatomy** (lines 269–277) — correct: EventTarget, GuardClause (optional `when`), EnsureClause, BecauseClause (optional). This was added in a prior edit and is accurate.

11. **§3 EventHandler anatomy** (lines 334–341) — correct: EventTarget, ActionChain. No guard (matches code).

12. **§3 TransitionRow anatomy** (lines 280–290) — correct: StateTarget, EventTarget, GuardClause, ActionChain, Outcome. The main diagram is accurate.

13. **§4 "How disambiguation works"** flowchart (lines 381–399) — correct.

14. **§4 `from` family detail** for TransitionRow line (line 427) — correct: shows `[when Guard]` for TransitionRow.

15. **§4 `field` family (Direct)** (lines 443–453) — correct description of three surface forms.

16. **§5 Slot-kind table** (lines 471–489) — all 17 slot kinds listed correctly (except BecauseClause "Mandatory" per Issue 13).

17. **§6 "Where expressions appear"** table (lines 535–542) — correct.

18. **§7 Linguistic Model** (lines 596–634) — accurate and well-written.

19. **§8 Invariants 1, 3, 4, 5, 6** (body text) — all accurate.

20. **§8 Invariant 2** (body text, line 652) — accurate after BUG-020 revision.

21. **§9 "The catalog is the grammar"** (lines 754–789) — accurate architectural description.

22. **Appendix slot-kind-to-expression-type mapping** (lines 814–833) — correct.

---

## § Priority Fix List

Ordered by file position (top to bottom) so edits can be applied without offset drift:

| # | Line(s) | Issue | Severity | Fix summary |
|---|---------|-------|----------|-------------|
| 1 | 19 | 18 | Minor | Update date on next edit pass |
| 2 | 33 | 16 | Warning | Reword "Context-dependent" row for precision |
| 3 | 227–235 | 8 | Error | Rebuild computed-field anatomy: fix slot order, remove "trailing modifiers" claim, fix "slot marker" annotations |
| 4 | 258–266 | 1, 2 | Error | Add `[when Guard]` slot to StateEnsure anatomy; change BecauseClause from "mandatory" to "optional" |
| 5 | 325–331 | 3 | Error | Add `[when Guard]` slot to StateAction anatomy |
| 6 | 408 | 4 | Error | Add `[when Guard]` and `[because]` to `in` family StateEnsure line |
| 7 | 418 | 7 | Error | Add `[when Guard]` and `[because]` to `on` family EventEnsure line |
| 8 | 428–429 | 5 | Error | Add `[when Guard]` and `[because]` to `from` family StateEnsure and StateAction lines |
| 9 | 437–438 | 6 | Error | Add `[when Guard]` and `[because]` to `to` family StateEnsure and StateAction lines |
| 10 | 485 | 13 | Warning | Remove "Mandatory" from BecauseClause slot-kind table row |
| 11 | 496–509 | 14 | Warning | Change `on` from "slot marker / slot delimiter" to "disambiguation token / family verb" |
| 12 | 563 | 9 | Error | Fix CIFunctionCall example from `startswith~("val")` to `~startsWith(Name, "val")` |
| 13 | 559–566 | 10 | Warning | Add note about presentation categories vs code categories, or restructure |
| 14 | 722 | 11 | Error | Change "13 node kinds" to "14 node kinds" |
| 15 | 800–809 | 15 | Warning | Add footnote about optional pre-verb guards in quick reference |
| 16 | 839 | 12 | Warning | Update invariant 2 quick-reference text to match body |

---

## § Methodology

- Verified all 12 `ConstructKind` members against doc claims
- Cross-checked every slot sequence in `Constructs.cs` against every anatomy diagram
- Verified `DisambiguationEntry` tokens for all scoped families
- Checked `IsRequired` flag on every slot mentioned in the doc
- Verified `TerminationTokens` for expression-carrying slots
- Read `ExpressionForms.cs` for kind count and category mappings
- Examined 6 sample files (`loan-application`, `insurance-claim`, `hiring-pipeline`, `event-registration`, `computed-tax-net`, `invoice-line-item`) for real-world usage patterns confirming guard positions
- Confirmed `AllowedIn` scopes for StateEnsure (`[StateDeclaration]`) and EventEnsure (`[EventDeclaration]`)
