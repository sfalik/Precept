# Language Server: Diagnostic Enrichment and Code Actions

**Surface:** VS Code editor + Problems panel  
**Author:** Elaine (UX)  
**Implementer:** Kramer (LS)  
**Status:** Approved — implement against this spec

---

## 1. Overview

Precept's language server already tells users what is wrong. These two features tell them what to do about it — instantly, without leaving the editor.

**Diagnostic enrichment** ("did you mean?") appends a fuzzy-matched suggestion to the diagnostic message when the failing name closely resembles a known name. The suggestion appears everywhere the diagnostic message appears: the Problems panel, the squiggle hover tooltip, and any lint overlay. No new UI surface is introduced.

**Code actions** (lightbulbs) turn diagnostic messages into one-click fixes. Two categories ship:

- *"Did you mean?" code actions* — rename the failing identifier to the matched suggestion.
- *FixHint code actions* — apply mechanical text edits where possible; show the FixHint guidance as a tooltip-only panel where automation is not safe.

Together these features reduce the edit→error→fix loop from several seconds to a single keypress. A user who typo-ed a field name should never have to read the FixHint, open the declaration, and scroll back.

---

## 2. Diagnostic Enrichment

### 2.1 Trigger Conditions

The enrichment pass runs inside `OnCompilationComplete`, after the full pipeline has settled. It processes only the four diagnostic codes listed in §2.4. All other codes are ineligible.

**SemanticIndex guard.** The enrichment pass must only run when `SemanticIndex` is non-null. A null `SemanticIndex` means compilation stopped before the type-check stage (lex or parse errors). The suggestion machinery depends on the symbol tables populated during type-checking; running it without them is undefined.

```
OnCompilationComplete:
  if compilation.Semantics is null → skip enrichment entirely
  for each Diagnostic d in compilation.Diagnostics:
    if d.Code matches a SuggestionSource entry → attempt enrichment
```

Lex-stage diagnostics (`UnterminatedStringLiteral`, `InvalidCharacter`, etc.) and parse-stage diagnostics (`ExpectedToken`, `NonAssociativeComparison`, etc.) never receive "did you mean?" enrichment, even if their messages happen to contain an identifier.

### 2.2 Fuzzy Match Behavior

**Algorithm:** Levenshtein edit distance (insertions, deletions, substitutions each cost 1).

**Threshold:** ≤ 3 edits. A match with edit distance 4 or more is ignored entirely — no suggestion, no lightbulb.

**Candidate selection:**
1. Compute Levenshtein distance between `Diagnostic.Args[0]` and every name in the suggestion pool (§2.4).
2. Discard all candidates with distance > 3.
3. If no candidates remain: no enrichment. The diagnostic message is unchanged, no code action is registered.
4. If one or more candidates remain: pick the one with the lowest distance.
5. **Tie-break:** if two or more candidates share the lowest distance, pick alphabetically by name (case-insensitive, ascending).
6. Append the suggestion suffix to the diagnostic message (§2.3) and register a code action (§3.1).

**Case sensitivity:** Levenshtein operates on the literal casing in the source. `Score` and `score` are distance 1, not 0. Tiebreak is case-insensitive. The replacement text preserves the casing of the matched candidate exactly as declared.

### 2.3 Message Format

The suggestion suffix is appended to the original formatted diagnostic message with an em dash separator.

```
— did you mean 'SuggestedName'?
```

Exact rules:

- One space before the em dash (`—`), one space after.
- The phrase is always lowercase: `did you mean`.
- The suggestion is wrapped in single straight quotes: `'Name'`.
- The phrase ends with a question mark and no trailing space.

**Example — UndeclaredField:**

Original:
```
Field 'ReasonTxt' is not declared
```

Enriched (match: `ReasonText`, distance 1):
```
Field 'ReasonTxt' is not declared — did you mean 'ReasonText'?
```

**Example — UndeclaredState:**

Original:
```
State 'AwaitngReturn' is not declared
```

Enriched (match: `AwaitingReturn`, distance 2):
```
State 'AwaitngReturn' is not declared — did you mean 'AwaitingReturn'?
```

