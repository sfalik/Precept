# Grammar Generator

## Status

| Property | Value |
|---|---|
| Doc maturity | Full |
| Implementation state | Implemented — `tools/Precept.GrammarGen/Program.cs` |
| Source | `tools/Precept.GrammarGen/Program.cs` |
| Upstream | `Tokens.All` (primary), structural pattern literals |
| Downstream | `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` |
| Related | `docs/compiler/tooling-surface.md` · `docs/language/catalog-system.md` |

---

## Overview

The grammar generator is a standalone .NET console tool that reads catalog metadata and emits a valid `precept.tmLanguage.json`. Its job is precisely the gap between "catalog metadata exists" and "a syntactically and semantically correct TextMate grammar is on disk."

For the generator's place in the tooling ecosystem — invocation, the npm pipeline hook, and how the grammar file flows into the VS Code extension — see `docs/compiler/tooling-surface.md § Grammar Generation (Build Time)`.

For the catalog architecture and what each catalog contains, see `docs/language/catalog-system.md`.

---

## Algorithm Overview

The generator executes in four linear steps:

```
Tokens.All
    │
    ▼
① Catalog read + partition
    Group Tokens.All by TextMateScope.
    Partition into keyword groups (word-char tokens) and operator groups (symbol tokens).
    │
    ▼
② Pattern template emission
    For each keyword group: emit one word-boundary alternation pattern.
    For each operator group: emit one symbol alternation pattern (longest-token-first).
    Register each pattern under a readable repository key derived from its scope name.
    │
    ▼
③ Structural pattern composition
    Append hand-written multi-token patterns to the repository:
    comment, messageStrings, ruleDesugaringModifiers, strings, typedConstants, numbers,
    construct-level patterns (machineDeclaration, stateDeclaration, event variants,
    field variants, etc.), and the catch-all identifierReference.
    │
    ▼
④ Top-level include list + JSON emit
    Assemble a fixed-order top-level `patterns` array (include references only).
    Serialize the complete grammar object as indented JSON to stdout or --output path.
```

The generator's main catalog inputs are `Tokens.All` (keyword/operator scope groups and token-position message strings), `Functions.All` (function-call and future function-position message-string patterns), and `Modifiers.All` (rule-desugaring modifier highlights). It does not read `Constructs`, `Types`, or `Actions` directly — those catalogs' keywords are already present in `Tokens.All` via their respective `TokenKind` entries.

---

## Pattern Template Strategy

### Keyword vs. operator discrimination

The generator classifies each `TokenMeta` entry as a keyword token or an operator token by inspecting its `Text` property:

- **Keyword token** — `Text` consists entirely of word characters (`[A-Za-z0-9_]`). Emitted pattern: `\b(alt1|alt2|...)\b`. Word boundaries are required to prevent substring matches (e.g., `or` matching inside `color`).
- **Operator token** — `Text` contains at least one non-word character (symbols: `->`, `<=`, `==`, `~=`, etc.). Emitted pattern: `(alt1|alt2|...)`. No word boundaries; operators are not lexically adjacent to identifier characters.

Only tokens where both `Text` and `TextMateScope` are non-null are eligible. Tokens with `Text: null` (parse-synthesized tokens such as `SetType`, literals, identifiers) are excluded — they are either hand-written structural patterns or handled by generic literal/identifier patterns.

### Alternation ordering

- **Keywords**: alternation members are sorted alphabetically. TextMate first-match semantics do not pose a risk for word-boundary keyword alternations because keywords are mutually non-overlapping.
- **Operators**: alternation members are sorted by descending length, then alphabetically within equal-length groups. This ensures that multi-character operators (`->`, `<=`, `==`, `~=`, `!=`) are tried before their single-character prefixes (`-`, `<`, `=`, `~`, `!`). Failing to sort longest-first would cause `->` to match as `-` followed by `>`.

### Scope grouping

All tokens sharing a `TextMateScope` value are merged into a single repository entry. The generator emits one pattern object per scope, not one per token. This is a deliberate choice: TextMate applies only the first matching repository include, so merging tokens into their scope group preserves correct behavior while keeping the repository compact.

### Scope-to-repository-key mapping

The `ScopeToRepositoryKey` function converts a TextMate scope string to a human-readable repository key. The mapping is a fixed switch:

| TextMate scope | Repository key |
|---|---|
| `keyword.declaration.precept` | `declarationKeywords` |
| `keyword.control.precept` | `controlKeywords` |
| `keyword.other.action.precept` | `actionKeywords` |
| `keyword.other.outcome.precept` | `outcomeKeywords` |
| `keyword.other.access-mode.precept` | `accessModeKeywords` |
| `keyword.other.quantifier.precept` | `quantifierKeywords` |
| `keyword.other.constraint.precept` | `constraintKeywords` |
| `keyword.operator.logical.precept` | `logicalOperators` |
| `keyword.operator.membership.precept` | `membershipOperators` |
| `storage.modifier.state.precept` | `stateModifiers` |
| `storage.type.precept` | `typeKeywords` |
| `constant.language.boolean.precept` | `booleanLiterals` |
| `keyword.operator.precept` | `symbolOperators` |
| `keyword.operator.arrow.precept` | `arrowOperators` |
| `keyword.other.precept` | `memberNameKeywords` |
| _(fallback)_ | scope with `.precept` stripped, dots replaced with `_`, `Keywords` appended |

