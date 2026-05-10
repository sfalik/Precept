Added `IsOutlineNode` and `OutlineSymbolTag` to `ConstructMeta` with safe defaults (`false` / `null`).

Outline nodes now declared in the catalog:
- `PreceptHeader` -> `Module`
- `FieldDeclaration` -> `Property`
- `StateDeclaration` -> `Enum`
- `EventDeclaration` -> `Function`
- `RuleDeclaration` -> `Boolean`

All other construct entries continue to default to non-outline metadata.
