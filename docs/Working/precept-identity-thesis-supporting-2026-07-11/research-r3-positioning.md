# Domain Integrity Engine — Competitive/Category Landscape Brief

Neutral research brief. Repo citations verified by direct read (path:line). External claims are attributed to product/vendor documentation and general market knowledge as of my training (through Jan 2026); I did not run live web searches for this pass — the categories below (Bean Validation, Eiffel, Code Contracts, Drools/BRMS, Spring State Machine) are mature/stable ecosystems where my trained knowledge is unlikely to be stale, and I flag every external claim as such rather than repo-grounded.

**Important framing note before the map**: `research/product/entity-governance-landscape.md:5-7` states this memo was "Requested by Shane" for Precept's "entity-first/data-centric repositioning" — it is company-commissioned positioning research, not neutral third-party market analysis. Its "structural gap / empty cell" conclusion (`research/product/entity-governance-landscape.md:416-430`) is an argument produced to support a repositioning thesis. I treat it as an input to reason about, not as settled fact — per the agent-artifacts-are-not-authority discipline.

---

## 1. Category map

### 1.1 Pure input validators — FluentValidation, Zod/Valibot, JSON Schema, Bean Validation (Jakarta/JSR 380)

**Guarantee offered:** Data conforms to a declared shape/rule set *at the moment the validator is invoked*. Nothing more.

