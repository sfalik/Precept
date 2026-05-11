### 2026-05-10T23:12:47.080-04:00: Typed Literal Autocomplete UX Design
**By:** Elaine (UX Designer)
**What:** Design spec for typed literal autocomplete experience
**Why:** Frank and Kramer are fixing bugs; this is the design they should implement toward

## Design Goals
- Make typed literal completion feel like a **typed mini-mode**: once the cursor is inside `'...'`, the dropdown must belong to the literal, not to the outer grammar.
- Be **semantically honest**: show only values valid for the expected type, the current slot, and any active qualifier contract.
- Prefer **no completions over wrong completions**. `field`, `state`, functions, and general expression items must never appear inside a typed literal.
- Treat this as the canonical UX for **all quoted scalar literals**, not just the currently common quote-delimited types.
- Support a general **qualifier-aware mode**: any `in` or `of` qualifier must hard-filter the candidate set before ranking.
- Make **compound temporal literals** a first-class V1 flow, not a deferred enhancement.
- Keep free-form types lightweight. Do not punish `text` authors with noisy menus when the correct action is simply to type.

### Non-goals
- This spec does not redesign diagnostics, parser recovery, or hover content outside the completion surface.
- This spec does not require a new runtime model; it assumes the LS can infer an expected type, read example literals from `TypeMeta.ContentValidation`, reuse same-type literals already present in the file, and expose qualifier metadata for qualifier-aware filtering.
- If the expected type or required qualifier metadata cannot be resolved confidently, the UX should stay quiet rather than guess.

## Trigger Behavior Design
### `'` (opening quote)
- If the expected type is known, `'` opens the **typed literal completion surface**.
- The surface is **type-aware** from the first frame.
- The opening menu should show:
  - **Closed-set values** for closed-set types (`boolean`).
  - **Example full literals** for structured types (`money`, `temporal`, and any future structured quoted scalar type).
  - **Recent/reused values only** for free-form types (`text`, `integer`, `decimal`) when useful.
- If the expected type is **not** known, show **nothing**. Do not fall back to top-level or expression completions.

### `space`
- Keep space as a registered trigger, but make it **slot-sensitive**.
- Inside a typed literal, `space` should only open completions when the user has just transitioned into a slot with an enumerable vocabulary or continuation choice:
  - money: after `<amount> ` -> show money codes
  - temporal: after `<number> ` -> show temporal units
  - temporal: after accepting `+` and typing the next `<number> ` -> show temporal units again
  - temporal: after a complete `<number> <temporal unit>` segment -> show only the continuation item `+`
- Inside free-form `text`, or after a complete non-enumerated segment, `space` should **not** open completions.
- Outside typed literals, existing completion behavior can stay as-is.

### `Ctrl+Space`
- Inside a typed literal, `Ctrl+Space` must always reopen the **typed literal surface for the current slot**.
- It must never escape back to outer-language completions while the caret remains inside the quotes.
- `Ctrl+Space` is the recovery path for empty literals (`''`), partially typed units/money codes, compound temporal continuation, and users who dismiss the automatic popup.

## By-Type Design
The sections below describe the baseline by-type behavior. **Qualifier-Aware Mode** can further narrow any candidate list before ranking.

### text
**Behavior:** mostly free-form.

- On `'`:
  - No automatic dropdown unless there are meaningful recent/reused text literals in the current file.
  - If qualifier-aware mode supplies a closed allowed set, show only those qualifier-legal values.
- On `Ctrl+Space` inside `''` or inside partial text:
  - Show recent/reused text literals from the current file.
  - If qualifier-aware mode is active, hard-filter the list to the declared qualifier value(s).
  - Do not invent fake closed-set suggestions.
- Rationale: for `text`, aggressive autocomplete is usually noise. The correct UX is to let the author type, while still making repeated domain phrases or qualifier-pinned values easy to reuse.

### boolean
**Behavior:** strict closed set.

- On `'` or `Ctrl+Space`, show exactly the legal `boolean` values for the slot:
  - `true`
  - `false`
- If qualifier-aware mode applies, intersect that closed set with the qualifier-legal value(s) before showing anything.
- Filtering:
  - typing `t` narrows to `true`
  - typing `f` narrows to `false`
- No other values, helpers, or examples should appear.
- Same behavior in defaults, expressions, ensures, and arg defaults.

