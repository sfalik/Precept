# Radical Parser Design: The Catalog IS the Grammar

**Author:** Frank (Lead/Architect)  
**Date:** 2026-05-03  
**Status:** Design sketch — input for rebuild decision and type-checker radical design  
**Builds on:** `frank-catalog-driven-parser-review.md` §§5.4–5.5, `frank-parser-rebuild-reassessment.md`

---

## §0. The Grammar of Precept

Before diving into the combinator vocabulary, the reader needs to see what Precept syntax *actually looks like*. This section illustrates the grammar's fundamental shape — the shape that makes a generic catalog-driven parser possible.

### 0.1 The Fundamental Shape

Every Precept construct is a **leading keyword** followed by a **flat sequence of named slots**. No recursive nesting of productions. No operator-precedence ambiguity at the construct level. No tree structure to the declaration grammar.

Three real examples from sample files:

```precept
state Red initial
```

Leading keyword: `state`. Slots: identifier (`Red`), optional modifier (`initial`).

```precept
field ClaimAmount as decimal default 0 nonnegative maxplaces 2
```

Leading keyword: `field`. Slots: identifier (`ClaimAmount`), type reference (`as decimal`), modifier chain (`default 0 nonnegative maxplaces 2`).

```precept
from UnderReview on Approve when DocumentsVerified and Approve.Amount <= ClaimAmount
    -> set ApprovedAmount = if FraudFlag then min(Approve.Amount, ClaimAmount / 2) else Approve.Amount
    -> set DecisionNote = Approve.Note
    -> transition Approved
```

Leading keyword: `from`. Slots: state identifier (`UnderReview`), event reference (`on Approve`), guard expression (`when ...`), action chain (`-> ... -> ... -> ...`).

The pattern is always the same: **Leader → Slot → Slot → ... → [Block]**. The slots are sequential. The grammar is a table row, not a tree.

### 0.2 Slot Types

Slots are typed positions. The slot types that recur across constructs:

**Identifiers** — names of fields, states, events:
```precept
state Approved
event Decline(Note as string notempty)
```

**Type references** — type keyword plus qualifiers:
```precept
field MissingDocuments as set of string
field ClaimAmount as decimal default 0 nonnegative maxplaces 2
```

**Expressions** — guard conditions, computed values, ensure conditions:
```precept
rule ApprovedAmount <= RequestedAmount because "Approved amount cannot exceed the request"
```

**Modifier keywords** — constraint/behavior annotations that follow the type or expression:
```precept
field EmergencyReason as string optional
field VehiclesWaiting as number default 0 nonnegative
```

**Action chains** — sequences of `->` prefixed statements:
```precept
from Red on Advance when LeftTurnQueued
    -> set LeftTurnQueued = false
    -> set CycleCount = CycleCount + 1
    -> transition FlashingGreen
```

**Outcome blocks** — terminal actions (transition, reject, no transition):
```precept
    -> transition Approved
    -> reject "No demand detected at red"
    -> no transition
```

Each slot type has exactly one sub-parser. The combinator vocabulary invokes these by reference (`ExprProd()`, `TypeRefProd()`, `ModifiersProd()`, `ActionChainProd()`).

### 0.3 Disambiguation and `ConstructFamily`

Some leading keywords introduce not one construct but a **`ConstructFamily`** — multiple construct shapes that share the same leading keyword and require disambiguation. Header and Direct constructs each have a unique leading keyword; no disambiguation is needed, and they have no `ConstructFamily` entry. The keyword `in` can introduce:

```precept
in Approved ensure ApprovedAmount > 0 because "Approved claims must specify a payout amount"
in UnderReview modify FraudFlag editable
```

Same leader (`in`), different constructs (state-ensure vs. access-mode). The **disambiguation token** — `ensure` vs. `modify` — is the second keyword after the anchor. The parser reads the anchor, optionally parses a floating guard, then peeks at the next token to select the construct shape.

Similarly, `on` introduces event-ensure vs. event-hook constructs, and `from` introduces transition constructs with different trailing shapes.

The disambiguation map is data: `{ ensure → StateEnsure, modify → AccessMode, omit → OmitDeclaration }`. The parser reads the map — it doesn't hardcode the dispatch.

### 0.4 The General Pattern and Combinator Mapping

Every construct can be described as:

```
Leader Slot* [Block]
```

The combinator vocabulary maps onto this directly. Consider `state`:

```csharp
// state Name [initial]
Grammar = Seq(
    ConsumeLeader(),                    // 'state'
    Tag("name", ConsumeIdent()),        // identifier slot
    Opt(Tag("initial", Consume(TokenKind.Initial)))  // optional modifier slot
)
```

And a more complex example — the event-ensure construct (`on Event ensure ...`):

```csharp
// on Event ensure Condition because "Message"
Grammar = Seq(
    Consume(TokenKind.Ensure),                     // disambiguation keyword
    Tag("condition", ExprProd()),                  // expression slot
    Opt(Seq(Consume(TokenKind.When), Tag("guard", ExprProd()))),  // optional guarded slot
    Expect(TokenKind.Because),
    Tag("message", ExprProd())                     // expression slot (string literal)
)
```

The combinator vocabulary is small: `Seq` for ordering, `Tag` for naming, `Opt` for optional parts, `Alt` for alternatives, and typed productions (`ExprProd`, `TypeRefProd`, etc.) for invoking sub-parsers. The interpreter walks the tree. One interpreter method, all constructs.

### 0.5 Why This Matters

This grammar shape — flat, keyword-anchored, slot-sequential — is why the catalog can *be* the grammar and why the parser can be a generic interpreter rather than a collection of per-construct methods. A language with recursive productions needs a recursive-descent parser with one method per production. Precept doesn't have recursive productions at the construct level. It has a table of shapes. The combinator tree is that table made executable. The catalog holds the tree; the interpreter executes it; no per-construct code exists anywhere.

### 0.6 Historical Context

