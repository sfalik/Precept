# Elaine — Visual System Notes Updated

Recorded: 2026-05-12T02:14:05.892-04:00
Requested by: Shane

## What changed

- Updated `design/system/semantic-visual-system-notes.md` to replace the obsolete unified `#B0BEC5` data-name story with the canonical split:
  - field names / field references = `--field` = `#A5B4FC`
  - event args / arg-member references = `--arg` = `#9AD8E8`
  - data types = `#9AA8B5`
  - data values = `#84929F`
- Added explicit palette guidance for `support.function.precept` (`#6366F1`) and `constant.character.escape.precept` (`#84929F`).
- Added the typed-literal color policy: TextMate `string.quoted.single.precept` stays on `#84929F`; semantic tokens must use a Precept-owned type such as `preceptTypedLiteral` / `preceptString`, or fall back to scoped `string:precept`, also at `#84929F`.
- Archived the old `--data: #B0BEC5` lane as superseded rather than leaving it as live canonical guidance.

## Why

- Fields name enduring entity shape; args name event-scoped behavioural input. Separate signals improve scanability and preserve the structure-axis / behaviour-axis read inside expressions.
- Built-in functions are language operations, not user-authored data, so they belong on the structure/operator lane.
- Escape sequences are still part of the literal's authored value surface and should not become a competing visual voice.
- Typed literals are trust-sensitive. If startup and semantic-token colors disagree, the editor tells two different stories about the same token.
