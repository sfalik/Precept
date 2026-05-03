# George's Review: Frank's Catalog-Driven Pipeline Analyses

**Author:** George (Runtime Dev)
**Date:** 2026-05-02
**Subject:** Implementer's review of Frank's two catalog-bias analyses — type checker and parser
**Status:** Active working document

---

## § 1: Frank's Type Checker Analysis — Implementer Verdict

### 1.1 Where Frank Is Right

**§ 1.3 Widening as runtime computation — confirmed, P0, no debate.** Frank's claim: Precept has 3 widening edges total (`integer → decimal`, `integer → number`; that's 2 direct, plus any transitive path). The 6-step fallback cascade (`direct → left-widen → right-widen → both-widen`) is algorithm for an unbounded graph applied to a 2-edge set. Precompute `FrozenDictionary<(OperatorKind, TypeKind, TypeKind), ResolvedOperation[]>` at startup including widened triples. The widening algorithm ceases to exist as runtime logic. Frank is completely right. Implementation cost: ~30-40 lines of startup code. Risk: near zero.

**§ 1.2 Overload resolution — confirmed over-engineered.** For 15 functions × 2-5 overloads × widening variants = maybe 120 total entries. A `FrozenDictionary<(FunctionKind, TypeKind[]), FunctionOverload>` precomputed at startup handles this. Arity filter + exact + widened collapses into one lookup. Implementation cost: ~40 lines. Risk: low. Frank is right.

**§ 2.4 Modifier validation as a generic loop — confirmed already exists in catalog, confirmed ~20 lines.** `FieldModifierMeta.ApplicableTo`, `.MutuallyExclusiveWith`, `.Subsumes` are real. Slice 7 should never be architected as a dedicated bespoke validator. Frank's generic loop sketch at § 2.4 is essentially correct. This is the clearest win in the document — zero new catalog metadata, just use what's there. The only thing Frank misses: `ApplicableTo` isn't a type target set that you can evaluate without resolving the field's type first, so Pass 1 must have completed TypeRef resolution before this loop runs. Sequencing dependency, not a blocker.

**§ 1.1 `Resolve()` as a giant switch — confirmed real smell.** The type-checker spec's `Resolve()` function lists 16 expression form match arms. The ExpressionForms catalog classifies every form. Frank is right that `[HandlesCatalogMember]` is an enforcement mechanism, not a design driver. The `ResolutionShape` DU proposal (§ 2.1) — `CatalogLookupResolution`, `FixedTypeResolution`, `PropagationResolution`, `StructuralResolution` — is architecturally sound.

**Frank's confirmation of prior George finding: `TypedActionShape` (§ 1.5)** — Frank independently identifies the 3-arm `ActionSyntaxShape → TypedAction DU` switch as catalog-expressible. This validates my Slice 5 finding. The gap is real and Frank found it independently. However, Frank classifies it as a deprioritized gap from Gap 3 without calling out that the checker will hardcode this switch if we don't resolve it before implementation. That makes it a blocker, not a footnote.

### 1.2 Where Frank Is Over-Reaching

**§ 4.5 "Type Checker as Catalog Consumer Only" / § 4.1 "No Resolve Function"** — The radical proposals hit a wall at `IdentifierExpression` and `ParenthesizedExpression`. Frank's own `StructuralResolution()` arm is a catch-all for "identifier (symbol table lookup), grouped (propagation)." But symbol table lookup IS the hard part of the checker — it's where scope rules, forward-reference checking, binding resolution, and error recovery all live. Wrapping it in a `StructuralResolution` subtype doesn't make it smaller; it just renames it. A `FrozenDictionary<ExpressionFormKind, IResolutionStrategy>` is a valid dispatch mechanism, but the implementations behind it are the same code that would live in `Resolve()`'s match arms. The claim that this reduces `Resolve()` to "dispatching to 4 small strategy classes" understates how much logic lives in the structural cases.

