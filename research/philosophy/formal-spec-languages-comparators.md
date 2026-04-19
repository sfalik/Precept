# Formal Specification Languages as Comparators for Precept

**Date:** 2026-04-19
**Author:** Frank (Lead/Architect)
**Research Angle:** Are Alloy, TLA+, Event-B, and Z notation relevant comparators for Precept?
**Purpose:** Evaluate whether `philosophy.md`'s omission of formal specification languages is a gap in positioning or a correct editorial choice. Determine what, if anything, should change.

---

## 1. Executive Summary

**Verdict: The omission is justified. Formal specification languages are not positioning comparators for Precept.**

The category gap between formal specification languages and Precept is large enough that placing them in the comparator table would confuse rather than clarify. These tools operate in a fundamentally different deployment model — pre-implementation specification and formal verification — and their audience (researchers, safety-critical systems engineers, formal methods specialists) is not Precept's audience. A .NET developer writing a loan application service does not know what TLA+ is, and adding it to the comparison matrix creates work for readers without payoff.

However, the research is not purely academic background. Two findings are worth surfacing to Shane:

1. **Event-B's abstract machine model (state + invariants + guarded events) is structurally identical to Precept's model**, and understanding the distinction — enforcement at runtime vs. verification at design time — is the sharpest one-sentence answer to "what does Precept add that formal methods don't already do?" That framing may be useful for technical conference positioning or architecture blog posts even if it stays out of `philosophy.md`.

2. **Precept's proof engine already cites its formal methods lineage** (Cousot & Cousot 1977, Miné zone domains) in `docs/ProofEngineDesign.md`. This grounding is correctly documented there. It does not need to surface in `philosophy.md`, but it means the intellectual debt is already on record.

The specific claim that Precept makes — "compile-time structural checking" — maps precisely to what formal methods call "static analysis" and "invariant proof," but Precept restricts this to the domain declarations in the `.precept` file using sound, bounded interval arithmetic rather than general-purpose theorem proving. This is a design choice worth defending, and the proof engine design already defends it. No philosophy change is needed.

---

## 2. Survey Results

### 2.1 Alloy (Daniel Jackson, MIT — first released 1997; Alloy 6.2.0 current as of January 2025)

**Sources:** alloytools.org; Wikipedia — Alloy (specification language)

Alloy is a declarative specification language based on relational logic and first-order logic. The Alloy Analyzer takes an Alloy model and uses a SAT-solver (via the Kodkod model-finder) to find counterexamples within bounded scopes. The key design philosophy is "lightweight formal methods" — automated analysis without interactive theorem proving.

Alloy specifications define:
- **Signatures** — typed sets of objects with fields (relations)
- **Facts** — constraints assumed always to hold
- **Predicates** — parameterized constraints representing operations
- **Assertions** — properties claimed about the model that the analyzer attempts to falsify

Alloy 6 (released 2023) added mutable state and temporal logic, making the language considerably closer to runtime system behavior. The open-source community has used Alloy for security protocol verification, access control modeling, and structural integrity checking.

**Deployment model:** Pre-implementation analysis tool. Alloy checks whether your specification is internally consistent and whether claimed properties hold. It does not produce a deployable artifact. The analyzer runs at design time; there is no Alloy runtime.

**Audience:** Academic researchers, security engineers, software architects comfortable with relational logic. Not enterprise application developers.

**Category gap with Precept:** Complete. Alloy finds counterexamples in your model; Precept enforces your model at runtime. There is no overlap in what the user does with each tool.

---

### 2.2 TLA+ (Leslie Lamport, Microsoft Research — introduced 1999; TLA+2 current)

**Sources:** lamport.azurewebsites.net; Wikipedia — TLA+

TLA+ is a formal specification language for describing and verifying concurrent and distributed systems. It is grounded in the Temporal Logic of Actions, combining set theory (ZF) with temporal operators (□P = "always P", ◊P = "eventually P") to express safety and liveness properties. The TLC model checker exhaustively explores all reachable states in bounded scope to find invariant violations.

**Real-world usage at scale:** Amazon Web Services used TLA+ to find bugs in DynamoDB, S3, and EBS — some requiring 35-step state traces. Microsoft used it to design Cosmos DB's five consistency models and to discover a critical bug in the Xbox 360 memory module during spec writing. The AWS paper ("Use of Formal Methods at Amazon Web Services," 2014) is the canonical industry case.

