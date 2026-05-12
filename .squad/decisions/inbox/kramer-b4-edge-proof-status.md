# Kramer B4 — edge proof status in state graph

## What shipped

- Added `EdgeProofStatus` to `src/Precept/Pipeline/StateGraph.cs` and threaded the new field through `StateGraph.Empty` plus both `GraphAnalyzer` construction sites.
- Enriched compiled graphs in `src/Precept/Compiler.cs` after `GraphAnalyzer.Analyze(...)` and `ProofEngine.Prove(...)` by projecting unresolved `TransitionRowContext` obligations onto graph edges and storing the result in `StateGraph.EdgeProofStatuses`.
- Updated `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs` to surface the B4 state proof narrative as a `📍 {state} graph position` card, and updated `tools/Precept.LanguageServer/Handlers/HoverHandler.cs` so fallback state identifier hovers also use the rich state card.
- Added regression coverage in `test/Precept.Tests/CompilerEdgeProofStatusTests.cs` and `test/Precept.LanguageServer.Tests/HoverHandlerTests.cs`.

## Files changed

- `src/Precept/Pipeline/StateGraph.cs`
- `src/Precept/Pipeline/GraphAnalyzer.cs`
- `src/Precept/Compiler.cs`
- `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs`
- `tools/Precept.LanguageServer/Handlers/HoverHandler.cs`
- `test/Precept.Tests/CompilerEdgeProofStatusTests.cs`
- `test/Precept.LanguageServer.Tests/HoverHandlerTests.cs`
- `.squad/agents/kramer/history.md`

## Design decisions

- Baked proof status into `StateGraph` rather than re-joining `ProofLedger` inside the language server so hover code can consume a stable graph-level projection.
- Reused `GraphAnalyzer`’s wildcard expansion semantics when projecting rows onto edges: wildcard rows apply to states without an explicit row for the same `(fromState, event)` pair, while explicit rows own their matching edge proof status.
- Aggregated unresolved summaries from `ProofObligation.Requirement.Description` and de-duplicated them per edge so the projection keeps compact human-readable reasons without duplicating proof-engine logic.
- State hover uses incident edges (`from` or `to`) for the B4 card, but the gap narrative remains edge-specific: `From --Event--> To can't be proven`.

## Validation

- `dotnet build`
- `dotnet test test\Precept.Tests\`
- `dotnet test test\Precept.LanguageServer.Tests\`

All three passed on this branch (`4966` core tests, `281` language-server tests).