**§ 3.7 "10-slice plan is over-scoped"** — Frank argues Slices 2-4 (binary ops, functions, typed constants) are "the same algorithm hitting different catalog indexes." True at an abstract level. But the slice plan exists to enforce testability gates — each slice is a testable, green checkpoint with a defined test suite. Collapsing slices 2-4 into one large slice produces 150+ tests that must all be written and pass simultaneously. That's not faster; it's a testing bottleneck. The slice plan is about layered-correct delivery, not structural uniqueness. Frank's "too much ceremony" argument applies to a codebase where you can ship the whole checker at once, not to an iterative implementation under test.

**§ 2.3 Construct-Declared Scope Rules — valid but overstated as P1.** Frank proposes `ScopeRule?` on `ConstructMeta` to replace manual per-construct scope setup. The 4 scope situations are real and encodable. But the proposal as stated is underspecified: `EventArgScope(ConstructSlotKind EventSlot)` presumes the checker navigates constructs by slot, which requires either the slot-based checker architecture (§ 4.3) to already be in place, or a separate slot-to-expression traversal. If the checker walks `TypedTransitionRow` directly, `ConstructMeta.ScopeRule` is metadata the checker reads once but still must apply with explicit Enter/Exit calls. It simplifies the code but doesn't eliminate the scope-setup calls. The P1 claim implies "must be done before the checker is written." I'd call it P1-conditional: must land before Slice 3, or the checker writes manual scope setup that gets thrown away. See § 6 for the dependency.

### 1.3 Frank Confirms Prior George Findings (Partially)

My prior type-checker review identified:
- **`TypedActionShape` enum on `ActionMeta`** — Frank confirms (§ 1.5). Still a Slice 5 blocker.
- **`LiteralRange?` on `TypeMeta`** — Frank does NOT address this. The out-of-range numeric literal check is now locked (range validation against representable range from `TypeMeta`), but Frank's analysis doesn't identify `TypeMeta.LiteralRange?` as a missing field. It's still outstanding — Slice 4 blocker, not yet in any catalog.
- **`ContentValidation DU`** — Frank does NOT address this either. Typed-constant content validation (bounds on `integer in 0..100`, regex-like constraints, enum membership) is still a Slice 4 gap that Frank's analysis skips entirely.

**Frank's independent confirmation of the action-shape smell validates it as a real gap. Frank's silence on `LiteralRange?` and `ContentValidation` means those gaps remain unaddressed by both analyses.**

---

## § 2: Frank's Parser Analysis — Implementer Verdict

### 2.1 Where Frank Is Right

**§ 1.2 / § 3.4 Action-kind switch ceremony — confirmed real, confirmed P1.** The source is unambiguous:

- `ParseAssignValueStatement` (Parser.Declarations.cs:327–346): 1 valid arm (`Set`), 14 throw arms
- `ParseCollectionValueStatement` (Parser.Declarations.cs:381–400): 5 valid arms (with inline kind-identity branches at 355–378), 9 throw arms
- `ParseFieldOnlyStatement` (Parser.Declarations.cs:455–473): 1 valid arm (`Clear`), 13 throw arms
- `ParseCollectionIntoStatement` (Parser.Declarations.cs:425–446): 2 valid arms, 12 throw arms

That's ~120 lines of throw-arm ceremony confirmed. Frank's proposal: one `Statement` type per `ActionSyntaxShape` carrying `ActionMeta`. This is correct in intent. Implementation cost: low — it's a node type consolidation with no behavioral change. Risk: downstream stages read `ActionMeta` from the node anyway; the switch disappears.

**§ 1.1 Dual-paradigm parsing — partially confirmed.** The evidence: `ParseStateDeclaration` (Parser.Declarations.cs:554–563), `ParseEventDeclaration` (565–574), `ParseRuleDeclaration` (576–585) — all 3 use the generic `ParseConstructSlots` path. `ParseFieldDeclaration` (510–552) is the outlier. Frank is right that the dual paradigm exists. However, the gap is smaller than Frank implies: 3 of 4 direct constructs are already correct. The real problem is isolated to `ParseFieldDeclaration` and the split-modifier pattern. See Hidden Gaps below.

