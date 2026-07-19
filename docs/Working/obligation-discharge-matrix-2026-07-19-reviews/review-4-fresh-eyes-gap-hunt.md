**Status**: Advisory review - 2026-07-19 (agent-authored fresh-eyes gap hunt over the matrix program; evidence only; rulings remain the owner's)

I've read the matrix (rev 3), the want doc, the decision packet, philosophy.md, the three review verdicts, and verified supporting facts in the repo (spec-freeze tag, proof-engine.md status, LS code-action machinery, CONTRIBUTING tracks, certificate-steps doc, corpus rule counts). Here is the report.

---

# What else is missing — findings not on the tracked list

## F1 (highest) — The amendment protocol has no soundness-shrink path; monotone growth forbids fixing an unsound cell

**Missing.** Tracked item 11 conceives amendments as *widening* the licensed set (monotone growth, band-as-candidate-pipeline). The inverse case is unhandled: a licensed derivation discovered post-ratification to be **semantically unsound** — a discharge contract that licenses a program which can actually violate its rule at runtime. Under the exact-iff ruling this is not a latent bug the compiler may conservatively decline; the definition *mandates* acceptance ("a compiler that accepts a program the contract does not license is nonconforming" — the same sentence makes a compiler that *rejects* a licensed program nonconforming, matrix `:33`). Fixing it requires **shrinking** the licensed set, which monotone growth forbids, which breaks previously-issued certificates and previously-compiling author files simultaneously.

**Why it matters.** This is not hypothetical: the repo has a live instance of exactly this shape — the `number` add/subtract interval transfers carry no float-rounding widening (`Operations.cs:171/:175`, DECISION-PACKET S5, confirmed at HEAD). Had a cell been ratified over that transfer rule, the definition would today *require* accepting unsound programs, and the fix would be a non-monotone definition edit with no governing protocol. In six months, the first soundness bug found in a ratified cell either sits unfixable (protocol says grow-only) or gets fixed ad hoc, breaking the versioning/certificate story item 12 is building.

**Cheapest first step.** Add one section to the amendment-protocol design (when item 11 is written): a *soundness-correction* amendment class — shrinks permitted, always a major version, always breaks certificate replay, always routed to the owner with the witness program that demonstrates unsoundness. Distinguish it explicitly from power-widening amendments.

**Grounding.** Matrix `:33` (iff both directions); DECISION-PACKET §(c)7 and S5 (`Operations.cs` drift — the existence proof that licensed-but-unsound happens in this codebase).

## F2 — No definition-soundness meta-obligation: nothing obliges each licensed derivation to be *valid* against runtime semantics

**Missing.** The matrix defines *which* derivations close (discharge contracts, 21 step kinds with bounded recompute rules), and the checker verifies a certificate *replays* those rules. Nothing anywhere obliges the rules themselves to be truth-preserving — that each step kind's recompute rule, each strategy's contract, and each transport rule (guard-fact through prior writes, vacuity-by-activation, sign tables) is proven or at least argued sound w.r.t. the evaluator's actual semantics. The certificate-steps doc (`certificate-steps-membership-2026-07-12.md` §Semantic Rules) specifies *replayability in bounded time*, which is determinism, not validity. The tracked "one semantics" work (packet outline §14) covers only the arithmetic instance.

**Why it matters.** Exact-iff makes every definition defect *normative* (see F1) — so the definition itself becomes the trusted computing base, and it currently has no per-rule validity argument. Event-B/Rodin, the matrix's closest precedent (review-3 §1), ships its PO-generation rules with published soundness meta-theory; the matrix ships them with citations to the want doc, which is intent, not validity. A hand-authored WP or transport rule with an off-by-one becomes canon, then law.

**Cheapest first step.** Add a required column/section per generative sentence (each discharge contract and step kind): a one-paragraph validity argument against the evaluator semantics, or a citation to one — mirroring the four-leg rationale rule the repo already enforces for decisions (CLAUDE.md § Per-Decision Rationale). Make it a ratification gate: no cell ratifies whose derivation cites an argument-less rule.

**Grounding.** Matrix `:33`, `:40`; `docs/Working/certificate-steps-membership-2026-07-12.md` (recompute rules ≠ validity proofs); `docs/philosophy.md` honesty-about-approximation (`:23-25`) — which the proof theory must satisfy per-step, not just per-arithmetic-lane.

## F3 — The reject side of iff is uncheckable by the certificate mechanism, and no discharge contract states a decision procedure

**Missing.** The iff contract has two halves. The accept half ("a licensed derivation closes it") is checkable — the certificate carries the derivation. The **"never more" half is structurally uncertifiable**: a rejection carries no certificate of non-derivability, so nothing in production or at load ever verifies that a rejection was correct, and nothing verifies the *compiler's prover is complete* relative to the licensed system (a conforming compiler must find *every* licensed derivation — incompleteness is nonconformance under iff, matrix `:33`). Relatedly, no discharge contract states that licensed-derivation-existence is *decidable* — with premise class (d) ranging over the whole rule set and multi-step transports, "no licensed derivation exists" needs a stated search bound or decision procedure per contract, or conformance on the reject side is not even testable in principle. The only assurance surface currently planned is the finite witness/near-miss test set (matrix `:58`, `:221`).

**Why it matters.** The failure this causes: a compiler version that silently under-proves (rejects licensed programs — e.g., a regression in premise enumeration) passes every certificate check, passes load gates, and is caught only if a finite near-miss/witness test happens to cover the regressed cell. Six months in, "the compiler conforms to the definition" will be claimed on evidence that structurally cannot support the reject half.

**Cheapest first step.** Two sentences in the matrix's Vocabulary: (1) each discharge contract must name its decision procedure or search bound (the §1a/§1b tiers already do this implicitly — make it explicit per cell); (2) an honesty note that reject-side conformance is test-assured only, so the cell-to-test conversion (§ Eventual use) is not a convenience but the *sole* verification mechanism for half the iff — which raises the bar on test-generation completeness accordingly.

**Grounding.** Matrix `:33`, `:58`, `:221`; want `:199-206` (certificates are accept-side artifacts by construction).

## F4 — No authoring model: nothing defines what a domain expert must understand to predict acceptance

**Missing.** Philosophy is explicit that the primary author is a domain expert/business analyst, not a developer (`docs/philosophy.md:92`). Under exact-iff, acceptance turns on **normal-form-equality** to a compiler-computed WP — a sound guard that *implies* the WP but isn't normal-form-equal rejects (the matrix's own Base A band member, `:188`). The author's ability to predict acceptance therefore depends on internalizing the normal form, the premise classes, and the licensed-derivation shapes — and no author-facing artifact defines that model anywhere: not the language docs, not a spec section, nothing (verified by search). Related unhandled consequences, same root:

- **Diagnostic register.** The suggestion schemas are written in prover vocabulary ("add a guard normal-form-equal to ⟨WP⟩", matrix `:36`; "weakest precondition", spec `:229`). The repo's own no-coined-jargon rule says undefined terms in artifacts propagate — these will land verbatim in diagnostics aimed at business analysts unless a register translation is defined.
- **Non-local regressions.** Premise (d) makes acceptance file-global: deleting rule R can break proofs of *other* rules that consumed R as an inductive hypothesis. The deletion method celebrates this at spec level; the author experiences it as spooky action ("I removed a rule and three unrelated handlers stopped compiling"). The certificate's load-bearing marking (want `:210`) is exactly the machinery to explain it, but no premise-loss regression diagnostic is defined anywhere.
- **Scaffolding rules need reasons.** The matrix's own escape hatch for the deferred multi-fact solver is "restating the combined fact as a third rule" (edge cell 1, `:208`). Rules carry a mandatory `because` (`philosophy.md:15`) — the honest reason for that rule is "the prover needs it," i.e., machine scaffolding leaking into the business specification with a fabricated business rationale. Unruled.

**Why it matters.** This is the program's primary product risk: the respellability rulings establish the power gap "costs authoring friction only" — but nobody has defined what that friction *is* or bounded it for the stated persona. If the answer to "why was my sound guard rejected?" requires understanding normalization theory, the domain-expert positioning fails even while every cell is correct.

**Cheapest first step.** Add "author-facing acceptance model" as a named deliverable — one language-doc section stating, in the author's vocabulary, the rule for when a guard/bound closes a proof ("your guard must state the post-state condition itself, in the same shape the rule states it") — and make the normalization normal form (tracked item 2) explicitly co-owned by author-predictability, not just proof theory: every normalization rule added must be author-explainable in one sentence.

**Grounding.** `docs/philosophy.md:92-94`; matrix `:32`, `:35-36`, `:188`, `:208`; spec `:229`; DECISION-PACKET g.4 (false-rejection rate for real authors: UNKNOWN, no study).

## F5 — The shipped quick-fix machinery collides with "it never inserts the premise itself" — an unruled product conflict