**Example — UndeclaredEvent:**

Original:
```
Event 'Submitt' is not declared
```

Enriched (match: `Submit`, distance 1):
```
Event 'Submitt' is not declared — did you mean 'Submit'?
```

**Example — UndeclaredFunction:**

Original:
```
'roudn' is not a recognized function
```

Enriched (match: `round`, distance 2):
```
'roudn' is not a recognized function — did you mean 'round'?
```

The enriched message replaces `Diagnostic.Message` on the in-flight diagnostic object before it is published via `textDocument/publishDiagnostics`. No separate field or protocol extension is used. The Problems panel and squiggle hover both display the enriched string automatically.

### 2.4 Scope: Which Diagnostic Codes Receive Enrichment

Enrichment is driven by `SuggestionSources` catalog metadata. Only these four codes are in scope:

| `DiagnosticCode` | Suggestion Pool | `Args[0]` Content |
|---|---|---|
| `UndeclaredField` | All field names declared in the precept (`SemanticIndex.UserFields`) | The failing field name as written in source |
| `UndeclaredState` | All state names declared in the precept (`SemanticIndex.UserStates`) | The failing state name as written in source |
| `UndeclaredEvent` | All event names declared in the precept (`SemanticIndex.UserEvents`) | The failing event name as written in source |
| `UndeclaredFunction` | All built-in function names (`Functions.All` — `min`, `max`, `abs`, `clamp`, `floor`, `ceil`, `truncate`, `round`, `roundPlaces`, `approximate`, `pow`, `sqrt`, `trim`, `startsWith`, `endsWith`, `toLower`, `toUpper`, `left`, `right`, `mid`, `tildeStartsWith`, `tildeEndsWith`, `now`) | The failing function name as written in source |

**Why only these four?** These are the only `DiagnosticCategory.Naming` errors where a single candidate pool can be enumerated at compile time and where a close match reliably indicates a typo rather than a conceptual error. Other naming errors (`DuplicateFieldName`, `DuplicateStateName`, etc.) are declaration conflicts, not lookup failures; they do not benefit from suggestions. Type-system errors (`TypeMismatch`, `QualifierMismatch`) involve structural mismatches where no single "intended name" exists to suggest.

---

## 3. Code Actions

### 3.1 "Did you mean?" Code Actions (Rename)

When enrichment produces a suggestion, a code action is registered alongside the enriched diagnostic.

**Code action properties:**

| Property | Value |
|---|---|
| Title | `Rename to 'X'` where X is the suggestion |
| Kind | `quickfix` |
| IsPreferred | `true` (VS Code renders this as the highlighted primary fix) |
| Diagnostics | The originating diagnostic (for correlation in the lightbulb) |

**Text edit:**

Replace the span of `Diagnostic.Span` with the suggestion string verbatim. No surrounding context is modified.

```
WorkspaceEdit:
  DocumentChange for the open document:
    TextEdit:
      range: LSP Range derived from Diagnostic.Span
        start: { line: Span.StartLine - 1, character: Span.StartColumn - 1 }
        end:   { line: Span.EndLine   - 1, character: Span.EndColumn   - 1 }
      newText: "<suggestion>"
```

Note: `SourceSpan` uses 1-based line/column; LSP uses 0-based. Subtract 1 from each component when building the LSP `Range`.

**One action per diagnostic.** Only the best-match suggestion produces a code action. There is no "show all suggestions" secondary menu.

**VS Code editor appearance:**

```
  line 13:   from Submitted on Appove when ...
                             ~~~~~~
                             💡  Rename to 'Approve'
```

The lightbulb appears on the line containing the diagnostic span when the cursor is on that line or anywhere in the span. Clicking it (or pressing `Ctrl+.`) opens the quick-fix menu:

```
  ┌─────────────────────────────────────────────────┐
  │ 💡 Rename to 'Approve'                          │
  │    Quick Fix                                    │
  └─────────────────────────────────────────────────┘
```

After applying, the identifier is replaced in place and the squiggle disappears after the next compile cycle.