**§ 3.5 `StructuralBoundaryTokens` derivability — confirmed real.** Parser.cs lines 104–108: `{ When, Because, Arrow, Ensure, EndOfSource }`. These are all slot-leader tokens (`GuardClause`→`When`, `BecauseClause`→`Because`, `ActionChain`/`ComputeExpression`/`Outcome`→`Arrow`, `EnsureClause`→`Ensure`) plus `EndOfSource`. The derivation from slot metadata is straightforward: collect all leader tokens across all slot kinds. Risk of derivation: low. This should be P1 because it's a drifting hand-maintained set — add a new slot kind with a new leader and it silently breaks expression termination.

**§ 3.3 `ParseTypeRef` 5-branch dispatch — confirmed over-sized.** The method (Parser.Declarations.cs:901–1091) is 190 lines. The 5 branches are real: Tilde (`~string`), LookupType, `SimpleCollectionTypeLeaders`, `ChoiceType`, scalar/TypeKeywords. Frank's `TypeParseShape` DU on `TypeMeta` is the right direction. The current dispatch already uses token identity — this just makes the shape explicit and catalog-declared.

**§ 1.3 Pratt loop hardcodes — partially confirmed.** Member access (`.`) at line 48: `if (minPrecedence > 80) break;` — **hardcoded 80, confirmed.** Method call (`(`) at line 82: `if (minPrecedence > 90) break;` — **hardcoded 90, confirmed.** However, Frank claims `is set` / `is not set` also hardcode precedence. That is **wrong.** Line 60: `if (minPrecedence > Operators.ByTokenSequence(TokenKind.Is, TokenKind.Set)!.Precedence) break;` — the `is set` branch already reads precedence from the catalog. Frank's analysis misread this. The actual issue is only `.` (80) and `(` (90).

### 2.2 Where Frank Is Over-Reaching

**§ 3.1 Unified slot-based `ParseFieldDeclaration` — proposal understates implementation cost.** Frank says "express its post-expression-modifier logic as slot metadata (`SplitAroundSlot` relationship)." But look at what `ParseFieldDeclaration` actually does (lines 522–537):

```
[pre-modifiers] → [compute expression] → [post-modifiers]
```

The modifier slot appears **twice**, split around an optional compute expression. The current `ConstructMeta.Slots` for `FieldDeclaration` has `[SlotIdentifierList, SlotTypeExpression, SlotModifierList, SlotComputeExpression]` — a single `ModifierList` slot, not two. Encoding the split as metadata requires either:
- A new `SlotPosition` or `SplitAroundSlot` field on `ConstructMeta` — non-trivial new metadata design
- Two `ModifierList` slots (pre and post), which breaks `BuildNode`'s slot indexing assumptions and requires both slots to be merged on assembly

Neither option is "just express it as metadata." It's a catalog redesign for one construct. Until that design is settled, pulling `ParseFieldDeclaration` into the generic path risks producing a construct parser that silently drops post-compute modifiers. This is **not P1** — it's P2 contingent on the split-modifier design being settled first.

**§ 5.1 Declarative Grammar Machine and § 5.4 PEG interpreter — optimistic sizing.** Frank claims "~80 lines instead of ~400." The stashed-guard pattern (pre-anchor guards parsed before the disambiguation token in `DisambiguateAndParse`) doesn't fit a flat PEG sequence. The `ParseCollectionValueStatement` inline kind-identity branches (`Remove...at`, `Append...by`, `Enqueue...by`) are parse-time variant detection that requires knowing the `ActionKind` mid-parse. Neither fits a simple slot-sequential `LeaderToken/ConsumesLeader` model. Realistic estimate for the DGM: 150-200 lines of slot interpreter after honest accounting for these edge cases. The `ParseFieldDeclaration` split-modifier problem alone requires PEG `Opt(Repeat(...))` combinators, which adds complexity to the metadata model. P3 is the right priority.