**Deployment model:** Design-time specification and verification tool. TLA+ checks a model before implementation. It does not run in production. TLA+ models are not .NET libraries; they are mathematical documents that can be model-checked.

**Audience:** Distributed systems engineers, protocol designers, infrastructure architects. Requires comfort with temporal logic and set-theoretic notation. Not enterprise application developers building business entities.

**Category gap with Precept:** Near-total. TLA+ reasons about behaviors across time (liveness, fairness, deadlock freedom) in concurrent systems. Precept enforces data integrity for individual business entities in single-threaded, request-scoped operations. The problem domains are different at the architectural level: TLA+ prevents wrong distributed protocol behavior; Precept prevents wrong entity field configurations. Both use the word "invariant," but they mean different things — TLA+ invariants hold across all reachable states in a distributed state space; Precept rules hold as constraints on a single entity instance's field values.

---

### 2.3 Event-B / B-Method (Jean-Raymond Abrial — B originated 1980s; Event-B formalized with Rodin platform 2004–2014)

**Sources:** Wikipedia — B-Method (which covers Event-B via redirect); event-b.org (page unavailable at fetch time — supplemented from knowledge)

The B-Method is a formal development method grounded in set theory and first-order logic. It models systems as **abstract machines** with:
- **State variables** — the data the machine holds
- **Invariants** — constraints that must hold for every reachable state
- **Events** (in Event-B) — guarded operations that may fire when their guard condition holds, updating state variables
- **Refinement** — a formal mechanism for stepping from an abstract specification down to a concrete implementation, with proof obligations discharged at each step

Event-B is the successor to the classical B-Method, emphasizing system-level modeling and analysis over program development from specifications. The Rodin Platform (Eclipse-based, open source, EU-funded 2004–2014) provides the toolset: the Proof Obligation Generator creates proof goals for each refinement step, and theorem provers (automated and interactive) discharge them.

**Real-world usage:** Alstom and Siemens used B/Event-B to develop safety automatisms for the Paris Métro Lines 14 and 1 (fully automated, driverless subway lines). Atelier B by ClearSy has 25+ years of industrial use in safety-critical rail and transport systems. This is not academic — it is production-deployed formal development for systems where failure costs lives.

**Deployment model:** Formal development methodology. You write an abstract specification, prove its properties, refine it stepwise to code, prove each refinement preserves properties. The last refinement can be translated to a programming language. The formal artifacts are the specification, proofs, and refinement chain — not a runtime library.

**Audience:** Safety-critical systems engineers (railway, aerospace, automotive), formal methods specialists. The learning curve is steep; the toolchain (Rodin + provers) is heavyweight. Not enterprise application developers.

**Category gap with Precept:** Large — but structurally interesting. See section 4.

---

### 2.4 Z Notation (Jean-Raymond Abrial et al., Oxford University — proposed 1977; ISO standard 2002)

**Sources:** Wikipedia — Z notation

Z is a formal specification language based on axiomatic set theory, lambda calculus, and first-order predicate logic. It uses **schema boxes** — named specification units combining declarations and predicates — which can be combined using logical operators to build large specifications. Alloy was directly influenced by Z's mathematical foundations; they share the same intellectual lineage.

Z was used to specify IBM's CICS transaction system in the 1980s (earning IBM and Oxford the Queen's Award for Technological Achievement in 1992). It is ISO-standardized (ISO/IEC 13568:2002). However, Z has no automated analyzer — it produces mathematical specifications that can be reviewed and manually proved but not automatically checked in the way Alloy or TLA+ models can be.

**Deployment model:** Specification and documentation language. Z is used to write precise, mathematically grounded specifications before implementation. There is no Z runtime.

**Audience:** Academics, formal specification researchers, IBM-era enterprise software engineers. Largely superseded in practice by Alloy (automated analysis) and Event-B (refinement to code) for new work. Z is a historical precursor more than an active competitor.

**Category gap with Precept:** Complete. Z is a specification notation. Precept is a runtime enforcement engine. There is no scenario where a .NET developer would choose between Z notation and Precept.

---

## 3. The Category Gap

