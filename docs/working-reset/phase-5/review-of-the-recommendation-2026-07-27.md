# Review of the overflow and divisor-non-zeroness recommendation

**Reviewing `recommendation-overflow-and-divisor-nonzero-2026-07-26.md` (2,446 lines). Written
2026-07-27.**

Six independent reviews ran — technical soundness, citation fidelity, claims about current compiler
behaviour, fidelity to what you actually ruled, an independent re-derivation from the underlying
evidence, and what is missing. Every finding was then put to a reader whose job was to refute it, and
roughly a third did not survive. What is below survived, and I re-verified the load-bearing pieces
myself against HEAD, against canon, and against the compiler. Where I checked something myself I say so.

---

## 1. The verdict in three sentences

The recommendation is sound with corrections: its central rule — prove the final stored divisor, not the
operands, and put a representability obligation at every operation — held up against every attack, and
none of the eight build items has to be withdrawn. One finding blocks, and it is not a technical error
but a governance one: a clause you explicitly disclaimed is cited nine times as your ruling and used to
schedule a modifier-semantics change as mechanical catalog work. The rest is a long tail of real but
correctable defects, of which the sharpest is that the document recommends an author write `5e-2` and the
compiler reads that literal as zero.

---

## 2. What has to change before it is acted on

### 2.1 Build item 8 is scheduled off a label you marked "not mine"

**What is wrong.** The document attributes the not-`NaN` clause to you six times, three of them as "the
owner's non-finite ruling of 2026-07-21" (lines 52, 1579, 1718, 1821, 2078, 2295). The phase-1 walk put
that exact item to you as item 14; you marked it **not mine**, with the word "derived", and on the plain
re-ask said the question had never been put to you. The walk records the clause as standing "as derived
and forced" — which is a good outcome for the clause, and a different thing from your having ruled it.

The document does distinguish your rulings elsewhere: it cites the walk by name twice, at line 443 (item
15) and line 2190 (item 16), and both of those are genuinely yours. Item 14 is never mentioned. A reader
has no way to tell the three apart.

**Why it matters here rather than as a citation nit.** Build item 8 (lines 2294–2308) says: "This item is
the catalog work that closes the distance between the ruling and the metadata", and then "it is not
behind the item-15 deferral … it belongs beside item 1 rather than after it." The change is that
`nonzero`, `positive` and `nonnegative` mean different things on `number` than on `decimal`.
`primitive-types.md` says of that same change, in its own voice: *"The change to what a qualifier
modifier establishes on the `number` lane is a modifier-semantics change and is designed before it
ships; this section states the target."* Calling it a settled ruling converts a modifier-surface design
decision into near-term mechanical sync and routes it past the pre-design consultation gate — on a label
you have already disclaimed once.

**Evidence you can check.** `docs/working-reset/phase-1/the-walk.md:377–392` (item 14 and its marks);
`docs/language/primitive-types.md:660` (the heading the document quotes) and the "designed before it
ships" sentence twelve lines below it; the six line numbers above.

**The fix.** Two changes, both small. Restate the clause as derived and forced, citing the walk's
own words rather than an owner attribution — the substance survives untouched, because the argument
(`NaN != 0` is true, so an unqualified `nonzero` establishes nothing usable) is what carries it, not the
label. Then move build item 8's modifier half out of "catalog work" and into the design queue, keeping
the requirement half (5d, the not-`NaN` obligation) where it is. That leaves item 8 smaller and honest.

---

## 3. What should change but does not block

### 3.1 The lane-costing note rests on a false comparison, and the workaround it recommends is unsafe

The document says at lines 2140–2144 that a `number` field carrying `rule Baseline >= 5e-2` leaves the
divisor obligation unresolved "where the identical `decimal` definition discharges it through
`CompositionalConstraint`", and concludes there is a lane dimension nobody costed. It then tells the
author to spell the number-lane threshold `5e-2`, because `0.05` is a type error on that lane.

It is not a lane difference. It is how the literal is spelled. I ran the four cells:

- `decimal` + `rule Baseline >= 0.05` → **Proved**, `CompositionalConstraint`
- `decimal` + `rule Baseline >= 5e-2` → **Unresolved**, PRE0083

The two definitions differ only in the literal. The cause is
`src/Precept/Pipeline/TypeChecker.Expressions.cs:203`:

```csharp
_ = long.TryParse(lit.Text, System.Globalization.CultureInfo.InvariantCulture, out var intVal);
```

