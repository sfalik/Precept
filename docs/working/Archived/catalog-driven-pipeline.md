# Catalog-Driven Pipeline: Generic Consumers for a Generic Parser

**Author:** Frank (Lead/Architect)  
**Date:** 2026-05-02  
**Status:** Design exploration — radical branch, pending Shane ruling  
**Builds on:** `parser-radical.md` §0.10 (Option F), `catalog-system.md` §Architectural Identity

---

## §1 — Thesis

In a traditional compiler, consumers hardcode per-node behavior for each AST node type. The type checker has a `case FieldDeclarationNode:` arm. The evaluator has a `case TransitionRowNode:` arm. The language server has N handlers, one per construct. Each consumer maintains its own model of what the language IS — scattered, duplicated, drifting.

The radical parser design inverted this for parsing: the catalog IS the grammar; the parser is generic machinery that reads it. **This document asserts the same inversion applies to every consumer in the pipeline.**

**The thesis:** A catalog-driven consumer dispatches on catalog metadata — not on AST node types. The consumer reads the catalog to know *what to do*, not its own hardcoded per-construct switches. The catalog describes the language's semantic rules; the consumer is a generic engine that evaluates them.

**Is this achievable across ALL pipeline stages?**

Yes — with precisely identified boundaries. The inversion is *fully* achievable for:
- The lexer (keyword recognition, operator/punctuation tables — already catalog-driven today)
- The parser (construct-level dispatch, slot-sequential grammar execution — the radical branch thesis)
- The precept builder (structural assembly from parse output — the most catalog-drivable stage of all)
- The type checker (constraint evaluation is metadata-driven)
- The graph analyzer (transition topology is data)
- The proof engine (obligation kinds are already cataloged)
- The language server (completions, hover, semantic tokens, go-to-definition)
- The MCP tools (they already consume catalogs + diagnostics)

The inversion is *partially* achievable for:
- The evaluator (action dispatch is metadata-driven; expression evaluation has an irreducible Pratt-style core)

The irreducible kernel in every case is **expression evaluation** — the Pratt parser's recursive descent over operators and function calls. Everything above the expression level is flat, keyword-anchored, slot-sequential — and therefore catalog-drivable. This mirrors the parser's own structure: construct-level parsing is generic; expression parsing is a specialized sub-engine.

The upstream stages (lexer, parser, builder) are *already closer to catalog-driven than the downstream consumers*. The lexer derives its keyword table, operator table, and punctuation table from `Tokens.All` today. The parser derives vocabulary recognition sets from catalogs at startup. The radical branch completes this inversion for construct dispatch. The precept builder — pure structural assembly — is arguably the most naturally catalog-driven stage in the entire pipeline.

---

## §2 — The Catalog-Driven Consumer Contract

### What "catalog-driven consumer" means

A catalog-driven consumer is a pipeline stage that:

1. **Iterates catalog entries** rather than hardcoding per-construct/per-action/per-modifier behavior
2. **Reads behavioral metadata from the catalog** rather than encoding domain knowledge in switch arms
3. **Has a single generic dispatch method** whose behavior is parameterized by metadata, not by node type
4. **Requires zero code changes when a new language feature is added** — only catalog metadata additions

### The generic dispatch pattern

**Traditional (switch-on-node-type):**

```csharp
// Type checker — one arm per construct, domain knowledge in every arm
Diagnostic[] Check(DeclarationNode node) => node switch
{
    FieldDeclarationNode f => CheckFieldDeclaration(f),
    TransitionRowNode t    => CheckTransitionRow(t),
    StateEnsureNode s      => CheckStateEnsure(s),
    // ... 12 arms, each with 20-100 lines of per-construct logic
};
```

Every arm encodes "what the language says about this construct." Adding a construct means adding an arm to every consumer. The domain knowledge is scattered across N consumers × M constructs.

**Catalog-driven (dispatch on metadata):**

```csharp
// Type checker — one generic method, metadata-parameterized
Diagnostic[] Check(ParsedConstruct c)
{
    var diagnostics = new List<Diagnostic>();
    
    // Phase 1: Validate slots against their declared types (generic)
    foreach (var (slot, meta) in c.SlotsWithMeta())
    {
        diagnostics.AddRange(ValidateSlot(slot, meta, c.Meta));
    }
    
    // Phase 2: Evaluate construct-level constraints from catalog (generic)
    foreach (var constraint in c.Meta.CheckerConstraints)
    {
        diagnostics.AddRange(EvaluateConstraint(constraint, c));
    }
    
    // Phase 3: Type-check expressions within slots (shared sub-engine)
    foreach (var exprSlot in c.ExpressionSlots())
    {
        diagnostics.AddRange(CheckExpression(exprSlot.Value, c.ResolutionScope));
    }
    
    return diagnostics.ToArray();
}
```

Zero per-construct arms. Adding a construct means adding its `CheckerConstraints` metadata — which declares what the checker must verify. The checker is generic machinery that evaluates declared constraints.

### How it differs from switch-on-node-type

| Dimension | Traditional | Catalog-driven |
|-----------|-------------|----------------|
| Where domain knowledge lives | In consumer switch arms | In catalog metadata fields |
| What adding a construct requires | Touch every consumer | Add metadata entries |
| What the compiler enforces | Exhaustiveness of switch (CS8509) | Completeness of metadata (same CS8509 on catalog switch) |
| What breaks when language evolves | Consumer implementations | Nothing — metadata drives behavior |
| What tests verify | Consumer behavior per construct | Metadata correctness + generic engine behavior |

### The key insight

The type checker doesn't need to know "for a FieldDeclaration, check that modifiers are applicable to the field's type." It needs a generic engine that, given ANY construct, can:
1. Resolve identifiers to their declarations
2. Type-check expressions against expected types
3. Validate that modifiers are applicable (reading applicability from `ModifierMeta.ApplicableTo`)
4. Validate that actions target legal field types (reading legality from `ActionMeta.ApplicableTo`)
5. Validate scope rules (reading scope from `ConstructMeta.AllowedIn`)

Every one of these is *already metadata*. The checker just needs to stop switching on construct kind and start reading the metadata it already has.

---

## §3 — Pipeline Stage Analysis

---

### §3.1 — Lexer

**What it does in the pipeline:**

The lexer transforms raw source text into a `TokenStream` — an ordered sequence of classified `Token` values, each carrying a `TokenKind`, lexeme text, and `SourceSpan`. It is the first pipeline stage: everything downstream operates on tokens, never on raw characters.

**What it needs:**

The lexer needs to answer one question per character sequence: *"what token kind is this?"* For keywords, this requires a mapping from text → `TokenKind`. For operators, a mapping from character(s) → `TokenKind`. For punctuation, the same. For identifiers and literals, structural rules (letter/digit classification, decimal points, escape sequences).

**What catalog metadata is already available:**

| Metadata | Catalog | Already exists? | How the lexer uses it |
|----------|---------|-----------------|----------------------|
| Keyword text → TokenKind | `Tokens.Keywords` (FrozenDictionary) | ✓ Yes | `_keywordLookup.TryGetValue(span, out kw)` — one table lookup |
| Two-char operator table | `Tokens.TwoCharOperators` (FrozenDictionary) | ✓ Yes | Maximal-munch: try (c1,c2) first |
| Single-char operator table | `Tokens.SingleCharOperators` (FrozenDictionary) | ✓ Yes | Fallback after two-char miss |
| Punctuation table | `Tokens.PunctuationChars` (FrozenDictionary) | ✓ Yes | Single-char dispatch |
| Two-char operator starter set | `Tokens.TwoCharOperatorStarters` (FrozenSet) | ✓ Yes | Fast guard before tuple lookup |
| Token categories | `TokenMeta.Categories` | ✓ Yes | Determines which tokens go into which table |

**Current state: the lexer is ALREADY largely catalog-driven.**

The existing `Lexer.cs` does not hardcode keywords, operators, or punctuation. It derives all recognition tables from `Tokens.All` at startup:

```csharp
// From Scanner constructor — the ONLY keyword recognition path:
_keywordLookup = Tokens.Keywords.GetAlternateLookup<ReadOnlySpan<char>>();

// From TryScanOperator — catalog-derived tables:
Tokens.TwoCharOperators.TryGetValue((c, PeekNext), out var two)
Tokens.SingleCharOperators.TryGetValue(c, out var one)

// From TryScanPunctuation — catalog-derived table:
Tokens.PunctuationChars.TryGetValue(Current, out var punc)
```

Adding a new keyword? Add a `TokenKind` member + `TokenMeta` entry. The lexer picks it up — zero lexer code changes. Adding a new operator? Same: add the `TokenMeta` with appropriate `Text` and `TokenCategory.Operator`. The frozen dictionaries rebuild from `Tokens.All`.

**What's missing (for a *fully* catalog-driven scan table):**

- **Lexer mode transitions:** The string/typed-constant/interpolation mode stack is hand-coded structural machinery. Modes handle delimiter nesting (`"..."`, `'...'`, `{...}`), escape sequences, and segment emission. This is not token-kind knowledge — it's scanning architecture.
- **Numeric literal shape:** Integer, decimal, exponent notation (`123`, `3.14`, `2e10`). This is a structural grammar for literals, not expressible as a simple text→kind mapping.
- **Identifier classification:** The `IsLetter`/`IsDigit`/`IsWordChar` predicates define what constitutes a word boundary. This is character-class infrastructure.

