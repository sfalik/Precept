# George — E1 complete

- What changed: `ProofEngine` now resolves `QualifierCompatibilityProofRequirement` operands directly from the `TypedBinaryOp` site, and PRE0114 diagnostics now read operand names from that site instead of ambiguous `ParamSubject` resolution.
- New test count: 5 new regression tests added (`Cross_currency_fields_now_detected`, `Operand_names_in_diagnostics`, `Quantity_same_dimension_proved`, `Quantity_different_dimension_detected`, `Price_same_qualifiers_proved`).
- Validation: 7 targeted proof tests pass; `dotnet build src\\Precept\\Precept.csproj --nologo` passes; full `dotnet test test\\Precept.Tests\\Precept.Tests.csproj --nologo` still has the pre-existing inventory-item baseline failure.
- Implementation commit SHA: `d549b4a5dc478a571ba639ca67ae483ab0ff9fd3`.
- Cross-currency false-positive confirmation: `Cross_currency_fields_now_detected` failed before the fix because PRE0114 was missing, and now passes with the cross-currency operation correctly reported as unresolved.