**§ 3.2 Pratt loop Operators as uniform catalog entries — small payoff.** The method call branch (lines 78–106) also checks `left is MemberAccessExpression` — it's not just a precedence comparison, it's a structural check on the left-expression type. Even with `LeftDenotationParsing` metadata, the parser would need to check "was the left expression a member access?" somewhere. The metadata encodes the *behavior* but the *check* is still structural. Frank's proposal is cleaner but the payoff is removing 2 hardcoded precedence constants. That's P2/P3 territory.

### 2.3 What Frank Got Flat Wrong

**§ 1.3 claim about `is set` / `is not set`** — Frank says these are "hand-written branches with hardcoded precedence values." The source (Parser.Expressions.cs:60) shows the `is set` branch reads precedence from `Operators.ByTokenSequence(...)` — entirely catalog-driven. This is a factual error in the analysis. The only hardcodes are `.` = 80 and `(` = 90.

---

## § 3: Hidden Gaps Frank Missed (Both Analyses)

### 3.1 Inline Kind-Identity Checks Inside Shape Parsers (Parser)

Frank identifies the throw-arm ceremony in the shape dispatch switches. He misses the **inline kind-identity checks buried inside the shape parsers themselves**:

- `ParseCollectionValueStatement` line 355: `if (meta.Kind == ActionKind.Remove && Current().Kind == TokenKind.At)` — switches on `ActionKind` identity mid-parse
- Line 365: `if (meta.Kind == ActionKind.Append && Current().Kind == TokenKind.By)` — same
- Line 373: `if (meta.Kind == ActionKind.Enqueue && Current().Kind == TokenKind.By)` — same
- `ParseCollectionIntoStatement` line 415: `if (meta.Kind == ActionKind.Dequeue && Current().Kind == TokenKind.By)` — same

These are variant-action detection points where the parser reads the post-field token (`at`, `by`) and dispatches to the variant node constructor. The catalog smells the same as the throw-arms: per-kind identity checks that should be driven by metadata. If we ever add a new variant action, these checks silently fail to route unless a developer knows to add another `if (meta.Kind == ...)` here. Frank's proposal for uniform action-shape `Statement` types doesn't address these — they're pre-switch divergence points.

The fix: `ActionMeta` needs a `VariantTriggerToken?: TokenKind` field (or similar) so the parser can ask the catalog "does this action have a variant triggered by token X?" without identity-switching. This is a small catalog addition but a real one Frank missed.

### 3.2 `ParseOutcomeNode` Hardcodes Three Shapes (Parser)

`ParseOutcomeNode` (Parser.Declarations.cs:146–?) dispatches on `TokenKind.Transition`, `TokenKind.No` (`no transition`), and `TokenKind.Reject`. These are hardcoded without any Outcomes catalog. Frank's parser analysis completely skips outcome parsing. This is GAP-062 that I filed in my iteration-11 audit. Frank's P1–P3 table doesn't mention it. The three outcome shapes are hardcoded in both the parser AND the checker's `TransitionOutcome` enum.

### 3.3 `BuildNode` Extra-Slot Tokens (Parser)

`BuildNode` (Parser.cs:513–545) has two arms with `default` values injected for tokens parsed *outside* the slot array:

- `ConstructKind.StateEnsure` (line 514): `default` for the preposition token (was consumed in `DisambiguateAndParse` before slot parsing began)
- `ConstructKind.StateAction` (line 531): same `default` for preposition token

Any unification of parsing through the generic slot path must account for these "extra-slot" tokens that carry semantic meaning (`in` vs `to` vs `from` for state ensures/actions). Frank's unified-slot proposal doesn't address how the preposition token, consumed before `ParseConstructSlots` is called, gets threaded through to `BuildNode`. This is a correctness hole in the P1 unification proposal.