**What buyers/adopters actually pay for:** Expressive, composable rule vocabulary (regex, cross-property, custom predicates); framework integration (ASP.NET model binding, Hibernate Validator container integration); IDE/type tooling (Zod's `z.infer`); portability (JSON Schema across every language and OpenAPI); standardization (Bean Validation is a JCP/Jakarta spec — buyers value that any compliant implementation, any app server, honors the same annotations).

**Cross-cutting shortfall (entity+lifecycle+constraint in one contract):** All are "invoked, not structural" per `research/language/references/governance-vs-validation.md:21-26` — a validator is a separate object/function; nothing prevents `entity.Amount = -1` from existing unchecked (`governance-vs-validation.md:64, 96`). None has a lifecycle/state concept (`governance-vs-validation.md:97`). Conditional-by-state validation is bolted on ad hoc: FluentValidation's `When()` (`entity-governance-landscape.md:192,196`), or Bean Validation's **validation groups** (`@NotNull(groups = OnCreate.class)`) — a real, cross-language precedent for lifecycle-conditional validation demand, but solved with a group-tagging convention rather than a declared state model (external knowledge, Jakarta Bean Validation spec — not in repo corpus).

### 1.2 Rules / decision engines & BRMS — Drools, NRules, DMN/Camunda

**Guarantee offered:** Given a fact set, apply declared rules (forward-chaining production rules or decision tables) and produce a classification/decision. Stateless per evaluation.

**What buyers actually pay for — beyond the technical rule vocabulary:** Two things the repo corpus does not foreground but are the dominant commercial BRMS value props: (1) **business-user rule authorship** — decision tables editable by non-developers without a code deploy; (2) **rule-change-without-redeploy and audit trail** — regulated industries (insurance, lending) buy BRMS specifically so rule versions can be swapped and the "which rule version approved this decision" question is answerable for compliance. `research/language/domain-map.md:342` confirms Drools is only lightly surveyed in-repo ("Referenced in conditional-invariant-survey but no dedicated study") and `research/language/references/conditional-invariant-survey.md:27-41` covers only its `when` rule-head syntax, not its BRMS deployment/authoring model.

**Cross-cutting shortfall:** No entity binding — rules fire over "facts" presented to the engine, not a persistent governed entity (`entity-governance-landscape.md:125-135` for DMN specifically). No mutation/persistence model. No lifecycle concept.

### 1.3 State-machine libraries — Stateless (.NET), XState (JS/TS), Spring State Machine (Java)

**Guarantee offered:** Only permitted transitions fire; guards gate transitions on ambient conditions.

**What buyers pay for:** Lightweight embeddability (Stateless), hierarchical/parallel statecharts + visual tooling via Stately Studio (XState) (`entity-governance-landscape.md:355-358`), Spring ecosystem integration — persistence (`StateMachinePersist`), Spring Security/Batch interop (external knowledge, Spring docs — not in repo corpus).

**Cross-cutting shortfall:** No declared field/data model — XState's `context` is the closest analog but nothing prevents an `assign` action from writing an invalid value (`entity-governance-landscape.md:346-352`); Stateless has no field schema at all (`entity-governance-landscape.md:320,325`). Guards are arbitrary lambdas/JS functions, not declared, provable constraints (`entity-governance-landscape.md:319`). None offers post-mutation constraint rollback (`entity-governance-landscape.md:350`).

### 1.4 Policy engines — OPA/Rego, Cedar

**Guarantee offered:** A decoupled, evaluated allow/deny (or arbitrary structured) decision over presented JSON/entity data at request time.

**What buyers pay for:** Language-agnostic, infrastructure-scope policy (K8s admission, API gateway authorization) (`entity-governance-landscape.md:154-157`); Cedar's formally-verifiable policy language — provably non-conflicting policies, a genuine capability neither Precept nor the other categories here claim (`entity-governance-landscape.md:176-178`); and — relevant to the niche question below — **hot policy reload without redeploy** (OPA bundle distribution), the same buyer value BRMS sells.

**Cross-cutting shortfall:** No entity/lifecycle concept — Cedar's "entity" means authorization principal/resource, a different meaning of the word entirely (`entity-governance-landscape.md:174,180`). Purely evaluative — no mutation (`entity-governance-landscape.md:151`). Enforcement is decoupled: the calling system must honor the decision — a real bypass surface (`entity-governance-landscape.md:145,152`).

### 1.5 Design-by-contract — Eiffel, .NET Code Contracts

**Guarantee offered:** Preconditions, postconditions, and **class invariants** checked structurally on every routine call, inherited through subtyping. This is the historically closest precedent to "structural, non-bypassable, entity-bound enforcement" — closer, arguably, than the DB CHECK-constraint comparison the repo corpus foregrounds. `docs/compiler-and-runtime-design.md:795` cites Eiffel only for its three-way violation taxonomy (precondition/postcondition/invariant), not for the entity-binding/inheritance angle. `research/language/references/governance-vs-validation.md:100-111` calls CHECK constraints "closest existing system to structural governance" but CHECK constraints have **no entity concept** at all (`governance-vs-validation.md:107`) — an Eiffel class invariant is bound to the object exactly the way a Precept invariant is bound to the entity. This is a research-corpus gap worth naming plainly: the closest historical analog to Precept's core claim is under-cited relative to a weaker one.

**What buyers actually got (market outcome, not a guarantee):** Very little, commercially. Eiffel itself never reached mainstream adoption outside niche/academic use. Microsoft's own DbC library for .NET, **Code Contracts, was effectively discontinued** — no VS tooling support beyond the .NET Framework era, no .NET Core successor gained traction (external knowledge; not in repo corpus). This is a load-bearing market data point for a .NET-embedded structural-contract tool specifically: the same jurisdiction has tried "structural, entity-bound contracts" before and it did not survive. That doesn't mean Precept repeats the outcome, but the "no one has tried this in .NET" framing implicit in the repo memo's uniqueness claim is not accurate at the *concept* level — only at the *lifecycle-integrated DSL* level.

### 1.6 Low-code/enterprise integrity platforms — Salesforce, ServiceNow, Guidewire

**Guarantee offered:** The fullest combination in the landscape — declared fields, validation rules, approval/state chains, all in one governed record model. `entity-governance-landscape.md:17-88` covers all three in depth with concrete mechanism inventories (Salesforce validation-rule formulas + record types + approval processes; ServiceNow business rules + data policies + State field; Guidewire's Gosu-based entity/state model).

**What buyers actually pay for:** Overwhelmingly the *platform* — persistence, UI, multi-tenancy, integration ecosystem (AppExchange, ITSM tooling, insurance-domain APIs) — not the entity-governance primitives in isolation. The memo's own "What X does that Precept doesn't" sections concede this for all three (`entity-governance-landscape.md:36-38, 59-61, 82-84`). This matters for the analogy's rhetorical use: **"Salesforce-grade governance, NuGet weight"** (`entity-governance-landscape.md:502`) demonstrates the *pattern* exists at platform scale, but no evidence in the corpus shows a market that pays for entity-governance primitives **unbundled** from the platform. That's an open question, not a settled analog.

### 1.7 Industry data standards — FHIR, ACORD, ISO 20022

**Guarantee offered:** None, structurally — these are vocabularies/schemas (`entity-governance-landscape.md:250-307`). FHIR's own `status` fields are explicitly unconstrained coded values with no enforced transition validity (`entity-governance-landscape.md:258,261`).

**What buyers pay for:** Interoperability and standardized semantics across an entire industry ecosystem — a network-effect good no embeddable library can replicate. `entity-first-positioning-evidence.md:198` self-rates this comparison's evidence as only "Moderate... fewer concrete benchmarks" — the weakest-evidenced leg of the whole positioning table.

---

## 2. Synthesis: category map

| Lane | Representative tools | Guarantee axis they own | What's structurally absent |
|---|---|---|---|
| Validates data, ignores lifecycle | FluentValidation, Zod, JSON Schema, Bean Validation | Data-shape checking, portable/standardized vocab | State awareness; structural (non-bypassable) enforcement |
| Manages lifecycle, ignores data | Stateless, XState, Spring State Machine | Transition legality | Declared field schema; data-constraint enforcement |
| Classifies/decides over facts, no persistence | Drools/BRMS, DMN, OPA, Cedar | Rule/policy evaluation; **business-user authorship + redeploy-free rule changes** | Entity binding; mutation; lifecycle |
| Structural, entity-bound contracts (historical) | Eiffel, .NET Code Contracts | Non-bypassable, inherited, entity-bound enforcement | Lifecycle/state model; **commercial traction in the .NET ecosystem specifically** |
| Full governance, requires platform | Salesforce, ServiceNow, Guidewire | All axes combined | Embeddability; buyers pay for the platform, not the primitive |
| Vocabulary, no enforcement | FHIR, ACORD, ISO 20022 | Domain-standard semantics | Any enforcement mechanism at all |

This is consistent with `entity-governance-landscape.md:420-430`'s five-lane table, with two additions the repo doesn't carry: the DbC lane (1.5) and the BRMS-specific "non-developer authorship + hot rule-swap" buyer value (1.2/1.4), both of which bear directly on whether the "empty cell" is actually empty.

---

## 3. Where a defensible niche could plausibly exist

- **The bypass-architecture distinction is real and cross-cutting.** Every validation-lane tool shares the same four failure modes catalogued in `research/language/references/governance-vs-validation.md:131-183` (Bypass, Timing Gap, Scattered Rules, Silent Mutation) — this is a coherent, well-evidenced technical argument independent of positioning language, and it's the strongest leg of the whole case.
- **Lifecycle-conditional data rules are a documented, recurring pain point across ecosystems**, not just anecdote: FluentValidation's `When()`, Bean Validation's validation groups, and XState's unconstrained `assign` are three independent, cross-language instances of teams hand-rolling the same missing primitive. That convergent evidence is stronger than any single-tool comparison.
- **The enterprise-platform pattern (§1.6) demonstrates the combination is valuable at platform scale** — it doesn't yet demonstrate a market pays for it unbundled.

## 4. Where the "gap" claim is weakest

1. **DbC precedent undercuts "no one has tried structural, entity-bound enforcement."** Eiffel did, decades ago; .NET itself tried it (Code Contracts) and discontinued it. The open question this raises, neutrally: was the failure mode "the idea is wrong" or "the ergonomics/tooling/performance story was wrong"? The repo corpus doesn't engage this question at all — it cites Eiffel once, for an unrelated taxonomy point (`docs/compiler-and-runtime-design.md:795`).
2. **Informal convergence already exists** in adjacent ecosystems (e.g., state-machine-plus-validator glue patterns common in Rails/Django/Node stacks — external knowledge, not repo-cited) that reduce, though don't eliminate, the "nobody has combined these" novelty; the bypass-architecture distinction (glue is bypassable, Precept's claim is not) is the part of the argument that survives this objection — the "nobody combines them at all" framing is the part that doesn't.
3. **The BRMS/OPA buyer-value axis (business-user authorship, redeploy-free rule changes) is not addressed** by a compiled `.precept` file checked into source control. CLAUDE.md's own core-principles list states "Primary author is the domain expert, not the developer" — worth flagging as a live tension for the owner to weigh, since a text-DSL compiled at build time is a different authoring/deployment model than what BRMS/OPA buyers are actually paying for when they cite "non-developer rule ownership" and "change without redeploy" as reasons to buy.
4. **The enterprise-platform analogy is rhetorically strong but evidentially thin on unbundling** (§1.6) — no cited evidence that a market pays for entity-governance-only, decoupled from persistence/UI/integration.
5. **The source memo is a company-commissioned positioning document**, not neutral market research (`entity-governance-landscape.md:5-7`) — its "structural gap / empty cell" conclusion should be read as the strongest form of the argument its author was asked to build, not as an external market finding.
6. **Category-name risk is unaddressed**: `entity-governance-landscape.md:482` itself flags "domain integrity" as a category-creation play requiring buyer education, and no cited evidence (search volume, analyst category, comparable category launches) grounds that education is achievable — the funnel evidence cited (FluentValidation 250M vs. Stateless 25M downloads, `entity-governance-landscape.md:434-441`) shows awareness of the *underlying pain*, not that the market will accept a *new engine category* versus an incremental feature bolt-on to an incumbent (e.g., FluentValidation or XState adding a state-conditional constraint feature) — that harder version of the "why not extend the incumbent" objection is not engaged anywhere in the corpus.

---

**Files read (full):**
- `/home/sfalik/source/repos/Precept/research/product/entity-governance-landscape.md`
- `/home/sfalik/source/repos/Precept/research/philosophy/entity-first-positioning-evidence.md`
- `/home/sfalik/source/repos/Precept/research/language/references/governance-vs-validation.md`

**Supplementary repo greps consulted:** `docs/compiler-and-runtime-design.md:795`, `research/language/references/conditional-invariant-survey.md:27-41`, `research/language/domain-map.md:60,342`, `research/language/domain-research-batches.md:132,194`.