These keys appear verbatim in the generated `repository` section and in the top-level `patterns` include list. Keeping them readable is intentional — the generated file should be inspectable without a decoder.

---

## Structural Composition

### Hand-written patterns

Not all grammar patterns can be derived from individual token metadata. Patterns that require multi-token positional context are hand-written in `AddStructuralPatterns()` and appended directly to the repository object. These patterns cover:

| Repository key | What it captures |
|---|---|
| `comment` | `#` to end-of-line |
| `messageStrings` | Catalog-derived message-position strings — currently `because "..."` and `reject "..."`; function wiring exists for future flagged built-ins |
| `ruleDesugaringModifiers` | Catalog-derived modifiers where `ModifierMeta.DesugarsToRule == true`; emitted as `keyword.other.grammar.precept` so they keep the legacy gold rule color |
| `strings` | Double-quoted string literal with escape sequences |
| `typedConstants` | Single-quoted typed constants (`'USD'`, `'kg'`) |
| `numbers` | Integer and decimal literals |
| `machineDeclaration` | `precept Name` header — entity name gets `entity.name.precept.message.precept` |
| `stateDeclaration` | `state Name [modifiers]` — parses name list + `initial` modifier inline |
| `eventWithArgsDeclaration` | `event Name[, ...] with Arg as Type` — full argument signature |
| `eventDeclaration` | `event Name[, ...]` — bare event list |
| `fieldCollectionDeclaration` | `field Name[, ...] as set\|queue\|stack\|... of type` |
| `fieldScalarDeclaration` | `field Name[, ...] as type [modifiers]` |
| `rootEditDeclaration` | `edit all \| edit Field1, Field2` — stateless precept edit declaration |
| `fromOnHeader` | `from State[, ...] on Event` — transition block header |
| `transitionTarget` | `transition StateName` — transition outcome target |
| `assertStatement` | `on Event assert` — event-scoped assert |
| `eventArgReference` | `Event.arg` dot access |
| `collectionMemberAccess` | `Collection.count`, `.min`, `.max`, `.peek`, `.peekby` |
| `identifierReference` | Catch-all bare identifier — last resort, placed last in top-level order |

### Top-level include ordering

TextMate applies the first match in the top-level `patterns` array. The ordering in `BuildTopLevelPatterns()` is not arbitrary — it is a specificity ladder:

1. `#comment` — must precede everything; `#` is valid in strings but the comment pattern anchors to start-of-token.
2. `#messageStrings`, `#strings`, `#typedConstants` — message-position strings must precede the generic string literal pattern so gold-scoped message payloads win before regular string matching.
3. Construct-level structural patterns (`#machineDeclaration` through `#collectionMemberAccess`) — most-specific multi-token patterns first. Among event patterns, `#eventWithArgsDeclaration` precedes `#eventDeclaration` because `with` in the event signature would be silently consumed by the more general pattern if ordered second.
4. Catalog-derived keyword and operator groups — in scope-family order, ending with the fallback `#identifierReference`.

`#messageStrings` remains ahead of `#strings` even though its contents are catalog-derived. That ordering preserves the gold message scope for any token or function marked `IsMessagePosition`. 

### begin/end pairs

The `strings` and `typedConstants` patterns use TextMate `begin`/`end` pairs rather than `match`. This is the required mechanism for constructs that can span positions and contain nested patterns (escape sequences inside strings). Begin/end pairs in TextMate must always specify both `begin` and `end`; the generator honors this constraint. All other patterns in the current output use `match` only.

### Repository key uniqueness

All repository keys must be unique within a grammar. The generator guarantees uniqueness by construction: catalog-derived entries are keyed by their scope (one entry per scope), and structural entries are keyed by fixed string names that do not overlap with any scope-derived key.

---

## Message-String Positional Context

`because "message"` and `reject "message"` both require the string argument to receive the gold scope `string.quoted.double.message.precept`. This scope is part of the visual system's message-position semantic — it tells the editor theme to render message strings in gold (`#FBBF24`) to distinguish them from data-value strings.

The generator now derives these patterns from catalog metadata instead of a hardcoded leader list. `TokenMeta.IsMessagePosition` marks keyword tokens whose following string literal is a user-facing message, and `FunctionMeta.IsMessagePosition` reserves the same contract for built-in functions whose trailing string argument is a message.

With this flag in place, `AddStructuralPatterns()`:
1. Finds all tokens where `IsMessagePosition == true` and emits one capture-group pattern per token.
2. Preserves the token's own `TextMateScope` on the keyword capture.
3. Applies `string.quoted.double.message.precept` to the captured message string.
4. Emits a simple function-call variant for any flagged built-in function so future trailing message arguments can use the same gold scope without new generator-side hardcoding.

No built-in functions currently set `IsMessagePosition`, so today's generated `messageStrings` repository still contains only the token-derived `because` and `reject` patterns. That empty-by-default function path is intentional and architecturally complete.

