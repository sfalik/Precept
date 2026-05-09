# Newman language tool done

- Catalogs enumerated: `Tokens.All`, `Types.All`, `Modifiers.All`, `Actions.All`, `Constructs.All`, `Constraints.All`, `Operators.All`, `Functions.All`, and `Diagnostics.All`.
- DTO shape summary: top-level `LanguageReferenceDto` with `tokens`, `types`, grouped `modifiers` (`field`, `state`, `event`, `access`, `anchor`), `actions`, `constructs`, `constraints`, `operators`, `functions`, `diagnostics`, and `firePipeline`.
- Test count: `Precept.Mcp.Tests` now has 12 passing tests total.
- Deviations from the older `docs/tooling/mcp.md` draft: the implemented tool exposes the language + diagnostic catalog surface above rather than the earlier 11-catalog draft (`Operations`, `ExpressionForms`, and `ProofRequirements` are not serialized here); modifier grouping is by modifier subtype and includes `event`.
- `docs/tooling/mcp.md` was updated: yes.
