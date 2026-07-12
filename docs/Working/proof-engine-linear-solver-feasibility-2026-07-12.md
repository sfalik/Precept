---
title: "Feasibility: Linear-Relationship Proof Work (§1a and §1b)"
status: Draft — 2026-07-12 (independent analysis; not owner-ratified)
author: Independent analysis (Fable), orchestrated by Claude
owner: Shane
reviews: docs/Working/proof-engine-mvp-and-proof-phases-2026-07-12.md (§1, split by Frank into §1a/§1b)
note: Plain-language. §1b's need is argued from first principles, not corpus incidence (the corpus is not comprehensive). Three owner-gated items are surfaced, not resolved. Compile figures are fresh HEAD measurements (see §4).
---

All load-bearing anchors verified against source (file names corrected to their actual `ProofEngine.*` forms, line anchors confirmed, bench numbers confirmed at 2.2–14.9 ns, 77 samples confirmed). Final document follows.

# Feasibility: Linear-Relationship Proof Work (§1a and §1b)

**Status:** Draft feasibility — 2026-07-12. Nothing in this document is owner-ratified. Three calls are explicitly reserved for the owner and flagged inline: §1b's phase placement, the spec:256 override, and the spec:225 certificate ratification.

---

## 1. §1a — Multi-term linear facts, used one at a time

### What it is

Today the proof engine understands a relational rule only in its simplest shape: one field compared to one field (`rule X >= Y`). §1a generalizes that to a *linear fact* — a rule whose sides may combine several fields with constant multipliers, e.g. `rule Principal + Interest <= CreditLimit` or `rule 0.8 * Estimate <= Actual`. Each such fact still participates in proof **one at a time**: when the engine needs to discharge an obligation (a division that must not hit zero, an assignment that must stay in bounds), it may consult a single linear fact plus the fields' declared intervals — never two facts combined.

This is a generalization of five mechanisms that already exist and were verified in source:

| Existing mechanism | Anchor |
|---|---|
| The field-to-field fact record (`FieldToFieldConstraint`) | `src/Precept/Pipeline/ProofEngine.Strategies.cs:1246` |
| Fact extraction from typed expressions | `ProofEngine.Strategies.cs:1240` |
| Flow-narrowing strategy | `ProofEngine.Strategies.cs:1078` |
| One-hop relational interval fold | `ProofEngine.Intervals.cs:629-654` |
| Reassignment staleness (facts die when their fields are rewritten) | `ProofEngine.cs:409` |

New behavior splices into the existing proof-strategy cascade at `ProofEngine.cs:1076` — it is an added strategy, not a new pipeline stage.

### Size

**Medium.** Roughly **500–700 lines of engine code** plus **1,000–1,600 lines of tests**. Calibration against the codebase's own strategy files supports this: `ProofEngine.QualifierNarrowing.cs` is 316 lines and `ProofEngine.Lengths.cs` is 351 lines, and §1a is somewhat larger than either because the fact shape (multiple terms, coefficients) is richer. Test sizing follows house style: `test/Precept.Tests/ProofEngineIntervalTests.cs` is 688 lines covering 56 tests (~12–22 lines per test).

### Performance

Negligible. The added work per obligation is a term-multiset comparison over at most ~6 terms and a handful of exact-decimal arithmetic operations, each costing 2–15 ns (measured: `BenchmarkDotNet.Artifacts/results/Precept.Bench.ArithmeticBench-report-github.md`, decimal add 2.8 ns, multiply 2.2 ns, divide 14.9 ns). §1a adds microseconds to a compile measured in milliseconds (see §4 for the measurement caveat).

### Spec posture

**Stays inside the current spec.** `precept-language-spec.md:256` locks relational reasoning to "single-pass and depth-bounded (no transitive chasing of a third field)." A multi-term fact consulted one at a time, with related-field bounds read one hop via the existing interval extraction, honors that constraint. No override needed; no owner gate beyond normal design review.

---

## 2. §1b — Combining two or more linear facts

