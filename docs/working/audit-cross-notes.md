# Phase 3: Cross-Surface Consistency Audit Notes
Date: 2026-04-28

## Surfaces Audited

1. `docs/language/precept-language-spec.md` — grammar and semantic specification
2. `docs/working/catalog-parser-design-v8.md` — approved parser design (reference quality)
3. `src/Precept/Language/Constructs.cs` — catalog source of truth (12 ConstructKinds)
4. `src/Precept/Language/TokenKind.cs` — token vocabulary
5. `src/Precept/Language/ConstructSlot.cs` — slot kind vocabulary
6. `src/Precept/Language/DiagnosticCode.cs` — diagnostic catalog
7. `src/Precept/Language/Tokens.cs` — token metadata (ValidAfter, categories)
8. `docs/compiler/parser.md` — canonical parser doc (patched in Phase 2)
9. Samples: `loan-application.precept`, `insurance-claim.precept`, `hiring-pipeline.precept`, `building-access-badge-request.precept`, `computed-tax-net.precept`

## Inconsistencies Found and Fixed

1. **Spec §1.2 reserved keyword list vs. TokenKind.cs** — Spec listed `write` and `read` as reserved keywords; TokenKind.cs does not have `Write`/`Read` entries (fully removed in B4). **Authoritative:** TokenKind.cs (catalog). **Fix:** Removed `write  read` from the reserved keyword block. Updated v2 additions to remove `write`, `read`. Updated v3 additions to say write/read are removed entirely (not just retired).

2. **parser.md sync token list vs. v8 Layer E** — parser.md listed `modify  omit` as top-level sync tokens alongside leading tokens. v8 §5 Layer E explicitly says these are "additional recovery anchors within in-scoped parse failures", not top-level sync tokens. The parser.md text also claimed the list was "exactly the LeadingToken values" — contradicted by including disambiguation tokens. **Authoritative:** v8. **Fix:** Removed `modify  omit` from the inline sync list; added a separate paragraph noting them as in-scoped recovery anchors.

3. **parser.md field declaration grammar vs. spec §2.2** — parser.md showed `("=" Expr)?` for computed expressions; spec §2.2 and sample `computed-tax-net.precept` confirm the syntax is `("->" Expr)?` (arrow, not equals). **Authoritative:** spec + samples. **Fix:** Changed `("=" Expr)?` to `("->" Expr)?` in parser.md.

4. **parser.md ConstructKind count** — Text said "11 ConstructKind values"; the table below it listed 12 (including OmitDeclaration, added in v8). **Authoritative:** Constructs.cs (12 members). **Fix:** Changed "11" to "12".

5. **parser.md AccessModeNode shape vs. v8 §3** — parser.md had `StateTargetNode? State` (nullable) and `FieldTargetNode Field` (singular). v8 §3 specifies `StateTargetNode State` (non-nullable) and `FieldTargetNode Fields` (plural — supports DU with list/all shapes). **Authoritative:** v8. **Fix:** Removed nullable `?` from State, renamed Field → Fields.

6. **parser.md OmitDeclarationNode shape vs. v8 §3** — Same issue: nullable State and singular Field. **Authoritative:** v8. **Fix:** Removed nullable `?` from State, renamed Field → Fields.

7. **ConstructSlot.cs ComputeExpression comment vs. spec §2.2** — Comment said `"= expression"` but spec §2.2 and samples use `->` (arrow) for computed expressions. **Authoritative:** spec + samples. **Fix:** Changed comment to `"-> expression" computed value`.

8. **Tokens.cs VA_AllQuantifier vs. spec grammar** — `VA_AllQuantifier` (ValidAfter for the `all` keyword) only listed `[TokenKind.Omit]`, missing `TokenKind.Modify`. The grammar allows `in State modify all readonly` — `all` must be valid after `modify`. **Authoritative:** spec grammar + v8 §2 9-form table. **Fix:** Added `TokenKind.Modify` to `VA_AllQuantifier`.

## Noted but Not Fixed (v8 Factual Claim)

- v8 §4 (line 121) states: "TokenKind.Write and TokenKind.Read — RETIRED from access mode context (remain in TokenKind enum for backward compatibility)." In reality, they are fully absent from the TokenKind enum. The design intent (retire from access mode) is satisfied — the implementation went further than v8 described. Per audit rules, v8 is not modified. This is a non-blocking factual stale note.

- v8 §3 proposes `RuleExpression` as a new ConstructSlotKind for RuleDeclaration's main constraint expression. The current catalog uses `GuardClause` for this slot. This is a known pre-implementation divergence that v8's PR 1 Slice 1.3 will address — not an audit fix.

## Surfaces with No Inconsistencies

- **Constructs.cs ↔ v8 slot sequences** — AccessMode `[StateTarget, FieldTarget, AccessModeKeyword, GuardClause?]` and OmitDeclaration `[StateTarget, FieldTarget]` match exactly.
- **Constructs.cs ↔ spec §2.2 access mode grammar** — 9-form grammar, field target shapes, guard positions all consistent.
- **DiagnosticCode.cs ↔ spec ↔ parser.md** — All four parser diagnostic codes (`ExpectedToken`, `UnexpectedKeyword`, `NonAssociativeComparison`, `InvalidCallTarget`) present and consistently described.
- **v8 disambiguation table ↔ spec §2.2 dispatch table** — In→3 constructs, To→2, From→3, On→2 all match.
- **Samples ↔ spec grammar forms** — All sample access mode declarations use correct `in State modify Field editable [when Guard]` syntax. Comma-list forms present (`building-access-badge-request`). Post-field guard position correct (`loan-application`, `insurance-claim`). No samples use old `write`/`read` vocabulary.
- **TokenKind.cs ↔ spec §1.1 token tables** — All keyword categories (access modes, quantifiers, state modifiers, constraints, types) align 1:1.

## Verdict

FIXED — 8 inconsistencies corrected across 4 files (spec, parser.md, ConstructSlot.cs, Tokens.cs). Build clean, all 2024 tests pass.
