## Square roots, powers, and rounding

**What it proves.** When a formula takes a square root, raises a value to a whole-number power, or rounds a number, the compiler can work out the tightest possible range of the result from the range of the input — because these functions never "jump around": a bigger input never produces a smaller output (or, for rounding, the output moves in lockstep with the input). That lets the compiler confirm the result stays inside the field's declared `min`/`max` band.

**Status split — half of this already exists.** Rounding is done: `floor`, `ceil`, `truncate`, and `round` already carry hand-written result-range rules in the function catalog (`src/Precept/Language/Functions.cs:108–155`), and the end-to-end proof works today — a computed `integer min 0 max 10 <- round(Rate)` over a `Rate` banded `[0 .. 9.9]` compiles clean with the proof ledger showing the computed range `[0 .. 10]` (verified via the compiler, 2026-07-12). The gap is `sqrt` and `pow`: both are declared in the catalog (`Functions.cs:183–213`) but carry **no result-range rule**, so the compiler treats their output as "could be anything" (−∞ to +∞).

**What gets rejected without it** (PROPOSED design — and, notably, today's engine already rejects this the same way; the posture is live for this case). Synthetic example — no shipped sample uses `sqrt` — mirroring `samples/` conventions:

```precept
precept SafetyStockPolicy

field DemandVariance as number default 25 nonnegative min 0 max 400

# Square-root-of-variance safety-stock heuristic.
# sqrt of [0..400] is truly [0..20] — but the compiler can't see that.
field SafetyStock as number min 0 max 20 <- sqrt(DemandVariance)
```

Verified today: this fails with `PRE0078` ("Numeric computation exceeded the representable range on field 'SafetyStock'") because the sqrt result is treated as unbounded, even though the true result range `[0 .. 20]` fits the band exactly. (Without the `nonnegative` modifier it also fails `PRE0084` — the separate "input must be non-negative" check, which does work today.)

**How the author clears it today, by hand.** Wrap the formula in `clamp`, stating the endpoint values yourself:

```precept
field SafetyStock as number min 0 max 20 <- clamp(sqrt(DemandVariance), 0, 20)
```

Verified: compiles clean today, all obligations Proved, computed range `[0 .. 20]` (the `clamp` range rule at `Functions.cs:91–105` absorbs the unknown inner range). Grade: **DUPLICATE-A-FORMULA** — the wrapper is one line, but the author must do the endpoint math by hand (know that √400 = 20) and restate it, which is exactly the arithmetic the compiler should be doing. And `clamp` doesn't *check* — it silently clips, so if the hand math is wrong the definition still compiles and quietly caps values instead of surfacing the error.

**Cost to build.** Small. The delivery mechanism exists and is proven: each function carries an optional result-range rule in the catalog (`Function.cs:31`), consumed generically by the interval machinery (`ProofEngine.Intervals.cs:104,124`), with four shipped rounding functions plus `abs`/`min`/`max`/`clamp` as the direct template (`Functions.cs:44–155`). The work is writing two more such rules: `sqrt` (map the two endpoints; input already proven non-negative by the existing check) and `pow` (the fiddly one — an even exponent on a range straddling zero needs the same sign-split treatment `abs` already demonstrates at `Functions.cs:74–86`, and an exponent that isn't a known constant must conservatively give up). No new strategy, no new obligation kind, no engine changes. Main risk: getting `pow`'s corner cases right — mitigated by the corner-evaluation precedent already in `NumericInterval.Multiply` (`NumericInterval.cs:85–90`).

**Benefit.** Low frequency today: across all 76 files in `samples/`, grep finds **zero** call sites of `sqrt`, `pow`, `round`, `floor`, `ceil`, or `truncate` (the only "floor(" hit is prose in a comment, `loan-application.precept:94`); the math functions actually used are `min`/`max` — 5 call sites across 4 files — which already have range rules. The real benefit is forward-looking: under prove-or-reject, `sqrt` and `pow` are today *unusable* in any banded computed field without the clamp workaround, so shipping the language with these two functions effectively broken-by-default is a coherence cost even if the corpus doesn't hit it yet.

**Verdict.** WORTH-BUILDING-AS-A-LATER-PHASE — zero corpus usage and a one-line (if slightly dishonest) hand fix mean it blocks no one now, but the fix is cheap, pattern-following catalog work that removes a "this function can never pass a bound check" wart.