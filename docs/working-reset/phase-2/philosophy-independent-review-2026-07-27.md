# Independent review of the reworked `docs/philosophy.md`

**Status:** Opinion — 2026-07-27. Written by Frank (Lead/Architect), on request, as a fresh
cold read of the document after its recent rework (`bcbe7c66`, `e7c1255f`, `20021deb`,
`5e77865a`), with the intent behind those commits set aside so the document could be judged
on its own merits. Advisory only — no edits made to `docs/philosophy.md`. Two of the fix
items below are explicitly flagged as owner-level, not mechanical.

**Method:** Read the current `docs/philosophy.md` in full, then the diffs of the four commits
that reworked it. Spot-checked the load-bearing claims against the compiler and runtime —
via `precept_compile` probes and a grep of `src/` — rather than trusting the document's own
account of what the runtime does. A second pass then evaluated the document specifically as
a philosophy: whether its convictions are strong enough to decide a hard future case, or
whether a motivated future contributor could rationalize around them.

---

## 1. Internal consistency

**Mostly tight. Two tensions:**

- **Line 29 vs. line 71.** Line 29 opens: "Every adjacent tool detects: it checks data at a
  boundary, when called, and reports what it finds." But line 71 concedes SQL CHECK
  constraints "are the original declarative constraint mechanism, and they work" — and they
  *prevent*: a violating row cannot be committed. The comparator section is honest; the
  section opener's universal claim ("every adjacent tool detects") is not quite true by the
  document's own later admission. The real differentiator (line 71 has it right) is *layer
  and inspectability*, not detection-vs-prevention for that one comparator.
- **Line 17, "every possible event, from any position — without executing anything."** The
  runtime inspection API (`Version.InspectFire` / `InspectUpdate`,
  `src/Precept/Runtime/Inspection.cs`) previews from the position of a version you hold.
  "From any position" reads as if you can ask about a state you're not in; you can only get
  there by holding or restoring a version at that position. Mild overstatement, not a
  contradiction.

