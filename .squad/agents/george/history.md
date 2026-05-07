## Core Context



- Owns code-level feasibility, parser/runtime implementation detail, and architecture-to-code translation across checker, analyzer, and tooling surfaces.

- Parser and checker work stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.

- Shared-environment build discipline matters: targeted build/test commands are safer than full-solution runs when the workspace may have external file locks.



## Learnings



- Closed-vocabulary slot data resolves at parse time: types, modifiers, access modes, and `because` text should not be deferred as raw spans.

- `peek(2)` is structural parser geometry for scoped-construct disambiguation because the anchor production is a single token.

- Optional construct slots still produce sentinel `SlotValue`s; downstream code should not assume omitted entries shrink the slot array.

- New parser metadata should expose the lookup axis the parser actually queries; branch-local linear scans are a smell.

- PRECEPT0019-style exhaustiveness is valuable when it is attached to the true owner of a catalog axis, not sprayed broadly.



## Recent Updates



### 2026-05-07T08:42:03-04:00 — Bare `<-` parse defect fixed; `ExpressionFormKind` exhaustiveness enforced

- `ParseComputeExpression` now validates the token after `BackArrow` against a catalog-derived expression-start set; bare `<-` emits `ExpectedToken("expression")` and recovers structurally instead of synthesizing a fake compute expression.

- Added narrow `[HandlesCatalogExhaustively(typeof(ExpressionFormKind))]` coverage on `ParserState` plus member annotations on the expression handlers; removing one annotation reproduced `PRECEPT0019`, proving the guard is live.

- Validation closed green: `dotnet build src\Precept\Precept.csproj --no-restore --nologo` and `dotnet test test\Precept.Tests\Precept.Tests.csproj --no-restore --nologo` passed for 2810/2810.



### 2026-05-07T08:18:45Z — BackArrow (`<-`) computed-field delimiter implemented

- Executed Frank's approved `<-` plan in commit `266ee5a`: added `TokenKind.BackArrow`, switched computed-field parsing to `<-`, and propagated the new delimiter through docs, samples, and tooling.

- Lexer support stayed catalog-derived; no dedicated lexer code path was required beyond the token addition.

- Batch validation at handoff: 2799 passing tests.



### 2026-05-07T08:05:00Z — ParsedOutcome DU implemented

- Executed Frank's approved plan in commit `94dec3b`: `OutcomeSlot` now carries `ParsedOutcome`, and malformed rows stay explicit via `MalformedOutcome` rather than falling back into unrelated expression nodes.

- This closes the synthetic-binary-expression outcome encoding defect while keeping the parser/type-checker boundary honest.



### Historical summary through 2026-05-07

- Earlier active-history detail was compacted to keep George under the 15 KB gate.

- The durable parser baseline remains: catalog-driven construct parsing, sentinel slot invariants, `EventHandler` without an Outcome slot, and parser-resolved closed vocabularies feeding `ParsedExpression` / `ParsedOutcome` rather than span-only placeholders.

- Use `.squad/decisions.md` for the full per-batch provenance trail and branch-level decision chronology.



