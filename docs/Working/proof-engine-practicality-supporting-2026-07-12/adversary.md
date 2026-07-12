I verified every load-bearing "compiles clean / rejected today" claim against the live engine. Findings below, ranked by severity. Section-by-section verification results are cited with the exact obligation dispositions I observed.

---

**FINDING 1 — Aggregates section: the "cleared by hand" fix does NOT compile as written (false verification claim). [most severe]**

The section states of its hand-fix: *"Verified: this compiles clean on today's engine (proof ledger shows the bound proved, computed range 1 to 5,000)."* That is false. I compiled the exact hand-fix code and it **fails** with `PRE0136`: *"Cannot prove `ComponentPartNumbers` stays within `maxcount 50` after this add — guard with `when ComponentPartNumbers.count < 50`."*

What happened: the hand-fix **dropped** the `when ComponentPartNumbers.count >= 50 -> reject` row that guarded the collection in the *problem* example, and replaced it with a `TotalComponentUnits`-only guard. The numeric obligation the section cares about does prove (I confirmed `IntervalContainment … Proved, computedInterval [1 .. 5000]`), but the collection's `maxcount 50` is now unguarded, so the `add` fails a *separate* count obligation. The section verified only the numeric obligation in isolation and reported the whole file as clean.

The escape does exist — I confirmed a **compound two-guard** version compiles clean (`when ComponentPartNumbers.count < 50 and TotalComponentUnits <= 4900` → both obligations Proved). So the LATER-PHASE verdict survives, but three things must change:
- Fix the hand-fix code to the two-guard form and correct the "compiles clean" claim.
- The example bundles **two independent obligations** (collection `maxcount` + numeric total); the narrative silently addresses only the total. Either drop the `maxcount` from the collection so the example is single-concern, or acknowledge both.
- The DUPLICATE-A-FORMULA cost is understated: the author must maintain **two coupled guards** (a `.count` guard *and* a hand-derived `<= 4900` headroom), both re-derived whenever the bound or per-item cap changes — not "the one-line pre-computed guard."

---

**FINDING 2 — Show-your-work section: "Medium" is lowballed; the independent re-checker is a from-scratch proof-checking subsystem.**

The section prices the whole capability Medium and calls the re-verifier *"the small independent re-checker."* That undersells it. Per the inventory, *"A serialized certificate format and a separate re-verifier are entirely new."* A re-checker that re-reads and confirms each justification must independently cover **all 11 discharge strategies'** claim shapes — interval arithmetic, sign-set, relational facts, qualifier/currency, dimensional product, count, and length containment (`proof-engine.md:710–1727`). That is a second checking implementation, not a bolt-on, and the section itself concedes *"every future proof recipe inherits an 'emit-your-reasoning' obligation — a permanent tax on all later proof work."* Cost (b)+(c) together (format + independent verifier) read as **Large**. Fix: split the grade — the ledger-surfacing half (a) is Medium on finished plumbing; the certificate-format-plus-independent-verifier half is Large, and it is a standing tax, not a one-time build.

---

**FINDING 3 — Coverage: whole built proof families have no section, and the doc never states its scope.**

The doc covers numeric-bound reasoning thoroughly but omits several *built* proof types that authors hit constantly:
- **Qualifier / currency / dimension compatibility.** I confirmed this is live and prove-or-reject today: mixing USD + EUR rejects with `PRE0114` (`QualifierCompatibility … Unresolved`). Money appears in **37 of 77** sample files; this family is ~950 lines of engine (`ProofEngine.Qualifiers.cs`) and 3 of the 13 requirement kinds. Zero coverage.
- **String-length containment** (`LengthContainment`, a built proof type): `maxlength/minlength/notempty` appear in **72 of 77** files. Zero coverage.

The doc covers other already-built proof types (rounding, bare-field relational), so "already built" isn't the exclusion rule — these are genuine gaps. Fix: either add short "already built, keep it, ~zero cost" sections for the qualifier and length families, or state explicitly at the top that the doc's scope is *numeric bound reasoning only* and these are out of scope by design.

(Note: `maxcount/mincount` appears in only **1** file — low corpus signal — so count-cardinality does *not* need its own section; Finding 1 covers where it bites.)

---

**FINDING 4 — Linear-relationships section: grade is right (Large) but the mitigation language understates build cost and double-books witness work.**

The core (a general linear solver with constants/coefficients and multi-fact combination) is correctly greenfield and correctly graded Large — I confirmed the problem rejects (`PRE0078, [0 .. 20000]`) and the `min`-clamp hand-fix proves clean (`[0 .. 10000]`), so the section's mechanics are honest. Two framing problems:
- The mitigations *"each proof problem is tiny — one guard, a handful of rules, one assignment"* and *"well-bounded per use"* conflate **per-invocation runtime** with **build cost**. A Farkas/elimination core is expensive to build regardless of how small each run is; the smallness argument doesn't reduce the engineering.
- The section folds *"producing a concrete 'here's a value assignment that breaks it' counter-example"* into its own new work, but counterexample-witness generation is **separately scoped and priced** in the "Telling the author" section (graded Small–Medium there). Pick one owner; as written it's double-counted.

---