## Rule-Desugaring Modifier Context

Some field modifiers are surface sugar for rule semantics and must render in the same gold keyword scope as rule grammar. `ModifierMeta.DesugarsToRule` is the catalog flag for that contract.

With this flag in place, `AddStructuralPatterns()` emits `ruleDesugaringModifiers` from `Modifiers.All.Where(m => m.DesugarsToRule)`, assigns `keyword.other.grammar.precept`, and includes that repository entry before `#constraintKeywords` anywhere modifier patterns are composed. That ordering keeps the gold rule-desugaring scope from being swallowed by the generic constraint keyword alternation.

---

## Scope Vocabulary

The generator does not define the TextMate scope vocabulary. The 49-scope vocabulary — which scope names exist, what each means visually and semantically, and how editor themes map them to colors — is defined in the visual system design documents:

- `design/system/semantic-visual-system-manifest.md` — semantic layer model and color role definitions
- `design/system/semantic-visual-system-notes.md` — implementation notes and scope-to-color assignments

The generator's role is mechanical: it reads `TokenMeta.TextMateScope` and emits the value as the `name` field of a pattern object. Every scope name that appears in the generated grammar originates from a `TokenMeta` entry or from a structural pattern literal. The generator does not invent scope names.

---

## Output Contract

### Semantic equivalence

The generator's output must be semantically equivalent to the hand-authored `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`. Equivalence means: every token kind that receives a specific scope in the hand-authored grammar must receive the same scope (or a superset scope) in the generated grammar, and no token kind should receive a degraded or incorrect scope.

The generator does not need to produce identical JSON — key ordering, whitespace, and repository key names may differ — but the effective highlighting behavior must match or exceed the hand-authored baseline.

### Stale patterns must not appear

The following keywords were retired and must not appear in any generated alternation pattern:

| Keyword | Retirement notes |
|---|---|
| `nullable` | Retired; replaced by `optional` |
| `invariant` | Retired; replaced by `rule` |
| `assert` | Retired as a top-level keyword; replaced by `ensure` in event/state context |
| `write` | Retired B4 (2026-04-28); replaced by `modify`/`editable` |

Because the generator derives patterns exclusively from `Tokens.All`, stale keywords are excluded by construction as long as their `TokenKind` entries either have `Text: null` or have no `TextMateScope`. Periodic verification that no retired `TokenKind` carries a non-null `Text` and `TextMateScope` is sufficient.

### Maturity threshold

The generated grammar replaces the hand-authored `precept.tmLanguage.json` in production when:

1. All 49 scopes in the visual system vocabulary are correctly assigned in generated output.
2. The message-string metadata is catalog-driven (`IsMessagePosition` present on the relevant catalog records and consumed by the generator).
3. `#messageStrings` is generated from catalog metadata and remains present before `#strings`.
4. A diff comparison between the generated output and the hand-authored file shows no scope regressions on the canonical sample files in `samples/`.

Until these conditions are met, the hand-authored file remains in production and the generator output is a build artifact for comparison only.

---

## Deliberate Exclusions

The following are intentionally outside the generator's scope:

| Excluded concern | Where it lives |
|---|---|
| Scope vocabulary definition | `design/system/` visual system docs |
| Semantic token classification (Pass 1 + Pass 2) | Language server; see `docs/compiler/tooling-surface.md § Semantic Tokens` |
| Completion candidate derivation | Language server |
| Hover text assembly | Language server |
| Catalog schema and metadata-driven architecture | `docs/language/catalog-system.md` |
| Generator's place in the tooling ecosystem | `docs/compiler/tooling-surface.md` |

The generator does not handle `Constructs` or `Actions` catalogs directly. Most keyword surface still comes through `Tokens.All` via `TokenKind` entries; the explicit exceptions are `Functions.All` for function/message-string structural patterns and `Modifiers.All` for `DesugarsToRule` gold-keyword derivation.

The generator does not produce begin/end patterns for multi-token construct bodies (e.g., a `begin: "from"` / `end: "$"` block for the full transition row body). Construct-level nesting is handled by the structural patterns in `AddStructuralPatterns()`. Automating construct-body patterns from `Constructs.All` would require the generator to read slot sequences and termination tokens — possible in principle but not implemented and not required for the current feature set.

---

## Cross-References

| Document | Relationship |
|---|---|
| `docs/compiler/tooling-surface.md` | Generator's role in the tooling ecosystem; invocation; extension integration |
| `docs/language/catalog-system.md` | Catalog architecture; what `Tokens.All` contains; metadata-driven design principle |
| `design/system/semantic-visual-system-manifest.md` | Scope vocabulary — color semantics and 49-scope role definitions |
| `design/system/semantic-visual-system-notes.md` | Scope-to-color assignments and visual system implementation notes |
| `tools/Precept.GrammarGen/Program.cs` | Implementation — the definitive source for all generator behavior |
| `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` | Hand-authored grammar — the production baseline the generator must match |
| `src/Precept/Language/Tokens.cs` | `Tokens.All` / `TokenMeta` — the primary catalog input |