### 3.4 `TypeMeta.LiteralRange?` — Missing Catalog Field, Slice 4 Blocker (TypeChecker)

Frank's type-checker analysis is silent on out-of-range literal validation. The team decision locked range checking to Slice 4 (validate numeric literal values against `TypeMeta`'s representable range). But `TypeMeta` doesn't have a `LiteralRange?` field yet. This is a hidden blocker: without `TypeMeta.LiteralRange?`, the checker cannot implement range validation without hardcoding per-type ranges inline. Frank's analysis doesn't call this out. This must land before Slice 4.

### 3.5 `ContentValidation DU` — Missing from Both Analyses (TypeChecker)

Neither Frank's analysis nor my prior review fully specifies the `ContentValidation` structure needed for `TypedConstantExpression` and `InterpolatedTypedConstantExpression` checking. The Slice 4 checker must validate typed constants (e.g., currency codes, unit values) against some domain constraint. That constraint doesn't exist as catalog metadata anywhere visible in the current source. This is a Slice 4 blocker that both analyses have noted but neither has proposed a concrete catalog solution for.

### 3.6 `QualifierMatch.Same` Enforcement — Absent from Both Slice Plans (TypeChecker)

GAP-065 from my iteration-11 audit: the Operations catalog declares `QualifierMatch.Same` for operations where both operands must have the same qualifier identity (e.g., `money + money` requires both to be the same currency). Neither Frank's checker analysis nor the type-checker spec identifies this as a checker slice. The check-time logic (`FindCandidates` returns a `SameQualifierRequired` binding; checker must validate that qualifier identities are compatible) is a real missing slice. The precomputed operation table (Frank's § 2.2) will include these entries — the precomputed result type is easy. But the ENFORCEMENT that both operands carry the same qualifier needs explicit slice ownership.

### 3.7 `ParseFieldDeclaration` Split-Modifier Problem — Unresolved Design Gap (Parser)

As noted in § 2.2 above. Frank says "encode the post-expression-modifier logic as slot metadata" without specifying what that metadata looks like. Until someone proposes and approves a concrete `ConstructMeta` extension that handles the pre/post split, Frank's P1 unification proposal cannot be implemented without risking a silent correctness regression on field declarations with post-compute modifiers.

---

## § 4: Radical Proposal Reality-Check

### 4.1 Declarative Grammar Machine (Parser § 5.1) / PEG Interpreter (§ 5.4)

**Catalog metadata additions required:**
- `LeaderToken?: TokenKind` on `ConstructSlot` (which token introduces this slot, e.g., `When` introduces `GuardClause`)
- `ConsumesLeader: bool` on `ConstructSlot`
- `RecoveryStrategy` enum on `ConstructSlot`
- Some mechanism for the stashed-guard pattern (a pre-anchor optional clause that applies to the current construct but is consumed before the anchor is parsed)
- Some mechanism for the split-modifier pattern in `FieldDeclaration`
- Variant-action trigger detection (inline `meta.Kind == X` checks)

**Frank's "~80 lines instead of ~400" claim:** Optimistic by 2×. The stashed-guard pattern and split-modifier pattern require either metadata extensions beyond what Frank specifies, or retained special-case code. Honest estimate: 150-200 lines of slot interpreter + residual special-case handling. Still a significant improvement over 400 lines, but not the extreme reduction claimed.

**Risk:** High. Slot iteration is currently well-tested through `ParseConstructSlots` for 3 constructs. Making it the only path affects all 12 constructs simultaneously. Regression surface is large. The stashed-guard preposition-token threading problem (§ 3.3 above) has no clean catalog solution yet.

**P3 is appropriate.** Don't attempt before all 12 constructs have independent slot-path tests.

### 4.2 Precomputed Operation and Function Resolution Tables (TypeChecker § 2.2, § 3.1, § 3.2)

