# Elaine — Color Gaps Review

Recorded: 2026-05-12T02:03:27.628-04:00
Requested by: Shane

## 1. Gap 1 verdict — data-name drift

**Verdict:** keep the current field/arg split; do **not** normalize everything back to `#B0BEC5`.

The older visual-system notes and brand spec still tell the older story: a unified data-name lane led by `#B0BEC5`. But the merged team decision ledger and my own history now treat that as superseded. The live design intent is:

- field names and field references = `--field` = `#A5B4FC`
- event args and arg-member references = `--arg` = `#9AD8E8`
- data types = `#9AA8B5`
- data values = `#84929F`

That split has real UX value. Field references name enduring entity shape; arg references name event-scoped behavioural input. Giving them different signals improves scanability inside expressions and reinforces the structure-axis / behaviour-axis model instead of flattening both back into anonymous slate.

`entity.name.type.precept.precept` is **not** a field name used as a type. It is the precept declaration name. Keeping it on `#A5B4FC` is correct: it reads as top-level authored identity, not data-type syntax.

`variable.other.precept` is the one scope in this cluster I would **not** treat as evidence for a unified field lane. It is the catch-all unresolved identifier fallback. It should not visually pretend to be a confirmed field reference. If Kramer touches it, make it an explicitly neutral fallback rather than using it to justify reverting field names to slate.

**Recommended path:** align TextMate and semantic tokens to the **current split**, then fix the documentation drift. In the choice set Shane gave, this is closest to **(b)**, with one refinement: canonize the field/arg split in the design system, keep the extension aligned to that split, and do not rewrite TextMate toward the obsolete `#B0BEC5` story.

## 2. Gap 2 verdict — `support.function.precept`

**Verdict:** built-in functions do **not** belong in the data-name lane.

Built-in functions are language-supplied operations (`count`, `trim`, `now`, etc.), not governed business data. They should read as expression machinery, closer to operators than to field or arg identity.

**Recommendation:** add an explicit rule for `support.function.precept` and place it on the structure/operator lane: `#6366F1`, regular weight.

Why this is the right read:

- it distinguishes built-ins from user-authored data names
- it keeps expression parsing clear: operation vs. operand
- it avoids giving built-ins the same semantic authority as declaration keywords (`#4338CA` bold)
- it removes theme/default drift, which is not acceptable for core language vocabulary

So: **explicit rule required**. Theme/default is not acceptable here.

## 3. Gap 3 verdict — `constant.character.escape.precept`

**Verdict:** Kramer's suspicion is right. Use the data-value lane: `#84929F`.

Escape sequences are still part of the literal's authored value surface. They are technically special, but semantically they should read as part of the same value, not as a second visual voice inside the string.

**Recommendation:** add an explicit `constant.character.escape.precept` rule at `#84929F` and keep it visually quiet. No special contrast boost.

## 4. Gap 4 verdict — typed literals semantic drift

**Verdict:** yes, this fix belongs on the roadmap, and I would treat it as a pre-ship visual consistency item.

Typed literals are one of Precept's most important value surfaces. If `'5 {USD}'` starts in the correct value tone and then flips to the active theme's generic string color once semantic tokens arrive, the editor is telling the user two different stories about the same token. That is exactly the kind of trust-breaking shift we should remove.

**Preferred semantic-token approach:** give typed literals a Precept-owned semantic token type (for example `preceptTypedLiteral` or `preceptString`) and lock that type to `#84929F`. That is cleaner than keeping them on the global VS Code `string` token type, because these are not generic prose strings; they are typed data-value atoms.

If implementation stays on the built-in semantic token type `"string"`, the rule must be language-scoped so it does not recolor strings in other languages. The package-level selector should target **`string:precept`** and set it to `#84929F`. The fallback scope should still point at `string.quoted.single.precept`.

So the design answer is:

- best: move typed literals to a Precept-owned semantic token type and color that type `#84929F`
- acceptable fallback: keep `string`, but scope the semantic rule to `string:precept`
- not acceptable: leave typed literals on theme/default string colors

## 5. Overall assessment

### Needs fixing before ship

- **Gap 4 — typed literals semantic drift.** Too visible, too central to Precept's value-authoring story, too likely to erode trust.
- **Gap 2 — built-in functions uncolored.** Not as severe as Gap 4, but still a real readability hole in core expression syntax. I would fix this in the same pass.

### Acceptable debt for a short window

- **Gap 3 — escape sequences uncolored.** Worth cleaning up, but low-salience compared with typed literals and built-in functions.

### Not a visual blocker, but a source-of-truth blocker

- **Gap 1 — data-name drift** is mostly not a code-coloring defect anymore; it is a **design-document drift** problem. The extension's field/arg split reflects the newer, stronger UX decision. The docs still describe the older unified-slate model. That mismatch will keep generating false audits until the docs catch up.

Net: Kramer should not spend time forcing the extension back to `#B0BEC5`. He should preserve the field/arg split, fix functions and typed-literal semantic consistency, and treat the older docs as the thing that now needs reconciliation.
