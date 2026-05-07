# Pipeline Stage Design

## When to Apply

When designing a new pipeline stage in a multi-stage compilation/analysis pipeline (lexer → parser → name binder → type checker → graph analyzer → proof engine → builder).

## Pattern

Each pipeline stage in a catalog-driven architecture follows this template:

### 1. Single Responsibility

Each stage owns exactly one category of resolution:
- Lexer: lexical classification (token kinds from catalog)
- Parser: structural recognition (constructs, slots from catalog)
- NameBinder: name resolution (declarations + references)
- TypeChecker: semantic resolution (types, operations, expressions)
- GraphAnalyzer: structural lifecycle reasoning (topology, reachability)
- ProofEngine: safety obligation discharge
- Builder: selective transformation to runtime shapes

**Rule:** If a stage is doing two categories, split it. If a stage needs output from a category it doesn't own, that output should already exist as an earlier stage's artifact.

### 2. Stage Output Shape

```
[StageName] produces [ArtifactRecord]
- The artifact is a sealed record class
- Carries ImmutableArray<Diagnostic> for stage-owned diagnostics
- Carries prebuilt indexes (dictionaries) for O(1) downstream lookup
- Carries back-pointers to source artifacts for LS navigation
- Is deeply immutable (ImmutableArray, ImmutableDictionary, init-only)
```

### 3. Stage Input Contract

A stage receives:
- The **immediately preceding** stage's output (primary input)
- Optionally, an earlier stage's output if the primary doesn't carry what's needed
- Catalog metadata (read-only, always available)

A stage does NOT:
- Re-derive facts the previous stage already established
- Reach backward two stages when the intervening stage could carry the data forward
- Own diagnostics for a category belonging to an earlier stage

### 4. LS Consumption Layering

Each LS feature reads the **earliest sufficient artifact:**
- Lexical features → TokenStream
- Syntax features → ConstructManifest  
- Name-level features → SymbolTable (completions for declared names, fuzzy matching, identifier classification)
- Semantic features → SemanticIndex (hover with type info, operation resolution, typed expressions)
- Lifecycle features → StateGraph/ProofLedger (only for explicit graph/proof surfaces)

**Benefit:** Earlier artifacts are available even when later stages fail. Completions work during type errors; outline works during parse errors.

### 5. Compilation Aggregate

The `Compilation` record carries every stage artifact as a named field. Adding a stage means adding one field to `Compilation`. The merged diagnostic stream accumulates from all stages.

### 6. Naming Convention

| Stage Class | Output Record |
|---|---|
| `Lexer` | `TokenStream` |
| `Parser` | `ConstructManifest` |
| `NameBinder` | `SymbolTable` |
| `TypeChecker` | `SemanticIndex` |
| `GraphAnalyzer` | `StateGraph` |
| `ProofEngine` | `ProofLedger` |

Stage class is the verb/actor. Output record is the noun/artifact.

## Anti-Patterns

- Stage that builds dictionaries another stage already built
- Stage that emits diagnostics for another stage's concern
- LS feature that walks an artifact from two stages ago when the intervening artifact could answer the question
- Output record that mirrors input record structure (flatten to inventory shape)
- Stage that needs general-purpose extension points (pipeline is purpose-built)