### integer
**Behavior:** free-form numeric with light assist.

- On `'`:
  - Usually no automatic dropdown.
  - If the file already contains same-type literals, reused values may appear.
  - If qualifier-aware mode supplies a closed allowed set, show only those qualifier-legal values.
- On `Ctrl+Space`:
  - Show reused values first.
  - Then show a few starter examples/snippets such as `0`, `1`, `-1`.
  - If qualifier-aware mode is active, hard-filter reused values and examples to the allowed value(s).
- While the user is typing digits, no intrusive popup is needed.

### decimal
**Behavior:** free-form numeric with light assist.

- On `'`:
  - Usually no automatic dropdown.
  - Reused same-type values may appear if available.
  - If qualifier-aware mode supplies a closed allowed set, show only those qualifier-legal values.
- On `Ctrl+Space`:
  - Show reused values first.
  - Then show starter examples/snippets such as `0.0`, `1.0`, `0.85`.
  - If qualifier-aware mode is active, hard-filter reused values and examples to the allowed value(s).
- Do not show money-oriented suggestions unless the type is actually `money` or a richer money-adjacent domain type.

### money
**Behavior:** two-phase structured literal.

#### Phase 0: empty literal (`''` or just typed `'`)
- Show full-literal examples and reused values, e.g.:
  - `0 USD`
  - `100 USD`
  - `50.00 EUR`
- In qualifier-aware mode, filter those examples to the legal money code(s) before ranking.

#### Phase 1: numeric amount (`'100|`)
- Do not auto-popup while the author is still typing the amount.
- On `Ctrl+Space`, show:
  - reused full money literals
  - one or two format examples
- In qualifier-aware mode, hard-filter reused values and examples to the legal money code(s).
- This is a typing phase, not a menu phase.

#### Phase 2: amount + space (`'100 |`)
- Automatically show **money codes only**.
- In qualifier-aware mode, hard-filter the list to the qualifier-legal code(s).
- If the slot is not qualifier-constrained, rank money codes already used in the current file first, then the remaining codes alphabetically.
- Typing `u` filters toward `USD`; typing `e` filters toward `EUR`, etc.
- Selecting a money code inserts the code, and the closing quote if it is missing.

### temporal
**Behavior:** structured literal with a repeatable compound path. This is the primary blocked flow and should feel excellent.

#### Phase 0: empty literal (`''` or just typed `'`)
- Show full-literal single-segment starters and reused values first, e.g.:
  - `30 days`
  - `1 hour`
  - `2 weeks`
  - `6 months`
  - `1 year`
- Also show a **small secondary group** of compound examples so V1 compound support is discoverable without crowding the core path, e.g.:
  - `2 hours + 30 minutes`
  - `1 day + 12 hours`
- If the expected temporal subtype is known more precisely, filter both groups accordingly.
- The empty-state menu is exploratory: it teaches both the single-segment format and the compound pattern.

#### Phase 1: numeric amount (`'3|`)
- Do **not** show temporal unit completions yet.
- The user is still typing the quantity.
- On `Ctrl+Space`, show either:
  - reused full temporal literals, and/or
  - lightweight starter examples from Phase 0
- No keywords. No functions. No outer grammar leakage.

#### Phase 2: number + space (`'3 |`)
- This is the key moment.
- Automatically show **temporal unit words only**.
- Default temporal unit list:
  - `days`
  - `hours`
  - `minutes`
  - `seconds`
  - `weeks`
  - `months`
  - `years`
- If the expected subtype narrows the allowed set, filter the list accordingly rather than showing invalid units.
- If the number is singular (`1` or `-1`), prefer singular insertion (`day`, `hour`, `week`, `month`, `year`, `minute`, `second`) while still matching plural prefixes leniently.
- Selecting a temporal unit inserts the remaining unit text only and keeps the caret **inside** the open quote at the end of the segment.

#### Phase 3: partial or complete unit (`'3 d|` / `'3 days|`)
- Filter the temporal unit list live while the unit is still partial.
  - `d` -> `day`/`days`
  - `mo` -> `month`/`months`
  - `y` -> `year`/`years`
- Once the segment is syntactically complete (`'3 days|`), completion shifts into a **continuation choice**, not a new type search.
- Show exactly one proactive continuation item:
  - `+` -> continue the temporal literal with another `<number> <temporal unit>` segment
