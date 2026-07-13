# Soundness and Coverage

## Status

| Property | Value |
|---|---|
| Doc maturity | Design (skeleton) |
| Implementation state | Designed, not yet implemented — filled per implementation slice |
| Source | `src/Precept/Pipeline/ProofEngine.cs`, `src/Precept/Pipeline/ProofLedger.cs`, `src/Precept/Pipeline/NumericInterval.cs` (planned) |
| Upstream | Proof Engine (`proof-engine.md`), Graph Analyzer, Type Checker, Fault system |
| Downstream | Language Server (hover, `precept_proofs`), MCP proof DTOs |

> [!NOTE]
> **This is a skeleton.** Headings and one-line intents are laid down here so each implementation
> slice fills its own section. It is the canonical home for how the prove-or-reject guarantee is made
> sound: the certificate format, the witness, the deferred independent re-checker, and the coverage of
> the known fail-open holes. It cross-cuts the proof engine, graph analyzer, type checker, and fault
> correspondence.

---

## 1. The soundness model — certificate over trust

Precept's compile-time guarantee rests on prove-or-reject: when the compiler cannot prove a computed
value stays inside its declared limit, it **rejects the definition** rather than deferring to a runtime
check. Soundness is delivered two ways, and this doc is the home for both:

- **A legible certificate on every verdict** — the engine shows its work in a form a reader (and, later,
  an independent checker) can replay. Authority rests in the certificate, not in trusting the engine's
  say-so.
- **Direct engine correctness** — because the MVP ships no live re-checker (see §5b), MVP soundness comes
  from fixing the known fail-open holes in the engine directly and from the ⊥ / single-hop rails, not
  from an independent replay catching drift.

*(One-line intent — fill per slice: state the prove-or-reject contract, the "search is free, authority
is the certificate" principle, and how the two mechanisms above compose.)*

---

## 2. The three-way verdict

*(Intent — fill per slice.)* The `ProofVerdict` discriminated union — `Proven` / `ProvenViolating(witness)`
/ `Unresolved(condition)` — each case carrying only its own evidence. Both non-proven cases are
`Severity.Error` and reject. Cross-reference: `proof-engine.md` §13 (prove-or-reject MVP), and the
severity model in `diagnostic-system.md`.

---

## 4. The witness (§4a)

*(Intent — fill per slice.)* A `ProvenViolating` verdict carries a **witness**: one concrete configuration
that is re-evaluated against every collected fact and actually violates. The mandatory validation gate —
an unvalidated corner demotes to `Unresolved`, never presented as a proven violation. The per-family
witness grid (value witness / configuration witness / category-mismatch N/A) and the point-binding
limits (field leaves in the MVP; arg/element leaves demote to `Unresolved` until case-by-case narrowing
lands).

---

## 5. The certificate

### 5a. Certificate format (MVP prerequisite)

*(Intent — fill per slice.)* The format the MVP ships. A certificate is a small derivation: **premises**
(each quoting an author declaration — a bound, guard, rule, or reject-row — by source span) and **steps**
(each applying one primitive inference from the spec-enumerated `CertificateSteps` catalog, carrying its
own recomputable conclusion). Built during discharge from values the folds already hold — bookkeeping,
not a second analysis pass. Every step renders to one author-readable line. This is the home for the
`CertificateSteps` catalog vocabulary and the rendering contract that feeds hover / `precept_proofs`.

### 5b. Independent re-checker (deferred)

*(Intent — fill when scheduled.)* A second, independent implementation that replays a certificate and
fails loudly if a strategy's proof and its certificate disagree. **Deferred — not in the MVP.** Its value
is builder-side drift defense, not author unblocking; it is a separate, larger build than the format. The
MVP designs the certificate format to be re-checkable *in principle* so this checker is a later addition
rather than a retrofit — there is no checker-gated mint in the MVP (a `Proven` verdict is minted by the
discharge cascade, not by passing a replay first).

---

## 6. Soundness-hole coverage (Slice-0 fail-open fixes)

*(Intent — fill per slice.)* With the independent re-checker deferred, MVP soundness comes from fixing the
engine directly. An adversarial pass found four fail-open holes — each a discharge path that returns a
proved/clean verdict on an input it has not actually proven. Under prove-or-reject, unprovable must
**reject**, never pass. Each is a failing-test-first Slice-0 fix:

1. **Undecidable-magnitude default bound** — an undecidable bound check that silently stamped `Proved`
   must route to `Unresolved` (reject).
2. **Arg / field name collision** — a guard on an event arg must not narrow a same-named field's interval;
   add an arg-vs-field discriminator at the narrowing site.
3. **Sequential staleness ignored** — the bound-check narrowing path must consult `ReassignedBefore` and
   drop facts whose subject was reassigned earlier in the same handler, matching the sibling paths.
4. **Dead-end→dead-end suppression stamped as proved** — stop stamping a false `Proved` on unanalyzed
   dead-end rows; render the honest `Unresolved` (outcome-neutral once dead-end is an Error).

Plus the **systematic fail-open sweep**: audit every `bool?`-returning discharge path (does `null` route
to reject?) and every dict-write site into the narrowed-interval dictionary (does it guard the empty
interval before storing?). The four named holes are the found instances, not a closed set.

---

## 7. Rails — ⊥ suppression and single-hop

*(Intent — fill per slice.)* The soundness-critical rails the strategies must not breach: the ⊥ (empty
interval) suppression at dict-write time, the kernel ⊥-guard inside the numeric-interval arithmetic
methods, and the single-hop / anti-transitivity rail (the engine reads a cross-field relation exactly
once and never chases a third field — spec §0.6 single-pass, depth-bounded).

---

## Cross-References

- [`proof-engine.md`](proof-engine.md) — the engine these fixes and the verdict/certificate live in.
- [`diagnostic-system.md`](diagnostic-system.md) — the severity model (three-way verdict + structural
  severity families).
- [`graph-analyzer.md`](graph-analyzer.md) — process-topology diagnostics whose severity the structural
  family covers.
- [`../language/precept-language-spec.md`](../language/precept-language-spec.md) §0.6 — the proof-engine
  design contract and proof philosophy.