**Missing.** Want `:179`: the compiler "never inserts the premise itself. The author writes it, deliberately: a premise the author never wrote is deferral relabeled as authorship." The shipped language server does the opposite by architecture: "Every diagnostic with a non-null `FixHint` in `DiagnosticMeta` generates a code action … whether the action carries a `TextEdit`" (`docs/tooling/language-server.md:1140`, with one-click apply at `:1127`). When the missing-premise diagnostics land with computed closing premises in their text, the existing pipeline will mechanically turn them into one-keystroke premise insertion — the exact act the want forbids, unless someone rules whether a Ctrl+. apply counts as "the author writes it."

**Why it matters.** Whichever way it's ruled, a surface must change: either the new obligation diagnostics are exempted from TextEdit code actions (a deliberate carve-out in the LS enrichment design, currently nowhere), or the want's sentence gets an owner-approved scoping. Left unruled, the default path ships auto-insert and silently converts the teachable-diagnostic philosophy into obey-the-tool — the failure mode the want names explicitly.

**Cheapest first step.** One owner question, Tier-2 conversation per CLAUDE.md: "does a quick-fix TextEdit that inserts the suggested guard violate want `:179`?" Record the ruling; add the LS/MCP/hover propagation row to the population plan's doc-sync obligations either way.

**Grounding.** Want `:179`; `docs/tooling/language-server.md:1090-1159`. (Beyond this one conflict, tooling propagation is mechanical: the doc-sync table already routes it, and tracked items 3/4 cover the catalog side. `precept_proofs` derives from the ProofRequirements catalog, so it updates when the pending rule-obligation catalog entries land — nothing else structurally hard found.)

## F6 — Evidence base: respellability is asserted, never measured; the 59 rule-bearing samples have never been adjudicated under the matrix contracts

**Missing.** Every respellability verdict in the matrix is model-derived (matrix `:188` says so honestly). Meanwhile 59 of the 77 corpus samples contain `rule` statements, all authored under pre-want semantics where rules mint zero write-site obligations — no one has measured how many of those files *reject* under the matrix's discharge contracts, what their band members look like, or whether their respellings are the "friction only" the verdicts claim. The packet's own g.4 flags the adjacent unknown (false-rejection rate for non-self-authored authors; the ~89% in-fragment figure is a different measurement — fragment membership, not discharge-contract licensing). The 2026-07-14 probe failure additionally warns that model-derived verdicts at scale are exactly the artifact class that proved untrustworthy in this repo.

**Why it matters.** Respellability is now load-bearing (owner-ruled per cell) and it's the one claim standing between "iff-strictness is fine" and "iff-strictness is an expressiveness loss routed to the owner." If population ratifies dozens of model-derived "respellable: yes" verdicts and the corpus later shows clusters of sound-but-unlicensed spellings with ugly respellings, the surface-shrink decisions arrive *after* canonization instead of before.

**Cheapest first step.** A bounded, deterministic pass (the kind the gap-probe salvage memory prescribes): for each of the 59 rule-bearing samples, enumerate its rule×write-site obligations against the matrix's contracts *on paper/agent-mechanically* (no engine needed), classify each as licensed / band / respell-needed, and attach the tally to the matrix as measured evidence before ratification. This doubles as the composed-exemplar corpus the deletion-method section wants.

**Grounding.** Matrix `:143`, `:188` ("model-derived"); DECISION-PACKET g.4; 59/77 count verified by grep at HEAD.

## F7 — Ratification protocol and matrix storage format: what "ratified" means at matrix scale is undefined, and prose markdown collides with the repo's own catalog-first principle

**Missing.** The population product will be large (the family-indexed product across 6 rule structures × 5 write-site categories × read-set combinations × 4 type families, plus fault/editable/structural shapes — the sibling gap-analysis enumerations ran to hundreds of cells per unit). Three things are undefined:

1. **What owner ratification means** for a document too large to read line-by-line. The repo's own memory doctrine holds that agent-authored "ratified" labels don't bind the owner — so a bulk stamp over hundreds of agent-populated cells recreates the exact authority fiction the project has already burned on. Tracked item 8 (population plan) plans the *work*; the ratification *protocol* (what the owner actually reads, what's spot-checked, what's mechanically verified, what "ratified" certifies) is a distinct missing artifact.
2. **Verification-before-ratification of hand-derived content.** Most cells cannot carry ✓v until the rule-obligation machinery exists — the definition will be ratified largely model-derived (hand-computed WPs included). A hand-computed WP with an arithmetic slip becomes normative under iff (F1/F2 compound this). At minimum, WP computation should be mechanized (a standalone WP calculator over the write-plan grammar is small and buildable *before* the prover) so cell schemas are machine-derived even where discharge status can't be.
3. **Storage format.** The matrix as canonical *prose* is domain knowledge maintained outside a catalog — the inverse of the repo's non-negotiable catalog principle, and both tracked item 4 (maintenance checker) and the "converts mechanically into tests" claim (`:221`) actually require cells to be machine-readable. The format decision (cells as structured data with generated/derived prose, vs markdown tables) is upstream of the checker and cheaper to make before population than after.