- The author may also type `+` manually; the completion item exists for discoverability, not to force the path.

#### Phase 4: after `+` (`'3 days + |` / `'3 days + 2|`)
- Accepting the `+` item inserts ` + ` and re-enters the same number -> temporal-unit cycle.
- Immediately after `+ `, do **not** show temporal units yet.
- On `Ctrl+Space` at `+ `, show **segment starter examples** such as:
  - `30 minutes`
  - `1 hour`
  - `15 seconds`
- Once the user types the next number, the UX is identical to Phase 1.
- After the user types the next space, the UX is identical to Phase 2.

#### Phase 5: finishing the literal
- The `+ <number> <temporal unit>` pattern repeats for as many segments as the grammar allows.
- Selecting a temporal unit does **not** auto-close the quote; the closing quote is explicit so compound continuation always stays available.
- Full-literal starter items from Phase 0 remain quote-aware and may still finish the literal in one step.

## Qualifier-Aware Mode
Qualifier-aware mode is a general overlay that applies to **any** quoted scalar literal slot whose field or event arg carries an `in` or `of` qualifier.

- `money in 'USD'` -> money code list hard-filters to `USD`
- `quantity of 'kg'` -> unit list hard-filters to `kg`
- Any other qualifier-supporting type with `in` or `of` -> value list hard-filters to the declared qualifier value(s)

### Rules
- This is a **hard filter**, not a ranking hint.
- Apply the qualifier filter **before** same-file reuse ranking, example ranking, or prefix matching.
- By-type design still defines *when* completion opens and *what slot* is active; qualifier-aware mode defines the **allowed candidate set** inside that slot.
- If the qualifier reduces the set to one legal value, show that single item cleanly. Do not pad the menu with broader catalog items.
- If the slot should be qualifier-constrained but the LS cannot provide the resolved qualifier metadata, prefer **no completions** over a misleading broader list.

### LS contract
For this mode to work, the LS must expose qualifier metadata to the completion provider for the current expected slot:
- whether `in` and/or `of` applies
- the normalized allowed value set for that qualifier
- enough type/slot context to map the qualifier onto the correct literal segment (full value vs suffix segment)

## Completion Item Spec
### Full literal value items
Use for `boolean` values and full starter examples like `30 days` or `100 USD`.

- **Label:** raw literal content without quotes (`true`, `30 days`, `100 USD`)
- **Kind:** `Value`
- **Detail:** short type annotation (`boolean literal`, `temporal literal`, `money literal`)
- **Documentation:** one-line explanation + one example in full quoted form
- **InsertText:** insert the literal content; also insert the closing quote if it is missing

### Segment items (temporal units / money codes / qualifier values)
Use when the user is already inside the second slot of a structured literal or another qualifier-constrained segment.

- **Label:** segment only (`USD`, `days`, `kg`)
- **Kind:** `Unit`
- **Detail:** `money code`, `temporal unit`, `quantity unit`, or `qualifier value`
- **Documentation:** explain the segment in the context of the full literal (`Example: '100 USD'`, `Example: '30 days'`, `Example: '5 kg'`)
- **InsertText:** replace only the current segment prefix; do not replace the numeric part or earlier segments

### Continuation items (`+`)
Use only for compound `temporal` continuation.

- **Label:** `+`
- **Kind:** `Operator`
- **Detail:** `continue temporal literal`
- **Documentation:** `Add another <number> <temporal unit> segment.`
- **InsertText:** ` + `

### Reused free-form items
Use for `text`/`integer`/`decimal` values already seen in the file.

- **Label:** raw value content
- **Kind:** `Text` for `text`, `Value` for numbers
- **Detail:** `used elsewhere in this file`
- **Documentation:** optional; only if it adds context
- **InsertText:** literal content, plus closing quote if missing

### Example/snippet items
Use sparingly for numeric and empty structured states.

- **Label:** the example itself (`0.0`, `0 USD`, `30 days`, `2 hours + 30 minutes`)
- **Kind:** `Snippet`
- **Detail:** `example format`
- **Documentation:** the format rule in plain language using DSL type names
- **InsertText:** a usable starter value, not explanatory prose

