# Canon against canon, check 1: types declared in more than one document

**Run 2026-08-27** against HEAD `43fa6905`. The check is
[`checks/declared-types.py`](checks/declared-types.py) and re-running it reproduces the list below.

## Why this check exists

Every search this phase has run starts from a claim in the want document. That is what the phase was
scoped to, and it is why nothing ever went looking for two canonical documents disagreeing with each
other on their own account — the eleven rows carrying the intra-canon marker all arrived from the
2026-07-14 input list or as a by-product of chasing a want disagreement.

A canon-against-canon search cannot be written the same way, because there is no claim list to start
from. What it can be is **mechanical**: find the facts canon states more than once, and compare the
statements. This is the first such check and the highest-yield one. A type declared in a fenced code
block in two documents is the same fact stated twice, and the two statements either match or they do not.

**What makes it comprehensive rather than sampled**: it reads every fenced block in the canonical set,
so no document is favoured and no reader's attention decides the coverage. **What it does not establish**
is that these are all the canon-against-canon defects — only that this class of them is enumerated.

## What it found

235 type names are declared in a fenced block somewhere in the canonical set. **87 are declared in more
than one document.** Of those, **20 have member sets that are not identical**, and reading each one
sorts them into three groups.

### Group A — genuine disagreements: both declarations present as complete

Neither side carries an elision marker, so a reader has no way to know which to believe.

| Type | The disagreement |
|---|---|
| `ProofRequirement` | **The base record differs.** `catalog-system.md:1117` — `ProofRequirement(ProofSubject Subject, string Description)`. `proof-engine.md:493` — `ProofRequirement(ProofRequirementKind Kind, string Description)`. The subtypes chain accordingly: `: ProofRequirement(Subject, Description)` against `: ProofRequirement(ProofRequirementKind.Numeric, Description)`. **And `catalog-system.md` contradicts itself**: its class diagram at `:596` gives the base `Kind : ProofRequirementKind`, agreeing with `proof-engine.md` and disagreeing with its own C# five hundred lines later. |
| `ConstraintDescriptor` | Two declarations sharing two members out of seven. `precept-builder.md:518` — `Kind, StateAnchor, EventAnchor, Expression (ExecutionPlan), Because, Source (SourceSpan)`. `runtime-api.md:423` — `Kind, ScopeTarget, ExpressionText, Because, ReferencedFields, HasGuard, SourceLine`. One holds a compiled plan and typed anchors; the other holds source text and a string scope name. |
| `ConstraintInfluenceMap` | Declared with **inverted contents**. `proof-engine.md:2162` — `FieldToConstraints` and `ArgToConstraints`, keyed by strings, `FrozenDictionary`. `precept-builder.md:563` — `ConstraintToFields` and `FieldToConstraints`, keyed by descriptor objects, `ImmutableDictionary`. Only one member name is common, and it is keyed differently in each. |
| `TokenMeta` | `catalog-system.md:749` carries `VisualCategory` as a `SemanticTokenTypeKind?`, plus `IsStateWildcard`, `IsFieldBroadcast`, `IsFunctionCallLeader`, and `IsValidAsMemberName` as a **computed property**. `tooling-surface.md:366` carries `TextMateScope` and `SemanticTokenType` as two strings, has none of the three flags, and makes `IsValidAsMemberName` a constructor parameter. This one has a consumer: the grammar generator produces `tmLanguage.json` from `Tokens`, and its input contract is stated two ways. |
| `ArgDescriptor` | Three declarations, two shapes. `precept-builder.md:363` — `SlotIndex, ClrType`. `runtime-api.md:511` and `descriptor-types.md:101` — `DefaultExpression, SourceLine`. |
| `EventDescriptor` | `precept-builder.md:355` carries `IsInitial`; `descriptor-types.md:94` does not, and orders `Modifiers` before `Args`. |
| `FieldDescriptor` | `precept-builder.md:322` — `IsOptional, ClrType, SourceSpan Source`. `descriptor-types.md:78` — `DefaultExpression, int SourceLine`, no `IsOptional`. **Already found by the cold read**, and reaching it independently is the evidence that this check works. Neither declaration carries `AccessModes`, which `evaluator.md:1333` reads — recorded at `descriptor-types.md:109`. |
| `StateDescriptor` | `precept-builder.md:344` — `SourceSpan Source`. `descriptor-types.md:88` — `int SourceLine`. |
| `Diagnostic` | `catalog-system.md:873` declares five members. `diagnostic-system.md:111` declares the same five plus `Args` and an init-only `RelatedSpans`. Both present as the output value type of the pipeline. |
| `FaultMeta` | `diagnostic-system.md:593` — `Code, MessageTemplate`. `fault-system.md:173` — the same two plus `Severity` and `RecoveryHint`. The shorter one appears under a heading calling the switch exhaustive. |
| `ParsedConstruct` | `parser.md:83` — four members including `LeadingTokenKind`. `type-checker.md:89` — three, under the words "Each construct is a generic `ParsedConstruct`". The parser is the document that produces the type. |
| `Prospect` / `ConstraintStatus` | `result-types.md:171` declares both without explicit values; `evaluator.md:402` assigns `= 1, 2, 3` to each. Lowest severity here, and still a difference a serializer would notice. |

**A pattern across four of these, worth one decision rather than four.** `precept-builder.md` gives every
descriptor a `SourceSpan Source`; `descriptor-types.md` gives every descriptor an `int SourceLine`.
`ArgDescriptor`, `EventDescriptor`, `FieldDescriptor` and `StateDescriptor` all split exactly this way, and
the same split was already recorded for `FaultSiteDescriptor` (`precept-builder.md:598` against
`descriptor-types.md:110`). One question — what a descriptor carries to point at source — answered two ways
in five places.

### Group B — legitimately abbreviated, and marked as such

The declaration carries an elision, so no reader is misled. No row needed.

`StateGraph` (`proof-engine.md:210`, "`// ... topology and structural analysis ...`"), and `TypedField`,
`TypedEvent` and `TypedState` (`language-server.md:838`, `:843`, `:848`, all "`// ... semantic properties
...`", showing only the back-pointer the section is about).

`StateModifierMeta` belongs here too and the abbreviation is on the other side from where the check first
put it: `graph-analyzer.md:359` declares it in full, while `catalog-system.md:471` is a class diagram
listing three members it needs for the diagram's purpose and `:800` is a one-line sketch written with
literal `...` in the parameter list. Nothing disagrees.

### Group C — two partial views, neither marked

`Precept`. `precept-builder.md:206` shows descriptor tables, identity markers and five internal execution
structures, and no operations. `runtime-api.md:212` shows construction and the four operations and the
query surface, and none of the internal structures. Each is right about what it covers and neither says it
is partial — and `runtime-api.md:210` introduces its version with "It exposes the full definition-level
query surface". Lower severity than group A: nothing here contradicts, but a reader taking either as the
class has an incomplete picture with no warning.

## What this leaves

**Twelve entries in group A, one of them already a known finding.** Eleven are new. None of them can be
resolved by choosing a wording — each needs somebody to say which shape is right, which makes them rows
whose resolution needs a decision rather than rows a correction pass can execute.

**The next checks of this kind, not yet built**: counts stated about a set against the set itself (the
diagnostic document declaring 147 codes and stating 164 is this shape); an identifier attributed by one
document to another document's stage, against whether that document knows it (`StructuralSinkState` is
this shape); and an ordering or numbering stated more than once (`precept-builder.md` numbering its own
passes two ways). All three shapes are already represented among the cold read's findings, which is the
evidence that each is worth a check rather than a reader.