**Why it matters.** Populating first and deciding these after means re-entering hundreds of cells or ratifying an unverifiable artifact. The CONTRIBUTING Track B gate ("all inline review comments resolved") was designed for normal-sized design docs and will need explicit adaptation for this artifact regardless.

**Cheapest first step.** A one-page ratification-protocol note settled with the owner before population starts: cell format (data vs prose), the owner's read/spot-check contract, the machine checks that must pass per cell (WP recomputation, citation resolution, disposition totality), and staged-vs-bulk ratification.

**Grounding.** CLAUDE.md § Catalog System; CONTRIBUTING § 3 Track B; matrix `:143`, `:221`; the 2026-07-14 probe post-mortem (memory) as precedent for what unverified bulk enumeration produces.

## F8 — No standing authority ordering after ratification: the matrix, spec §0.6, and proof-engine.md become three canonical proof-engine sources

**Missing.** `docs/compiler/proof-engine.md` is a Full-maturity canonical stage doc for the same engine; the frozen spec (`spec-freeze-2026-07-13` tag, live §0.6 text at `precept-language-spec.md:225-256` with its own locked proof-philosophy decisions) is normative for the language. Tracked item 9 corrects *known contradicting sentences*, but the structural disease the 07-15 forensics identified — canon asserting both answers — recurs whenever two canonical docs both *may* speak to proof semantics and a *future* divergence appears. What's missing is the standing rule, written into canon at ratification: the matrix is the single owner of proof-engine semantics; spec §0.6 and proof-engine.md are re-scoped (surface/architecture respectively) and *cite* rather than restate; any future proof-semantics sentence outside the matrix is a defect by rule, not by comparison. Same pass should include a **section-level supersession map for the want doc itself** — it's superseded "for the proof-engine surface" only, and its surviving halves (runtime role, MVP boundary, open questions) otherwise become exactly the stale-framing hazard the reconcile-before-feeding-agents memory warns about.

**Why it matters.** Without the standing rule, the correction worklist fixes the past and the disease reinfects the future — this is the mechanism by which both boundary disputes originally arose (forensics commit 395e677f).

**Cheapest first step.** One paragraph in the matrix's Authority section (it already handles drafting-time and ratification-moment authority; add *steady-state* authority), plus a re-scoping line in proof-engine.md §1 and spec §0.6 at promotion.

**Grounding.** Matrix § Authority; `docs/compiler/proof-engine.md:1-10`; `precept-language-spec.md:229`; git tag `spec-freeze-2026-07-13`.

---

## Dimensions probed and found covered (one line each)

- **Premise-(d) non-locality at the definition level**: handled — discharge contracts are class-relative, the induction is simultaneous over the rule set (standard Event-B shape), and the matrix already flags the compositionality claim for the canonical doc (`:137`); the *author-facing* residue is in F4.
- **Composed-file deletion vs iff**: the consumed/unconsumed asymmetry section (`:139`) plus the per-family deletion exemplar cover it; no new interaction found beyond F3's reject-side testability.
- **Load-gate refusal UX / certificate-mismatch in production / restore×versioning**: covered by tracked items 6 and 12 plus packet Q3 — no residue found beyond what those already scope.
- **Performance at population scale**: ruled out on measured evidence (full corpus ~44 ms; project memory records the incremental-compilation ruling).
- **Certificate tamper/forgery**: self-answering under PCC — a forged certificate that verifies is a valid proof; the checker, not provenance, is the root of trust (want `:204`), and item 5 owns the format.
- **Q-item gating**: tracked item 8 already owns the gating map; the matrix's `⧖` marks are consistently placed.

**Rank order**: F1, F2, F3 (the iff ruling's unhandled meta-consequences — these can make the ratified definition wrong and unfixable), then F4, F6 (product/persona risk on unmeasured claims), then F5, F7, F8 (process and surface conflicts that are cheap now and expensive after population).