The original Precept runtime was built on [Superpower](https://github.com/datalust/superpower) — Nicholas Blumhardt's .NET parser combinator library. The grammar's flat, keyword-anchored, slot-sequential shape was not designed in a vacuum: it was designed *for* a combinator model from the start, and that heritage is why it fits so naturally here. The radical design described in this document is not a departure from Precept's origins — it is a return to them. The single addition is lifting the combinator tree out of C# source code and into catalog metadata (`ConstructMeta.Grammar`), making the grammar a first-class, inspectable, tooling-visible artifact instead of an implementation detail buried in method bodies.

### 0.7 Four Concepts: Constructs, Actions, Outcomes, and Slots

These four concepts compose to form Precept's declaration grammar. Understanding their roles and relationships is essential before reading the combinator designs that follow.

**Construct** — a complete declaration, a "sentence" in the DSL. There are 12 construct kinds (precept header, field declaration, state declaration, event declaration, rule, transition row, state ensure, access mode, omit, state action, event ensure, event handler). Every construct begins with a leading keyword and is followed by a flat sequence of slots. Constructs are cataloged: `ConstructKind` enum + `Constructs.cs` + `ConstructMeta` records with full grammar trees.

**Slot** — a named parse position within a construct's grammar tree, captured via `Tag("name", rule)` nodes. Slots are not a separate catalog; they exist only as positions in grammar trees. Each slot has a type: identifier, type-ref, expression, modifiers, action-chain, or outcome. A construct's slots ARE its grammar.

**Action** — a mutation verb inside an action-chain slot: `set`, `add`, `remove`, `enqueue`, `push`, `clear`, etc. Actions describe *what you do to a field*. Actions are cataloged: `ActionKind` enum + `Actions.cs` + `ActionMeta` records with rich per-member metadata (type targets, syntax shapes, proof requirements, allowed contexts). 15 members, all sharing the same record shape but differing in behavioral metadata — a textbook catalog case.

**Outcome** — the terminal disposition of a transition row: *where does the state machine go?* Three variants: `transition TargetState` (move), `no transition` (stay), `reject "reason"` (refuse). Outcomes describe *what happens to the state machine*. Outcomes are cataloged at the metadata level (`OutcomeKind` enum + `OutcomeMeta` + `Outcomes.cs`) and represented at the syntax-node level as a discriminated union (`OutcomeNode` with three sealed subtypes). Same two-level pattern as Actions: catalog for enumeration and metadata, DU for parsing and structural access.

**How they compose — a concrete example:**

```precept
from Red on Advance when LeftTurnQueued
    -> set LeftTurnQueued = false
    -> set CycleCount = CycleCount + 1
    -> transition Green
```

| Concept | In this example | Role |
|---------|----------------|------|
| **Construct** | `TransitionRow` | The sentence shape — `from State on Event [when Guard] [-> Actions] -> Outcome` |
| **Slots** | `from` (state-target), `on` (event), `when` (guard expr), `actions` (action-chain), `outcome` (outcome block) | Named positions in the grammar tree |
| **Actions** | `set LeftTurnQueued = false`, `set CycleCount = CycleCount + 1` | Mutation verbs inside the action-chain slot — they modify fields |
| **Outcome** | `transition Green` | Terminal disposition inside the outcome slot — it moves the state machine |

**Key distinctions:**

- *Construct vs. Action*: A construct is a complete declaration. An action is a verb *within* a construct's action-chain slot.
- *Action vs. Outcome*: Both appear after `->` arrows, but actions **mutate fields** while outcomes **resolve the state machine**. A transition row can have zero or many actions, but exactly one outcome.
- *Slot vs. Construct*: A slot is a position within a construct. Slots exist only as parts of constructs.
- *Catalog vs. DU*: Actions have a catalog (15 members, rich per-member metadata) AND action-specific DU subtypes. Outcomes follow the same two-level pattern: a catalog (`OutcomeKind` + `OutcomeMeta` + `Outcomes.cs`) for enumeration and metadata, and a DU (`OutcomeNode` subtypes) for structural parse results. The test is: "Can a consumer enumerate all members and get their metadata without hardcoding composition logic?" The catalog ensures they can.

### 0.8 Outcomes: DU + Catalog (Two-Level Pattern) — REVISED

> **Revised 2026-05-02.** The original ruling stated outcomes do NOT need a catalog. That ruling is reversed. Outcomes SHOULD have a catalog, following the same two-level pattern as Actions.

**The decisive argument: the `no transition` composition gap.**

`TokenCategory.Outcome` tags three tokens (`Transition`, `No`, `Reject`). But `no transition` is a **two-token sequence** — `No` + `Transition` compose into a single outcome. A consumer enumerating `Tokens.All` filtered by `TokenCategory.Outcome` gets three token entries, not three outcome entries. Reconstructing outcome-level abstractions from token-level metadata requires hardcoding the composition rule that `No` + `Transition` = one outcome. That composition knowledge is domain knowledge — it belongs in the catalog.

**The ruling: outcomes get a catalog.**

- `OutcomeKind` enum — 3 members: `Transition`, `NoTransition`, `Reject`
- `OutcomeMeta` record — 5 fields: `Kind`, `LeadToken`, `Syntax`, `Description`, `SyntaxShape`
- `Outcomes.cs` catalog with `Outcomes.All` returning 3 `OutcomeMeta` entries

**Why the DU stays.**

The `OutcomeNode` discriminated union — `TransitionOutcomeNode(Span, TargetState)`, `NoTransitionOutcomeNode(Span)`, `RejectOutcomeNode(Span, Message)` — remains the correct parse-result representation. Each subtype carries structurally different fields. The catalog does not replace the DU; it supplements it. Consumers use the DU for parsing and structural access; the catalog for enumeration and metadata.

This is the same two-level pattern as Actions: `ActionKind` + `ActionMeta` + `Actions.cs` at the metadata level, action-specific DU subtypes at the syntax-node level. Both layers coexist — different purposes, different consumers.

**The consumer test:** `Outcomes.All` returns 3 `OutcomeMeta` entries. A consumer asking "give me all outcomes" gets a clean answer — their syntax, their lead token, their shape — without any token-composition logic. This is the enumerability the catalog system provides.

**Why the original arguments were insufficient:**

1. ~~"No per-member behavioral metadata."~~ The metadata is real: `LeadToken`, `Syntax`, `SyntaxShape`, `Description`. Lighter than Actions, but the catalog threshold is consumer enumerability, not metadata weight.
2. ~~"Structurally different shapes."~~ The DU handles structural differences at the syntax-node level. The catalog operates at the metadata level — flat records describing the *concept*, not the *parse result*. No inapplicable nullable fields.
3. ~~"No metadata to centralize."~~ The `no transition` composition gap IS the metadata to centralize. Token-level enumeration cannot reconstruct outcome-level abstractions without hardcoding domain knowledge.
4. ~~"The Tokens catalog already handles the keyword level."~~ Exactly — the *keyword* level, not the *outcome* level. Token enumeration gives consumers token-level metadata. Outcome enumeration gives consumers outcome-level metadata. These are different abstraction levels.

The radical parser's `OutcomeProd` combinator continues to handle outcome parsing within the grammar tree. The catalog supplements it with the enumeration and metadata layer that consumers need.

### 0.9 Grammar Hierarchy

The diagram below maps every construct to its routing family and syntax shape, with action groups and outcome variants annotated to make the cross-cutting relationships explicit. Within StateScoped and EventScoped — where constructs share a leading keyword and require disambiguation — the ConstructFamily anchor (◆) is shown as a sub-group header; Header and Direct constructs have unique leading keywords and carry no anchor.

```
PRECEPT DSL — GRAMMAR HIERARCHY
────────────────────────────────────────────────────────────────────────────────────────────────────
FAMILY         CONSTRUCT              SYNTAX EXAMPLE
────────────────────────────────────────────────────────────────────────────────────────────────────
Header      ─► PreceptHeader          precept LoanApplication
────────────────────────────────────────────────────────────────────────────────────────────────────
Direct      ─► FieldDeclaration       field amount as money nonnegative
               StateDeclaration       state Draft initial, Submitted, Approved terminal success
               EventDeclaration       event Submit(approver as string)
               RuleDeclaration        rule amount > 0 because "Amount must be positive"
────────────────────────────────────────────────────────────────────────────────────────────────────
StateScoped ─► ◆ From (StateAnchor)
               TransitionRow  [A][O]  from Draft on Submit [when G] [-> …] -> outcome
               ◆ In   (StateAnchor)
               StateEnsure            in Approved ensure amount > 0 because "…"
               AccessMode             in Draft modify Amount editable
               OmitDeclaration        in Draft omit InternalNotes
               ◆ To   (StateAnchor)
               StateAction    [A]     to Submitted -> set submittedAt = now()
────────────────────────────────────────────────────────────────────────────────────────────────────
EventScoped ─► ◆ On   (EventAnchor)
               EventEnsure            on Submit ensure reviewer != "" because "…"
               EventHandler   [A]     on UpdateName -> set name = newName
────────────────────────────────────────────────────────────────────────────────────────────────────

[A] ACTIONS (15 verbs) — appear in: TransitionRow · StateAction · EventHandler
  Scalar     set
  Set        add · remove
  Queue      enqueue · dequeue · enqueueBy · dequeueBy
  Stack      push · pop
  List       append · appendBy · insert · removeAt · put
  Universal  clear

[O] OUTCOMES (3 variants) — appear ONLY in TransitionRow
  transition State    move to a new state
  no transition       stay in current state (two tokens, one outcome)
  reject "message"    refuse the event with a reason
────────────────────────────────────────────────────────────────────────────────────────────────────
```

**Key:** `─►` = routing family anchors constructs by leading token · `◆` = ConstructFamily anchor (shared leader; StateScoped and EventScoped only) · `[A]` = carries action-chain slot · `[O]` = carries outcome slot

> **Terminology note:** "Routing family" (the FAMILY column) classifies all 12 constructs by *parse scope* — where in the file the parser looks for them. `ConstructFamily` (§3.2) is a narrower catalog type for the subset of StateScoped and EventScoped constructs whose leading keywords are *shared* and require disambiguation; these appear as `◆` anchor rows in the diagram above. Header and Direct constructs each have a unique leader and have no `ConstructFamily` entry.

### 0.10 AST Design Options — Radical Alternatives

The radical parser claim is: *"A new construct requires a grammar rule and a ConstructMeta entry. The parser is untouched."* But if adding a construct still requires hand-writing a new AST node class, the catalog hasn't fully absorbed construct-specific behavior. The per-construct type system is a leak in the abstraction. This section explores radical alternatives.

**Summary Table:**

| Option | Construct-specific code? | Type safety | Debuggability | AI legibility | Precept fit |
|--------|--------------------------|-------------|---------------|---------------|-------------|
| A. Universal ConstructNode (property bag) | None | ⚠ Runtime-only | ⚠ Bag inspection | ✓ Uniform JSON | ✓✓ Flat grammar |
| B. Slot-indexed struct (flat array) | None | ✗ Untyped | ✗ Index guessing | ⚠ Positional | ✓✓ Flat grammar |
| C. Source-generated typed nodes | None (generated) | ✓✓ Full compile-time | ✓✓ Named properties | ✓ Named JSON | ✓ Grammar-derived |
| D. No AST — catalog + slot values | None | ⚠ Runtime | ⚠ Raw pairs | ✓ Direct mapping | ✓✓ Perfect fit |
| E. CST-only (lossless generic tree) | None | ✗ Untyped tree | ✓ Full source fidelity | ⚠ Requires interpretation | ✓ Generic |
| **F. Hybrid: generic internal + typed external** | **None internal** | **✓✓ Typed at boundary** | **✓ Both layers** | **✓✓ Typed DTOs** | **✓✓ Best of both** |

---

#### Option A — Universal ConstructNode (Property Bag)

**What it looks like:**

```csharp
sealed record ConstructNode(
    SourceSpan Span,
    ConstructKind Kind,
    ConstructMeta Meta,
    ImmutableDictionary<string, object?> Slots);

// Access:
var fieldType = node.Slots["type"] as TypeRefNode;
var guard = node.Slots["guard"] as Expression;
```

**What it buys:** Zero per-construct types. Adding a construct = adding a `ConstructMeta` entry. The parser produces `ConstructNode` for everything. One AST type for 12 constructs. The `ResultBag` from §2 already IS this dictionary — there's no mapping step.

**What it costs:** Compile-time type safety evaporates. Every downstream consumer casts from `object?`. Typos in slot-name strings are runtime errors. Refactoring requires grep, not the compiler. The type checker becomes a dictionary-walking machine with stringly-typed lookups.

**Tradeoff:** Total extensibility at the cost of total type safety. Viable only if consumers are few and disciplined (or if typed wrappers exist at the boundary — see Option F).

**Precept fit:** Excellent. The grammar is flat and slot-sequential. Slots don't nest recursively. The bag model maps 1:1 to how the radical parser already works internally (`ResultBag` keyed by `Tag` names). The jump from ResultBag to typed node is the *only* per-construct code in the radical parser design — Option A eliminates it entirely.

---

#### Option B — Slot-Indexed Struct (Flat Array)

**What it looks like:**

```csharp
readonly struct ParseResult(ConstructKind Kind, object?[] SlotValues);

// Positional access via meta:
var meta = Constructs.Get(result.Kind);
var slotNames = ExtractNamedCaptures(meta.Grammar); // ["state", "event", "guard", "actions", "outcome"]
var guard = result.SlotValues[2] as Expression;     // index 2 = "guard"
```

**What it buys:** Maximally compact. No allocations beyond the array. Adding a construct = grammar + meta entry. No types, no dictionaries, no overhead.

**What it costs:** Positional indexing is fragile and illegible. Reordering grammar slots silently breaks downstream consumers. Debuggers show `object?[5]` with no labels. AI tools get opaque arrays. This is the assembly language of ASTs.

**Tradeoff:** Maximum compactness and zero per-construct ceremony, but positional fragility makes it unsuitable for any system with more than one consumer. Viable only as an *internal* intermediate representation hidden behind accessors.

**Precept fit:** The flat grammar makes fixed-position arrays feasible (no recursion means stable slot counts). But the fragility cost is too high for a system that serves the type checker, MCP tools, AND the language server.

---

#### Option C — Source-Generated Typed Nodes

**What it looks like:**

```csharp
// Source generator reads Constructs.All at build time and emits:
[GeneratedFromCatalog(ConstructKind.FieldDeclaration)]
sealed partial record FieldDeclarationNode(
    SourceSpan Span,
    ImmutableArray<Token> Names,     // from Tag("names")
    TypeRefNode Type,                // from Tag("type")
    ImmutableArray<FieldModifierNode> Modifiers,  // from Tag("mods1") + Tag("mods2")
    Expression? ComputedExpression)  // from Tag("expr")
    : DeclarationNode(Span);

// BuildNode is also generated — maps ResultBag → typed constructor args
```

**What it buys:** Full compile-time type safety. Named properties. Pattern matching. IntelliSense. Refactoring support. The type checker sees exactly the same strongly-typed nodes it sees today. AND — no hand-written per-construct code. The generator reads the catalog; the catalog is the single source of truth.

**What it costs:** A Roslyn source generator adds build-pipeline complexity. The generation template needs maintenance (mapping from grammar `Tag` types to C# property types requires a type-mapping table). Debugging generated code requires understanding the generator. The generated code must be inspectable (checked in or emit-to-file mode).

**Tradeoff:** The complexity moves from "hand-write N node classes" to "maintain one generator that emits N node classes." This is a net win only if N is expected to grow. For Precept's 12 constructs — a number that changes rarely — the generator overhead may exceed the savings.

**Why it partially fails the radical test:** The generator still needs a *type-mapping specification* — a rule that says "Tag named 'type' → TypeRefNode, Tag named 'guard' → Expression." This mapping IS per-construct knowledge, just expressed declaratively in the generator config rather than in C# classes. It moves the work, it doesn't eliminate it.

**Precept fit:** Good, but the grammar-to-type mapping table is the irreducible per-construct information. Whether it lives in a C# class or a generator attribute, someone writes it once per construct.

---

#### Option D — No Separate AST (Catalog + Slot Values)

**What it looks like:**

```csharp
// The parse output is directly:
record ParsedConstruct(ConstructMeta Meta, ImmutableArray<SlotValue> Slots);

// SlotValue is a tagged union:
abstract record SlotValue;
sealed record IdentSlot(Token Value) : SlotValue;
sealed record IdentListSlot(ImmutableArray<Token> Values) : SlotValue;
sealed record TypeRefSlot(TypeRefNode Value) : SlotValue;
sealed record ExprSlot(Expression Value) : SlotValue;
sealed record ModifiersSlot(ImmutableArray<FieldModifierNode> Values) : SlotValue;
sealed record ActionChainSlot(ImmutableArray<Statement> Values) : SlotValue;
sealed record OutcomeSlot(OutcomeNode Value) : SlotValue;
sealed record ArgListSlot(ImmutableArray<ArgumentNode> Values) : SlotValue;

// Type checker consumes ParsedConstruct directly:
void Check(ParsedConstruct c) {
    switch (c.Meta.Kind) {
        case ConstructKind.TransitionRow:
            var state = c.Slots[0] as IdentSlot;    // or by name lookup
            var guard = c.Slots[2] as ExprSlot;
            // ...
    }
}
```

**What it buys:** The AST layer disappears entirely. Parse output = catalog metadata + concrete slot values. No per-construct types, no generation, no mapping step. The `ResultBag` from §2 IS the final representation. The "build node" step that §7.2 describes — the only per-construct code in the radical parser — is simply deleted.

**What it costs:** The type checker must switch on `ConstructKind` and cast `SlotValue` subtypes. This is essentially Option A with a typed union instead of `object?`. Better than raw dictionaries, worse than named properties. Every downstream consumer re-derives the slot structure from the catalog on every access.

**Tradeoff:** Eliminates the AST layer entirely, but pushes per-construct knowledge into *every consumer*. The construct-specific behavior doesn't disappear — it relocates from a type definition to a switch statement in the type checker (and the evaluator, and the language server, and the MCP serializer...).

**Precept fit:** Excellent structural fit. The flat, non-recursive grammar means slot arrays are always shallow and predictable. The small construct count (12) means the consumer switches are manageable. But duplicating slot-access logic across 4+ consumers is worse than centralizing it in a node type.

---

#### Option E — CST-Only (Lossless Concrete Syntax Tree)

**What it looks like:**

```csharp
// Generic lossless tree — every token preserved, trivia attached
sealed record CstNode(SyntaxKind Kind, ImmutableArray<CstChild> Children, SourceSpan Span);
// Children are either tokens or child nodes
abstract record CstChild;
sealed record CstToken(Token Token) : CstChild;
sealed record CstBranch(CstNode Node) : CstChild;

// Downstream interpretation via catalog:
var meta = Constructs.Get(ConstructKind.TransitionRow);
var slotNames = ExtractNamedCaptures(meta.Grammar);
// Walk children using grammar as the interpretation key
```

**What it buys:** Full source fidelity (refactoring, formatting, comments preserved). Completely generic parser — produces one universal tree type. No per-construct types whatsoever. This is the Roslyn/tree-sitter model.

**What it costs:** Semantic interpretation requires a second pass. Every consumer that wants "the guard expression of a transition row" must navigate a generic tree using catalog metadata as a map. This is powerful for tooling (formatters, linters) but expensive for semantic analysis. The type checker becomes a tree-walker that re-discovers structure on every node.

**Tradeoff:** Maximum generality and source fidelity at the cost of semantic directness. Roslyn needs this because C# has 200+ syntax kinds and supports incremental re-parsing. Precept has 12 constructs and no incremental requirement. The CST model is massively over-engineered for the problem.

**Precept fit:** Poor cost/benefit ratio. Precept's non-recursive grammar means CST depth is always 1-2 levels. The CST buys you nothing over a flat slot array. It adds tree-navigation overhead without structural benefit. Skip.

---

#### Option F — Hybrid: Generic Internal + Typed External (RECOMMENDED FOR EXPLORATION)

**What it looks like:**

```csharp
// === INTERNAL (parser output) ===
// The parser produces generic ParsedConstruct — same as Option D
record ParsedConstruct(ConstructMeta Meta, ImmutableArray<SlotValue> Slots, SourceSpan Span);

// === TYPED ACCESSOR LAYER (consumed by type checker, MCP, evaluator) ===
// Static accessor methods — one per construct — provide typed access without per-construct node classes:
static class ConstructAccessors {
    public static (ImmutableArray<Token> Names, TypeRefNode Type, 
                   ImmutableArray<FieldModifierNode> Modifiers, Expression? Computed)
        FieldDeclaration(ParsedConstruct c) {
        Debug.Assert(c.Meta.Kind == ConstructKind.FieldDeclaration);
        return (c.Slots.Named<IdentListSlot>("names").Values,
                c.Slots.Named<TypeRefSlot>("type").Value,
                c.Slots.Named<ModifiersSlot>("mods1").Values.AddRange(c.Slots.Named<ModifiersSlot>("mods2").Values),
                c.Slots.TryNamed<ExprSlot>("expr")?.Value);
    }
}

// Type checker usage:
void CheckField(ParsedConstruct c) {
    var (names, type, mods, computed) = ConstructAccessors.FieldDeclaration(c);
    // Full type safety from here down — named, typed, IDE-navigable
}

// === MCP / EXTERNAL (serialized for AI consumers) ===
// MCP DTOs are typed as today — mapped from ParsedConstruct via accessor layer
record FieldDeclarationDto(string[] Names, string Type, string[] Modifiers, string? ComputedExpression);
```

**What it buys:**
- **Parser is fully generic.** Zero per-construct code in the parser. Adding a construct = grammar rule + `ConstructMeta` entry. Period.
- **Type safety at consumption boundaries.** The accessor layer provides named, typed access. The MCP layer provides typed DTOs. AI consumers get clean JSON with named fields.
- **Accessor layer is mechanically derivable.** Unlike Option C's source generator, accessors can be hand-written OR generated — they're simple tuple-returning static methods. A code generator is optional, not required.
- **The only per-construct artifact is the accessor function.** One function per construct (~5 lines), not one class per construct (~15 lines + constructor + pattern-match support).
- **Internal representation can evolve.** Swap slot storage (bag → array → arena) without changing any consumer. Consumers only see the typed tuple.

**What it costs:**
- Accessor functions ARE per-construct code. The claim "no code per construct" becomes "no *parser* code per construct, minimal accessor code per construct."
- Pattern matching on node types is lost. The type checker dispatches on `ConstructKind` enum rather than `switch (node) { case FieldDeclarationNode f => ... }`. This is a style regression for C# developers who prefer exhaustive pattern matching.
- Two levels of indirection: parser → ParsedConstruct → accessor → typed tuple. Debuggers show the generic layer unless you step through the accessor.

**Tradeoff:** Accepts that per-construct *knowledge* is irreducible (each construct has different slot meanings) but minimizes its *expression* to a single thin accessor function rather than a full class hierarchy. The accessor function is the irreducible minimum — it maps from positional data to semantic meaning.

**Precept fit:** Ideal. The flat grammar means ParsedConstruct is always a shallow bag. The small construct count (12) means the accessor module is ~100 lines total. The accessor pattern is the same pattern as `BuildNode` in §7.2, just without the intermediate class allocation.

---

#### Cross-Cutting Concerns

**AI Legibility (MCP tool output):**

| Option | `precept_compile` output quality |
|--------|----------------------------------|
| A (bag) | ⚠ Returns `{ "kind": "TransitionRow", "slots": { "state": "Red", "event": "Advance", ... } }` — uniform but unnamed-typed |
| B (array) | ✗ Returns `{ "kind": "TransitionRow", "slots": ["Red", "Advance", ...] }` — positional, opaque |
| C (generated) | ✓ Returns named JSON identical to today — `{ "state": "Red", "event": "Advance", "guard": {...} }` |
| D (no AST) | ⚠ Same as A — named slots, untyped values |
| E (CST) | ✗ Returns raw tree — requires AI to interpret via grammar |
| **F (hybrid)** | **✓ MCP layer maps through typed accessors → clean named DTOs identical to today** |

Options C and F produce MCP output indistinguishable from hand-written typed nodes. Options A and D are workable but slightly worse (slot-name keys instead of property names). Options B and E are unacceptable for AI consumption without a mapping layer.

**Type Checker Implications:**

The type checker's fundamental operation is: "given this construct, validate its slots against the type system." Under the current design, it pattern-matches on node types. Under the radical AST:

- **Options A/B/D:** Type checker switches on `ConstructKind`, casts slots. This is the same pattern as today's `BuildNode` dispatch — a `switch` on kind with per-case logic. No worse, but no better.
- **Option C:** Type checker is unchanged — same typed nodes, same pattern matching.
- **Option E:** Type checker must navigate an untyped tree. Strictly worse.
- **Option F:** Type checker calls accessor functions, gets typed tuples, proceeds with validation. The accessor call replaces the pattern match. Ergonomically similar — just tuple destructuring instead of record property access.

**The Irreducible Core:**

Every option reveals the same truth: **per-construct semantic knowledge is irreducible.** The slots of a `TransitionRow` mean different things than the slots of a `FieldDeclaration`. No amount of genericity eliminates the need for *something* that maps from positional slots to semantic meaning. The question is only where that mapping lives:

- In a C# class definition (status quo)
- In a source generator template (Option C)
- In a thin accessor function (Option F)
- In every consumer independently (Options A/B/D — worst case)
- In a tree-walking interpreter (Option E — over-engineered)

**The Hybrid Recommendation:**

Option F (hybrid) is the sweet spot for Precept because:

1. It completes the radical parser claim — the parser is truly untouched for new constructs.
2. It minimizes per-construct ceremony to one accessor function (~5 lines vs. ~15-line class).
3. It preserves full type safety at every consumption boundary.
4. It's orthogonal to source generation — accessors CAN be generated later (making it a superset of Option C without the upfront generator investment).
5. It matches Precept's actual problem size — 12 constructs that change rarely.

**What Shane is being asked to weigh:** Is the loss of C# pattern matching on node types (ergonomic cost) acceptable in exchange for eliminating per-construct AST classes and making the parser fully generic? The hybrid accessor pattern is the compromise position — less beautiful than typed nodes, less fragile than raw bags, and the only option that makes the "parser is untouched" claim fully true without a source generator dependency.

---

The existing parser is built on one implicit assumption: that a parser is a collection of methods, one per grammar production. That assumption is correct for languages like C# or TypeScript, where productions have heterogeneous recursive structure. It is wrong for Precept.

Precept's grammar is not recursive and heterogeneous. It is **flat, keyword-anchored, and slot-sequential**. Every top-level construct begins with a fixed leading token. Every construct body is a fixed sequence of typed slots. The entire grammar fits in a table. The catalog already *is* that table.

The bet: replace the ~1,800-line three-file parser with a ~650-line single-file PEG rule interpreter. The catalog describes grammar; the parser executes it. No per-construct parse methods. No per-action-kind switches. No `DisambiguateAndParse` hand-coded dispatch. One interpreter, all constructs.

This is not theoretical. The `ParseConstructSlots` + `InvokeSlotParser` pattern in the current parser already proves the architecture is viable — it handles `state`, `event`, and `rule` declarations generically today. The radical design extends that principle to *everything*.

---

## 2. Core Model: The ParseRule Combinator

The fundamental abstraction is a small, sealed combinator vocabulary called `ParseRule`. Each `ConstructMeta` carries a `ParseRule Grammar` field. The interpreter takes a grammar and a cursor and produces a tagged result bag.

### 2.1 The Combinator Vocabulary

```csharp
// ParseRule is a discriminated union (abstract sealed class hierarchy)
abstract sealed class ParseRule { }

// --- Terminals ---
sealed class ConsumeLeader()       : ParseRule  // consume the construct's leading keyword
sealed class Consume(TokenKind K)  : ParseRule  // consume exactly this token (assert it's there)
sealed class Expect(TokenKind K)   : ParseRule  // expect + error-recover if absent
sealed class ConsumeIdent()        : ParseRule  // consume identifier (with required-guard)

// --- Structure ---
sealed class Seq(ParseRule[] Rules)             : ParseRule  // ordered sequence
sealed class Opt(ParseRule Rule)                : ParseRule  // optional — never fails
sealed class Rep(ParseRule Rule)                : ParseRule  // zero or more
sealed class Alt(ParseRule[] Rules)             : ParseRule  // first match wins

// --- Named Slots (produce tagged nodes in result bag) ---
sealed class Tag(string Name, ParseRule Rule)   : ParseRule  // run Rule, store result under Name

// --- Typed Productions (invoke sub-parsers, produce typed nodes) ---
sealed class TypeRefProd()                      : ParseRule  // → TypeRefNode
sealed class ExprProd()                         : ParseRule  // → Expression
sealed class ModifiersProd()                    : ParseRule  // → ImmutableArray<FieldModifierNode>
sealed class StateEntriesProd()                 : ParseRule  // → ImmutableArray<StateEntryNode>
sealed class ActionChainProd()                  : ParseRule  // → ImmutableArray<Statement>
sealed class OutcomeProd()                      : ParseRule  // → OutcomeNode
sealed class IdentListProd()                    : ParseRule  // → ImmutableArray<Token>
sealed class ArgListProd()                      : ParseRule  // → ImmutableArray<ArgumentNode>
sealed class FieldTargetProd()                  : ParseRule  // → FieldTargetNode
sealed class AccessModeKeywordProd()            : ParseRule  // → Token

// --- ConstructFamily / Disambiguation ---
sealed class FamilyDispatch(ConstructFamily Family) : ParseRule  // anchor + float + disambiguate
```

The parser has exactly one method that matters: `object? Interpret(ParseRule rule, Cursor cursor, ResultBag bag)`. Everything flows through it. No per-construct code.

### 2.2 Example: FieldDeclaration Grammar

Before the design, `ParseFieldDeclaration` was 40 lines of handwritten sequential logic with special-case split-modifier handling. As a grammar rule it becomes:

```csharp
// ConstructMeta for FieldDeclaration:
Grammar = Seq(
    ConsumeLeader(),                          // 'field'
    Tag("names", IdentListProd()),            // Name, Name2, ...
    Consume(TokenKind.As),
    Tag("type",  TypeRefProd()),              // as Type [qualifiers]
    Tag("mods1", ModifiersProd()),            // pre-expression modifiers (zero or more)
    Opt(Seq(
        Consume(TokenKind.Arrow),
        Tag("expr",  ExprProd()),             // -> ComputedExpression
        Tag("mods2", ModifiersProd())         // post-expression modifiers (zero or more)
    ))
)
```

`BuildNode` for `FieldDeclaration` reads `bag["names"]`, `bag["type"]`, concatenates `bag["mods1"] + bag["mods2"]`, and reads `bag["expr"]`. The split-modifier problem dissolves. The grammar says exactly what the syntax is; the interpreter executes it literally.

### 2.3 Example: StateEnsure Grammar (with stashed guard)

```csharp
// ConstructMeta for StateEnsure — body grammar only (anchor zone handled by FamilyDispatch):
Grammar = Seq(
    Consume(TokenKind.Ensure),
    Tag("condition", ExprProd()),
    Opt(Seq(Consume(TokenKind.When), Tag("guard", ExprProd()))),
    Expect(TokenKind.Because),
    Tag("message",   ExprProd())
)
```

The stashed guard from the anchor zone is injected by the `FamilyDispatch` interpreter before executing this body grammar (§4.1). `BuildNode` checks `bag["guard"]` for the anchor-zone stash first, then the body-level `Opt(When, ...)` — the stash wins if it was set.

---

## 3. Catalog Metadata Expansions

Three additions to the existing catalog. Everything else already exists.

### 3.1 `ParseRule Grammar` on `ConstructMeta`

```csharp
public sealed record ConstructMeta(
    ConstructKind Kind,
    string DisplayName,
    TokenKind LeadingToken,
    ImmutableArray<TokenKind>? DisambiguationTokens,
    ParseRule Grammar                            // NEW — the runtime grammar rule
)
```

The `Slots` field is removed. `Grammar` is the single authoritative representation of a construct's structure. Named captures (the concept formerly served by `Slots`) are derived from the grammar tree at startup:

```csharp
/// Walks a ParseRule tree and collects the Name from every Tag node.
/// Called once per ConstructMeta at catalog initialization.
static ImmutableArray<string> ExtractNamedCaptures(ParseRule grammar)
{
    var names = ImmutableArray.CreateBuilder<string>();
    Collect(grammar, names);
    return names.ToImmutable();

    static void Collect(ParseRule rule, ImmutableArray<string>.Builder acc) => rule switch
    {
        Tag t      => { acc.Add(t.Name); Collect(t.Rule, acc); },
        Seq s      => { foreach (var r in s.Rules) Collect(r, acc); },
        Opt o      => Collect(o.Rule, acc),
        Rep r      => Collect(r.Rule, acc),
        Alt a      => { foreach (var r in a.Rules) Collect(r, acc); },
        _          => { }   // terminals and typed productions have no sub-rules
    };
}
```

`ExtractNamedCaptures` produces the same information `Slots` carried — an ordered list of named positions — but derived from the grammar tree rather than maintained as a parallel field. IDE tooling and documentation consumers call this utility instead of reading a separate `Slots` array.

> **Design rationale:** The `Tag` nodes in the combinator tree ARE the named captures. Keeping a separate `Slots` field creates a parallel representation of the same truth that can diverge. One representation, one source: the grammar tree is both the executable parse description and the named-capture registry.

The grammar rules for all 12 constructs are declared once, inline, in `Constructs.cs`. Total: approximately 80 lines of combinator trees.

### 3.2 `ConstructFamily` — New Catalog Type

`ConstructFamily` replaces the hand-coded `DisambiguateAndParse` logic. One `ConstructFamily` entry per leading token that requires disambiguation. Only StateScoped and EventScoped constructs qualify — Header and Direct constructs each have a unique leading keyword, so no disambiguation is needed and they have no `ConstructFamily` entry.

```csharp
public sealed record ConstructFamily(
    TokenKind Leader,
    AnchorKind Anchor,              // StateAnchor | EventAnchor
    ParseRule? FloatingGuard,       // optional guard parseable before disambiguation token
    FrozenDictionary<TokenKind, ConstructKind> DisambiguationMap
)

public enum AnchorKind { StateAnchor, EventAnchor }
```

There are four `ConstructFamily` entries: `In` (StateAnchor), `To` (StateAnchor), `From` (StateAnchor), `On` (EventAnchor).

`FloatingGuard` for the three state `ConstructFamily` entries (`In`, `To`, `From`): `Seq(Consume(TokenKind.When), Tag("stashedGuard", ExprProd()))`.  
`FloatingGuard` for the `On` entry: `null` — event handlers do not support a pre-event guard.

`DisambiguationMap` is computed from `Constructs.ByLeadingToken`, same as `FindDisambiguatedConstruct` today. No logic changes — just data that lives in the catalog instead of in `DisambiguateAndParse`.

Static initializer:
```csharp
public static class Families
{
    public static readonly ImmutableArray<ConstructFamily> All = [
        new ConstructFamily(
            Leader: TokenKind.In,
            Anchor: AnchorKind.StateAnchor,
            FloatingGuard: Seq(Consume(TokenKind.When), Tag("stashedGuard", ExprProd())),
            DisambiguationMap: FrozenDictionary.ToFrozenDictionary([
                (TokenKind.Modify,  ConstructKind.AccessMode),
                (TokenKind.Omit,    ConstructKind.OmitDeclaration),
                (TokenKind.Ensure,  ConstructKind.StateEnsure),
            ])
        ),
        // ... To, From, On entries
    ];
    public static readonly FrozenDictionary<TokenKind, ConstructFamily> ByLeader = ...;
}
```

### 3.3 `ImmutableArray<ActionVariant> Variants` on `ActionMeta`

This resolves the variant-action detection gap without any mid-parse `meta.Kind` checks.

```csharp
public sealed record ActionVariant(
    VariantPeekPosition Position,  // BeforeValue or AfterValue
    TokenKind TriggerToken,        // the token that triggers the variant shape
    ActionSyntaxShape VariantShape // the shape to use if triggered
)

public enum VariantPeekPosition { BeforeValue, AfterValue }
```

**Variant declarations in `Actions.cs`:**

```
remove:   Variants = [ new(BeforeValue, TokenKind.At,  ActionSyntaxShape.RemoveAtIndex) ]
append:   Variants = [ new(AfterValue,  TokenKind.By,  ActionSyntaxShape.CollectionValueBy) ]
enqueue:  Variants = [ new(AfterValue,  TokenKind.By,  ActionSyntaxShape.CollectionValueBy) ]
dequeue:  Variants = [ new(AfterValue,  TokenKind.By,  ActionSyntaxShape.CollectionIntoBy) ]
```

All other actions: `Variants = []`.

The action shape interpreter reads `Variants` from the catalog — no per-kind branching anywhere in the parser.

### 3.4 `TypeParseShape` on `TypeMeta` (from review §3.3)

Already proposed in `frank-catalog-driven-parser-review.md`. Carried forward unchanged. Shrinks `ParseTypeRef` from 190 lines to ~45 lines of generic dispatch.

```csharp
public abstract sealed class TypeParseShape { }
sealed class ScalarShape()              : TypeParseShape
sealed class CISensitiveShape()         : TypeParseShape   // ~string
sealed class CollectionShape()          : TypeParseShape   // keyword of Element [qualifiers] [by P [dir]]
sealed class LookupShape()              : TypeParseShape   // lookup of K to V
sealed class ChoiceShape()              : TypeParseShape   // choice of ElemType (options...)
```

The per-type parse dispatch table is computed at startup:
```csharp
FrozenDictionary<TokenKind, TypeParseShape> TypeParseDispatch =
    Types.ByToken.ToFrozenDictionary(kvp => kvp.Key, kvp => kvp.Value.ParseShape);
```

---

## 4. The Three Design Gaps: Resolved

### 4.1 Gap 1: Stashed-Guard Pattern

**The problem**: In `DisambiguateAndParse`, the optional `when Guard` is consumed *before* the disambiguation token is seen. The grammar isn't left-to-right from the perspective of a flat slot sequence — the guard floats between the anchor and the disambiguation token. Per-construct slot iteration can't express this without knowing which construct it's in.

**The resolution: `FamilyDispatch` with `FloatingGuard`**.

The `FamilyDispatch` combinator is the only non-trivial part of the interpreter. Its execution sequence:

```
1. Consume leading token (in/to/from/on)
2. Parse anchor target per Family.Anchor:
     StateAnchor → ConsumeIdent or Consume(Any) → StateTargetNode → bag["anchor"]
     EventAnchor → ConsumeIdent → bag["anchor"]
3. If Family.FloatingGuard is non-null:
     Speculatively try to parse FloatingGuard:
       - Save cursor position
       - Attempt to parse Seq(Consume(When), ExprProd())
       - If the current token is When AND parsing succeeds: stash result in bag["stashedGuard"]
       - If no When token: skip (guard is absent), do not restore cursor (nothing was consumed)
4. Read Current().Kind → look up in Family.DisambiguationMap → ConstructKind
5. Retrieve body Grammar from ConstructMeta for that kind
6. Execute body Grammar with current bag (stash available under "stashedGuard")
```

Step 3 is deterministic: `When` is never a disambiguation token, so peeking for `When` has no ambiguity. If `When` is present, parse the guard expression. If not, skip. No speculative backtracking required — it's a one-token peek.

In `BuildNode` for `StateEnsure`, the guard is resolved as:
```
guard = bag.Get("stashedGuard") ?? bag.Get("guard")
```

The body grammar for `StateEnsure` attempts `Opt(When, guard)` for post-condition guards. The stash from `FamilyDispatch` pre-empts this. Both guard positions are expressed cleanly — one in the family definition, one in the body grammar — and assembly logic picks the stash if present.

**No `TryParseStashedGuard` method. No stash parameter passed through method signatures. The catalog describes where the guard goes; the interpreter handles the rest.**

### 4.2 Gap 2: Split-Modifier Problem

**The problem**: `ParseFieldDeclaration` runs `ParseFieldModifierNodes()` twice — once before `->` and once after. A pure left-to-right slot sequence produces two separate modifier arrays that must be concatenated. The split can't be expressed in a flat slot sequence without a `SplitAroundSlot` meta-relationship.

**The resolution: PEG nesting makes it trivial.**

The field grammar (shown in §2.2) wraps the compute expression and post-modifiers in an `Opt(Seq(...))`. The two modifier positions are two separate `Tag("mods1", ...)` and `Tag("mods2", ...)` in the same grammar tree. The interpreter doesn't see a "split" — it sees a sequence with an optional tail that happens to contain another modifier run.

`BuildNode` for `FieldDeclaration`:
```csharp
var mods = bag.GetArray<FieldModifierNode>("mods1")
              .AddRange(bag.GetArray<FieldModifierNode>("mods2"));
```

That's the entire solution. The grammar expresses the syntax; the assembly step concatenates what it finds. **No special metadata relationship needed. No positional metadata. The problem disappears in the PEG model because nested structure is the native currency.**

### 4.3 Gap 3: Variant-Action Detection

**The problem**: `ParseCollectionValueStatement` checks `meta.Kind == ActionKind.Remove && Current().Kind == TokenKind.At` to branch to `RemoveAtStatement`. Similarly for `Append`+`By` and `Enqueue`+`By`. These are mid-parse routing decisions that depend on both the action kind and a subsequent token — neither piece alone is sufficient. The current code is correct but puts domain routing knowledge in the parser.

**The resolution: `ActionVariant` in `ActionMeta` (§3.3).**

The action shape interpreter becomes a pure catalog reader:

```
ParseActionStatement():
  1. meta = Actions.ByTokenKind[Current().Kind]
  2. kw = Advance()                                    // consume action keyword
  3. field = ConsumeIdent()                            // consume field name

  4. // BeforeValue variants (e.g. 'remove at')
     for each v in meta.Variants where v.Position == BeforeValue:
       if Current().Kind == v.TriggerToken:
         return ExecuteVariantShape(v.VariantShape, kw, field)

  5. // Parse base value(s) per meta.SyntaxShape
     result = ExecuteBaseShape(meta.SyntaxShape, kw, field)
     if result is not null: return result     // for FieldOnly and CollectionInto shapes

  6. // AfterValue variants (e.g. 'append by', 'enqueue by')
     for each v in meta.Variants where v.Position == AfterValue:
       if Current().Kind == v.TriggerToken:
         return ExecuteVariantShape(v.VariantShape, kw, field, stashedValue)

  7. return BuildActionStatement(meta, kw, field, stashedValue)
```

`ExecuteBaseShape` returns the parsed value and stashes it for step 6. `ExecuteVariantShape` reads the variant shape and constructs the appropriate result.

**The parser contains zero `meta.Kind` checks.** All branching decisions are driven by `ActionVariant.TriggerToken` — a datum in the catalog. Adding `insertAt`-style variants to new actions in the future requires only a catalog entry, not a parser change.

---

## 5. The Interpreter Loop

The core interpreter is a single recursive method. This is the entire generic machinery:

```
Interpret(ParseRule rule, Cursor cur, ResultBag bag):

  Seq(rules):
    for each r in rules: Interpret(r, cur, bag)

  Opt(rule):
    snapshot = cur.Position
    try: Interpret(rule, cur, bag)
    on ParseFailed: cur.Restore(snapshot)   // backtrack to snapshot

  Rep(rule):
    while true:
      snapshot = cur.Position
      try: Interpret(rule, cur, bag)
      on ParseFailed: cur.Restore(snapshot); break

  Alt(rules):
    for each r in rules:
      snapshot = cur.Position
      try: Interpret(r, cur, bag); return
      on ParseFailed: cur.Restore(snapshot)
    // all alternatives failed — emit diagnostic

  Consume(k):
    if cur.Current().Kind == k: cur.Advance()
    else: emit error (ExpectedToken)           // do NOT throw — just emit diagnostic and advance

  ConsumeLeader():
    cur.Advance()   // already know it's the right token — dispatch loop verified it

  ConsumeIdent():
    if cur.Current().Kind == Identifier: cur.Advance()
    else: emit error; synthetic token

  Tag(name, rule):
    inner = new ResultBag()
    Interpret(rule, cur, inner)
    bag[name] = inner.SingleResult()  // typed production rules produce a single node

  TypeRefProd():   return ParseTypeRef(cur)          // dedicated sub-parser (§6.3)
  ExprProd():      return ParseExpression(cur, 0)    // Pratt loop (§6)
  ModifiersProd(): return ParseModifiers(cur)         // small loop over ModifierKeywords
  // ... other typed productions

  FamilyDispatch(family):
    // as described in §4.1
```

`ParseFailed` is a lightweight sentinel — not an exception. `Opt` and `Alt` use cursor snapshots. Since Precept's grammar is LL(1) at the construct level, speculative parsing only occurs inside `Opt` clauses and `FamilyDispatch`'s floating-guard step. The grammar never requires deep backtracking.

**No per-construct code anywhere in this loop.** It reads grammar rules from metadata and executes them. The per-construct behavior lives in the grammar trees declared in `Constructs.cs`.

---

## 6. Expression Parsing

### 6.1 The Pratt Loop Survives

The Pratt loop in `ParseExpression` is already correctly designed and already mostly catalog-driven. It survives in the radical model. Its residence moves from `Parser.Expressions.cs` to the single `Parser.cs`, because the file split was organizational habit, not structural necessity. The code is essentially unchanged.

Minor cleanup: the three hardcoded branches for `.`, `is set`, and `()` can be unified with catalog entries for `MemberAccess` (precedence 80) and `MethodCall` (precedence 90) operators with `LeftDenotation` parse behavior metadata. This is §5.2 of the review. It's P2 — correct but not load-bearing. The current Pratt loop code is fine; carrying it forward as-is loses nothing.

### 6.2 `ExpressionBoundaryTokens` Becomes Fully Derived

`StructuralBoundaryTokens` is currently hand-listed. In the radical model it's derived from the combinator trees: any `Consume(k)` or `Expect(k)` that appears as the first rule after an `ExprProd()` in any construct grammar is a boundary token. This is computable at startup from the grammar trees. Hand-listing is eliminated.

### 6.3 Type Parsing via `TypeParseShape` Dispatch

`ParseTypeRef` shrinks from 190 lines to a 5-arm switch dispatching on `TypeMeta.ParseShape`:

```
ParseTypeRef(cur):
  current = cur.Current()
  if current.Kind == Tilde: return ParseCISensitive(cur)        // ~string
  if !TypeParseDispatch.TryGetValue(current.Kind, out shape): error
  return shape switch
    ScalarShape     → ParseScalarType(cur)       // keyword [qualifiers]
    CollectionShape → ParseCollectionType(cur)   // keyword 'of' elem [qualifiers] [by P [dir]]
    LookupShape     → ParseLookupType(cur)       // 'lookup of' K 'to' V
    ChoiceShape     → ParseChoiceType(cur)       // 'choice of' ElemType '(' options ')'
```

Each branch is 8–15 lines. `TryPeekQualifierKeyword` is unchanged and shared by `ParseScalarType` and `ParseCollectionType`.

---

## 7. AST Shape

The AST is what the type checker builds on. These are the concrete changes from current types.

### 7.1 Action Statements: Shape-Based, Not Kind-Based

Current design: one C# type per `ActionKind` — 14 statement types with parallel field structures.

New design: **one C# type per `ActionSyntaxShape`** — 7 types, each carrying `ActionMeta` so downstream stages can read the kind.

```csharp
// Replaces: SetStatement, AddStatement, RemoveStatement, EnqueueStatement, PushStatement, AppendStatement
sealed record ActionStatement(SourceSpan Span, ActionMeta Meta, Token Field, Expression Value)
    : Statement(Span);

// Replaces: AppendByStatement, EnqueueByStatement
sealed record ActionByStatement(SourceSpan Span, ActionMeta Meta, Token Field, Expression Value, Expression Key)
    : Statement(Span);

// Replaces: DequeueStatement, PopStatement  
sealed record ActionIntoStatement(SourceSpan Span, ActionMeta Meta, Token Field, Token? IntoField)
    : Statement(Span);

// Replaces: DequeueByStatement
sealed record ActionIntoByStatement(SourceSpan Span, ActionMeta Meta, Token Field, Token? IntoField)
    : Statement(Span);

// Replaces: ClearStatement
sealed record ActionFieldOnlyStatement(SourceSpan Span, ActionMeta Meta, Token Field)
    : Statement(Span);

// Unchanged shapes (already unique — no ambiguity about what they are):
sealed record InsertStatement(SourceSpan Span, Token Field, Expression Value, Expression Index)
    : Statement(Span);
sealed record PutStatement(SourceSpan Span, Token Field, Expression Key, Expression Value)
    : Statement(Span);
sealed record RemoveAtStatement(SourceSpan Span, Token Field, Expression Index)
    : Statement(Span);
```

**Type-checker impact**: The type checker pattern-matches on the shape-based types and reads `Meta.Kind` for kind-specific semantics (e.g., validating that `add` is applied to a collection field). This is identical to the current pattern where the TC reads `ActionMeta`. The C# type now carries the shape; the metadata carries the kind. No breaking change to TC logic — a `SetStatement` was already read as a shape (`AssignValue`) by the TC.

### 7.2 Declaration Nodes: Unchanged

All 12 declaration node types (`FieldDeclarationNode`, `StateDeclarationNode`, `TransitionRowNode`, etc.) keep their current record shapes and property names. The radical design changes *how they're assembled* (from a `ResultBag` rather than hand-coded slot arrays), not *what they contain*.

The type checker sees exactly the same node types it sees today. The only thing that changes from the TC's perspective is action statement types (§7.1) and a cleaner assembly path.

### 7.3 Expression Nodes: Unchanged

All expression node types survive unchanged. The Pratt loop produces the same output as today. This is entirely transparent to the type checker.

### 7.4 Type Reference Nodes: Unchanged

`ScalarTypeRefNode`, `CollectionTypeRefNode`, `LookupTypeRefNode`, `ChoiceTypeRefNode`, etc. — all unchanged. The type parser outputs the same nodes; it just arrives there via `TypeParseShape` dispatch instead of manual if/else branches.

### 7.5 Summary for Type Checker Designer

The type checker can assume:
- **All 12 declaration node types are identical** — same properties, same record shapes
- **All expression node types are identical**
- **All TypeRef node types are identical**
- **Action statement types change**: match on the 8 shape-based types above; read `Meta.Kind` for kind routing; the meta is always attached
- `ActionMeta` is richer: `Variants` is new but the TC doesn't need it — that's parser-only metadata

---

## 8. Disambiguation

Disambiguation survives in the radical model, but it moves from hand-coded method logic to catalog-declared `ConstructFamily` data.

`Constructs.ByLeadingToken` dispatch is **unchanged and correct**. It was always right. The catalog-driven dispatch loop continues to:
1. Look up the leading token in `Constructs.ByLeadingToken`
2. If single unambiguous candidate: execute `ConstructMeta.Grammar` directly
3. If multiple candidates: look up in `Families.ByLeader`, execute `FamilyDispatch`

`FamilyDispatch` is 40–50 lines of interpreter code that replaces the entire `DisambiguateAndParse` method (75 lines of hand-coded switch logic, plus `TryParseStashedGuard`, `ParseStateTargetDirect`, `ParseEventTargetDirect`, and two 15-arm construct switch bodies).

The disambiguation *correctness* is unchanged — the same token sequences disambiguate the same constructs. What changes is that the routing table (`DisambiguationMap`) is declared in `Families.cs` rather than embedded in a method's switch arms. Adding a new disambiguated construct under `in` is a one-line entry in `Families.ByLeader[TokenKind.In].DisambiguationMap`. Today it requires editing `DisambiguateAndParse`.

---

## 9. Error Recovery

The current recovery model is minimal and correct:
- `SyncToNextDeclaration`: skip until `Constructs.LeadingTokens`
- `ConsumeThrough(k)`: skip until closing delimiter
- `TryParseActionStatementWithRecovery`: skip until next `->` or construct boundary

These survive unchanged in the radical model. Recovery is a property of the *cursor infrastructure*, not of per-construct code. The interpreter calls `SyncToNextDeclaration` when a top-level dispatch fails; it calls `ConsumeThrough` when a typed production (like `ChoiceType`) needs bracket-recovery.

**One improvement**: The `Consume` combinator in the interpreter already handles absent-but-expected tokens gracefully (emit diagnostic, produce synthetic token, continue). This means the interpreter naturally recovers at the slot level — if a `because` keyword is missing from a `rule` declaration, the interpreter emits the diagnostic and continues from the next token, rather than aborting the parse. This is slightly *better* than the current behavior in the slot-iteration path, and costs nothing.

There is no slot-level `RecoveryStrategy` metadata (proposed in review §5.3) in this design. The combinator model's implicit recovery (emit + continue) is sufficient for Precept's flat grammar. DSL-specific recovery strategies are over-engineering for a grammar with no deeply nested structures.

---

## 10. What Survives From the Current Design

The radical redesign is surgical. Several pieces of the current parser are exactly right and survive intact:

| Piece | Fate | Reason |
|---|---|---|
| `Constructs.ByLeadingToken` dispatch loop | **Survives** | Already catalog-driven, working perfectly |
| `OperatorPrecedence` FrozenDictionary | **Survives** | Derived correctly from `Operators.All` |
| All 16+ vocabulary FrozenSets | **Survives** | Exemplary catalog derivation, never duplicate |
| `TryPeekQualifierKeyword` | **Survives unchanged** | Best piece of catalog-driven logic in the codebase |
| Pratt loop core | **Survives** | Correct algorithm, already mostly catalog-driven |
| `ParseAtom` | **Survives** | ~165 lines of legitimately heterogeneous atom forms |
| `ParseModifiers` | **Survives** | Already catalog-driven via `Modifiers.ByFieldToken` |
| `ParseInterpolatedString/TypedConstant` | **Survives** | Legitimately complex multi-token sequences |
| `ParseListLiteral` | **Survives** | Correctly sized |
| `SyncToNextDeclaration` | **Survives** | Minimal, correct |
| `InvokeSlotParser` approach | **Superseded** | The PEG interpreter replaces this, but the per-slot-kind typed productions (`TypeRefProd`, `ExprProd`, etc.) are its direct descendants |

| Piece | Fate | Reason |
|---|---|---|
| `ParseFieldDeclaration` (dedicated method) | **Dissolves** | Replaced by `FieldDeclaration.Grammar` interpretation |
| `DisambiguateAndParse` (hand-coded) | **Dissolves** | Replaced by `FamilyDispatch` + `ConstructFamily` data |
| `TryParseStashedGuard` | **Dissolves** | Replaced by `Family.FloatingGuard` combinator |
| `ParseDirectConstruct` switch | **Dissolves** | `ConstructMeta.Grammar` interpretation handles all cases |
| Per-action-kind throw switches (~120 lines) | **Dissolves** | Replaced by `ActionVariant` data + shape interpreter |
| `BuildNode` exhaustive switch | **Shrinks** | Becomes tag-based assembly per kind (~40 lines) |
| `Parser.Declarations.cs` (650 lines) | **Dissolves** | Absorbed into single `Parser.cs` |
| `Parser.Expressions.cs` (406 lines) | **Merges** | Pratt + Atom become sections of single `Parser.cs` |

---

## 11. Size Estimate

| Section | Lines |
|---|---|
| `ParseSession` cursor infrastructure (unchanged) | 80 |
| `ParseAll` top-level dispatch loop | 30 |
| `Interpret` core — full PEG interpreter | 110 |
| `FamilyDispatch` interpreter (anchor + float + disambiguate) | 50 |
| Typed production sub-parsers (TypeRef, Modifiers, etc.) | 100 |
| Action shape interpreter (`ParseActionStatement` + helpers) | 70 |
| Pratt loop (`ParseExpression`) | 105 |
| `ParseAtom` | 165 |
| `BuildNode` (tag-based assembly) | 60 |
| Error recovery helpers | 25 |
| **Total** | **~795 lines** |

Catalog additions (in `Constructs.cs`, `Actions.cs`, `Types.cs`):

| Section | Lines |
|---|---|
| `ParseRule` combinator type hierarchy | 50 |
| `ConstructMeta.Grammar` fields for 12 constructs | 80 |
| `ConstructFamily` type + `Families` static class | 60 |
| `ActionVariant` type + 4 variant declarations | 30 |
| `TypeParseShape` type + 5 shape declarations on `TypeMeta` | 40 |
| **Total** | **~260 lines** |

**Combined new code: ~1,055 lines.**  
**Replaced code: ~1,800 lines across 3 parser files.**  
Net reduction: ~745 lines. And the surviving code is structurally simpler — no 12-arm switches, no 14-arm kind switches, no `#pragma disable CS8524` blocks.

More importantly: every construct-specific behavior has moved from code to data. A new construct requires a grammar rule and a `ConstructMeta` entry. The parser is untouched.

---

## 12. Appendix: Full Grammar Rules (Sketch)

For reference, the approximate grammar combinator trees for all 12 constructs. These become the `Grammar` field of each `ConstructMeta`.

```
PreceptHeader:
  Seq(ConsumeLeader, Tag("name", ConsumeIdent))

FieldDeclaration:
  Seq(ConsumeLeader,
      Tag("names", IdentListProd),
      Consume(As),
      Tag("type",  TypeRefProd),
      Tag("mods1", ModifiersProd),
      Opt(Seq(Consume(Arrow), Tag("expr", ExprProd), Tag("mods2", ModifiersProd))))

StateDeclaration:
  Seq(ConsumeLeader, Tag("entries", StateEntriesProd))

EventDeclaration:
  Seq(ConsumeLeader,
      Tag("names", IdentListProd),
      Opt(Seq(Consume(LeftParen), Tag("args", ArgListProd), Consume(RightParen))),
      Opt(Tag("initial", Consume(Initial))))

RuleDeclaration:
  Seq(ConsumeLeader,
      Tag("guard",     Opt(Seq(Consume(When), ExprProd))),
      Tag("condition", ExprProd),
      Expect(Because),
      Tag("message",   ExprProd))

--- Disambiguated constructs (body grammar only; anchor zone via FamilyDispatch) ---

AccessMode (in ... modify):
  Seq(Consume(Modify),
      Tag("fields", FieldTargetProd),
      Tag("mode",   AccessModeKeywordProd),
      Opt(Seq(Consume(When), Tag("guard", ExprProd))))

OmitDeclaration (in ... omit):
  Seq(Consume(Omit),
      Tag("fields", FieldTargetProd))

StateEnsure (in ... ensure):
  Seq(Consume(Ensure),
      Tag("condition", ExprProd),
      Opt(Seq(Consume(When), Tag("guard", ExprProd))),
      Expect(Because),
      Tag("message", ExprProd))

StateAction (to ... ->):
  Seq(Consume(Arrow),
      Tag("actions", ActionChainProd))

TransitionRow (from ... on):
  Seq(Consume(On),
      Tag("event",   ConsumeIdent),
      Opt(Seq(Consume(When), Tag("guard", ExprProd))),
      Tag("actions", ActionChainProd),
      Tag("outcome", OutcomeProd))

EventEnsure (on ... ensure):
  Seq(Consume(Ensure),
      Tag("condition", ExprProd),
      Opt(Seq(Consume(When), Tag("guard", ExprProd))),
      Expect(Because),
      Tag("message", ExprProd))

EventHandler (on ... ->):
  Seq(Consume(Arrow),
      Tag("actions", ActionChainProd),
      Opt(Seq(Consume(Ensure), Tag("postCondition", ExprProd))))
```

These 12 grammar trees, plus the 4 `ConstructFamily` entries, constitute the complete parse description of Precept's grammar. The parser interprets them. No construct-specific code anywhere.

---

## 13. The Key Insight, Restated

Traditional compilers need per-production methods because their grammars are recursive, context-sensitive, and heterogeneous. Precept's grammar is none of those things. It is flat, keyword-anchored, LL(1) at the construct level, and fully described by a table of ~80 combinator expressions.

The current parser has two paradigms living in the same codebase: the generic slot-iteration path (which handles `state`, `event`, and `rule` correctly), and the hand-written per-construct path (which handles everything else redundantly). The radical design has one paradigm: the generic PEG interpreter path, handling all 12 constructs identically.

The catalog already is the grammar. The parser's only job is to interpret it.

---

*Next: type-checker radical design. Build on §7 (AST Shape) — the declaration and expression node types are unchanged; the action statement types are the only delta to account for.*