### What it is

§1b is the step §1a deliberately does not take: letting the engine **combine** facts. If the file declares `rule A + B <= C` and separately `rule C <= Limit`, no single fact proves `A + B <= Limit` — but adding the two (each possibly scaled by a positive constant) does. The standard exact technique is *variable elimination over rational arithmetic* (Fourier–Motzkin): repeatedly combine pairs of inequalities to eliminate one variable at a time until the target inequality falls out or is refuted. The multipliers used in the combination form a **checkable certificate** (a Farkas certificate): a short list saying "take fact 1 times 1, fact 2 times 1, add them, and you get the claimed bound" — which an independent, much simpler checker can re-verify, and which reads as a legible explanation, not a solver trace.

### Size

**Large.** Roughly **1,500–2,200 lines of code** plus **2,500–4,000 lines of tests**, covering:

- A fact database and normalization layer (canonical linear form, coefficient handling)
- The exact-rational elimination procedure — which requires productionizing `tools/Precept.Bench/BigRational.cs` (currently a benchmark-support type, not a production one)
- Certificate construction plus an **independent certificate checker**
- Counterexample extraction for refuted claims
- Re-derived staleness and circularity disciplines: the existing "fact dies when its field is rewritten" logic (`ProofEngine.cs:409`) must be re-proved sound when a conclusion depends on *several* facts with different lifetimes

That is roughly 3× §1a — a different order of investment, and the honest reason §1b is a candidate *second* phase rather than part of the first.

### Performance

**Fine at Precept scale — performance is NOT the deferral reason.** Variable elimination is worst-case explosive in the abstract, but Precept's problems are tiny: 2–6 variables per obligation, no loops, no unbounded derivation chains, and a small fixed cap on variables eliminated makes the theoretical worst case unreachable by construction. On exact-rational arithmetic at 10–15 ns per operation, each discharge attempt costs microseconds. Deferral, if any, rests on engineering size and spec posture — not speed.

### Spec posture — two owner gates

1. **spec:256 override (owner-authorized only).** The spec's locked relational-reasoning decision reads: "The mechanism is single-pass and depth-bounded (no transitive chasing of a third field), per §0.4" (`precept-language-spec.md:256`). Combining facts *is* transitive chasing — `A + B <= C` and `C <= Limit` connect through the third field `C`. Building §1b requires the owner to explicitly authorize amending that locked sentence. This cannot be decided inside a design pass.
2. **spec:225 certificate ratification (owner call).** The spec rejects opaque solvers: "Proof witnesses must be structured data, not opaque solver traces" (`precept-language-spec.md:225`). A Farkas certificate is arguably the *strongest possible compliance* with that principle — it is small, structured, independently checkable, and human-readable ("fact 1 + fact 2 implies the bound"). But whether a multiplier-list certificate satisfies the spec's legibility bar is a judgment about the inspectability commitment itself, and that ratification belongs to the owner, not to this document.

### First-principles need analysis

**Why the corpus cannot answer this.** The prior assessment verified that every bound-discharge in all 77 sample files uses at most one relational fact — the closest case, `samples/production-order-tracking.precept:234` (`set ScrapQuantity = PlannedQuantity - ProducedQuantity`), resolves via one fact plus the existing one-hop interval substitution (`ProofEngine.Intervals.cs:629-654`). That finding is accurate, and it is **not evidence that §1b is unneeded**. The owner has ruled the 77-sample corpus is not comprehensive: the samples were written by people who knew what the engine could prove, so they systematically avoid shapes the engine can't handle. Corpus silence here is selection effect, not absence of demand. The need question has to be answered from the target domains directly.

**Business patterns that genuinely require combining two or more facts** — each of these is a shape where no single multi-term rule can carry the whole argument, because the knowledge naturally arrives as separate declarations:

1. **Blended / multi-tranche caps.** A loan or policy priced across tranches: `rule BlendedRate = (RateA * AmtA + RateB * AmtB) / (AmtA + AmtB)` with a ceiling `rule BlendedRate <= MaxRate` *and* per-tranche limits `rule RateA <= TrancheACap`, `rule RateB <= TrancheBCap`. Proving the ceiling from the per-tranche limits requires adding scaled facts. Ubiquitous in lending, reinsurance layering, and tiered pricing.
2. **Cross-field balance identities used with a fourth field's bound.** `rule Assets = Liabilities + Equity` combined with `rule Liabilities <= LeverageLimit` to discharge an obligation on `Assets - Equity`. Balance identities are the backbone of accounting, claims reserving, and settlement — and an identity is only useful in proof when combined with a *second* fact.
3. **Allocation / apportionment across bounded fields.** `rule AllocA + AllocB + AllocC = Total` with individual caps on each allocation, proving a bound on the remainder or on any partial sum. This is cost allocation, premium apportionment, bed/capacity assignment, and shipment splitting — core patterns in insurance, healthcare, and logistics.
4. **Ratio-of-sums limits.** Loss ratios, medical-loss ratios (a regulatory requirement in US healthcare), debt-service coverage: `rule Claims <= 0.85 * Premiums` interacting with separate bounds on the components. The regulated quantity is a ratio of sums whose parts are governed by their own rules.
5. **Staged approvals jointly bounding a value.** Two rules from different lifecycle concerns — `rule Approved <= Requested` (approval stage) and `rule Requested <= BudgetLine` (intake stage) — jointly bound `Approved <= BudgetLine`. Authors correctly write these as separate rules *because they belong to separate concerns*; asking the author to also write the composite rule is asking them to hand-run the proof step the engine should own.

**Likelihood in Precept's target domains.** Finance, insurance, healthcare, logistics, and contracts are precisely the domains built on identities and layered caps — patterns 1–5 are not exotic corner cases there, they are the standard furniture. The honest assessment is that a domain expert authoring a realistically complete precept in any of these domains has a **high** chance of writing a pair of rules whose composition matters, and the "one file, complete rules" commitment cuts against telling them to manually flatten two rules into one.

**Prior art.** Multi-fact linear reasoning is table stakes in every serious invariant-checking system: SPARK/GNATprove discharges the bulk of its verification conditions through linear-arithmetic reasoning that freely combines hypotheses; refinement-type systems (Liquid Haskell and its relatives) are built on exactly this decidable linear-arithmetic fragment because real invariants routinely need it; and abstract interpretation moved beyond per-variable intervals to relational domains (octagons, polyhedra — the latter introduced by Cousot & Halbwachs in 1978 specifically because single-relation reasoning was insufficient for real programs, and deployed industrially in Astrée). No mature prover in this space stops at one-fact-at-a-time; the field's consistent experience is that combination is where relational reasoning starts paying.

**The obligation-shape caveat (from the prior assessment, and it matters).** The obligation "prove rule R still holds after this write" is the shape that would *most commonly* need two facts — the written field's new value plus a second rule constraining a sibling field. The engine does not currently generate that obligation. If rule-preservation-after-write ever becomes a generated obligation (a natural evolution for a prevention engine), §1b's need stops being prospective and becomes immediate.

**Verdict (non-corpus, first-principles):** §1b is a **legitimate candidate second phase**. It is not out of scope, and it is not "build only when a corpus case forces it" — the corpus cannot force it, by construction. It earns its place when either (a) the owner accepts that the target domains' standard patterns (blended caps, balance identities, apportionment, ratio limits, staged approvals) should discharge without authors hand-composing rules, or (b) rule-preservation-after-write obligations land. What keeps it out of the *first* phase is honest engineering sequencing — 3× the size, two spec gates, and §1a delivering the shared substrate (fact records, normalization, staleness) that §1b builds on. **Whether §1b enters the MVP or lands as phase 2 is the owner's call**; this document deliberately does not make it.

---

## 3. Build vs. buy

**The owner has decided: build.** The justification, on four grounds:

1. **LP solvers answer the wrong question.** Off-the-shelf linear-programming libraries return an optimization result ("the maximum of this objective is 42"), not a legible proof certificate. Precept needs "here is why this bound holds, in a form an author and an independent checker can read" — that artifact would have to be reconstructed around the solver anyway.
2. **Floating-point vs. exact decimal, and determinism.** Production LP solvers compute in floating point with tolerance thresholds. Precept's guarantee is exact-decimal and deterministic — same definition, same outcome, bit-for-bit. A tolerance-based "probably ≤" is not a proof the engine can stake prevention on.
3. **The problems are tiny, so overhead dominates.** At 2–6 variables per obligation, interop marshalling and solver startup cost more than the mathematics. OR-Tools/GLOP is a ~100 MB native dependency; the math it would do for Precept is microseconds of decimal arithmetic.
4. **Native-binary dependencies fight embeddability.** OR-Tools and HiGHS ship native binaries per platform; CPLEX is commercially licensed. Precept is an embeddable .NET runtime; a native solver dependency contradicts that shape.

**The reframe:** the cost of building was never the mathematics — eliminating a variable from a pair of inequalities is undergraduate algebra. The real cost is **exactness (production-grade rational arithmetic), certificate construction and checking, and integration with the engine's staleness/circularity disciplines**. That cost exists whether we build or buy; buying just adds a solver on top of it.

---

## 4. Measurement caveat

The performance baseline is now a **fresh measurement on current HEAD**, not archaeology. A compile-latency benchmark was added to `tools/Precept.Bench` (`CompileBench.cs`, run via `dotnet run -c Release --project tools/Precept.Bench -- --filter *CompileBench*`) and produced:

- **Full 77-sample corpus: 44.0 ms** — matching the documented ~44 ms exactly.
- **Worst single file: 1.23 ms** — *better* than the documented ~3 ms.

The worst single file is the unit that matters for interactive editing (the language server recompiles one file per debounced keystroke): at **~1.2 ms it uses roughly 2.5% of a 50 ms budget**, leaving ~40× headroom. Method caveat: a BenchmarkDotNet ShortRun (N = 3), so the corpus figure's confidence interval is wide; the means are stable and match the record. Nothing in this document's conclusions is sensitive to the exact figure — both §1a and §1b add microseconds against a millisecond-scale budget — but the number is now regenerable on demand rather than quoted from June.

---

## 5. Bottom line

- **§1a** — Medium build: ~500–700 engine LOC + ~1,000–1,600 test LOC, generalizing five verified existing mechanisms and splicing into the strategy cascade at `ProofEngine.cs:1076`. Performance impact: microseconds. Spec posture: fully inside the locked spec (`spec:256`). No owner gate beyond normal design review.
- **§1b** — Large build: ~1,500–2,200 code LOC + ~2,500–4,000 test LOC (fact DB, exact-rational elimination on a productionized `BigRational`, certificate + independent checker, counterexamples, re-derived staleness). Performance is fine at Precept scale and is **not** the deferral reason. Two owner gates: the `spec:256` single-pass/depth-bounded override, and `spec:225` ratification of the Farkas certificate as a legible witness. On first principles — the corpus being non-comprehensive and unable to answer the need question — §1b is a **legitimate candidate second phase**: the target domains' standard patterns (blended caps, balance identities, apportionment, ratio-of-sums limits, staged approvals) genuinely require combining facts, prior art uniformly treats multi-fact linear reasoning as essential, and the trigger conditions (owner accepting the domain-pattern case, or rule-preservation obligations landing) are concrete. Its phase placement is the owner's call.
- **Build over buy** — decided (build), on four grounds: certificates not optimization results, exact-decimal determinism vs. float tolerances, tiny problems where interop overhead dominates, and native-binary dependencies vs. embeddability. The cost is exactness + certificate + integration — not the math.

**Reserved for the owner, unresolved here:** (1) §1b in MVP vs. phase 2; (2) the `spec:256` override authorizing fact combination; (3) the `spec:225` ratification that a Farkas certificate meets the legibility bar.