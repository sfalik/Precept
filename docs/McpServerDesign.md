# MCP Server Design

Current MCP surface for `tools/Precept.Mcp/`.

## Tool Surface

| Tool | Parameters | Returns |
|---|---|---|
| `precept_ping` | none | scalar text: `ok` |
| `precept_quickstart` | none | markdown quickstart |
| `precept_syntax` | none | markdown syntax reference |
| `precept_types` | `scope?` | markdown type-system reference |
| `precept_operations` | `category?` | markdown operations catalog |
| `precept_proofs` | none | markdown proofs and runtime faults |
| `precept_patterns` | none | markdown patterns and anti-patterns |
| `precept_diagnostic` | `code` | markdown diagnostic explanation |
| `precept_domains` | `scope?` | markdown domain catalog |
| `precept_compile` | `text` | compact JSON diagnostics + proof obligations + summary |

`precept_language` is removed. There is no compatibility shim.

## Markdown Tool Conventions

Catalog/reference tools return compact markdown for AI readers:

- `#` title
- `##` major sections
- tight bullets and short entry lines
- fenced `precept` blocks only when code examples matter

## precept_ping

Returns plain text:

```text
ok
```

## precept_quickstart

Returns markdown with this shape:

```text
# Precept Quickstart
## What Precept Is
## Core Guarantee
## Core Concepts
## Tool Guide
## Minimal Examples
```

## precept_syntax

Returns markdown with these sections:

```text
# Precept Syntax Reference
## Grammar Rules
## Operator Precedence
## Conventional Order
## Constructs
## Actions
## Outcomes
## Operators
```

## precept_types

Optional `scope` values:

- `types`
- `modifiers`
- `modifiers:value`
- `modifiers:state`
- `modifiers:event`
- `modifiers:access`
- `modifiers:anchor`
- `functions`
- omitted = full bundle (large; prefer a scope)

Returns markdown with only the requested sections. Full output shape:

```text
# Precept Type System
## Types
## Modifiers
### Value Modifiers
### State Modifiers
### Event Modifiers
### Access Modifiers
### Anchor Modifiers
## Built-in Functions
```

## precept_operations

Optional `category` filter is the intended path. Without it, the tool returns the full catalog.

```text
# Precept Operations
Filtered by: `Money`   # only when a category is supplied
## Available Categories
## Matching Operations
## Count
```

## precept_proofs

```text
# Precept Proofs and Runtime Faults
## Proof Requirements
## Runtime Faults
```

## precept_patterns

```text
# Precept Patterns
## Common Patterns
## Anti-Patterns
```

## precept_diagnostic

Accepted `code` formats:

- code name, for example `UndeclaredField`
- PRE number, for example `PRE0017`

Found result:

```text
# Diagnostic UndeclaredField (PRE0017)
## Summary
## Trigger
## Recovery Steps
## Fix Hint
## Related Codes
## Prevents Fault
## Examples
```

Missing result:

```text
# Diagnostic Lookup Failed
Requested: `PRE9999`
Use a diagnostic code name such as `UndeclaredField` or a PRE number such as `PRE0017`.
```

## precept_domains

Optional `scope` values:

- `currencies`
- `units`
- `prefixes`
- `dimensions`
- `temporal`
- omitted = full bundle (large; prefer a scope)

Full output shape:

```text
# Precept Domain Catalog
## Currencies
## UCUM Tier-1 Units
## UCUM Prefixes
## Dimensions
## Temporal Units
```

## precept_compile

Returns compact JSON with diagnostics and proof obligations:

```json
{
  "success": true,
  "diagnosticCount": 0,
  "diagnostics": [],
  "summary": "TrafficLight: 4 states, 3 events, 12 transitions, 2 rules, 0 ensures, 0 type errors.",
  "proofObligations": []
}
```

Diagnostic entry shape:

```json
{
  "line": 12,
  "column": 5,
  "severity": "error",
  "code": "PRE0101",
  "message": "Field 'Balance' is not writable in state 'Locked'."
}
```

`summary` is a compact prose description, not a projected definition graph.

Proof obligation entry shape:

```json
{
  "kind": "IntervalContainment",
  "disposition": "Unresolved",
  "strategy": "IntervalContainment",
  "emittedDiagnostic": "NumericOverflow",
  "description": "Expression assigned to 'Balance' must stay within declared bounds.",
  "computedInterval": "[0 .. 160]",
  "targetField": "Balance",
  "declaredMin": 0,
  "declaredMax": 100
}
```
