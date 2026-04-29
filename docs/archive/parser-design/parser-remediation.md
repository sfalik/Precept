# Parser Remediation Report — Phase 7

## Summary

All findings from Frank's Phase 6 review (`docs/working/parser-review.md`) have been addressed. Build green, full test suite passes (2033 core + 207 analyzers + LS + MCP tests).

---

## Findings and Resolutions

### Priority 1 — Behavioral Fix

| ID | Finding | Resolution | Outcome |
|----|---------|------------|---------|
| **NB-2** | `EventHandler` silently discards stashed `when` guard — no diagnostic | Added `EventHandlerDoesNotSupportGuard` diagnostic code. `ParseOnScoped` now checks for stashed guard before routing to `ParseEventHandler` and emits the diagnostic. Consistent with `OmitDoesNotSupportGuard` pattern. | ✅ Diagnostic emitted, EventHandlerNode still produced |

### Priority 2 — Comment Fix

| ID | Finding | Resolution | Outcome |
|----|---------|------------|---------|
| **NB-1** | Stale `Write` references in Parser.cs header comment | Removed `Write` from the 1:N problem description, dispatch table, and sync-point recovery comment. Updated `In` disambiguation to show `Modify`/`Omit`. | ✅ Comment matches actual dispatch |

### Priority 3 — Test Coverage

| ID | Test | Outcome |
|----|------|---------|
| **TG-1** | `Parse_AccessMode_Editable_WithPreFieldGuard` | ✅ Passes — AccessModeNode, Guard non-null, Mode=Editable |
| **TG-2** | `Parse_OmitDeclaration_List_WithPostFieldGuard_EmitsDiagnostic` | ✅ Passes — OmitDoesNotSupportGuard emitted |
| **TG-3** | `Parse_OmitDeclaration_All_WithPostFieldGuard_EmitsDiagnostic` | ✅ Passes — OmitDoesNotSupportGuard emitted |
| **TG-4** | `Parse_OmitDeclaration_List_WithPreFieldGuard_EmitsDiagnostic` | ✅ Passes — OmitDoesNotSupportGuard emitted |
| **TG-5** | `Parse_OmitDeclaration_All_WithPreFieldGuard_EmitsDiagnostic` | ✅ Passes — OmitDoesNotSupportGuard emitted |
| **TG-6** | `Parse_BareModifyAtTopLevel_ProducesDiagnosticAndSyncs` | ✅ Passes — diagnostic emitted, parser recovers to field decl |
| **TG-7** | `Parse_EventHandler_WithStashedGuard_EmitsDiagnostic` | ✅ Passes — EventHandlerDoesNotSupportGuard emitted, node produced |
| **TG-8a** | `Parse_AccessMode_EOF_After_Modify_ProducesDiagnostic` | ✅ Passes — diagnostic, no crash |
| **TG-8b** | `Parse_OmitDeclaration_EOF_After_Omit_ProducesDiagnostic` | ✅ Passes — diagnostic, no crash |
| **TG-9** | `Parse_MixedAccessModeAndOmit_DistinctNodeTypes` | ✅ Passes — 2 distinct node types |
| **TG-10** | `Parse_TransitionRow_PreEventGuard_WithComplexGuardExpression` | ✅ Passes — PreEventGuardNotAllowed, complex guard preserved |
| **TG-11** | Already covered by existing reflection + sample file tests | Skipped — existing tests cover multiple sample files |

### Priority 4 — Documentation Updates

| ID | Finding | Resolution |
|----|---------|------------|
| **D-1** | Status line says "Stub" | Updated to "Complete — 5-PR implementation per v8 plan. All 12 constructs parse. 2033+ tests green." |
| **D-2** | Architecture code sample incorrect | Fixed `private struct ParseSession` → `internal ref struct ParseSession`; `session.ParseAll(); return session.Build();` → `return session.ParseAll();` |
| **D-3** | "catalog-driven dispatch" framing incorrect | Revised to "hand-written keyword switch" with catalog-derived vocabulary. Updated both Overview and Top-Level Dispatch Loop sections. |
| **D-4** | References to `IsMissing` property and `SkippedTokens` | Replaced all 13 occurrences. Described actual behavior: synthetic tokens with empty text and zero-length spans. `SyncToNextDeclaration()` silently advances. |
| **D-5** | `UnexpectedKeyword` in sync-point description and error table | Replaced with `ExpectedToken` in both locations |
| **D-6** | `InvalidCallTarget` in error-conditions table | Removed from table (not emitted by parser). Added `EventHandlerDoesNotSupportGuard` and `ExpectedOutcome` instead. |
| **D-7** | `Precept` header not distinguished from dispatch loop | Added note that header is parsed before the main dispatch loop |
| **D-8** | EventDeclaration grammar shows `("with" ArgList)?` | Fixed to `("(" ArgList ")")?` |
| **D-9** | Test file reference `PreceptParserTests.cs` | Fixed to `ParserTests.cs` |

### Priority 5 — Diagnostic Code Reconciliation

| ID | Finding | Resolution |
|----|---------|------------|
| **NB-3** | `ExpectedOutcome` missing from parser-v2.md diagnostics table | Added to table with description |
| **NB-4** | `UnexpectedKeyword` and `InvalidCallTarget` listed as parse-stage in doc | Marked as "reserved — not currently emitted" in both DiagnosticCode.cs and parser-v2.md |
| **NB-5** | `MutuallyExclusiveQualifiers` in `// ── Parse ──` section of DiagnosticCode.cs | Moved to `// ── Type ──` section (it is `DiagnosticStage.Type` in Diagnostics.cs) |

---

## Final Test Count

- **Precept.Tests:** 2033 passed
- **Precept.Analyzers.Tests:** 207 passed
- **Precept.LanguageServer.Tests:** all passed
- **Precept.Mcp.Tests:** all passed
- **Build:** 0 warnings, 0 errors

## Deferred Items

- **TG-11:** Skipped — the existing `Parse_SampleFile_OmitDeclarationNodes_HaveNoGuard` reflection test and the 3-file sample suite already cover the intent. No additional test needed.
