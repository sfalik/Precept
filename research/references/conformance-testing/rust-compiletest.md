# Mirror: Rust compiletest UI tests

Snapshotted 2026-06-01.

Source: Rust Compiler Development Guide, "UI tests" (https://rustc-dev-guide.rust-lang.org/tests/ui.html), accessed 2026-06-01. Grade: Primary (official rustc-dev-guide).

## Expected-output snapshots + blessing

> Tests store expected compiler output in `.stderr` and `.stdout` snapshot files located next to the test source. Filename format: `test-name.revision.compare_mode.extension`. Files are typically generated using the `--bless` CLI flag.

> The `--bless` option generates or updates expected output files. Developers run tests with this flag, then manually inspect the generated snapshots.

## `//~ ERROR` annotation syntax

> - `//~ ERROR` – Associates with the **current line**
> - `//~^ ERROR` – Associates with the **previous line** (each `^` adds one line up)
> - `//~| ERROR` – Associates with the **same line as the previous comment**
> - `//~v ERROR` – Associates with the **next line** (each `v` adds one line down)
> - `//~? ERROR` – Matches diagnostics **without line information**

Example:
```rust
(_a, _x @ ..) => {} //~ ERROR `_x @` is not allowed in a tuple
```
Multiple on same line:
```rust
//~^^^^ ERROR `_x @` is not allowed in a tuple struct
//~| ERROR this pattern has 1 field, but the struct has 3 [E0023]
```

> Diagnostic kinds: `ERROR`, `WARN`/`WARNING`, `NOTE`, `HELP`, `SUGGESTION`, `RAW`. "Error and warning kinds require exhaustive line annotation coverage by default."

> Error codes (like `E0425`, `E0023`) are included as part of the error message in brackets ... matched against the actual compiler output within the `.stderr` files.

> `error-pattern` directive provides a fallback for runtime messages or compile-time messages without specific spans ... "less preferred than line annotations."
