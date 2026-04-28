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

## PR 2: Parser Infrastructure (2025-07-23)

### Slices Completed

| Slice | Description | Status |
|-------|-------------|--------|
| 2.1a | Base types (SyntaxNode, Declaration, Statement, Expression) + FieldTargetNode DU + StateTargetNode | ✅ |
| 2.1b | 12 declaration nodes (one per ConstructKind) + supporting types (TypeRefNode DU, FieldModifierNode DU, OutcomeNode DU, 8 action statements) | ✅ |
| 2.2 | Vocabulary FrozenDictionaries (OperatorPrecedence, TypeKeywords, ModifierKeywords, ActionKeywords) derived from catalog metadata | ✅ |
| 2.3 | InvokeSlotParser exhaustive switch (16 arms + wildcard for unnamed values) | ✅ |
| 2.4 | BuildNode exhaustive switch (12 arms, one per ConstructKind) | ✅ |
| 2.5 | ParseConstructSlots generic loop | ✅ |
| 2.6 | SlotOrderingDriftTests | ✅ |

### Test Count
- Before: 1859 (Precept.Tests) + 207 (Analyzers) = 2066
- After:  1885 (Precept.Tests) + 207 (Analyzers) = 2092
- Delta:  +26 new tests (12 AstNodeTests, 10 ParserInfrastructureTests, 4 SlotOrderingDriftTests)

### Design Decisions
1. **SyntaxTree expanded**: Added `Header` (PreceptHeaderNode?) and `Declarations` (ImmutableArray\<Declaration>) to the existing SyntaxTree record. No existing callers affected.
2. **BuildNode helper extensions**: `AsToken()`, `AsTokenArray()`, etc. are temporary `NotImplementedException` stubs. PR 3 will replace these when slot parsers return concrete types.
3. **CS8524 wildcard arm**: `InvokeSlotParser` switch uses all 16 named enum members plus `_ => throw` for unnamed values. Adding a new `ConstructSlotKind` member still triggers a build warning/error for the missing named arm.
4. **InternalsVisibleTo**: Added for `Precept.Tests` so tests can access `internal` members (BuildNode, vocabulary dictionaries).
5. **ParseSession as ref struct**: Mutable parse state in a ref struct prevents heap allocation and enforces single-pass usage.

### Issues for PR 3
- All 16 slot parser methods are `NotImplementedException` stubs — PR 3 fills them in.
- `BuildNodeExtensions` methods are stubs — need real implementations or removal once slot parsers return typed nodes.
- `Parser.Parse()` entry point is still `NotImplementedException` — the dispatch loop is PR 3's first slice.
- `StateEnsureNode` and `StateActionNode` `Preposition` fields use `default` in BuildNode — the dispatch loop will inject the actual preposition token.

## PR 3: Non-Disambiguated Constructs (2025-07-23)

### Slices Completed

| Slice | Description | Status |
|-------|-------------|--------|
| 3.1 | Pratt expression parser (ParseExpression, ParseAtom) with full precedence table | ✅ |
| 3.2 | 9 slot parsers: IdentifierList, TypeExpression, ModifierList, StateModifierList, ArgumentList, ComputeExpression, RuleExpression, GuardClause, BecauseClause | ✅ |
| 3.3 | Top-level dispatch loop (ParseAll), 5 non-disambiguated construct parsers (PreceptHeader, FieldDeclaration, StateDeclaration, EventDeclaration, RuleDeclaration), SyncToNextDeclaration, error recovery | ✅ |

### Test Count
- Before: 1885 (Precept.Tests) + 207 (Analyzers) = 2092
- After:  1944 (Precept.Tests) + 207 (Analyzers) = 2151
- Delta:  +59 new tests (20 ExpressionParserTests, 26 SlotParserTests, 13 ParserTests)

### Expression Node Types Added
9 new expression types in `SyntaxNodes/Expressions/`:
- `IdentifierExpression`, `LiteralExpression`, `BinaryExpression`, `UnaryExpression`
- `CallExpression`, `MemberAccessExpression`, `ConditionalExpression`
- `ParenthesizedExpression`, `InterpolatedStringExpression` (with `InterpolationPart` DU)

### Wrapper Nodes for Slot→BuildNode Bridge
Internal wrapper records replace the `NotImplementedException` stubs from PR 2:
- `TokenWrapper`, `TokenArrayWrapper`, `FieldModifierArrayWrapper`
- `StateEntryArrayWrapper`, `ArgumentArrayWrapper`, `StatementArrayWrapper`
- `BuildNodeExtensions` now unpack these via concrete casts.

