# Contradictions and open readings found by the block inventory, 2026-07-14

**Status**: extracted 2026-07-25 from `docs/Working/exhaustive-gap-analysis-2026-07-14/coverage/`
before that folder is deleted. Never reviewed by anyone.

This is an input to **phase 2**. The nine agents that walked the canonical documents on
2026-07-14 flagged eleven items. Five are places where the canonical documents contradict
themselves. **Four of the five were ruled on 2026-07-14 and the documents corrected in commit
`90f0792f`** — phase 2 does not re-litigate those; they are here so a reader who meets the
same text knows it was already settled. **The fifth was never ruled and is still live at
HEAD.** The remaining six are not contradictions; they are readings the walk could not settle
from the document alone.

Every citation below was re-checked against HEAD on 2026-07-25 unless the row says otherwise.

---

## The live one

### 1. `temporal-type-system.md` — the `(none)` expected-type row against the type-resolution rules

**Still live at HEAD. Never ruled.**

`temporal-type-system.md:143` is the last row of the context-aware classification table:

| Expected type | Calendar units (day, week) | Time units (hour, min, sec) | Mixed units | Months/years with Duration target |
|---|---|---|---|---|
| (none) | Period path | Duration path | TEMP005 compile error | N/A |

So with no expected type, a calendar-unit quantity silently takes the period path and a
time-unit quantity silently takes the duration path.

The later and more specific section, *Type resolution rules — context-dependent*
(`:1167` onward), says the opposite for both. `:1182`:

> | No context / ambiguous | **compile error** | "Can't determine the type of `'3 days'` without context. Use it in an expression like `DueDate ± '3 days'` so the type is clear." |

and `:1208`, for the time units:

> | No context / ambiguous | **compile error** | "Can't determine the type of `'3 hours'` without context." |

One says silent default, the other says compile error, for both halves of the row. The walk's
cells followed the later section — `temporal/date/literal/no-context-rejected` and
`temporal/duration/literal-ambiguous-no-context` both expect a rejection — but the agent
declined to resolve a document-internal disagreement on its own, which was the correct call.

Verified 2026-07-25: `docs/language/temporal-type-system.md` is clean against HEAD, and both
texts are present at the lines above.

**How this survived eleven days.** The agent's structured return did report it. Its
`stopAndFix` array held one entry, opening with the words:

> "None outright, but flagged for Phase 2: the F-LANG-TEMP-01/02 classification table's
> '(none)' expected-type row (temporal-type-system.md:143) appears to conflict with the later
> 'Type resolution rules — context-dependent' section…"

Its own coverage file then wrote that as "## Stop-and-fix — None." and the rollup at
`coverage-report.md:59` recorded "Temporal's inventory reports **Stop-and-fix: None**". The
hedge was dropped at each step of summarising, and an unruled contradiction inside a canonical
document sat unseen from 2026-07-14 to 2026-07-25. Nothing in the run was wrong except the
summary. The recovered workflow script now carries an instruction to the synthesis step not to
summarise away a hedge, which is a mitigation and not a fix — the general shape of this failure
is a summary standing in for what it summarised.

---

## The four already ruled — do not re-litigate

