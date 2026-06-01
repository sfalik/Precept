# Spec-Conformance Audit & Remediation — 2026-05-30

**Status**: Active — candidate register. Goal: find every spec↔implementation gap and fix the implementation (the spec is authoritative). This register is the **first seed of Phase 7 (total language conformance sweep)** of the compiler-readiness plan (inserted 2026-05-30) — one input among several, known to be non-comprehensive. Phase 7 also commissions fresh audits, builds conformance tests, and probes the catalog surface, running slice after slice until the owner is satisfied the compiler is accurate to the spec. The Phase 7 finding register is the living tracker; this audit is its first contributor.

**Method (non-negotiable, to keep findings trustworthy)**:
- The spec (`docs/language/*.md`) is the source of truth. Any spec-vs-code mismatch = an implementation gap to fix (not a spec change), unless the owner rules a specific spec line is itself wrong.
- Every row starts `unverified`. It is promoted to `CONFIRMED` only when this doc records (a) the **verbatim spec claim + line**, including the spec's **canonical declaration form**, and (b) a **direct MCP probe of that exact form** with its result. It is `DROPPED` if the probe shows the agent used a non-canonical form and the canonical form actually works.
- No agent summary is treated as fact. Candidates below are sourced from the 2026-05-30 audit sub-agents and are **unverified** until checked here.

**Why a register**: prevents the "synthesize across everything at once" failure. One row, one spec quote, one probe, one verdict — independently checkable.

---

## Verdict legend
- `unverified` — candidate from an audit agent; not yet re-checked here.
- `CONFIRMED` — spec canonical form quoted + my probe reproduces the gap.
- `DROPPED` — canonical form actually works; agent used a non-canonical form.
- `DOC-ONLY` — mismatch is the doc being stale, implementation is correct → doc fix, not impl fix (separate track; owner confirms spec is the stale side).

---

## A. Functional gaps — spec describes a feature the compiler does not deliver