**Catalog metadata additions required:** None. The data is all there: `Operations.All`, `Types.ByTokenKind` with `WidensTo`. For functions: `Functions.All` with `Overloads` (or equivalent).

**Frank's sizing:** "Iterate Operations.All × WidensTo at startup." The widen graph has 2 edges (`integer → decimal`, `integer → number`). For each operation entry, generate variants: exact, left-widened, right-widened, both-widened. Total entries: ~200 operation entries × 4 widening variants at most = at most ~800 entries, probably fewer because not all ops apply to all widenable types. A `FrozenDictionary<(OperatorKind, TypeKind, TypeKind), ResolvedOperation[]>` keyed by (op, lhs, rhs) including widened combinations. Frank's "LOW" effort claim is correct: ~40 lines of startup code.

**Risk:** Near zero. This is startup computation from deterministic catalog data with an immutable result. If the computation is wrong, every test that involves operations fails immediately.

**P0 is correct. Implement this before the widening fallback algorithm is written by hand.**

### 4.3 `TypeParseShape` DU on `TypeMeta` (Parser § 3.3)

**Catalog metadata additions required:** A `TypeParseShape` DU field on `TypeMeta`:
- `ScalarParse()` — scalar keyword + optional qualifiers
- `CollectionParse(CollectionVariant)` — `X of T [qualifiers] [by P]`
- `LookupParse()` — `lookup of K to V`
- `ChoiceParse()` — `choice of T(options)`
- `CISensitiveParse(TypeKind inner)` — `~ innerType`

Frank's "~80 lines from 190" claim: The current `ParseTypeRef` already dispatches on token identity (5 branches). The `TypeParseShape` DU doesn't eliminate the 5 cases — it declares them in catalog, so `ParseTypeRef` dispatches via a catalog lookup rather than if/else. The 5 handler methods still exist. Reduction is from one 190-line method with embedded logic to one ~40-line dispatch method + 5 ~20-line handler methods = ~140 lines. Better, but not 80.

**Risk:** Low-medium. Type parsing is well-tested. The `TypeParseShape` DU is a pure metadata addition; the parser code change is a dispatch refactor. The `~string` path has a subtlety: `Tilde` is not a `TypeMeta` token — it's a prefix modifier. `CISensitiveParse` needs to live somewhere sensible. Consider making `~string` a first-class `TypeKind.CIString` with its own `TypeMeta` entry rather than a parse-time modifier.

### 4.4 `ResolutionShape` DU on `ExpressionFormMeta` (TypeChecker § 2.1)

**Catalog metadata additions required:** A `ResolutionShape` DU on `ExpressionFormMeta`:
- `CatalogLookupResolution(CatalogSource, LookupStrategy)`
- `FixedTypeResolution(TypeKind)`
- `PropagationResolution(PropagationRule)`
- `StructuralResolution()`

Frank's "~80 lines from 350" claim: The `StructuralResolution` arm covers `Identifier` (symbol table lookup, scope rules, error recovery), `Grouped` (propagation but trivial), `ListLiteral` (element-type checking), `Quantifier` (binding push/pop), `CIFunctionCall` (~string enforcement + function lookup), `TypedConstant`/`InterpolatedTypedConstant` (content validation). That's 7 of 16 forms in the catch-all — nearly half. The generic `Resolve()` doesn't shrink to 80 lines; it shrinks from 350 to maybe 150 with this approach. Still a win, but not the radical simplification claimed.

**Risk:** Medium. Adding `ResolutionShape` metadata while also implementing the checker is a chicken-and-egg risk — you're building the metadata and its consumer simultaneously. I'd implement the baseline checker first, then add `ResolutionShape` as a refactor after all slices are green. This avoids a metadata design error poisoning the initial implementation.

### 4.5 Unified Action-Shape `Statement` Types (Parser § 3.4)

**Catalog metadata additions required:** None new. `ActionMeta.SyntaxShape` already exists and routes dispatch correctly. The change is AST node consolidation.