**Can it be made fully generic (table-walker reading the catalog)?**

**Already 90% there.** The character-class dispatch structure in `ScanToken()` is irreducible infrastructure — it determines *which scan path to enter* (word? digit? string? operator? punctuation?). But once you're in a scan path, the token classification is catalog-driven:

- Word path → `Tokens.Keywords` lookup
- Operator path → `Tokens.TwoCharOperators` / `Tokens.SingleCharOperators` lookup
- Punctuation path → `Tokens.PunctuationChars` lookup

**The irreducible kernel:**

1. **Character-class routing:** The initial `if (IsLetter) / if (IsDigit) / if (c == '"') / ...` dispatch. This is not token-kind knowledge — it's the structural observation that human-readable text has letters, digits, and symbols, and they require different scanning strategies.
2. **Literal scanning:** Number shape (integer/decimal/exponent) and string shape (escapes, interpolation, delimiters). These are *structural grammars* of literal token classes — they describe how to extract a token's text boundaries, not what kind the token is.
3. **Mode transitions:** The push/pop mode stack for interpolated strings and typed constants. This is nesting infrastructure that no flat table can express.

**What a fully catalog-driven lexer would look like:**

It's already built. The current `Lexer.cs` IS a catalog-driven scan engine with irreducible structural mechanics. The domain knowledge (keywords, operators, punctuation — everything that changes when the language evolves) lives in the `Tokens` catalog. The scanning mechanics (whitespace, newlines, digits, string escapes) are universal infrastructure that would exist in any lexer for any keyword-based language.

**Degree of catalog-drivenness: ~95%.** The only hand-maintained token-kind knowledge in `Lexer.cs` is the mode-transition dispatch for `"`, `'`, `{`, `}` — four delimiter characters that define structural boundaries. Everything else derives from `Tokens.All`.

---

### §3.2 — Parser

**What it does in the pipeline:**

The parser transforms a `TokenStream` into structured parse output (currently a `SyntaxTree` of typed AST nodes; in the radical design, `ParsedConstruct[]`). It recognizes construct boundaries, dispatches to per-construct parse methods, extracts slot values, and invokes sub-parsers for expressions, type references, and modifiers.

**What it needs:**

- Leading-token routing: "which token starts which construct?"
- Disambiguation: "when two constructs share a leader, which secondary token distinguishes them?"
- Slot structure: "for this construct, what slots follow the leader in what order?"
- Vocabulary recognition: "which tokens are type keywords? modifiers? action verbs? operators?"
- Expression parsing: "given an expression slot, parse a full expression with correct precedence"

**What catalog metadata is already available:**

| Metadata | Catalog | Already exists? | Parser usage |
|----------|---------|-----------------|-------------|
| Construct leading tokens | `Constructs.ByLeadingToken` | ✓ Yes | Top-level dispatch |
| Disambiguation tokens | `ConstructMeta.Entries` / `DisambiguationEntry` | ✓ Yes | Family-level peek dispatch |
| Construct grammar trees | `ConstructMeta.Grammar` (radical design) | ✗ Design-only | Would BE the parser |
| Operator precedence | `Operators.All` → `OperatorPrecedence` | ✓ Yes | Pratt expression loop |
| Type keywords | `Types.ByToken` → `TypeKeywords` | ✓ Yes | Type-ref sub-parser |
| Modifier keywords | `Modifiers.All` → `ModifierKeywords` | ✓ Yes | Modifier zone recognition |
| Action keywords | `Actions.All` → action token set | ✓ Yes | Action-chain sub-parser |
| State modifier keywords | `Modifiers.All` (state subset) | ✓ Yes | State modifier zone |

**Current state (existing parser):**

The existing `Parser.cs` already derives vocabulary recognition from catalogs:

```csharp
// Catalog-derived frozen sets (at class level, not per-method):
internal static readonly FrozenDictionary<TokenKind, (int Precedence, bool RightAssociative)> OperatorPrecedence = ...;
internal static readonly FrozenSet<TokenKind> TypeKeywords = Types.ByToken.Keys.ToFrozenSet();
internal static readonly FrozenSet<TokenKind> ModifierKeywords = Modifiers.All.OfType<FieldModifierMeta>()...;
```

But the construct-level dispatch and per-construct parse methods are still hand-coded. Each construct has a dedicated parse method that knows its slot structure.

**The radical design (from `parser-radical.md`):**

The radical branch's thesis — fully developed in `docs/compiler/parser-radical.md` — asserts that the catalog CAN be the grammar and the parser CAN be a generic interpreter. The key elements:

1. **Option F output shape:** `ParsedConstruct(ConstructMeta Meta, ImmutableArray<SlotValue> Slots, SourceSpan Span)` — generic, no per-construct node types.

2. **`ConstructMeta.Grammar` combinator tree:** Each construct declares its syntax as an executable combinator tree:
   ```csharp
   // state Name [initial]
   Grammar = Seq(ConsumeLeader(), Tag("name", ConsumeIdent()), Opt(Tag("initial", Consume(TokenKind.Initial))))
   ```

3. **Slot-sequential dispatch:** The interpreter walks the grammar tree — `Seq` for ordering, `Tag` for named captures, `Opt` for optionals, typed productions (`ExprProd`, `TypeRefProd`, `ActionChainProd`) for sub-parsers.

4. **Leading-token routing from `Constructs.ByLeadingToken`:** The top-level loop reads the current token, looks up the construct(s) it can start, and if multiple share the leader, uses `DisambiguationEntry.DisambiguationTokens` to select.

5. **Routing families:** Header (unique leader, no disambiguation), Direct (unique leader), StateScoped (shared leader `in`/`from`/`to` + disambiguation peek), EventScoped (shared leader `on` + disambiguation peek).

**Can it be made fully generic?**

**Yes, for construct-level parsing.** The radical design demonstrates this completely:
- The grammar tree IS the parse table
- The interpreter IS the parse engine
- Adding a construct = adding a `ConstructMeta` entry with a `Grammar` tree — zero parser code changes

**The irreducible kernel:**

1. **The Pratt expression parser:** Recursive, precedence-climbing, handling binary ops, unary ops, function calls, member access, conditionals (`if/then/else`), quantifiers (`any`/`all`), list literals. This is an algorithmic structure — not flat, not table-walkable. However, *within* the Pratt loop, operator precedence and associativity are catalog-driven via `Operators.All`.

2. **Error recovery heuristics:** When a parse fails mid-construct, the recovery strategy (skip to next leading keyword, synchronize on newline) is heuristic infrastructure, not construct-specific domain knowledge.

3. **The combinator interpreter itself:** The ~15 interpreter methods (`InterpretSeq`, `InterpretTag`, `InterpretOpt`, `InterpretAlt`, `InterpretConsume`, etc.) are the generic engine. They exist once, handle all constructs, and never change when the language evolves.

**Degree of catalog-drivenness:** In the radical design, ~85%. The construct-level dispatch is 100% catalog-driven. The expression sub-parser is the irreducible 15%. The current parser is ~40% catalog-driven (vocabulary sets yes, construct dispatch no).

**Cross-reference:** See `docs/compiler/parser-radical.md` for the full treatment — §0 (grammar shape), §0.10 (Option F), §§1–7 (combinator vocabulary, interpreter, error recovery, and the build step).

---

### §3.3 — Type Checker

**What it needs from the AST/parse output:**

The type checker validates semantic correctness of parsed constructs. It needs:
- The construct kind (to know which constraints apply)
- Slot values (identifiers to resolve, expressions to type, modifiers to validate)
- The resolution scope (what names are in scope — fields, events, states, event args)

**What catalog metadata is already available:**

| Metadata | Catalog | Already exists? |
|----------|---------|-----------------|
| Modifier applicability to types | `ModifierMeta.ApplicableTo` (via `Modifiers`) | ✓ Yes |
| Action applicability to types | `ActionMeta.ApplicableTo` (via `Actions`) | ✓ Yes |
| Action syntax shape (what arguments it expects) | `ActionMeta.SyntaxShape` | ✓ Yes |
| Action allowed contexts | `ActionMeta.AllowedIn` | ✓ Yes |
| Operation legality (which (op, type, type) triples are valid) | `Operations.All` | ✓ Yes |
| Function signatures | `FunctionMeta` (via `Functions`) | ✓ Yes |
| Type families and widening | `TypeMeta` (via `Types`) | ✓ Yes |
| Construct scope rules | `ConstructMeta.AllowedIn` | ✓ Yes |
| Construct slot structure | `ConstructMeta.Slots` / Grammar `Tag` nodes | ✓ Yes |
| Proof obligations per action | `ActionMeta.ProofRequirements` | ✓ Yes |
| Expression form metadata | `ExpressionFormMeta` | ✓ Yes |

**What's missing (needed for fully generic dispatch):**

