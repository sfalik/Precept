# Patterns & Anti-Patterns Audit

**Auditor:** Frank (Lead/Architect)
**Date:** 2026-05-17
**File:** `src/Precept/Language/SyntaxReference.cs`
**Branch:** `spike/Precept-V2-Radical`

---

## CommonPatterns — Findings

### Entries audited: 17

1. ✅ **Guarded transition** — OK. Fragment compiles in context.
2. ✅ **Computed field** — OK. Compiles clean.
3. ✅ **Conditional action** — OK. Compiles clean.
4. ✅ **Collection state gate** — **FIXED** (was SYNTAX_ERROR). `CurrentInterviewer` is optional but was used in `remove` without a presence guard. Added `and CurrentInterviewer is set` to both guarded rows. Now compiles with all proof obligations proved.
5. ✅ **Stateless write-only precept** — OK. Compiles clean.
6. ✅ **Multi-state lifecycle precept** — OK. Compiles clean.
7. ⚠️ **Constructor Pattern (Existential Fields)** — COMPILER_GAP. Snippet is correct per spec §3A.5 and matches canonical `samples/loan-application.precept`. However, `precept_compile` MCP tool emits PRE0092 ("Event handler not valid in stateful precept") because the type checker doesn't properly exempt initial-event construction rows from the stateless-handler-only rule. The SYNTAX IS CORRECT — this is a known compiler implementation gap. Do not change the pattern.
8. ✅ **Free-Construction Pattern (Governed Draft)** — OK. Compiles clean.
9. ✅ **Ensures invariant** — OK. Compiles clean.
10. ✅ **Money and quantity typed fields** — OK. Compiles clean with all qualifier proofs discharged.
11. ✅ **Entry action hook** — OK. Compiles clean.
12. ✅ **Cross-cutting event (from any)** — OK. Compiles clean.
13. ✅ **Stack and queue operations** — OK. Compiles clean with non-empty proofs proved.
14. ✅ **Optional-with-fallback assignment** — OK. Compiles clean.
15. ✅ **Conditional rule (rule when)** — OK. Compiles clean when fields are money or decimal (intended context). Fragment is type-correct in its natural domain.
16. ✅ **State-scoped editing window** — OK. Compiles clean.
17. ✅ **Interpolation in diagnostic strings** — OK. Fragment compiles (expected graph warnings for reject-only example; no type errors).

### Coverage gaps (CommonPatterns)

- **`omit` / lifecycle-absent fields** — No pattern demonstrates `in State omit Field` usage. The anti-pattern for sentinel defaults shows the fix, but there's no positive pattern demonstrating lifecycle-scoped field absence as a first-class idiom.
- **`to State ensure` (entry ensures)** — No pattern demonstrates entry ensures as a construction-time invariant or transition-gate check, despite being a key feature (§3A.5 §4).
- **`from any` with guard** — The cross-cutting event pattern shows ungarded `from any`. A guarded variant (`from any on X when Condition`) is a common real-world pattern worth demonstrating.

---

## AntiPatterns — Findings

### Entries audited: 6

1. ✅ **Arrow direction for computed fields**
   - Bad snippet: fails with PRE0009 ("Expected declaration keyword, found '->'") ✅
   - Good snippet: compiles clean ✅
   - WhyItFails: accurate — `->` is not valid in a field declaration

2. ⚠️ **Chaining comparisons** — DIAGNOSTIC_CITE_INACCURATE
   - Bad snippet: fails ✅ (but with PRE0018 "Expected boolean, got integer" — not PRE0010 NonAssociativeComparison)
   - Good snippet: compiles clean ✅
   - WhyItFails: Claims "produces a parse error (NonAssociativeComparison)". Actual behavior: the parser doesn't detect the chain; it parses `0 <= Amount` as boolean, then `boolean <= 1000` triggers PRE0018 (type error). The diagnostic PRE0010 exists in the catalog but the parser doesn't emit it for this form. **Not fixing** — the code still fails, the conceptual explanation is correct, and the actual diagnostic is a compiler implementation gap (PRE0010 should fire but doesn't).

3. ⚠️ **Assigning a computed field** — COMPILER_GAP
   - Bad snippet: **compiles clean** ❌ (should produce PRE0038 ComputedFieldNotWritable)
   - Good snippet: compiles clean ✅
   - WhyItFails: Claims "ComputedFieldNotWritable error" — the diagnostic (PRE0038) exists in the catalog and the code exists in `TypeChecker.Validation.Modifiers.cs`, but the check is not firing for `set` actions in transition rows. **Not fixing the pattern** — the entry is correct per language design intent; the compiler has an implementation gap.

4. ✅ **Sentinel defaults for not-yet-meaningful fields**
   - Bad snippet: compiles (valid DSL, just bad practice) ✅
   - Good snippet: compiles clean ✅
   - WhyItFails: accurate conceptual explanation ✅

5. ✅ **Exhaustive rejection rows**
   - Bad snippet: compiles with PRE0126 warnings ("Event always rejects from X — if not applicable, remove the row") ✅ — compiler actively warns about this
   - Good snippet: compiles clean ✅
   - WhyItFails: accurate ✅. Bonus: compiler now confirms the bad practice via PRE0126.

6. ⚠️ **Hollow draft state** — COMPILER_GAP (GoodSnippet only)
   - Bad snippet: compiles with graph warnings (valid DSL, just bad practice) ✅
   - Good snippet: fails with PRE0092 ❌ — same construction row compiler gap as Pattern A
   - WhyItFails: accurate and thorough explanation ✅
   - **Not fixing** — syntax is correct per spec; compiler gap.

### Coverage gaps (AntiPatterns)

- **Using `transition` in a construction row** — new users try `on Create -> set X = Y -> transition Active` which the grammar should structurally exclude. Worth documenting when PRE0092 fix lands.
- **Reading an uninitialized field in its own assignment** — `on Create -> set X = X + 1` produces PRE0095/PRE0096. Common AI agent mistake.
- **Duplicate event declarations** — declaring the same event twice (PRE0002). Simple but common.

---

## Quickstart.cs sync

- ❌ **Count was stale:** said "CommonPatterns (15 verified examples)" — actual count is 17 (George added 2 entries). **FIXED** → updated to 17.
- ✅ AntiPatterns count (6) is accurate.

---

## Summary

- **Total issues found:** 5
- **Fixed in this commit:** 2
  1. CommonPattern "Collection state gate" — added `and CurrentInterviewer is set` presence guards (PRE0116 fix)
  2. Quickstart.cs pattern count 15 → 17

- **Known compiler gaps (do NOT fix patterns — patterns are correct per spec):** 3
  1. PRE0092 not exempting initial-event construction rows → affects Pattern A + Hollow-draft GoodSnippet
  2. PRE0038 (ComputedFieldNotWritable) not firing on `set` in transition rows → affects anti-pattern #3
  3. PRE0010 (NonAssociativeComparison) not firing in parser → affects anti-pattern #2

- **Non-blocking (nice to fix):**
  - Anti-pattern #2 WhyItFails could mention that actual current diagnostic is PRE0018 (type error) rather than PRE0010 (parse error), with a note that PRE0010 will fire once the parser implements chaining detection
  - Consider adding a note to Pattern A and hollow-draft GoodSnippet: "Note: `precept_compile` may emit PRE0092 due to a known compiler gap; this syntax is correct per §3A.5"

- **Recommended new entries (draft in audit only — pending Shane approval):**
  - CommonPattern: "Lifecycle-absent fields (`omit`)"
  - CommonPattern: "Entry ensures as construction gate"
  - AntiPattern: "Reading uninitialized field in construction row"