Approximation vs. exactness (line 21) is reconciled explicitly ("Exact stays exact;
approximation is declared") — no tension. Stateless treatment is consistent throughout
(lines 11, 51–53) and matches the spec (`precept-language-spec.md:1789`).

## 2. Claims vs. reality

- **Line 13, "Every rule carries a mandatory reason" — verified true.** A rule without
  `because` fails with PRE0009 (probe confirmed).
- **Lines 37 & 41, "no overflow… no rule that any operation can break" — currently false in
  the shipped compiler.** Reproduced: `field Bal as decimal default 0 nonnegative max 100`
  with `on Deduct -> set Bal = Bal - Deduct.Delta` (`Delta` bounded to `max 200`) compiles
  with **zero diagnostics** — the interval obligation is marked **Proved** against declared
  bounds `[−∞ .. 100]` and a computed interval of `[-200 .. 100]`. `nonnegative` contributes
  nothing, and one event drives `Bal` to −200. This is the known proof-engine gap `CLAUDE.md`
  documents. The commit message on `5e77865a` shows the owner already knows: "Overflow is
  still listed unqualified and the headline guarantee still carries no boundary. Both remain
  the owner's, and both are still open." A philosophy doc may state commitments — but line
  41's "A definition that compiles without diagnostics therefore has none of these defects"
  is written as present-tense fact and is presently falsifiable in one probe.
- **Line 39, "The proof travels with the definition, and before an engine will govern under
  it, it verifies that proof for itself" — not implemented.** `Precept.From(Compilation)`
  (`src/Precept/Runtime/Precept.cs:35`) checks only `compilation.HasErrors` and returns. No
  certificate type exists anywhere in `src/` (grep: zero hits for `Certificate` in `*.cs`).
  `docs/runtime/runtime-api.md:254` describes the same certificate gate as if built. This is
  the clearest aspirational-as-if-implemented passage in the document.
- **Restore semantics (line 17)** — matches `runtime-api.md:277` and the code: restore does
  not re-validate. Consistent.
- **Determinism, one-file completeness, language server + MCP server (line 90)** — all check
  out cheaply.

## 3. Positioning and comparators

The "Where Precept stands" section (lines 59–82) is strong: each comparator gets a fair
description of what it does well, and the failure-mode example (the `CancelledOrder` with
`Total > 0`, line 65) is concrete and effective. The complements section (workflow
orchestrators, event sourcing, ORMs) prevents the doc from overselling. Two notes:

- The DB-constraints entry undercuts the section's own opening (see finding 1, first bullet).
- **Broken link, line 82**:
  `[Entity-First Positioning Evidence](research/philosophy/entity-first-positioning-evidence.md)`
  — the doc lives in `docs/`, so this resolves to `docs/research/...`, which doesn't exist.
  Should be `../research/philosophy/entity-first-positioning-evidence.md`.

## 4. Clarity and plain language

Clean. No banned vocabulary anywhere — no "source of truth," "load-bearing," bare "surface,"
"witness," "orthogonal," etc. (grep confirmed zero hits). The prose is dense but plain;
metaphors ("coordinate system," "contract") are defined where introduced. The three-paragraph
split of the closed-set passage (from `5e77865a`) works — line 19 now lands as intended. The
"Who authors" and "What Precept refuses to do" sections are the best-written parts of the
document.

## 5. Gaps

- **The guarantee's boundary is undescribed.** The document never says where the guarantee
  stops: restored data is trusted (spec §0.7 owns this), and calendar/host-boundary concerns
  (samples explicitly note "Precept does not expose `today()`") are outside it. The commit
  message admits "the headline guarantee still carries no boundary" is open with the owner.
  This is the one substantive missing section, and everything else in `docs/` assumes the
  boundary exists.
- Minor: the file's declaration inventory (line 13) omits computed fields, guards, and
  actions, all of which are spec-level constructs the same paragraph's peers get named for.
  Fine for a philosophy doc; noting for completeness.

## 7. Strength and boldness of conviction

*(Second-pass angle, requested after the first read: evaluate the document as a philosophy
— strong, assertive, bold enough to guide future authors and agents deciding hard cases
nobody has asked yet — not just as a spec-adjacent doc.)*

**Overall: this is a genuinely assertive document — closer to a creed than a spec preamble —
and its strongest passages will decide future hard cases correctly. But its rhetorical
weight is misallocated, and there is one hole a motivated future contributor could route a
bad decision through.**

**What is strong enough:**

- The refusal section (lines 98–102) is the document doing its job. "Precept has no escape
  hatch. No way to suspend a rule in production, no annotation that says trust me, no
  construct that compiles with its obligations unproven, no warning where an error belongs"
  — each clause pre-answers a specific future rationalization. Test cases: *"best-effort mode
  trading a guarantee for speed"* → killed by "no way to suspend a rule in production" plus
  the closing decision rule, "A smaller language in which every rule is a guarantee is worth
  more than a larger one in which every rule is a hope" (line 102). *"Migration/adoption mode
  that downgrades unproven obligations to warnings"* → killed by name: "no warning where an
  error belongs." That clause is exactly the kind of foresight a philosophy needs.
- The AI-authoring case is decided, not hedged (line 94): "Neither can produce a definition
  the compiler has not proven, because it is the same refusal — the guarantee does not
  weaken when an agent writes the rules." A future *"let the LLM emit definitions on a fast
  path without static verification"* proposal has no room here.
- The closed-set paragraph now standing alone (line 19) is a conviction stated as a
  mechanism: "an operation nobody declared has no such proof and no existence." And line 41's
  "The difference is not which bugs get caught. It is which bugs can exist" is the best
  sentence in the document and correctly placed as a section close.
- The tradeoff is owned, not softened (line 102): "some things that can be written in another
  tool cannot be written here at all… That is expressiveness surrendered on purpose." No
  hedging.

**Where it is not strong enough:**

- **Determinism is stated at the rank of an incidental.** It gets half a clause buried
  mid-paragraph (line 21: "The engine is deterministic — same definition, same data, same
  outcome —") sharing a paragraph with inspectability *and* approximation honesty. It never
  says what determinism *rules out*: no clock, no randomness, no external lookups inside
  evaluation. Concrete test the document fails: the samples themselves work around the
  absence of `today()` ("Precept does not expose `today()`, so calendar arithmetic relative
  to 'now' lives at the host boundary" — `samples/academic-course-registration.precept:44–46`).
  A future proposal to add `today()` to the language would be a determinism violation, and
  nothing in `philosophy.md` explains *why* it's absent — a plausible-sounding "authors keep
  asking for date arithmetic" proposal could sail past this document. Determinism deserves
  its own paragraph with the same "here is what this forbids" structure the refusal section
  uses.
- **The trust model at the embedding boundary is unstated, and it is the routing hole.** Line
  15 says "Only a closed set of operations can ever change a governed entity's data," and
  line 23 hands storage and presentation to the application — but the document never says
  whether the guarantee is a claim against *cooperative* host code or *any* host code, and
  never rules out a privileged API. A future contributor proposing "an unsafe bulk-mutation
  fast path for the ORM integration — the host is trusted anyway, look at restore" has a real
  argument: restore (line 17) already trusts persisted data without re-validation, so "trusted
  host writes" has an in-document precedent to stretch. The refusal section's "no way to
  suspend a rule in production" *should* cover this, but it reads as being about the language
  and the definition, not the embedding API. This is the same missing-boundary gap flagged in
  point 5, now with a sharper consequence: it isn't just undescribed, it's the one place a
  rationalization fits.
- **Rhetorical weight is inverted.** The comparator section is ~24 lines and eleven bullets
  (lines 59–82) — careful, defensive, thorough. The refusal section — the part a future
  author will actually decide against — is two paragraphs. The document spends more words
  distinguishing Precept from Drools than it spends on the commitments themselves. The
  comparators earn their place, but the commitments should not be outweighed by them.
- **One claim is bolder than the product and should be either owned or re-anchored:** line
  39's runtime proof verification (see point 2). A conviction document may promise; it must
  not describe. As written, a future reader will *rely* on a mechanism that doesn't exist —
  which is the opposite failure from hedging, but equally corrosive to the document's
  authority: the first time a reader catches the philosophy asserting something false, every
  other assertion weakens.

**Underselling:** minimal. If anything, one genuinely distinctive property is stated but not
claimed as distinctive: the mandatory `because` on every rule (line 13). No comparator in the
positioning section requires a rationale as a syntactic precondition; the document mentions
it in one sentence and never returns to it, though it is arguably the clearest expression of
"the primary author is the rule-owner" the language has.

## 6. Overall verdict

**Good shape — targeted fixes, no structural problem.** The document reads coherently cold,
its identity claims are consistent, and the recent rework visibly improved it. Fixes, in
priority order:

1. **Line 39** — the runtime certificate-verification paragraph describes an unbuilt
   mechanism as fact. Either move it to explicit-commitment phrasing or wait for the
   certificate work (`docs/Working/certificate-steps-membership-2026-07-12.md`) to land.
   *Owner already owns this class of issue; needs a decision, not a silent edit.*
2. **Line 82** — fix the relative link (`../research/philosophy/...`). Mechanical.
3. **Line 29** — soften "Every adjacent tool detects" so it doesn't contradict the
   DB-constraints entry at line 71 (e.g., scope it to code-layer tools).
4. **Line 17** — "from any position" → "from the entity's current position" (or whatever the
   inspection API actually promises).
5. **Lines 37/41 (overflow, headline boundary)** — already open with the owner per
   `5e77865a`; nothing new to add except that the gap is one-probe reproducible today, so it
   should stay near the top of the owner's queue.
6. **Give determinism its own paragraph** stating what it forbids (no ambient time, no
   randomness, no external evaluation inputs), so proposals like `today()` are decided by the
   document rather than argued past it.
7. **State the trust model at the embedding boundary** — one paragraph saying whether "no way
   around the contract" binds the host API surface too, and why restore's trust of persisted
   data is not a precedent for trusted live writes. This closes the only rationalization route
   found that the document, as written, does not shut.

Per the philosophy-gate rule, no edits were made to `docs/philosophy.md`. Items 1, 5, 6, and 7
are owner-level — they touch philosophy content and need Shane's direction, not a silent edit.
