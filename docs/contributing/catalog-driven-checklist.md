# Catalog-Driven Architecture Checklist

**Principle:** Domain knowledge is declared as structured metadata in catalogs. Pipeline stages are generic machinery that reads it. The catalog is the language specification in machine-readable form. See [`docs/language/catalog-system.md § Architectural Identity`](../language/catalog-system.md#architectural-identity-metadata-driven) for the full principle and decision framework.

This document is the operational companion. It does not replace `catalog-system.md` — it operationalizes it for implementation and review.

---

## For Implementers

### Before you write any code, ask:

1. **Is this a new language element?** (token, keyword, type, operator, action, modifier, construct, expression form, outcome form, constraint, proof requirement, diagnostic, fault)
   → It belongs in a catalog. Do not add it as a bare constant, inline set, or ad-hoc condition.

2. **Does an existing catalog cover it?**
   Fourteen catalogs cover the complete language surface: twelve language-definition catalogs plus two failure-mode catalogs. Check each one before creating new structure. The fourteen: Tokens, Types, Functions, Operators, Operations, Modifiers, Actions, Constructs, ExpressionForms, Constraints, ProofRequirements, Outcomes, Diagnostics, Faults.

3. **Do all members of this kind share the same metadata shape?**
   → Flat `sealed record`. If shapes vary by kind → discriminated union (`abstract record` base + `sealed` subtypes). Do not use flat records with nullable inapplicable fields. If the type itself is the semantic signal, use a DU as identity rather than adding a classification field.

4. **Does any pipeline stage need to behave differently per member?**
   → That per-member behavior is metadata. Put it in the catalog entry — not in a `switch`, not in a hand-written array, not in an `if` chain.

5. **Does the parser need to know which tokens lead or disambiguate a construct or outcome?**
   → Derive from the catalog. Check `Constructs.ByLeadingToken`, `Constructs.LeadingTokens`, `ConstructMeta.Entries`, `DisambiguationEntry.DisambiguationTokens`, `Outcomes.ByLeadingToken`, `Outcomes.LeadingTokens`, and the relevant token/operator catalogs before writing any lookahead check.

6. **Does the parser need to know the binding power of an operator or led expression form?**
   → Derive it from the catalogs. Binary/postfix operator precedence comes from `Operators.ByToken` / `Operators.ByTokenSequence`; non-operator led forms come from `ExpressionForms.LedForms`. Add metadata there — do not hardcode binding power inside `GetLedBindingPower` or `ParseExpression`.

7. **Does the parser need to know which keywords are valid as member names?**
   → Update the owning accessor metadata in `Types.All`. `Tokens.KeywordsValidAsMemberName` and `Parser.KeywordsValidAsMemberName` are derived automatically from accessor names. Do not maintain a manual allow-list or try to set this in `Tokens.GetMeta`.

8. **Does the lexer need to know about new keywords, operators, or punctuation?**
   → `Tokens.Keywords`, `TwoCharOperators`, `SingleCharOperators`, `PunctuationChars`, and `AccessModeKeywords` are derived from `Tokens.All`. Add or update the `GetMeta` entry — do not edit parallel tables.

9. **Did you add, rename, or remove a `*Kind` enum member or add a new `[CatalogDU]` subtype?**
   → Verify every exhaustive switch compiles, every distributed dispatcher still has full `[HandlesCatalogExhaustively]` / `[HandlesCatalogMember]` coverage, and every switch over a `[CatalogDU]` hierarchy has explicit subtype arms with no `_` / `default` catch-all.

10. **Did you update all downstream artifacts?**
    - MCP catalog surfaces in `tools/Precept.Mcp/CatalogFormatters.cs` and any affected tool outputs
    - Language server completions, hover, semantic tokens, outline/snippets if a user-visible language surface changed
    - TextMate grammar in `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` — do not hand-edit; regenerate it via `tools/Precept.GrammarGen`

---

## Before You Submit

Verify each of the following is true:

- [ ] Every new `*Kind` enum member has a complete `GetMeta` entry
- [ ] Every new `[CatalogDU]` concrete subtype is handled explicitly wherever its base is switched on — no wildcard or `default` catch-all arms
- [ ] Every affected distributed dispatcher still satisfies `[HandlesCatalogExhaustively]` / `[HandlesCatalogMember]` coverage
- [ ] No new `FrozenSet<TokenKind>`, `TokenKind[]`, or `IEnumerable<TokenKind>` is maintained manually — token sets derive from catalog queries
- [ ] No binding power or precedence value is hardcoded outside the Operators or ExpressionForms catalogs
- [ ] No keyword/member-name/operator/punctuation tables are maintained outside `Tokens.All` and its derived indexes
- [ ] No pipeline stage switches on a catalog member's enum identity to apply per-member behavior — per-member behavior lives in catalog metadata
- [ ] No inline disambiguation conditions duplicate information already in `Constructs`, `Outcomes`, `ExpressionForms`, `Operators`, `Tokens`, or other catalogs
- [ ] No downstream pipeline stage reads `Typed*.Syntax` back-pointers outside `TypeChecker`
- [ ] All exhaustive switch methods compile with no `default` suppressions added
- [ ] MCP catalog formatters verified against any catalog shape changes
- [ ] If any user-visible language surface changed, LS completions / hover / semantic tokens / outline / snippets still derive correctly
- [ ] If any language surface changed, the TextMate grammar was regenerated (not hand-edited)

---

## For Reviewers

### Red flags — investigate before approving:

**Token / keyword handling**
- `new[] { TokenKind.X, TokenKind.Y }` or `FrozenSet<TokenKind>` containing specific members → likely a parallel list that should be a catalog query
- `token.Kind == TokenKind.X || token.Kind == TokenKind.Y` chains in parser/lexer/checker → likely metadata that belongs in a catalog field
- Any keyword/operator string literal that mirrors a `Tokens.GetMeta` entry → parallel copy
- A manual allow-list of keywords valid after `.` → member-name legality should derive from `Types.All` accessors via `Tokens.KeywordsValidAsMemberName`

**Operator / precedence handling**
- A binding power integer in `ParseExpression`, `GetLedBindingPower`, or a lookahead helper → should derive from `Operators` or `ExpressionForms`
- A new `if (kind == TokenKind.X)` block in expression parsing that handles precedence or led dispatch → the behavior should be catalog metadata
- `new TokenMeta(...)` inside `Operators.GetMeta` → operator tokens must reference `Tokens.GetMeta(...)`

**Disambiguation**
- Lookahead sets constructed manually in the parser → check whether `Constructs.ByLeadingToken`, `ConstructMeta.Entries`, `DisambiguationEntry.DisambiguationTokens`, `Outcomes.ByLeadingToken`, or `ExpressionForms` already encode this
- Special-cased peek sequences for constructs that share a leading token → the catalog's disambiguation entry should handle this

**Per-member behavior**
- A `switch (kind) { case FooKind.Bar: ...; case FooKind.Baz: ... }` in a pipeline stage → the behavior on each branch is per-member domain knowledge; it belongs in metadata, not in the stage
- Hardcoded member lists in tests like `new[] { ActionKind.Add, ActionKind.Remove }` that represent a semantic grouping → the grouping should be a catalog field (`ApplicableTo`, `Category`, etc.) and the test should query it
- A switch over a `[CatalogDU]` base with `_`, `default`, or a catch-all arm over the abstract base → this silently swallows new subtypes

**Anti-mirroring**
- `TypedField.Syntax`, `TypedState.Syntax`, `TypedEvent.Syntax`, or similar `Typed*.Syntax` access outside `TypeChecker` → downstream stages must consume typed semantic data, not parse-tree back-pointers

**MCP / tooling sync**
- A core catalog changed but `tools/Precept.Mcp/CatalogFormatters.cs`, `tools/Precept.GrammarGen`, or language-server handlers stayed untouched
- A new user-visible element was added but completions, hover, semantic tokens, outline/snippets, or generated grammar show no corresponding change in behavior
- `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` was hand-edited directly

**Completeness enforcement**
- A `default:` or `_:` arm added to silence a CS8509 exhaustive switch → this bypasses the enforcement mechanism; all members must be handled explicitly
- A class marked `[HandlesCatalogExhaustively]` that is missing `[HandlesCatalogMember]` coverage for one or more members
- A switch over a `[CatalogDU]` hierarchy that is missing an explicit subtype arm
- A new `*Kind` member that has no `GetMeta` arm

### The one question that catches everything:

> Does any code in this PR maintain domain knowledge about the language that isn't declared in a catalog?

If yes, that knowledge belongs in the catalog — not in the code.

---

## When You Think the Catalog Can't Express It

Do not work around it. Escalate.

The catalog system is designed to be extended. If a pipeline stage needs metadata that no catalog field currently carries, the answer is to add that field to the catalog — not to hardcode it in the pipeline stage.

Before proposing an inline workaround, you must be able to answer:

1. Which catalog should carry this knowledge?
2. What field shape would it have?
3. Which consumer(s) would read it?
4. Why does the catalog system structurally prevent expressing this — not just "it's inconvenient today"?

If you cannot answer (4), the catalog system can express it. Add the field.

If you can answer (4) and have evidence of a genuine structural limitation, bring it to the owner with a concrete proposal. Do not ship the workaround while the discussion is open.

---

## Reference

- [`docs/language/catalog-system.md`](../language/catalog-system.md) — Catalog system design, the fourteen-catalog inventory, enforcement model, and full architectural identity statement
- [`docs/philosophy.md`](../philosophy.md) — Product philosophy (read before making language design decisions)
- [`CONTRIBUTING.md`](../../CONTRIBUTING.md) — Issue and PR workflow