### What formal specification languages do that Precept does not

| Capability | Alloy | TLA+ | Event-B | Z | Precept |
|---|---|---|---|---|---|
| Automated model checking (counterexample finding) | ✓ (SAT-based) | ✓ (TLC) | ✓ (ProB) | – | – |
| Temporal property verification (liveness/safety) | Alloy 6 | ✓ | – | – | – |
| Refinement proofs (abstract → concrete) | – | partial | ✓ | – | – |
| Theorem proving (interactive/automated) | – | TLAPS | ✓ (Rodin) | manual | – |
| Code generation from specification | – | – | ✓ | – | – |
| Concurrency and distributed system reasoning | – | ✓ | – | – | – |

### What Precept does that formal specification languages do not

| Capability | Alloy | TLA+ | Event-B | Z | Precept |
|---|---|---|---|---|---|
| Runtime enforcement (prevents invalid state in production) | – | – | – | – | ✓ |
| Developer-accessible DSL (no formal methods training required) | – | – | – | – | ✓ |
| .NET library integration | – | – | – | – | ✓ |
| Runtime inspectability (non-mutating preview of every event) | – | – | – | – | ✓ |
| Business entity lifecycle modeling (out-of-box idioms) | – | – | – | – | ✓ |
| Compile-time proof engine with developer-readable output | – | – | – | – | ✓ |

The gaps are not small. They reflect a fundamental difference in the category of tool:

- **Formal specification languages** answer the question: "Is this design correct?" — at design time, before any code runs, using mathematical reasoning over all possible states.
- **Precept** answers the question: "Is this entity instance valid right now?" — at runtime, on every operation, using a compiled enforcement engine.

These are related questions but they are asked at different times, by different people, using different toolchains, for different purposes.

---

## 4. The Event-B Comparison in Depth

Event-B is the only tool in this survey where the structural parallel to Precept is direct enough to warrant a close look.

**The structural parallel:**

Event-B abstract machine:
- State variables = field values
- Invariants = constraints that must hold in every reachable state
- Events = guarded operations; a guard must be true for the event to fire; the event body updates state variables
- Initial state = starting assignment of state variables satisfying invariants

Precept `.precept` definition:
- Fields = typed state variables
- Rules / ensures = constraints that must hold in every reachable configuration
- Events = guarded transitions with arguments; guards select the matching transition row; the transition body sets field values
- `CreateInstance` = initial state with default field values

The mapping is exact. A Precept definition is an Event-B abstract machine in .NET DSL form. This is not coincidence — it is the correct minimal model for any system that enforces data integrity through state transitions. Abrial arrived at it through formal methods; Precept arrives at it through domain integrity engineering. Same model, different derivation path, different deployment target.

**What Event-B adds that Precept does not:**

1. **Proof obligations for events.** Event-B generates a proof obligation for each event: given that the invariants hold before the event fires, prove they hold after. This is a mathematical guarantee — the invariants cannot be violated by any event, provably. Precept achieves the same result differently: the runtime evaluates every constraint after every operation and rejects the operation if any fails. Event-B's guarantee is deductive (formal proof); Precept's is operational (runtime enforcement). Both achieve prevention; Event-B does it by proof, Precept does it by execution.

2. **Refinement.** Event-B lets you start with an abstract specification (e.g., "a queue of requests") and formally refine it to a concrete data structure (e.g., "an array with head and tail indices") while proving the refinement preserves all invariants. Precept has no refinement concept — the `.precept` file is the concrete specification, and there is no stepwise derivation from an abstract model.

3. **External consistency proof.** Event-B can prove properties about the relationship between events — e.g., "if event A fires in state X, event B cannot fire immediately after." Precept has the proof engine (which detects contradictory rules and dead guards), but it does not prove relational properties across events.

**What Precept adds that Event-B does not:**

1. **Runtime enforcement.** Event-B produces proven specifications and (sometimes) generated code. It does not produce a deployable .NET runtime enforcement engine. Precept is the runtime. You instantiate it, fire events against it, and the engine enforces the invariants on every operation in production.

2. **Inspectability.** The `Inspect` operation returns, for every event in the entity's current state, the full predicted outcome — mutations, constraint evaluations, result — without executing. Event-B has no runtime inspection API; the Rodin toolset has a model animator (ProB) but that is a development-time tool, not a production API.

