# George — pipeline audit implementation notes

## Decision

Adapted Fix 7's event-modifier dispatch loop to the current semantic model instead of widening `TypedEvent` during the audit batch.

## Why

The fix plan assumed `TypedEvent.Modifiers`, but the runtime model currently carries only `TypedEvent.IsInitial` plus syntax back-pointers. Adding a new typed-event modifier collection would have expanded the semantic surface well beyond the audit's surgical scope.

## Implementation

`GraphAnalyzer.Analyze()` now derives the active event modifier set from `evt.IsInitial` (`ModifierKind.InitialEvent` when true, empty otherwise), then routes that set through `Modifiers.GetMeta(modifier)` and `EventModifierMeta.RequiredAnalysis`. The dispatch keeps the unconditional `throw` default the plan required, so future `GraphAnalysisKind` additions still fail loudly in every build.

## Secondary testability decision

`ResolveAction()` remains private; the new defensive tests invoke it via reflection instead of widening production visibility just for test access.