### 3.2 FixHint Code Actions

Every diagnostic with a non-null `FixHint` in `DiagnosticMeta` generates a code action, regardless of whether the fix is automatable. The distinction is whether the action carries a `TextEdit` or opens an informational panel.

#### 3.2.1 Mechanical (Text-Edit) Code Actions

These FixHints correspond to unambiguous single-location edits. The LS applies them directly.

| `DiagnosticCode` | Code Action Title | Text Edit |
|---|---|---|
| `UnterminatedStringLiteral` | `Add closing "` | Insert `"` at `Span.End` (offset-based) — after the last character of the literal span |
| `UnterminatedTypedConstant` | `Add closing '` | Insert `'` at `Span.End` |

For `UnterminatedStringLiteral` and `UnterminatedTypedConstant`, the LS derives an insertion point from `Diagnostic.Span.Offset + Diagnostic.Span.Length`. The LSP character position for an insert-only edit has `start == end` (zero-length range, `newText` is the inserted character):

```
TextEdit:
  range: { start: spanEnd, end: spanEnd }
  newText: "\""   (or "'")
```

The "did you mean?" codes (`UndeclaredField`, `UndeclaredState`, `UndeclaredEvent`, `UndeclaredFunction`) also produce mechanical text-edit code actions **when a suggestion is found** — these are the rename actions described in §3.1. The FixHint text (`"Declare the field at the top of the precept using 'field Name as Type'"`) is suppressed in the code action title when a suggestion is available; the rename action takes precedence.

#### 3.2.2 Tooltip-Only (Informational) Code Actions

When a FixHint is present but no automatable text edit can be derived, the code action still appears — it opens a VS Code information panel displaying the FixHint text. This is the VS Code "Show Fix" pattern: the lightbulb is visible, the action is clickable, but clicking it shows guidance rather than mutating the document.

**Affected diagnostics (representative, not exhaustive):**

| `DiagnosticCode` | FixHint (shown in tooltip) |
|---|---|
| `UndeclaredField` (no suggestion) | `Declare the field at the top of the precept using 'field Name as Type'` |
| `UndeclaredState` (no suggestion) | `Declare the state using 'state StateName' before referencing it` |
| `UndeclaredEvent` (no suggestion) | `Declare the event using 'event EventName' before referencing it` |
| `UndeclaredFunction` (no suggestion) | `Use a recognized built-in function name, or check the function catalog` |
| `NoInitialState` | `Add 'initial' to the first state the precept starts in` |
| `CircularComputedField` | `Restructure the computed fields to break the circular dependency` |
| `ConflictingAccessModes` | `Ensure each field has at most one access mode per state` |
| `TypeMismatch` | *(no FixHint — no lightbulb)* |
| `NonChoiceAssignedToChoice` | `Use a string literal from the declared choice set, or an event argument with a compatible choice type` |
| `ChoiceLiteralNotInSet` | `Use one of the declared values of the choice type` |

**Code action properties (tooltip-only):**

| Property | Value |
|---|---|
| Title | `Show fix hint` |
| Kind | `quickfix` |
| IsPreferred | `false` |
| Command | `precept.showFixHint` with the FixHint text as argument |

The `precept.showFixHint` command displays the FixHint text in a VS Code information message (`vscode.window.showInformationMessage`). It does not modify the document.

**Appearance in VS Code:**

```
  line 6:   field Amount as number nonnegative
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            💡  Show fix hint
```

Quick-fix menu:
```
  ┌─────────────────────────────────────────────────────────────────┐
  │ 💡 Show fix hint                                                │
  │    Quick Fix                                                    │
  └─────────────────────────────────────────────────────────────────┘
```

After clicking, a VS Code notification appears:
```
  ┌──────────────────────────────────────────────────────────────────────────────────┐
  │ ℹ Restructure the computed fields to break the circular dependency              │
  └──────────────────────────────────────────────────────────────────────────────────┘
```

#### 3.2.3 No Lightbulb

