# Ink — Project Knowledge

## Role
Core Developer on the Precept project.

## Learnings

### 2026-05-01: Cross-check gap fixes (GAP-1/2/3/4/5/9/10)
- **DiagnosticCode exhaustiveness**: Adding new enum values requires a matching arm in Diagnostics.cs's GetMeta switch — enforced by analyzer PRECEPT0007. Always add both the enum value and the switch arm together.
- **CollectionTypeRefNode CaseInsensitive**: The ~ (Tilde, TokenKind.Tilde = 93) prefix on a collection element type is now captured as a CaseInsensitive bool property on CollectionTypeRefNode. Parser checks Current().Kind == TokenKind.Tilde after Expect(TokenKind.Of) and advances past it.
- **Catalog-derived sets in Parser.cs**: Static FrozenSet<TokenKind> fields at the Parser class level are the right place to hold catalog-derived vocabulary. Added QualifierPrepositionTokens from Types.All → QualifierShape.Slots → Preposition. The ParseSession ref struct can access these statics from the outer class.
- **ParseAccessModeKeyword consistency**: The Direct variant already used Tokens.AccessModeKeywords.Contains(...). The optional variant hardcoded Readonly or Editable; aligned them.
- **Test naming**: GAP-regression tests follow the pattern GAP{N}_<scenario>_<expected> to make the fix being tested immediately obvious in the test output.
