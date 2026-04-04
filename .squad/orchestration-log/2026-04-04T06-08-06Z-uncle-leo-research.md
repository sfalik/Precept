# Uncle Leo (Code Quality) — Orchestration Log

**Date:** 2026-04-04T06:08:06Z  
**Agent:** uncle-leo-research  
**Task:** Team knowledge refresh — codebase quality patterns

## Execution Summary

- **Status:** ✅ Complete
- **Deliverables:** `.squad/decisions/inbox/uncle-leo-code-review.md`
- **Method:** Read codebase quality patterns, nullable handling, error strategies

## Three Quality Concerns Identified

### 1. **Unsafe Null-Forgiveness Pattern (LOW SEVERITY)**

**Location:** `tools/Precept.LanguageServer/PreceptAnalyzer.cs:35`

```csharp
public bool TryGetDocumentText(DocumentUri uri, out string text)
    => _documents.TryGetValue(uri, out text!);
```

**Issue:** `text!` null-forgiveness bypasses type system. While technically safe (TryGetValue contract ensures non-null on true), it's a code smell that breaks nullable flow analysis.

**Risk:** If logic changes (refactor redirects to different dictionary), suppressed compiler warnings would catch the bug.

**Fix (Ranked):**
1. **Explicit assignment** (Option 1) — most explicit, zero surprise
2. **Assertion** with comment explaining unreachability
3. **Accept pattern** — document inline if performance-critical

**Recommendation:** Option 1 (explicit assignment).

---

### 2. **Grammar-Completions Synchronization Risk (MEDIUM SEVERITY)**

**Location:** `tools/Precept.LanguageServer/PreceptAnalyzer.cs:10–23`

**Issue:** Analyzer uses hand-written regexes (NewFieldDeclRegex, NewCollectionFieldRegex, etc.) NOT derived from parser grammar. If grammar changes (e.g., `field X as string` → `field X: string`), regexes fail silently.

**Risk:** Silent failures in IDE teach users wrong patterns; worse than crashes.

**Fix (Ranked):**
1. **Short-term:** Add documented checklist comment in PreceptAnalyzer.cs header:
   ```csharp
   // ⚠️ GRAMMAR SYNC REQUIRED: If DSL syntax changes, update these regexes:
   //   - field syntax: NewFieldDeclRegex, NewCollectionFieldRegex
   //   - event syntax: NewEventWithArgsRegex
   //   - transition syntax: SetAssignmentExpressionRegex, CollectionMutationExpressionRegex
   ```
2. **Long-term:** Add unit test that runs parser on regex patterns to ensure matches.
3. **Future:** Extract patterns to shared catalog or generate from parser meta.

**Recommendation (Immediate):** Add documented checklist comment.

---

### 3. **Hydrate/Dehydrate Dual-Format Complexity (MEDIUM SEVERITY)**

**Location:** `src/Precept/Dsl/PreceptRuntime.cs:162–253`

**Issue:** Instance data lives in two formats:
- **Public format:** Field names → values (no prefix). Collections are `List<object>`.
- **Internal format:** `__collection__<fieldName>` → `CollectionValue` objects.

Conversion via three methods (`Hydrate`, `Dehydrate`, `CloneCollections`) invoked at three mutation sites (Fire, Inspect, Update).

**Risk:** If any mutation site forgets one step, silent data corruption occurs. Example: Fire forgets to Dehydrate → returned instance has internal keys.

**Fix (Ranked by effort):**
1. **Highest confidence (Medium effort):** Extract `DataMutation` record encapsulating the triple: `(Clean, Internal, Collections)`. Pass through mutation methods instead of juggling three variables.
2. **Good practice (Low effort, high ROI):** Add invariant checks before returning:
   ```csharp
   foreach (var kvp in resultData) {
       if (kvp.Key.StartsWith("__collection__")) {
           throw new InvalidOperationException("Dehydrate forgot to strip collection prefix");
       }
   }
   ```
3. **Documentation (Immediate):** Add comment block explaining three-step protocol.

**Recommendation:** Implement option 2 immediately (catches mistakes in testing). Schedule option 1 (DataMutation wrapper) for next refactor.

---

## Summary

| Issue | Severity | Effort | ROI |
|-------|----------|--------|-----|
| Null-forgiveness | Low | Low | High (clarity) |
| Grammar-completions sync | Medium | Low | High (catches future drift) |
| Hydrate/dehydrate dual format | Medium | Medium | High (prevents silent corruption) |

None block current functionality. All preventable via documentation or lightweight invariant checks.

---

**Recorded by:** Scribe  
**From:** uncle-leo-code-review.md
