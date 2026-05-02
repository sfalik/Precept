# Catalog-Driven Architecture Checklist

**Principle:** Domain knowledge is declared as structured metadata in catalogs. Pipeline stages are generic machinery that reads it. The catalog is the language specification in machine-readable form. See [`docs/language/catalog-system.md § Architectural Identity`](../language/catalog-system.md#architectural-identity-metadata-driven) for the full principle and decision framework.

This document is the operational companion. It does not replace `catalog-system.md` — it operationalizes it for implementation and review.

---

## For Implementers

### Before you write any code, ask:

1. **Is this a new language element?** (token, keyword, type, operator, action, modifier, constraint, diagnostic, fault, grammar construct, proof requirement)
   → It belongs in a catalog. Do not add it as a bare constant, inline set, or ad-hoc condition.

2. **Does an existing catalog cover it?**
   Twelve catalogs cover the complete language surface. Check each one before creating new structure. The twelve: Tokens, Types, Functions, Operators, Operations, Modifiers, Actions, Constructs, Constraints, ProofRequirements, Diagnostics, Faults.

3. **Do all members of this kind share the same metadata shape?**
   → Flat `sealed record`. If shapes vary by kind → discriminated union (`abstract record` base + `sealed` subtypes). Do not use flat records with nullable inapplicable fields.

4. **Does any pipeline stage need to behave differently per member?**
   → That per-member behavior is metadata. Put it in the catalog entry — not in a `switch`, not in a hand-written array, not in an `if` chain.

5. **Does the parser need to know which tokens lead or disambiguate a construct?**
   → Derive from the catalog. Check `Constructs.ByLeadingToken`, `ConstructMeta.Entries`, `DisambiguationEntry.DisambiguationTokens`, and the relevant token/operator catalogs before writing any lookahead check.

6. **Does the parser need to know the binding power of an operator?**
   → The `OperatorPrecedence` dictionary is derived automatically from `Operators.All.OfType<SingleTokenOp>()`. Add the operator to the Operators catalog with its `BindingPower` — do not set binding power inline.

7. **Does the parser need to know which keywords are valid as member names?**
   → Set `IsValidAsMemberName: true` in `Tokens.GetMeta`. The `KeywordsValidAsMemberName` set in `Parser.cs` is derived automatically.

8. **Does the lexer need to know about new keywords?**
   → `Tokens.Keywords` is a computed `FrozenDictionary` derived from `Tokens.All`. Add the `GetMeta` entry — do not edit a keyword table.

9. **Did you add, rename, or remove a `*Kind` enum member?**
   → Verify every exhaustive switch compiles (`[HandlesCatalogExhaustively]`). Every member must have a `GetMeta` arm — the compiler enforces this. Do not add `_` or `default` catch-all arms to silence it.

10. **Did you update all downstream artifacts?**
    - MCP tool DTOs in `tools/Precept.Mcp/Tools/` — check if serialization still matches
    - Language server completions and hover if a new element is user-visible
    - TextMate grammar — do not hand-edit; it is generated from catalog metadata

---

## Before You Submit

Verify each of the following is true:

- [ ] Every new `*Kind` enum member has a complete `GetMeta` entry
- [ ] No new `FrozenSet<TokenKind>`, `TokenKind[]`, or `IEnumerable<TokenKind>` is maintained manually — all token sets are derived from catalog queries
- [ ] No binding power or precedence value is hardcoded outside the Operators catalog
- [ ] No keyword list is maintained outside `Tokens.All` — no parallel arrays of keyword strings or token kinds
- [ ] No pipeline stage switches on a catalog member's enum identity to apply per-member behavior — per-member behavior lives in catalog metadata
- [ ] No inline disambiguation conditions duplicate information already in `Constructs`, `Operators`, `Tokens`, or other catalogs
- [ ] All exhaustive switch methods compile with no `default` suppressions added
- [ ] MCP DTOs verified against any catalog shape changes
- [ ] If any language surface changed, the TextMate grammar was regenerated (not hand-edited)

---

## For Reviewers

### Red flags — investigate before approving:

**Token / keyword handling**
- `new[] { TokenKind.X, TokenKind.Y }` or `FrozenSet<TokenKind>` containing specific members → likely a parallel list that should be a catalog query
- `token.Kind == TokenKind.X || token.Kind == TokenKind.Y` chains in parser/lexer/checker → likely metadata that belongs in a catalog field
- Any keyword string literal that mirrors a `Tokens.GetMeta` description → parallel copy

**Operator / precedence handling**
- A binding power integer in `ParseExpression` or a lookahead method → should derive from `Operators.All`
- A new `if (kind == TokenKind.X)` block in `ParseExpression` handling infix behavior → the operator should be in the Operators catalog with a `BindingPower`

**Disambiguation**
- Lookahead sets constructed manually in the parser → check whether `Constructs.ByLeadingToken` or `DisambiguationEntry.DisambiguationTokens` already encodes this
- Special-cased peek sequences for constructs that share a leading token → the catalog's disambiguation entry handles this

**Per-member behavior**
- A `switch (kind) { case FooKind.Bar: ...; case FooKind.Baz: ... }` in a pipeline stage → the behavior on each branch is per-member domain knowledge; it belongs in metadata, not in the stage
- Hardcoded member lists in tests like `new[] { ActionKind.Add, ActionKind.Remove }` that represent a semantic grouping → the grouping should be a catalog field (`ApplicableTo`, `Category`, etc.) and the test should query it

**MCP / tooling sync**
- Core model type changed but `tools/Precept.Mcp/Tools/` DTOs unchanged
- `LanguageTool.cs` serialization references a property that was renamed or removed
- A new catalog was added but `precept_language` output doesn't reflect it

**Completeness enforcement**
- A `default:` or `_:` arm added to silence a CS8509 exhaustive switch → this bypasses the enforcement mechanism; all members must be handled explicitly
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

- [`docs/language/catalog-system.md`](../language/catalog-system.md) — Catalog system design, the twelve catalogs, and the full architectural identity statement
- [`docs/philosophy.md`](../philosophy.md) — Product philosophy (read before making language design decisions)
- [`CONTRIBUTING.md`](../../CONTRIBUTING.md) — Issue and PR workflow