- **Slot-level type expectations:** The catalog knows *what* slots a construct has, but not what *type* each slot must resolve to in the checker's sense. For example: "the `guard` slot of a TransitionRow must be a boolean expression." This is currently implicit knowledge that would live in per-construct checker logic. **Recommendation:** Add a `ExpectedType` field to slot metadata (or to `Tag` nodes in the grammar tree) — e.g., `Tag("guard", ExprProd(), ExpectedType: TypeKind.Boolean)`.
- **Construct-level semantic constraints:** Rules like "a TransitionRow's target state must exist in the state declarations" or "an AccessMode's field target must exist." These are *relationship* constraints between constructs, not slot-type constraints. **Recommendation:** Model as a `SemanticConstraint[]` field on `ConstructMeta` — a declarative DSL for cross-construct validation rules.

**Can it be made fully generic?**

**Yes, for construct-level checking.** The checker's dispatch loop becomes:

1. For each slot: validate the slot value against its declared type expectation (metadata-driven)
2. For each modifier: validate applicability against the resolved field type (already metadata)
3. For each action: validate target type, syntax shape, scope legality (already metadata)
4. For each expression: recursively type-check using the Pratt-style expression checker (shared sub-engine)
5. For each semantic constraint on the construct: evaluate it (metadata-driven)

**The irreducible core:** Expression type-checking (the Pratt loop — binary ops, function calls, member access, conditionals) is NOT catalog-drivable at the *dispatch* level. The Pratt loop is a recursive algorithm, not a flat table walk. However, *within* the Pratt loop, operations ARE metadata-driven — `Operations.All` declares what (op, type, type) → result-type triples are legal. The loop structure is irreducible; the operation semantics within it are catalog-driven.

**Option F interaction:**

Option F gives the type checker exactly what it needs: `ParsedConstruct` with `ConstructMeta` (to know what constraints apply) and typed `SlotValue[]` (to validate). The accessor layer provides typed extraction. The checker doesn't need per-construct node classes — it needs per-construct *constraint declarations*, which belong in the catalog, not in AST types.

---

### §3.4 — Graph Analyzer

**What it needs from the AST/parse output:**

The graph analyzer validates the state machine's topology: reachability (every state is reachable from initial), dead-state detection (every non-terminal state has an outgoing transition), and transition completeness (every event is handled in every state where it's relevant).

It needs:
- The set of declared states (with modifiers: initial, terminal)
- The set of declared events
- The set of transition rows (source state, event, outcome)
- State-action hooks (entry/exit actions)

**What catalog metadata is already available:**

- State modifier metadata (`StateModifierMeta.AllowsOutgoing`, `StateModifierMeta.RequiresDominator`) — exists
- Transition structure (from the parsed TransitionRow constructs) — from parse output
- Outcome kinds (`Outcomes.All`) — exists

**Can it be made fully generic?**

**Yes — fully.** The graph analyzer doesn't switch on construct kinds at all. It consumes a *derived model*: the state graph. Its inputs are:

```csharp
record StateGraph(
    ImmutableArray<StateNode> States,        // name, modifiers (initial/terminal)
    ImmutableArray<TransitionEdge> Edges,    // source, event, guard?, outcome
    ImmutableArray<EventNode> Events         // name, args
);
```

This derived model is constructed from `ParsedConstruct[]` by filtering for relevant construct kinds and extracting slot values. The construction is generic — it reads `ConstructKind.StateDeclaration`, `ConstructKind.TransitionRow`, `ConstructKind.EventDeclaration` from the catalog and extracts their named slots.

The graph analysis itself (reachability via BFS/DFS, dead-state detection, completeness checking) is pure algorithm over the derived model. It has zero per-construct dispatch — it operates on the *graph abstraction*, not on syntax nodes.

**The irreducible core:** The graph algorithms themselves (BFS, DFS, SCC). These are not catalog-drivable because they are not language-specific — they are graph theory. But they are also not *per-construct* — they are pure infrastructure.

**What IS catalog-drivable:** The semantic rules about what makes a graph valid. "A terminal state must not have outgoing transitions." "The initial state must exist." "Every non-terminal state must be reachable." These rules could be declared as graph constraints in catalog metadata:

```csharp
record GraphConstraint(string Name, GraphConstraintKind Kind, string DiagnosticCode);
enum GraphConstraintKind { Reachability, DeadState, TransitionCompleteness, TerminalNoOutgoing }
```

Whether this is worth the abstraction depends on whether the set of graph constraints is expected to grow. For Precept's fixed set (4-5 rules), the generic machinery may be over-engineered. But the principle holds: the graph analyzer's *behavior* is derivable from declared rules about graph validity.

**Option F interaction:**

The graph analyzer doesn't consume `ParsedConstruct` directly — it consumes a derived `StateGraph` model. Option F is transparent to it. The graph model builder reads `ParsedConstruct[]` and extracts relevant slots. The builder is ~30 lines of generic extraction logic, not per-construct code.

---

### §3.5 — Proof Engine

**What it needs from the AST/parse output:**

The proof engine verifies that expressions cannot fault at runtime — division by zero, overflow, empty-collection access, null dereference. It needs:
- Expression trees (to analyze for potential faults)
- The surrounding constraint context (what guards/rules protect against the fault)
- The proof obligation kinds (`ProofRequirementKind`)

**What catalog metadata is already available:**

- Proof obligation kinds: `ProofRequirements.All` — 5 kinds, each a DU subtype
- Per-action proof requirements: `ActionMeta.ProofRequirements` — which actions demand which proofs
- Expression forms: `ExpressionFormMeta` — structural classification
- Operator semantics: `Operations.All` — which operations can fault (division, modulo)

**What's missing:**

- **Per-expression-form fault potential:** Which expression forms can fault and under what conditions. E.g., `BinaryOperation` with `OperatorKind.Divide` can fault if the right operand is zero. This fault potential is derivable from `Operations.All` (operations that require proof), but the proof engine needs a mapping from "this operation" → "what must be proved."
- **Proof discharge rules:** How a surrounding constraint (guard, rule, modifier) satisfies a proof obligation. E.g., `nonnegative` modifier on a field discharges the "non-zero divisor" proof for `x / field` when combined with `field > 0` in a guard. These rules are currently implicit in the proof engine's logic.

**Can it be made fully generic?**

**Partially.** The proof engine has two layers:

1. **Obligation generation** — "find all potential faults" — is catalog-drivable. The engine walks expressions, checks each operation against `Operations.All` for fault potential, and emits obligation records. This is a metadata-driven scan.

2. **Obligation discharge** — "prove the fault cannot occur given the surrounding context" — has irreducible logical reasoning. Proving that `x > 0` ensures `y / x` won't fault requires understanding arithmetic implication. This is not a table lookup — it's theorem proving (even if simplified).

**Recommendation:** The obligation-generation phase should be fully catalog-driven. Add a `FaultCondition?` field to `OperationMeta` or a separate `ProofObligation` catalog that maps (operation + condition) → obligation kind. The discharge phase is algorithm — like graph analysis, it's pure logic infrastructure, not per-construct code.

**Option F interaction:**

The proof engine operates on expression trees, not construct-level parse output. It receives typed expressions from the type checker's output. Option F is transparent — expressions are `SlotValue` subtypes (`ExprSlot`, etc.) and the proof engine consumes the expression AST regardless of how constructs are represented.

---

### §3.6 — Language Server

**This is Shane's wildcard. Maximum radical thinking applied.**

**Current state:** The language server (`tools/Precept.LanguageServer/`) is a bare shell — `Program.cs` initializes an OmniSharp server with no handlers registered. Everything is to be built. **We have zero legacy to accommodate.**

**What an LS traditionally needs:**

| Feature | Traditional approach | What it typically requires |
|---------|---------------------|---------------------------|
| Diagnostics | Re-run compiler pipeline, emit squiggles | Compiler output (already catalog-driven) |
| Completions | Context-sensitive item lists | Knowledge of what tokens are valid at cursor position |
| Hover | Per-node documentation | Knowledge of what the node IS and its metadata |
| Semantic tokens | Per-token classification | Token → category mapping |
| Go-to-definition | Per-node navigation | Symbol table (identifier → declaration location) |
| Rename | Per-symbol rename | Symbol table + all reference locations |

**The radical hypothesis: a zero-per-construct language server.**

Every LS feature can be derived from catalog metadata + the compiler pipeline's existing output, with ZERO construct-specific LS code. Here's how:

#### Diagnostics — Already Solved

The LS doesn't compute diagnostics. The compiler pipeline does. The LS just runs the pipeline and maps `Diagnostic[]` to LSP `PublishDiagnostics`. This is 100% generic — one handler, all constructs, forever.

#### Completions — Catalog-Derived

At any cursor position, the valid completions are determined by the grammar:

1. **Top-of-line (no leader yet):** Complete with all construct leading tokens from `Constructs.LeadingTokens`.
2. **After a leader, in a slot position:** The `ConstructMeta.Grammar` tree knows what slot type is expected next. The grammar tree is the completion engine:
   - `ExprProd()` → offer field names, event arg names, functions, literals
   - `TypeRefProd()` → offer type keywords from `Types.All`
   - `ModifiersProd()` → offer applicable modifiers from `Modifiers.All` filtered by the field's type
   - `ActionChainProd()` → offer action verbs from `Actions.All`
   - `OutcomeProd()` → offer outcome keywords from `Outcomes.All`
