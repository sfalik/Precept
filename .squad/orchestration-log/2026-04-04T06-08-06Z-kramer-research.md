# Kramer (Tooling Dev) — Orchestration Log

**Date:** 2026-04-04T06:08:06Z  
**Agent:** kramer-research  
**Task:** Team knowledge refresh — language server, extension, grammar review

## Execution Summary

- **Status:** ✅ Complete
- **Deliverables:** `.squad/decisions/inbox/kramer-tooling-review.md`
- **Method:** Read language server + extension + TextMate grammar

## Critical Findings

### 1. **Grammar-Completions Synchronization (SYNC RULE) — NON-NEGOTIABLE**

**Finding:** DSL parser and VS Code tooling loosely coupled via regex patterns in `PreceptAnalyzer.cs` and `syntaxes/precept.tmLanguage.json`. No automated drift detection.

**Recommendation:** Add comment to top of `PreceptAnalyzer.cs` and `.squad/decisions.md` flagging this as NON-NEGOTIABLE. Kramer must review tooling whenever parser syntax changes.

### 2. **Incomplete Syntax Highlighting Implementation**

**Finding:** Design docs describe 7-phase rollout for 8-shade palette. Current state:
- Phase 0 (Grammar refactor) — not started
- Phase 1-2 (Custom semantic tokens) — not started
- Phase 3-7 (Color binding + modifiers) — not started

**Impact:** 8-shade palette defined but not implemented; users see generic theme colors.

**Recommendation:** Lane assignment: George (Phases 0-1), Kramer (Phases 2-7). Multi-week project, not urgent.

### 3. **Completions Type-Awareness Gaps**

Three scenarios lack type-aware filtering:
- Set assignment expressions
- Collection mutations
- Dequeue/pop "into" targets

**Status:** Nice-to-have validations; parser already catches errors. Queue as Phase 2 enhancement.

### 4. **Semantic Token Modifiers Not Emitted**

**Finding:** `preceptConstrained` modifier registered but never emitted. Design calls for italic text on constrained fields/states/events.

**Status:** Phase 7 of implementation plan; queue after colors bound (Phase 5).

### 5. **Grammar Coverage Gaps (Minor)**

- Numeric literal edge cases (grammar loose, parser handles fine)
- `"rule"` keyword (not yet live; add when feature ships)
- `"contains"` verb (registered but rarely used)

**Status:** No action required; document as known.

### 6. **Hover & Definition Limitations**

- Built-in collection members (`.count`) show no tooltip
- Precept name (top-level machine name) not clickable for "go to"

**Status:** Design limitations; document for Phase 2 enhancement.

## Recommendations Summary

| Issue | Priority | Owner | Timeline |
|-------|----------|-------|----------|
| Implement Phases 0-1 of syntax highlighting | Medium | George + Kramer | 4-6 weeks |
| Add drift detection tests (parser ↔ grammar/analyzer) | Medium | Kramer | Post-launch |
| Type-aware completions (Phase 2) | Low | Kramer | Post-launch |
| Built-in member hover tooltips | Low | Kramer | Phase 2 enhancement |
| **Document critical sync rule** | **High** | **Kramer** | **Now** |
| CLI tooling scope (if desired) | TBD | Product | Backlog |

---

**Recorded by:** Scribe  
**From:** kramer-tooling-review.md