### Design Decisions
1. **StateDeclaration uses direct parsing** (not ParseConstructSlots) because its comma-separated state-entry grammar doesn't fit the slot-per-call model. ParseStateEntries handles `Identifier StateModifier*` entries separated by commas.
2. **EventDeclaration uses direct parsing** for the same reason — argument lists use `(name as type, ...)` syntax with parens.
3. **StateModifierKeywords** FrozenSet added, derived from `Modifiers.All.OfType<StateModifierMeta>()`.
4. **ExpressionBoundaryTokens** FrozenSet defines natural expression termination points.
5. **Keyword identifiers in expression position**: `min`, `max`, etc. lex as constraint keywords, not identifiers. Function calls require identifier tokens — tests use `myFunc(a, b)` rather than `min(a, b)`. True built-in function call support will need lexer-context awareness or parser keyword-to-identifier reinterpretation (similar to `set` → `SetType`).
6. **Disambiguated constructs (In/To/From/On)** have a placeholder `DisambiguateAndParse` that skips to the next declaration — PR 4's scope.

### Surprises / Deviations
- The `NonAssociativeComparison` diagnostic fires correctly for chained comparisons like `a < b < c`.
- Interpolated string parsing (StringStart/StringMiddle/StringEnd tokens) works end-to-end with the Pratt parser.
- No changes needed to `Constructs.cs` or any catalog files.

### Issues for PR 4
- Disambiguated construct parsers: In → StateEnsure/AccessMode/OmitDeclaration, To → StateEnsure/StateAction, From → TransitionRow/StateEnsure/StateAction, On → EventEnsure/EventHandler.
- Remaining stub slot parsers: ActionChain, Outcome, StateTarget, EventTarget, EnsureClause, AccessModeKeyword, FieldTarget.
- Keyword-as-function-name reinterpretation for `min()`, `max()`, etc.

## PR 4: Disambiguated Constructs — in/to/on (2025-07-24)

### Slices Completed

| Slice | Description | Status |
|-------|-------------|--------|
| 4.1 | Generic disambiguator with stashed guard injection | ✅ |
| 4.2 | `in`-scoped: AccessMode, OmitDeclaration, StateEnsure(in) + FieldTarget DU, AccessModeKeyword | ✅ |
| 4.3 | `to`-scoped: StateEnsure(to), StateAction + action chain + all 8 action statements | ✅ |
| 4.4 | `on`-scoped: EventEnsure, EventHandler | ✅ |
| 4.5 | DiagnosticCode additions: OmitDoesNotSupportGuard, PreEventGuardNotAllowed | ✅ |

### Test Count
- Before: 1950 (Precept.Tests) + 207 (Analyzers) = 2157
- After:  1985 (Precept.Tests) + 207 (Analyzers) = 2192
- Delta:  +35 new tests in ParserTests.cs

### Design Decisions

1. **Direct parsers vs generic slot path:** Disambiguated constructs use direct parse methods (ParseAccessMode, ParseOmitDeclaration, ParseStateEnsure, etc.) because the disambiguator pre-consumes the anchor and guard. The generic ParseConstructSlots + BuildNode path is unsuitable here — it would re-parse tokens already consumed. The generic slot parsers (ParseStateTarget, ParseFieldTarget, ParseAccessModeKeyword, etc.) are also implemented for any future generic-slot usage or InvokeSlotParser calls.

2. **Stashed guard injection:** `TryParseStashedGuard` peeks for `when` before the disambiguation token. If found, the guard expression is pre-parsed and passed to the construct parser, which injects it into the guard slot. OmitDeclaration rejects it with `OmitDoesNotSupportGuard`.

3. **OmitDeclaration guard rejection is two-sided:** Both pre-field (`in State when X omit ...`) and post-field (`in State omit Field when X`) paths emit `OmitDoesNotSupportGuard`. The post-field case consumes and discards the guard expression to maintain sync.

4. **ActionChain implemented:** Parses `-> action` chains with `IsOutcomeAhead()` lookahead to stop before outcomes. Used by both `ParseStateAction`/`ParseEventHandler` (direct) and the generic `ParseActionChain` slot parser.

5. **`from`-scoped branch is a passthrough stub** — always emits diagnostic and syncs. PR 5 will implement TransitionRow + from-scoped StateEnsure/StateAction.

6. **`ParseOutcome` still throws `NotImplementedException`** — only needed for TransitionRow (PR 5).

### Surprises / Deviations
- No changes needed to any AST node definitions — the PR 2 node hierarchy was exactly right.
- `Token` is a `readonly record struct`, so `Token?` is a nullable value type requiring `.Value` after null-check.
- `PreEventGuardNotAllowed` diagnostic code registered but not yet emitted — it's for TransitionRow stashed guard rejection in PR 5.

### Issues for PR 5
- `ParseOutcome` slot parser: transition / no transition / reject.
- `from`-scoped disambiguation: `From State On Event` → TransitionRow, `From State ensure` → StateEnsure, `From State ->` → StateAction.
- TransitionRow grammar: `from State on Event [when Guard] [-> Actions] -> Outcome`.
- `PreEventGuardNotAllowed` emission when a stashed guard would land on TransitionRow.