3. **Inside an expression:** Offer identifiers in scope (fields, event args, functions)

**The completion engine is a grammar-tree walker.** Given the cursor position and the construct being typed, it walks the grammar tree to the current position, determines what production is expected, and queries the appropriate catalog for candidates. This is ONE generic completion handler, parameterized by the grammar tree.

```csharp
CompletionItem[] Complete(ParsedConstruct? partial, int cursorOffset)
{
    // Walk grammar tree to cursor position
    var expectedProduction = WalkGrammarToPosition(partial?.Meta.Grammar, cursorOffset);
    
    // Dispatch on production type (slot type, not construct type!)
    return expectedProduction switch
    {
        TypeRefProd    => Types.All.Select(t => CompletionItem(t.Token.Text, t.Description)),
        ModifiersProd  => Modifiers.All.Where(m => ApplicableTo(m, currentFieldType)).Select(...),
        ExprProd       => GetScopeIdentifiers(currentScope).Select(...),
        ConsumeIdent   => GetMatchingIdentifiers(expectedKind).Select(...),
        // ... one arm per production type (~10 types), NOT per construct kind
    };
}
```

**Critical insight:** The dispatch dimension is *slot type* (10 production types), not *construct kind* (12 constructs). Slot types are stable — they are the fundamental vocabulary of the grammar. Constructs grow; production types don't. A new construct with the same slot types needs ZERO completion code changes.

#### Hover — Catalog-Derived

Hover information for any token or identifier comes from catalog metadata:

- **Keyword tokens:** `Tokens.GetMeta(tokenKind).Description` — always available
- **Type keywords:** `Types.GetMeta(typeKind).HoverDocs`
- **Modifier keywords:** `Modifiers.GetMeta(modKind).HoverDescription`
- **Action keywords:** `Actions.GetMeta(actionKind).HoverDescription`
- **Field identifiers:** Resolved field type + modifiers (from semantic model)
- **Event identifiers:** Event name + argument list (from semantic model)
- **Function identifiers:** `Functions.GetMeta(funcKind).Signature`

**One generic hover handler:**

```csharp
Hover? GetHover(Token token, SemanticModel model)
{
    // Try catalog lookups first (keywords, types, modifiers, actions)
    if (TypesByToken.TryGetValue(token.Kind, out var typeMeta))
        return FormatHover(typeMeta.HoverDocs);
    if (ModifiersByToken.TryGetValue(token.Kind, out var modMeta))
        return FormatHover(modMeta.HoverDescription);
    if (ActionsByToken.TryGetValue(token.Kind, out var actionMeta))
        return FormatHover(actionMeta.HoverDescription);
    
    // Fall back to semantic model for identifiers
    if (model.TryResolve(token, out var symbol))
        return FormatHover(symbol.Description);
    
    // Generic token description
    return FormatHover(Tokens.GetMeta(token.Kind).Description);
}
```

Zero per-construct code. The hover handler doesn't know or care what construct the token belongs to — it resolves through catalog indexes.

#### Semantic Tokens — Already Catalog-Derived

`TokenCategory` on `TokenMeta` already classifies every token. The semantic token handler maps `TokenCategory` → LSP `SemanticTokenType`. This is a fixed-size mapping (~17 categories → ~10 LSP types). No per-construct code.

#### Go-to-Definition — Symbol Table

Go-to-definition resolves an identifier to its declaration location. This requires a symbol table — the output of the type checker's name resolution pass. The LS queries the symbol table by position. This is generic: one handler, all identifier kinds, parameterized by the semantic model.

No per-construct LS code needed. The semantic model provides `ResolveAtPosition(line, col) → Declaration?`.

#### **Verdict: The language server CAN be a thin shell with ZERO per-construct code.**

The LS architecture becomes:

```
┌─────────────────────────────────────────────────┐
│  Language Server (thin shell)                     │
│                                                   │
│  ┌──────────────┐  ┌───────────────────────────┐ │
│  │ LSP Protocol │  │ Catalog Indexes            │ │
│  │ (OmniSharp)  │  │ (TypesByToken, etc.)       │ │
│  └──────┬───────┘  └──────────┬────────────────┘ │
│         │                     │                   │
│         ▼                     ▼                   │
│  ┌─────────────────────────────────────────────┐ │
│  │ Generic Handlers (one per LSP method)        │ │
│  │ - DiagnosticsHandler: run pipeline, map      │ │
│  │ - CompletionHandler: walk grammar tree       │ │
│  │ - HoverHandler: query catalog indexes        │ │
│  │ - SemanticTokenHandler: map TokenCategory    │ │
│  │ - DefinitionHandler: query semantic model    │ │
│  └─────────────────────────────────────────────┘ │
│                      │                            │
│                      ▼                            │
│  ┌─────────────────────────────────────────────┐ │
│  │ Compiler Pipeline (catalog-driven)           │ │
│  │ → Parser → Type Checker → Graph → Proof     │ │
│  └─────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────┘
```

**No per-construct LS handlers. No per-construct LS knowledge. The LS is pure infrastructure — it orchestrates the pipeline and maps results to LSP protocol.**

The LS has exactly **two** knowledge sources:
1. **Catalog metadata** (for completions, hover, semantic tokens)
2. **Compiler pipeline output** (for diagnostics, go-to-definition, rename)

Adding a new construct? Add catalog metadata + grammar rule. The LS picks it up automatically — completions include the new keyword, hover shows the new description, diagnostics report new violations. Zero LS code changes.

---

### §3.7 — Precept Builder (Assembler)

**What it does in the pipeline:**

The precept builder takes parse output — whether `SyntaxTree` nodes or `ParsedConstruct[]` — and assembles a `PreceptDefinition`: the typed semantic model that all downstream consumers operate on. It pairs state declarations with transition rows, resolves event references, collects field declarations, builds the constraint model, and produces the coherent domain object that represents a complete precept.

This is **pure structural assembly**. No evaluation. No validation. No type inference. It reads parsed structure and materializes the domain model. This makes it the most naturally catalog-drivable stage in the entire pipeline.

**What it needs from parse output:**

- All parsed constructs, classified by `ConstructKind`
- Slot values from each construct (identifiers, type references, modifiers, expressions, action chains, outcomes)
- Source spans for diagnostic reporting

**What catalog metadata is already available:**

| Metadata | Catalog | Already exists? | Builder usage |
|----------|---------|-----------------|--------------|
| Construct kinds (full enumeration) | `ConstructKind` enum / `Constructs.All` | ✓ Yes | Grouping constructs by kind |
| Slot structure per construct | `ConstructMeta.Grammar` / `Tag` nodes | ✓ Yes | Knowing which slot holds which value |
| Named captures (slot names) | `ExtractNamedCaptures(grammar)` | ✓ Design | Named extraction from `ParsedConstruct` |
| Construct scope rules | `ConstructMeta.AllowedIn` | ✓ Yes | Validation of placement (could be builder or checker) |
| Action kinds | `Actions.All` | ✓ Yes | Classifying actions in action chains |
| Outcome kinds | `Outcomes.All` | ✓ Yes | Classifying transition outcomes |
| Type metadata | `Types.All` | ✓ Yes | Building typed field descriptors |
| Modifier metadata | `Modifiers.All` | ✓ Yes | Building modifier sets per field/state |

**Why this is the MOST catalog-drivable stage:**

The builder does no semantic reasoning. Its job is structural: "take parsed pieces, assemble them into a model." Every piece it handles is already classified by the parser (via `ConstructKind` and typed `SlotValue` subtypes). The assembly rules are:

1. **Group constructs by kind** — trivial filter on `ConstructKind`
2. **Extract slot values by name** — read named captures from `ParsedConstruct.Slots`
3. **Populate model fields** — map slot values to model properties

This is *exactly* what a generic assembly loop looks like:

```csharp
PreceptDefinition Build(IReadOnlyList<ParsedConstruct> constructs)
{
    var builder = new DefinitionBuilder();
    
    foreach (var c in constructs)
    {
        // The catalog tells us what this construct contributes to the model
        var contribution = c.Meta.ModelContribution;  // enum: DeclaresField, DeclaresState, etc.
        
        switch (contribution)
        {
            case ModelContribution.DeclaresField:
                builder.AddField(ExtractField(c));    // generic: reads "name", "type", "mods" slots
                break;
            case ModelContribution.DeclaresState:
                builder.AddState(ExtractState(c));    // generic: reads "name", "mods" slots
                break;
            case ModelContribution.DeclaresEvent:
                builder.AddEvent(ExtractEvent(c));    // generic: reads "name", "args" slots
                break;
            case ModelContribution.AddsTransition:
                builder.AddTransition(ExtractTransition(c));  // reads "state", "event", "guard", etc.
                break;
            // ... ~6 contribution kinds covering all 12 constructs
        }
    }
    
    return builder.Resolve();  // cross-construct wiring
}
```

