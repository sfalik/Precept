# Kramer: from-clause comma completions

Date: 2026-05-16T08:23:29.087-04:00

## Root cause

`from off, ` was not a missing completion catalog entry. The slot resolver treated `,` as the current significant token, failed to map that punctuation back into the `StateTarget` slot, and fell through to `TopLevel`. Because top-level completions are now correctly gated to whitespace-only line starts, the resulting list at that cursor site was empty.

## Durable rule

Comma-delimited continuation positions must stay on the owning slot lane. For state-target lists, recover `InStateTarget` after a completed state name plus comma, suppress wildcard `any` on continuation entries, and filter out state names already listed in the same `StateTargetSlot`.
