# Catalog System

---

## Status

| Property | Value |
|---|---|
| Doc maturity | Full |
| Implementation state | Implemented — all catalogs in `src/Precept/`; team review complete (2026-04-25) |
| Related | `docs/compiler/diagnostic-system.md` · `docs/runtime/fault-system.md` · `docs/compiler-and-runtime-design.md` |

> **Catalog inventory.** The catalogs describe what the language IS (Tokens, Types, Functions, Operators, Operations, Modifiers, Actions, Constructs, ExpressionForms, Constraints, ProofRequirements, Outcomes) and how it reports failures (Diagnostics, Faults). One is tooling-adjacent: **SemanticTokenTypes**, which carries visual classification metadata consumed by the TextMate grammar generator and the LSP semantic-tokens handler. The Slice-10 architectural decision (`docs/Working/Archive/language-server-implementation-plan.md:641, 645`) treats SemanticTokenTypes as a first-class catalog rather than a hardcoded TokenMeta → scope mapping. **This document is the canonical inventory; other docs reference catalogs by name, not by count.**

> [!IMPORTANT]
> **Non-Negotiable Rules — Read Before Implementing**
>
> - **Catalog the full language surface** — everything language-relevant belongs in a catalog: keywords, types, operators, actions, modifiers, constructs, constraints, proof requirements, diagnostics, and faults. See [§Vision: Metadata for the Entire Language](#vision-metadata-for-the-entire-language) and [§Completeness Principle](#completeness-principle).
> - **Keep pipeline stages generic** — parser, type checker, proof engine, evaluator, tooling, and docs consumers read catalog metadata; they do not encode language knowledge themselves. See [§Architectural Identity: Metadata-Driven](#architectural-identity-metadata-driven).
> - **Put per-member behavior in metadata** — if behavior varies by catalog member, that behavior belongs in the catalog entry's metadata rather than switch statements on enum identity. See [§Architectural Identity: Metadata-Driven](#architectural-identity-metadata-driven) and [§Pattern Definition](#pattern-definition).
> - **Match metadata shape to member shape** — use a discriminated union when members need different metadata shapes, use a flat sealed record when they do not, and never paper over shape differences with nullable fields. See [§Pattern Definition](#pattern-definition) and [§Proof Obligations](#proof-obligations).
> - **Derive from catalogs, never mirror them** — do not maintain parallel token sets, keyword lists, or other copies of knowledge the catalogs already own. See [§Architectural Identity: Metadata-Driven](#architectural-identity-metadata-driven), [§Cross-Catalog Derivation](#cross-catalog-derivation), and [§Pipeline Stage Impact](#pipeline-stage-impact).

## Contents

- [Overview](#overview)
- [Vision: Metadata for the Entire Language](#vision-metadata-for-the-entire-language)
- [Completeness Principle](#completeness-principle)
  - [Enums that remain bare](#enums-that-remain-bare)
- [Architectural Identity: Metadata-Driven](#architectural-identity-metadata-driven)
  - [Vision precedes consumers](#vision-precedes-consumers)
  - [The decision framework](#the-decision-framework)
  - [Enforcement](#enforcement)
  - [Derive, never duplicate](#derive-never-duplicate)
- [Catalog Schema](#catalog-schema)
  - [Level 1 — Catalog Overview Map](#level-1--catalog-overview-map)
  - [Level 2 — Schema Anatomy](#level-2--schema-anatomy)
  - [Level 3 — Member Inventories](#level-3--member-inventories)
- [Pattern Definition](#pattern-definition)
  - [1. Kind enum — the closed set](#1-kind-enum--the-closed-set)
  - [2. Meta record — what consumers need to know](#2-meta-record--what-consumers-need-to-know)
  - [Meta record shape: flat record vs discriminated union](#meta-record-shape-flat-record-vs-discriminated-union)
  - [3. Static catalog class — the exhaustive switch](#3-static-catalog-class--the-exhaustive-switch)
  - [4. Output value type — what the pipeline produces](#4-output-value-type--what-the-pipeline-produces)
- [Why Exhaustive Switch, Not Attributes + Reflection](#why-exhaustive-switch-not-attributes--reflection)
- [Roslyn Enforcement Layer](#roslyn-enforcement-layer)
  - [Implemented Rules](#implemented-rules)
  - [Two-Layer Enforcement Model](#two-layer-enforcement-model)
  - [Future Rules](#future-rules)
- [Exhaustiveness Enforcement Strategies](#exhaustiveness-enforcement-strategies)
  - [Strategy 1: CS8509 — Compiler-Enforced Exhaustive Switch](#strategy-1-cs8509--compiler-enforced-exhaustive-switch)
  - [Strategy 2: `[HandlesCatalogExhaustively]` + `[HandlesCatalogMember]` — Analyzer-Enforced Distributed Dispatch](#strategy-2-handlescatalogexhaustively--handlescatalogmember--analyzer-enforced-distributed-dispatch)
  - [Decision Rule for New Dispatchers](#decision-rule-for-new-dispatchers)
  - [Implementation Dispatchers](#implementation-dispatchers)
- [Naming Convention](#naming-convention)
- [Catalog Inventory](#catalog-inventory)
  - [Language Definition Catalogs](#language-definition-catalogs)
  - [Failure Mode Catalogs](#failure-mode-catalogs)
- [Supporting Types](#supporting-types)
  - [TypeTarget discriminated union](#typetarget-discriminated-union)
- [Qualifier Propagation](#qualifier-propagation)
  - [The split](#the-split)
  - [Qualifier propagation patterns](#qualifier-propagation-patterns)
  - [Five qualifier-bearing types](#five-qualifier-bearing-types)
  - [Unknown qualifier handling](#unknown-qualifier-handling)
- [Proof Obligations](#proof-obligations)
  - [ProofSubject discriminated union](#proofsubject-discriminated-union)
  - [ProofRequirement discriminated union](#proofrequirement-discriminated-union)
  - [Valid subjects and requirement types per catalog entry](#valid-subjects-and-requirement-types-per-catalog-entry)
  - [ParameterMeta — object-reference safety](#parametermeta--object-reference-safety)
  - [Complete proof obligation inventory](#complete-proof-obligation-inventory)
- [Construct Slot Model](#construct-slot-model)
- [Qualifier Registries (Not Catalogs)](#qualifier-registries-not-catalogs)
- [Syntax Reference](#syntax-reference)
- [Cross-Catalog Derivation](#cross-catalog-derivation)
- [Future Opportunities](#future-opportunities)
- [Test Strategy](#test-strategy)
  - [Non-negotiable rules](#non-negotiable-rules)
  - [Cross-catalog integrity tests](#cross-catalog-integrity-tests)
- [Pipeline Stage Impact](#pipeline-stage-impact)
  - [Parser-catalog integration pattern](#parser-catalog-integration-pattern)
  - [TypeChecker-catalog integration pattern](#typechecker-catalog-integration-pattern)
  - [GraphAnalyzer-catalog integration pattern](#graphanalyzer-catalog-integration-pattern)
  - [ProofEngine-catalog integration pattern](#proofengine-catalog-integration-pattern)
  - [Evaluator-catalog integration pattern](#evaluator-catalog-integration-pattern)
  - [PreceptBuilder-catalog integration pattern](#preceptbuilder-catalog-integration-pattern)
  - [LanguageServer-catalog integration pattern](#languageserver-catalog-integration-pattern)
- [Open Questions / Implementation Notes](#open-questions--implementation-notes)
  - [ValueModifierMeta.ProofSatisfactions](#valuemodifiermetaproofsatisfactions)
  - [ConstructMeta.ModelContribution (Candidate)](#constructmetamodelcontribution-candidate)
  - [FieldDescriptor.AccessModes](#fielddescriptoraccessmodes)
  - [Catalog documentation strings (HoverDescription) — CC#19](#catalog-documentation-strings-hoverdescription--cc19)
- [Cross-References](#cross-references)

## Overview

The catalog system is the **authoritative machine-readable definition of the Precept language.** The catalogs — describing what the language IS, how it reports failures, and (in the tooling-adjacent case) visual classification — form a closed, compiler-enforced registry. This document defines the catalog pattern, the canonical catalog inventory, their shapes, cross-catalog derivation relationships, and future opportunities.

## Vision: Metadata for the Entire Language

Every aspect of Precept — its keywords, types, functions, operators, operations, modifiers, actions, grammar forms, expression forms, constraints, proof requirements, outcome forms, diagnostics, faults, and visual classification — is defined as structured metadata in a static, compiler-enforced catalog. Fifteen catalogs cover the complete language surface (twelve language-definition + two failure-mode + one tooling-adjacent). Their union IS the language specification in machine-readable form.

Every consumer reads from these catalogs:

| Consumer | What it reads |
|----------|---------------|
| MCP `precept_language` | All keywords, types, operators, operations, functions, constraints, grammar forms, outcome forms |
| TextMate grammar | Token keyword alternations, type name alternations, construct slot patterns, `SemanticTokenTypeMeta.TextMateScope` |
| LS completions | Types, functions, modifiers, actions, outcome forms — context-dependent |
| LS hover | Type documentation, function signatures, operator descriptions, outcome descriptions |
| LS semantic tokens | `TokenMeta.VisualCategory` → `SemanticTokenTypeMeta.CustomType` |
| Type checker | Modifier applicability, function signatures, operation legality |
| Parser (outcome dispatch) | `Outcomes.ByLeadingToken`, `OutcomeMeta.ArgumentKind` |
| AI grounding | All catalogs — complete language knowledge |
| Reference docs | All 12 language definition catalogs |

No consumer maintains its own parallel copy. Adding a language feature to an enum is the single atomic act that propagates it to every surface. The compiler refuses to build if any member is missing metadata.

## Completeness Principle

> If something is part of the Precept language, it gets cataloged.

The test: **if I enumerated every catalog's `All` property, would I have a complete description of Precept?** The catalogs needed are those whose union covers the entire language surface.

Fifteen catalogs in three groups (12 language-definition + 2 failure-mode + 1 tooling-adjacent).

**Language Definition (what the language IS):**

| # | Catalog | What it covers |
|---|---------|----------------|
| 1 | **Tokens** | Lexical vocabulary |
| 2 | **Types** | Type system families |
| 3 | **Functions** | Built-in function library |
| 4 | **Operators** | Operator symbols — precedence, associativity, arity |
| 5 | **Operations** | Typed operator combinations — what (op, lhs, rhs) triples are legal |
| 6 | **Modifiers** | Declaration-attached modifiers — field constraints, state lifecycle, event modifiers, access modes, anchors (DU with 5 subtypes) |
| 7 | **Actions** | State-machine action verbs |
| 8 | **Constructs** | Grammar forms / declaration shapes |
| 9 | **ExpressionForms** | Expression grammar forms — expression node kinds (literal, identifier, binary op, function call, quantifier, CI function call, etc.) |
| 10 | **Constraints** | Constraint declaration forms — invariant, state-anchored, event precondition (DU as identity) |
| 11 | **ProofRequirements** | Proof obligation kinds — 10 members (numeric, presence, dimension, modifier, qualifier compatibility, qualifier chain, interval / length / count containment, key presence) — DU as identity; see § 11 ProofRequirements for the full inventory |
| 12 | **Outcomes** | Transition-row outcome forms — transition, no transition, reject (closed 3-member vocabulary) |

**Failure Modes (how it tells you what's wrong):**

| # | Catalog | What it covers |
|---|---------|----------------|
| 13 | **Diagnostics** | Compile-time rules |
| 14 | **Faults** | Runtime failure modes |

**Tooling-Adjacent (visual classification):**

| # | Catalog | What it covers |
|---|---------|----------------|
| 15 | **SemanticTokenTypes** | LSP semantic-token custom types and TextMate scopes for visual classification — the single-axis bridge `TokenMeta.VisualCategory → SemanticTokenTypeMeta` that powers both the TextMate grammar generator and the LSP semantic-tokens handler |

If a further aspect of the language emerges that isn't covered, it needs a catalog. The system is complete when the catalogs are.

### Enums that remain bare

Not every enum becomes a catalog. Supporting enums stay bare:

| Enum | Why bare |
|------|----------|
| `DiagnosticStage` | 5 values, classification axis for diagnostics, no per-member metadata |
| `Severity` | 3 values, no per-member metadata |
| `TokenCategory` | ~17 values. Grouping key — consumers iterate tokens grouped by category, not categories themselves |

These are internal classification axes. They organize catalog members or AST nodes but don't independently describe the language surface.

**Previously bare, now absorbed by the Modifiers DU:** `StateModifierKind`, `AccessMode`, `EnsureAnchor`, `StateActionAnchor` — these are first-class language surface members as `StateModifierMeta`, `AccessModifierMeta`, and `AnchorModifierMeta` subtypes in `Modifiers.All`.

## Architectural Identity: Metadata-Driven

Precept's compiler and runtime follow a **metadata-driven architecture.** Domain knowledge is declared as structured metadata in catalogs. Pipeline stages are generic machinery that reads it.

This inverts the traditional compiler model:

| | Traditional (Roslyn, GCC, TypeScript) | Precept |
|---|---|---|
| Where domain knowledge lives | Scattered across pipeline stage implementations | Declared in metadata catalogs |
| What a pipeline stage is | A domain-expert that knows the language | Generic machinery that reads metadata |
| How you add a language feature | Touch dozens of files across the pipeline | Add an enum member, fill the exhaustive switch — propagation is automatic |
| What the compiler refuses to build | Code that doesn't compile | Code with incomplete metadata (CS8509 on every exhaustive switch) |
| What tests verify | Implementation behavior | Metadata completeness and correctness |
| What consumers read | Their own parallel copies | The single source of truth |

The catalogs are expressions of this principle — not the principle itself. The principle is: **if something is domain knowledge, it is metadata; if it is metadata, it has a declared shape; if shapes vary by kind, the shape is a discriminated union.** Pipeline stages, tooling, and consumers derive from the metadata — they never maintain parallel copies or encode domain knowledge in their own logic.

**External reference data is distinct from catalogs.** ISO 4217 and UCUM are authoritative third-party data sources that Precept validates against, but they are not themselves part of the Precept language specification. They ship as embedded XML resources with lazy loaders because the data belongs to the outside world. The test is: *is this part of a complete description of Precept?* `TypeMeta` for `currency` is Precept. The 159 currently admitted currency codes are not.

### Vision precedes consumers

The catalog-driven architecture means: **describe the language completely in metadata, then consumers derive from it.** The vision precedes the consumers — not the other way around.

This ordering is non-negotiable. The catalog is not built bottom-up from consumer demand. It is built top-down from language reality. If something IS the language — if it appears in `.precept` files, if it carries semantics, if it would appear in a complete description of Precept — it gets cataloged. Period. Whether any consumer currently reads that metadata is irrelevant to the cataloging decision.

**"No consumer currently uses it" is never a valid argument against cataloging something that IS the language.** This argument inverts the architectural vision. It says: "build the spec from what consumers happen to need today." That is the traditional compiler model — domain knowledge scattered across implementations, maintained as parallel copies, accruing drift. It is exactly what the catalog system exists to prevent.

The correct reasoning is:

1. **Is it language surface?** → Catalog it.
2. **Do consumers exist that read it?** → Irrelevant to step 1. Consumers will derive from it when they need it. The catalog is ready before the consumer arrives.
3. **Does cataloging it have "value" today?** → Yes. Completeness IS the value. A machine-readable spec with gaps is not a spec — it is a collection of implementation notes.

Future agents, reviewers, and architects: if you find yourself arguing "but nothing reads this yet" as grounds to reject a catalog entry, you are repeating the mistake this principle exists to prevent. Stop. Re-read this section. The catalog describes the language. The language is not defined by what consumers happen to query.

### The decision framework

When evaluating whether something belongs in a catalog:

1. **Is it language surface?** Does it appear in `.precept` files, carry semantics that consumers need, or represent a concept that would appear in a complete description of the Precept language?
   - No → bare enum, internal classification axis (e.g., `DiagnosticStage`, `Severity`)
   - Yes → it gets cataloged. Continue:
2. **Do all members share the same metadata shape?** → flat `sealed record`
3. **Do members have varying metadata by kind?** (e.g., field modifiers need `ApplicableTo` but state modifiers need `AllowsOutgoing`) → DU: `abstract record` base + `sealed` subtypes, each carrying exactly its consumers' metadata
4. **No per-member metadata beyond identity?** → still catalog, minimal record `(Kind, Keyword, Description)`

**Anti-pattern: "small enum = bare."** Size is irrelevant. `AccessMode` has 3 values but each has distinct behavioral semantics (`IsPresent`, `IsWritable`) that consumers need. The question is never "how many members?" — it's "do consumers hardcode per-member knowledge that should be metadata?"

**Anti-pattern: "flat record with inapplicable fields."** If a flat metadata record has fields that are meaningless for some members, that's a signal for a DU — not a signal to reject cataloging. The DU ensures each subtype carries exactly the fields its consumers need.

**Anti-pattern: "no consumer currently uses it."** The absence of a current consumer is not evidence against cataloging. It is evidence that the catalog is ahead of the consumers — which is the correct ordering. The catalog is the machine-readable language specification. A spec does not omit sections because no reader has asked about them yet. If the element is language surface, it is cataloged — consumer demand is not a prerequisite.

### Enforcement

The exhaustive switch is the enforcement — the C# compiler refuses to build if any member is missing metadata (CS8509). The `All` property is the enumeration surface — MCP and LS iterate it rather than maintaining their own lists. The `Create()` factory is the derivation path — pipeline stages produce output values from the catalog rather than constructing them ad hoc.

### Derive, never duplicate

The catalogs cover vocabulary, types, functions, operators, operations, modifiers, actions, grammar constructs, expression forms, constraints, proof requirements, outcome forms, compile-time rules, runtime failure modes, and visual classification. Their union is the language. Every downstream artifact — grammar, completions, hover, MCP output, documentation — derives from catalog metadata. No consumer maintains a parallel copy. Adding a language feature to an enum is the single atomic act that propagates it to every surface.

### Architectural Violation Patterns

Pipeline-stage code that violates the catalog-driven contract reliably falls into one of eight recurring shapes. Naming them gives reviewers a shared vocabulary for spotting drift and gives authors a checklist to consult before writing pipeline code that "looks fine but answers a language question locally."

| Pattern | Symptom | Catalog remediation |
|---|---|---|
| **A — Hardcoded token allowlist** | A method whitelists specific `TokenKind` values when deciding whether to start parsing an expression, modifier value, or message body. | Derive the allowlist from catalog metadata such as `ExpressionStartTokens`, `TokenMeta.IsMessagePosition`, or per-construct slot start sets. |
| **B — Enum-identity switch on `*Kind` to dispatch per-member behavior** | `kind switch { FooKind.Bar => …, FooKind.Baz => … }` where each arm exists "because the language says so." Per-member behavior leaks from metadata into the consumer. | Move the behavior onto the catalog's meta record; consumer becomes a generic walker. (Switching on a DU **subtype** remains correct — the subtype IS the metadata shape.) |
| **C — Bypassed metadata shape** | A specialized path parses or validates a construct using a stripped-down grammar that ignores the catalog's declared shape (e.g., event-argument modifiers skipping `ValueModifierMeta.HasValue`). | Reuse the shared metadata-driven path. Specializations of grammar are catalog gaps; encode them on the meta record. |
| **D — Hardcoded language fact** | An operator-implication table, type-default table, collection-suffix table, or operator-folding table lives in C# code. The next reader has to discover the table. | Promote to catalog metadata (`OperatorMeta.InverseComparison`, `TypeMeta.AbstractDefaultValue`, `TypeMeta.CollectionSyntax`, etc.). |
| **E — Sentinel-encoded semantics** | A wildcard or broadcast target is carried as `null`, `"any"`, or a magic string instead of a typed metadata field. Consumers re-derive its meaning by literal comparison. | Add a typed metadata field (`TokenMeta.IsStateWildcard`, `FieldTargetKind`, broadcast policy on transition rows). The sentinel disappears. |
| **F — Proof-engine local knowledge** | Proof discharge, diagnostic mapping, or guard reasoning encodes operator/accessor/requirement semantics in handwritten switches. | Add proof metadata to the relevant catalog (`ProofRequirementMeta.FailureDiagnostic`, `OperatorMeta.SatisfactionCovers`, accessor-level `LogicalRole`). The discharge engine becomes generic. |
| **G — Reserved** | (Reserved — no representative violations in current audit; kept for taxonomy stability.) | — |
| **H — Out-of-band classification axis** | A consumer maintains a side-channel taxonomy (`if mod == X || mod == Y` checks for "initial-ish" modifiers, paired-bound modifier checks) rather than reading a metadata flag. | Add the semantic role flag to the catalog (`Modifiers.BoundCounterpart`, `MarksInitial`, `AffectsPresence`, `AffectsWritability`). |

**Reviewer rule.** Any new pipeline-stage code that adds switch statements over `*Kind`, parses tokens by hand-crafted allowlists, or encodes language facts as local C# tables should be referred back to the catalog. If the catalog cannot express the needed metadata, the gap is the actual deliverable — not a workaround in the consumer.

These eight patterns originated in the 2026-05 catalog compliance audit. See [`docs/Working/Archive/catalog-compliance-audit.md`](../Working/Archive/catalog-compliance-audit.md) for the historical violation inventory and bug map.

---

## Catalog Schema

Three levels of detail. Use the overview map to see the full 15-catalog topology at a glance, the schema anatomy sections to understand the structurally complex catalogs, and the reference tables for complete member inventories.

---

### Level 1 — Catalog Overview Map

Two views of the same system. The **topology** diagram shows the four-layer catalog structure and cross-catalog references. The **consumer landscape** diagram shows which pipeline stages and tooling derive from which layers.

**Catalog topology**

```mermaid
flowchart TB
    subgraph L1["① Lexical foundation"]
        Tokens["Tokens (138)"]
    end

    subgraph L2["② Grammar / structure"]
        Constructs["Constructs (15)"]
        ExpressionForms["ExpressionForms (15)"]
        Outcomes["Outcomes (3)"]
        Constraints["Constraints (5)"]
    end

    subgraph L3["③ Semantic / behavior"]
        Types["Types (32)"]
        Operators["Operators (21)"]
        Operations["Operations (203)"]
        Functions["Functions (23)"]
        Modifiers["Modifiers (29)"]
        Actions["Actions (15)"]
        ProofRequirements["ProofRequirements (10)"]
    end

    subgraph L4["④ Failure modes"]
        Diagnostics["Diagnostics (148)"]
        Faults["Faults (15)"]
    end

    subgraph L5["⑤ Tooling-adjacent"]
        SemanticTokenTypes["SemanticTokenTypes (13)"]
    end

    %% Seven catalogs anchor to Tokens
    Types       --> Tokens
    Operators   --> Tokens
    Modifiers   --> Tokens
    Actions     --> Tokens
    Constructs  --> Tokens
    ExpressionForms --> Tokens
    Outcomes    --> Tokens

    %% Semantic wiring
    Functions  -->|param/return types| Types
    Operations -->|operand/result types| Types
    Operations -->|op| Operators
    Actions    -->|applicable to| Types
    Types     <-->|"implied ↔ applicable"| Modifiers

    %% Proof obligations
    Types      --> ProofRequirements
    Functions  --> ProofRequirements
    Operations --> ProofRequirements
    Actions    --> ProofRequirements

    %% Grammar nesting
    Actions    -->|AllowedIn| Constructs
    Constructs -->|AllowedIn| Constructs

    %% Failure link
    Diagnostics <-->|"PreventsFault / StaticallyPreventable"| Faults
```

**Consumer landscape**

```mermaid
flowchart LR
    subgraph Pipeline["Pipeline stages"]
        Lexer
        Parser
        TypeChecker["TypeChecker"]
        GraphAnalyzer["GraphAnalyzer"]
        ProofEngine["ProofEngine"]
        Evaluator
    end

    subgraph Layers["Catalog layers"]
        CL1["① Lexical\nTokens"]
        CL2["② Grammar\nConstructs · ExprForms · Constraints"]
        CL3["③ Semantic\nTypes · Operators · Operations\nFunctions · Modifiers · Actions\nProofRequirements"]
        CL4["④ Failure\nDiagnostics · Faults"]
    end

    subgraph Tooling["Tooling consumers"]
        LS["Language Server"]
        Grammar["TextMate Grammar"]
        MCP["MCP precept_language"]
    end

    Lexer         --> CL1
    Parser        --> CL1
    Parser        --> CL2
    Parser        --> CL3
    TypeChecker   --> CL2
    TypeChecker   --> CL3
    TypeChecker   --> CL4
    GraphAnalyzer --> CL2
    GraphAnalyzer --> CL3
    GraphAnalyzer --> CL4
    ProofEngine   --> CL3
    ProofEngine   --> CL4
    Evaluator     --> CL3
    Evaluator     --> CL4

    LS      -.-> CL1
    LS      -.-> CL2
    LS      -.-> CL3
    LS      -.-> CL4
    Grammar -.-> CL1
    Grammar -.-> CL2
    Grammar -.-> CL3
    MCP     -.-> CL1
    MCP     -.-> CL2
    MCP     -.-> CL3
    MCP     -.-> CL4
```

**Reading the diagrams:**

- **Tokens** is the lexical foundation — six catalogs carry direct `TokenMeta` or `TokenKind` references upward to it.
- **Types** and **Constructs** are the twin semantic hubs — Types for runtime behavior, Constructs for grammar schema.
- **Operations** is the typed-legality hub — it ties Operators + Types + ProofRequirements together.
- **Diagnostics ↔ Faults** is the only bidirectional pair — each points to the other via `PreventsFault` / `[StaticallyPreventable]`.
- **SemanticTokenTypes** is tooling-adjacent — `TokenMeta.VisualCategory` points upward to it, and both the TextMate grammar generator and the LSP semantic-tokens handler read its metadata. See §Catalog Inventory → SemanticTokenTypes for the one-field architecture rationale.
- `ConstructSlotKind` is a helper enum embedded in the Constructs schema surface, not a catalog — it carries no `GetMeta()` or `All`; see Level 3 for its 20-member inventory and the `SlotVocabulary` enum that pairs with it.
- In the consumer diagram, each layer box aggregates multiple catalogs. Solid arrows = pipeline stages; dashed arrows = tooling. No consumer maintains its own parallel list — all derive from catalog `All` properties.

---

### Level 2 — Schema Anatomy

Schema detail for the five structurally complex catalogs.

---

#### Constructs — grammar schema hub

`ConstructMeta` is the grammar schema hub — every declaration shape in the language maps to one entry. The schema hangs off three supporting types.

```
ConstructMeta
├── Kind              : ConstructKind                         identity
├── Name              : string                                human-readable name
├── Description       : string                                purpose description
├── UsageExample      : string                                DSL snippet
├── AllowedIn         : ConstructKind[]                  ◄── self-reference: nesting rules (empty = top-level)
├── Slots             : IReadOnlyList<ConstructSlot>     ◄── ordered schema of declaration positions
├── Entries           : ImmutableArray<DisambiguationEntry>   ◄── leading-token routing map
├── RoutingFamily     : RoutingFamily                         parser dispatch category
└── SnippetTemplate   : string?                               optional LS completion snippet

ConstructSlot
├── Kind                 : ConstructSlotKind                ◄── 20-member helper enum (see Level 3)
├── IsRequired           : bool                                  whether slot is mandatory
├── Description          : string?                               optional hint
├── TerminationTokens    : TokenKind[]?                          optional slot-local Pratt terminators
├── IsList               : bool                                  slot accepts comma- or introducer-separated items
├── IsChainable          : bool                                  slot accepts an arrow chain (action chains)
├── ItemIntroducerToken  : TokenKind?                            separator/introducer token between list items
└── Vocabulary           : SlotVocabulary                        completion vocabulary (13-member enum) for this slot position

DisambiguationEntry
├── LeadingToken      : TokenKind                        ◄── anchors to Tokens catalog
├── DisambiguationTokens : ImmutableArray<TokenKind>?         second-token disambiguation set
└── LeadingTokenSlot  : ConstructSlotKind?                    which slot the leading token occupies
```

`RoutingFamily` controls parser dispatch:

| Value | Meaning |
|-------|---------|
| `Header` | Pre-loop preamble — not through standard dispatch |
| `Direct` | Unique leading token; routed directly |
| `StateScoped` | Shares `in`/`to`/`from`; routed via `DisambiguationEntry` |
| `EventScoped` | Shares `on`; routed via `DisambiguationEntry` |

`Constructs.ByLeadingToken` is the derived index — maps each leading `TokenKind` to all construct entries that begin with it. The parser derives disambiguation from this index, not from hardcoded token sets.

---

#### Modifiers — 5-subtype discriminated union

`ModifierMeta` is a discriminated union with **5 sealed subtypes** — the clearest example of the catalog-system thesis. Previously four bare enums (`StateModifierKind`, `AccessMode`, `EnsureAnchor`, `StateActionAnchor`) are now first-class catalog members.

```mermaid
classDiagram
    class ModifierMeta {
        <<abstract>>
        Kind : ModifierKind
        Token : TokenMeta
        Description : string
        Category : ModifierCategory
        MutuallyExclusiveWith : ModifierKind[]
    }
    class ValueModifierMeta {
        ApplicableTo : TypeTarget[]
        HasValue : bool
        Subsumes : ModifierKind[]
        HoverDescription : string?
        UsageExample : string?
        SnippetTemplate : string?
    }
    class StateModifierMeta {
        AllowsOutgoing : bool
        RequiresDominator : bool
        PreventsBackEdge : bool
    }
    class EventModifierMeta {
        RequiredAnalysis : GraphAnalysisKind
    }
    class AccessModifierMeta {
        IsPresent : bool
        IsWritable : bool
    }
    class AnchorModifierMeta {
        Scope : AnchorScope
        Target : AnchorTarget
    }
    ModifierMeta <|-- ValueModifierMeta : 15 members
    ModifierMeta <|-- StateModifierMeta : 7 members
    ModifierMeta <|-- EventModifierMeta : 1 member
    ModifierMeta <|-- AccessModifierMeta : 3 members
    ModifierMeta <|-- AnchorModifierMeta : 3 members
```

**Subtype member distribution (29 total):**

| Subtype | Count | Representative members |
|---------|------:|------------------------|
| `ValueModifierMeta` | 15 | `optional`, `writable`, `nonnegative`, `positive`, `notempty`, `min`, `max`, `ordered` |
| `StateModifierMeta` | 7 | `initial` (state), `terminal`, `required`, `irreversible`, `success`, `warning`, `error` |
| `EventModifierMeta` | 1 | `initial` (event) |
| `AccessModifierMeta` | 3 | `editable` (Write), `readonly` (Read), `omit` (Omit) |
| `AnchorModifierMeta` | 3 | `in`, `to`, `from` |

> The `modify` keyword is a construct-level verb that introduces the AccessMode construct — it is not a `ModifierKind` member. The access adjective (`editable`, `readonly`) or the `omit` verb determines the `ModifierKind`. The `initial` keyword appears in two subtypes— `InitialState` (`StateModifierMeta`) and `InitialEvent` (`EventModifierMeta`) — same token text, different subtypes, different metadata. `AnchorTarget` disambiguates the three anchor kinds between ensure and state-action contexts.

---

#### Operations — typed-legality hub

`OperationMeta` is a discriminated union with **2 sealed subtypes**. 203 entries cover every legal `(operator, operand type(s)) → result type` combination in the language.

```mermaid
classDiagram
    class OperationMeta {
        <<abstract>>
        Kind : OperationKind
        Op : OperatorKind
        Result : TypeKind
        Description : string
    }
    class UnaryOperationMeta {
        Operand : ParameterMeta
    }
    class BinaryOperationMeta {
        Lhs : ParameterMeta
        Rhs : ParameterMeta
        BidirectionalLookup : bool
        Match : QualifierMatch
        ProofRequirements : ProofRequirement[]
    }
    class ParameterMeta {
        Kind : TypeKind
        Name : string?
    }
    OperationMeta <|-- UnaryOperationMeta : 9 unary
    OperationMeta <|-- BinaryOperationMeta : 194 binary
    UnaryOperationMeta --> ParameterMeta : Operand
    BinaryOperationMeta --> ParameterMeta : Lhs
    BinaryOperationMeta --> ParameterMeta : Rhs
```

- **`ParameterMeta`** uses object identity — `ParamSubject` holds a direct reference to the `ParameterMeta` instance in the containing operation or overload, enforcing referential integrity across the catalog (see Proof Obligations).
- **`BidirectionalLookup = true`** registers both `(op, lhs, rhs)` and `(op, rhs, lhs)` in the index so commutative operations use one entry for both orderings.
- **`QualifierMatch`** handles the two operations whose result type depends on qualifier comparison (`money/money`, `quantity/quantity`): `Same` → ratio result, `Different` → currency-pair or compound-unit result.

---

#### ProofRequirements — catalog meta vs. obligation instances

Two separate type hierarchies: **catalog meta** (static identity, 10 members in `ProofRequirements.All`) and **obligation instances** (per-use payload, carried inside other catalog entries that declare requirements).

**Catalog meta — DU as identity (10 members):**

```mermaid
classDiagram
    class ProofRequirementMeta {
        <<abstract>>
        Kind : ProofRequirementKind
        Description : string
        DiagnosticCode : DiagnosticCode?
    }
    class Numeric { }
    class Presence { }
    class Dimension { }
    class Modifier { }
    class QualifierCompatibility { }
    class QualifierChain { }
    class IntervalContainment { }
    class LengthContainment { }
    class CountContainment { }
    class KeyPresence { }
    ProofRequirementMeta <|-- Numeric
    ProofRequirementMeta <|-- Presence
    ProofRequirementMeta <|-- Dimension
    ProofRequirementMeta <|-- Modifier
    ProofRequirementMeta <|-- QualifierCompatibility
    ProofRequirementMeta <|-- QualifierChain
    ProofRequirementMeta <|-- IntervalContainment
    ProofRequirementMeta <|-- LengthContainment
    ProofRequirementMeta <|-- CountContainment
    ProofRequirementMeta <|-- KeyPresence
```

The base record carries a `DiagnosticCode? DiagnosticCode` field (F-LANG-CAT-09) — the catalog-mediated diagnostic emitted when this obligation kind fails. Null only for `Numeric` (1:many mapping; routes to `DivisionByZero`, `SqrtOfNegative`, `UnguardedCollectionAccess`, or `UnguardedCollectionMutation` depending on context) and `KeyPresence` (routes to `PRE0099` or `PRE0101` depending on `RequireAbsence`). Source: `src/Precept/Language/ProofRequirement.cs:204`.

**Obligation instances — DU with per-kind payloads:**

```mermaid
classDiagram
    class ProofRequirement {
        <<abstract>>
        Kind : ProofRequirementKind
        Description : string
    }
    class NumericProofRequirement {
        Subject : ProofSubject
        Comparison : OperatorKind
        Threshold : decimal
    }
    class PresenceProofRequirement {
        Subject : ProofSubject
    }
    class DimensionProofRequirement {
        Subject : ProofSubject
        RequiredDimension : PeriodDimension
    }
    class QualifierCompatibilityProofRequirement {
        LeftSubject : ProofSubject
        RightSubject : ProofSubject
        Axis : QualifierAxis
    }
    class QualifierChainProofRequirement {
        LeftSubject : ProofSubject
        LeftAxis : QualifierAxis
        RightSubject : ProofSubject
        RightAxis : QualifierAxis
    }
    class ModifierRequirement {
        Subject : ProofSubject
        Required : ModifierKind
    }
    class IntervalContainmentProofRequirement {
        Subject : ProofSubject
        TargetField : string
        DeclaredMin/Max : decimal?
        AuthoredMin/Max : decimal?
    }
    class LengthContainmentProofRequirement {
        Subject : ProofSubject
        TargetField : string
        DeclaredMinLength/MaxLength : int?
    }
    class CountContainmentProofRequirement {
        Subject : ProofSubject
        TargetField : string
        DeclaredMinCount/MaxCount : int?
    }
    class KeyPresenceProofRequirement {
        Subject : ProofSubject
        RequireAbsence : bool
    }
    ProofRequirement <|-- NumericProofRequirement
    ProofRequirement <|-- PresenceProofRequirement
    ProofRequirement <|-- DimensionProofRequirement
    ProofRequirement <|-- QualifierCompatibilityProofRequirement
    ProofRequirement <|-- QualifierChainProofRequirement
    ProofRequirement <|-- ModifierRequirement
    ProofRequirement <|-- IntervalContainmentProofRequirement
    ProofRequirement <|-- LengthContainmentProofRequirement
    ProofRequirement <|-- CountContainmentProofRequirement
    ProofRequirement <|-- KeyPresenceProofRequirement
```

`QualifierCompatibilityProofRequirement` and `QualifierChainProofRequirement` are dual-subject — they carry both `LeftSubject` and `RightSubject` independently. Consumers check `meta is ProofRequirementMeta.QualifierCompatibility` (or `.QualifierChain`) to detect dual-subject obligations without a `SubjectArity` field. Source: `src/Precept/Language/ProofRequirement.cs`.

Obligation instances are declared **inside other catalog entries** (`BinaryOperationMeta.ProofRequirements`, `FunctionOverload.ProofRequirements`, `TypeAccessor.ProofRequirements`, `ActionMeta.ProofRequirements`). The `ProofRequirementMeta` catalog describes obligation *kinds* — the instances are the actual declared obligations attached to specific catalog entries.

---

#### Diagnostics and Faults — bidirectional failure-mode pair

The only bidirectional catalog pair. Each catalog points to the other.

```
DiagnosticMeta                                       FaultMeta
─────────────────────────────────────────────        ─────────────────────────────────────
Code              : string                           Code            : string
Stage             : DiagnosticStage                  MessageTemplate : string
Severity          : Severity                         Severity        : FaultSeverity = Fatal
MessageTemplate   : string                           RecoveryHint    : string?
Category          : DiagnosticCategory
RelatedCodes      : DiagnosticCode[]?
FixHint           : string?
PreventsFault     : FaultCode?         ──────────►
SuggestionSources : SuggestionSource[]?              FaultCode enum members decorated with:
TriggerCondition  : string?                ◄─────── [StaticallyPreventable(DiagnosticCode.X)]
RecoverySteps     : string[]?
ExampleBefore     : string?
ExampleAfter      : string?
```

`DiagnosticMeta` (source: `src/Precept/Language/Diagnostics.cs:23`) carries 13 fields. Beyond the original 8 above, five additional fields drive richer LS / MCP surfacing (F-LANG-CAT-12):

- **`SuggestionSources : SuggestionSource[]?`** — identifies the symbol namespaces the language server searches for "did you mean?" suggestions. The `SuggestionSource` enum has 4 members: `UserFields`, `UserStates`, `UserEvents`, `FunctionCatalog`.
- **`TriggerCondition : string?`** — human-readable description of the exact source-code condition that triggers this diagnostic. Surfaced by MCP `precept_diagnostic` for AI grounding.
- **`RecoverySteps : string[]?`** — ordered remediation steps shown to users when the diagnostic fires.
- **`ExampleBefore : string?`** / **`ExampleAfter : string?`** — minimal source-text examples bracketing the fix. MCP and LS both surface these.

`FaultMeta` (source: `src/Precept/Language/Faults.cs:3`) carries 4 fields: `Code`, `MessageTemplate`, `Severity` (`FaultSeverity.Fatal` for every shipping fault — the field exists so non-fatal severities are representable when the runtime grows them; F-LANG-CAT-14), and a per-fault `RecoveryHint` populated on every member.

**Two enforcement layers close the loop:**

| Mechanism | What it enforces |
|-----------|-----------------|
| `DiagnosticMeta.PreventsFault` | Every diagnostic that prevents a runtime fault names which fault |
| `[StaticallyPreventable(DiagnosticCode.X)]` on `FaultCode` | Every runtime fault names the compile-time rule that should prevent it — **PRECEPT0002** fires if a `FaultCode` member lacks this attribute |

The pair is bidirectional by design: diagnostics are the compile-time face, faults are the runtime face. Every fault that can be statically prevented has a corresponding diagnostic, and every diagnostic that prevents a fault names it. Any gap in the linkage is a build error.

---

### Level 3 — Member Inventories

Complete enum counts and key groupings. See source files for the full member lists.

| Catalog | Enum | Members | Key groupings | Source |
|---------|------|--------:|---------------|--------|
| **Tokens** | `TokenKind` | 138 | Keywords (declaration, lifecycle, expression, action, constraint, qualifier, anchor), Operators, Punctuation, Literals, Special | `TokenKind.cs` |
| **Types** | `TypeKind` | 32 | Scalar (6: `string`, `boolean`, `integer`, `decimal`, `number`, `choice`), Temporal (8), Business-domain (7), Collection (3), Special (2: `error`, `stateref`) | `TypeKind.cs` |
| **Functions** | `FunctionKind` | 23 | Numeric (12), String (8), Temporal (1), CI variants (2) | `FunctionKind.cs` |
| **Operators** | `OperatorKind` | 21 | Arithmetic (5: `+`, `-`, `*`, `/`, `%`), Comparison (8), Logical (3), Membership (1), Negation (1), CI (2) | `OperatorKind.cs` |
| **Operations** | `OperationKind` | 203 | Unary (9), Binary: arithmetic, comparison, logical, business-type (money/quantity/price/exchangerate/period), CI string | `OperationKind.cs` |
| **Modifiers** | `ModifierKind` | 29 | Field/Value (15), State (7), Event (1), Access (3), Anchor (3) — see DU anatomy above | `ModifierKind.cs` |
| **Actions** | `ActionKind` | 15 | Scalar (1: `set`), Set collection (3), Queue (4), Stack (3), Universal (1: `clear`), Compound (3: `append`, `insert`, `put`) | `ActionKind.cs` |
| **Constructs** | `ConstructKind` | 15 | Header (1), Direct declarations (4), State-scoped (5), Event-scoped (2), Stateless (1: `EventRow`), Construction rows (2), Transition-reject (1) | `ConstructKind.cs` |
| **ExpressionForms** | `ExpressionFormKind` | 15 | Atoms (3: literal, identifier, grouped), Composites (5), Invocations (3: function/method/CI-function), Collection (1: list), Quantifier (1), InterpolatedString (1), InterpolatedTypedConstant (1) | `ExpressionForms.cs` |
| **Outcomes** | `OutcomeKind` | 3 | Transition (1), NoTransition (1), Reject (1) | `Outcomes.cs` |
| **Constraints** | `ConstraintKind` | 5 | Invariant (1), StateAnchored (3: resident/entry/exit), EventPrecondition (1) | `ConstraintKind.cs` |
| **ProofRequirements** | `ProofRequirementKind` | 10 | Numeric, Presence, Dimension, Modifier, QualifierCompatibility, QualifierChain, IntervalContainment, LengthContainment, CountContainment, KeyPresence | `ProofRequirementKind.cs` |
| **Diagnostics** | `DiagnosticCode` | 148 | By stage — Lex (8), Parse (7+), Type (60+), Graph (2+), Proof (3+); see `DiagnosticCode.cs` for the canonical breakdown | `DiagnosticCode.cs` |
| **Faults** | `FaultCode` | 15 | Arithmetic (2: `DivisionByZero`, `SqrtOfNegative`), Type/field (4), Collection (2), Qualifier (1), Numeric (2), Range/bound (3: `OutOfRange`, `LengthBoundViolation`, `CountBoundViolation`), `FunctionArityMismatch` + `FunctionArgConstraintViolation` | `FaultCode.cs` |
| **SemanticTokenTypes** | `SemanticTokenTypeKind` | 13 | Visual classification — name/state/event/field/argument identifiers, semantic vs grammar keywords, operator, type, value, message, comment, typed literal | `SemanticTokenTypes.cs` |

> **`ConstructSlotKind`** (20 members — `IdentifierList`, `TypeExpression`, `ModifierList`, `StateEntryList`, `ArgumentList`, `ComputeExpression`, `GuardClause`, `ActionChain`, `Outcome`, `StateTarget`, `EventTarget`, `EnsureClause`, `BecauseClause`, `AccessModeKeyword`, `FieldTarget`, `RuleExpression`, `InitialMarker`, `RejectClause`, `SuccessOutcome`, `EventEntryList`) is a helper enum in the Constructs schema surface, not a catalog. The paired **`SlotVocabulary`** enum (13 members: `None`, `StateNames`, `EventNames`, `FieldNames`, `ActionVerbs`, `TypeKeywords`, `Modifiers`, `Expression`, `TopLevel`, `OutcomeKeywords`, `AccessModes`, `StateEntryNames`, `RejectReason`) declares what completion vocabulary each slot offers — it drives `CompletionHandler` dispatch once `SlotPositionResolver` ships (Slice 3). Source: `ConstructSlot.cs`.

---

## Pattern Definition

A catalog has four parts:

### 1. Kind enum — the closed set

```csharp
public enum TokenKind
{
    // ── Keywords: Declaration ───────────────────────────
    Precept,
    Field,
    State,
    Writable,
    ...
}
```

A plain C# `enum`. One member per distinct entry. No attributes — metadata lives in the switch, not on the enum. Grouped by section with comment headers for readability.

### 2. Meta record — what consumers need to know

```csharp
public sealed record TokenMeta(
    TokenKind                      Kind,
    string?                        Text,
    IReadOnlyList<TokenCategory>   Categories,
    string                         Description,
    SemanticTokenTypeKind?         VisualCategory = null,
    TokenKind[]?                   ValidAfter = null,
    bool                           IsAccessModeAdjective = false,
    bool                           IsStateWildcard = false,
    bool                           IsFieldBroadcast = false,
    bool                           IsFunctionCallLeader = false,
    bool                           IsMessagePosition = false
)
{
    // Computed — derived from Types.All accessor names; not a constructor parameter.
    public bool IsValidAsMemberName { get; }
}
```

`TokenMeta` now carries one lightweight visual-catalog link: `VisualCategory` points to `SemanticTokenTypes` by enum kind. The bridge direction remains **unidirectional upward** for object references: downstream catalogs (Types, Operators, Modifiers, Actions) point up to Tokens via `TokenMeta Token` object references. Consumers that need reverse direction (token → catalog entry or token → visual metadata) use **derived frozen indexes** built at startup or the corresponding catalog lookup:

```csharp
// Derived at startup from the catalog that owns the relationship
FrozenDictionary<TokenKind, TypeMeta>     TypesByToken     = Types.All.ToFrozenDictionary(t => t.Token.Kind);
FrozenDictionary<TokenKind, OperatorMeta> OperatorsByToken = Operators.All.ToFrozenDictionary(o => o.Token.Kind);
```

This avoids cross-ref fields on `TokenMeta` that would be inapplicable for most members (only ~25 of 91+ tokens are type keywords, ~16 are operators). It also avoids the dual-use problem — `Set` is both an action and a type, `Min`/`Max` are both modifiers and functions — which would require either multiple nullable fields or a DU wrapper. Derived indexes handle dual-use naturally: each catalog builds its own index from its own `All` property.

The LS never needs token → catalog entry lookups — it works from AST nodes and resolved symbols, which already carry the typed `Kind`. MCP iterates each catalog's `All` directly. The reverse index exists for any consumer that needs it, derived from the source of truth (the downstream catalog's `Token` field).

An immutable record holding all metadata for a single enum member. The shape varies per catalog — each Meta type carries exactly the fields its consumers need, no more. Shared fields across all catalogs:

| Field | Purpose |
|-------|---------|
| The kind enum value | Identity — which member this metadata describes |
| A string code or text | Stable string identity for serialization (MCP, LS) |
| A description or message template | Human-readable explanation |

Domain-specific fields are added as needed. `TokenMeta` has `Categories` and nullable `Text`. `DiagnosticMeta` has `Stage` and `Severity`. `FaultMeta` has only `Code` and `MessageTemplate`. The meta type is right-sized for its consumers.

### Meta record shape: flat record vs discriminated union

When all catalog members share the same metadata shape, the meta type is a **flat sealed record**. When members fall into groups with genuinely different fields, the meta type is a **discriminated union** — an abstract record base with sealed subtypes.

**DU with different fields** — the subtype carries fields that only make sense for that group. `ModifierMeta` and `OperationMeta` use this pattern:

```csharp
// ValueModifierMeta carries ApplicableTo, Subsumes — inapplicable to StateModifierMeta
public sealed record ValueModifierMeta(..., TypeTarget[] ApplicableTo, ...) : ModifierMeta(...);
// StateModifierMeta carries AllowsOutgoing, RequiresDominator — inapplicable to ValueModifierMeta
public sealed record StateModifierMeta(..., bool AllowsOutgoing, ...) : ModifierMeta(...);
```

**DU as identity** — subtypes carry no unique fields, but the type IS the semantic signal. `ConstraintMeta` and `ProofRequirementMeta` use this pattern. Consumers pattern-match exhaustively; the compiler catches unhandled cases when new members are added:

```csharp
public abstract record ConstraintMeta(ConstraintKind Kind, string Description)
{
    public sealed record Invariant()     : ConstraintMeta(...);
    public abstract record StateAnchored(ConstraintKind Kind, string Description) : ConstraintMeta(Kind, Description);
    public sealed record StateResident() : StateAnchored(...);  // in <State> ensure
    public sealed record StateEntry()    : StateAnchored(...);  // to <State> ensure
    public sealed record StateExit()     : StateAnchored(...);  // from <State> ensure (three subtypes under StateAnchored)
    public sealed record EventPrecondition() : ConstraintMeta(...);
}

// Consumer: type IS the signal — no field check needed
var plan = meta switch
{
    ConstraintMeta.Invariant         => alwaysBucket,
    ConstraintMeta.StateAnchored     => stateBucket,  // matches all three state kinds
    ConstraintMeta.EventPrecondition => eventBucket,
};
```

The full five-subtype hierarchy: `Invariant`, `StateResident`, `StateEntry`, `StateExit`, and `EventPrecondition`. `StateResident`, `StateEntry`, and `StateExit` are all subtypes of the `StateAnchored` abstract intermediate. Canonical shape locked in `precept-builder.md` §Pass 4 (CC#7).

This is **not** an enum in disguise. The DU provides:
1. **Compile-time exhaustiveness** — a new enum member without a matching subtype is a build error at every pattern-match site
2. **Type IS the semantic signal** — `meta is StateAnchored` is structurally safer than `meta.Scope == ConstraintScope.State`; no field value can be misassigned
3. **Hierarchy encodes grouping** — the intermediate `StateAnchored` abstract layer expresses that three kinds share a scope axis without needing an extra `ConstraintScope` enum field
4. **Extensibility** — adding a field to one subtype later is non-breaking; flat records require all-member changes

The rule: if you find yourself adding a `ScopeKind`, `SubjectArity`, or similar classification field whose values map 1:1 to subsets of enum members, that field is a sign the meta should be a DU instead.

### 3. Static catalog class — the exhaustive switch

```csharp
public static class Diagnostics
{
    public static DiagnosticMeta GetMeta(DiagnosticCode code) => code switch
    {
        DiagnosticCode.UnterminatedStringLiteral => new(nameof(...), ...),
        DiagnosticCode.InvalidCharacter          => new(nameof(...), ...),
        ...
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null),
    };

    public static IReadOnlyList<DiagnosticMeta> All { get; } =
        Enum.GetValues<DiagnosticCode>().Select(GetMeta).ToList();

    public static Diagnostic Create(DiagnosticCode code, SourceSpan span, params object?[] args)
    {
        var meta = GetMeta(code);
        return new(meta.Severity, meta.Stage, meta.Code,
            string.Format(meta.MessageTemplate, args), span);
    }
}
```

Three members:

| Member | Purpose | Present in all catalogs |
|--------|---------|------------------------|
| `GetMeta(Kind)` | Exhaustive switch — every enum member maps to its metadata | Yes — this is the catalog |
| `All` | `IReadOnlyList<Meta>` built from `Enum.GetValues<Kind>().Select(GetMeta)` | Yes — MCP enumeration surface |
| `Create(...)` | Factory that formats a runtime output value from the metadata | When the catalog has an associated output type |

Domain-specific members are added as needed. `Tokens` has a `Keywords` frozen dictionary for lexer keyword lookup, derived from `All`.

### 4. Output value type — what the pipeline produces

```csharp
public readonly record struct Diagnostic(
    Severity        Severity,
    DiagnosticStage Stage,
    string          Code,
    string          Message,
    SourceSpan      Span
);
```

Constructed exclusively through the catalog's `Create()` factory, which derives fields from the meta. Not all catalogs have an output type — `Tokens.All` is consumed directly as metadata, with `Token` being the lexer's own output type.

## Why Exhaustive Switch, Not Attributes + Reflection

An alternative approach: decorate enum members with attributes (`[TokenCategory]`, `[TokenDescription]`) and read them via reflection at startup. That works, but: **a missing attribute is a runtime failure, not a compile-time failure.** Closing the gap requires a custom Roslyn analyzer — two layers doing related work.

The exhaustive switch uses the C# compiler directly:

- Add an enum member without a switch arm → **CS8509** (non-exhaustive switch expression). With `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` → **build fails**.
- No custom Roslyn rule needed for catalog completeness.
- No reflection. No source generator. No startup cost.
- The switch IS the catalog. The compiler IS the enforcement.

The trade-off: the switch is more verbose than attributes. At Precept's scale (50–170 enum members per catalog), this is acceptable.

## Roslyn Enforcement Layer

The exhaustive switch (CS8509) enforces catalog completeness — every enum member must have metadata. But the catalog system has obligations the C# compiler cannot see: that every `FaultCode` member is linked to a preventing diagnostic; that the `Diagnostic` and `Fault` output values are constructed via their factory rather than with arbitrary string codes; that `ParamSubject` parameter references stay reference-equal to the owning overload's `ParameterMeta` instances; that every catalog DU base is sealed and every subtype is exhaustively pattern-matched. These cross-file, semantic obligations require analyzers.

**Why two gates exist.** The analyzer family enforces a single architectural guarantee: **the catalog's declared promises are kept and stay kept.** Adding a new diagnostic code without an emission site silently degrades user experience — the spec advertises a compiler check the compiler does not run. Adding an emission site without a test allows regressions to slip through. **Gate 1** (`PRECEPT0027`) requires every declared `DiagnosticCode` either to have an emission site (direct `Diagnostics.Create(...)` call, catalog-mediated `CIDiagnosticCode`, or proof-engine dispatch) or to be on the allow-list with a tracking comment. **Gate 2** (`PRECEPT0028`) requires every emitted code to be referenced in at least one test file. The allow-list hygiene rules (`PRECEPT0029`, `PRECEPT0030`) prevent the allow-list itself from becoming a quiet dumping ground for unmet obligations — every allow-list entry must be a tracked, time-bounded debt. Together this is the answer to "how do we know the catalog's promises are kept?"

**Catalog DU discipline.** A catalog whose meta is a discriminated union (`ModifierMeta`, `OperationMeta`, `ConstraintMeta`, `ProofRequirementMeta`, etc.) only delivers its safety guarantee when the hierarchy is **sealed** (no out-of-tree subclassing can bypass the catalog) and when every consumer pattern-matches the base type **exhaustively** (no `_ => default` wildcard arm that silently swallows a new subtype). `PRECEPT0025` forbids wildcard arms on catalog DU pattern matches, and `PRECEPT0026` verifies every concrete subtype has a corresponding arm. Without these, the DU degrades to "switch-on-enum with an inert subtype hierarchy" — the failure mode the DU exists to prevent.

### Enforcement Rules — Categorized

The ten categories below cover every analyzer rule currently shipping in `src/Precept.Analyzers/`. Each row points to the source file(s) for the per-rule mechanics — this table intentionally does not duplicate them.

| # | Category | What it enforces (one line) | Rule prefix(es) | Source file(s) |
|---|---|---|---|---|
| 1 | Fault / diagnostic conventions | `Fail(...)` must pass a `FaultCode` first; output values (`Diagnostic`, `Fault`) must go through the `Create()` factory; every `FaultCode` carries `[StaticallyPreventable(DiagnosticCode.X)]`. | `PRECEPT0001`–`PRECEPT0004` | `Precept0001FailMustUseFaultCode.cs`, `Precept0002FaultCodeMustHaveStaticallyPreventable.cs`, `Precept0003DiagnosticMustUseCreate.cs`, `Precept0004FaultMustUseCreate.cs` |
| 2 | `GetMeta` exhaustiveness | Every catalog's `GetMeta(...)` switch covers every enum member; missing arms are a build error rather than a runtime throw. | `PRECEPT0007` | `Precept0007GetMetaExhaustiveness.cs` |
| 3 | Per-catalog cross-reference integrity | Every cross-catalog reference is resolvable and consistent — `Types ↔ Tokens`, `Operations ↔ Operators`, `Modifiers ↔ TypeTarget`, `Functions ↔ Types`, `Actions ↔ TypeTarget`, `Constructs ↔ TokenKind`, `Diagnostics ↔ FaultCode`, `Faults ↔ DiagnosticCode`, `Operators ↔ TokenKind`. | `PRECEPT0008`–`PRECEPT0017` (subset) | `Precept0008TypesCrossRef.cs`, `Precept0009OperationsCrossRef.cs`, `Precept0011ModifiersCrossRef.cs`, `Precept0012FunctionsCrossRef.cs`, `Precept0013ActionsCrossRef.cs`, `Precept0014ConstructsCrossRef.cs`, `Precept0015DiagnosticsCrossRef.cs`, `Precept0016FaultsCrossRef.cs`, `Precept0017OperatorsCrossRef.cs` |
| 4 | Semantic enum zero-slot enforcement | An enum used as a semantic axis must explicitly declare a sentinel zero member (or carry the `[AllowZeroDefault]` annotation), preventing a silently-defaulted zero from becoming an unintended axis value. | `PRECEPT0018` | `Precept0018SemanticEnumZeroSlot.cs` |
| 5 | Pipeline exhaustiveness (`[HandlesCatalogExhaustively]`) | A class decorated with `[HandlesCatalogExhaustively(typeof(Kind))]` must carry `[HandlesCatalogMember(Kind.X)]` annotations whose union covers every member of the catalog. Used where dispatch is distributed across multiple methods and CS8509 cannot see across the split (canonical example: `ExpressionFormKind` in the Pratt parser). | `PRECEPT0019` | `Precept0019PipelineCoverageExhaustiveness.cs` |
| 6 | Operator / token integrity | An operator's token reference is unique (no two `OperatorMeta` instances share a token); no `TokenMeta.Text` collisions across the Tokens catalog; operator metadata is declared via the canonical `Operators.cs` factory rather than inline; `OperatorMeta` DU shape invariants are upheld. | `PRECEPT0020`–`PRECEPT0023` | `Precept0020OperatorsTokenCollision.cs`, `Precept0021TokensDuplicateText.cs`, `Precept0022OperatorsInlineToken.cs`, `Precept0023OperatorsDUShapeInvariants.cs` |
| 7 | Catalog DU discipline (sealed hierarchies + completeness) | Catalog DU base records must be sealed at the family root (no wildcard `_ => default` arms); every concrete subtype must have a pattern-match arm at every consumer site. Drives the DU-as-identity safety guarantee. | `PRECEPT0024`–`PRECEPT0026` | `Precept0024AntiMirroringEnforcement.cs`, `Precept0025CatalogDUWildcard.cs`, `Precept0026CatalogDUCompleteness.cs` |
| 8 | Diagnostic emission gate (Gate 1) | Every `DiagnosticCode` member must have at least one emission site (direct `Diagnostics.Create(...)`, catalog-mediated `CIDiagnosticCode`, or proof-engine dispatch) — or be allow-listed with a tracking entry. Prevents catalog-declared diagnostics from advertising checks the compiler does not actually run. | `PRECEPT0027` | `Precept0027DiagnosticEmissionCoverage.cs`, `DiagnosticCoverageScanner.cs` |
| 9 | Diagnostic test gate (Gate 2) | Every emitted `DiagnosticCode` must be referenced in at least one test file. Scoped to codes that pass Gate 1 — codes on the Gate 1 allow-list have no test obligation. Prevents emitted diagnostics from regressing untested. | `PRECEPT0028` | `Precept0028DiagnosticTestCoverage.cs`, `DiagnosticCoverageScanner.cs` |
| 10 | Allow-list hygiene | Allow-list entries for Gate 1 / Gate 2 must carry tracking metadata (issue link or deadline marker) and must not be stale; analyzer flags entries that have been on the list past their tracking horizon. | `PRECEPT0029`, `PRECEPT0030` | `DiagnosticCoverageAllowLists.cs` |

**Two-layer enforcement model.** The C# compiler enforces metadata completeness through CS8509 on every `GetMeta()` switch (no analyzer needed — the switch IS the catalog). The Roslyn layer enforces everything CS8509 cannot see: cross-file references, factory discipline, distributed dispatch coverage, catalog DU hierarchy invariants, and the catalog-promises-are-kept gates.

| Layer | Mechanism | What it enforces | Scope |
|-------|-----------|-----------------|-------|
| **Compiler** | CS8509 (exhaustive switch) | Every enum member has a metadata entry in its catalog's `GetMeta()` switch | All catalogs |
| **Roslyn** | `PRECEPT0001`–`PRECEPT0030` | The 10 categories above | Diagnostics + Faults + cross-catalog references + DU discipline + emission/test gates |

For per-rule mechanics — error message, severity, code shape, suppression mechanism, allow-list format — read the source files cited above. They are short and self-contained; duplicating their text here is the failure mode the pointer-philosophy rewrite is meant to prevent.

## Exhaustiveness Enforcement Strategies

The catalog system uses two distinct enforcement strategies for dispatch over catalog enum types. The choice depends on the dispatch topology — whether responsibility is centralized in a single switch or distributed across multiple handler methods.

### Strategy 1: CS8509 — Compiler-Enforced Exhaustive Switch

**When to use:** A single centralized C# switch expression dispatches over all members of a catalog enum type.

The compiler fires CS8509 (non-exhaustive switch expression) at the switch site if a new named member is added without a corresponding arm. With `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, this is a build-breaking error. No annotation or analyzer is needed — the compiler enforces it directly.

**Current dispatch sites using CS8509:**

| Catalog Enum | Dispatch Site | What it builds |
|---|---|---|
| `ConstructKind` | `BuildNode` | AST node construction per grammar construct |
| `ActionKind` | `ParseSetStatement`, `ParseTransitionStatement`, `ParseEmitStatement`, `ParseFailStatement` | Action parsing — shape-partitioned across four switch expressions |
| `ActionSyntaxShape` | `ParseActionStatement` | Action verb → syntax shape routing |
| `ConstructSlotKind` | `InvokeSlotParser` | Slot kind → parser method dispatch |

In each case, the switch expression covers the full enum surface in one location. Adding a member without an arm is a compile error at that exact site.

### Strategy 2: `[HandlesCatalogExhaustively]` + `[HandlesCatalogMember]` — Analyzer-Enforced Distributed Dispatch

**When to use:** Dispatch is distributed across multiple handler methods with no single switch expression covering all members. CS8509 physically cannot see across the dispatch split — each method handles a subset of members, but no location covers the whole enum.

The canonical example is `ExpressionFormKind` in the Pratt parser. The nud/led architecture splits responsibility:

- **`ParseAtom`** handles null-denotation forms: literals, identifiers, prefix operators, parenthesized expressions, function calls
- **`ParseExpression`** handles left-denotation forms: infix operators, postfix operators, method calls

No single switch covers all 11 expression forms. Adding `ExpressionFormKind.NewForm` without a corresponding handler method would silently leave it unhandled — the compiler cannot see the gap.

**Enforcement mechanism:**

1. The class (or partial primary declaration) is annotated with `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` — declaring "this class collectively handles every member of this catalog enum."
2. Each handler method is annotated with `[HandlesCatalogMember(ExpressionFormKind.Literal)]` (one per form handled) — declaring "this method is responsible for this specific catalog member."
3. **PRECEPT0019** (pipeline coverage exhaustiveness analyzer) verifies: the union of all `[HandlesCatalogMember]` annotations on the class covers the full enum. If a member is missing, PRECEPT0019 fires a diagnostic.

This gives the same safety guarantee as CS8509 — no member goes unhandled — but operates across distributed dispatch boundaries where the compiler's switch exhaustiveness cannot reach.

### Decision Rule for New Dispatchers

When adding a new dispatcher over a catalog enum type, choose at the same commit:

| Dispatch topology | Enforcement | Annotation needed |
|---|---|---|
| Single centralized switch expression | CS8509 (compiler) | None |
| Distributed handler methods (multiple methods, no unified switch) | PRECEPT0019 (analyzer) | `[HandlesCatalogExhaustively(typeof(T))]` on class + `[HandlesCatalogMember(Kind.X)]` on each handler |

Do not defer this decision. The enforcement mechanism is chosen at the commit that introduces the dispatch, not retrofitted later.

### Implementation Dispatchers

Implementation dispatchers (TypeChecker, ProofEngine, Evaluator, etc.) follow the same decision rule: if the dispatch uses a centralized switch expression, CS8509 enforces exhaustiveness automatically; if the dispatch uses distributed handler methods, use `[HandlesCatalogExhaustively]` + `[HandlesCatalogMember]` and PRECEPT0019 enforcement. See the decision rule table above. The catalog metadata itself may drive behavior generically (no per-kind switch) — in that case, neither enforcement mechanism applies because there is no per-kind dispatch to exhaust.

## Naming Convention

| Part | Convention | Examples |
|------|-----------|----------|
| Kind enum | `{Thing}Kind` or `{Thing}Code` | `TokenKind`, `DiagnosticCode`, `FaultCode` |
| Meta record | `{Thing}Meta` | `TokenMeta`, `DiagnosticMeta`, `FaultMeta` |
| Catalog class | `{Thing}s` (plural) | `Tokens`, `Diagnostics`, `Faults` |
| Output value | `{Thing}` (singular) | `Token`, `Diagnostic`, `Fault` |

**Kind vs Code:** Use `Kind` when the enum classifies a *kind of thing* (tokens have kinds, types have kinds). Use `Code` when the enum identifies a *rule or failure mode* (diagnostics have codes, faults have codes).

## Catalog Inventory

### Language Definition Catalogs

These twelve catalogs describe what the Precept language IS.

#### 1. Tokens (✅ Implemented)

The lexical vocabulary. 90+ members spanning keywords, operators, punctuation, literals, identifiers, and structural tokens.

| Part | Type |
|------|------|
| Kind enum | `TokenKind` |
| Meta record | `TokenMeta(Kind, Text?, Categories[], Description, VisualCategory?, ValidAfter[]?, IsAccessModeAdjective, IsStateWildcard, IsFieldBroadcast, IsFunctionCallLeader, IsMessagePosition)` — `IsValidAsMemberName` is a computed property (derived from `Types.All` accessor names, not a constructor parameter) |
| Catalog class | `Tokens` — `GetMeta()`, `All`, `Keywords` (frozen dictionary for lexer lookup) |
| Output type | `Token` (produced by lexer from scan state, not via `Create()`) |

> **✅ Resolved — No `SemanticTokenModifiers` field on `TokenMeta`**
> Precept tokens carry zero LSP semantic token modifier bits. The modifier flags defined in the LSP specification (`readonly`, `defaultLibrary`, `deprecated`, etc.) describe declaration-role dimensions that have no meaning in Precept's token taxonomy. Tokens are categorized by their structural role in `TokenMeta`, while visual identity is derived through `TokenMeta.VisualCategory` → `SemanticTokenTypes`. The language server hardcodes `tokenModifiers: 0` for all tokens and no catalog property is needed.
> *Resolved: 2026-05-07 — Wave 4, team-autonomous*

**Consumers:** MCP vocabulary, LS semantic tokens, LS completions, lexer keyword lookup, TextMate grammar keyword alternations.

#### 2. Types (✅ Implemented)

The type system's family taxonomy. Each member represents a type *family*.

| Part | Type |
|------|------|
| Kind enum | `TypeKind` (26 members) |
| Meta record | `TypeMeta` — full shape with `Traits`, `WidensTo`, `ImpliedModifiers`, `Accessors` (see below) |
| Catalog class | `Types` — `GetMeta()`, `All` |
| Output type | `Type` — `abstract record` hierarchy with qualifier payloads (e.g., `MoneyType(string? Currency)`, `PriceType(string? Currency, string? Unit, string? Dimension)`) |

`TypeKind` is the catalog key. `Type` is the type checker's working type — an `abstract record` hierarchy where each sealed variant carries qualifier payloads. `Type.Kind` bridges from one to the other, same as `Token.Kind` bridges `Token` to `TokenKind`. The `abstract Kind` property is compiler-enforced: adding a new `Type` variant without implementing it fails to build.

**Members (1:1 with `Type` variants in `SemanticIndex.cs`):**

| Category | Members |
|----------|---------|
| Scalar | `String`, `Boolean`, `Integer`, `Decimal`, `Number`, `Choice` |
| Temporal | `Date`, `Time`, `Instant`, `Duration`, `Period`, `Timezone`, `ZonedDateTime`, `DateTime` |
| Business-domain | `Money`, `Currency`, `Quantity`, `UnitOfMeasure`, `Dimension`, `Price`, `ExchangeRate` |
| Collection | `Set`, `Queue`, `Stack` |
| Special | `Error`, `StateRef` |

**Consumers:** MCP type vocabulary (field types in `precept_compile` output), LS hover (type documentation), LS completions (type names in expression context), function catalog (parameter/return type signatures), operator lane definitions (lhs/rhs/result types), modifier applicability matrix.

##### TypeMeta — full shape

```csharp
public record TypeMeta(
    TypeKind                     Kind,
    TokenMeta?                   Token,              // object reference to Tokens catalog entry; null for special types (Error, StateRef)
    string                       Description,
    TypeCategory                 Category,
    string                       DisplayName,        // required — human-readable type name (e.g., "zoned date-time", "money")
    QualifierShape?              QualifierShape      = null,
    TypeTrait                    Traits              = TypeTrait.None,
    IReadOnlyList<TypeKind>?     WidensTo            = null,
    ModifierKind[]?              ImpliedModifiers    = null,
    IReadOnlyList<TypeAccessor>? Accessors           = null,
    string?                      HoverDescription    = null,
    string?                      UsageExample        = null,
    bool                         NotemptyApplicable  = true,
    IReadOnlyList<TokenKind>?    ChoiceLiteralTokens = null,
    ContentValidation?           ContentValidation   = null
);
```

> **✅ Resolved (CC#16) — user-facing filter is structural, not a field.** No `IsUserFacing` boolean was added to `TypeMeta`. The architectural conclusion: `Token` nullability *is* the user-facing signal. The two internal types (`TypeKind.Error`, `TypeKind.StateRef`) carry `Token: null`; every user-facing type carries a non-null `TokenMeta` reference because every surface type has a surface keyword. Filtering on `meta.Token is not null` therefore picks out exactly the user-facing types — the structural equivalent of an `IsUserFacing` flag without the duplication or drift risk. The canonical filter site is `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs:518` (`Types.All.Where(meta => meta.Token is not null)`). This is the permanent solution — the original CC#16 question is closed.

The `Token` field holds a direct reference to the `TokenMeta` instance from the Tokens catalog (nullable for special types like `Error` and `StateRef` that have no surface keyword). Consumers access the keyword text via `typeMeta.Token.Text` — no string duplication, no cross-catalog lookup. The Tokens catalog initializes first; all other catalogs reference its instances.

##### TypeMeta — undocumented fields (F-LANG-CAT-21)

The `TypeMeta` constructor (see `src/Precept/Language/Type.cs:207`) carries two additional fields the prose above did not call out:

- **`ImpliedQualifiers : DeclaredQualifierMeta[]?`** — qualifier metadata intrinsically carried by this type regardless of explicit field declarations. Used by the proof engine's `ResolveQualifierOnAxis` after declared qualifiers are exhausted. Example: `duration` carries an implied `TemporalDimension(Time, Baseline)` because duration is intrinsically a time-dimension measurement. Defaults to empty.
- **`RequiredBoundQualifierAxes : IReadOnlyList<QualifierAxis>?`** — qualifier axes that must be present when `min`/`max` bounds are declared on this type. Empty means bounds do not require qualifier context.

Both fields shipped in the same window as the qualifier-axis additions documented in §Qualifier Propagation; including them here keeps the meta-shape claim complete.

> **Static initialization constraint:** No catalog in Layers ②–④ may reference `Tokens` static members in its own static field initializers or cctor — this is the normal downward direction and is safe. The reverse — `Tokens` referencing a downstream catalog's static members — must use `Lazy<T>` to defer materialization past cctor completion. Currently, `Tokens.KeywordsValidAsMemberName` is the only such reverse reference (deferred via `Lazy<FrozenSet<TokenKind>>`). This constraint exists because .NET's cctor re-entrancy returns `null` for a static field that hasn't been assigned yet on the same thread — a reverse reference in a field initializer will silently receive `null` rather than a valid value.

`DisplayName` is required — every type must have a human-readable name. Single-word types use their keyword (e.g., `"money"`); multi-word types use the human form (e.g., `"zoned date-time"`, `"unit of measure"`). Omitting it is a compile error.

##### ContentValidation — typed-literal registration hook

`TypeMeta.ContentValidation` is the compile-time typed-literal registration hook. The DU subtype declares how `'...'` content is validated for the type, and `TypedConstantValidation.Validate(...)` is the single dispatcher. The DU is the registry — there is no parallel validator interface.

`ContentValidation` carries an `InterpolationFormsCategory?` field (see `src/Precept/Language/Type.cs`) that identifies which interpolated-typed-constant form-grammar set applies to the type. Subtypes that always support interpolation hardcode their category (e.g., `MoneyValidation → InterpolationFormsCategory.Money`); polymorphic subtypes (`NodaTimeValidation`, `ClosedSetValidation`) accept it as an optional parameter. The type checker reads this field to dispatch to form arrays without a TypeKind switch — `null` means the type accepts typed-constant literals but not the interpolated form.

##### TypeRuntime — typed-lane registration (target runtime shape)

The runtime design also uses per-type registration metadata for JSON ingress/egress and typed CLR lanes. That runtime registration shape is documented in `docs/runtime/runtime-api.md`; it is target-state documentation rather than a direct description of today's `TypeMeta` record.

The property uses the abstract base:

```csharp
// Abstract base — catalog holds this; carries JSON, string, and executor delegates
public abstract class TypeRuntime
{
    public abstract PreceptValue ReadJson(ref Utf8JsonReader reader);
    public abstract void WriteJson(Utf8JsonWriter writer, PreceptValue value);
    public abstract PreceptValue ParseString(string text);
    public abstract string FormatString(PreceptValue value);
    
    // Catalog-owned executor delegates, indexed by OperationKind ordinal.
    // The builder fetches these at compile time and embeds the delegate directly in each
    // BinaryOp / UnaryOp opcode. The evaluator calls opcode.Executor(l, r) — it never
    // indexes these arrays at evaluation time. Slots for inapplicable operations carry null;
    // the type checker guarantees no null-slot delegate is ever embedded in an opcode.
    public abstract Func<PreceptValue, PreceptValue, PreceptValue>?[] BinaryExecutors { get; }
    public abstract Func<PreceptValue, PreceptValue>?[] UnaryExecutors { get; }
}

// Generic sealed subclass — adds zero-boxing CLR lane delegates
public sealed class TypeRuntime<T> : TypeRuntime
{
    public Func<T, PreceptValue> FromClr { get; }
    public Func<PreceptValue, T> ToClr   { get; }
}
```

Registration is process-global via `PreceptRuntime.Register<T>(fromClr, toClr)`. The runtime-side metadata described here is target-state design documentation for the typed lane. Types without a typed registration remain JSON-lane-only at the runtime boundary.

**Naming context:** The abstract base methods (`ReadJson`, `WriteJson`, `ParseString`, `FormatString`) are the internal hot-path streaming API. These are distinct from the public-facing `PreceptValue` static methods (`PreceptValue.FromJson(JsonElement)` / `PreceptValue.ToJson()`) which are higher-level convenience conversions over `JsonElement`. The internal `TypeRuntime` delegates use `Utf8JsonReader`/`Utf8JsonWriter` for streaming efficiency; the public `PreceptValue` methods wrap `JsonElement`.

> **Resolved (CC#25):** The `BinaryExecutors` and `UnaryExecutors` arrays are `static readonly` instance properties on each `TypeRuntime<T>` — allocated once at type initialization, immortal. The builder fetches the appropriate delegate at compile time and embeds it directly in the `BinaryOp`/`UnaryOp` opcode. There is no global aggregation class. The evaluator calls the opcode's embedded delegate — it never indexes these arrays at evaluation time.

**Durable architecture rule:** Persistence and typed-lane conversion behavior belongs on catalog metadata — do not reintroduce per-`TypeKind` consumer switches in serializer or ingress code. The catalog entry IS the behavior.

##### TypeTrait flags enum

```csharp
[Flags]
public enum TypeTrait
{
    None               = 0,
    Orderable          = 1 << 0,
    EqualityComparable = 1 << 1,
}
```

Orderable types: `integer`, `decimal`, `number`, `date`, `time`, `instant`, `duration`, `datetime`, `money`, `quantity`, `price`. `period` is NOT orderable (ambiguous calendar arithmetic). `zoneddatetime` is NOT orderable (timezone-dependent comparison). `choice` is NOT orderable at the type level — orderable is field-level via the `ordered` modifier.

EqualityComparable types: all surfaced types except collections. The `EqualityComparable` trait is set on every type that supports `==` and `!=` operations in the Operations catalog.

##### TypeAccessor discriminated union

```csharp
public record TypeAccessor(
    string    Name,
    string    Description,
    TypeKind? ParameterType    = null,
    TypeTrait RequiredTraits   = TypeTrait.None,
    ProofRequirement[] ProofRequirements = []
);
```

`TypeAccessor` is a discriminated union: the base record plus two sealed subtypes — `FixedReturnAccessor` and `ElementParameterAccessor`. Both are defined after `QualifierShape` below.

```csharp
public enum QualifierAxis
{
    None,
    Currency,
    Unit,
    Dimension,
    FromCurrency,
    ToCurrency,
    Timezone,
    TemporalDimension,  // period of 'date' / period of 'time' (category)
    TemporalUnit,       // period in 'days' / period in 'months' (specific unit)
    PriceIn,            // polymorphic 'in' axis for price — value may be currency, UCUM unit, or compound currency/unit
}
```

`QualifierAxis` ships **10 members** (see `src/Precept/Language/Type.cs:39`). `PriceIn` is the polymorphic axis for `price`: the value may be an ISO 4217 currency code, a UCUM unit code, or a compound `currency/unit` expression. The type checker disambiguates at check time via `DeclaredQualifierMeta.CompoundPrice`.

##### QualifierShape — the in/of qualification system

```csharp
public sealed record QualifierSlot(TokenKind Preposition, QualifierAxis Axis);

public sealed record QualifierShape(
    IReadOnlyList<QualifierSlot> Slots,
    bool InOfExclusive = false,
    bool OfRequiresCurrencyIn = false
);
```

`QualifierShape` defines which qualifiers a type accepts. Each `QualifierSlot` pairs a preposition keyword (`in`, `of`, `to`) with a semantic axis. `InOfExclusive` declares whether `in` and `of` are mutually exclusive (only one can appear) or can coexist. `OfRequiresCurrencyIn` (used by `price`) declares that the `of` qualifier is valid only when `in` resolves to a currency-only value (not a unit or compound price); the type checker enforces this gating at check time. Source: `src/Precept/Language/Type.cs:77`.

Shared shapes in the Types catalog:

| Shape | Slots | InOfExclusive | Used by |
|-------|-------|---------------|----------|
| `QS_Currency` | `In(Currency)` | n/a (single slot) | `money` |
| `QS_UnitOrDimension` | `In(Unit)`, `Of(Dimension)` | true | `quantity` |
| `QS_TemporalUnitOrDimension` | `In(TemporalUnit)`, `Of(TemporalDimension)` | true | `period` |
| `QS_CurrencyAndDimension` | `In(Currency)`, `Of(Dimension)` | false | `price` |
| `QS_ExchangeRate` | `In(FromCurrency)`, `To(ToCurrency)` | n/a | `exchangerate` |

```csharp
public sealed record FixedReturnAccessor(
    string    Name,
    TypeKind  Returns,
    string    Description,
    bool      ReturnNonnegative  = false,
    TypeKind? ParameterType      = null,
    TypeTrait RequiredTraits     = TypeTrait.None,
    ProofRequirement[] ProofRequirements = [],
    QualifierAxis ReturnsQualifier = QualifierAxis.None
) : TypeAccessor(Name, Description, ParameterType, RequiredTraits, ProofRequirements);

public sealed record ElementParameterAccessor(
    string    Name,
    string    Description,
    TypeTrait RequiredTraits    = TypeTrait.None,
    ProofRequirement[]? ProofRequirements = null
) : TypeAccessor(Name, Description, null, RequiredTraits, ProofRequirements);
```

- `TypeAccessor` base = inner-type return (`.peek`, `.min`, `.max`). No `Returns` field — absence of a subtype is the declaration.
- `FixedReturnAccessor` = fixed return type (`.count`, `.currency`, `.amount`, `.inZone(tz)`). `Returns` declares the exact `TypeKind`. `ReturnNonnegative = true` marks accessors whose numeric result is structurally guaranteed ≥ 0 — the proof engine can discharge `accessor >= 0` obligations without a modifier proof. Currently set `true` on `Types.CollectionCountAccessor` (`.count`).
- `ElementParameterAccessor` = parameter resolves to the bag's element type (`bag.countof(x)`). No fixed `ParameterType` — the type checker resolves the parameter type against the owning field's element type at the call site. Always returns `integer`.
- `RequiredTraits` on base — checked against the collection's inner type for inner-type accessors, and against the owner type for fixed-return accessors.
- `ReturnsQualifier != None` means the accessor returns the qualifier value itself on the named axis — the result type carries the same qualifier as the owner field's qualifier on that axis. Examples: `.currency` on `money` → `ReturnsQualifier: QualifierAxis.Currency`; `.amount` on `money` → `ReturnsQualifier: QualifierAxis.None`. LS hover uses this to display "returns the currency of this field."

##### WidensTo — implicit widening

`TypeMeta.WidensTo` declares lossless implicit widening targets per type. The type checker uses these to allow narrower types where wider types are expected without explicit conversion. Declared per type in the exhaustive switch — no hardcoded widening logic in the type checker.

Only two widening edges in the language: `integer → [Decimal, Number]`. `decimal → number` is NOT implicit — requires `approximate()`. All other types have `WidensTo = []`.

#### 3. Functions (✅ Implemented)

The built-in function library. 21 functions defined in the language spec (§3.7).

| Part | Type |
|------|------|
| Kind enum | `FunctionKind` (21 members) |
| Meta record | `FunctionMeta(Kind, Name, Description, Overloads[], Category, UsageExample?, SnippetTemplate?, HoverDescription?, HasCIVariant, CIVariantOf, IsMessagePosition)` — `FunctionOverload` uses `ParameterMeta[]` (see below) |
| Catalog class | `Functions` — `GetMeta()`, `All` |
| Output type | None — functions are evaluated inline |

**Members (from `precept-language-spec.md` §3.7):**

| Category | Members |
|----------|---------|
| Numeric | `Min`, `Max`, `Abs`, `Clamp`, `Floor`, `Ceil`, `Truncate`, `Round`, `RoundPlaces`, `Approximate`, `Pow`, `Sqrt` |
| String | `Trim`, `StartsWith`, `EndsWith`, `ToLower`, `ToUpper`, `Left`, `Right`, `Mid` |
| Temporal | `Now` |

(`Round` and `RoundPlaces` are listed separately because they are distinct overloads with different return types: `round(value) → integer` vs `round(value, places) → decimal`.)

**Consumers:** MCP vocabulary, LS completions (function names in expression context), LS hover (function documentation), type checker (overload resolution, CI-qualified variant resolution, and argument validation), evaluator (runtime dispatch).

**Rationale:** The function catalog serves the most consumers of any planned catalog. The evaluation delegate eliminates a parallel copy in the evaluator — the evaluator dispatches through the catalog rather than maintaining its own function switch.

##### ParameterMeta and FunctionOverload shapes

```csharp
public sealed record ParameterMeta(TypeKind Kind, string? Name = null);

public sealed record FunctionOverload(
    IReadOnlyList<ParameterMeta> Parameters,
    TypeKind                     ReturnType,
    QualifierMatch?              QualifierMatch    = null,
    ProofRequirement[]           ProofRequirements = [],
    bool                         ReturnNonnegative = false
);

public sealed record FunctionMeta(
    FunctionKind                    Kind,
    string                          Name,
    string                          Description,
    IReadOnlyList<FunctionOverload> Overloads,
    FunctionCategory                Category,
    string?                         UsageExample = null,
    string?                         SnippetTemplate = null,
    string?                         HoverDescription = null,
    bool                            HasCIVariant = false,
    FunctionKind?                   CIVariantOf = null,
    bool                            IsMessagePosition = false
);
```

Parameters are declared as named statics so `ParamSubject` can reference them by object identity — see Proof Obligations.

`FunctionOverload.ReturnNonnegative = true` tells the proof engine's Strategy 2 that this overload's return value is structurally guaranteed ≥ 0, allowing it to discharge `count >= 0` obligations without a modifier proof. Currently set `true` on all five `abs` overloads (integer, decimal, number, money, quantity).

Note: `FunctionMeta.Name` stays as a `string` because functions are identifiers (`min`, `round`), not keyword tokens. There is no `TokenKind` for functions. `FunctionCategory` groups functions by semantic domain (`Numeric`, `String`, `Temporal`) for completions and MCP vocabulary presentation. `HasCIVariant` marks the canonical case-sensitive function that has a CI-qualified partner, and `CIVariantOf` points from the CI-qualified function back to its canonical base. `IsMessagePosition` reserves a catalog-driven way to mark functions whose trailing argument is a user-facing message string; no built-in functions populate it yet. The type checker reads these fields when resolving CI-qualified function calls such as `~startsWith(...)`. 

##### Business-type overloads and QualifierMatch

Functions like `abs`, `min`, `max`, `clamp`, `round` have overloads for `money` and `quantity` in addition to numeric types. `FunctionOverload.QualifierMatch` declares how the result's qualifier is derived from the operands' qualifiers. Most overloads use `null` (no qualifier reasoning needed). Business-type overloads use `QualifierMatch.Same` (result carries the same qualifier as the input).

Note: `QualifierMatch` on `FunctionOverload` is `QualifierMatch?` — `null` means no qualifier reasoning needed, distinct from the `Any/Same/Different` values used by `OperationMeta`.

`ParameterMeta` is shared between `FunctionOverload.Parameters` and `BinaryOperationMeta.Lhs`/`Rhs` — see Proof Obligations for why it uses object references rather than `TypeKind`.

#### 4. Operators (✅ Implemented)

Operator symbols — the `+`, `-`, `*`, `/`, `==`, etc. Each member is an operator symbol with its own metadata.

| Part | Type |
|------|------|
| Kind enum | `OperatorKind` (21 members: `Or`, `And`, `Not`, `Equals`, `NotEquals`, `CaseInsensitiveEquals`, `CaseInsensitiveNotEquals`, `LessThan`, `GreaterThan`, `LessThanOrEqual`, `GreaterThanOrEqual`, `Contains`, `Plus`, `Minus`, `Times`, `Divide`, `Modulo`, `Negate`, `IsSet`, `IsNotSet`, `LookupAccess`) |
| Meta record | `OperatorMeta(Kind, Description, Arity, Associativity, Precedence, Family, IsKeywordOperator, ResultType: TypeKind?, ResultTypePolicy: ResultTypePolicy, HoverDescription?, UsageExample?)` — token shape lives on the `SingleTokenOp(Token, …)` and `MultiTokenOp(Tokens, …)` sealed subtypes |
| Supporting enums | `Arity { Unary, Binary, Postfix }`, `OperatorFamily { Arithmetic, Comparison, Logical, Membership, Presence }`, `Associativity { Left, Right, NonAssociative }` |
| Catalog class | `Operators` — `GetMeta()`, `All` |
| Output type | None |

**Consumers:** MCP vocabulary (operator symbols via `Token.Text` and descriptions), LS hover (operator documentation), TextMate grammar (operator alternations), parser (precedence and associativity).

**Rationale:** `BinaryOp` and `UnaryOp` are currently bare parser-internal enums. The Operators catalog promotes them to first-class language surface with per-member metadata — symbol text, human-readable description, precedence, associativity. Consumers no longer need hardcoded operator lists.

##### ResultTypePolicy

```csharp
public enum ResultTypePolicy
{
    Fixed = 1,            // result type is OperatorMeta.ResultType
    LhsType = 2,          // result type is the resolved left/only operand type
    ElementType = 3,      // result type is the resolved element/value type of the left collection operand
    BothOperands = 4,     // operands must agree on OperatorMeta.ResultType; result is that type
    OperationResult = 5,  // result type comes from the resolved Operations catalog entry
}
```

Assignment rules:
- Logical binary operators (`or`, `and`) → `BothOperands`, `ResultType: TypeKind.Boolean`
- Logical unary `not`, all comparison operators (`==`, `!=`, `<`, `>`, `<=`, `>=`, `~==`, `~!=`), `contains`, `is set`, `is not set` → `Fixed`, `ResultType: TypeKind.Boolean`
- Unary `-` (`Negate`) → `LhsType`, `ResultType: null`
- Binary arithmetic operators (`+`, `-`, `*`, `/`, `%`) → `OperationResult`, `ResultType: null`
- `for` (`LookupAccess`) → `ElementType`, `ResultType: null`

The type checker reads `OperatorMeta.ResultType` and `OperatorMeta.ResultTypePolicy` to choose its result-type resolution path — no per-operator switch in the type-checker.

#### 5. Operations (✅ Implemented)

Typed operator combinations — each member is one legal `(operator, lhs TypeKind, rhs TypeKind) → result TypeKind` triple. The catalog is the **source of truth** for what the language can do with specific type combinations — every consumer (type checker, doc generation, MCP, LS hover, AI grounding) derives from these entries.

| Part | Type |
|------|------|
| Kind enum | `OperationKind` (~200 members: `NumberPlusNumber`, `MoneyPlusMoney`, `DatePlusPeriod`, `MoneyTimesDecimal`, `MoneyDivideMoneySameCurrency`, `MoneyDivideMoneyCrossCurrency`, ...) |
| Meta record | `OperationMeta` — abstract DU with `UnaryOperationMeta` and `BinaryOperationMeta` sealed subtypes (see below) |
| Discriminator enum | `QualifierMatch { Any, Same, Different }` |
| Catalog class | `Operations` — `GetMeta()`, `All`, `FindCandidates(OperatorKind, TypeKind, TypeKind) → ReadOnlySpan<BinaryOperationMeta>`, `Resolve(OperatorKind, TypeKind, TypeKind) → BinaryOperationMeta?`, `DisambiguateCandidates(ReadOnlySpan<BinaryOperationMeta>) → BinaryOperationMeta?`, `FindUnary(OperatorKind, TypeKind) → UnaryOperationMeta?` |
| Output type | None |

##### OperationMeta discriminated union

```csharp
public abstract record OperationMeta(
    OperationKind Kind,
    OperatorKind  Op,
    TypeKind      Result,
    string        Description
);

public sealed record UnaryOperationMeta(
    OperationKind Kind,
    OperatorKind  Op,
    ParameterMeta Operand,
    TypeKind      Result,
    string        Description
) : OperationMeta(Kind, Op, Result, Description);

public sealed record BinaryOperationMeta(
    OperationKind         Kind,
    OperatorKind          Op,
    ParameterMeta         Lhs,
    ParameterMeta         Rhs,
    TypeKind              Result,
    string                Description,
    bool                  BidirectionalLookup    = false,
    QualifierMatch        Match                  = QualifierMatch.Any,
    ProofRequirement[]?   ProofRequirements      = null,
    bool                  HasCIVariant           = false,
    DiagnosticCode?       CIDiagnosticCode       = null,
    ResultQualifierPolicy ResultQualifierPolicy  = ResultQualifierPolicy.None
) : OperationMeta(Kind, Op, Result, Description)
{
    /// <summary>Interval transfer function for compile-time overflow analysis (init-only).</summary>
    public IntervalTransferFn? IntervalTransfer { get; init; }
}
```

Source: `src/Precept/Language/Operation.cs:76`. Three additional fields beyond the original (F-LANG-CAT-17):

- **`HasCIVariant : bool`** — marks the canonical case-sensitive operation that has a CI-qualified partner; consumed by `ValidateCIEnforcement` during type checking.
- **`CIDiagnosticCode : DiagnosticCode?`** — catalog-mediated diagnostic for CI-misuse on this operation. The Gate 1 emission scanner treats this as a valid catalog-mediated emission site (see Roslyn Enforcement Layer § Gate 1).
- **`ResultQualifierPolicy : ResultQualifierPolicy`** — declares how the result's qualifier identity is derived when the operation produces structured qualifier identity beyond raw `QualifierMatch`. Enum (`src/Precept/Language/Operation.cs:28`): `None`, `CompoundUnitCancellation`, `InheritFromQualifiedOperand` (e.g. `money × decimal → money` with same qualifier), `CurrencyConversion` (e.g. `ExchangeRateTimesMoney` — result currency is the rate's ToCurrency), `CompoundDimensionElevation` (e.g. `PriceDivideQuantity` — currency inherited from price, unit dimension elevated from compound-quantity numerator).
- **`IntervalTransfer : IntervalTransferFn?`** (init-only) — optional delegate transferring numeric intervals through the operation for compile-time overflow analysis. The unary subtype carries an analogous `UnaryIntervalTransferFn?` slot.

`BidirectionalLookup = true` marks operations where `(op, lhs, rhs)` and `(op, rhs, lhs)` are the same entry — the index registers both key orderings so type-checking commutative operations doesn't require two entries (e.g., `money * decimal` and `decimal * money`).

`Operations.All` is `IReadOnlyList<OperationMeta>`. Two internal indexes: `FrozenDictionary<(OperatorKind, TypeKind), UnaryOperationMeta>` keyed by `(Op, Operand.Kind)`, and `FrozenDictionary<(OperatorKind, TypeKind, TypeKind), BinaryOperationMeta[]>` keyed by `(Op, Lhs.Kind, Rhs.Kind)`. The index key uses `ParameterMeta.Kind` for the `TypeKind` component.

`Lhs`, `Rhs`, and `Operand` are `ParameterMeta` (not `TypeKind`) so `ParamSubject` can hold a direct reference to the instance — see Proof Obligations.

**Unary operations (9 total):** `-integer`, `-decimal`, `-number`, `-money`, `-quantity`, `-price`, `-exchangerate`, `-duration`, `not boolean`. Result type is always the same as the operand type.

##### QualifierMatch — conditional result types

Two operations in the language produce different result types depending on whether the operands' qualifiers match:

| Operation | Same qualifier | Different qualifier |
|-----------|---------------|---------------------|
| `money / money` | `decimal` (dimensionless ratio) | `exchangerate` (currency pair) |
| `quantity / quantity` | `decimal` (dimensionless ratio) | `quantity` (compound unit) |

Rather than hiding this branching in the type checker, the catalog declares it explicitly via the `QualifierMatch` discriminator:

```csharp
enum QualifierMatch { Any, Same, Different }
```

- `Any` — default. No qualifier inspection needed. Used by ~95% of entries.
- `Same` — entry applies when operand qualifiers are equal (same currency, same dimension).
- `Different` — entry applies when operand qualifiers differ.

The four entries that use it:

```csharp
new(MoneyDivideMoneySameCurrency,      Divide, Money,    Money,    Decimal,      "Same-currency ratio",       Same),
new(MoneyDivideMoneyCrossCurrency,     Divide, Money,    Money,    ExchangeRate, "Cross-currency derivation", Different),
new(QuantityDivideQuantitySameDim,     Divide, Quantity, Quantity, Decimal,      "Same-dimension ratio",      Same),
new(QuantityDivideQuantityCrossDim,    Divide, Quantity, Quantity, Quantity,     "Compound unit derivation",  Different),
```

Every other entry has `Match = QualifierMatch.Any` and stands alone for its `(Op, Lhs, Rhs)` triple.

This keeps the catalog as the single source of truth: doc generation, MCP output, and the type checker all derive from the same entries. A doc generator groups by `(Op, Lhs, Rhs)`, detects multi-entry groups, and labels them by `Match`. An AI consumer sees both entries with their `qualifierMatch` discriminator and understands the branching without reading source code.

##### Resolution

The internal index groups entries by `(Op, Lhs TypeKind, Rhs TypeKind)`. Most triples have one entry; the two branching operations (`MoneyDivideMoney`, `QuantityDivideQuantity`) have two — one for `QualifierMatch.Same`, one for `QualifierMatch.Different`.

`FindCandidates` returns all entries for a given triple — the raw catalog data, usable by doc generators and MCP serialization.

`Resolve` is the type-checker-facing method. It returns a single `BinaryOperationMeta?` by composing `FindCandidates` with `DisambiguateCandidates`:

```csharp
public static class Operations
{
    public static ReadOnlySpan<BinaryOperationMeta> FindCandidates(
        OperatorKind op, TypeKind lhs, TypeKind rhs) =>
        BinaryIndex.TryGetValue((op, lhs, rhs), out var entries)
            ? entries.AsSpan()
            : ReadOnlySpan<BinaryOperationMeta>.Empty;

    public static BinaryOperationMeta? Resolve(OperatorKind op, TypeKind lhs, TypeKind rhs)
        => DisambiguateCandidates(FindCandidates(op, lhs, rhs));

    public static BinaryOperationMeta? DisambiguateCandidates(
        ReadOnlySpan<BinaryOperationMeta> candidates)
    {
        if (candidates.Length == 0) return null;
        if (candidates.Length == 1) return candidates[0];

        // Multi-candidate: default to QualifierMatch.Same — the structurally safe assumption.
        // ProofEngine adds obligations to verify qualifier compatibility at deeper analysis.
        foreach (var c in candidates)
            if (c.Match == QualifierMatch.Same) return c;
        return candidates[0];
    }
}
```

**Shipped surface (2026-05-25, F-LANG-CAT-15 Decision 4):** the simple `(TypeKind, TypeKind)` signature. The richer `Resolve(OperatorKind, Type, Type) → OperationMeta?` design that took full `Type` objects with qualifier-equality dispatch via `Type.QualifierEquals` was deliberately deferred — it requires a `Type` (qualified) vs `TypeKind` (unqualified) distinction that does not exist in the current type representation. The shipped wrapper defaults to `QualifierMatch.Same` for multi-candidate dispatch and lets the proof engine verify qualifier compatibility via `ProofRequirementKind.QualifierCompatibility` obligations. See `OperationsTests.Resolve_*` for the contract.

**Future:** when qualifier-aware `Type` objects ship (Phase 4 / Phase 5 with the qualified-inner-types work), `Resolve` can grow a `(Type, Type)` overload that performs static qualifier dispatch. The proof-obligation pathway remains in place as the fallback for runtime-determined qualifiers.

**Consumers:** Type checker (legal combinations and result types via `Resolve`), doc generation (complete operation table including conditional branches), MCP vocabulary ("what operations are legal and what do they produce?"), LS hover (per-combination documentation), evaluator dispatch, AI grounding (full operation surface).

**Relationship to Operators catalog:** Each `OperationMeta` references an `OperatorKind`. You can query "what operations use `Plus`?" by filtering `Operations.All` where `Op == OperatorKind.Plus`. The Operators catalog describes the symbols; the Operations catalog describes what those symbols can do with specific types.

#### 6. Modifiers (✅ Implemented)

All declaration-attached modifiers across the language surface — field constraints, state lifecycle modifiers, event modifiers, access modes, and ensure/action anchors. The Modifiers catalog uses a **discriminated union with 5 sealed subtypes**, each carrying exactly the metadata its consumers need.

| Part | Type |
|------|------|
| Kind enum | `ModifierKind` (29 members across 5 subtypes) |
| Meta record | `ModifierMeta` — abstract DU base with `ValueModifierMeta`, `StateModifierMeta`, `EventModifierMeta`, `AccessModifierMeta`, `AnchorModifierMeta` sealed subtypes (see below) |
| Supporting enums | `ModifierCategory`, `GraphAnalysisKind`, `AnchorScope`, `AnchorTarget` |
| Catalog class | `Modifiers` — `GetMeta()`, `All` |
| Output type | None |

**Members by subtype:**

| Subtype | Members | Count |
|---------|---------|-------|
| `ValueModifierMeta` | `optional`, `writable`, `default`, `nonnegative`, `positive`, `nonzero`, `notempty`, `min`, `max`, `minlength`, `maxlength`, `mincount`, `maxcount`, `maxplaces`, `ordered` | 15 |
| `StateModifierMeta` | `initial` (state), `terminal`, `required`, `irreversible`, `success`, `warning`, `error` | 7 |
| `EventModifierMeta` | `initial` (event) | 1 |
| `AccessModifierMeta` | `editable` (Write), `readonly` (Read), `omit` (Omit) | 3 |
| `AnchorModifierMeta` | `in`, `to`, `from` | 3 (with `AnchorTarget` disambiguating ensure vs state-action) |

**Consumers:** MCP vocabulary, LS completions (modifier names after type, state modifiers in state declarations, access modes in state blocks), LS hover, type checker (modifier applicability per TypeKind, access mode enforcement, state modifier graph analysis), graph analyzer (structural modifier validation).

**Rationale:** The language vision (archived at `docs/archive/language-design/precept-language-vision.md` §Modifier System Expansion) defines modifiers across 5 declaration surfaces — fields, states, events, rules, and potentially the precept itself — with 3 modifier categories (structural, semantic, severity). A flat `ModifierMeta` record would carry many inapplicable fields per subtype (e.g., `ApplicableTo` is meaningless for state modifiers; `AllowsOutgoing` is meaningless for field modifiers). The DU ensures each subtype carries exactly its consumers' metadata, and the C# type system prevents accessing inapplicable fields.

The DU also absorbs 4 bare enums (`StateModifierKind`, `AccessMode`, `EnsureAnchor`, `StateActionAnchor`) that were previously classified as internal classification axes. With proper subtypes, these are now first-class language surface members in `Modifiers.All`.

##### ModifierMeta — discriminated union

```csharp
// ── Base ──────────────────────────────────────────────────
// Source: src/Precept/Language/Modifier.cs:98
public abstract record ModifierMeta(
    ModifierKind      Kind,
    TokenMeta         Token,                          // object reference to Tokens catalog entry
    string            Description,
    ModifierCategory  Category,                       // Structural, Semantic, Severity
    bool              DesugarsToRule        = false, // when true, modifier is syntactic sugar for a rule construct
                                                      // and is highlighted in the message-position gold color
    ModifierKind[]?   MutuallyExclusiveWith = null    // at most one of the group may appear on a declaration
);

// ── Value modifiers (15) ─────────────────────────────────
public sealed record ValueModifierMeta(
    ModifierKind     Kind,
    TokenMeta        Token,
    string           Description,
    ModifierCategory Category,
    TypeTarget[]     ApplicableTo,
    bool             HasValue          = false,
    ValueModifierDeclarationSite ApplicableDeclarationSites =
        ValueModifierDeclarationSite.FieldDeclaration | ValueModifierDeclarationSite.EventArgDeclaration,
    ModifierKind?    BoundCounterpart  = null,  // the opposing bound modifier: Min↔Max, Minlength↔Maxlength, Mincount↔Maxcount
    ModifierKind[]   Subsumes          = [],
    ProofSatisfaction[] ProofSatisfactions = [],  // Strategy 2 proof discharge table for this modifier
    string?          HoverDescription  = null,
    string?          UsageExample      = null,
    string?          SnippetTemplate   = null,
    bool             DesugarsToRule    = false,  // inherited from ModifierMeta base
    ModifierKind[]?  MutuallyExclusiveWith = null
) : ModifierMeta(Kind, Token, Description, Category, DesugarsToRule, MutuallyExclusiveWith);

/// <summary>
/// A positive carrier fact that can satisfy a <see cref="ProofRequirement"/>.
/// Source: src/Precept/Language/ProofRequirement.cs:280
/// </summary>
public abstract record ProofSatisfaction(ProofRequirementKind RequirementKind)
{
    public sealed record Numeric(
        SatisfactionProjection Projection,
        OperatorKind           Comparison,
        NumericBoundSource     Bound)
        : ProofSatisfaction(ProofRequirementKind.Numeric);
    public sealed record Presence()                       : ProofSatisfaction(ProofRequirementKind.Presence);
    public sealed record Dimension(DimensionSource Source) : ProofSatisfaction(ProofRequirementKind.Dimension);
    public sealed record Modifier(ModifierKind RequiredModifier) : ProofSatisfaction(ProofRequirementKind.Modifier);
    public sealed record QualifierCompatibility(QualifierAxis Axis) : ProofSatisfaction(ProofRequirementKind.QualifierCompatibility);
    public sealed record IntervalContainment()            : ProofSatisfaction(ProofRequirementKind.IntervalContainment);
    public sealed record LengthContainment()              : ProofSatisfaction(ProofRequirementKind.LengthContainment);
    public sealed record CountContainment()               : ProofSatisfaction(ProofRequirementKind.CountContainment);
    public sealed record KeyPresence(bool Negated)        : ProofSatisfaction(ProofRequirementKind.KeyPresence);
}

// Supporting DUs (source: same file):
public abstract record SatisfactionProjection
{
    public sealed record SelfValue() : SatisfactionProjection;
    public sealed record Accessor(string Name) : SatisfactionProjection;
}
public abstract record NumericBoundSource
{
    public sealed record Constant(decimal Value) : NumericBoundSource;
    public sealed record DeclarationValue() : NumericBoundSource;
}
public abstract record DimensionSource
{
    public sealed record Constant(PeriodDimension Value) : DimensionSource;
    public sealed record DeclaredTemporalDimension() : DimensionSource;
}

// ── State modifiers (7) ─────────────────────────────────
public sealed record StateModifierMeta(
    ModifierKind     Kind,
    TokenMeta        Token,
    string           Description,
    ModifierCategory Category,
    bool             AllowsOutgoing    = true,   // terminal = false
    bool             RequiresDominator  = false,  // required = true
    bool             PreventsBackEdge   = false   // irreversible = true
) : ModifierMeta(Kind, Token, Description, Category);

// ── Event modifiers (1) ─────────────────────────────────
public sealed record EventModifierMeta(
    ModifierKind       Kind,
    TokenMeta          Token,
    string             Description,
    ModifierCategory   Category,
    GraphAnalysisKind  RequiredAnalysis = GraphAnalysisKind.None
) : ModifierMeta(Kind, Token, Description, Category);

// ── Access modes (3: editable/Write, readonly/Read, omit/Omit) ──
// Note: `modify` is the construct verb, not a ModifierKind member.
public sealed record AccessModifierMeta(
    ModifierKind     Kind,
    TokenMeta        Token,
    string           Description,
    ModifierCategory Category,
    bool             IsPresent    = true,    // false = omit (structurally absent)
    bool             IsWritable   = true     // false = read-only
) : ModifierMeta(Kind, Token, Description, Category);
// modify + editable: IsPresent=true,  IsWritable=true
// modify + readonly: IsPresent=true,  IsWritable=false
// omit:              IsPresent=false, IsWritable=false

// ── Ensure/action anchors (in, to, from) ────────────────
public sealed record AnchorModifierMeta(
    ModifierKind     Kind,
    TokenMeta        Token,
    string           Description,
    ModifierCategory Category,
    AnchorScope      Scope,          // InState, OnEntry, OnExit
    AnchorTarget     Target          // Ensure, StateAction
) : ModifierMeta(Kind, Token, Description, Category);
```

`Token` replaces the `string Keyword` field — consumers access keyword text via `modifier.Token.Text`. `MutuallyExclusiveWith` declares modifier exclusion groups on the base; consumers (type checker, LS) enforce the constraint without hardcoding group membership. `DesugarsToRule = true` marks modifiers that desugar to rule constructs (numeric bound modifiers) and should be highlighted in the same gold color as message-position keywords. `BoundCounterpart` links the six min/max bound pairs: `Min ↔ Max`, `Minlength ↔ Maxlength`, `Mincount ↔ Maxcount`. `ApplicableDeclarationSites` restricts where the modifier may appear: `Writable` carries `FieldDeclaration` only; all other value modifiers default to `FieldDeclaration | EventArgDeclaration`. Modifiers with no runtime validation (e.g., `ordered` is compile-time only) have no inline delegate — execution is handled by the evaluator's pass over `ValueModifierMeta.ApplicableTo` entries.

##### ModifierCategory

Three categories from the language vision (issues #58 and #86):

```csharp
public enum ModifierCategory { Structural, Semantic, Severity }
```

- **Structural:** Compile-time-provable properties — lifecycle shape, one-write behavior, entry behavior, terminality. Requires graph analysis for validation.
- **Semantic:** Intent and tooling meaning — success, error, sensitive, audit, deprecated. No graph analysis needed.
- **Severity:** Language-level control over how declarations surface as warnings vs hard invariants.

##### GraphAnalysisKind (for EventModifierMeta)

Maps each event modifier to the graph reasoning the compiler must perform. `GraphAnalysisKind` is the enum the catalog uses to drive that analysis; only the analysis kinds invoked by **shipping** event modifiers are surfaced today (F-LANG-CAT-19).

```csharp
public enum GraphAnalysisKind { None, IncomingEdge, OutcomeType, Reachability, InitialEventCompatibility }
```

**Shipping event modifier (1 — catalog member):**

| Event modifier | `ModifierKind` | GraphAnalysisKind | What the compiler checks |
|---|---|---|---|
| `initial` (event) | `InitialEvent` | `InitialEventCompatibility` | Every transition triggered by the initial event targets the state marked `initial` |

The remaining graph-property event modifiers (`entry`, `advancing`, `settling`, `completing`, `absorbing`, `guarded`, `isolated`, `universal`, and the state-side `sealed after` / `writeonce`) are **deferred to Compiler 1.1** per the graph-analyzer roadmap — they are not catalog members today and are not enforced by the type checker. The `GraphAnalysisKind` enum carries `IncomingEdge`, `OutcomeType`, and `Reachability` as forward-compatible analysis hooks so that adding any of these modifiers as catalog members later is purely additive. See **`docs/language/graph-analyzer-roadmap.md`** for the full deferred-modifier inventory, the graph-property semantics each one would assert, and the sequencing decision (none require runtime support — every modifier is a pure compile-time graph property; metadata exposure on descriptors is additive when they ship).

##### AnchorScope and AnchorTarget

```csharp
public enum AnchorScope  { InState, OnEntry, OnExit }
public enum AnchorTarget { Ensure, StateAction }
```

Anchors (`in`, `to`, `from`) appear in both ensure and state-action contexts. 3 `ModifierKind` values (`In`, `To`, `From`) with `AnchorTarget` disambiguating ensure vs state-action.

##### `initial` keyword resolution

The keyword `initial` appears on both states and events with different semantics. Resolution: two `ModifierKind` values — `InitialState` and `InitialEvent` — same keyword text `"initial"`, different subtypes (`StateModifierMeta` vs `EventModifierMeta`), different metadata.

##### Field modifier applicability

The applicability matrix is currently validated by ad-hoc logic in the type checker. The catalog makes it explicit: `nonnegative`, `nonzero`, and `positive` apply to `Integer`, `Decimal`, `Number`, `Money`, `Quantity`, `Price`, `ExchangeRate`, `Duration`, and `Period` (the `ZeroBoundNumericTypes` set — F-LANG-TEMP-03 added Duration and Period); `notempty` applies to `String`, `Set`, `Queue`, `Stack`, `Log`, `Bag`, `List`, `Queue of T by P` (on collections it is equivalent to `mincount 1`); `mincount`/`maxcount` apply to `Set`, `Queue`, `Stack`; `maxplaces` applies to `Decimal`, `Money`, `Quantity`, `Price`, and `ExchangeRate`; `ordered` applies to `Choice` only. The `HasValue` flag distinguishes value-carrying modifiers (`min 0`) from bare flags (`nonnegative`). `ApplicableTo` uses `TypeTarget[]` (see Supporting Types) for modifier-sensitive applicability.

Field modifiers also apply in event arg positions (e.g., `event Submit(amount: money nonnegative)`). The modifier catalog declares type-level applicability; the *position* where a modifier can appear (field declaration vs event arg) is a parser/construct-level concern handled by the Constructs catalog.

##### Modifier subsumption

`Subsumes` declares which weaker modifiers this modifier makes redundant. When a field has `positive`, it already implies `nonzero` and `nonnegative` — these need not be declared. The type checker uses `Subsumes` to detect redundant modifier declarations and emit a diagnostic. Roslyn analyzer enforces that `Subsumes` entries are always drawn from the correct subsumption chain (a modifier cannot claim to subsume something it doesn't structurally imply).

Static relationships: `positive.Subsumes = [Nonnegative, Nonzero]`. All other modifiers: `Subsumes = []`.

##### `Subsumes` vs `MutuallyExclusiveWith` — layered concerns

`Subsumes` and `MutuallyExclusiveWith` operate at distinct layers and can co-exist on the same modifier pair:

- **`Subsumes`** is **meaning-level**. It drives proof-obligation discharge (a stronger modifier discharges the obligation a weaker subsumed modifier would carry) and surface-level `RedundantModifier` reporting when both are declared.
- **`MutuallyExclusiveWith`** is **syntax-level**. It drives the `ConflictingModifiers` error when both are declared, regardless of subsumption relationship.

These are not redundant. A modifier pair like `nonnegative + positive` exercises both: `positive.Subsumes = [Nonnegative, Nonzero]` declares the meaning relationship, while `nonnegative.MutuallyExclusiveWith = [Positive]` declares the surface-syntax rule. Both fire by design — the author wrote two structural modifiers when one was sufficient, and that's worth surfacing as an error rather than silently accepting the redundancy. See F-LANG-PRIM-04 in `docs/Working/compiler-readiness-plan-2026-05-24.md § Resolved for Phase 3` for the rationale.

##### Implied modifiers (TypeMeta.ImpliedModifiers)

Some types carry implied modifiers intrinsically. `currency` and `unitofmeasure` fields are always `notempty`. These are declared on `TypeMeta.ImpliedModifiers` — the type checker merges them with declared modifiers before validation. Roslyn analyzer rule: every entry in `ImpliedModifiers` must be present in the subsumption chain of at least one existing `ModifierMeta.Subsumes` entry — prevents phantom modifier implications.

##### State modifier graph analysis

State modifiers that are structural (`terminal`, `required`, `irreversible`) require graph analysis at compile time. The `StateModifierMeta` boolean fields declare the graph property each modifier asserts:

- `AllowsOutgoing = false` → terminal. Compiler validates no outgoing transition rows exist.
- `RequiresDominator = true` → required. Compiler performs dominator analysis (Lengauer-Tarjan, O(V+E)) to verify all initial→terminal paths visit this state.
- `PreventsBackEdge = true` → irreversible. Compiler performs reverse-reachability to verify no path from this state back to any ancestor in the initial→forward ordering.

Semantic state modifiers (`success`, `warning`, `error`) require no graph analysis — they are intent declarations for tooling and documentation.

#### 7. Actions (✅ Implemented)

State-machine action verbs — the keywords that appear after `->` in transition rows and state action hooks.

| Part | Type |
|------|------|
| Kind enum | `ActionKind` (15 members) |
| Meta record | `ActionMeta(Kind, Token, Description, ApplicableTo TypeTarget[], SyntaxShape ActionSyntaxShape, ValueRequired bool, ProofRequirements[]?, AllowedIn ConstructKind[]?, HoverDescription?, UsageExample?, SnippetTemplate?, PrimaryActionKind ActionKind?, DynamicObligationGenerator?)` — `Token` is a `TokenMeta` object reference; `DynamicObligationGenerator` is an optional context-aware proof-obligation generator (F-LANG-CAT-24); see full shape below |
| Catalog class | `Actions` — `GetMeta()`, `All` |
| Output type | None |

> **✅ Settled (Wave 4 Gap 6):** `ActionMeta.Description` is the canonical hover/MCP documentation field and is present in the `ActionMeta` shape above. `SyntaxShape` (`ActionSyntaxShape`) is a first-class `ActionMeta` field that encodes the action's operand grammar shape. `SnippetTemplate` is a deferred implementation milestone.

**Members (from `ActionKind.cs`):**

| Action | ApplicableTo (`TypeTarget[]`) | Value | Into | AllowedIn | SyntaxShape |
|--------|-------------------------------|-------|------|----------|-------------|
| `set` | any `TypeKind` (empty = caller validates) | required (`= Expr`) | no | all action contexts | `AssignValue` |
| `add` | `[TypeTarget(Set), TypeTarget(Bag)]` | required (`Expr`) | no | all action contexts | `CollectionValue` |
| `remove` | `[TypeTarget(Set), TypeTarget(Bag), TypeTarget(List), TypeTarget(Lookup)]` | required (`Expr`) | no | all action contexts | `CollectionValue` |
| `enqueue` | `[TypeTarget(Queue)]` | required (`Expr`) | no | all action contexts | `CollectionValue` |
| `dequeue` | `[TypeTarget(Queue), TypeTarget(QueueBy)]` | no value | yes (`into Field`) | all action contexts | `CollectionInto` |
| `push` | `[TypeTarget(Stack)]` | required (`Expr`) | no | all action contexts | `CollectionValue` |
| `pop` | `[TypeTarget(Stack)]` | no value | yes (`into Field`) | all action contexts | `CollectionInto` |
| `clear` | `[TypeTarget(Set), TypeTarget(Queue), TypeTarget(Stack), TypeTarget(Bag), TypeTarget(List), TypeTarget(QueueBy), ModifiedTypeTarget(null, [Optional])]` | no value | no | all action contexts | `FieldOnly` |
| `append` | `[TypeTarget(Log), TypeTarget(List)]` | required (`Expr`) | no | all action contexts | `CollectionValue` |
| `append ... by` | `[TypeTarget(LogBy)]` | required (`Expr by Expr`) | no | all action contexts | `CollectionValueBy` |
| `insert` | `[TypeTarget(List)]` | required (`Expr at Expr`) | no | all action contexts | `InsertAt` |
| `remove ... at` | `[TypeTarget(List)]` | no value | no | all action contexts | `RemoveAtIndex` |
| `put` | `[TypeTarget(Lookup)]` | required (`Key = Value`) | no | all action contexts | `PutKeyValue` |
| `enqueue ... by` | `[TypeTarget(QueueBy)]` | required (`Expr by Expr`) | no | all action contexts | `CollectionValueBy` |
| `dequeue ... by` | `[TypeTarget(QueueBy)]` | no value | yes (`into Field by Field`) | all action contexts | `CollectionIntoBy` |

"All action contexts" means `[ConstructKind.EventDeclaration, ConstructKind.StateAction, ConstructKind.TransitionRow]`. `clear` on optional scalars: `ModifiedTypeTarget(Kind: null, RequiredModifiers: [ModifierKind.Optional])` — matches any field with the `Optional` modifier, regardless of type kind. Actions with `ProofRequirements` (`dequeue`, `pop`, `insert`, `remove ... at`, `dequeue ... by`) require emptiness/bounds proofs. Actions with `PrimaryActionKind` (`append ... by`, `remove ... at`, `enqueue ... by`, `dequeue ... by`) share a keyword token with the primary action; disambiguation is by target type.

##### ActionMeta — full shape

```csharp
// Source: src/Precept/Language/Action.cs:11
public sealed record ActionMeta(
    ActionKind          Kind,
    TokenMeta           Token,             // object reference to Tokens catalog entry
    string              Description,
    TypeTarget[]        ApplicableTo,
    ActionSyntaxShape   SyntaxShape,
    bool                ValueRequired      = false,
    ProofRequirement[]? ProofRequirements  = null,
    ConstructKind[]?    AllowedIn          = null,
    string?             HoverDescription   = null,
    string?             UsageExample       = null,
    string?             SnippetTemplate    = null,
    ActionKind?         PrimaryActionKind  = null,   // non-null for secondary-dispatch actions sharing a token
    Func<TypedAction, SemanticIndex, ImmutableArray<ProofObligation>>? DynamicObligationGenerator = null
);
```

Consumers access the action keyword text via `action.Token.Text`. `ActionMeta.SyntaxShape` encodes the action's operand form — the parser reads `Actions.GetShapeMeta(SyntaxShape)` to get an `ActionShapeMeta` with the ordered slot list. `PrimaryActionKind` is non-null for secondary-dispatch actions that share a leading token with a primary action (e.g., `AppendBy` shares `append` with `Append`); the parser consults target type to disambiguate. The type checker reads `SyntaxShape` to dispatch the corresponding `TypedAction` subtype, and PreceptBuilder reads it to choose the emitted action-plan opcode.

**`DynamicObligationGenerator`** (F-LANG-CAT-24) is an optional delegate that generates proof obligations whose shape depends on runtime context the catalog cannot statically encode — e.g., interval-containment obligations whose declared bounds depend on the specific target field's `min`/`max` modifiers. Returns an empty array when no dynamic obligations apply. The proof engine invokes it during obligation collection for actions that need per-call-site shape; static `ProofRequirements` cover obligations whose shape is the same at every call site.

##### ActionShapeMeta, ActionSyntaxSlot, and ActionSlotRole

```csharp
public enum ActionSlotRole
{
    Target          = 1,  // always first, always present
    Value           = 2,  // value being assigned or added
    Key             = 3,  // key in PutKeyValue
    Index           = 4,  // zero-based index (InsertAt, RemoveAtIndex)
    IntoTarget      = 5,  // capture variable in dequeue-into (optional)
    OrderingKey     = 6,  // ordering key in CollectionValueBy (by expr)
    OrderingCapture = 7,  // capture variable in CollectionIntoBy (by expr, optional)
}

public sealed record ActionSyntaxSlot(
    ActionSlotRole Role,
    TokenKind?     PrecedingSeparator,  // null = positional (no preceding keyword)
    bool           IsOptional);

public sealed record ActionShapeMeta(
    ActionSyntaxShape  Shape,
    ActionSyntaxSlot[] Slots)
{
    // Pre-computed set of every distinct separator token in Slots.
    public FrozenSet<TokenKind> SeparatorTokens { get; }
}
```

`Actions.GetShapeMeta(shape)` returns the canonical `ActionShapeMeta` for a given `ActionSyntaxShape`. The parser reads `SeparatorTokens` to identify the end of an action's target expression (any separator token terminates target parsing), then reads each slot's `Role` and `PrecedingSeparator` to parse the remaining argument structure without per-action logic.

**9 shapes and their slot structures:**

| Shape | Slots (Role: PrecedingSeparator, optional?) |
|-------|---------------------------------------------|
| `AssignValue` | Target: null, Value: `=` |
| `CollectionValue` | Target: null, Value: null |
| `CollectionInto` | Target: null, IntoTarget: `into` (opt) |
| `FieldOnly` | Target: null |
| `CollectionValueBy` | Target: null, Value: null, OrderingKey: `by` |
| `InsertAt` | Target: null, Value: null, Index: `at` |
| `RemoveAtIndex` | Target: null, Index: `at` |
| `PutKeyValue` | Target: null, Key: null, Value: `=` |
| `CollectionIntoBy` | Target: null, IntoTarget: `into` (opt), OrderingCapture: `by` (opt) |

**Consumers:** MCP vocabulary, LS completions (action verbs after `->` in event bodies), LS hover, parser validation, type checker (target type compatibility and `TypedAction` dispatch per `precept-language-spec.md` §3.8), PreceptBuilder (action-plan shape selection).

#### 8. Constructs (✅ Implemented)

Grammar forms / declaration shapes.

| Part | Type |
|------|------|
| Kind enum | `ConstructKind` (15 members) |
| Meta record | `ConstructMeta(Kind, Name, Description, UsageExample, AllowedIn[], Slots[], Entries, RoutingFamily, SnippetTemplate?, ModifierDomain, IsOutlineNode, OutlineSymbolTag?)` — see full shape below |
| Supporting types | `ConstructSlot(Kind, IsRequired, Description?, TerminationTokens?, IsList, IsChainable, ItemIntroducerToken?, Vocabulary)`, `ConstructSlotKind` (20-member enum), `SlotVocabulary` (13-member enum, completion vocabulary per slot) |

| Catalog class | `Constructs` — `GetMeta()`, `All` |
| Output type | None |

**Members (from `precept-language-spec.md` §2.2 top-level dispatch):**

`PreceptHeader`, `FieldDeclaration`, `StateDeclaration`, `EventDeclaration`, `RuleDeclaration`, `TransitionRow`, `StateEnsure`, `EventEnsure`, `AccessMode`, `OmitDeclaration`, `StateAction`, `EventRow` (stateless precept event row), `ConstructionRow`, `ConstructionRowReject`, `TransitionRowReject`

(Source: `src/Precept/Language/ConstructKind.cs` — 15 members.)

**Consumers:** MCP vocabulary (grammar reference), LS completions (context-sensitive construct suggestions), TextMate grammar (derivable from slot arrays), reference documentation, parser validation tests.

Guard position is encoded directly in the ordered `Slots` array. Constructs with pre-verb guards place an optional `GuardClause` slot before the verb-bearing slot; there is no separate guard-placement boolean in `ConstructMeta`.

##### ConstructMeta — full shape

```csharp
public sealed record ConstructMeta(
    ConstructKind                        Kind,
    string                               Name,
    string                               Description,
    string                               UsageExample,
    ConstructKind[]                      AllowedIn,           // empty = valid at precept body level (top-level)
    IReadOnlyList<ConstructSlot>         Slots,
    ImmutableArray<DisambiguationEntry>  Entries,
    RoutingFamily                        RoutingFamily,
    string?                              SnippetTemplate          = null,
    ModifierDomain                       ModifierDomain           = ModifierDomain.None,
    bool                                 IsOutlineNode            = false,
    string?                              OutlineSymbolTag         = null   // e.g. "Module", "Property"; non-null when IsOutlineNode = true
);

public sealed record ConstructSlot(
    ConstructSlotKind Kind,
    bool              IsRequired          = true,
    string?           Description         = null,
    TokenKind[]?      TerminationTokens   = null,
    bool              IsList              = false,
    bool              IsChainable         = false,
    TokenKind?        ItemIntroducerToken = null,
    SlotVocabulary    Vocabulary          = SlotVocabulary.None
);

// 20-member helper enum (source: ConstructSlot.cs):
//   IdentifierList, TypeExpression, ModifierList, StateEntryList, ArgumentList,
//   ComputeExpression, GuardClause, ActionChain, Outcome, StateTarget, EventTarget,
//   EnsureClause, BecauseClause, AccessModeKeyword, FieldTarget, RuleExpression,
//   InitialMarker, RejectClause, SuccessOutcome, EventEntryList
public enum ConstructSlotKind { ... }

// 13-member paired enum (source: ConstructSlot.cs) — declares the completion
// vocabulary a slot offers; drives CompletionHandler dispatch once SlotPositionResolver
// ships (Slice 3):
//   None, StateNames, EventNames, FieldNames, ActionVerbs, TypeKeywords, Modifiers,
//   Expression, TopLevel, OutcomeKeywords, AccessModes, StateEntryNames, RejectReason
public enum SlotVocabulary { ... }
```

`AllowedIn` declares where a construct can appear: empty means the construct is valid at precept body level (top-level declarations); populated means the construct is only valid nested inside one of the listed parent construct kinds. `LeadingToken` identifies the keyword that starts the construct — used by the grammar generator to emit keyword-anchored rules from catalog metadata. LS completions use `AllowedIn` to filter context-sensitive suggestions: "which constructs have the current cursor's parent construct kind in their `AllowedIn`?"

The four additional `ConstructSlot` fields beyond the basic `(Kind, IsRequired, Description, TerminationTokens)` quartet (`IsList`, `IsChainable`, `ItemIntroducerToken`, `Vocabulary`) drive list-recognition (comma-separated state/event entries, field target lists), action-chain recognition (the `->` arrow chain in state actions and event handlers), and per-slot LS completion dispatch. The `Vocabulary` field is the bridge from slot position to the completion candidates the language server offers when the cursor lands in that slot.

#### 9. ExpressionForms (✅ Implemented)

Expression grammar forms — the 15-member taxonomy of what the Pratt parser can construct and what role each form plays (null-denotation atom vs. left-denotation extension).

| Part | Type |
|------|------|
| Kind enum | `ExpressionFormKind` (15 members) |
| Meta record | `ExpressionFormMeta(Kind, Category, IsLeftDenotation, LeadTokens, HoverDocs, BindingPower?)` |
| Supporting type | `ExpressionCategory` (4 members: `Atom`, `Composite`, `Invocation`, `Collection`) |
| Catalog class | `ExpressionForms` — `GetMeta()`, `All` |
| Output type | None |

**Members:**

`Literal`, `Identifier`, `Grouped` (atoms — null-denotation); `BinaryOperation`, `UnaryOperation`, `MemberAccess`, `Conditional`, `PostfixOperation` (composites); `FunctionCall`, `MethodCall`, `CIFunctionCall` (invocations); `ListLiteral` (collection); `Quantifier`, `InterpolatedString`, `InterpolatedTypedConstant`

**Consumers:** parser coverage enforcement (via `[HandlesCatalogMember]`/`[HandlesCatalogExhaustively]` annotations), Pratt led binding-power lookup for non-operator forms such as member access, MCP vocabulary, reference documentation.

#### 10. Constraints (✅ Implemented)

The five constraint declaration forms. Each form has a distinct activation shape: invariants are always active; state-anchored constraints activate on state entry, residency, or exit; event preconditions fire before an event executes.

| Part | Type |
|------|------|
| Kind enum | `ConstraintKind` (5 members: `Invariant`, `StateResident`, `StateEntry`, `StateExit`, `EventPrecondition`) |
| Meta record | `ConstraintMeta` — DU as identity (see below) |
| Catalog class | `Constraints` — `GetMeta()`, `All` |
| Output type | `ConstraintDescriptor` (in `Precept.Runtime`) — instance carrying `Kind`, `ScopeTarget`, `ExpressionText`, `Because`, `ReferencedFields`, `HasGuard`, `SourceLine` |

**Meta shape — DU as identity:**

```csharp
public abstract record ConstraintMeta(ConstraintKind Kind, string Description)
{
    public sealed record Invariant()         : ConstraintMeta(ConstraintKind.Invariant, ...);

    // Abstract intermediate — groups the three state-anchored kinds
    public abstract record StateAnchored(ConstraintKind Kind, string Description)
        : ConstraintMeta(Kind, Description);
    public sealed record StateResident()     : StateAnchored(ConstraintKind.StateResident, ...);
    public sealed record StateEntry()        : StateAnchored(ConstraintKind.StateEntry, ...);
    public sealed record StateExit()         : StateAnchored(ConstraintKind.StateExit, ...);

    public sealed record EventPrecondition() : ConstraintMeta(ConstraintKind.EventPrecondition, ...);
}
```

The `StateAnchored` intermediate layer allows consumers to check `meta is ConstraintMeta.StateAnchored` for any state-scoped constraint without individually testing three kinds.

**Consumers:** plan router (constraint bucket assignment), evaluator (activation timing), MCP vocabulary, LS hover.

#### 11. ProofRequirements (✅ Implemented)

The **10** proof obligation kinds that catalog entries can declare. Used by the proof engine to determine what must be proven before an operation, function, accessor, or action can execute.

| Part | Type |
|------|------|
| Kind enum | `ProofRequirementKind` (10 members: `Numeric`, `Presence`, `Dimension`, `Modifier`, `QualifierCompatibility`, `QualifierChain`, `IntervalContainment`, `LengthContainment`, `CountContainment`, `KeyPresence`) |
| Meta record | `ProofRequirementMeta(Kind, Description, DiagnosticCode?)` — DU as identity with a `DiagnosticCode?` field that names the catalog-mediated diagnostic for the obligation (null for `Numeric` and `KeyPresence`, which use context-dependent dispatch); see Schema Anatomy § ProofRequirements |
| Catalog class | `ProofRequirements` — `GetMeta()`, `All` |
| Instance values | `ProofRequirement` abstract record + 10 sealed subtypes (`NumericProofRequirement`, `PresenceProofRequirement`, `DimensionProofRequirement`, `QualifierCompatibilityProofRequirement`, `QualifierChainProofRequirement`, `ModifierRequirement`, `IntervalContainmentProofRequirement`, `LengthContainmentProofRequirement`, `CountContainmentProofRequirement`, `KeyPresenceProofRequirement`) — per-use obligation instances carried in `ActionMeta.ProofRequirements`, `FunctionOverload.ProofRequirements`, `BinaryOperationMeta.ProofRequirements`, `TypeAccessor.ProofRequirements` |

**Instance vs meta separation:** The `ProofRequirementMeta` DU describes the KIND statically (`ProofRequirements.All` enumerates them). The `ProofRequirement` instance record hierarchy carries per-use data — specific subjects, thresholds, comparisons, target field names, declared bounds — and lives in the catalog entries that declare obligations. The base `ProofRequirement` record carries `Kind` (catalog membership) and `Description`; subtypes carry kind-specific subjects and payloads. See Schema Anatomy § ProofRequirements for the full per-subtype shape table.

**Dual-subject kinds:** `QualifierCompatibility` and `QualifierChain` are the only dual-subject kinds — their obligation instances carry both `LeftSubject` and `RightSubject`. Consumers check `meta is ProofRequirementMeta.QualifierCompatibility` (or `.QualifierChain`) rather than inspecting a `SubjectArity` field.

**Consumers:** proof engine (obligation dispatch), type checker, Roslyn analyzers (`PRECEPT0005`, `PRECEPT0006`), MCP vocabulary.

#### 12. Outcomes (✅ Implemented)

The three transition-row outcome forms — the ways a transition row can conclude. Outcomes are a closed 3-member vocabulary resolved at parse time; they are not expressions and do not participate in expression resolution.

| Part | Type |
|------|------|
| Kind enum | `OutcomeKind` (3 members: `Transition`, `NoTransition`, `Reject`) |
| Meta record | `OutcomeMeta(Kind, LeadingToken, ArgumentKind, ParsedSubtype, Description, Example, SerializedKind: string)` |
| Supporting enum | `OutcomeArgumentKind { None, RequiredIdentifier, RequiredStringLiteral, SecondaryToken }` |
| Catalog class | `Outcomes` — `GetMeta()`, `All`, `ByLeadingToken`, `LeadingTokens`, `NoTransitionSecondaryToken` |
| Output type | `ParsedOutcome` abstract record + 4 sealed subtypes: `TransitionOutcome(StateName, Span)`, `NoTransitionOutcome(Span)`, `RejectOutcome(Reason, Span)`, `MalformedOutcome(Span)` |

**Outcome entry summary:**

| OutcomeKind | LeadingToken | ArgumentKind | SerializedKind | Produces |
|-------------|--------------|--------------|----------------|----------|
| `Transition` | `TokenKind.Transition` | `RequiredIdentifier` | `"transition"` | `TransitionOutcome(stateName)` |
| `NoTransition` | `TokenKind.No` | `SecondaryToken` | `"no transition"` | `NoTransitionOutcome()` |
| `Reject` | `TokenKind.Reject` | `RequiredStringLiteral` | `"reject"` | `RejectOutcome(reason)` |

**Derived indexes:**

- `ByLeadingToken` — O(1) `FrozenDictionary<TokenKind, OutcomeMeta>` for parser dispatch after consuming the arrow.
- `LeadingTokens` — `FrozenSet<TokenKind>` of all tokens that can follow the outcome arrow; used for vocabulary recognition and error recovery.
- `NoTransitionSecondaryToken` — structural constant (`TokenKind.Transition`) for the compound `no transition` form.

**Cross-catalog dependency:** Depends on `Tokens` (like all grammar catalogs) but has no other catalog dependencies.

**Consumers:** parser (outcome dispatch via `Outcomes.ByLeadingToken` and `OutcomeMeta.ArgumentKind`), LS completions (outcome context suggestions via `Outcomes.All`), LS hover (`OutcomeMeta.Description`, `OutcomeMeta.Example`), MCP `precept_language` (grammar vocabulary).

### Failure Mode Catalogs

#### 13. Diagnostics (✅ Implemented)

Compile-time rules — every error and warning the pipeline can produce. **148 members** as of the current build, spanning Lex, Parse, Type, Graph, and Proof stages. See `src/Precept/Language/DiagnosticCode.cs` for the canonical list and `docs/compiler/diagnostic-system.md` for the per-code documentation.

| Part | Type |
|------|------|
| Kind enum | `DiagnosticCode` (148 members) |
| Meta record | `DiagnosticMeta(Code, Stage, Severity, MessageTemplate, Category, RelatedCodes?, FixHint?, PreventsFault?, SuggestionSources?, TriggerCondition?, RecoverySteps?, ExampleBefore?, ExampleAfter?)` — see the Diagnostics/Faults schema-anatomy section for field-by-field semantics; source `src/Precept/Language/Diagnostics.cs:23` |
| Supporting enums | `DiagnosticStage { Lex, Parse, Type, Graph, Proof }`, `Severity { Info, Warning, Error }`, `DiagnosticCategory { Naming, TypeSystem, Temporal, BusinessDomain, Structure, Safety, Proof }`, `SuggestionSource { UserFields, UserStates, UserEvents, FunctionCatalog }` (drives "did you mean?" candidate search) |
| Catalog class | `Diagnostics` — `GetMeta()`, `All`, `Create()` |
| Output type | `Diagnostic(Severity, Stage, Code, Message, Span, Args)` — `Args` is an `ImmutableArray<string>` of the message-template arguments; `RelatedSpans : ImmutableArray<RelatedSpan>` is an init-only extra carrying secondary source spans (`RelatedSpan(Span, Message)`) when a diagnostic refers to multiple locations (F-LANG-CAT-25). Source: `src/Precept/Language/Diagnostic.cs:47` |

`DiagnosticCategory` describes *what* a diagnostic is about, complementing `DiagnosticStage` which describes *when* it fires. Used by the language server for filtering, documentation generation, and AI grounding.

#### 14. Faults (✅ Implemented)

Runtime failure modes — every fault the evaluator can produce. Currently 15 members. Each `FaultCode` carries a `[StaticallyPreventable(DiagnosticCode)]` attribute linking it to the compile-time rule that should prevent that site.

| Part | Type |
|------|------|
| Kind enum | `FaultCode` (15 members — see `src/Precept/Language/FaultCode.cs` for the canonical list; covers division/sqrt safety, type/field resolution, function arity/argument constraints, collection emptiness on access and mutation, qualifier compatibility, numeric overflow, range/length/count bounds) |
| Meta record | `FaultMeta(Code, MessageTemplate, Severity = FaultSeverity.Fatal, RecoveryHint?)` — `Severity` is `FaultSeverity.Fatal` for every shipping fault (transition aborts, no state changes committed); the field exists to make non-fatal severities representable when the runtime grows them. `RecoveryHint` is a per-fault user-facing remediation string, set on every member (see `src/Precept/Language/Faults.cs`). |
| Catalog class | `Faults` — `GetMeta()`, `All`, `Create()` |
| Output type | `Fault(Code, CodeName, Message, ExpressionContext?, InputValues?)` — `CodeName` is the `nameof`-derived stable identity string used for logging and MCP reporting; `ExpressionContext` (`SourceSpan?`) and `InputValues` (field/arg values at fault time) are optional structured context attached via `with` expressions at evaluator call sites that have the relevant payload available. |

**Consumers:** MCP fire/inspect, runtime outcome reporting.

---

#### 15. SemanticTokenTypes (✅ Implemented — tooling-adjacent)

The visual-classification catalog — the LSP custom semantic-token types and TextMate scopes that drive Precept's editor presentation. The 14th-vs-15th catalog by convention; see the Status note at the top of this document.

| Part | Type |
|------|------|
| Kind enum | `SemanticTokenTypeKind` (13 members — `Name`, `KeywordSemantic`, `KeywordGrammar`, `Operator`, `State`, `Event`, `Type`, `Value`, `FieldName`, `ArgName`, `Message`, `Comment`, `TypedLiteral`) |
| Meta record | `SemanticTokenTypeMeta(Kind, CustomType, TextMateScope, Description, ForegroundHex, Bold, Italic, SupportsConstrainedModifier)` — every member declares both an LSP custom token type string (e.g. `preceptState`) and a TextMate scope (e.g. `entity.name.type.state.precept`) in a single record. |
| Catalog class | `SemanticTokenTypes` — `GetMeta()`, `All` |
| Output type | None — consumers read meta directly |

**One-field architecture.** The bridge from the lexical surface to the visual surface is a single field — `TokenMeta.VisualCategory : SemanticTokenTypeKind?` — pointing upward into this catalog. There is no per-`TokenKind` switch in tooling code, no parallel TextMate scope dictionary in the grammar generator, no per-token color hex in the language server. A token's visual identity is `tokens.GetMeta(kind).VisualCategory → SemanticTokenTypes.GetMeta(visualCategory)`; the resulting `SemanticTokenTypeMeta` carries every consumer's view (LSP custom type, TextMate scope, foreground hex, bold/italic, constrained-modifier support) on one record. Adding a visual category means adding one enum member and one switch arm; both downstream consumers pick it up automatically.

**Two-consumer note.** The catalog serves two distinct surfaces with the same metadata:

- **TextMate grammar generator** reads `TextMateScope` (plus `Bold`/`Italic`/`ForegroundHex` for the matching VS Code theme file) to emit scope-anchored patterns. The grammar is static — every member produces grammar output.
- **LSP semantic-tokens handler** reads `CustomType` (the LSP token-type string) to encode tokens on the wire. The encoding is dynamic — only tokens with a non-null `TokenMeta.VisualCategory` are emitted as semantic tokens.

**Why it's a catalog rather than a hardcoded mapping.** A hardcoded TextMate template + a separate LSP enum drifts. A catalog with one record per visual category keeps the LSP and TextMate views structurally aligned and lets the grammar generator regenerate from one source. Without this catalog the two surfaces would mention the same color/scope/type-string facts independently and would inevitably diverge — exactly the failure mode the catalog system exists to prevent. The Slice-10 design decision (`docs/Working/Archive/language-server-implementation-plan.md:641, 645`) makes this explicit.

**Membership and meta.** Source: `src/Precept/Language/SemanticTokenTypes.cs`. Membership, per-member hex/scope/customType values, and the meta record shape are canonical there — not enumerated here.

**Consumers:** TextMate grammar generator (`tools/Precept.LanguageServer.GrammarGenerator/`), LSP semantic-tokens handler (`tools/Precept.LanguageServer/SemanticTokens/`), VS Code theme file generator, MCP visual-classification surface.

---

## Supporting Types

These record types are shared across multiple catalogs. They are not catalogs themselves — they have no `Kind` enum or `All` property — but they are part of the catalog system's vocabulary.

### TypeTarget discriminated union

Used in `ModifierMeta.ApplicableTo` and `ActionMeta.ApplicableTo`. Replaces `TypeKind[]` wherever a catalog entry needs to declare type applicability with optional modifier requirements.

```csharp
public record TypeTarget(TypeKind Kind);

public sealed record ModifiedTypeTarget(
    TypeKind?      Kind,
    ModifierKind[] RequiredModifiers
) : TypeTarget(Kind ?? TypeKind.Error);
```

- `TypeTarget(Kind)` — applies to fields of the given type, no modifier requirement.
- `ModifiedTypeTarget(Kind, RequiredModifiers)` — applies when the field has the given type AND all listed modifiers. `Kind = null` means "any type."
- List = OR semantics. Each entry in an `ApplicableTo` array is checked independently. Each `ModifiedTypeTarget.RequiredModifiers` array is an AND condition.

```csharp
public static bool IsApplicable(TypeTarget target, Type fieldType, IReadOnlyList<ModifierKind> fieldModifiers)
    => target switch
    {
        ModifiedTypeTarget m => (m.Kind == null || fieldType.Kind == m.Kind)
                                && m.RequiredModifiers.All(fieldModifiers.Contains),
        TypeTarget t         => fieldType.Kind == t.Kind,
    };
```

---

## Qualifier Propagation

The Operations catalog declares **what** each operation produces, including conditional results via `QualifierMatch`. The type checker implements **how** qualifiers propagate through expressions — taking the catalog's result `TypeKind` and constructing the fully-qualified result `Type`.

### The split

| Concern | Owner | Needs qualifier values? |
|---------|-------|------------------------|
| Operation legality + result TypeKind | Operations catalog | No — `QualifierMatch` discriminator handles branching |
| Qualifier compatibility (same-currency check, same-dimension check) | Type checker | Yes |
| Result qualifier construction (which currency/unit does the result carry?) | Type checker | Yes |
| Diagnostic emission (`CrossCurrencyArithmetic`, etc.) | Type checker | Yes |
| Proof obligations for unknown qualifiers | Type checker → proof engine | Yes |

The catalog is the source of truth for result types. The type checker is the source of truth for qualifier-level reasoning. These are complementary, not competing.

### Qualifier propagation patterns

All qualifier-bearing operations follow one of four patterns. The type checker applies these after the catalog lookup succeeds:

**Pattern A — Homogeneous ±:** Both operands must have compatible qualifiers. Result inherits the resolved qualifier. (`money ± money`, `quantity ± quantity`, `price ± price`)

**Pattern B — Scalar scaling:** The qualified operand's qualifiers pass through unchanged. (`money * decimal`, `quantity / decimal`, etc.)

**Pattern C — Dimensional cancellation / derivation:** Result qualifiers are derived from input qualifiers. Operation-specific: `price * quantity → money` takes currency from price and cancels unit; `exchangerate * money → money` verifies pair alignment. (`price * quantity → money`, `money / quantity → price`, `exchangerate * money → money`)

**Pattern D — Same-type ratio:** Qualifiers must be compatible (same as Pattern A), but result is dimensionless — `DecimalType()` with no qualifier. (`money / money` same currency, `quantity / quantity` same dimension)

### Five qualifier-bearing types

| Type | Qualifier axes | Pattern A ops | Pattern B ops | Pattern C ops | Pattern D ops |
|------|---------------|---------------|---------------|---------------|---------------|
| `money` | Currency | `money ± money` | `money * decimal` | `exchangerate * money`, `price * quantity` | `money / money` (same) |
| `quantity` | Unit, Dimension | `quantity ± quantity` | `quantity * decimal` | `price * quantity` (cancel), `money / quantity` | `quantity / quantity` (same) |
| `period` | Unit, Dimension | `period ± period` | — | `price * period` (cancel) | — |
| `price` | Currency, Unit, Dimension | `price ± price` | `price * decimal` | `money / quantity → price` | — |
| `exchangerate` | FromCurrency, ToCurrency | — | `exchangerate * decimal` | `money / money → exchangerate` (diff) | — |

### Unknown qualifier handling

When qualifiers are statically unknown (open field, no `in` constraint, no guard), the type checker follows the three-tier enforcement model from the business-domain-types spec:

- **Tier 1 (compile time):** Both qualifiers known → resolve immediately
- **Tier 2 (proof engine):** One or both unknown → emit proof obligation ("prove these currencies match at this execution point")
- **Tier 3 (runtime boundary):** Qualifier validated at fire/update boundary before engine runs

---

## Proof Obligations

Catalog entries declare proof obligations they impose on the type checker and proof engine. The proof layer reads these from catalog metadata — no hardcoded obligation lists in the proof engine.

### ProofSubject discriminated union

```csharp
public abstract record ProofSubject;

// References a parameter by object identity — no index, no string.
// Must be reference-equal to one of the ParameterMeta instances in the
// containing overload's Parameters list. Enforced by Roslyn analyzer.
public sealed record ParamSubject(ParameterMeta Parameter) : ProofSubject;

// References the receiver of an accessor or action.
// Accessor is a TypeAccessor reference (e.g. the count accessor) — not a string.
// Null Accessor means "the field itself" — used for PresenceProofRequirement.
public sealed record SelfSubject(TypeAccessor? Accessor = null) : ProofSubject;
```

### ProofRequirement discriminated union

```csharp
public abstract record ProofRequirement(ProofSubject Subject, string Description);

// Numeric interval proof: subject comparison threshold must hold
public sealed record NumericProofRequirement(
    ProofSubject Subject,
    OperatorKind Comparison,
    decimal      Threshold,
    string       Description
) : ProofRequirement(Subject, Description);

// Presence proof: optional field must be set before access
public sealed record PresenceProofRequirement(
    ProofSubject Subject,
    string       Description
) : ProofRequirement(Subject, Description);

// Dimension proof: period operand must have a specific time dimension.
// Valid only on BinaryOperationMeta.ProofRequirements.
public enum PeriodDimension { Any, Date, Time }

public sealed record DimensionProofRequirement(
    ProofSubject    Subject,
    PeriodDimension RequiredDimension,
    string          Description
) : ProofRequirement(Subject, Description);
```

`OperatorKind` is reused directly — no parallel enum.

### Valid subjects and requirement types per catalog entry

| Catalog entry | Valid proof requirement types |
|---|---|
| `FunctionOverload.ProofRequirements` | `NumericProofRequirement`, `PresenceProofRequirement` |
| `BinaryOperationMeta.ProofRequirements` | `NumericProofRequirement`, `DimensionProofRequirement` |
| `TypeAccessor.ProofRequirements` | `NumericProofRequirement`, `PresenceProofRequirement` |
| `ActionMeta.ProofRequirements` | `NumericProofRequirement` |

Enforced by Roslyn analyzer — see PRECEPT0005/PRECEPT0006.

### ParameterMeta — object-reference safety

`ParameterMeta` is shared across `FunctionOverload.Parameters` and `BinaryOperationMeta.Lhs`/`Rhs`. Parameters are declared as named statics; `ParamSubject` holds a direct reference to the `ParameterMeta` instance. The Roslyn analyzer verifies `ParamSubject.Parameter` is reference-equal to one of the containing overload/operation's parameter instances — compile-time referential integrity with no strings.

### Complete proof obligation inventory

| Obligation | Catalog entry | Requirement type | Subject | Condition |
|---|---|---|---|---|
| Divisor safety | `BinaryOperationMeta` `/` and `%` | `NumericProofRequirement` | `ParamSubject(Rhs)` | `!= 0` |
| Sqrt non-negative | `FunctionOverload` sqrt | `NumericProofRequirement` | `ParamSubject(Input)` | `>= 0` |
| Pow exponent non-negative | `FunctionOverload` pow(integer,integer) | `NumericProofRequirement` | `ParamSubject(Exponent)` | `>= 0` |
| Collection accessor (peek, min, max) | `TypeAccessor` | `NumericProofRequirement` | `SelfSubject(countAccessor)` | `> 0` |
| Dequeue / Pop | `ActionMeta` | `NumericProofRequirement` | `SelfSubject(countAccessor)` | `> 0` |
| Optional field access | `TypeAccessor` on optional field | `PresenceProofRequirement` | `SelfSubject()` | is set |
| Date + period dimension | `BinaryOperationMeta` date±period | `DimensionProofRequirement` | `ParamSubject(Period)` | Date dimension |
| Time + period dimension | `BinaryOperationMeta` time±period | `DimensionProofRequirement` | `ParamSubject(Period)` | Time dimension |

---

## Construct Slot Model

**Precept has no block-structured constructs.** Every declaration is line-oriented — a single line introduced by a leading keyword (`field`, `state`, `on`, `from`, `rule`, …) followed by an ordered sequence of slots. There is no `{ … }` body that nests further declarations. The lone brace-delimited form in the language is **interpolation** — `"… {expr} …"` and `'… {expr} …'` inside string and typed-constant literals — which holds an embedded **expression**, not a nested construct. This is why `ConstructMeta.Slots` is a flat ordered list rather than a tree, and why grammar generation is single-pass.

The construct slot model is the machine-readable form of this fact. Each entry in `Constructs.GetMeta(...)` declares an ordered list of `ConstructSlot` records describing the slots a parser must fill to recognize the construct.

**Canonical sources:**

- **`src/Precept/Language/ConstructSlot.cs`** — the canonical record shape, the `ConstructSlotKind` enum (20 members), the paired `SlotVocabulary` enum (13 members) declaring per-slot completion vocabulary, and the optional fields (`IsList`, `IsChainable`, `ItemIntroducerToken`, `Vocabulary`) that govern parser dispatch and LS completion behavior.
- **`docs/compiler/grammar-generator.md`** — the canonical grammar-generation rules: how slot arrays + Tokens + Types compose into TextMate patterns, how termination tokens (`SlotGuardClause`, `SlotEnsureClause`) constrain Pratt parsing, how `ItemIntroducerToken` drives list-slot recognition.

**Two-layer visual-classification story** (TextMate vs LSP semantic tokens):

| Surface | Built from | Generator |
|---|---|---|
| **TextMate grammar** | `Tokens × SemanticTokenTypes` — every `TokenMeta.VisualCategory` resolves to a `SemanticTokenTypeMeta.TextMateScope`, and the grammar generator emits one alternation per visual category. | The grammar generator in `tools/Precept.LanguageServer.GrammarGenerator/` (see `docs/compiler/grammar-generator.md`). |
| **LSP semantic tokens** | `SemanticIndex` + interpolation traversal — the LS walks the resolved semantic index for declaration-position tokens and re-tokenizes interpolated expressions inside `"…{…}…"` and `'…{…}…'` literals. | `tools/Precept.LanguageServer/SemanticTokens/` (see `docs/tooling/language-server.md`). |

The TextMate layer is **catalog-driven and static** — every member of `Tokens` × `SemanticTokenTypes` is enumerable, and the grammar is regenerated whenever either catalog changes. The LSP layer is **AST-driven and dynamic** — it needs the resolved meaning of an identifier (state name vs event name vs field name) and the recursive expression structure inside interpolations, neither of which is statically expressible in TextMate. The two layers cover complementary surfaces of the same visual model; both read `SemanticTokenTypes` for the visual category lookup.

---

## Qualifier Registries (Not Catalogs)

Currency codes and measurement units are validated at type-check time, but they are NOT catalogs. They are **reference data registries**:

| Registry | Shape | Size | Source |
|----------|-------|------|--------|
| ISO 4217 currencies | Embedded XML + lazy-loaded frozen set/dictionary | 159 admitted codes after Precept exclusions | ISO 4217 `list-one.xml` |
| UCUM units | Embedded XML + lazy-loaded atom/prefix catalogs plus compositional parser | Open compositional space over seeded atoms/prefixes | UCUM `ucum-essence.xml` |

**Why not catalogs:** These are not aspects of the *language* — they are aspects of the *data domain*. Adding a new currency code doesn't change the language; adding a new type keyword does. Currency codes don't have per-member metadata that consumers need; they have a single validation predicate ("is this a valid code?"). Catalogs describe the language surface; registries validate domain values. External reference data uses embedded XML plus lazy loading, not the catalog pattern.

---

## Syntax Reference

The catalogs cover the language's *vocabulary* exhaustively. The language also has *grammar meta-rules* — singular facts about how source text is structured — that consumers need but that have no per-member enum. These are language-level constants, not catalogs.

`SyntaxReference` is a static class with typed properties, part of the same metadata-driven source of truth:

```csharp
public static class SyntaxReference
{
    public static string GrammarModel       => "line-oriented";
    public static string CommentSyntax      => "# to end of line";
    public static string IdentifierRules    => "Starts with letter, alphanumeric + underscore, case-sensitive";
    public static string StringLiteralRules => "Double-quoted, \\\" escape only, no interpolation";
    public static string NumberLiteralRules => "Integers (42), decimals (3.14), no hex/scientific/underscore separators";
    public static string WhitespaceRules    => "Not significant — indentation is cosmetic, line breaks separate declarations";
    public static string NullNarrowing      => "if Field is set narrows to non-nullable in the then branch";
    public static IReadOnlyList<string> ConventionalOrder => ["header", "fields", "rules", "states", "ensures", "accessModes", "events", "event ensures", "transitions", "state actions"];
}
```

**Consumers:**

| Consumer | How it reads |
|----------|-------------|
| MCP `precept_language` | Serializes to a `syntaxReference` JSON object in the response |
| Human reference docs | Generates a "Grammar Basics" section from the same properties |
| LS hover | Tooltip text for identifier tokens, comment tokens, etc. |
| AI grounding | Reads alongside catalog data for complete language understanding |

This is not a catalog — there is no enum, no `GetMeta()`, no `All`. It is structured metadata about the grammar as a whole, derived from the same codebase. No hand-written docs page that drifts from the implementation.

---

## Cross-Catalog Derivation

The test of completeness: every cell should trace back to a catalog, never to hardcoded consumer logic.

| Consumer surface | Catalogs read | How |
|------------------|---------------|-----|
| **TextMate grammar** | Constructs → Tokens → Types | **Generated** from catalog metadata. Construct slot arrays generate patterns; token keywords generate keyword alternations; type keywords via `Types.All` filtered by `Token.Text`. No hand-maintained alternation lists. Tests verify the generator produces correct output. |
| **MCP `precept_language`** | All catalogs' `.All` + `SyntaxReference` | Union of all catalog enumerations IS the language spec. MCP tool iterates each and serializes. `SyntaxReference` adds grammar meta-rules. |
| **LS completions** | Tokens + Types + Functions + Modifiers + Actions | **Generated** from catalog metadata. Context-filtered: type position → `Types.All`; expression → `Functions.All`; after type → `Modifiers.All` filtered by `ApplicableTo`; event body → `Actions.All`. No hand-maintained completion lists. |
| **LS hover** | Types + Functions + Operators + Operations + Constraints | Per-member descriptions from catalog metadata. `ConstraintMeta.Description` populates hover for `rule`/`ensure` declarations. |
| **LS semantic tokens** | Tokens (via `TokenMeta.Categories`) | Token categories map directly to semantic token types |
| **Type checker validation** | Types + Functions + Operations + Modifiers + Actions + ProofRequirements | Catalog lookups replace hand-coded validation logic: modifier applicability → `Modifiers.GetMeta().ApplicableTo`; function signatures → `Functions.GetMeta()`; operation legality → `Operations.Resolve()`; type keyword resolution → `Types.All` frozen dictionary keyed by `Token.Kind`; typed-constant validation registration → `TypeMeta.ContentValidation`; proof obligation kinds → `ProofRequirements.GetMeta()` |
| **Parser vocabulary** | Operators + Types + Modifiers + Actions + Constructs | Frozen dictionaries derived from catalogs at startup: `Operators.All` → precedence table; `Types.All` → type keyword mapping; `Modifiers.All` → recognition sets; `Actions.All` → action keywords. No hand-maintained vocabulary tables. |
| **Plan router / Precept Builder** | Constraints | `Constraints.GetMeta(kind)` routes each `ConstraintDescriptor` into the correct activation bucket (`always`, `StateResident`, `StateEntry`, `StateExit`, `EventPrecondition`). `ConstraintMeta.StateAnchored` groups all state-scoped kinds without per-member checks. |
| **Proof engine** | ProofRequirements | `ProofRequirements.GetMeta(kind)` dispatches proof obligation instances by kind. `ProofRequirementMeta.QualifierCompatibility` identifies dual-subject obligations without per-kind conditionals. |
| **Evaluator dispatch** | Functions + Operations + Constraints | Binary/unary dispatch: builder embeds `static readonly` executor delegates (from `TypeRuntimeMeta.BinaryExecutors`/`UnaryExecutors`) directly in `BinaryOp`/`UnaryOp` opcodes at compile time; evaluator calls `opcode.Executor(l, r)` — no lookup, no switch. `Constraints.GetMeta()` drives constraint activation timing — no hardcoded per-kind activation logic. Function execution dispatch delegate design is pending. |
| **Typed constant dispatcher** | Types | `TypedConstantValidation.Validate(...)` reads `TypeMeta.ContentValidation` and dispatches on the DU subtype. No parallel validator registry. |
| **Runtime boundary validation** | Modifiers | `ValueModifierMeta.ApplicableTo` and `HasValue` drive boundary checks. No `switch` on `ModifierKind`. |
| **Reference documentation** | All 12 language-definition catalogs + `SyntaxReference` | **Generated** from catalog metadata. Tables, syntax sections, grammar reference all derived from `All` properties. |
| **AI grounding** | All catalogs + `SyntaxReference` | Complete, always-accurate language reference — AI grounded on catalog output cannot hallucinate features |

No consumer surface maintains its own parallel list. Every fact comes from a catalog `All` property, `GetMeta()` call, or `SyntaxReference` property. TextMate grammar, LS completions, and reference documentation are **generated** from catalogs — not hand-maintained. Tests verify the generators produce correct output.

---

## Future Opportunities

These are enabled by the catalog system but not part of the initial implementation:

1. **Error message enrichment** — diagnostics that cross-reference catalog entries (e.g., "did you mean `MoneyType`?" suggestions from `Types.All`).
2. **Quick fixes / code actions** — LS code actions derived from catalog metadata (e.g., suggest valid modifiers for a type from `Modifiers.All` filtered by `ApplicableTo`).
3. **Parser validation** — Catalog-driven parser conformance generation from construct slot sequences, optional-slot presence matrices, and disambiguation paths.
4. **Version diffing** — catalog snapshots as changelog. Diff two versions of `All` to produce a human-readable changelog of language surface changes.
5. **Playground / explorer UI** — all catalog-derived. Browse the language interactively from the catalog data.

---

## Test Strategy

Catalogs are the language specification in machine-readable form. Tests verify that the specification is correct, complete, and that all generated artifacts match.

### Non-negotiable rules

1. **No catalog without snapshot test.** Every catalog's `All` property is snapshotted to a golden file. Any change to the catalog requires explicit snapshot update. This catches unintended metadata changes.

2. **Exhaustive matrix green before old logic deleted.** When a catalog replaces hand-coded logic (e.g., `Operations` replaces `OperatorTable`), the new catalog-driven path must have complete test coverage before the old procedural path is removed. Clean replacement — not parallel old+new.

3. **Property-based test generation from catalogs.** The catalogs ARE the test oracle. Tests are generated from catalog metadata:
   - Every `(OperatorKind × TypeKind × TypeKind)` combination tested for legality against `Operations.All`
   - Every `(ModifierKind × TypeKind)` combination tested for applicability against `Modifiers.All`
   - Every `FunctionOverload` tested with valid and invalid argument types
   - Every `TypeAccessor` tested for return type correctness
   - Every cross-catalog reference validated (e.g., every `TypeMeta.Token.TypeKind` equals `TypeMeta.Kind`)
   - Projected: ~15,500+ auto-generated test cases

4. **Generated artifact tests.** TextMate grammar, LS completions, and MCP vocabulary are generated from catalogs. Tests verify the generators produce correct output — not that hand-maintained copies match. There are no hand-maintained copies to drift.

### Cross-catalog integrity tests

| Invariant | Test |
|-----------|------|
| Every type has a valid token | `Types.All.All(t => t.Token.Categories.Contains(TokenCategory.Type))` |
| Every operator has a valid token | `Operators.All.All(o => o.Token.Categories.Contains(TokenCategory.Operator))` |
| Token→Type index is complete | `Types.All.Select(t => t.Token.Kind).Distinct().Count() == Types.All.Count` (no two types share a token) |
| Widening acyclicity | No circular chains in `TypeMeta.WidensTo` |
| Subsumption acyclicity | No circular chains in `ValueModifierMeta.Subsumes` |
| `ParamSubject` referential integrity | Every `ParamSubject.Parameter` is reference-equal to a `ParameterMeta` in the containing overload/operation |
| Proof requirement type validity | `DimensionProofRequirement` only on `BinaryOperationMeta`, etc. (per PRECEPT0006 rules) |

---

## Pipeline Stage Impact

As catalogs are implemented, each pipeline stage gets thinner — domain knowledge migrates from hand-coded logic into metadata, and stages become generic machinery that reads catalog data.

| Stage | Current state | Catalog impact |
|-------|--------------|----------------|
| **Lexer** | Already uses `Tokens.Keywords` for keyword classification | Minimal further impact. Operator scan priority derivable from `Operators.All` sorted by `Token.Text.Length` descending. |
| **Parser** | Hand-coded vocabulary tables + recursive descent grammar | Vocabulary tables — operator precedence, type keyword mappings, modifier/action recognition sets (~40–50% of language knowledge decisions) — migrate to catalog-derived frozen dictionaries at startup. Grammar productions stay hand-written. Construct slots enable test generation and LS completions. When a new type, modifier, operator, or action is added to a catalog, the parser adapts automatically — no parser edit needed. |
| **TypeChecker** | Catalog-driven validation | Full design documented in `docs/compiler/type-checker.md`. Modifier applicability/exclusivity → `ValueModifierMeta.ApplicableTo`, `ModifierMeta.MutuallyExclusiveWith`; modifier subsumption → `ValueModifierMeta.Subsumes`; access-mode semantics → `AccessModifierMeta.IsPresent`, `IsWritable`; anchor scope/target → `AnchorModifierMeta.Scope`, `Target`; function resolution → `Functions.FindByName(name)`, `FunctionMeta.Overloads`, `FunctionOverload.Match`, `FunctionMeta.HasCIVariant`, `FunctionMeta.CIVariantOf`; operator resolution → `Operations.FindUnary(op, type)`, `Operations.FindCandidates(op, lhs, rhs)`; type widening/traits/qualifiers/implied modifiers → `TypeMeta.WidensTo`, `Traits`, `QualifierShape`, `ImpliedModifiers`, `Accessors`; action legality → `ActionMeta.ApplicableTo`, `AllowedIn`, `SyntaxShape`, `ValueRequired`, `IntoSupported`; proof obligations → `ProofRequirements.GetMeta()`. |
| **GraphAnalyzer** | Hand-coded state reachability, modifier semantics | Moderate: state modifier structural semantics (`AllowsOutgoing`, `RequiresDominator`, `PreventsBackEdge`) are catalog metadata on `StateModifierMeta`. Event modifier graph requirements → `EventModifierMeta.RequiredAnalysis`. Graph algorithms (reachability, dominator trees, SCC) remain generic machinery. |
| **ProofEngine** | Catalog-declared obligations | Full design documented in `docs/compiler/proof-engine.md`. `ProofRequirement[]` on `BinaryOperationMeta`, `FunctionOverload`, `TypeAccessor`, and `ActionMeta` carry all proof obligations as metadata. `ProofRequirements.GetMeta(kind)` dispatches obligation instances. `ValueModifierMeta.ProofDischarges` enables catalog-driven modifier-proof strategy (CC#5 resolved). |
| **PreceptBuilder** | Catalog-driven routing | Full design documented in `docs/runtime/precept-builder.md`. `Constraints.GetMeta(kind)` routes each `ConstraintDescriptor` into the correct activation bucket. Pattern-match on `ConstraintMeta` DU subtypes (`Invariant`, `StateResident`, `StateEntry`, `StateExit`, `EventPrecondition`) — not the `ConstraintKind` enum directly. `ConstraintMeta.StateAnchored` groups the three state-scoped subtypes for shared graph-analysis paths. `ActionMeta.SyntaxShape` drives action plan opcode emission. |
| **Evaluator** | Stub — full design in `docs/runtime/evaluator.md` | Constraint activation timing → `Constraints.GetMeta(kind)` → `ConstraintMeta` DU subtype; modifier boundary validation → `ValueModifierMeta.ApplicableTo`, `HasValue`; accessor metadata → `TypeMeta.Accessors`, `TypeAccessor.ParameterType`, `RequiredTraits`; access mode enforcement → `FieldDescriptor.AccessModes` (pending — see Open Questions). Binary/unary operation dispatch: executor delegates are embedded in `BinaryOp`/`UnaryOp` opcodes at build time (fetched from `TypeRuntimeMeta.BinaryExecutors`/`UnaryExecutors`); evaluator calls `opcode.Executor(l, r)` — no per-`OperationKind` switch, no catalog lookup at evaluation time. Function and action execution dispatch delegate design is pending. |

Pattern: domain knowledge → metadata. Stages → generic machinery that reads catalogs.

### Parser-catalog integration pattern

The parser is the most common site for accidentally re-encoding catalog knowledge as hardcoded sets. The rule: **before writing any `FrozenSet<TokenKind>` or lookahead condition in the parser, check whether the catalog already encodes the distinction.**

| Parser need | Catalog source |
|---|---|
| Which tokens lead a construct | `Constructs.ByLeadingToken.Keys` |
| Which tokens disambiguate constructs with the same leader | `DisambiguationEntry.DisambiguationTokens` (via `ConstructMeta.Entries`) |
| Which tokens are valid modifier keywords | `Modifiers.All.OfType<ValueModifierMeta>().Select(m => m.Token.Kind)` |
| Which tokens are type keywords | `Types.ByToken.Keys` |
| Which tokens are action keywords | `Actions.All.Select(a => a.Token.Kind)` |
| Operator precedence and associativity | `Operators.All` |

**Canonical example — `in`/`to` qualifier disambiguation:**

```csharp
// Derived from Constructs.ByLeadingToken — never hardcoded.
// Maps each qualifier-preposition token that is also a declaration leader to the
// catalog-derived set of its disambiguation verbs.
//   In → {Ensure, Modify, Omit}
//   To → {Arrow, Ensure}
internal static readonly FrozenDictionary<TokenKind, FrozenSet<TokenKind>>
    AmbiguousQualifierPrepositions =
        new[] { TokenKind.In, TokenKind.To }
            .Where(Constructs.ByLeadingToken.ContainsKey)
            .ToFrozenDictionary(
                k => k,
                k => Constructs.ByLeadingToken[k]
                    .Where(c => c.Entry.DisambiguationTokens is { IsDefaultOrEmpty: false })
                    .SelectMany(c => c.Entry.DisambiguationTokens!.Value)
                    .ToFrozenSet());
```

When a new `in`-construct is added to the catalog (e.g., `in State flag Field`), its disambiguation token is automatically included in `AmbiguousQualifierPrepositions` — the parser adapts for free. No parser edit required.

**The failure mode to avoid:** a hardcoded `FrozenSet<TokenKind>` in the parser that encodes `{Modify, Omit, Ensure}` directly. This is catalog knowledge in the wrong layer — it becomes stale the moment a new construct is added.

---

### TypeChecker-catalog integration pattern

Before writing any `switch` on modifier, type, function, operator, or action identity in the type checker, check whether the catalog already carries the distinction as metadata.

| TypeChecker need | Catalog source |
|---|---|
| Modifier applicability to a type | `ValueModifierMeta.ApplicableTo` |
| Modifier mutual exclusion | `ModifierMeta.MutuallyExclusiveWith` |
| Modifier subsumption | `ValueModifierMeta.Subsumes` |
| Access-mode semantics | `AccessModifierMeta.IsPresent`, `IsWritable` |
| Anchor scope and target | `AnchorModifierMeta.Scope`, `Target` |
| Function name resolution | `Functions.FindByName(name)` → `FunctionMeta.Overloads` → `FunctionOverload.Match`; CI-qualified lookup additionally uses `FunctionMeta.HasCIVariant` / `CIVariantOf` |
| Operator resolution | `Operations.FindUnary(op, operandType)` / `Operations.FindCandidates(op, lhs, rhs)` |
| Type widening compatibility | `TypeMeta.WidensTo` |
| Type traits (orderable, equality, choice-element) | `TypeMeta.Traits` |
| Type qualifier shape | `TypeMeta.QualifierShape` |
| Implied modifiers for a type | `TypeMeta.ImpliedModifiers` |
| Field accessor signatures | `TypeMeta.Accessors`, `TypeAccessor.ParameterType`, `TypeAccessor.RequiredTraits` |
| Action legality and shape | `ActionMeta.ApplicableTo`, `AllowedIn`, `SyntaxShape`, `ValueRequired`, `PrimaryActionKind`; slot structure via `Actions.GetShapeMeta(SyntaxShape)` |
| Proof obligations (all sources) | `BinaryOperationMeta.ProofRequirements`, `FunctionOverload.ProofRequirements`, `TypeAccessor.ProofRequirements`, `ActionMeta.ProofRequirements` |

**The failure mode:** `switch (modifierKind) { case ModifierKind.Nonnegative: /* check applies to number */ ... }` or `switch (typeKind) { case TypeKind.Integer: ... }`. These are catalog-known facts displaced into stage logic. Use `ValueModifierMeta.ApplicableTo` and `TypeMeta.Traits` instead.

---

### GraphAnalyzer-catalog integration pattern

Before hardcoding state or event modifier semantics into graph algorithms, check catalogs:

| GraphAnalyzer need | Catalog source |
|---|---|
| Whether a state modifier allows outgoing transitions | `StateModifierMeta.AllowsOutgoing` |
| Whether a state modifier requires a dominator state | `StateModifierMeta.RequiresDominator` |
| Whether a state modifier prevents back-edges | `StateModifierMeta.PreventsBackEdge` |
| Which graph analysis an event modifier triggers | `EventModifierMeta.RequiredAnalysis` (e.g. `GraphAnalysisKind.InitialEventCompatibility`) |
| Modifier metadata lookup | `Modifiers.GetMeta(kind)` → pattern-match on `StateModifierMeta` / `EventModifierMeta` |

Graph algorithms themselves (reachability, dominator trees, SCCs) are generic machinery and stay hand-written. Only the *meaning* of each modifier is catalog-driven.

---

### ProofEngine-catalog integration pattern

Proof obligations are declared in catalog metadata — never hardcoded per operator/function/accessor/action. Before writing obligation lists for any construct:

| ProofEngine need | Catalog source |
|---|---|
| Obligations for a binary operator | `BinaryOperationMeta.ProofRequirements` |
| Obligations for a function overload | `FunctionOverload.ProofRequirements` |
| Obligations for a type accessor | `TypeAccessor.ProofRequirements` |
| Obligations for an action | `ActionMeta.ProofRequirements` |
| Obligation dispatch by kind | `ProofRequirements.GetMeta(kind)` |

Subject shape (what the obligation applies to) is encoded in the requirement instance (`ParamSubject`, `SelfSubject`, etc.) — do not hardcode per-requirement subject logic.

---

### Evaluator-catalog integration pattern

| Evaluator need | Catalog source | Status |
|---|---|---|
| Constraint activation timing | `Constraints.GetMeta(kind)` → `ConstraintMeta` DU subtype | Available |
| Modifier boundary validation | `ValueModifierMeta.ApplicableTo`, `HasValue` | Available |
| Accessor signatures | `TypeMeta.Accessors`, `TypeAccessor.ParameterType`, `RequiredTraits` | Available |
| Access mode enforcement | `FieldDescriptor.AccessModes` | Pending (see Open Questions) |
| Operation dispatch | `Operations.FindUnary` / `FindCandidates` | Delegate design pending |
| Function dispatch | `Functions.GetMeta(kind)` | Delegate design pending |
| Action dispatch | `Actions.GetMeta(kind)` | Delegate design pending |

Execution dispatch delegate design is pending. The evaluator's core loop (expression tree walking, working copy, atomicity) remains hand-written.

---

### PreceptBuilder-catalog integration pattern

The Precept Builder transforms analysis artifacts into the executable runtime model. Its catalog integrations:

| Builder need | Catalog source |
|---|---|
| Constraint activation bucket routing | `Constraints.GetMeta(kind)` → `ConstraintMeta` DU subtype (`Invariant`, `StateAnchored`, `EventPrecondition`) |
| Constraint anchor family grouping | `ConstraintMeta.StateAnchored` — groups `StateResident`, `StateEntry`, `StateExit` |
| Action plan shape determination | `ActionMeta.SyntaxShape` — drives `ActionPlan` opcode emission |
| Modifier lists on descriptors | `Modifiers.GetMeta(kind)` — populates `FieldDescriptor.Modifiers[]` |
| Construct model contribution | `ConstructMeta.ModelContribution` (pending — see Open Questions) |

**The failure mode:** Hardcoding which ConstraintKind values are "state-scoped" in the builder's routing logic. Use `Constraints.GetMeta(kind)` and pattern-match on `ConstraintMeta` DU subtypes — the subtype IS the routing decision.

---

### LanguageServer-catalog integration pattern

The language server produces editor artifacts from catalog metadata and compiler output. Its catalog integrations:

| LS feature | Catalog source |
|---|---|
| Semantic token classification (Pass 1) | `TokenMeta.VisualCategory` + `SemanticTokenTypes.GetMeta(...).CustomType` — maps token kinds to LSP custom token types |
| Completion candidates — type position | `Types.All` |
| Completion candidates — modifier position | `Modifiers.All` filtered by `ApplicableTo(constructKind)` |
| Completion candidates — action verb position | `Actions.All` |
| Completion candidates — function position | `Functions.All` |
| Hover text for keywords | Routed to higher-level catalog: `TypeMeta` / `ValueModifierMeta` / `OperatorMeta` / `ActionMeta` / `FunctionMeta` `.HoverDescription` per CC#19 (`TokenMeta` carries no `HoverDescription` field by design) |
| Hover text for types | `TypeMeta.HoverDescription` |
| Hover text for functions | `FunctionMeta.HoverDescription` |
| TextMate grammar patterns | `TokenMeta.VisualCategory` + `SemanticTokenTypes.GetMeta(...).TextMateScope` (plus grammar-local structural patterns where needed) |

**The failure mode:** Hardcoding completion candidate lists in LS code. The LS derives candidates from catalogs — new language features appear automatically in completions when added to the catalog.

---

## Open Questions / Implementation Notes

The following catalog additions have been identified by pipeline stage design documents but are not yet implemented. Each entry specifies what's being added, the proposed shape, which consumer reads it, and implementation steps.

### ValueModifierMeta.ProofSatisfactions

**Source:** `docs/compiler/proof-engine.md` §7 Strategy 2: Modifier Proof

**Status:** ✅ Resolved (CC#5, 2026-05-06; renamed ProofDischarges → ProofSatisfactions in Phase A)

`ProofSatisfaction[] ProofSatisfactions = []` on `ValueModifierMeta` carries the discharge table used by proof engine Strategy 2. The `ProofSatisfaction` abstract DU record (with `Numeric`, `Presence`, `Dimension`, `Modifier`, `QualifierCompatibility` subtypes) is defined in `ProofRequirement.cs`. See the `ValueModifierMeta` shape definition above for the canonical form.

**Implementation checklist:**
- [x] Add `ProofSatisfaction` DU to `src/Precept/Language/ProofRequirement.cs`
- [x] Add `ProofSatisfactions` property to `ValueModifierMeta` (canonical shape above)
- [x] Update `Modifiers.GetMeta()` entries with satisfaction tables per modifier

---

### ConstructMeta.ModelContribution (Candidate)

**Source:** `docs/runtime/precept-builder.md` §13 Open Questions

**Purpose:** Make the Precept Builder's assembly loop fully generic by declaring what each construct contributes to the `Precept` model.

**Status:** Candidate — value is marginal for ~12 constructs. Pending owner ruling.

**Proposed shape:**

```csharp
public enum ModelContribution
{
    None,              // No model contribution
    DeclaresField,     // Adds a FieldDescriptor
    DeclaresState,     // Adds a StateDescriptor
    DeclaresEvent,     // Adds an EventDescriptor
    AddsTransition,    // Adds an ExecutionRow
    AddsConstraint,    // Adds a ConstraintDescriptor
    AddsAccessMode,    // Modifies FieldDescriptor access mode
    AddsStateHook,     // Adds entry/exit actions
}

// Add to ConstructMeta:
ModelContribution Contribution
```

**Consumer:** PreceptBuilder dispatches on `construct.Meta.Contribution` instead of `ConstructKind`.

**Implementation checklist:**
- [ ] Owner decision on marginal value vs. catalog complexity
- [ ] If approved: Add `ModelContribution` enum and `ConstructMeta.Contribution` property
- [ ] If approved: Update `Constructs.GetMeta()` entries
- [ ] If approved: Refactor builder to dispatch on contribution

---

### FieldDescriptor.AccessModes

**Source:** `docs/runtime/evaluator.md` §7.5 Access Mode Enforcement

**Purpose:** Enable the evaluator to check field access modes without re-deriving from modifiers at runtime.

**Proposed shape:**

```csharp
public enum AccessMode { Writable = 1, ReadOnly = 2, Omit = 3 }

// Add to FieldDescriptor:
IReadOnlyDictionary<StateDescriptor?, AccessMode> AccessModes
```

**Consumer:** Evaluator reads `field.AccessModes[currentState]` during Update operations for O(1) access mode lookup.

**Implementation checklist:**
- [ ] Add `AccessMode` enum to `src/Precept/Runtime/Descriptors.cs`
- [ ] Add `AccessModes` property to `FieldDescriptor`
- [ ] Update Precept Builder to resolve access modes during descriptor pass
- [ ] Update evaluator's `Update` operation to use `AccessModes`

**Open question:** Dictionary vs. array representation — dictionary is O(1) but has overhead; array is cache-friendly but O(n). For typical 3–10 states, difference is negligible. Recommend dictionary for clarity.

---

### Catalog documentation strings (HoverDescription) — CC#19

**Source:** `docs/tooling/language-server.md` § 7.4 Hover Design

**Status:** ✅ Resolved (CC#19, 2026-05-06) — TokenMeta.HoverDescription deliberately NOT added.

`HoverDescription` ships on **5 of the 6 catalogs** that feed LS hover:

| Catalog | `HoverDescription` field | Source |
|---|---|---|
| `TypeMeta` | ✅ present (nullable string) | `src/Precept/Language/Type.cs:218` |
| `FunctionMeta` | ✅ present (nullable string) | `src/Precept/Language/Function.cs:46` |
| `OperatorMeta` (and `SingleTokenOp`/`MultiTokenOp` subtypes) | ✅ present (nullable string) | `src/Precept/Language/Operator.cs:47, 66, 87` |
| `ValueModifierMeta` | ✅ present (nullable string) | `src/Precept/Language/Modifier.cs:137` |
| `ActionMeta` | ✅ present (nullable string) | `src/Precept/Language/Action.cs:20` |
| `TokenMeta` | ❌ **deliberately absent** | `src/Precept/Language/Token.cs` (no field) |

**Structural reason for the asymmetry.** Token-level hover routes through the higher-level catalog that the token resolves to, not through `TokenMeta` directly. When the editor hovers a keyword in source, the language server resolves the token to its semantic referent — a type keyword (`money`) routes to `TypeMeta.HoverDescription`; a modifier keyword (`nonnegative`) routes to `ValueModifierMeta.HoverDescription`; an operator (`==`) routes to `OperatorMeta.HoverDescription`; an action verb (`add`) routes to `ActionMeta.HoverDescription`. There is no token whose hover content is *only* expressible at the token level: every keyword is either a type, a modifier, an operator, an action, or a structural keyword whose role is described by the construct that contains it. Adding a `HoverDescription` field to `TokenMeta` would duplicate the catalog-level descriptions and create a per-token override site whose drift would silently degrade hover quality. The fall-through default (`meta.HoverDescription ?? meta.Description`) at every hover-provider site uses the higher-level catalog's `Description` when no per-member `HoverDescription` is authored — making the route to `TokenMeta`-only content structurally unnecessary.

**Hover routing canonical doc:** see `docs/tooling/language-server.md § 7.4 Hover Design` for the full per-token-category dispatch table and interval-hover (proof-engine bound display) behavior. Both `hover-design.md` and `interval-hover-design.md` from the Archive are promoted there.

---

## Cross-References

| Document | Relationship |
|---|---|
| [Compiler and Runtime Design](../compiler-and-runtime-design.md) | Architectural grounding — metadata-driven identity established here |
| [Diagnostic System](../compiler/diagnostic-system.md) | Catalog 13 — `DiagnosticCode` + `DiagnosticMeta` shapes defined there |
| [Fault System](../runtime/fault-system.md) | Catalog 14 — `FaultCode` + `FaultMeta` shapes; StaticallyPreventable chain |
| [Graph Analyzer Roadmap](graph-analyzer-roadmap.md) | Deferred event/state graph-property modifiers — referenced from §6 Modifiers > GraphAnalysisKind |
| [Language Server](../tooling/language-server.md) | § 7.4 Hover Design — token-level hover routing (referenced from §Open Questions > CC#19) |
| [Language Spec](precept-language-spec.md) | Consumers of catalog metadata for each pipeline stage |
| [Primitive Types](primitive-types.md) | `Types` catalog is the machine-readable version of primitive type rules |