| # | Spec claim (doc:line) | Canonical form per spec | Probe result | Verdict |
|---|---|---|---|---|
| A1 | `price × quantity → money` cancels; `business-domain-types.md:211-220` headline example | `field OrderQty as quantity in 'each'` (the `in 'unit'` form) | `price in 'USD/each' * quantity in 'each'` → PRE0114, quantity Dimension "unresolved" | **FIXED (Phase 7 Slice 1, 2026-05-31)** — root cause: qualifier resolvers lacked a `Dimension ← Unit` projection (the quantity's `in '<unit>'` qualifier carries its dimension but was never consulted on the Dimension axis); added `TryProjectUnitToDimension` at all four projection sites in `ProofEngine.Qualifiers.cs`. Same-unit cancellation now works (headline example compiles). **Caveat:** the fix matches at *dimension* granularity, so same-dimension/different-unit (`'USD/kg' × quantity in 'g'`) now cancels silently (scale-factor hole) — deliberately left open (owner decision) pending the cross-unit policy decision tracked in [`price-cross-unit-cancellation-2026-05-31.md`](price-cross-unit-cancellation-2026-05-31.md). |
| A2 | `exchangerate` declaration — **two contradictory forms appear in the spec** | (see both) | (see both) | **NEEDS OWNER RULING** — spec/catalog contradiction, not a clean impl gap |

**A2 detail (spec-vs-catalog contradiction — owner decision required):**
- **Slash form `exchangerate in 'USD/EUR'`** — 9 occurrences in `business-domain-types.md` (incl. type-summary table :358 and the "Fixed" declaration row :982) → **PRE0076, does not parse.**
- **`to` form `exchangerate in 'USD' to 'EUR'`** — `business-domain-types.md:178`, `collection-types.md:602`, AND the **`Types` catalog `UsageExample` (`Types.cs` L604)** → **compiles clean.** `to` is a real keyword (`TokenKind.To`, `QualifierAxis.ToCurrency`, `DeclaredQualifierMeta.ToCurrency`).
- The implementation matches the **catalog** (`to` works). Per CLAUDE.md the catalog is "the language specification in machine-readable form" — i.e. the more authoritative of the two spec surfaces.
- **OWNER RULING (2026-05-30): SLASH is canonical.** `exchangerate in 'USD/EUR'` is the intended syntax. Git history showed the `to` form was AI prose-drift in docs-only commit `d5c6532c` (a philosophy/approximation-stance pass, 0 .cs files) that then propagated into the catalog UsageExample + impl, while the original 7 slash examples (commit `1c352fa2`) were never honored by the parser. So: **impl + catalog must parse `'X/Y'` and split into from/to; the `to` form is removed.** This is a functional impl gap (CONFIRMED), not DOC-ONLY.
  - Fix scope: parser/type-checker `exchangerate` qualifier parsing (split `'USD/EUR'` → FromCurrency=USD, ToCurrency=EUR; reject bare single unless numerator-only "Partial" mode per `:983`); remove `to`-form support; update `Types.cs` UsageExample (L604) to slash; sweep `business-domain-types.md:178`, `collection-types.md:602`, and any `to`-form tests to slash.
- *Process note: I mislabeled this DROPPED then CONFIRMED before establishing it's a contradiction; git history then settled it. The register caught both flips.*
| A3 | `date ± '<n> days/months/years/weeks'` → date; `temporal-type-system.md:439-443` | `date + '30 days'` (inline typed-constant quantity) | `date + '30 days'` → PRE0056 (treats literal as a date) | unverified — re-confirm spec form + probe |
| A4 | time-denominator compound quantity unit `quantity in 'kg/hour'`; `business-domain-types.md:128,1086` | `quantity in 'kg/hour'` | agent: → PRE0075 "not a valid unit" | unverified |

## B. Enforcement gaps — spec says compile error, compiler accepts (soundness holes)

| # | Spec claim (doc:line) | Probe (agent-reported) | Verdict |
|---|---|---|---|
| B1 | collection inside string interpolation is a type error (`primitive-types.md:122`, `collection-types.md:867`) | `"{Tags}"` compiles clean | unverified |
| B2 | `dequeue/pop F into G` where G type ≠ element type → `TypeMismatch` (`collection-types.md:239,293`) | compiles clean | unverified |
| B3 | `queue of T by P` requires P orderable (`collection-types.md:64,1137`) | `queue of string by string` clean | unverified |
| B4 | `in`/`of` not available on identity types (`business-domain-types.md:363`) | `currency in 'USD'` clean | unverified |
| B5 | cross-partition `quantity.dimension == period.dimension` is a compile error (`business-domain-types.md:792`) | clean | unverified |
| B6 | `unitofmeasure` value with structural `/` rejected (`business-domain-types.md:715,756`) | `'kg/m'` accepted | unverified |
| B7 | `-exchangerate` and `exchangerate < exchangerate` are compile errors (`business-domain-types.md:1005,1007`) | both clean | unverified |
| B8 | legacy timezone abbrev `'EST'` rejected (`temporal-type-system.md:900`) | accepted | unverified |
| B9 | `mid/left/right` negative arg rejected (`primitive-types.md:672`) | `mid(Name,-1,3)` clean | unverified |

## C. Diagnostic identity gaps — rejected correctly, but wrong/generic code vs spec's named one
*(Behavior sound; question is whether spec names the wired code. Includes the PRE0073 seed: `price in 'USD/days' * duration` → PRE0114 not `DurationDenominatorMismatch`. Many of these are downstream of A1/A2 — the chain proof failing emits generic PRE0114 instead of the specific code. Re-check after A-class fixes.)*

- C1 — PRE0073 `DurationDenominatorMismatch` not emitted for its spec example (emits PRE0114)
- C2 — `DenominatorUnitMismatch` / `CompoundPeriodDenominator` likewise generic PRE0114 on the `×` path
- C3 — collection action errors: spec names `CollectionOperationOnScalar`, impl emits `ScalarOperationOnCollection` (possible swap) + several `set Coll=` → PRE0018
- C4 — temporal "teachable message" drift: ~15 cases reject via generic PRE0018 instead of the spec's per-case teachable message

## D. Doc-stale drift — implementation is correct, the spec/doc is out of date (DOC-ONLY track)
*(Owner: confirm these are the doc being stale, not the impl. If spec is truly authoritative everywhere, some may flip to impl work.)*

- D1 — `precept-language-spec.md` §0.6 status table marks obligations 7-10 (contradictory/vacuous rule, dead/tautological guard) "Specification-only / Phase 5" but they are **implemented and firing** (PRE0153-0155, PRE0082)
- D2 — `collection-types.md` Quantifier Predicates marked "Not yet implemented" but fully wired
- D3 — composite malformed-basis codes (PRE0160-0162) marked "TBD" but shipped
- D4 — `temporal-type-system.md` cites non-existent code spellings (TEMP005/007/017, PRECEPT0007, C92/C93)

## E. Unit-scale honesty gaps (silent irrational approximation) — found 2026-05-31 (Phase 7 Slice 2 enumeration)

Surfaced while mapping full-UCUM special-unit behavior for the cross-unit cancellation work. The catalog silently holds rational approximations of irrational scales, with no flag to surface them, and one path already reads them. Honesty/soundness against P8 ("the exact/approximate line must be visible"). Verdicts CONFIRMED by code read + probe.

- **E1 — `[pi]`-derived units carry a silent rational approximation of π.** `deg`/`rad`/`gon`/`'`/`''` reduce through `[pi]` (a 64-digit rational literal in `ucum-essence.xml`), so their `UcumExactFactor.Scale` is an *approximation of an irrational* with no `exact/approximate` marker. `ProofEngine.Intervals.cs:295` (`ApplyStaticUnitScaling`) + `TypedConstantNormalizer.cs:91` already read `Scale → decimal` for interval proofs, so a bound check on a `deg`-valued typed constant already trusts an approximated scale while reporting `Proved`. **CONFIRMED.** *Mitigation begins in Phase 7 Slice 2 (the `ScaleIsRational` flag).*
- **E2 — log units have their function stripped → meaningless scales.** `dB`/`B`/`Np` lose their `lg`/`ln` wrapper (`UcumAtomCatalog.cs:466`), leaving scales (`dB`=0.1, `Np`=1) that bear no relation to the true `dB↔Np` log relationship (≈8.686). Cross-log cancellation cancels on dimension name (`count`) but any code that "converts" with these scales computes a wrong number. **CONFIRMED.** *Routed to Phase 7 Slice 6 (log cross-unit policy — design ruling needed).*
- **E3 — angle units rejected vs. allowed.** `deg`/`rad` resolve to an *empty* dimension name (`UnitDimensionHelper.cs:48`) → `PRE0114` even same-unit, diverging from the locked cross-unit-cancellation design (allow + surface). **CONFIRMED.** *Routed to Phase 7 Slice 5.*

---

## Coverage gaps in the audit itself (not yet probed)
- `precept-language-spec.md` §1 (lexer), §2.7 (parser), §3.10 (full diagnostic catalog), §3A semantics — not audited.
- The 162 `DiagnosticCode` members: not cross-checked for "emits from a real path" (the catalog-completeness lens — **Phase 9**'s stated job; distinct from the conformance/identity probing this register drives, which is **Phase 7**).

## Phase routing (where each cluster gets fixed)

This register is the durable tracker — nothing is lost as long as every row lands in a phase here. **This register is Phase 7's driver doc** ([`compiler-readiness-plan-2026-05-24.md` § Phase 7](compiler-readiness-plan-2026-05-24.md)): the "total language conformance sweep" inserted 2026-05-30 is the execution arm for these rows.

- **A-class (functional gaps: A1 price×quantity, A2 exchangerate slash, A3 date+literal-quantity, A4 kg/hour compound)** → **Phase 7** (this audit doc is its driver). Phase 3/5 (the natural type-system/proof-engine owners) are closed. **A1 and the PRE0073/compound-denominator C-items may share ONE root cause** (qualifier-chain resolver not reading the right operand's `in`-declared qualifier — same `PRE0114 "unresolved"` signature; the analogous period case was fixed during Phase 6 W-C). **Verify that grouping before routing** — if shared, the temporal C-items ride with A1 in Phase 7.
- **B-class (spec says error, doesn't fire — soundness holes)** → **Phase 7** (re-homed from the old Phase 8: these are conformance defects — the language fails to reject what the spec declares illegal — which is Phase 7's charter, not the catalog-completeness lens).
- **C-class (wrong/generic code, incl. PRE0073)** → **Phase 7** when entangled with A1's root cause (the named spec diagnostic must fire); standalone catalog-wiring C-items → **Phase 9** (diagnostic completeness / emission architecture). The A1↔C1 grouping check decides.
- **D-class (doc-stale)** → doc cleanup sweep within Phase 7 W-F (incl. the slash `to`→`/` doc/test sweep from A2, which rides with the A2 impl fix).

**Phase 6 (composite period basis) — what actually stays here:** only **W-D** (scenario-test matrix + lease sample + doc-sync) remains. PRE0073 is *temporally* adjacent to W-C but is mechanically a C-class diagnostic gap likely entangled with A1 — so it routes with the A-class root-cause fix, NOT as a Phase 6 item, pending the grouping check. (Recorded so it is not lost: see C1.)

## Next actions
1. Verify each candidate (spec canonical form + probe) → flip to CONFIRMED / DROPPED / DOC-ONLY. **In progress.**
2. Owner ruling on the D-track (doc-stale vs impl).
3. Sequence CONFIRMED gaps into fix slices; fix one at a time.

## Verification log
- 2026-05-30: A1 CONFIRMED (price×quantity `in 'unit'` form does not discharge — spec headline example `business-domain-types.md:211-220`).
- 2026-05-30: A2 — labeled DROPPED → CONFIRMED → finally **NEEDS OWNER RULING** across three steps. Root cause of my flip-flop: I kept treating it as a single-canonical-form question when it is actually a **spec-vs-catalog contradiction** (slash form in business-domain-types prose vs `to` form in the `Types` catalog UsageExample + working impl). The right verdict isn't CONFIRMED or DROPPED — it's "two parts of the spec disagree; owner picks which is canonical." **Lesson: before labeling a gap, check whether the spec is internally consistent on the point. If two spec surfaces disagree, it's an owner ruling, not a find-and-fix.**
