## Squared / statistical quantities (like variance)

**What it proves.** When a value is multiplied by itself — a squared deviation, a variance, anything built from squares — the result can never be negative, even when nobody knows whether the underlying value is positive or negative. This proof type teaches the engine to notice "both sides of this multiplication are the same thing, so the result is at least zero."

**What gets rejected without it.** Today the engine estimates each side of a multiplication separately and never notices the two sides are the same value (`NumericInterval.cs:90` — the product of a value spanning −1..1 with itself is estimated as −1..1, when the true answer is 0..1). So under the PROPOSED prove-or-reject design, this definition would be REJECTED — the engine cannot prove the squared deviation is non-negative, so writing it into a field declared `nonnegative` fails (synthetic example, written to match `samples/statistical-process-control.precept` conventions):

```precept
precept ProcessVariance

field TargetValue as decimal default 0
field SquaredDeviation as decimal nonnegative default 0

state Monitoring initial

event SetTarget(Target as decimal)
event RecordSample(Value as decimal)

from Monitoring on SetTarget
    -> set TargetValue = SetTarget.Target
    -> no transition

from Monitoring on RecordSample
    -> set SquaredDeviation = (RecordSample.Value - TargetValue) * (RecordSample.Value - TargetValue)
    -> no transition
```

(today: compiles clean — this posture isn't built; verified via the compiler tool, 0 diagnostics. Today's engine only checks the field's *default* against `nonnegative`, not the computed assignment.)

**How the author clears it today, by hand.** Wrap each side of the multiplication in `abs(...)` — the absolute value. Mathematically identical (|d| × |d| = d²), and it hands the engine exactly what it already understands: `abs` has a hand-written result-range rule that guarantees the result is at least zero (`Functions.cs:44–149`), and multiplying two known-non-negative ranges stays non-negative under the existing multiplication estimate (`NumericInterval.cs:85–102`):

```precept
from Monitoring on RecordSample
    -> set SquaredDeviation = abs(RecordSample.Value - TargetValue) * abs(RecordSample.Value - TargetValue)
    -> no transition
```

Verified: compiles clean on today's engine (0 diagnostics). The same rewrite also helps with *upper* bounds (variance ≤ B) provided the input fields carry declared min/max bands, since the product of two bounded non-negative ranges is bounded. Grade: **TRIVIAL** — a one-line rewrite of the same formula, no duplication, no new rules.

**Cost to build.** The narrow version — "recognize that both sides of a `*` are the identical expression, and conclude the result is ≥ 0" — is one new discharge step spliced into the existing strategy cascade (`ProofEngine.cs:1054`) plus one strategy name (`ProofLedger.cs:100`), reusing the existing multiplication and sign machinery (`NumericInterval.cs:85–102`, `Composition.cs:847`). That is **Small**. The full version — recognizing squares hidden inside rearranged formulas (e.g. variance written as "average of squares minus square of the average"), or sums of several squared terms — needs genuine formula-rearrangement machinery that the inventory confirms does not exist in any form (no equation-solving code anywhere in `src/Precept`; the engine's only numeric tools are ranges and a three-way plus/zero/minus sign check). That tail is **Medium-to-Large**, and its main risk is scope creep: "x·x ≥ 0" quietly growing into general polynomial reasoning, which the engine's deliberately bounded, non-searching design (`proof-engine.md` §12) was built to avoid.

**Benefit.** Zero occurrences in the corpus: across all 77 `samples/*.precept` files, no file computes a square, a variance, or a squared deviation (`grep -iE 'squar|varianc|deviat'` → 0 hits outside one prose comment; `grep '\) *\* *\('` → 0 hits). 22 files use multiplication, and every one multiplies two *different* quantities (price × count, payment × schedule) — shapes the existing corner-evaluated multiplication already handles. Even the flagship statistics sample (`statistical-process-control.precept`) takes its control limits as inputs from capability studies rather than computing them. And where the need does arise, the hand-fix is a trivial, exact one-line rewrite.

**Verdict.** NOT-WORTH-IT — zero incidence in 77 real definitions plus a trivial exact hand-fix (`abs`-wrap) means this buys almost nothing; revisit only if computed statistical quantities become a real authoring pattern.