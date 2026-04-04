# Soup Nazi (QA/Tester) — Orchestration Log

**Date:** 2026-04-04T06:08:06Z  
**Agent:** soup-nazi-research  
**Task:** Team knowledge refresh — test coverage and analyzer diagnostic gaps

## Execution Summary

- **Status:** ✅ Complete
- **Deliverables:** 
  - `.squad/decisions/inbox/soup-nazi-coverage-gaps.md` (coverage gaps)
  - `.squad/decisions/inbox/soup-nazi-rule-analyzer-gaps.md` (analyzer diagnostics)
- **Method:** Read all 3 test suites (666 tests across Precept.Tests, LanguageServer.Tests, Mcp.Tests)

## Key Findings

### Coverage Baseline

✅ 666 tests passing  
✅ Comprehensive drift-defense coverage  
✅ Core runtime correctness well-tested

### Critical Gap: Rule Analyzer Diagnostics

**Finding:** PreceptAnalyzerRuleWarningTests.cs has only **1 test**, covering 1 warning scenario. Seven critical diagnostic cases are untested:

1. **From-state asserts never checked** — no incoming transitions to state
2. **Field rule scope violations** — references field other than its own
3. **Event rule scope violations** — references instance data (only args visible)
4. **Top-level rule forward reference** — references field before declaration
5. **Null expression failure in rule** — may fail if nullable field is null
6. **Rule violated by field defaults** — default value violates invariant
7. **Initial state rule violated by defaults** — boot failure scenario

**Action:** Write 7 additional test methods in PreceptAnalyzerRuleWarningTests.cs (one per gap).

**Why This Matters:** These diagnostics are compile-time checks already (parser + compiler validate them). The analyzer should expose them via Diagnostic objects for real-time IDE highlighting. Without them:
- Users edit a rule, hit save
- No red squiggles appear
- They publish → compile fails at deploy time
- Or worse: code compiles but rule is silently never checked

### Recommended Test Methods

```csharp
[Fact] public void Diagnostics_FromStateAssertWithoutExitingTransitions_ProducesWarning() { ... }
[Fact] public void Diagnostics_FieldRuleReferencesAnotherField_ProducesError() { ... }
[Fact] public void Diagnostics_EventRuleReferencesInstanceData_ProducesError() { ... }
[Fact] public void Diagnostics_RuleForwardReferencesField_ProducesError() { ... }
[Fact] public void Diagnostics_NullableFieldInNonNullExpression_ProducesWarning() { ... }
[Fact] public void Diagnostics_FieldDefaultViolatesRule_ProducesError() { ... }
[Fact] public void Diagnostics_InitialStateRuleViolatedByDefaults_ProducesError() { ... }
```

---

**Recorded by:** Scribe  
**From:** soup-nazi-rule-analyzer-gaps.md and soup-nazi-coverage-gaps.md
