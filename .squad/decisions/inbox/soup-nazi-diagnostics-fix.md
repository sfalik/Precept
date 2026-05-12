# Soup Nazi — Diagnostics fixture fix

- The three `DiagnosticsTests` failures were not proof regressions.
- Root cause: the shared `Diagnostics.Create(...)` fixture in `test/Precept.Tests/DiagnosticsTests.cs` only supplied four placeholder format args, but `DiagnosticCode.UnprovedQualifierCompatibility` now uses placeholders through `{5}`.
- Action taken: expanded the test fixture arg list to six placeholders so the tests validate diagnostic metadata instead of crashing in `string.Format`.
