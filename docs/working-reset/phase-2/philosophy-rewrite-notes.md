# Notes on the philosophy rewrite — 2026-07-27

What changed in `docs/philosophy.md`, for your review. The rewrite starts from the working tree
with the phase 2 mechanism corrections applied, and keeps all of their substance: proof instead of
re-checking, the four kinds of declared premise, values held to their constraints as they enter,
and the runtime verifying a definition's proof before governing under it.

## Cut

- **The opening line.** "A domain integrity engine for .NET" is gone. The document now opens:
  *"Precept governs what a business entity's data is allowed to become."* .NET appears once in the
  first section, as a plain fact about how the engine ships, and again in the positioning bullets.
- **The four-operation list** (CreateInstance / Inspect / Fire / Update). Replaced by one paragraph:
  a closed set of things can happen to an entity — created, moved by an event, edited where declared
  editable, previewed — and the set is closed because an operation exists only if its proof does.
  No method names anywhere; "Fire/Inspect/Update at the application boundary" is also out of the
  authoring section.
- **The explicitly-forbidden-event sentence.** It was operation-list detail, not philosophy. The
  spec carries it. Flag if you want it back.
- **The Terraform / SQL / DDD precedent list** under "States are optional." The claim now stands on
  the commitments themselves rather than citing prior art. The research link still covers it.
- **The "If you're writing service-layer code…" paragraph** — folded into the hand-rolled
  governance bullet, which already said it better.
- **One of the two approximation paragraphs** — they said the same thing; merged into one.
- **The "What makes it different" bullet list as a structure.** Its content survives as the
  "Prevention, not detection" section's prose; the closing sentence carries the full defect list so
  compile-time structural checking is still stated in one place.

## Kept, deliberately

- **All nine positioning comparisons and the three complements**, tightened but not thinned — you
  said the comparison earns its place, and it does.
- **Overflow, unqualified, in two places** (the calculation paragraph and the closing defect list),
  exactly as ruled: target stated, no caveat. The trust-boundary claim stays unqualified too —
  "structurally impossible" appears with no qualifier, per your reservation of that decision.
- **"The hierarchy of concepts"** section largely intact — it was already the most philosophical
  part of the document. The workflow-is-one-dimension paragraph is folded into the states paragraph.
- **The mandatory reason on every rule** — one clause now, but still stated as a language refusal,
  not a convention.

## Look at twice

1. **The closed-set paragraph** is the replacement for the operation list. It says less than the
   list did (no per-operation detail) and claims more ("there is no fifth thing"). If any operation
   detail was doing work for a reader you care about, this is where it went missing.
2. **"Every adjacent tool detects… Precept prevents"** opens the guarantee section harder than the
   old text did. I believe the positioning bullets back it, but it is the most quotable and most
   attackable sentence in the rewrite.
3. **The certificate stays unnamed.** The runtime-verifies-its-own-proof paragraph survives in full,
   but as with the phase 2 text, the word "certificate" and the load-time mechanism stay in the
   spec. That was the applied choice and I did not reopen it.

Word count: 3,309 at HEAD → 2,622 now. Nothing committed.
