# Decision: P1 presence-proof for typed-constant interpolation holes

## What was fixed

`ProofEngine.WalkExpression` now traverses `TypedInterpolatedTypedConstant` slot expressions instead of treating the node as opaque. That closes the presence-proof gap where `'{test2} [lb_av]'` could read an optional value in a typed-constant hole without producing PRE0116.

## Exact traversal case added

- Added a `TypedInterpolatedTypedConstant` case in `src/Precept/Pipeline/ProofEngine.cs`.
- That case iterates `typedConstant.Slots` and calls `WalkExpression(slot.Expression, ..., includeOptionalArgRefs: true)` for each slot.
- `WalkExpression` now carries an `includeOptionalArgRefs` flag so typed-constant hole subtrees can also emit presence obligations for optional event args without widening optional-arg proofing across unrelated expression surfaces.
- `TypedPostfixOp` remains a stop node, so `is set` / `is not set` checks inside hole expressions do not create spurious presence obligations.
- `ResolveSubject` and guard extraction were extended to understand `TypedArgRef` presence checks so guarded typed-constant hole reads can discharge correctly.

## Test coverage summary

Added four proof-engine presence tests in `test/Precept.Tests/ProofEnginePresenceTests.cs`:

1. Unguarded optional integer in `'{Source} kg'` -> PRE0116
2. Unguarded optional quantity in `'{SourceQty}'` -> PRE0116
3. Guarded optional typed-constant hole with `when Source is set` -> no PRE0116, obligation proved
4. Non-optional field in typed-constant hole -> no PRE0116

## Validation summary

- Focused `ProofEnginePresenceTests` run passed.
- `samples/Test.precept` now compiles with `UnprovedPresenceRequirement` on line 14.
- `dotnet build src/Precept/Precept.csproj` passed cleanly.