The `Extract*` methods are also generic — they read named slots from `ParsedConstruct` by slot name. With Option F's `SlotValue[]`:

```csharp
FieldDescriptor ExtractField(ParsedConstruct c)
{
    var name = c.GetIdent("name");          // reads IdentSlot by tag name
    var type = c.GetTypeRef("type");        // reads TypeRefSlot by tag name  
    var mods = c.GetModifiers("modifiers"); // reads ModifiersSlot by tag name
    return new FieldDescriptor(name, type, mods);
}
```

If the catalog declared `ModelContribution` per construct AND the model shape per contribution kind (what fields the `FieldDescriptor`/`StateDescriptor`/etc. expect), the builder becomes fully metadata-parameterized: iterate constructs, look up contribution kind, extract slots by declared names, populate model objects by declared shape.

**What's missing (for fully generic assembly):**

- **`ModelContribution` on `ConstructMeta`:** A declaration of what each construct contributes to the `PreceptDefinition` model. Currently implicit — the builder "knows" that `FieldDeclaration` adds a field and `TransitionRow` adds a transition. This knowledge is trivially catalogable.
- **Slot-to-model-field mapping:** A declaration that "the `name` slot of a `FieldDeclaration` becomes the `Name` property of `FieldDescriptor`." Currently implicit in hand-written builder methods. Could be metadata, but the value is marginal for 12 constructs.

**Can it be made fully generic?**

**Yes — this is the strongest "yes" in the entire pipeline.** The builder has:
- No recursive structures (unlike the expression engine)
- No algorithmic complexity (unlike graph analysis or proof discharge)
- No validation logic (unlike the type checker)
- No precedence concerns (unlike the Pratt parser)

It is pure structural mapping: catalog-classified input → model objects. The catalog already declares the structure of each construct (via grammar/slot metadata). The model already declares its shape (via the `PreceptDefinition` record hierarchy). The builder is the bridge — and that bridge is a generic slot-extraction loop.

**The irreducible kernel:**

1. **Cross-construct resolution:** Pairing a `TransitionRow`'s target state name (a string in a slot) to the actual `StateDescriptor` object. This requires the builder to have assembled all states before resolving transition targets — an ordering dependency. It's trivially handled by a two-pass strategy (collect, then resolve), but it IS logic the builder must contain. It is not, however, *per-construct* logic — it's generic name resolution over any identifier-referencing slot.

2. **Conflict detection:** "Two transition rows with the same (source, event) and overlapping guards" — detecting this requires understanding guard relationships. This is arguably a checker responsibility, not a builder responsibility. If the builder stays pure assembly and delegates all validation to the type checker, even this kernel disappears.

3. **Event argument model construction:** Building the argument signature of an event from its parenthesized parameter list requires understanding the sub-structure of the `ArgListSlot`. This is a small per-slot-type procedure, not a per-construct one.

**The builder's irreducible minimum is vanishingly small.** If the checker owns validation and the parser delivers fully-classified slot values, the builder is a ~50-line generic extraction loop plus a resolution pass. No consumer in the pipeline is more naturally suited to the catalog-driven inversion than this one.

**Option F interaction:**

Option F is the *perfect* input for a catalog-driven builder. `ParsedConstruct` carries its `ConstructMeta` (identity, contribution kind) and typed `SlotValue[]` (named, classified values). The builder reads metadata to know what to build and reads slots to know what values to use. No casting, no guessing, no per-construct destructuring.

---

### §3.8 — Evaluator

**What it needs from the AST/parse output:**

The evaluator is the runtime execution engine. Given a precept definition, current state, and incoming event, it:
1. Selects the matching transition row (source state + event + guard evaluation)
2. Executes the action chain (field mutations)
3. Evaluates constraints (rules, ensures)
4. Produces the new state + field values, or rejects

It needs:
- Transition rows (source, event, guard, actions, outcome) — pre-resolved from checker output
- Action metadata (what each action verb does to a field)
- Expression evaluation (guards, computed expressions, action arguments)
- Constraint evaluation (rules, ensures)

**What catalog metadata is already available:**

- Action semantics: `ActionMeta.SyntaxShape`, `ActionMeta.ApplicableTo` — what each action does structurally
- Type semantics: `TypeMeta` — collection operations, scalar operations
- Constraint kinds: `Constraints.All` — when constraints apply (invariant, state-scoped, event-scoped)

**Can it be made fully generic?**

**Largely yes.** The evaluator's dispatch loop is:

```csharp
// Fully generic transition execution
Result Execute(TransitionRow row, EntityState current, EventArgs args)
{
    // 1. Evaluate guard — generic expression evaluation
    if (row.Guard != null && !EvalBool(row.Guard, current, args))
        return Result.NoMatch;
    
    // 2. Execute actions — dispatch on ActionMeta.SyntaxShape
    var newState = current;
    foreach (var action in row.Actions)
    {
        var meta = Actions.GetMeta(action.Kind);
        newState = ExecuteAction(meta, action, newState, args);
    }
    
    // 3. Evaluate constraints — generic constraint evaluation
    foreach (var constraint in GetApplicableConstraints(newState))
    {
        if (!EvalBool(constraint.Expression, newState, args))
            return Result.Rejected(constraint.Reason);
    }
    
    // 4. Route outcome — dispatch on OutcomeKind (3 members)
    return RouteOutcome(row.Outcome, newState);
}
```

**The catalog-driven action dispatch:**

```csharp
EntityState ExecuteAction(ActionMeta meta, ActionNode action, EntityState state, EventArgs args)
{
    // Dispatch on SyntaxShape — 9 variants, all metadata-driven
    return meta.SyntaxShape switch
    {
        ActionSyntaxShape.AssignValue       => state.SetField(action.Target, Eval(action.Value)),
        ActionSyntaxShape.CollectionValue   => state.CollectionOp(action.Target, meta.Kind, Eval(action.Value)),
        ActionSyntaxShape.CollectionInto    => state.CollectionInto(action.Target, action.Into),
        ActionSyntaxShape.FieldOnly         => state.ClearField(action.Target),
        // ... remaining shapes
    };
}
```

This switches on `ActionSyntaxShape` (a catalog-driven enum), not on `ActionKind` directly. The 9 syntax shapes describe 9 execution patterns that cover all 15 actions. No per-action-kind code — the shape metadata tells the evaluator what to do.

**The irreducible core:** Expression evaluation. `Eval(expr)` is a recursive tree-walker over the expression AST — it handles binary ops, function calls, member access, conditionals, quantifiers, etc. This is the same irreducible kernel as the type checker. The Pratt-style expression structure requires recursive evaluation — it cannot be flattened into a metadata table walk.

However, *within* expression evaluation, operations are metadata-driven:
- Binary operations look up result behavior in `Operations.All`
- Function calls look up signatures in `Functions.All`
- Type coercions follow `TypeMeta` widening rules

**Option F interaction:**

The evaluator doesn't consume `ParsedConstruct` directly. It consumes a *compiled model* — the output of the type checker. This compiled model (resolved transitions, typed actions, resolved constraints) is a level above the parse output. Option F's `ParsedConstruct` is consumed by the type checker; the evaluator sees the checker's typed output. The evaluator is therefore fully insulated from AST representation choices.

---

### §3.9 — MCP Tools

**Shane's hypothesis:** MCP doesn't consume the AST. It consumes (a) catalogs for language description, (b) diagnostics for teaching/context, (c) runtime (inspect/fire/update).

**Validation: Shane is correct.** Let me trace each existing MCP tool:

| Tool | What it consumes | AST involvement |
|------|-----------------|-----------------|
| `precept_language` | Catalogs directly (all 13) | None — enumerates `*.All` properties |
| `precept_compile` | Pipeline output (diagnostics + typed model) | Consumes *compiler output*, not raw AST |
| `precept_inspect` | Runtime engine (compiled definition + state) | None — uses runtime API |
| `precept_fire` | Runtime engine (compiled definition + state + event) | None — uses runtime API |
| `precept_update` | Runtime engine (compiled definition + state + fields) | None — uses runtime API |

**AST contact point:** `precept_compile` receives the pipeline's *typed output* — the fully resolved, validated semantic model. It doesn't receive raw `ParsedConstruct[]`. It receives the model the type checker produces. If the AST changes from typed nodes to Option F's `ParsedConstruct`, the MCP tool is insulated — it sees the *compiled model*, not the parse representation.

**What MCP actually needs from the pipeline:**

1. **Structural output for compile results:** The compiled precept definition — its states, fields, events, transitions, rules, ensures — serialized as JSON. This is a *semantic model* serialization, not an AST serialization. The MCP tool maps `TypedFieldDeclaration`, `TypedTransitionRow`, etc. to DTOs.
2. **Diagnostics as teaching tools:** `Diagnostic[]` with codes, messages, spans — straight from the pipeline.
3. **Runtime operations:** The `PreceptEngine` API — `CreateInstance`, `Inspect`, `Fire`, `Update`.

**Challenge to Shane's hypothesis:** There is ONE edge case. `precept_compile` currently returns *structural information about what was parsed* — field declarations, transition rows, etc. This could be viewed as "consuming the AST." But it's more precisely *consuming the typed semantic model* — the type checker's output, not the parser's output.