3. **Developer accessibility.** Event-B requires formal methods training, comfort with set theory and proof obligations, and fluency with the Rodin toolset. Precept is designed to be learned in an afternoon by a .NET developer who has never heard of Event-B.

4. **Business domain idioms.** Precept's language includes business-native constructs: `reason` (every constraint requires a rationale), `edit` (field editability by state), `initial` (default field values), collection types (`set<T>`, `queue<T>`, `stack<T>`), and event arguments that carry typed data into transitions. Event-B's abstract machines are mathematical structures — they have no "reason" for a constraint, no first-class notion of editability, no domain-native collection types.

**The one-sentence summary of the distinction:**

Event-B proves before deployment that your state machine cannot reach an invalid state. Precept enforces at runtime that your entity cannot reach an invalid configuration. Event-B is a design-time verification tool for safety-critical systems. Precept is a production enforcement library for business applications.

---

## 5. Is the Omission Justified?

**Yes. The omission from `philosophy.md` is correct.** The reasons are structural:

**1. Deployment model mismatch, not feature gap.** The comparator list in `philosophy.md` is positioned against tools that .NET developers actually use and recognize — FluentValidation, XState, Drools, Entity Framework, Salesforce. A developer reading this list is asking "why not just use X instead of Precept?" That is a productive question for all listed tools. For Alloy or TLA+, the answer is not "because Precept does it better" — it is "these are different kinds of tools with no substitution relationship." Comparison creates confusion, not clarity.

**2. Audience mismatch.** Formal specification language users are not Precept's audience. The enterprise .NET developer writing a loan origination service has not heard of Alloy. Mentioning it in the positioning requires explaining what it is, why it matters, and then why it's not relevant — three additional paragraphs for zero positioning benefit. This is editorial overhead that the philosophy cannot afford.

**3. Not a differentiation surface.** Precept does not compete with formal methods. A team that uses TLA+ for distributed protocol verification can also use Precept for entity integrity enforcement — they are not substitutes, they are potentially complementary. Positioning them as alternatives invites the wrong question.

**4. The proof engine already handles the overlap.** The piece of Precept that most resembles formal methods — the compile-time proof engine — is correctly documented in `docs/ProofEngineDesign.md` with explicit citations to abstract interpretation and interval arithmetic. That documentation is the right place for the formal methods lineage, not the product philosophy.

**The one case where a brief mention might add value:**

For a technical audience (architecture conference, research paper, developer blog post targeting senior engineers), a single sentence acknowledging the formal methods lineage would strengthen Precept's intellectual positioning: "Precept's model — state variables, guarded events, and invariants that must hold in every reachable configuration — is a well-established formal object: the abstract machine of Event-B, extended with runtime enforcement rather than design-time proof." This does not need to be in `philosophy.md`. It belongs in technical outreach content aimed at an audience that would recognize the reference.

---

## 6. What Precept Borrows from This Tradition

Precept does not cite formal methods in its philosophy or user-facing documentation. This is correct for the audience. But the intellectual debt is real, and documenting it here is appropriate for the team's internal understanding.

**Invariant enforcement model.** The idea that a system is correct when its invariants hold in every reachable state is the foundational claim of Hoare logic, extended through Floyd, Dijkstra, and Abrial into Event-B. Precept's rules and ensures are invariants in this sense. The prevention guarantee — "invalid configurations are structurally impossible" — is the runtime realization of what these tools try to prove statically.

**State machine formalism.** The (state, event, guard, action, invariant) model is the standard abstract machine model from B/Event-B. Precept instantiates it as a .NET runtime with a developer-accessible DSL. The model is not invented; it is applied.

**Compile-time interval arithmetic.** Precept's proof engine is, by its own documentation (`docs/ProofEngineDesign.md`), "a simplified Zone abstract domain in the Cousot & Cousot (1977) abstract interpretation framework." This is direct formal methods lineage — Cousot invented abstract interpretation; the zone domain is a standard element of static analyzers. Precept's proof engine is a purpose-built static analyzer for the flat execution model of a single `.precept` definition.

