# Kramer decision — typed-constant completion replace range

## Context
Selecting a typed-constant slot completion inside existing partial text was inserting at the cursor because the returned LSP items only populated `InsertText`.

## Decision
Attach `TextEditOrInsertReplaceEdit` payloads to typed-constant slot vocabulary items. Use an insert range from the slot-fragment start to the cursor and a replace range from the same start to the fragment end so VS Code can replace the active fragment instead of concatenating onto it.

## Scope
Applied to typed-constant slot completions for:
- zoneddatetime bracket timezone IDs
- currency slots (`money`, `price`, `exchangerate`, direct currency literals)
- UCUM unit slots (`quantity`, `price`, direct unit literals)
- dimension slots (direct dimension literals and qualifier sites)
- direct timezone literals

Snippet/example completions outside slot-vocabulary lanes remain unchanged.

## Rationale
The bug was protocol-shape, not catalog selection. The completion labels were correct; the server simply failed to tell the client what range to replace.

## Validation
- `dotnet test .\test\Precept.LanguageServer.Tests\Precept.LanguageServer.Tests.csproj --nologo --verbosity minimal`
- `dotnet build --nologo --verbosity minimal`
- `dotnet test --nologo --verbosity minimal`