A diagnostic receives no code action lightbulb when **both** conditions hold:
- `DiagnosticMeta.FixHint` is null for its code, AND
- No "did you mean?" suggestion was found.

Examples: `TypeMismatch`, `QualifierMismatch`, `UnsatisfiableGuard`, `DivisionByZero`, `UnreachableState`. These diagnostics have no FixHint in the catalog and no suggestion pool. They show squiggles and Problems panel entries only.

---

## 4. VS Code Integration

### 4.1 Protocol Surface

Diagnostic enrichment operates entirely on the `textDocument/publishDiagnostics` notification. The LS mutates `Diagnostic.Message` before publishing; no new LSP capabilities or protocol extensions are required.

Code actions use the standard `textDocument/codeAction` request/response cycle.

### 4.2 Capability Declaration

The LS must declare code action support in its server capabilities:

```json
"codeActionProvider": {
  "codeActionKinds": ["quickfix"],
  "resolveProvider": false
}
```

`resolveProvider: false` — all code action details (edits, commands) are fully populated at request time. No lazy resolution step is used.

### 4.3 `textDocument/codeAction` Request Handling

VS Code sends a `textDocument/codeAction` request when the cursor enters a diagnostic span. The request carries:

```json
{
  "textDocument": { "uri": "..." },
  "range": { "start": {...}, "end": {...} },
  "context": {
    "diagnostics": [ /* diagnostics overlapping the range */ ],
    "only": ["quickfix"],
    "triggerKind": 1
  }
}
```

The handler must:

1. For each diagnostic in `context.diagnostics`, look up the corresponding enriched diagnostic and code action set.
2. Return an array of `CodeAction` objects. If no actions apply, return an empty array (not null, not an error).

Code action object shape (mechanical):
```json
{
  "title": "Rename to 'Approve'",
  "kind": "quickfix",
  "isPreferred": true,
  "diagnostics": [ /* the originating diagnostic */ ],
  "edit": {
    "changes": {
      "file:///path/to/file.precept": [
        {
          "range": {
            "start": { "line": 12, "character": 18 },
            "end":   { "line": 12, "character": 24 }
          },
          "newText": "Approve"
        }
      ]
    }
  }
}
```

Code action object shape (tooltip-only):
```json
{
  "title": "Show fix hint",
  "kind": "quickfix",
  "isPreferred": false,
  "diagnostics": [ /* the originating diagnostic */ ],
  "command": {
    "title": "Show fix hint",
    "command": "precept.showFixHint",
    "arguments": ["Restructure the computed fields to break the circular dependency"]
  }
}
```

### 4.4 Ordering

When multiple code actions apply to the same diagnostic span, they are returned in this order:

1. `isPreferred: true` mechanical actions (rename, insert closing quote) — VS Code auto-applies these on `Fix All`
2. `isPreferred: false` tooltip-only actions
3. Any additional non-preferred actions

Within each group, maintain declaration order from the diagnostic list.

---

## 5. Edge Cases and Guard Rails

### 5.1 No Match Within Threshold

**Condition:** Levenshtein distance to all candidates in the suggestion pool is ≥ 4.

**Behavior:** The diagnostic message is published unchanged. No code action is registered for that diagnostic. The FixHint code action (if any) still applies per §3.2.

**Example:** `Submitttttt` (7 characters away from `Submit`) — no suggestion, no rename lightbulb. The FixHint tooltip lightbulb still appears if `FixHint` is set on the code.

### 5.2 Multiple Candidates at Same Distance

**Condition:** Two or more candidates in the pool share the minimum Levenshtein distance.

**Behavior:** Pick alphabetically by name, case-insensitive ascending. Only one suggestion is surfaced — in the message and in the code action.

**Example:** A precept has fields `Approved` and `Approvee`. User types `Approve`. Both are distance 1. Tiebreak selects `Approved` (alphabetically before `Approvee`).

```
Field 'Approve' is not declared — did you mean 'Approved'?
```

### 5.3 SemanticIndex Is Null

**Condition:** Compilation halted at lex or parse stage; `Compilation.Semantics` is null.

