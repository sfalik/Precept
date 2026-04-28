# Parser Implementation Notes — Phase 5

## PR 1: Catalog Migration (2025-07-22)

### Slices Completed
All 6 slices completed in a single pass:

- **1.1** `DisambiguationEntry` record — new file, 14 lines
- **1.2** `ConstructMeta` migration — `LeadingToken: TokenKind` → `Entries: ImmutableArray<DisambiguationEntry>`, bridge + obsolete alias
- **1.3** `ConstructSlotKind.RuleExpression` added
- **1.4** `GetMeta()` rewrite — all 12 constructs with full Entries arrays; RuleDeclaration F8 fix (added RuleExpression slot)
- **1.5** Derived indexes — `ByLeadingToken` FrozenDictionary + `LeadingTokens` FrozenSet
- **1.6** Consumer migration — test updated to `PrimaryLeadingToken`, comment references in Parser.cs and analyzer updated

### Test Count
- Before: 1839 (Precept.Tests) + 207 (Analyzers) = 2046
- After:  1859 (Precept.Tests) + 207 (Analyzers) = 2066
- Delta:  +20 new Soup Nazi tests in ConstructsTests.cs

### Surprises / Deviations
- **No `LeadingToken` code references in tools/** — only the test file and two source comments used it. MCP and LanguageServer never referenced `LeadingToken` directly, so consumer migration was minimal.
- **No `Write`/`Read` TokenKind members exist** — they were fully removed in prior work. The retired-token test checks `Writable` instead (the nearest surviving non-leading token) to verify it's not in `LeadingTokens`.
- **RuleDeclaration had only 2 slots** (GuardClause, BecauseClause) — now 3 with RuleExpression. This is the F8 fix noted in the design doc.
- **Pre-existing RS1030 analyzer warning** in `Precept0013ActionsCrossRef.cs` — unrelated, not touched.

### Design Decisions
- `DisambiguationEntry.LeadingTokenSlot` is retained with no current consumers per the spec — future constructs where the leading token is also slot content will use it.
- `[Obsolete]` alias on `LeadingToken` kept so any external consumers get a compile warning pointing them to `PrimaryLeadingToken` or `Entries`.

### Issues for PR 2
- The parser (`Parser.cs`) still uses hand-written dispatch — it does not consume `Entries` or `ByLeadingToken` yet. PR 2 will wire the disambiguation table into parser lookahead.
- The `Precept0014ConstructsCrossRef` analyzer references the old `LeadingToken` concept in its X32 rule description; the rule semantics may need updating once the parser uses `Entries` for multi-entry disambiguation.
