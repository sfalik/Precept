APPROVED

Frank addressed the major architecture concerns from the prior pass. The `SyntaxTree` vs `TypedModel` boundary is now enforceable instead of rhetorical, the LS section now gives feature-to-artifact consumption rules precise enough for implementation, runtime lowering now names the executable metadata and operation-facing indexes the evaluator actually depends on, and the diagnostics/runtime-outcomes/faults split now coheres with the no-runtime-errors promise by reserving `Fault` for impossible-path invariant breaches only.

Follow-on notes that can wait for stage-level docs:
- `docs\runtime\fault-system.md` and the diagnostic-system wording still lag this newer non-symmetric faults/outcomes model and should be aligned later.
- Descriptor-backed public API shape under D8/R4 still needs its own runtime-level design pass, but the main architecture doc is now concrete enough to carry that work without reopening the top-level split.
