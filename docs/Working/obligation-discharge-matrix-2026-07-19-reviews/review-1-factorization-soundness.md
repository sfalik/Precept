**Status**: Advisory review - 2026-07-19 (agent-authored adversarial review of the matrix premise; evidence only; rulings remain the owner's)

All four documents read (the matrix, the want doc, proof-engine.md including the ProofRequirement DU at :529-543 and §14 exclusions, DECISION-PACKET Q1-Q14, and the §1a/§1b boundary section of `proof-engine-mvp-and-proof-phases-2026-07-12.md` the matrix cites). Review follows.

---

# Adversarial review — obligation-discharge-matrix-2026-07-19.md

## What is sound (stated explicitly, with reasons)

**S-1. The triple ≅ certificate isomorphism is genuinely load-bearing, not decorative.** The matrix's case shape (obligation-as-theorem, premises, derivation) is exactly the certificate record the want doc specifies at `what-i-want-2026-07-16.md:199` ("per obligation, the theorem, the premises used … and the derivation"). One shape flows from diagnostic → test → certificate, and an exhaustive matrix is precisely the obligation-enumeration substrate that decision-packet Q2 (checker coverage root) needs on either of its two options. This is the strongest structural idea in the doc.

**S-2. The pair-of-behaviors cell definition is sound and anti-drift by construction.** Defining a cell as (base rejects, naming the missing premise) + (base+addition discharges via expected strategy) makes the diagnostic's suggestion and the fix the same artifact — a structural implementation of the want doc's teachable-message requirement (`:179`) that cannot drift, exactly as the matrix claims at :22.

**S-3. WP-through-the-write does real definitional work for the authored-handler rule family.** The Base A/B/C contrast (:109 — same rule, three WPs, different candidate premise classes) is a genuine insight, and WP handles the want's symmetric write site (`ReduceLimit`, want :103) *uniformly* — no special axis needed for "the field the author wasn't thinking about." Within its home family, the core is right.

**S-4. The provable-under-want vs proven-today split via strategy build status (:32, ✓v marks) is sound** — it keeps model-definition and implementation-status honest and separately falsifiable, which the 2026-07-14 probe failure showed is essential.

**S-5. The edge-cell discipline (no silent cells; every cell defined/deferred/open with citation, :111-121, :127) is the right completeness mechanism** — *for the cells the axes generate*. That qualifier is where the findings below live.

---

## Findings

### F-1 — BLOCKING-THE-PREMISE: the write-site-category axis is missing; the second ingress point has no cell

**Claim.** The matrix's "write structure (overwrite / read-modify-write / no-write)" (:125) classifies the *shape of an authored assignment's RHS*. It cannot generate a cell for **editable-field writes** — one of the want doc's exactly-two ingress points (want :141) and the want doc's own worked case (`DailyWithdrawalLimit`, want :106, :129: "that ingress check is the declared premise, and the certificate marks it load-bearing"). An editable write has no authored RHS, no guard, no arg; its discharge is the ingress evaluation of the field's modifier-rules *plus every rule that mentions the field* (want :143) — a premise that is not (a), (b), (c), or (d) as the matrix's premise-class vocabulary (:30) defines them, or is at best an undeclared extension of (a). The word "editable" does not appear in the matrix.

The same axis gap swallows the other write-site categories the packet's confirmed comprehensiveness finding says the symmetric sweep must enumerate or "the symmetric-obligation guarantee is silently unsound" (DECISION-PACKET §(a)3 M-completions; §(c) item 9): entry hooks, state actions, stateless event hooks, the non-`set` actions, and computed-field (`<-`) transitive write sites. None has a coordinate.

**This is the hidden fifth axis** the enumeration needs: **write-site category** (handler `set` / editable edit / entry hook / state action / collection action / computed-field transitive), orthogonal to RHS shape. The matrix currently conflates the two into one axis and worked only the (handler set × authored RHS) column, which makes the exhaustiveness claim at :127 ("exhaustively documented when every non-empty cell of the pruned matrix carries a disposition") false as stated — the pruned matrix structurally cannot contain the editable-write cell, so its silence is invisible to the mechanism whose whole point is that silence is impossible.

**Amendment (smallest).** Split the axis: write-site category × RHS/read-set shape (RHS shape applying only where an authored expression exists). Add premise class (e) "ingress evaluation of the applicable rule set" — or an explicit owner-visible ruling that it is class (a) generalized — citing want :143. The categories can be lifted from the spec's mutation-surface enumeration (`precept-language-spec.md:1971`, per the packet) rather than invented.

### F-2 — BLOCKING-THE-PREMISE (for the completeness claim as scoped): structural integrity has no family, no WP, no addition form

**Claim.** The want doc's compile-time promise has two halves: "The proof engine's half of the promise governs data; the graph analyzer governs shape" (want :149). Three rows of the want's own must-not-compile table are structural (unreachable `Suspended`, `Frozen` dead end, dead row — want :125-127), and the inapplicability-vs-refusal principle (want :154) is part of the model. The matrix's obligation-family axis (:29) is "rule establishment, rule preservation, fault prevention" — the data half only. Structural obligations resist the triple: no write site, no WP, no premise class; the "discharge addition" for an unreachable state is an inbound *row* — a structural edit with no premise class or `ProofStrategy`. So the base→addition direction as *typed* (premise class + strategy columns) loses cells the want doc's deletion direction captured (this is the concrete answer to review question 4).

Yet the matrix's purpose statement claims the enumeration is "a completeness check on the want doc itself" (:8) and the exhaustiveness claim at :127 quantifies over "the want doc's model." Either the scope is the proof-engine half — then say so and narrow the completeness claim — or structural cells need a second case shape. Note also the **dead row is a genuine cross-cutter that a clean data/shape split leaks**: the want's dead-row example is a guard contradicting a *rule* (want :127, Q6 in the packet) — rule facts feed the satisfiability scan — so at least that structural family consumes the matrix's premise vocabulary even though it mints no WP.

**Amendment (smallest).** Add one sentence of scope declaration ("this matrix covers the data half; the structural half gets a sibling enumeration keyed to `ProofForwardingFact` kinds, `proof-engine.md:214-240`") and narrow :127 accordingly; give dead rows an explicit home in whichever enumeration wins, flagged ⧖ Q6. Structural cells still fit a *weaker* pair (flagged base → required rejection; fixing edit → clean), just without the premise/strategy columns — a second case shape, honestly typed, preserves the no-silent-cells property.

### F-3 — WEAKENS: "the write" (singular) presupposes an answer to open Q1, at the axis level where the open-disposition vocabulary cannot represent it

**Claim.** The obligation is defined as "the weakest precondition of the rule through **the write**" (:15), and Base A says "minted at the write site" (:58). Decision-packet Q1 (per-plan vs per-write, confirmed ambiguous in the want itself: "every handler preserves" at want :19 vs "every write site" at want :20) is open. For multi-write handlers — including the want's own `OpenAccount`, which writes both fields a relational rule mentions (want :49-51) — the two readings mint different obligations: WP through the composed plan vs per-write obligations on intermediates. The matrix's disposition vocabulary (defined/deferred/open) applies to *cells*; Q1 is openness in the *coordinate system* — a per-plan ruling changes what a "case" is, not what one cell contains. The worked slice, being all single-write, cannot surface this. Related and also invisible at single-write scale: the §1a "guard fact = WP verbatim" derivation (:66, :107) is only sound when no earlier write in the plan has invalidated the guard's pre-state reading — guard-fact transport through prior writes is a real derivation step the matrix's derivation column has no name for.

**Amendment (smallest).** Define the obligation over the row's **write plan** (a possibly-empty sequence), state that Q1 decides whether plan-cells decompose into per-write cells, and add plan shape (single-write / multi-write) as an explicitly Q1-gated axis parameter. This converts a silent presupposition into a visible structure-level open item.

### F-4 — WEAKENS: the premise-availability derivation from the write-structure axis is unsound as stated

**Claim.** The matrix's pruning rests on derivations like "overwrite excludes premise (d) structurally" (:125), instantiated at :60: "Overwrite severs the pre-state: premise (d) is unavailable by construction." False in general. Counterexample inside the want's own file shape: `set Total = Cap` under `rule Total <= Cap` is an overwrite, yet premise (d) (pre-state rules about `Cap`, which the write does not touch) is exactly what closes it. What determines (d)'s availability is not overwrite-vs-RMW but the **RHS read-set intersected with the rule's mention set** — overwrite/RMW is the special case where the only interesting read is the written field itself. Since "premise availability … derived from them" is the matrix's stated justification for pruning the product, an unsound derivation rule silently prunes non-empty cells — the exact failure mode the matrix exists to prevent.

**Amendment (smallest).** Redefine the write-structure axis by read-set relation: RHS reads {nothing / args only / the written field / other rule-mentioned fields / rule-unmentioned fields}, and restate the (d)-availability rule in those terms. The three worked bases survive unchanged as three of these classes.

### F-5 — WEAKENS: the obligation-minting (attachment) function is outside the matrix but determines which cells exist — and it is unsettled

**Claim.** The matrix defines discharge *given* an obligation, but which (rule, site) pairs mint obligations is prior to the matrix: the "mentions" definition (must include guard positions, desugared cross-field modifiers, derived-field dependents — packet S4, confirmed), ensure-activation sites (a no-write `Freeze` row entering a state with an `in Frozen ensure` — packet S3/Q5, confirmed leak under write-site-only attachment), and computed-field transitivity. "No silent cells" is only as strong as the generative rule for cell candidates; with attachment unstated, a whole obligation *category* (mid-lifecycle establishment at state entry) is absent from the family axis — the axis lists establishment and preservation (:29), and establishment is implicitly construction-only (the matrix's :48 treats "every initial event" as the establishment site set). Relatedly, the family axis's cited authority is wrong: :29 says "Canonical; catalog-enumerated (`ProofRequirement` records)," but the ProofRequirement DU at `proof-engine.md:529-543` enumerates numeric/presence/dimension/modifier/qualifier/containment kinds — it contains **no** rule-establishment or rule-preservation subtype (packet-confirmed: zero rule obligations minted at HEAD). The three families are want-derived, not catalog-canonical; citing them as catalog-enumerated is the exact assert-from-the-wrong-authority pattern this project polices.

**Amendment (smallest).** Add a "minting rule" section as an explicit input alongside the axes: the mention-set definition (⧖ S4 vocabulary), the activation-site rule (⧖ Q5), computed-field transitivity — each cited as open where open. Fix the :29 citation to "want-derived; catalog entries to be added" and either extend establishment to activation sites under Q5 or mark the family axis ⧖ Q5.

### F-6 — WEAKENS (minor on wording, real on scope): the WP clause is rule-family-specific; fault obligations are eval-site conditions, not WP-through-a-write

**Claim.** For the fault family the obligation is a safety precondition at an *evaluation site*, which need not sit in any write: the want's own `PlanRepayment` **reject row** interpolates a division (`{-Balance / PlanRepayment.Months}`, want :72) — a fault site in a no-write row — and guards can divide too. "Weakest precondition of the rule through the write" (:15) has neither a rule nor a write there. The axes also degrade for this family: "rule structure" is inapplicable; the relevant secondary axis is operation kind, which the ProofRequirement catalog already enumerates (`proof-engine.md:521-525`). So the four axes are not one orthogonal product; they are a family-indexed schema — fittingly, a DU of axis sets, per this project's own design idiom.

**Amendment (smallest).** Generalize the obligation clause: "the site's proof condition — WP of the constraint through the write plan for rule families; the catalog-declared safety precondition at the evaluation site for the fault family" — and state per-family which secondary axes apply.

### F-7 — MINOR: the proof-class taxonomy cannot classify derivations the matrix's own axes generate

**Claim.** The derivation vocabulary (:31) is pure-interval / §1a / §1b — an arithmetic taxonomy (verified against `proof-engine-mvp-and-proof-phases-2026-07-12.md`, whose §1a/§1b boundary is about counts of *stated relational facts*). The rule-structure axis includes "conditional," whose distinctive discharges — vacuous preservation by post-state deactivation of the rule's activation condition (want :40's `when MonthlyRepayment is set`), and frame preservation — are not arithmetic derivations at all and have no name in the taxonomy. "Proof class … derived from the axes" (:125) fails exactly where the axes leave the worked slice.

**Amendment (smallest).** Derive the derivation-class column from the designed certificate step kinds (21 step kinds, packet §(a)2 — already the certificate's vocabulary) instead of the three-class working-doc taxonomy; the triple≅certificate isomorphism (S-1) then holds in the derivation column too, not just in shape.

### F-8 — MINOR: "no-write" conflates three semantically different rows, and the establishment pre-configuration is unstated

**Claim.** The no-write axis value covers reject rows (no obligation; the inapplicability-vs-refusal semantics, want :154), transition-only rows (ensure-activation obligations under Q5), and unmatched inapplicability (not a row at all) — different definitional content sharing one cell coordinate. Separately, establishment WP composes over *what* pre-configuration? The want implies defaults for fields the initial event does not write (want :18, :106), but the matrix never says it, and its worked slice sidesteps by declaring establishment vacuous (:48).

**Amendment.** One sentence each: enumerate the no-write sub-kinds; state "establishment WP is taken over the default configuration."

### F-9 — MINOR: the addition direction loses the want's consumed/unconsumed-premise asymmetry

**Claim.** The want's deliberate omission (want :129 — deleting `DailyWithdrawalLimit`'s modifiers breaks no proof; "deleting a consumed premise makes a file unprovable; deleting a rule nobody consumes merely makes it a weaker spec") is a *global* property of premise consumption that the certificate must reflect (load-bearing marking, want :210). Cell-local base+addition pairs, where every addition is by construction consumed, cannot express it. The matrix's claim that addition framing "is stronger as a definition" (:25) is true per-cell and overclaims globally.

**Amendment.** A derived per-file property ("a premise appearing in no cell's premise list is unconsumed: enforced at its ingress, load-bearing for nothing"), noted once — not a cell.

---

## Verdict

**The triple + pair-of-behaviors core is a sound definitional decomposition for the family it grew up in — rule obligations at authored single-write handler sites — and its certificate isomorphism and no-silent-cells discipline are the right mechanisms.** But the four-axis enumeration, as drafted, **cannot support the exhaustiveness claim at :127**: it is missing the write-site-category axis entirely (no cell for the want's second ingress point — F-1), excludes the structural half of the want's promise while claiming to check the whole want doc (F-2), bakes in an answer to open Q1 at the coordinate level where its own open-disposition vocabulary can't see it (F-3), and rests its pruning on a premise-availability derivation that is false in general (F-4), with the obligation-minting function that generates cell candidates left outside the structure and unsettled (F-5).

None of these breaks the *case shape*; all of them break the *enumeration's* claim to exhaustiveness. The structure is salvageable with bounded amendments, all identified above: split write-site category from RHS read-set shape (F-1, F-4), declare the data/shape scope split and give structural cells a second, honestly-typed case shape (F-2), define obligations over write plans with Q1 as a visible structure-level gate (F-3), add the minting rule as an explicit ⧖-gated input and fix the family-axis citation (F-5), generalize the obligation clause per family (F-6), and swap the derivation taxonomy for the certificate step kinds (F-7). With those amendments the matrix is what it claims to be — a completeness check on the want doc. Without F-1/F-2/F-5 in particular, it is a well-built enumeration of roughly one column of the surface, whose silences are structural and therefore exactly the kind it promises not to have.