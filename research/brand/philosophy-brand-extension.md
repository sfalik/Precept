# Philosophy Rewrite — Brand Extension for Expanded Landscape

**Author:** J. Peterman, Brand/DevRel  
**Requested by:** Shane  
**Scope:** Extension to `philosophy-rewrite-brand-impact.md` based on the expanded analog landscape, sample rebalancing, and the possibility of stateless / data-only precepts

**Inputs read:**
- `design/brand/research/philosophy-rewrite-brand-impact.md`
- `design/brand/research/data-vs-state-philosophy.md`
- `design/brand/brand-decisions.md`

---

## Executive judgment

The new context strengthens the original conclusion, but it changes the emphasis again:

- Precept should still own **domain integrity engine**.
- It should now present itself as governing **business entities and records**, not only lifecycle-driven entities.
- Lifecycle remains a powerful differentiator, but it can no longer be treated as universal.
- The central promise is broader than workflow and stronger than validation:

> **Precept defines and enforces the contract of a business entity — what data may exist, what may change, and, when lifecycle matters, how that change is governed.**

That sentence is the strategic hinge. It keeps Precept out of the validator bucket without trapping it in the workflow bucket.

---

## 1. Positioning against the expanded landscape

The new analog set does not collapse Precept into an existing category. It gives the brand more translation layers for different audiences.

### A. Enterprise platform record models  
### Salesforce objects, ServiceNow tables, Guidewire entities, MDM

This is the most commercially useful comparison set.

These platforms are understood as places where an organization defines:

- what a record is,
- what fields it carries,
- what rules apply,
- what transitions or approvals are allowed,
- and what governance surrounds that record.

Precept's brand story relative to them should be:

> **Precept is that level of record governance, but as an embeddable .NET library and executable contract.**

That is the cleanest story. It says:

- not a hosted enterprise platform,
- not a low-code admin surface,
- not a database abstraction,
- but the same seriousness about governed records inside application code.

Brand-safe phrasing:

- **"record governance for .NET applications"**
- **"an executable entity contract"**
- **"the integrity layer for governed business records"**

Important nuance: we should use these comparisons as **translation devices**, not as the primary category name. Precept is not "a Salesforce for .NET" or "a ServiceNow-style modeler." Those comparisons help enterprise buyers understand the level of governance, but the owned category remains **domain integrity engine**.

### B. Policy engines  
### OPA, Cedar, DMN

These tools govern **decisions**. Precept governs **entities**.

That is the clean category distinction.

OPA, Cedar, and DMN usually answer questions like:

- may this subject perform this action?
- which policy outcome should be selected?
- which branch should a decision table take?

Precept answers a different class of question:

- may this business entity exist in this configuration?
- may this record be edited this way?
- what state/data combination is allowed after this event?
- which fields, invariants, edits, and transitions are valid for this entity?

So the brand distinction should be:

> **Policy engines govern authorization and decisions across systems. Precept governs the integrity of business entities inside the application model itself.**

Or more sharply:

> **OPA and Cedar decide. Precept constrains and governs the entity being decided about.**

That framing matters because otherwise "rules engine" gravity will pull Precept into an overloaded category it does not own.

### C. Industry vocabularies and standards  
### FHIR, ACORD, ISO 20022

These standards describe canonical domain shapes, vocabularies, and interchange meaning. They do not, by themselves, provide an executable integrity runtime for an application's entity behavior.

That opens a strong supporting position:

> **Precept can be the runtime contract that enforces what industry standards describe.**

This is especially promising for standards-heavy domains:

- healthcare records,
- insurance entities,
- financial message objects,
- master data domains,
- compliance-sensitive reference data.

But we must keep the claim disciplined. The brand should not imply:

- full conformance certification,
- automatic implementation of a standard,
- or complete semantic coverage of a large vocabulary.

The safe story is:

> **Standards define the language of the domain. Precept enforces the executable rules of your implementation of that domain.**

That is brand-strong and believable.

### D. Pure validators  
### JSON Schema, FluentValidation

This remains the most important defensive distinction.

Validators primarily answer:

- is this payload shaped correctly?
- do these fields satisfy these checks right now?

Precept answers the larger contract question:

- is this business entity allowed to exist this way?
- are these edits permitted?
- are these invariants maintained over time?
- if lifecycle exists, is this transition and resulting configuration allowed?

The brand-safe differentiator is not "better validation." It is:

> **Validation checks data. Precept governs the entity contract.**

Or:

> **Validators inspect inputs. Precept defines the permissible configurations and changes of a business entity.**

