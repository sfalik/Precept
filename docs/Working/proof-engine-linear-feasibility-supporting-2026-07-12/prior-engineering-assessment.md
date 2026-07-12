All evidence is in hand. Here is the feasibility assessment.

---

# Feasibility: linear-relationships proof work (§1a vs §1b)

## 1. Corpus finding — is §1b needed at all?

**Frank's claim verified: no sample in the 77-file corpus requires combining two relational facts. Every real discharge site is a single multi-term fact (§1a). §1b has zero corpus demand today.**

I swept the corpus three ways and scrutinized every flagged candidate:

**The 24 guard-then-apply sites** (grep `when … A + B <op> C` across `samples/*.precept`: 24 rows, e.g. `event-venue-booking.precept:234`, `production-order-tracking.precept:140`, `equipment-lease-agreement.precept:172,193`, `vehicle-registration-renewal.precept:191–305`). Each is one guard bounding one sum; the success row assigns that same sum. Discharge = transfer one fact. §1a.

**The "three-variable / Farkas" candidates**, examined individually:

- `production-order-tracking.precept:66` — `rule ProducedQuantity + ScrapQuantity <= PlannedQuantity`. One three-term fact. Using it never requires a second *relational* fact: the closest-to-the-line case is `production-order-tracking.precept:234` (`set ScrapQuantity = PlannedQuantity - ProducedQuantity` into a `nonnegative` field, declared at `:32`), which needs the rule **plus** ScrapQuantity's own declared `nonnegative`. But declared bounds live in the interval environment, not the relational fact set — and the engine *already* substitutes one-hop bare intervals into relational facts exactly this way (`ProofEngine.Intervals.cs:629–654` reads the related field "ONE HOP via the bare non-relational ExtractFieldInterval — never the dict being built"; same discipline at `ProofEngine.Composition.cs:509–513`). One fact + interval substitution is §1a's mechanism, not fact-chaining. This is the boundary case and it lands on the §1a side.
- `production-order-tracking.precept:153` — a **three-term guard** (`Produced + Scrap + RecordScrap.Quantity > Planned`). Still one fact; it just needs the multi-term representation §1a builds anyway.
- The other sum-vs-field rules (`hotel-reservation-management.precept:64`, `event-venue-booking.precept:97`, `equipment-lease-agreement.precept:79`, plus 8 date-difference rules in `prior-auth-appeal.precept:98–103` / `medical-prior-auth.precept:94–97`): each is one multi-term fact standing alone.
- The 18 add/subtract computed fields (grep; doc says 16 — e.g. `vehicle-registration-renewal.precept:46` with six addends, `saas-usage-metering-and-billing.precept:34–35`): interval arithmetic over the addends' declared bounds, at most one relational fact. None chains facts.