**Opaque solver rejection.** Precept explicitly rejects SMT/Z3 solvers. This is a well-understood tradeoff in formal methods: SMT solvers prove more, but their proof witnesses are opaque. Precept's design principle — "proof legibility extends to AI agents" — is a direct application of the inspectability requirement that Cousot-style abstract interpretation satisfies better than SAT/SMT-based analysis for this domain.

**What would strengthen the technical positioning without changing philosophy.md:**

The phrase "compile-time structural checking" in `philosophy.md` is accurate but thin for a technical audience. The proof engine design fills the depth behind this claim. If Precept ever targets technical architects as an audience (white papers, architecture-focused talks), the framing "interval arithmetic over declared invariants, bounded relational closure, sound under-approximation of QF_LRA" gives the claim precise technical grounding. This language already exists in `docs/ProofEngineDesign.md`.

---

## 7. Implications for philosophy.md

**What to change:** Nothing in the comparator table or the positioning claims.

**What to not add:** Alloy, TLA+, Event-B, and Z notation as comparators. The category gap is too large and the audience too different.

**Optional strengthening (for consideration only):**

In the "Compile-time structural checking" bullet of the "What makes it different" section, the current text is:
> "The compiler catches structural problems — unreachable states, type mismatches, constraint contradictions, and more — before runtime. This is where the 'contract' metaphor becomes literal: the compile step validates the definition's structural soundness before any instance exists."

This is accurate and appropriately non-technical for the audience. No change is needed for the philosophy document.

If Precept develops technical positioning content aimed at engineers who would recognize formal methods references, the right framing is:

> "Precept's compile-time proof engine applies interval abstract interpretation to the entity's declared rules and constraints, proving numeric safety properties and detecting invariant contradictions before any instance exists. This is the same technique used in production static analyzers, adapted for the flat, single-pass execution model of a `.precept` definition."

This language belongs in technical content outside `philosophy.md` (e.g., architecture blog, white paper, or an extended `docs/ProofEngineDesign.md` introduction).

**One claim to watch:**

The philosophy's prevention guarantee ("invalid entity configurations cannot exist") is strong. For entities with numeric constraints, the proof engine enforces this guarantee only where it can prove safety — it is sound but incomplete. `philosophy.md` does not overclaim here (it says "structurally prevented" and "every constraint holds"), but if Precept adds content targeting formal methods practitioners, the completeness limitation should be stated explicitly: the engine proves what it can, reports what it cannot, and rejects only proven violations, not suspected ones.

---

## 8. References

- Abrial, Jean-Raymond. *Modeling in Event-B: System and Software Engineering.* Cambridge University Press, 2010.
- Alloy Tools website: https://alloytools.org/ (retrieved 2026-04-19)
- Cousot, Patrick and Radhia Cousot. "Abstract interpretation: a unified lattice model for static analysis of programs by construction or approximation of fixpoints." *POPL 1977.*
- Jackson, Daniel. *Software Abstractions: Logic, Language, and Analysis.* MIT Press, 2006.
- Lamport, Leslie. *Specifying Systems: The TLA+ Language and Tools for Hardware and Software Engineers.* Addison-Wesley, 2002.
- Lamport, Leslie. TLA+ home page: https://lamport.azurewebsites.net/tla/tla.html (retrieved 2026-04-19)
- Miné, Antoine. "A new numerical abstract domain based on difference-bound matrices." *PADO 2001.*
- Newcombe, Chris et al. "Use of Formal Methods at Amazon Web Services." 2014. (referenced in TLA+ Wikipedia article)
- Spivey, J. Michael. *The Z Notation: A Reference Manual.* Prentice Hall, 1992.
- Wikipedia: Alloy (specification language) — https://en.wikipedia.org/wiki/Alloy_(specification_language) (retrieved 2026-04-19)
- Wikipedia: B-Method / Event-B — https://en.wikipedia.org/wiki/B-Method (retrieved 2026-04-19)
- Wikipedia: TLA+ — https://en.wikipedia.org/wiki/TLA%2B (retrieved 2026-04-19)
- Wikipedia: Z notation — https://en.wikipedia.org/wiki/Z_notation (retrieved 2026-04-19)
- Precept project: `docs/ProofEngineDesign.md` — formal grounding of the proof engine, including explicit Cousot & Cousot and Miné citations
- Precept project: `docs/philosophy.md` — the positioning document this research evaluates