A literal with no `.` goes to `long.TryParse`, the boolean result is discarded, and any exponent-notation
literal becomes the value **0** everywhere downstream. Confirmed directly: `field Y as decimal <- A /
5e-2` emits *"Division is unsafe: '0' can be zero in the computed expression for field 'Y'"*.

So the recommended spelling at lines 2143–2144 tells an author to write a rule that silently says "at
least zero". The direction of build item 1 is unaffected — teaching narrowing about `abs()` still needs
building on both lanes, and the `number`-lane fractional-literal type error is real and separate — but
the costing paragraph needs rewriting and the workaround needs withdrawing until the literal defect is
fixed. That defect is worth filing on its own; it is not confined to this document's advice.

### 3.2 The runtime-backstop reassurance is false in the shape the document itself uses

Form (g) reassures that the runtime refuses the handler when the rule fires, "so nothing bad is stored."
I reproduced the document's own example: a computed field `field UnitCost as decimal <- ShipmentCost /
EffectiveRate` with `rule EffectiveRate >= 0.05` compiles with zero errors and the obligation Proved via
`CompositionalConstraint` — and computed fields recompute at evaluator step **2g**, while rules evaluate
at **2h**. The division runs before the rule can refuse anything. The document cites that same 2g-then-2h
ordering forty lines earlier to argue a rule *does* govern the recomputed value, and then draws the
opposite conclusion here.

Two corrections to how this was first reported, both in the document's favour. Row actions run at 2e,
before recomputation, so in an action-chain division the rule genuinely did govern the value one
operation earlier. And the per-write establishment duty the document recommends closes the case
outright. What survives is that the sentence is a false statement about runtime behaviour, stated twice,
and it understates a live breach at HEAD as hygiene. One paragraph naming the computed-divides-computed
shape and saying the backstop degrades there from a clean constraint failure to a divide-by-zero fault
would close it.

### 3.3 The item-15 deferral is stretched over the `decimal` lane without saying so

Both grounds the document cites are narrower than the use it makes of them. `the-walk.md:394` reads "The
arithmetic overflow model — integer, temporal, and number-lane infinity — is deferred past the MVP", and
`primitive-types.md` § Open Questions closes with "Until it is, **`integer`** arithmetic that could
exceed the 64-bit range carries no live compile-time guarantee", with the number-infinity and temporal
paragraphs explicitly riding on the integer question. That question turns on a choice `decimal` does not
have: fixed-width and prove, or arbitrary precision. `System.Decimal` throws, always.

This cuts both ways and I want to be fair to the document. The consequence you assented to was put in
lane-general terms ("a program can overflow and nothing stops it"), and the claims the ruling qualifies —
`philosophy.md:53`, `precept-language-spec.md:110` and `:112` — say "arithmetic overflow" with no lane
restriction. So the document's reading is defensible, possibly the better one. What is not defensible is
resolving the ambiguity silently in the permissive direction, in a document that routes three other
overflow decisions to you. The document says out loud at line ~2374: "On `decimal` a deferred overflow
means a runtime fault the author will meet in testing" — a fault firing in a definition that compiled
clean, which is the Principle 11 breach the deferral was meant to authorize away, and for `decimal`
nothing authorized it. The resolution decides whether items 5a–5c are MVP work. It should be a fourth
bullet in § Work that would require owner consultation, not a silent reading.

### 3.4 Build item 4 is routed to you as owner-gated when the document elsewhere says it is not

Lines 2363–2364 say "Build items 4 and 5 describe where the model should end up; scheduling any of them
is a decision to close the exemption, and that is the owner's to make." Item 4 is the
interval-representation change, and it is inside the divisor half: form (c) case 2 discharges `nonzero`
by "build items 2 and 4" (line 1149), and its soundness argument is representation-aware — "the floor
`0.005` is far above `decimal`'s smallest magnitude of `1e-28`, so underflow cannot pull the product to
zero." The underflow policy's first requirement (line 856) is item 4's transfer, and final-judgment item
1 is "calculate possible stored values with representation-aware intervals."

The document contradicts itself here: line 448 says "The divisor work does not wait on any of it", and
line 2242 asserts of 5a–5c that "none of them is a prerequisite for the divisor work" while 5a is defined
as the item-4 change. The contradiction proves the consultation bullet is wrong, not that it is harmless
— the consultation section is the one you would read to find out what needs you. Item 5 is correctly
placed there; item 4 should come out.

### 3.5 The SMT prohibition is stated more strongly than canon supports

Line 375 reads "Do not pursue — anything that discharges through an SMT solver. This is settled by canon."
Canon's §0.6 Proof philosophy item 3 is titled "Legible certificates, not opaque solvers" and says "The
search procedure is unconstrained — a strategy may use any method to find a proof — but authority rests
in the certificate, not the search." Under that, an SMT-backed strategy emitting a legible replayable
certificate is admissible, and the document neither says so nor engages with the four-criteria test that
replaced the ban.

Two thirds of the original complaint failed and I record that: §0.8 item 4 does exclude "SMT-style or
theorem-prover-style proof artifacts" by name, so the sentence at lines 249–250 is accurate as written.
And the document's own second requirement — a worst case that does not depend on the data — independently
disqualifies SMT here, so the conclusion does not change. What changes is the ground: cite the
requirement, not a ban canon has retired. (Minor: "§0.6 #3" is ambiguous, since §0.6 has two numbered
lists and the responsibilities item 3 is divisor safety.)

### 3.6 "That also settles the open question" takes a decision the plan reserved for you

Lines 1885 and 42–43 say the underflow policy settles the open question canon recorded against the
`number` lane. Half of it genuinely is settled: both the walk and the plan agree "non-zero times non-zero
is non-zero" is unsound and cannot be the answer, and the document states that half correctly. But the
plan's row reserves the other half explicitly: *"What is genuinely open is the cost: if a division may
only be discharged from something declared on the divisor itself … then authors who divide by a computed
product must restate the constraint on the product"* — and it stalls "Phase 5's discharge procedure for
the fault family."

The document is in fact the preparation the walk asked for — the seven forms, the patterns and the worked
examples are exactly the author-cost material that was missing. So the fix is small: present it as
prepared options with costs and refusal messages and name whose decision it is, rather than as an answer.
It is currently absent from § Work that would require owner consultation, which lists three overflow
decisions and a temporal one.

### 3.7 `ensure` is absent from the entire document

`grep -c ensure` on the recommendation returns **0**. Canon names both `ensure` forms as divisor proof
sources: `temporal-type-system.md:706` lists them, `precept-language-spec.md:994–995` gives the grammar
for all four scopes, spec line 206 names "a rule, an ensure, or a guard" as the proof paths, and line 247
names "an explicit rule or ensure" as the author's hand-written substitute. So "there is no eighth route
worth listing" is wrong.

The state-scoped form covers the exact case the document says nothing else reaches: line 1497 concedes
that a floor holding on some paths and not others must fall back to a repeated per-division guard, and
`in State ensure Field > 0` states it once. Build item 3 says the duty should be built "as one duty
covering both declaration forms", so an implementer reading this builds modifiers and rules and not
ensures. Nothing in the document becomes untrue — `ensure` fits the prove-the-final-stored-divisor rule
cleanly and the machinery is the same establishment-and-preservation duty already scoped — but it is a
missing route in an enumeration presented as closed. (Your item-7 ruling names five constraint forms;
item 3's "both declaration forms" should say two of five.)

### 3.8 Modulo carries the identical obligation and is nowhere in the model

Verified in the catalog: `IntegerModuloInteger`, `DecimalModuloDecimal` and `NumberModuloNumber` each
carry `NumericProofRequirement(..., NotEquals, 0m, "Divisor must be non-zero")`, and **none of the three
has an `IntervalTransfer`** while all three division counterparts do (`src/Precept/Language/Operations.cs`
— modulo at lines 128, 159, 190; the transfer list has division at 126, 157, 188 and no modulo).

Build item 5c names "the missing transfers, `pow` above all" and leaves three unnamed. The document
mentions modulo three times in total — the technique table at line 280, the general rule at line 861
("division or modulo by the result must be proved against the final stored interval"), and one example
rule at 1469 — and never in the seven forms, the patterns, the comparison tables or the final judgment.
`NumberModuloNumber` existing means `x % 0.0` produces a `NaN` on that lane, which the `number` section
does not cover either. One mitigation: `primitive-types.md:207` has the same gap, omitting the
divisor-safety note on the `integer % integer` row that it carries on the division row above.

### 3.9 Lane-crossing functions are obligation sites canon already names, and the enumeration misses them

`primitive-types.md:624` makes `round(value, places)` an explicit `number → decimal` bridge, and `:671`
says conversion overflow on `floor`/`ceil`/`truncate`/`round` is a runtime fault that is "statically
preventable when the proof engine can bound the expression range." Canon also carries a table (`:652`)
titled *Non-finite `number` inputs* stating that `+∞`, `-∞` and `NaN` reaching those functions produce a
runtime fault, and says explicitly that the trap is defensive redundancy and that under Principle 10 the
operation is proven safe or the definition rejected.

The recommendation contains `floor(` once, as a lane-crossing route with no obligation attached, and
`truncate` never. Build item 5b is worded "at arithmetic operations", which does not obviously reach a
conversion function. The claim needed is the same one 5b and 5d already produce — this value is finite,
not `NaN`, and inside the target range — just stamped at a different site, and the machinery exists,
since the document cites `sqrt`'s stamped precondition twice. The document's line 49 stays correct: canon
does confirm non-finite values cannot reach `decimal`-lane rounding.

### 3.10 The `duration` divisor lane is unaddressed, and canon's model for it would be silently overturned

`temporal-type-system.md:698–707` and `:738`: `duration / integer|number` and `duration / duration` both
carry divisor safety; `min` and `max` are compile errors on `duration` and there is no `maxplaces`, so
forms (c), (e) and (f) do not exist there at all. More seriously, canon's rule for compound divisors on
that lane is "assume satisfiable — no diagnostic", which is the opposite of prove-or-reject and would be
overturned by this recommendation without anyone saying so. The document says `duration` once (line 2379,
about `instant ± duration`) and never says C92 or C93.

The money and quantity divisor rows in `business-domain-types.md` are a weaker complaint — those types
are decimal-backed with `min`/`max` available, so they inherit the decimal reasoning almost entirely and
the gap is presentational. The `duration` lane and the unreconciled compound-divisor concession are the
real content.

### 3.11 The UCUM paragraph's conclusion does not follow from its premise

Lines 704–706 argue that because bounds are normalized to UCUM base units, "a calculation quoted in
milligrams starts six orders of magnitude closer to the ceiling than the same calculation quoted in
kilograms." Normalization is what defeats this. `business-domain-types.md:428` describes it as happening
at interval containment, on *both* sides of the comparison, so `5 kg` and `5000000 mg` both become 5000
grams and there is no difference between the spellings at all. The only surviving effect is a uniform
factor of 1000 for every mass, because UCUM's mass base is the gram, and that factor does not depend on
what the author wrote.

The conclusion is probably still true for a different reason: a field declared `in 'mg'` *stores*
milligram numbers and carries them through the arithmetic, and it is the stored value the overflow
ceiling applies to — which is the document's own central commitment. The paragraph needs that argument.
The authoring convention at lines 2106–2108 repeats the same non sequitur, in the summary an author is
most likely to read.

---

## 4. Minor points

- **Totality is cited twice as "§0.6 #10"** (lines 1578 and 1613). It is Design Principle 10 in §0.1, at
  `precept-language-spec.md:110`; §0.6's item 10, at line 212, is tautological guard detection. The
  quoted text is verbatim correct — only the address is wrong, in both places. I checked both lines.
- **Lines 1809–1812 attach the wrong letters to both forms.** Form (a) is the `abs(x) >= k` magnitude
  guard (line 1084), form (b) is `nonzero` on a derived field (line 1098), and the bare `!= 0` guard is
  not one of the seven lettered forms at all. The surrounding prose describes the shapes correctly, so a
  reader gets the right substance with the wrong labels.
- **Line 728's mechanism is wrong.** `System.Decimal` is 128 bits — a 96-bit coefficient plus a separate
  32-bit flags word holding the sign and a scale byte. The scale costs none of the 96 bits. The
  conclusion in the same sentence is right for a different reason: the coefficient must hold every
  significant digit, so a large integer part plus many fractional digits together exceed its ~28–29
  digit capacity.
- **"Eleven orders of magnitude" at line 1931 matches nothing in the passage.** The table two lines above
  gives 7.9e28 and 1.8e308, a gap of about 279 orders of magnitude; below the floor it is about 295. The
  point is correct and much stronger than the figure attached to it.
- **`pow(NaN, 0)` is 1.0**, so the absolutes at lines 1726–1729 ("any arithmetic operation with a `NaN`
  operand produces a `NaN`") are false. `Functions.cs` declares a `pow` overload with a `number` base and
  `integer` exponent and no non-negativity requirement, and .NET follows IEEE here. It weakens the hazard
  rather than widening it, and the real argument is the second bullet at line 1730 — no bound detects a
  `NaN` — which is untouched. Worth qualifying so the passage does not rest on an absolute a reader can
  break in one line.
- **The gradual-versus-abrupt underflow contrast at lines 1870–1873 is not the contrast stated.**
  `System.Decimal`'s scale caps at 28, so the finest grid is `1e-28` for every value below ~7.92, every
  representable decimal below that is a multiple of `1e-28`, and precision falls with magnitude across 28
  decades. That is gradual underflow by another name. "Reaches zero in one step" is true of binary64 from
  its smallest subnormal too, so it does not distinguish the lanes. Policy is unaffected, and the
  document is accurately reporting the underflow research's own self-assessment.
- **Line 2236 attaches the wrong test result to the wrong definition.** `[0.01 .. 300000000000]` and "and
  discharges" belong to the three-factor chain at line 570 (10000 × 1000000 × 30 = 3e11, which I
  reproduced). The four-operation chain at lines 544–559 reports `[0.01 .. 1.99e13]` by the document's
  own arithmetic and does not discharge at all — the next sentence of 5c says `pow` blocks it. A reader
  could take 5c as evidence the compounding chain proves today, which is the opposite of what 5c says.
- **Overflow pattern 4** (`rule Pallets * UnitsPerPallet <= 5000000`) performs a multiplication of its
  own, at step 2h, over the values just written. The document never says how *that* multiplication's
  representability obligation under 5b is discharged, nor how a write is proved to preserve the rule
  without evaluating the same product in a guard. There is a route — write the preserving guard in
  division form, `SetPallets.N <= 5000000 / UnitsPerPallet` — so this needs a paragraph, not a
  withdrawal. (The stronger objection, that the rule is defeated by the document's own ordering argument,
  does not hold: a rule over the operands is a standing statement about inputs before any operation runs,
  which is a different thing from a rule over a result computed at 2g and checked at 2h.)
- **The divisor property at line 1768** — finite, non-zero, not `NaN` — is not sufficient on its own:
  `1e300 / 1e-300` satisfies all three and produces `+∞`. It is a wording fix at 1768–1770, 2292–2293 and
  2427–2429 (say this is what the *divisor* must satisfy and that the result's finiteness is item 5b's
  job), not a change to what gets built. The necessity half of the original complaint fails, since a `+∞`
  divisor is already refused upstream by 5b.
- **A transition-row guard bounding the operands is missing from the overflow forms table.** The divisor
  side lists a row guard as form (a); the overflow table has ten rows and no guard row. The brief's stated
  reason — "by the time an operation could be guarded it has already had to produce its result" — is
  about guarding the *result*, and the document makes exactly this distinction for `rule` four hundred
  lines later without applying it to guards. Two things keep it small: pattern 2's worked example already
  uses an operand guard, and the form reaches only arithmetic inside the guarded row's own action chain,
  since a computed field recomputes on every operation. Say both rather than leave the route unmentioned.
- **"Immediately before it is used" (lines 1092, 1405) overstates.** Canon already answers the headline
  case — `precept-language-spec.md` §0.6 Proof principle 7: "when a field is reassigned, prior proof facts
  about that field are invalidated before the new assignment's facts are stored". The document's own
  governing rule at line 878 is correctly scoped ("at that program point"). What is left is loose prose,
  plus the state-exit-action window between evaluator steps 2a and 2e which is never mentioned anywhere.
- **`mincount 1` is unnamed as a collection divisor route.** `collection-types.md:143` says `.count`
  returns 0 for empty and carries no proof requirement, and `:354` records that a statically-literal
  `mincount 1` already discharges a different access obligation. The ordinary average is already covered —
  `when Lines.count > 0` is form (a) — so this is one missing declared-constraint route with an existing
  precedent, not an unhandled class of divisor.
- **Line 601–602's escape hatch misdirects.** Pattern 2 opens with a declared `max` (line 609), and
  pattern 3 alone can never discharge anything, since with no ceiling anywhere every interval stays
  unbounded above. Pattern 4 is a genuine alternative for a domain whose ceiling sits on the combination.
  The document does state the consequence plainly elsewhere (lines 739, 843, 2096), so this is one
  inaccurate sentence of author guidance, not a hidden obligation.
- **The `abs()` disjunction equivalence is priced at zero.** Lines 2134–2137 call
  `rule Rate >= 0.05 or Rate <= 0.0 - 0.05` "the same magnitude claim written without the function call"
  and then price reading it as "a small addition to the same code". Your item 13 says an equivalence
  between two differently-written expressions carries a written argument. This one is a direct logical
  step over what the author wrote rather than an algebraic rewrite, so it is nearly fine — one sentence
  naming the argument owed would close it.
- **Addition, subtraction and unary negation are absent from the authoring guidance**, which reasons about
  products throughout ("bound every factor", "every field that takes part in a multiplication chain").
  `primitive-types.md:203–208` lists checked overflow on `integer + integer`, subtraction, division and
  `-integer`; negating the most negative 64-bit value throws, as does dividing it by −1. The general rule
  at line 418 covers them, so this is a scope error in the guidance, not in the model.
- **Two deferrals carry no reopening condition** — backward reasoning through `round()` (line 2274) and the
  general underflow obligation kind (line 2317) — where line 356 and line 2362 give theirs. "If it turns
  out to be common in real definitions" has no method behind it, since counting sample files establishes
  nothing and a refused definition is the only evidence such a rule generates. Arguable that no condition
  is owed, since both are argued on grounds that do not expire.
- **No diagnostic text is drafted for any new obligation**, and none is more awkward than 5b's, which has
  to point at an anonymous sub-expression and name which field the author should bound. The document says
  at line 3 that it is a recommendation and not a locked design, and drafting messages is `/design` work,
  so carry this forward as a design risk rather than a defect. (The related complaint that backward
  propagation is listed and never dispositioned is wrong — line 391 assigns and declines it.)

---

## 5. What the review did not cover

Named honestly, because several of these matter more than some of what is above.

- **Author cost.** Nobody estimated how many real definitions this recommendation would refuse, and no
  legitimate method exists — counting sample files establishes nothing, and a refused definition is the
  only evidence the rule would generate. This is the half of the underflow question the plan reserved for
  you (§3.6), and the review can neither answer it nor tell you the document's answer is wrong.
- **Whether the seven divisor forms are exhaustive as a set.** Two omissions were found by looking for
  specific constructs (`ensure`, `mincount`). Nobody derived the space of forms independently and checked
  the document's list against it, so there may be more.
- **The underflow policy on its own terms.** It was read for its `decimal`-versus-`number` contrast and
  for whether it settles the open question. Its actual soundness — whether the stated requirements are
  sufficient to keep a product away from zero — was not attacked.
- **Implementation cost and sequencing.** The eight build items carry no estimates and nobody tested the
  claimed orderings ("do 5c in the same pass as 5b", "item 8 belongs beside item 1"). Whether item 4's
  representation-aware intervals are a week or a quarter is unknown and unexamined.
- **The temporal flag at the end** (`instant ± duration` leaving the calendar range) got no review at all,
  beyond noting that `duration` divisors are missing from the body.
- **The two research documents this recommendation combines.** Every surviving citation finding is against
  canon. Nothing checked whether the finite-representation and solver-free-analysis research is
  represented faithfully, so I cannot tell you whether that dimension was read and found clean or not read.
- **The two earlier phase-5 notes** the document merges were not diffed against it. Whether the
  combination dropped anything is unknown.
- **Runtime behaviour.** The runtime is unbuilt, so every claim about what happens at execution rests on
  `evaluator.md` and the compiler. The step-ordering findings in §3.2 are as good as that document is.
- **Diagnostic wording and acceptance criteria** — out of scope for a recommendation, and not written.

---

## 6. Where the independent re-derivation agreed

It did not endorse the document. Its brief was to derive the answer from the underlying evidence without
following the document's reasoning, and everything it produced that survived refutation is a
disagreement — §3.2, §3.3, the Totality pointer, and the missing operand guard. There is no record of it
reaching the document's central rule independently and confirming it, and that absence is worth stating
plainly: the strongest claim in this recommendation has not been independently re-derived. It has only
survived attack, which is a weaker thing.

Two convergences are real and worth recording, because they were reached by separate readers who did not
see each other's work:

- **The step-2g-before-2h error** (§3.2) was found by the technical-soundness reading of form (g) and
  independently by the re-derivation, which then reproduced it against the compiler. Two routes to the
  same defect, one of them empirical.
- **The Totality pointer error** was found by the citation reading and independently by the re-derivation.
  Trivial in itself, but it means the document was chased to `precept-language-spec.md` twice by different
  readers and landed wrong both times.

Everything in §3.1 and §3.2 I reproduced myself against HEAD through the compiler; §3.7, §3.8, §3.9,
§3.10, §3.11 and the Totality pointer I verified by reading the cited canon and catalog lines. The rest I
am relaying with its evidence attached.