Frank's "HIGH" impact claim and "MEDIUM" effort: I'd say LOW-MEDIUM on impact (downstream stages already read `ActionMeta` from nodes), MEDIUM on effort (6 `Statement` subtypes replace ~15; downstream code needs audit). The throw-arm ceremony disappears entirely. The inline kind-identity branches (§ 3.1 above) need a separate solution — they're not addressed by node consolidation alone.

This is the cleanest refactor in both proposals. Do it before implementing the checker's Slice 5, so the checker is written against the consolidated AST.

---

## § 5: Priority Corrections

### 5.1 Parser Priorities

| Frank Priority | Frank Proposal | My Verdict |
|---|---|---|
| P1 | Unify `ParseFieldDeclaration` through slot path | **P2** — split-modifier metadata design must be settled first (§ 3.7) |
| P1 | Eliminate action-kind switches | **P1 confirmed** — zero behavioral change, real noise removal |
| P1 | Derive `StructuralBoundaryTokens` from slot metadata | **P0** — should be immediate, it's a drifting hand-maintained set |
| P2 | `TypeParseShape` DU on `TypeMeta` | **P1** — lower risk than field-declaration unification, cleaner payoff |
| P2 | Catalog-ize Pratt loop `.` and `()` branches | **P2 confirmed** — small payoff, medium effort, correct priority |
| P3 | Declarative Grammar Machine | **P3 confirmed** — after all constructs have independent slot-path tests |
| P3 | Uniform action-shape `Statement` types | **P1** — must land before Slice 5 checker implementation (see § 6) |

**Correction summary:** Frank's biggest priority error is keeping uniform action-shape nodes at P3. They must be P1 because the checker implementation will be written against the current AST. Writing the checker against per-kind nodes and then consolidating after is double work plus refactoring risk.

### 5.2 Type Checker Priorities

| Frank Priority | Frank Proposal | My Verdict |
|---|---|---|
| P0 | Precompute operation resolution at startup | **P0 confirmed** |
| P0 | Precompute function resolution at startup | **P0 confirmed** |
| P1 | Declare scope rules on `ConstructMeta` | **P1-Conditional** — must land before Slice 3 implementation; if delayed past Slice 3, manual scope setup gets written and thrown away |
| P1 | Add `ResolutionShape` to `ExpressionFormMeta` | **P3** — implement baseline checker first, add as post-green refactor; don't design metadata and its consumer simultaneously |
| P1 | Modifier validation as generic loop | **P1 confirmed** |
| P2 | Construct-declared slot constraints | **P2 confirmed** |
| P2 | Precomputed accessor resolution | **P2 confirmed** |

**Additional required items not in Frank's priority table:**
- **P0**: `TypeMeta.LiteralRange?` field — Slice 4 blocker, currently missing
- **P0**: `ActionMeta.TypedActionShape` (catalog entry for `ActionSyntaxShape → TypedAction DU`) — Slice 5 blocker, confirmed by both Frank and prior George review
- **P1**: Explicit `QualifierMatch.Same` enforcement as a checker slice item (GAP-065)
- **P1**: `ActionMeta.VariantTriggerToken?` for inline kind-identity check replacement

---

## § 6: Cross-Review Synthesis

### 6.1 The Precomputed-Table Pattern Bridges Both Analyses

The parser already uses the precomputed-FrozenSet/FrozenDict pattern at startup — that's the "Vocabulary FrozenSets" section (Parser.cs:26–207). The type checker should use the identical pattern for operation and function resolution tables. This is not two separate ideas; it's one architectural principle applied to two pipeline stages. The checker's startup code should mirror the parser's: a `static readonly` block that iterates catalogs and builds frozen lookup structures before any check occurs. The design pattern is already established — Frank is asking the checker to match the parser.

### 6.2 Action Statement Unification Connects Parser and Checker