Ruled by Shane on 2026-07-14, corrections committed in `90f0792f` ("docs(gap-analysis): rule +
fix the four Phase-1 stop-and-fix contradictions"). Each is listed with what the contradiction
was, what was ruled, and what the text says at HEAD now.

### 2. `primitive-types.md` — three incompatible claims about integer overflow

The Approximation Stance table at `:81` said integer was "Arbitrary-precision whole numbers; no
overflow, no rounding". The integer Backing line at `:197` said "Overflow is a type error". The
operators table at `:203` said "Checked overflow". Unbounded, compile-time error and runtime
checked arithmetic are three different things.

**Ruled**: `integer` is fixed-width 64-bit today; the overflow *model* — compile-time
proof-or-reject against adopting an arbitrary-precision representation — is a deferred post-MVP
decision, with arbitrary-precision still a live option. `:81` was rewritten to say fixed-width
and to disclose the deferral; `:197` and `:203` were left as the fixed-width end state.

At HEAD `:81` reads "Fixed-width 64-bit whole numbers; no rounding. Overflow handling —
compile-time proof-or-reject vs. adopting an arbitrary-precision representation — is a deferred
post-MVP decision".

The ruling also parked integer representational-overflow cells as a known build-order gap
rather than a finding.

### 3. `primitive-types.md` — whether `pow` accepts a negative exponent outside the integer lane

`:613` read "`exp` must be non-negative for integer lane". The explicit scoping to one lane left
open whether a decimal or number base accepts a negative exponent.

**Ruled**: no new surface — existing locked precedent already answers it. `:677` admits `pow`'s
decimal overload only because the operation is closed over finite decimals, and a negative
exponent is not (`pow(3m, -1)` is 0.333…), so non-negative holds across all lanes. `:613` was
corrected.

At HEAD `:613` reads "`exp` must be non-negative — negative exponents break decimal-lane
closure."

The ruling left one thing behind for a later phase to check: whether the compiler actually
enforces an exponent-sign obligation today. `Functions.cs` was reported to encode the proof only
on the integer-base overload. That is a compiler-against-document check, not a document
question.

### 4. `business-domain-types.md` — the Implementation Scope bullet against the retired D10

`:1989` said the type checker does an "ISO 4217 minor-units lookup during constraint
resolution", derives a default `maxplaces` from the currency code, and applies "half-even
rounding on all money arithmetic results". That is implicit precision and automatic rounding,
and it contradicted both the locked D10 retirement at `:1760-1768` ("`money in '<Cur>'` carries
no implicit `maxplaces` constraint") and the Approximation Stance at `:174` ("There is no
implicit rounding… the author calls `round(...)` explicitly"). It read as text that was never
updated when D10 was retired.

**Ruled**: stale. Rewritten to explicit-only `maxplaces` and author-invoked rounding.

At HEAD the money row of the stance table says minor-unit data "is **not** automatically
enforced — author writes `maxplaces 2` explicitly when strict mode is required", and the money
constraints paragraph says "`maxplaces` is explicit-only".

### 5. `business-domain-types.md` — a worked example that its own rule rejects

`:1576` illustrated the plain `max N` constraint with `field Score as quantity max 100`. The
bounds-qualification rules at `:412-430` reject exactly that form with `PRE0133
BoundsRequireQualifier`, because `quantity` there carries no `in` or `of` qualifier.

**Ruled**: the example was wrong and was corrected. Note the first proposed fix, `in 'points'`,
fails `PRE0075` because `'points'` is not a valid unit; the committed fix is `in 'each'`.

At HEAD `:1576` reads `field Score as quantity in 'each' max 100`.

---

## The other six — readings the walk could not settle

These are not contradictions between two pieces of canon. They are places where a document does
not say enough for a check to be written against it, or where the check the document implies
does not fit the shape the run was recording. Phase 2 should decide whether each is a canon
question at all; several are questions for later phases instead.

### 6. `primitive-types.md` — claims about produced values, not about accept or reject

Two blocks state what a computation produces rather than whether it compiles: the
rounding-function edge-value table (2.5, −2.5, 3.5, 0.0 and their exact outputs) and the
decimal-exact against number-approximate arithmetic claims (`0.1+0.2`). The run recorded them as
`accept` with the expected value written into the description, because its schema only had
accept and reject. Anything checking these has to compare a produced value, not a compile
result.

### 7. `proof-engine.md` — the Pass 1.5 verdicts are warnings, recorded as rejections

`UnsatisfiableGuard`, `TautologicalGuard`, `UnsatisfiableRule`, `VacuousRule` and
`ContradictoryRule` are classified in `§0.6` items 7 and 8 as `Warning`-severity reports, not
hard errors. The run recorded each as `reject:<Name>`, meaning only "emits that diagnostic", and
said severity should be treated as secondary. Confirmed at HEAD: `proof-engine.md:2537` states
the contradictory, vacuous and unsatisfiable rule family "stay `Warning` per §0.6 items 7/8
'reports'". Whether a structural-soundness finding should be a warning is a canon question;
whether the check records it as a rejection is not.

### 8. `proof-engine.md` — logarithmic units are "outside the proof guarantee" with no stated outcome

`:110` says that `dB` and `[pH]` are outside the exactness guarantee, without naming a
diagnostic or saying whether such a definition is accepted or rejected. The run wrote
`reject:any` and flagged that the real expectation is "not provably exact", which is neither of
the two words available. Confirmed at HEAD: the note is at `proof-engine.md:110` and names no
code and no outcome.

### 9. `proof-engine.md` — key-presence and index-bounds carry no diagnostic code

Both are reported under `GuardInPath` with no explicit numeric code in the document, so those
cells fall back to `reject:any`. Everything else in the unit could be pinned to a named code
from the `§9` diagnostic table.

### 10. `temporal-type-system.md` — DST resolution is a runtime value, not a diagnostic

The locked DST ambiguity resolution at `:1091-1096` says which wall-clock instant a gap or
overlap resolves to. That is a produced value, like item 6, and the run recorded it as
no-behaviour rather than inventing a diagnostic-shaped check for it. It is a real specified
behaviour with nothing checking it.

### 11. `temporal-type-system.md` — a diagnostic cited by a name that may not exist

`:262` cites "`PRECEPT0007` — calendar-variable period rejected as duration" by name. The run
could not confirm that code exists under that name or what its current equivalent is, and used
`reject:any`. Confirmed present at HEAD, `temporal-type-system.md:262`. Whether the name
resolves is a document-against-catalog question.

---

## One more, not counted in the eleven

`spec-preamble-lexer.md:70` records that the `§0.6` implementation-status table names concrete
codes — `PRE0155`, `PRE0159`, `PRE0154`, `PRE0082`, `PRE0153`, `PRE0136` — that the lexer unit
deliberately did not home, on the grounds that they are proof-engine behaviours belonging to
another unit, and asks a reviewer of that unit to confirm they landed there. It is bookkeeping
across two regions of one run rather than a finding about canon, which is why it is not one of
the eleven. It is recorded because the confirmation it asks for was never done, and if the
inventory is run again the same cross-region gap can open.