### General insertion rule
- Labels should omit quotes for readability.
- The system should be **quote-aware**:
  - after opening `'`, selecting a full literal item should finish the literal and place the caret after the closing quote
  - inside `''`, selecting should fill the content without duplicating quotes
  - inside a partial segment, selecting should replace only that segment
- **Temporal segment exception:** selecting a `temporal` unit keeps the caret inside the quote so compound continuation remains available.

## Interaction Flow Walkthrough
### Single-segment temporal default
Target authoring flow:

1. User types: `field ReviewDelay temporal default '`
2. Completion opens with temporal starters: `30 days`, `1 hour`, `2 weeks`, `6 months`, `1 year`, plus a small secondary group of compound examples
3. User ignores the menu and types `3`
4. No noisy keyword menu appears; the author is still in the number phase
5. User types space -> `field ReviewDelay temporal default '3 `
6. Completion opens immediately with temporal units only
7. User sees `days`, `hours`, `minutes`, `seconds`, `weeks`, `months`, `years`
8. User types `d`
9. The list narrows to `day`/`days`
10. User accepts `days`
11. Editor inserts `days` and keeps the caret inside the quote at `field ReviewDelay temporal default '3 days|`
12. User types `'`
13. Final result: `field ReviewDelay temporal default '3 days'`

That flow should work identically in rules, ensures, and arg defaults. The expected type, not the surrounding construct, owns the UX.

### Compound temporal default
Target authoring flow:

1. User types: `field RetryAfter temporal default '`
2. Completion opens with single-segment starters first and compound examples second
3. User types `2`
4. No popup appears yet; the author is still in the number phase
5. User types space -> `field RetryAfter temporal default '2 `
6. Completion opens with temporal units only
7. User accepts `hours`
8. Editor inserts `hours` and keeps the caret inside the quote
9. Completion now offers a one-item continuation choice: `+`
10. User accepts `+` (or types it manually)
11. Editor inserts ` + ` -> `field RetryAfter temporal default '2 hours + `
12. User types `30`
13. No unit popup appears until the next space
14. User types space -> `field RetryAfter temporal default '2 hours + 30 `
15. Completion opens with temporal units only again
16. User accepts `minutes`
17. Editor inserts `minutes` and keeps the caret inside the quote
18. If the author wants another segment, `+` is available again; if not, the author types `'`
19. Final result: `field RetryAfter temporal default '2 hours + 30 minutes'`

## Edge Cases
- **Empty typed literal (`''`):**
  - structured types show starter examples
  - free-form types stay quiet unless the user explicitly asks for help or there are meaningful reused values
- **Unknown expected type:**
  - show nothing
  - never leak outer-language completions into the literal
- **Qualifier metadata unavailable for a qualifier-constrained slot:**
  - show nothing rather than a misleading broader list
- **Existing closing quote:**
  - do not insert a second closing quote
- **Cursor at end of a complete value before the closing quote:**
  - `Ctrl+Space` should reopen completions for the current slot, not restart the entire language surface
- **Space inside `text`:**
  - do not auto-open completion just because space is globally registered
- **One valid suffix only:**
  - show the single valid temporal unit / money code / qualifier value cleanly; do not pad the menu with unrelated items
- **Expressions vs defaults:**
  - same expected type = same completion behavior
  - a `boolean` literal in a rule should feel exactly like a `boolean` literal in a default
- **Compound temporal continuation:**
  - after a complete temporal segment, the only proactive continuation item should be `+`
  - finishing the literal is explicit via the closing quote

## Resolved Decisions
- **Scope:** this UX applies to **all quoted scalar literals** and is the long-term canonical path for any type that uses `'...'` syntax.
- **Qualifier filtering:** `in` and `of` qualifiers trigger a general **qualifier-aware mode** across all qualifier-supporting types; candidate lists are **hard-filtered**, not merely ranked.
- **LS dependency:** qualifier-aware completion requires the LS to expose resolved qualifier metadata for the expected slot.
- **Compound temporal V1:** compound `temporal` literals are in V1 scope. The opening menu teaches both single-segment and compound forms, `+` is the discoverable continuation affordance, and the number -> temporal-unit cycle repeats after each `+`.
- **Vocabulary:** completion copy uses DSL type names throughout: `text`, `temporal`, `money`, `integer`, `decimal`, `boolean`.