Frank identifies action-kind switch ceremony in the parser (§ 1.2 / § 3.4) and action-shape classification in the checker (§ 1.5) as separate proposals with separate priorities. These are the **same problem at two pipeline stages**:

1. Parser: switch on `ActionKind` to pick concrete `Statement` subclass → Frank proposes uniform `ActionStatement` per shape
2. Checker: switch on `ActionSyntaxShape` to pick concrete `TypedAction` DU subtype → prior George review identifies as Slice 5 blocker

If we consolidate the parser's action AST (one node type per shape) and add `ActionMeta.TypedActionShape` to the catalog (what DU subtype to produce), both problems dissolve together. The implementation dependency is: parser change lands first, checker Slice 5 is written against the consolidated AST. Frank doesn't call out this ordering dependency across documents.

### 6.3 `TypeParseShape` DU Benefits Both Parser and Checker

Frank proposes `TypeParseShape` solely as a parser dispatch improvement (§ 3.3). But the checker's Pass 1 also parses `TypeRef` nodes to resolve `TypeKind`. If `TypeMeta` carries `TypeParseShape`, both the parser's dispatch and the checker's TypeRef resolution become catalog-driven. The checker's TypeRef handler becomes: look up `TypeMeta` by token → read `TypeParseShape` → dispatch to the appropriate handler. Same pattern, second pipeline stage. Frank misses this downstream benefit because the two analyses are isolated documents.

### 6.4 `ScopeRule` on `ConstructMeta` Must Land Before Checker Slice 3

Frank's type-checker analysis proposes `ScopeRule?` on `ConstructMeta` (§ 2.3, § 3.3, priority P1). The type-checker plan's Pass 2 begins pushing scope frames in Slice 3 (transition row checking — `EventArgScope`). If `ScopeRule` is designed and added before Slice 3 is implemented, the Slice 3 code reads it naturally. If not, Slice 3 writes manual per-construct scope setup code, and `ScopeRule` becomes a refactor-after-the-fact. This is a binding ordering dependency Frank doesn't state explicitly: **`ScopeRule` metadata design must complete before Slice 3 implementation begins, not at some P1-unordered time.**

### 6.5 Frank's Split Documents Miss a Shared Gap: No Outcomes Catalog

Frank's parser analysis skips `ParseOutcomeNode` entirely. Frank's type-checker analysis doesn't address `TransitionOutcome` enum as a hardcoded 3-member set. Both analyses have the same blind spot: outcomes are hardcoded at two separate pipeline stages with no catalog coverage. GAP-062 (which I filed in iteration-11) is confirmed outstanding by both analyses' silence. This is a cross-cutting gap requiring decisions at both the parser level (does `ParseOutcomeNode` derive from an Outcomes catalog?) and the checker level (does `TransitionOutcome` become a catalog member set?).

### 6.6 Ordering Dependencies Frank Doesn't State

These must be tracked explicitly as implementation sequencing constraints:

1. **Precomputed tables (P0) → before any checker slice that uses them** — obvious but needs to be explicit
2. **`TypeMeta.LiteralRange?` (P0 blocker) → before Slice 4**
3. **`ActionMeta.TypedActionShape` (P0 blocker) → before Slice 5**
4. **Uniform action-shape `Statement` nodes (parser P1) → before Slice 5 (checker must be written against consolidated AST)**
5. **`ScopeRule` on `ConstructMeta` (P1-conditional) → before Slice 3**
6. **`TypeParseShape` DU on `TypeMeta` (parser P1) → optional before Pass 1 checker, but beneficial if done first**
7. **`StructuralBoundaryTokens` derivation (P0) → before adding any new slot kind to any construct**
8. **Split-modifier metadata design (prerequisite) → before `ParseFieldDeclaration` unification**

Frank's two documents have no cross-document ordering table. This list is the one that matters for sequencing the actual implementation work.

---

*Written as a peer implementer's review — not a diplomatic summary. If any item above is wrong, show me the source line that proves it.*
