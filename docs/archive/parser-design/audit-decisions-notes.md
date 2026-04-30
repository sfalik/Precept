# Phase 2: Decisions Audit Notes
Date: 2026-04-28

## Files Audited

### Source files
- `src/Precept/Language/Constructs.cs` — ConstructKind entries, slot sequences, descriptions
- `src/Precept/Language/TokenKind.cs` — access mode tokens, retired tokens
- `src/Precept/Language/ConstructSlot.cs` — slot kind completeness
- `src/Precept/Language/DiagnosticCode.cs` — diagnostic names and categories

### Documentation
- `docs/language/precept-language-spec.md` — grammar, dispatch tables, composition rules, diagnostic catalog
- `docs/archive/language-design/precept-language-vision.md` — access mode surface, two-layer model (archived)
- `docs/compiler/parser.md` — dispatch tables, AST nodes, construct table, 1:N table, sync points
- `docs/compiler/diagnostic-system.md` — diagnostic metadata references
- `docs/compiler/lexer.md` — keyword catalog references

### Decision records
- `.squad/decisions.md` — all 2026-04-28 entries
- `.squad/decisions/inbox/frank-v8-design.md`
- `.squad/decisions/inbox/frank-v8-fixes.md`
- `.squad/decisions/inbox/george-v8-approved.md`
- `.squad/decisions/inbox/george-v8-review.md`

### Working docs
- `docs/working/catalog-parser-design-v8.md` — reference only (not modified)

### Samples
- `samples/*.precept` — scanned for `write`/`read`/`write all` usage (none found)
- `research/language/expressiveness/*.precept` — found `write` in two research files (pre-decision artifacts, not samples — left as-is)

## Gaps Found and Fixed

1. **`docs/compiler/parser.md` line 148 — ParseInScoped dispatch table**: `Modify, Omit` both routed to `ParseAccessMode(stateTarget)` → `AccessMode`. Per decision F12/D3/D4, `Omit` is a separate construct. **Fixed:** Split into two rows — `Modify` → `ParseAccessMode` → `AccessMode`, `Omit` → `ParseOmitDeclaration` → `OmitDeclaration`.

2. **`docs/compiler/parser.md` line 795 — Construct-to-parse-method table**: `AccessMode` listed with leading token `In or Modify`, no `OmitDeclaration` row. **Fixed:** Split into two rows — `AccessMode` with `In (via Modify lookahead)`, `OmitDeclaration` with `In (via Omit lookahead)`.

3. **`docs/compiler/parser.md` line 800 — Shared leading token prose**: Said "`In` → `StateEnsure` or `AccessMode`". Missing `OmitDeclaration`. **Fixed:** Now reads "`In` → `StateEnsure`, `AccessMode`, or `OmitDeclaration`".

4. **`docs/compiler/parser.md` line 950 — 1:N problem table**: `In` mapped to `StateEnsure, AccessMode` only. **Fixed:** Now reads `StateEnsure, AccessMode, OmitDeclaration` with disambiguation `ensure vs. modify vs. omit`.

5. **`docs/compiler/parser.md` line 527-551 — AccessMode AST section**: Showed both `modify` and `omit` grammar under a single `AccessMode` heading with one `AccessModeNode` that had a `Token Mode` field handling both verbs. Per decisions, `OmitDeclaration` is a separate construct with its own 2-slot AST node. **Fixed:** Split into two subsections — `AccessMode` (with `AccessModeNode` carrying `FieldTargetNode`, `AccessModeKeyword`, optional `Guard`) and `OmitDeclaration` (with `OmitDeclarationNode` carrying only `StateTargetNode` and `FieldTargetNode`).

6. **`docs/compiler/parser.md` line 40 — Declaration family list**: Listed "access mode" but not "omit declaration". **Fixed:** Added "omit declaration" to the list.

7. **`docs/language/precept-language-spec.md` line 568 — Top-level dispatch table**: `in` mapped to `StateEnsureDeclaration or AccessModeDeclaration`. **Fixed:** Now reads `StateEnsureDeclaration, AccessModeDeclaration, or OmitDeclaration`.

8. **`docs/language/precept-language-spec.md` line 616 — `in` dispatch sub-table**: `modify`/`omit` lumped into single "access mode" row. **Fixed:** Split into two rows — `modify` → "access mode" and `omit` → "omit declaration (state-scoped structural exclusion)".

9. **`docs/language/precept-language-spec.md` line 673 — Grammar section heading**: Headed "Access mode" but contained both AccessMode and OmitDeclaration grammar. **Fixed:** Renamed to "Access mode and omit declaration" with introductory sentence clarifying the two-construct split. Grammar rule labels now explicitly tag `AccessMode:` and `OmitDeclaration:` sections.

## Files with No Issues

- `src/Precept/Language/Constructs.cs` — ✓ `AccessMode` (4 slots) and `OmitDeclaration` (2 slots, no guard) correctly separated with accurate descriptions
- `src/Precept/Language/TokenKind.cs` — ✓ `Write`/`Read` removed, `Modify`/`Readonly`/`Editable`/`Omit` present with correct retirement comment
- `src/Precept/Language/ConstructSlot.cs` — ✓ `AccessModeKeyword` and `FieldTarget` slot kinds present
- `src/Precept/Language/DiagnosticCode.cs` — ✓ No stale references to `write`/`read` vocab; `ConflictingAccessModes`, `RedundantAccessMode` present
- `docs/archive/language-design/precept-language-vision.md` — ✓ Uses correct `modify`/`readonly`/`editable`/`omit` vocabulary throughout
- `docs/compiler/diagnostic-system.md` — ✓ Access mode section header clean
- `docs/compiler/lexer.md` — ✓ References AccessMode keyword category correctly
- `samples/*.precept` — ✓ No retired `write`/`read` usage found
- `.squad/decisions.md` — ✓ Historical entries are chronologically correct (T05:08:10Z uses old vocab but is superseded by T06:41:30Z entry — correct archival pattern)

## Verdict

FIXED — 9 gaps corrected across `docs/compiler/parser.md` (6 fixes) and `docs/language/precept-language-spec.md` (3 fixes). All gaps were the same category: dispatch tables and AST documentation still treating `OmitDeclaration` as part of `AccessMode` rather than as a separate construct, contradicting decisions D3/D4/D5. Source files (`Constructs.cs`, `TokenKind.cs`, `ConstructSlot.cs`, `DiagnosticCode.cs`) were already correct.
