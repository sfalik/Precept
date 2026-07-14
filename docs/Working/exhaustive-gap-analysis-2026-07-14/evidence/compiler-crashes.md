# Compiler crashes found during exhaustive gap probing (2026-07-14)

Status: Draft evidence — candidate real compiler bugs, not yet triaged or filed.

All 6 cells below fired the same internal invariant violation during compilation:

> `D26 violated: TypedErrorExpression present but no Error diagnostic in SemanticIndex`

(the `assign-number-exponent-accept` cell fires a close variant: `D26 violated: TypedErrorExpression present in transition rows but no Error-severity diagnostic emitted.`)

In every case the shared trigger is a `choice of number(...)` (or `choice of decimal(...)`) domain whose literal values are written in scientific-notation form (e.g. `1.5e1`, `1e-1`). The compiler produces a `TypedErrorExpression` node internally but fails to emit a corresponding `Error`-severity diagnostic, tripping the D26 structural invariant check rather than surfacing a normal diagnostic. `allDiagnostics` is empty in every case — no diagnostic reached the caller at all.

Source: extracted from `all-results.json` (`crashMessage` field) and `recovered-corpus.json` (`recovered_text` field), both gid-matched by `id`, from the exhaustive full-corpus probe run archived in `../evidence/`.

---

## 1. `choice/decl/number-backed`

- gid: 3037

```precept
precept Test3037
field ConfidenceLevel as choice of number(1.5e1, 2.5e1, 3.5e1) ordered default 1.5e1
```

Crash message:
```
InvalidOperationException: D26 violated: TypedErrorExpression present but no Error diagnostic in SemanticIndex
```

## 2. `choice/lane/domain-number-exponent-accept`

- gid: 3084

```precept
precept Test3084

field ConfidenceLevel as choice of number(1.5e1, 2.5e1, 3.5e1) ordered default 1.5e1
```

Crash message:
```
InvalidOperationException: D26 violated: TypedErrorExpression present but no Error diagnostic in SemanticIndex
```

## 3. `choice/lane/default-decimal-exponent-reject`

- gid: 3088

```precept
precept T3088
field DiscountTier as choice of decimal(0.05, 0.10, 0.15) ordered default 1e-1
```

Crash message:
```
InvalidOperationException: D26 violated: TypedErrorExpression present but no Error diagnostic in SemanticIndex
```

## 4. `choice/lane/default-number-exponent-accept`

- gid: 3089

```precept
precept T3089
field ConfidenceLevel as choice of number(1.5e1, 2.5e1) ordered default 1.5e1
```

Crash message:
```
InvalidOperationException: D26 violated: TypedErrorExpression present but no Error diagnostic in SemanticIndex
```

## 5. `choice/lane/assign-number-exponent-accept`

- gid: 3093

```precept
precept T3093
field ConfidenceLevel as choice of number(1.5e1, 2.5e1) ordered default 1.5e1 editable
state Active initial terminal
event UpdateConfidence
from Active on UpdateConfidence
    -> set ConfidenceLevel = 2.5e1
    -> no transition
```

Crash message:
```
InvalidOperationException: D26 violated: TypedErrorExpression present in transition rows but no Error-severity diagnostic emitted.
```

## 6. `choice/lane/compare-number-exponent-accept`

- gid: 3097

```precept
precept T3097
field ConfidenceLevel as choice of number(1.5e1, 2.5e1) ordered default 1.5e1
rule ConfidenceLevel == 2.5e1 because "Confidence level must be 2.5e1"
```

Crash message:
```
InvalidOperationException: D26 violated: TypedErrorExpression present but no Error diagnostic in SemanticIndex
```

---

## Common thread

All 6 defs use `choice of number(...)` / `choice of decimal(...)` domains with scientific-notation literal values (`1.5e1`, `2.5e1`, `3.5e1`, `1e-1`). All 6 produced empty `allDiagnostics` and crashed on the D26 structural invariant instead of returning a diagnostic. This looks like a real gap in how the choice-domain lane handles scientific-notation numeric/decimal literals — worth a focused repro against `src/Precept/` before filing as a bug.
