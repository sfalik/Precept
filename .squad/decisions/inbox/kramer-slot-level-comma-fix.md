# Kramer: slot-level comma continuation fix

Date: 2026-05-16T08:25:44.026-04:00

## Root cause

The remaining comma bugs were all the same class of failure: slot routing treated `,` as punctuation outside the owning semantic list. That worked only for the first repaired `from` case; state declaration entry lists still fell into modifier completions, and `modify`/`omit` field lists had no continuation lane at all.

## Durable rule

Comma continuation must be resolved at the slot level. When the cursor follows a comma inside a parsed name-list slot, keep completion on that slot's semantic lane (`StateTarget`, state declaration names, or `FieldTarget`) and filter out names already present in the same slot payload before offering candidates. Wildcards like `any` and `all` belong only at list starts, not on continuation entries.