This distinction works for both stateful and stateless precepts. Even without states, Precept still claims:

- one contract,
- explicit invariants,
- governed edits,
- inspectable semantics,
- deterministic outcomes,
- and structural prevention rather than scattered checks.

That is larger than validation without pretending to replace every validator use case.

---

## 2. The stateless precept implication

If Issue #22 lands, the brand must stop speaking as if every entity has a lifecycle state machine.

That does **not** weaken the category. It broadens it.

### A. Impact on the flagship positioning sentence

My previous recommended line was:

> "Precept is a domain integrity engine for .NET — a single declarative contract for modelling business entities, governing how their data evolves under business rules across a lifecycle and making invalid configurations structurally impossible."

That line is still strong for stateful entities, but it assumes lifecycle is universal. With stateless precepts, it becomes too narrow.

### Recommended stateful + stateless variant

> **Precept is a domain integrity engine for .NET — a single declarative contract for modelling governed business entities, enforcing what data may exist and how it may change under business rules, and making invalid configurations structurally impossible.**

Why this works:

- **"governed business entities"** keeps the subject concrete.
- **"what data may exist"** covers stateless/reference/policy/config entities.
- **"how it may change"** covers lifecycle and mutation without requiring states.
- **"invalid configurations"** still covers state + data where states exist, and field combinations where they do not.

If we need a shorter form:

> **Precept is a domain integrity engine for .NET — a single declarative contract that governs a business entity's valid data and changes, making invalid configurations structurally impossible.**

### B. Impact on lifecycle language

Lifecycle should move from **universal identity language** to **important-but-conditional capability language**.

Meaning:

- Do not say every Precept contract governs an entity "across its lifecycle."
- Do say Precept governs an entity **through change**, and **through lifecycle when lifecycle exists**.
- Treat lifecycle as one major expression of entity governance, not the only one.

Recommended brand habit:

- Prefer **"entity contract," "configuration," "change," "governed edits," "rules," "invariants"**
- Use **"lifecycle"** when describing workflow-capable or stateful examples
- Avoid writing as though lifecycle is mandatory for credibility

### C. Risk of looking like "just a validator"

Yes, this risk increases if stateless precepts ship and the brand gets lazy.

The danger is not the feature. The danger is the language around the feature.

If we describe stateless precepts as:

- validation-only,
- schema-like,
- lightweight checks,
- or just "business rule validation,"

then we collapse into the validator bucket immediately.

The mitigation is to keep four ideas visible:

1. **contract** — not ad hoc checks
2. **entity** — not just payloads
3. **governed change / edits** — not just pass/fail inspection
4. **structural prevention** — not just post hoc rejection

Stateless precepts should therefore be positioned as:

> **full entity contracts without lifecycle states, not as a reduced validation mode**

That phrasing preserves category dignity.

---

## 3. Brand vocabulary expansion

Yes: the brand should widen its vocabulary, but only with clear hierarchy.

### Terms we should start using

#### 1. **Entity contract**

This is the strongest addition.

Why:

- it works for both stateful and stateless precepts,
- it is legible to DDD-minded developers,
- it avoids ORM connotations better than "entity model" alone,
- and it naturally supports the "one file, every rule" story.

Recommended role: major supporting phrase, just below **domain integrity engine**.

#### 2. **Record model** / **governed record**

Useful in enterprise-facing copy, especially when translating for buyers familiar with Salesforce, ServiceNow, MDM, or Guidewire.

Recommended role: audience translation term, not primary identity.

Good usage:

- "a governed record model"
- "record-level integrity"
- "record governance in application code"

#### 3. **Domain rule engine**

Useful, but secondary.

It can help explain Precept to developers who need a bridge from validator/rules-engine mental models. But standing alone, it is too broad and too close to classic rule-engine baggage.

Recommended role: explanatory subphrase, not category.

#### 4. **Governed entity**

Very strong supporting phrase.

It suggests:

- rules,
- accountability,
- edit control,
- lifecycle where needed,
- and more seriousness than a DTO or POCO.

### Terms we should use carefully

#### **Policy-as-code**

Useful only in comparison. Do not let this become identity language.

Why: it drags Precept toward authorization and cross-system decisioning, where OPA/Cedar already have established meaning.

Safe use:

- "more like an entity contract than policy-as-code"
- "governs entities, not just policies"

#### **Schema**

Use cautiously and usually in contrast.

Why: schema implies shape, not executable business semantics. Precept is more than schema.

### Terms we should avoid as primary framing

- **workflow engine**
- **state machine library**
- **rules engine**
- **policy engine**
- **validator**
- **schema framework**
- **ORM companion**
- **low-code record platform**