**FINDING 5 — "Letting a rule do the work" section: the PREREQUISITE label contradicts its own verdict text and is inconsistent with the other PREREQUISITE.**

Section §6 says *"prove-or-reject can **technically ship without it** (the mirror-field fix compiles today)"* — and I confirmed the mechanics: a bare field-vs-field rule discharges a division (`Divisor must be non-zero … Proved, strategy FlowNarrowing`), while the arithmetic-in-rule form rejects (`PRE0083 … Unresolved`), and the mirror-field workaround is what flips it. So by its own admission this is **not** a build-ordering prerequisite. Yet it carries the same label as "Show-your-work," whose §6 argues a genuine *build-before* ordering ("costs strictly more … before"). A planner reading "PREREQUISITE" will treat this as must-build-first. Fix: give it a distinct verdict (e.g. MVP-on-philosophy-grounds, or a separate tier) and reserve PREREQUISITE for the build-ordering cases, or add one sentence distinguishing "foundational-ordering prerequisite" from "product-promise prerequisite with a working hand-fix."

---

**FINDING 6 (minor) — Coverage: modulo has no section and no interval transfer.**

Inventory confirms modulo carries no `IntervalTransfer` → result `Unbounded`, the same failure class as `sqrt`/`pow` (unbounded result rejects a banded field). But it's excluded from the "Square roots, powers, rounding" section because it isn't monotone, leaving it with no home. A modulo result is actually *bounded* (`x % n ∈ [0, n-1]`), so a banded field computed via modulo is a plausible real pattern that would reject under prove-or-reject. Corpus signal is low/ambiguous (14 `%` lines, mostly percentages, not the operator). Fix: add one line to the sqrt/pow section — "modulo is the same missing-transfer story; its result is bounded and the fix is the same catalog-shaped rule" — or note it in the scope statement.

---

**FINDING 7 (minor, accuracy) — sqrt section file count is wrong.**

The sqrt section says *"across all 76 files in samples/"* and *"all 76 files"*. The corpus is **77** files (`ls samples/*.precept | wc -l` → 77), and every other section says 77. Fix the count so the "verified grep" claims stay trustworthy.

---

**FINDING 8 (minor, jargon) — two residual-jargon slips.**

- *"obligation" / "proof obligation"* is used undefined in several sections (linear §2 "no proof obligation is generated," products §4 "the obligation that triggers," telling-author §5 "each one is an obligation," show-your-work). A domain author won't know the term. Define once ("a fact the compiler is required to prove — an *obligation*") or replace with "check."
- Case-by-case §2 dumps the raw sentinel *"computed range [−79228162514264337593543950335 .. 50]"*. That giant number is noise to a business reader. Gloss it as "the lowest possible decimal — i.e. unbounded below." (I confirmed this exact value is what the engine returns, so the underlying claim is correct; only the presentation needs a gloss.)

---

**Sections I verified as solid (no action needed):**

- **Products and division** — I confirmed every claim: decimal product with bounds proves clean (`[0 .. 1000]`); the same shape in money is rejected (`PRE0078, [−∞ .. +∞]`); and critically, a *bare* money passthrough into a bounded money field **does** prove (`[0 .. 10]`). That last probe confirms the gap is narrow and exactly as the section frames it — money bands flow, but the `×`/`÷` interval transfer isn't wired for money/unit operands (it covers Integer/Decimal/Number only). So "Small / one wiring gap" is **defensible**, not lowballed. Division safety via `positive` is real. Good section.
- **Case-by-case (guards, min/max)** — all three of its probes reproduced exactly: `if/then/else` problem rejects (`PRE0078, [−∞ .. +∞]`); `min(input, 50)` proves upper but not lower (`[−79228162514264337593543950335 .. 50]`, exactly the event-input-doesn't-narrow claim); `clamp(input, 0, 50)` proves clean (`[0 .. 50]`). Honest and accurate; MVP defensible.
- **Square roots/powers/rounding** — problem rejects (`PRE0078, [−∞ .. +∞]`, with the `Argument must be non-negative` obligation independently Proved via the `nonnegative` modifier); `clamp(sqrt(…),0,20)` proves clean (`[0 .. 20]`). Mechanics honest; Small cost matches the catalog-rule-per-function pattern. (Only Findings 6–7 touch it.)
- **Squared/statistical** — honest that today's clean is because the nonnegative-computed-write posture isn't built; `abs`-wrap hand-fix is a valid exact rewrite; NOT-WORTH-IT is defensible on zero corpus incidence.
- **Telling the author** — the `PRE0078` rejection mechanism it relies on is the same one I reproduced repeatedly; the staged Small–Medium / Large split (surface stored numbers first, derive safe-condition later) is sound and matches the inventory.

**On the "hidden prerequisite that changes the MVP" check specifically:** I compiled every MVP-section hand-fix against the live engine — linear (`min`-clamp), products-money (honestly graded CAN'T-BE-DONE), case (`clamp`), telling-author (`max 46`) — and all the *MVP* hand-fixes hold up. No MVP flips. The strongest instance of the hidden-prerequisite pattern is **Finding 1** (aggregates), where the hand-fix as written genuinely does not compile, but it's repairable, so it corrects a false claim and hardens the cost rather than promoting the section into the MVP.