**Behavior:** The enrichment pass is skipped entirely. No diagnostics receive suggestion suffixes. FixHint code actions may still be registered for lex/parse diagnostics (e.g., `UnterminatedStringLiteral`, `UnterminatedTypedConstant`) because those actions do not depend on the semantic index — they operate on the raw `Diagnostic.Span` from the lexer.

**Rationale:** Lex errors precede type-checking. The symbol tables that back the suggestion pools (`UserFields`, `UserStates`, `UserEvents`, `Functions.All`) are not available. Attempting enrichment without them is not possible.

### 5.4 Diagnostic Has No FixHint and No Suggestion

**Condition:** `DiagnosticMeta.FixHint` is null AND the enrichment pass found no match.

**Behavior:** No lightbulb. No code action. The squiggle and Problems panel entry appear normally.

**Examples:** `TypeMismatch`, `QualifierMismatch`, `UnsatisfiableGuard`. These diagnostics carry no FixHint in `Diagnostics.cs` and are not in the suggestion-eligible set. The user sees the diagnostic message only.

### 5.5 Empty Suggestion Pool

**Condition:** A "did you mean?"-eligible diagnostic fires in a precept that has no declared fields (for `UndeclaredField`), no declared states (for `UndeclaredState`), no declared events (for `UndeclaredEvent`).

**Behavior:** The pool is empty; Levenshtein produces no candidates. Treat identically to §5.1 — no suggestion, no rename lightbulb.

**Example:** A brand-new file with only `precept Draft` and one field reference in a rule before any fields are declared. The `UndeclaredField` diagnostic fires but `UserFields` is empty; no suggestion is possible.

### 5.6 Suggestion Identical to Failing Name

**Condition:** Levenshtein distance is 0 — the failing name exactly matches a candidate in the pool.

**Behavior:** Distance 0 is within the threshold. However, a distance-0 match means the name *is* declared, which would mean the error fires incorrectly — this is a compiler bug, not an enrichment case. The enrichment pass should not suppress or modify the diagnostic if this occurs; surface it as-is. Do not append a "did you mean 'X'?" suffix when the suggestion equals the failing name.

Implementation guard: `if (suggestion == Args[0]) → skip enrichment`.

### 5.7 `Diagnostic.Span` Is `SourceSpan.Missing`

**Condition:** `Span.Length == 0 && Span.StartLine == 0` (the sentinel `SourceSpan.Missing`).

**Behavior:** Do not register a text-edit code action (there is no valid insertion point). If a suggestion exists, still enrich the message. If a FixHint is present and is tooltip-only, still register the tooltip action (it carries no `TextEdit`). A zero-extent span is valid for informational actions.

---

## 6. Out of Scope

The following are explicitly not part of this spec. They are tracked separately or deferred to a later phase.

**Multi-suggestion menus.** This spec delivers one suggestion per diagnostic. A "pick from N near-matches" secondary menu is Phase 2.

**Partial-word match during active typing.** Suggestions are computed on the settled compilation result. Live-as-you-type fuzzy suggestions (IntelliSense-style) are a completions feature, not a diagnostic enrichment feature.

**Cross-precept symbol resolution.** Suggestion pools are scoped to the current precept file only. If a project-level symbol index ships, pool resolution can be extended; this spec does not define that path.

**Automated multi-step fixes.** `CircularComputedField`, `ConflictingAccessModes`, and similar multi-cause diagnostics cannot be resolved with a single text edit. No automation is attempted for these; they remain tooltip-only per §3.2.2.

**`textDocument/codeAction` kind `refactor`.** Only `quickfix` actions are specified here. Refactor and source-action kinds are out of scope.

**MCP `args` field.** The addition of `args: string[]` to `precept_compile` diagnostic output (Q6) is a separate catalog change with its own implementation path. This spec covers only the language server surfaces.

**`Fix All in File` behavior.** VS Code's built-in "Fix All" applies all `isPreferred` code actions in a file sequentially. No special handling is required from the LS; this falls out of `isPreferred: true` on rename actions. Multi-action ordering and conflict resolution are not specified here.