Each of those terms captures one slice while misframing the whole.

### Recommended hierarchy

1. **Domain integrity engine** — owned category
2. **Executable entity contract** — strongest explanatory phrase
3. **Governed business entity / governed record** — audience translation
4. **Lifecycle / workflow / policy / validation** — mechanism or comparison layers

---

## 4. Updated risk assessment

The original four risks still stand. The expanded landscape changes their weight and adds several new ones.

### Original risks, updated

#### Risk 1: sounding like an ORM or Entity Framework companion

Still real, but now somewhat easier to manage.

Why it is partly mitigated:

- the enterprise-record analogs give us a richer comparison than plain data modelling,
- "governed record" is broader than "entity model,"
- and the standards/policy comparisons create more room for integrity language.

Mitigation still stands: never say "entity modelling" without pairing it with **contract, governance, integrity, rules, or invariants**.

#### Risk 2: sounding like a FluentValidation alternative

This remains the sharpest risk, and stateless precepts increase it.

What changed:

- the expanded market makes the validator comparison more natural,
- but it also gives us better defenses: governed records, standards enforcement, and edit semantics.

So this risk is simultaneously **more likely** and **more survivable** if the language is disciplined.

#### Risk 3: erasing state so aggressively that we undersell the mechanism

This becomes less severe as a global risk because the product may genuinely include stateless contracts.

But it remains a risk for stateful demos and flagship samples. We must not imply that lifecycle is decorative where it exists.

Updated mitigation:

- speak of **state as optional but first-class**
- speak of **lifecycle as a governing structure when present**
- never reduce stateful precepts to "just validation plus labels"

#### Risk 4: drifting back into workflow-engine language

Still present, but now materially mitigated by the sample portfolio rebalance.

A 60–70% workflow portfolio still means workflow remains the largest sample cluster, but the existence of entity and hybrid examples gives the brand more proof points to resist workflow capture.

### New risk 5: being mistaken for an enterprise platform clone

Because Salesforce/ServiceNow/Guidewire are now in the analog set, some readers may think Precept is:

- a low-code modeling environment,
- a replacement for those platforms,
- or a metadata-admin system rather than a developer library.

Mitigation:

> always pair those analogies with **embeddable .NET library**, **code-native**, or **application-level runtime contract**

### New risk 6: being swallowed by the policy-engine category

As soon as "rules" and "governance" expand, some audiences will hear:

- authorization,
- policy evaluation,
- or decision tables.

Mitigation:

> repeat the entity distinction relentlessly: **policy engines govern decisions; Precept governs the business entity itself**

### New risk 7: overclaiming industry-standard enforcement

FHIR, ACORD, and ISO 20022 are powerful analogs, but also dangerous. If the brand sounds like Precept automatically implements those standards, credibility drops.

Mitigation:

> say **enforces your implementation of the domain contract described by the standard**, not **implements the standard for you**

### Net effect of the expanded landscape

The landscape adds confusion risk, but it also materially improves positioning power.

It mitigates two older weaknesses:

1. **workflow over-association** — because Precept now has credible non-workflow analogs
2. **small-tool perception** — because standards, MDM, enterprise records, and policy comparisons all make the category feel more consequential

So overall, the change is positive if the language is tightly governed.

---

## 5. The "what Precept replaces" story

This story should now work for both lifecycle entities and pure data entities.

### Recommended 2-sentence version

> **Today, developers scatter domain rules across validators, DTO annotations, service methods, workflow handlers, policy files, and database constraints — so the entity's real contract exists nowhere as a whole. Precept replaces that fragmentation with one executable entity contract that defines what data may exist, what may change, and, when needed, how lifecycle transitions are governed.**

### Shorter 1-sentence version

> **Precept replaces scattered validation, transition logic, and rule enforcement with one executable contract for a governed business entity.**

That is the clean replacement story. It works whether the entity has:

- no states at all,
- a rich lifecycle,
- or a hybrid model where some rules are always-on and others are state-bound.

---

## Final recommendation

The expanded landscape does not ask Precept to change categories. It asks Precept to become more precise about the territory that category covers.

The updated hierarchy should be:

1. **Precept is a domain integrity engine.**
2. **Its object is the governed business entity or record.**
3. **Its form is an executable entity contract.**
4. **Its guarantee is structurally valid configurations and changes.**
5. **Lifecycle is a major capability, not a universal prerequisite.**

If we hold that line, Precept can speak credibly to workflow entities, reference data, policy/config records, and standards-heavy domain models without collapsing into either "just a validator" or "just a workflow tool."