**Verdict:** Shane's hypothesis holds with one refinement:

> MCP consumes: (a) catalogs for vocabulary, (b) **the compiled semantic model** for structural output, (c) diagnostics for feedback, (d) runtime engine for operations. It never touches raw parse output.

**Can MCP be fully catalog-driven?**

`precept_language` is ALREADY fully catalog-driven — it iterates `*.All` on every catalog. The remaining tools consume the runtime and pipeline output, which are themselves catalog-driven. MCP inherits the catalog-driven property transitively.

**Option F interaction:** Completely transparent. MCP never sees `ParsedConstruct`. It sees the compiled model (from the checker) and the runtime engine (from the compiler). The parser's internal representation is invisible to MCP.

---

## §4 — AST Interaction Model

### What shape does the AST actually need to be?

Given the consumer analysis, the requirements on the parser output are:

| Consumer | What it reads from parse output | Shape requirement |
|----------|--------------------------------|-------------------|
| Precept Builder | `ParsedConstruct[]` — all constructs with slot values | Generic construct + typed slots |
| Type Checker | `ParsedConstruct` with slot values + `ConstructMeta` | Generic construct + typed slots |
| Graph Analyzer | Filtered subset of constructs (states, transitions, events) | Iterable construct stream |
| Proof Engine | Expression trees within slots | Expression AST (sub-AST of slots) |
| Evaluator | Compiled model from type checker | Doesn't read parse output directly |
| Language Server | Compiler pipeline output + catalog indexes | Doesn't read parse output directly |
| MCP Tools | Compiled model + runtime | Doesn't read parse output directly |

**Only the builder and type checker directly consume the parse output.**Every other consumer either reads a derived model (graph analyzer, evaluator) or reads the compiler pipeline's *typed output* (LS, MCP).

This is a crucial finding: **the AST's shape is determined by exactly one consumer — the type checker.** If the type checker can work with Option F's `ParsedConstruct`, then Option F is sufficient for the entire pipeline.

### Does Option F hold up?

**Yes — emphatically.** The consumer analysis strengthens Option F:

1. **The type checker** needs `ConstructMeta` (for constraint lookup) + typed `SlotValue[]` (for validation). Option F provides exactly this.
2. **No other consumer** needs raw parse output. They all consume downstream typed models.
3. **The accessor layer** (Option F's typed extraction functions) serves as the type checker's entry point into slot values. It provides the same type safety as a class hierarchy, without requiring per-construct classes.
4. **The catalog-driven type checker** doesn't pattern-match on node types at all — it iterates constraints from metadata. It doesn't *need* a typed AST class hierarchy because its dispatch axis is *constraint kind*, not *construct kind*.

### A subtle implication: the accessor layer may be unnecessary

If the type checker is truly catalog-driven — dispatching on `ConstructMeta` constraints rather than switching on `ConstructKind` — it may not need the typed accessor functions at all. Consider:

```csharp
// Type checker doesn't destructure per-construct —
// it validates slots generically by their declared type expectations
foreach (var slot in c.Slots)
{
    if (slot is ExprSlot expr && GetExpectedType(c.Meta, slotIndex) == TypeKind.Boolean)
        CheckBooleanExpression(expr.Value);
    else if (slot is IdentSlot ident)
        CheckIdentifierResolvable(ident.Value, GetIdentifierScope(c.Meta, slotIndex));
    // ...
}
```

The dispatch is on *slot type* (the `SlotValue` DU — 8 subtypes), not on construct kind (12 members). This means the accessor layer's per-construct extraction functions might be an unnecessary intermediate step. The type checker can work directly with the `SlotValue[]` and `ConstructMeta`, dispatching on slot kind.

**Revised recommendation:** Option F remains correct as the parser output shape. But the accessor layer should be viewed as **optional convenience**, not **architectural requirement**. A fully catalog-driven type checker may bypass it entirely, dispatching on slot type + metadata rather than destructuring into per-construct tuples.

### What this implies about what the parser must produce

The parser must produce:
1. **`ConstructMeta`** — identity and metadata for the construct
2. **`SlotValue[]`** — typed slot values, ordered by grammar position
3. **`SourceSpan`** — for diagnostic reporting

This is exactly Option F's `ParsedConstruct(ConstructMeta Meta, ImmutableArray<SlotValue> Slots, SourceSpan Span)`.

The parser does NOT need to produce:
- Per-construct typed nodes (Options C, status quo)
- A lossless CST (Option E)
- Positional arrays without slot types (Option B)

**The synthesis:** Option F is confirmed as the right parser output. The consumer-generic analysis *strengthens* it — because catalog-driven consumers don't need per-construct node types, they need `ConstructMeta` + typed slots. Option F is precisely that.

---

## §5 — What Is Irreducible

### Expression evaluation — the universal kernel

Every consumer that touches expressions hits the same irreducible core: the recursive Pratt-style expression evaluator. This manifests differently in each context:

- **Type checker:** `InferType(expr)` — recursive type inference over the expression tree
- **Evaluator:** `Eval(expr)` — recursive value computation over the expression tree
- **Proof engine:** `FindObligations(expr)` — recursive obligation discovery over the expression tree

The expression tree (binary ops, unary ops, function calls, member access, conditionals, quantifiers, list literals) requires recursive traversal. This is not catalog-drivable at the *structural* level because the structure is recursive — you cannot flatten a tree walk into a table lookup.

**However:** Within the expression evaluator, *individual operations* are catalog-driven:
- Binary operation semantics → `Operations.All`
- Function behavior → `Functions.All`
- Type coercion → `Types.All` widening rules
- Operator precedence → `Operators.All`

**The irreducible minimum is the recursive structure.** The individual operations within that structure are all metadata-parameterized.

### Construct-level semantic relationships

Some validation rules require understanding relationships *between* constructs:
- "A transition row's target state must exist in the state declarations"
- "An access-mode's field must exist in the field declarations"
- "A state-ensure's state must exist"
- "An event-ensure's event must exist"

These are cross-construct reference validation rules. They require a symbol table — a mapping from names to declarations. Building the symbol table is a single-pass scan of all constructs, extracting declared names by slot type. This is generic (iterate constructs, for each identifier-declaring slot, register the name).

**Validating** cross-references is also generic: "for each identifier-referencing slot, verify the name exists in the appropriate namespace." The namespace (states, events, fields) is determinable from the slot's type expectation in the grammar.

**This is NOT irreducible per-construct logic.** It is generic reference resolution driven by slot metadata. The catalog can declare "this slot references a state identifier" vs. "this slot declares a state identifier" — that's the only per-slot metadata needed.

### Where per-construct logic genuinely lives

After the full analysis, the *only* places per-construct knowledge exists are:

1. **The grammar tree** — `ConstructMeta.Grammar` — one declaration per construct defining its syntax shape
2. **Slot-level type expectations** — what type each slot must resolve to (boolean expression, state identifier, field reference)
3. **The accessor layer** (if used) — one function per construct mapping slots to named tuples

Items 1 and 2 are catalog metadata. Item 3 is optional convenience code. **There is no irreducible per-construct *consumer* logic.** All consumer behavior is derivable from metadata about slots and constructs.

This is the key finding: **per-construct knowledge is irreducible, but it lives in the catalog, not in consumer code.**

---

## §6 — Design Recommendations

Each pipeline stage is addressed below in execution order. Entries answer: what to build, what metadata to add, what the generic dispatch looks like, and what stays hand-written. No hedging — this is the implementation playbook.

### §6.1 — Lexer

**Recommendation:** Leave it alone. The lexer is already catalog-driven to ~95%. Every keyword, operator, and punctuation character derives from `Tokens.All` at startup. There is no per-token-kind switch in the scan loop — it's table lookups against frozen dictionaries.

**New catalog metadata needed:** None. The `Tokens` catalog already carries everything the lexer reads.

**Generic dispatch pattern:** Already implemented. Character-class routing → table lookup against `Tokens.Keywords`, `Tokens.TwoCharOperators`, `Tokens.SingleCharOperators`, `Tokens.PunctuationChars`. Adding a keyword = adding a `TokenMeta` entry. Zero lexer code changes.

**Irreducible core:** Character-class dispatch (`IsLetter`, `IsDigit`, string/interpolation delimiters), literal scanning (number shape, escape sequences), and mode transitions (push/pop for interpolated strings). These are universal scanning mechanics — they exist in any lexer for any keyword-based language. They are not token-kind knowledge and they do not change when the language evolves.

**Do not:** Attempt to metadata-drive the mode stack or literal grammar. These are structural scanning concerns, not token classification concerns.

### §6.2 — Parser

**Recommendation:** The radical branch (§3.2 cross-ref, `parser-radical.md`) replaces per-construct parse methods with a grammar-tree interpreter. Build the ~15 combinator interpreter methods (`InterpretSeq`, `InterpretTag`, `InterpretOpt`, `InterpretAlt`, `InterpretConsume`, etc.) once. They handle all constructs forever. Construct dispatch becomes: read current token → `Constructs.ByLeadingToken` → if ambiguous, peek using `DisambiguationEntry.DisambiguationTokens` → execute `ConstructMeta.Grammar` tree.

**New catalog metadata needed:** `ConstructMeta.Grammar` — the combinator tree per construct. This is the radical branch's core addition. Each construct declares its syntax as an executable `ParseRule` tree. No other metadata additions required; vocabulary sets (`TypeKeywords`, `ModifierKeywords`, `OperatorPrecedence`) already exist.

**Generic dispatch pattern:**
```csharp
ParsedConstruct ParseConstruct(TokenStream tokens)
{
    var leader = tokens.Current.Kind;
    var candidates = Constructs.ByLeadingToken[leader];
    var meta = candidates.Length == 1
        ? candidates[0]
        : Disambiguate(candidates, tokens);
    var slots = InterpretGrammar(meta.Grammar, tokens);
    return new ParsedConstruct(meta, slots, span);
}
```

**Irreducible core:** The Pratt expression parser — recursive, precedence-climbing, handling binary/unary ops, function calls, member access, conditionals, quantifiers, list literals. Within the Pratt loop, operator precedence and associativity ARE catalog-driven via `Operators.All`. But the recursive structure of expression parsing is algorithmic — it cannot be flattened into grammar-tree interpretation. The combinator interpreter also stays hand-written, but it is generic infrastructure (written once, serves all constructs).

**Do not:** Try to express expression grammar as a combinator tree. Expressions are recursive and precedence-governed — the Pratt parser IS the right tool. Don't fight it.

### §6.3 — Type Checker

**Recommendation:** Build a single generic validation engine. No per-construct switch arms. Dispatch on `SlotValue` subtype (8 subtypes) × metadata from `ConstructMeta`, not on `ConstructKind` (12+ members). The checker iterates constructs, reads their slot metadata, and evaluates generic validation phases.

**New catalog metadata needed:**
- `SlotTypeExpectation` on grammar `Tag` nodes (or parallel array on `ConstructMeta`): what resolved type each slot must produce. E.g., the `guard` slot of a TransitionRow expects `TypeKind.Boolean`.
- `SlotReferenceKind` (declares vs. references): whether an identifier slot declares a name or references one. This drives cross-construct name resolution generically.
- `SemanticConstraint[]` on `ConstructMeta` (optional): declarative cross-construct validation rules. Whether this is worth abstracting for 12 constructs is a Shane call.

**Generic dispatch pattern:**
```csharp
Diagnostic[] Check(ParsedConstruct c)
{
    // Phase 1: Validate slots against declared type expectations (generic)
    foreach (var (slot, meta) in c.SlotsWithMeta())
        diagnostics.AddRange(ValidateSlot(slot, meta));

    // Phase 2: Validate modifiers (from ModifierMeta.ApplicableTo)
    // Phase 3: Validate actions (from ActionMeta.ApplicableTo + AllowedIn)
    // Phase 4: Type-check expressions (recursive Pratt sub-engine)
    // Phase 5: Cross-reference resolution (from SlotReferenceKind)
}
```

**Irreducible core:** Expression type-checking. `InferType(expr)` is a recursive tree-walker — binary ops, function calls, member access, conditionals. Within the Pratt loop, operation legality IS catalog-driven (`Operations.All` declares valid `(op, type, type) → result-type` triples). The recursive structure is irreducible; the operation semantics within it are metadata-parameterized.

**Do not:** Build the accessor layer. Shane ruled it YAGNI. If the checker dispatches on `SlotValue` subtype generically, it never needs per-construct typed extraction functions. If an ergonomic need emerges later, consider alternatives (source-generated helpers, extension methods on `ParsedConstruct`) before building a full accessor layer.

### §6.4 — Graph Analyzer

**Recommendation:** The graph analyzer is already clean — it consumes a derived `StateGraph` model, not parse output. Build a ~30-line generic `StateGraph` builder that filters `ParsedConstruct[]` for states/transitions/events and extracts slot values by name. The graph algorithms themselves (BFS, DFS, SCC for reachability and dead-state detection) are pure infrastructure.

**New catalog metadata needed:** None. State modifiers (`initial`, `terminal`) are already in `Modifiers.All`. The graph builder reads `ConstructKind` + named slots — both already exist.

**Generic dispatch pattern:** Not applicable at the graph-algorithm level. The `StateGraph` builder is the only catalog-touching code:
```csharp
StateGraph BuildGraph(IReadOnlyList<ParsedConstruct> constructs)
{
    var states = constructs.Where(c => c.Meta.Kind == ConstructKind.StateDeclaration)
        .Select(c => new StateNode(c.GetIdent("name"), c.GetModifiers("modifiers")));
    // ... similarly for transitions, events
}
```

**Irreducible core:** The graph algorithms (BFS, DFS, SCC). These are graph theory, not language-specific. They are also not per-construct — they operate on the graph abstraction. The graph constraint set (reachability, dead-state, terminal-no-outgoing, completeness) is small and stable. Declaring these as metadata is possible but likely over-engineered for 4–5 rules.

**Do not:** Over-abstract the graph constraints into a declarative catalog. The set is small, stable, and unlikely to grow. Write them as straightforward algorithm calls on the `StateGraph`.

### §6.5 — Proof Engine

**Recommendation:** Split into two clean phases. Phase 1 (obligation generation) is fully catalog-driven: walk expression trees, check each operation against `Operations.All` for fault potential, emit `ProofObligation` records. Phase 2 (obligation discharge) is algorithmic — proving that guards/modifiers/constraints prevent the fault.

**New catalog metadata needed:**
- `FaultCondition?` on `OperationMeta`: declares under what condition the operation faults (e.g., `RightOperandZero` for division). This is how the obligation generator knows what to emit.
- Consider a `DischargeRule` catalog later if discharge patterns become repetitive. Start with hand-written discharge logic — the proof engine is the newest, least-proven stage.

**Generic dispatch pattern (obligation generation):**
```csharp
IEnumerable<ProofObligation> FindObligations(TypedExpression expr)
{
    foreach (var op in expr.WalkOperations())
    {
        var meta = Operations.GetMeta(op.Kind);
        if (meta.FaultCondition is { } fault)
            yield return new ProofObligation(op, fault);
    }
}
```

**Irreducible core:** Obligation discharge. Proving that `x > 0` ensures `y / x` cannot fault requires arithmetic reasoning. This is theorem proving, not table lookup. Keep it algorithmic. The discharge engine is small, focused, and explicitly irreducible.

**Do not:** Over-catalog the discharge rules prematurely. Start with the hand-written discharge engine. If patterns repeat across obligation kinds, factor them into metadata later. Premature abstraction here risks a metadata schema that can't express the interesting cases.

### §6.6 — Language Server

**Recommendation:** Build a zero-per-construct LS. Every handler is generic: one per LSP method, each <50 lines. The LS has exactly two knowledge sources: catalog metadata (for completions, hover, semantic tokens) and compiler pipeline output (for diagnostics, go-to-definition). No per-construct LS handlers. No per-construct LS knowledge.

**New catalog metadata needed:** None for the initial build. Existing hover descriptions (`TokenMeta.Description`, type/modifier/action hover text), token categories (`TokenCategory`), and type descriptions are sufficient. Grammar-tree walking for completions uses `ConstructMeta.Grammar` — already planned for the parser.

**Generic dispatch pattern:** Five handlers, each fully generic:
1. **DiagnosticsHandler:** Run the compiler pipeline → map `Diagnostic[]` to LSP `PublishDiagnostics`. One handler, all constructs, forever.
2. **CompletionHandler:** Walk `ConstructMeta.Grammar` tree to cursor position → determine expected production → query the appropriate catalog. Dispatch on *production type* (~10 types), not construct kind.
3. **HoverHandler:** Resolve token through catalog indexes (`TypesByToken`, `ModifiersByToken`, `ActionsByToken`) → fall back to semantic model for identifiers → fall back to `TokenMeta.Description`.
4. **SemanticTokenHandler:** Map `TokenCategory` → LSP `SemanticTokenType`. Fixed-size mapping (~17 categories).
5. **DefinitionHandler:** Query semantic model `ResolveAtPosition(line, col)`. Pure symbol-table lookup.

**Irreducible core:** The grammar-tree cursor mapper for completions — determining "where am I in the grammar given partial input?" This is a focused sub-problem worth a dedicated design pass. It is not per-construct code, but it IS non-trivial infrastructure.

**Do not:** Write per-construct completion providers, per-construct hover resolvers, or per-construct anything. The LS is a green field — we have zero legacy to accommodate. Build it catalog-driven from day one.

### §6.7 — Precept Builder (Assembler)

**Recommendation:** This is the strongest candidate for a first catalog-driven proof-of-concept. The builder has the most naturally catalog-drivable architecture in the entire pipeline — near-zero irreducible kernel. Build it as a generic slot-extraction loop parameterized by `ModelContribution` metadata on `ConstructMeta`.

The builder does no semantic reasoning, no evaluation, no type inference, no precedence handling. It is pure structural mapping: catalog-classified input → model objects. If any stage should demonstrate that the catalog-driven inversion works end-to-end, it is this one. Start here.

**New catalog metadata needed:**
- `ModelContribution` on `ConstructMeta`: declares what each construct contributes to the `PreceptDefinition` model. Values: `DeclaresField`, `DeclaresState`, `DeclaresEvent`, `AddsTransition`, `AddsConstraint`, `AddsAccessMode`, etc. Currently implicit in hand-written builder code — trivially catalogable.
- Slot-to-model-field mapping (optional): declares which slot name maps to which model property. For 12 constructs with 2–4 slots each, the value over hand-written `Extract*` methods is marginal. Consider adding later if the construct count grows.

**Generic dispatch pattern:**
```csharp
PreceptDefinition Build(IReadOnlyList<ParsedConstruct> constructs)
{
    var builder = new DefinitionBuilder();
    foreach (var c in constructs)
    {
        switch (c.Meta.ModelContribution)
        {
            case ModelContribution.DeclaresField:
                builder.AddField(ExtractField(c));  // reads "name", "type", "mods" slots
                break;
            case ModelContribution.DeclaresState:
                builder.AddState(ExtractState(c));
                break;
            // ... ~6 contribution kinds total
        }
    }
    return builder.Resolve();  // cross-construct wiring (name resolution)
}
```

**Irreducible core:** Vanishingly small. Cross-construct name resolution (pairing a transition's target state name to the actual `StateDescriptor`) requires a two-pass strategy — collect declarations, then resolve references. This is generic name resolution, not per-construct logic. Event argument model construction (building arg signatures from `ArgListSlot`) is a small per-slot-type procedure. If the checker owns all validation, the builder's irreducible kernel is ~50 lines of generic extraction plus a resolution pass.

**Do not:** Combine validation with assembly. Keep the builder pure assembly; let the type checker own all semantic validation. A builder that validates is a builder that duplicates checker logic.

### §6.8 — Evaluator

**Recommendation:** Action dispatch is already catalog-driven via `ActionMeta.SyntaxShape` — 9 syntax shapes cover all 15 actions. No per-action-kind code. Guard evaluation, constraint evaluation, and outcome routing are each generic. The evaluator's structure is already correct; no architectural changes needed.

**New catalog metadata needed:** None. `ActionMeta.SyntaxShape` already provides the dispatch axis. The evaluator consumes the compiled model (type checker output), not raw parse output — it is fully insulated from AST representation choices.

**Generic dispatch pattern:**
```csharp
Result Execute(TransitionRow row, EntityState current, EventArgs args)
{
    if (row.Guard != null && !EvalBool(row.Guard, current, args))
        return Result.NoMatch;

    var newState = current;
    foreach (var action in row.Actions)
    {
        var meta = Actions.GetMeta(action.Kind);
        newState = ExecuteAction(meta.SyntaxShape, action, newState, args);
    }

    foreach (var constraint in GetApplicableConstraints(newState))
        if (!EvalBool(constraint.Expression, newState, args))
            return Result.Rejected(constraint.Reason);

    return RouteOutcome(row.Outcome, newState);
}
```

**Irreducible core:** Expression evaluation — `Eval(expr)` — the same recursive Pratt-style tree-walker that appears in the parser and type checker. Binary ops, function calls, member access, conditionals, quantifiers. Within the loop, operation semantics are catalog-driven (`Operations.All`, `Functions.All`, `Types.All` widening). The recursive structure is irreducible. This is the universal kernel shared across parser, checker, and evaluator.

**Do not:** Try to flatten expression evaluation into metadata. The Pratt expression evaluator is an irreducible algorithmic structure. It appears in three consumers (parser, type checker, evaluator) because expressions ARE recursive. Accept it.

### §6.9 — MCP Tools

**Recommendation:** MCP is already correctly positioned. It consumes catalogs for vocabulary (`precept_language`), compiled semantic model + diagnostics for feedback (`precept_compile`), and the runtime engine for operations (`precept_inspect`, `precept_fire`, `precept_update`). It never touches raw parse output. No architectural changes needed.

**New catalog metadata needed:** None. When core model types or catalog records change, the MCP DTOs in `tools/Precept.Mcp/Tools/` need corresponding updates — but this is maintenance, not new metadata. Keep the MCP tools thin (<30 lines of non-serialization code per tool method).

**Generic dispatch pattern:** Already in place. `precept_language` iterates `*.All` on every catalog. The remaining tools call core APIs. MCP inherits the catalog-driven property transitively from the pipeline stages it consumes.

**Irreducible core:** None. MCP is pure orchestration — it calls APIs and serializes results. The only maintenance cost is keeping DTOs in sync with core types. The `LanguageTool.cs` `FirePipeline` array must track pipeline stage changes.

**Do not:** Duplicate domain logic in MCP tool methods. If a tool method exceeds ~30 lines of non-serialization code, the logic belongs in `src/Precept/`, not in MCP.

---

## §7 — Implications for the Radical Branch

### Decisions in `parser-radical.md` that are LOCKED by this analysis

1. **Option F (`ParsedConstruct`) is confirmed as the correct parser output.** The consumer analysis shows that only the type checker directly consumes parse output, and the type checker can work generically with `ParsedConstruct` + metadata. No consumer needs per-construct node classes.

2. **The `SlotValue` DU (8 subtypes) is the correct slot representation.** Consumers dispatch on slot *type* (ExprSlot, IdentSlot, TypeRefSlot, etc.), not on construct kind. The DU provides exhaustive matching over slot types — a stable, small set.

3. **The grammar tree (`ParseRule Grammar` on `ConstructMeta`) is the LS's completion engine.** This is an additional motivation for the radical parser design: the grammar tree serves double duty as both the parser's execution blueprint AND the LS's completion-context engine.

4. **`ConstructMeta` must carry semantic metadata beyond syntax.** The radical parser design treats `ConstructMeta` as a grammar description. The consumer analysis shows it must also carry semantic metadata — slot type expectations, reference kinds, possibly semantic constraints. This is an expansion of `ConstructMeta`'s role from "parser data" to "pipeline data."

5. **The accessor layer (Option F's typed extraction functions) should NOT be built.** YAGNI: if a concrete consumer need emerges, alternatives (source-generated helpers, extension methods) must be evaluated before defaulting to a full accessor layer.

6. **Semantic constraint metadata lives in enriched `Tag` nodes, supplemented by `CrossConstructConstraint[]` on `ConstructMeta`.** Add `TypeExpectation?` and `ReferenceKind?` to `Tag` — per-slot expectations co-located with the slot they constrain (one lookup, all consumers served). Cross-construct relationship rules that cannot be expressed on a single slot node (e.g., "transition target must name a declared state") go in `CrossConstructConstraint[]` on `ConstructMeta`. A parallel sibling array (Option b) is rejected: it creates a split-brain where slot identity lives in the grammar tree but slot semantics live beside it. Derivation from existing metadata (Option c) fails for construct-specific slot knowledge that has no derivation source.

7. **Catalog structural discharge shortcuts; keep guard-expression discharge algorithmic.** Obligation *generation* is already fully catalog-driven. For discharge: finite O(1) lookup rules — modifier-to-interval implications (`nonzero` → `≠ 0`; `positive` → `> 0`, `≠ 0`, `≥ 0`; `nonnegative` → `≥ 0`; `notempty` → `count > 0`), declaration-vacuity (non-optional field satisfies Presence), modifier-presence (Modifier kind), and identity (QualifierCompatibility) — are expressed as `DischargeImplication[]` on `FieldModifierMeta`. Guard-expression reasoning (interval arithmetic, transitive implication, compound guards) stays algorithmic: it operates over an unbounded user-written expression space and cannot be reduced to a finite table without the catalog becoming Turing-complete. Over-cataloging is rejected as the worse failure mode — a false structural discharge is silent and allows a fault through to production. `DischargeImplication` lives on `FieldModifierMeta` directly (not a separate catalog) so the modifier entry is self-describing. The `.at(N)` full-bounds obligation gap is a known engineering simplification, not a missing obligation kind — the 5-kind set is complete for the current language surface.

### Decisions that remain OPEN

All decisions from this analysis are now locked. See §7 Decisions Locked by This Analysis above.


### What the radical parser enables for consumers

The radical parser's key contribution to consumers is NOT just "zero per-construct parser code." It is:

**The grammar tree is a machine-readable, runtime-accessible description of every construct's structure.**

This means:
- The LS can walk it for completions
- The type checker can read it for slot expectations
- Documentation generators can read it for syntax descriptions
- AI agents can read it to understand construct shapes
- Future tooling we haven't imagined can read it

This is the catalog-system principle applied to its logical conclusion: the grammar IS metadata, the metadata IS the language specification, and EVERY consumer derives from it. The parser is just the first consumer. The type checker is the second. The LS is the third. They all read the same artifact — `ConstructMeta.Grammar` — for different purposes.

---

*This document establishes the architectural direction for the radical branch's consumer design. The thesis is confirmed: catalog-driven consumers are achievable across the entire pipeline, with expression evaluation as the single irreducible algorithmic kernel. The parser produces `ParsedConstruct`; every consumer reads catalog metadata to know what to do with it. Per-construct code exists only in the catalog — never in consumer implementations.*