**Why the exposure is thinner than it looks:** none of the guard-then-apply *target* fields carries a declared constant `max` — they're money fields with defaults (verified: `event-venue-booking.precept:49–53`, `equipment-lease-agreement.precept:58–59`, etc.). The only constant-max accumulator targets in the corpus are `global-meeting-scheduler.precept:102/203`, `saas-trial-to-paid.precept:30/128,136`, `patient-care-plan-coordination.precept:33/185` — and each discharges from a **single field-vs-constant** fact (the reject-row's negated guard, e.g. `ExtensionCount >= 3 → reject` above `set ExtensionCount = ExtensionCount + 1`). Constants-in-facts is §1a work (today's fact shape is strictly field-vs-field, no constants — `ProofEngine.Strategies.cs:1240–1248`); fact-chaining is not needed even here.

I confirmed the flagship rejection live via `precept_compile`: the `Total + Charge.Amount <= 10000` guard-then-apply probe rejects today with PRE0078, computed interval [0..20000] against declared [0..10000] — the guard is invisible to the prover, exactly as the docs claim.

**Caveat on scope of the claim:** "no corpus case needs §1b" is a statement about the 77 samples under bound-discharge obligations (declared min/max/nonnegative/positive, divisor safety, overflow). Rules themselves are runtime governance, not compile-time preservation obligations, so "prove rule R still holds after this write" — the one shape that *would* commonly need two facts — is not an obligation the engine generates. If that ever changes, the §1b question reopens.

## 2. Size — §1a (the corpus-justified MVP core)

**Grade: Medium, not Large. Roughly 500–700 LOC of engine code + 1,000–1,600 LOC of tests. The original doc's "Large" grade belonged to the merged §1; Frank's split moves most of the weight into §1b.**

Anchoring against the engine as shipped. Today's entire relational-fact subsystem is small and §1a is a generalization of each of its pieces:

| Existing piece | Where | Size | §1a delta |
|---|---|---|---|
| Fact record | `FieldToFieldConstraint` (name, op, name) — `ProofEngine.Strategies.cs:1246` | 1 record | New `LinearFact`: signed term list + constant + op, with canonical term-multiset form (~80–120 LOC incl. normalization) |
| Fact extraction | `ExtractFieldToFieldLeaf` `:1240–1248` (9 lines — bare field vs bare field only) | ~10 LOC | Walk `+`/`−` chains, accept constants and event-input refs (~100–150 LOC). The and/or branch splitter above it (`:1174–1213`) is reused unchanged |
| Discharge strategy | `TryFlowNarrowingProof` `:1078–1149` + implication table `:1250–1275` | ~100 LOC | New `TryLinearFactProof`: guard-sum syntactic match (assigned expression ≡ guarded sum ⇒ bound transfers) + single-fact rearrangement using one-hop interval substitution for the remaining terms (~200–300 LOC). Splices cleanly into the cascade at `ProofEngine.cs:1076` |
| Interval fold | `Intervals.cs:629–654` + `RelationalHalfLine` `:660+` | ~70 LOC | Generalize the half-line to multi-term (subtract the other terms' one-hop intervals) (~80–120 LOC) |
| Staleness | `ReassignedBefore` stamps, `ProofEngine.cs:409–421`; checked at `Strategies.cs:1105–1107` | built | Extend the two-field check to all terms of the fact (~10–20 LOC) — the discipline itself is already enforced |
| Ledger/plumbing | `ProofStrategy` enum (`ProofLedger.cs:100–117`), justification emission | built | One enum entry + reasoning emission per §5a (~50–100 LOC) |

For scale comparison: `ProofEngine.QualifierNarrowing.cs` (a complete strategy file) is 316 LOC; `ProofEngine.Lengths.cs` (the whole length family) is 351 LOC. §1a is one new partial-class file of that order plus edits to three existing files.

**Tests.** House style is dense: `ProofEngineIntervalTests.cs` = 688 LOC / 56 facts (~12 LOC/test); `ProofEngineIntervalIntegrationTests.cs` = 1,195 LOC / 55 facts (~22 LOC/test). The §1a matrix — exact guard-sum match, reordered/subtraction forms, constants on either side, all four comparison orientations, event-input terms, OR-branch guards (every branch must prove), staleness (a term reassigned mid-chain must kill the fact), reject-row fall-through negation, three-term guards (the `production-order-tracking:153` shape), money/quantity units, integer vs decimal edge floors, and soundness negatives (a *different* sum must NOT discharge) — is ~50–80 tests, so ~1,000–1,600 test LOC.

**Spec posture:** §1a stays single-hop — it applies one fact, reading other terms' bounds one hop exactly as `Intervals.cs:635` does today — so it does not override spec:256's locked depth-bound (verbatim at `precept-language-spec.md:256`: "The mechanism is single-pass and depth-bounded (no transitive chasing of a third field), per §0.4"). The spec's *description* of fact shape ("field-to-field") needs a text update to admit multi-term facts, but that is extension within the discipline, not a reversal of the locked no-chaining decision.

## 3. Size — §1b (the deferred multi-fact solver)

**Grade: Large — roughly 3× §1a: ~1,500–2,200 LOC of code + 2,500–4,000 LOC of tests, plus two owner-gated spec actions.**

Genuinely new machinery with no substrate to anchor on (grep of `src/Precept` confirms no linear-solver code of any kind; the only "octagon/DBM" token is a comment analogy at `Composition.cs:554`):

- **Fact database + normalization** — collect rules, guards, row-complement negations into a canonical `Σ aᵢxᵢ ≤ c` form (~200–400 LOC).
- **Elimination core** — Fourier–Motzkin over exact arithmetic. Coefficients under elimination leave the ±1 world, so this needs exact rationals; the only BigRational in the repo is bench-only (`tools/Precept.Bench/BigRational.cs`) and would have to be productionized. ~600–1,000 LOC.
- **Farkas certificate + independent checker** — spec:225 makes this non-optional: Frank's review (§4, `frank-review-proof-engine-mvp-phases-2026-07-12.md:59`) holds that any linear-arithmetic strategy ships *with* a legible, independently checkable certificate or it violates the opaque-solver ban. Construction + re-checker ~300–500 LOC.
- **Counterexample extraction** on refutation (~150–300 LOC).
- **Staleness + circularity generalization** — the current guarantees are structural (one hop can't chase a third field, `Composition.cs:511–513`; `ReassignedBefore` kills facts per obligation, `ProofEngine.cs:411–414`). A combining solver loses both structural guarantees and must re-establish them by construction — this is the real engineering risk, not the math.
- Tests are soundness-heavy (every certificate independently re-checked, adversarial almost-matching premise sets): ~150+ tests.

And two gates before any code: an owner-authorized **spec:256 override** (relaxing the locked depth-bound) and owner **ratification that a Farkas certificate satisfies spec:225** — both explicitly named as Shane's calls in Frank's review §5.

## 4. Performance

**Today's measured baseline:** full 77-file corpus compiles in ~44ms; worst single file ~3ms. These figures are owner-verified project evidence, recorded at `docs/Working/compiler-readiness-plan-2026-06-11-appendices/spec-coverage-audit.md:2406` and `docs/Working/Superseded/compile-time-prevention-features-proposal-2026-06-06.md:8`, and cited consistently across the design chain. I could not take a *fresh* measurement in this session: the MCP `precept_compile` response carries no timing field (verified by probe), and running `tools/Precept.Bench` or a timing harness requires a build, which plan mode blocks. Re-measurement method when unblocked: add a `Compiler.Compile`-over-`samples/` benchmark to `tools/Precept.Bench` (BenchmarkDotNet is already wired there) — a one-file addition.

**Budget:** sub-50ms single-file (debounced-keystroke recompile). Today's worst file uses ~6% of it.

**§1a added cost — negligible, by construction.** Per obligation: one guard walk (already performed today for field-to-field extraction), a term-multiset comparison over ≤ ~6 terms (the corpus max is 6 addends, `vehicle-registration-renewal.precept:46`; guards max out at 3 terms, `production-order-tracking.precept:153`), and one fact application — a handful of decimal operations at 2–15ns each (measured: `BenchmarkDotNet.Artifacts/results/Precept.Bench.ArithmeticBench-report-github.md`). Even at hundreds of obligations per file this is microseconds against a 3ms baseline. The one superlinear piece §1a touches — the OR-branch cross-product splitter (`Strategies.cs:1195–1205`, exponential in and/or nesting) — already exists today and §1a reuses it without deepening it. **Verdict: §1a fits the budget with three orders of magnitude to spare.**

**§1b projected cost — also fine, which is worth stating plainly: performance is NOT the reason to defer §1b.** Fourier–Motzkin is doubly exponential in eliminated variables *in theory*, but the blowup is unreachable at Precept scale: the corpus averages ~2 rules per file (149/77), obligations involve 2–6 variables, and there are no loops so no fixpoints (spec:174). A 5-variable, 10-constraint eliminate is microseconds. A hard cap (refuse beyond k variables/facts, emitting "too entangled to certify") makes the worst case structurally unreachable and doubles as a legibility feature. §1b is deferred on corpus need and spec-legality, not latency.

## 5. Build vs. buy (owner decision: BUILD — the justification)

Off-the-shelf .NET LP solvers (Google OR-Tools/GLOP, HiGHS, CPLEX) are the wrong tool here for four concrete reasons:

**(a) They answer the wrong question.** An LP solver returns "feasible/infeasible, optimal value, variable assignments." Precept needs a *legible certificate* — "your rule at line 66, plus your guard at line 140, together guarantee this bound; here are the multipliers, check them yourself" — because spec:225 rejects opaque solvers *even when they could prove more*, and the whole product promise is that the compiler shows its work. A solver's dual values could in principle seed Farkas multipliers, but turning them into a checkable, author-readable certificate is precisely the part you'd still have to build — the solver saves you only the easy part.

**(b) Floating point vs. exact decimal.** GLOP and HiGHS compute in floating point; Precept's guarantee is exact-decimal and deterministic ("same definition + same data = same outcome"). A float-derived multiplier of 0.9999999 is a certificate that fails exact re-checking at the margin. Exactness costs nothing at this scale: the repo's own bench measured exact decimal ops at 2–15ns and arbitrary-precision rational ops at 11–46ns (`ArithmeticBench-report-github.md`).

**(c) The problems are tiny; the overhead isn't.** Each proof problem is 2–6 variables and 1–3 constraints, solved thousands of times per keystroke-recompile across obligations. Industrial-solver model construction and interop marshaling per call exceed the solve itself and would eat the ~3ms file budget doing bookkeeping — the solver is optimized for problems six orders of magnitude larger.

**(d) Native-binary dependency fights embeddability.** OR-Tools ships ~100MB of native binaries per platform; HiGHS is a native library; CPLEX is commercially licensed. The compiler is a single managed assembly embedded in a language server, an MCP server, and a shipped plugin — a native solver dependency complicates every one of those surfaces for a solve that is, mathematically, a few dozen decimal multiplications.

**The reframe:** the size estimates above are not driven by hard math — eliminating a variable from three inequalities is undergraduate algebra. The cost is *exact arithmetic + certificate emission and independent re-checking + integration with the engine's staleness and one-hop soundness disciplines*. None of that comes in a box; buying the solver would leave ~90% of the build intact.

## Bottom line

- **§1b is not needed by the corpus.** Verified independently: every bound-discharge in all 77 samples uses at most one relational fact (possibly multi-term, possibly with constants) plus declared-bound intervals — including every candidate the forcing analysis flagged as "Farkas." The closest case (`production-order-tracking.precept:234`) resolves via one fact + the existing one-hop interval substitution. Frank's split holds.
- **§1a is a Medium build, not Large:** ~500–700 engine LOC + ~1,000–1,600 test LOC, generalizing five existing, working mechanisms (fact record, extraction, flow-narrowing strategy, interval fold, staleness) with a clean cascade splice at `ProofEngine.cs:1076`. It stays inside spec:256's locked single-hop discipline and its per-check cost is microseconds against a ~3ms worst-file / 50ms budget.
- **§1b is a Large deferred build:** ~1,500–2,200 LOC + 2,500–4,000 test LOC (exact-rational elimination core, Farkas certificate + independent checker, counterexamples, re-derived soundness disciplines), gated on two owner decisions (spec:256 override, spec:225 certificate ratification). Its performance is fine at Precept scale — defer it on need and spec-legality, and build it only when a real definition produces a two-fact chain that §1a cannot discharge.
- **Build over buy is correct** for the four reasons above; the decision costs nothing mathematically and buys exactness, certificates, determinism, and embeddability.

One measurement gap to close when a build is permitted: add a compile-latency benchmark to `tools/Precept.Bench` and refresh the ~44ms/~3ms figures on current HEAD — the documented numbers date from mid-June and everything above assumes they still hold.