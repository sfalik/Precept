# Diagnostic and Output Design Survey

Raw research collection — external systems survey. NO interpretation, NO conclusions for Precept, NO recommendations.

---

## Roslyn (C#)

### Diagnostic Type

The core type is the abstract class `Microsoft.CodeAnalysis.Diagnostic` in `src/Compilers/Core/Portable/Diagnostic/Diagnostic.cs` (dotnet/roslyn).

Key abstract/virtual members:

```csharp
public abstract class Diagnostic : IEquatable<Diagnostic?>, IFormattable
{
    public abstract DiagnosticDescriptor Descriptor { get; }
    public abstract string Id { get; }
    internal virtual string Category { get; }
    public abstract string GetMessage(IFormatProvider? formatProvider = null);
    public virtual DiagnosticSeverity DefaultSeverity { get; }
    public abstract DiagnosticSeverity Severity { get; }
    public abstract int WarningLevel { get; }
    public abstract bool IsSuppressed { get; }
    public abstract Location Location { get; }
    public abstract IReadOnlyList<Location> AdditionalLocations { get; }
    internal virtual ImmutableArray<string> CustomTags { get; }
    public virtual ImmutableDictionary<string, string?> Properties { get; }
    internal virtual int Code { get; }
    internal virtual IReadOnlyList<object?> Arguments { get; }
}
```

Source: `dotnet/roslyn`, `src/Compilers/Core/Portable/Diagnostic/Diagnostic.cs`

### DiagnosticDescriptor (Rule vs Instance)

Roslyn explicitly separates the *rule definition* (`DiagnosticDescriptor`) from the *rule instance* (`Diagnostic`). `DiagnosticDescriptor` describes the diagnostic *class*:

```csharp
public sealed class DiagnosticDescriptor : IEquatable<DiagnosticDescriptor?>
{
    public string Id { get; }                           // e.g. "CS1001", "CA1001"
    public LocalizableString Title { get; }             // Short title
    public LocalizableString Description { get; }       // Longer description
    public string HelpLinkUri { get; }                  // URL for more info
    public LocalizableString MessageFormat { get; }     // Format string with {0}, {1}...
    public string Category { get; }                     // e.g. "Compiler", "Microsoft.Design"
    public DiagnosticSeverity DefaultSeverity { get; }
    public bool IsEnabledByDefault { get; }
    public IEnumerable<string> CustomTags { get; }      // e.g. WellKnownDiagnosticTags
}
```

Source: `dotnet/roslyn`, `src/Compilers/Core/Portable/Diagnostic/DiagnosticDescriptor.cs`

A `Diagnostic` instance is created by combining a `DiagnosticDescriptor` with a location and message arguments:

```csharp
Diagnostic.Create(
    descriptor,
    location,
    additionalLocations,
    properties,
    messageArgs);
```

### Position/Location Representation

Roslyn uses `Microsoft.CodeAnalysis.Location` which has several subtypes:
- `SourceLocation` — location in source text, carries a `SyntaxTree` reference and a `TextSpan`
- `ExternalFileLocation` — location in a file not part of the compilation
- `MetadataLocation` — location in metadata (assemblies)
- `Location.None` — no location

`TextSpan` is:
```csharp
public struct TextSpan
{
    public int Start { get; }
    public int Length { get; }
    public int End { get; }
}
```

Line/column information is derived from the `SyntaxTree.GetLineSpan()` method, which returns:
```csharp
public struct FileLinePositionSpan
{
    public string Path { get; }
    public LinePosition StartLinePosition { get; }   // 0-based line, 0-based character
    public LinePosition EndLinePosition { get; }
}
```

### Secondary/Related Locations

`Diagnostic.AdditionalLocations` is `IReadOnlyList<Location>`. These are typically locations of other items referenced in the message. There is no structured label/message associated with additional locations at the Diagnostic API level — they are just locations. The IDE layer (and SARIF serialization) can annotate them.

### Severity Model

```csharp
public enum DiagnosticSeverity
{
    Hidden = 0,     // Not surfaced through normal means
    Info = 1,       // Information, not prescriptive
    Warning = 2,    // Suspicious but allowed
    Error = 3       // Not allowed
}
```

Internal extensions add `Unknown = -1` and `Void = -2` for deferred resolution.

Additionally, `ReportDiagnostic` controls what the compiler *does* with a severity:
```csharp
enum ReportDiagnostic { Default, Error, Warn, Info, Hidden, Suppress }
```

There is also a `WarningLevel` integer (0 for errors, 1+ for warnings) used with `/warn:N`.

### Diagnostic Codes

IDs are strings like `"CS1001"` (compiler) or `"CA1001"` (analyzer). Convention: prefix identifies the source tool, digits identify the specific rule. The ID lives on both the `DiagnosticDescriptor` (the rule) and the `Diagnostic` instance.

### Fix/Suggestion Model

Roslyn separates diagnostics from fixes. Code fixes are provided by `CodeFixProvider` subclasses that register for specific diagnostic IDs. The data model:

```csharp
public abstract class CodeFixProvider
{
    public abstract ImmutableArray<string> FixableDiagnosticIds { get; }
    public abstract Task RegisterCodeFixesAsync(CodeFixContext context);
}
```

A `CodeAction` represents a fix:
```csharp
public abstract class CodeAction
{
    public abstract string Title { get; }
    public virtual string? EquivalenceKey { get; }
}
```

The `Properties` bag on `Diagnostic` (`ImmutableDictionary<string, string?>`) is the primary mechanism for the diagnostic producer to pass structured data to a code fix provider. For example, an analyzer might set `Properties["FixType"] = "AddParentheses"` so the fixer knows what to do.

### Diagnostic Collection

Diagnostics are collected via `DiagnosticBag` (internal), which is a thread-safe mutable collection. Analyzers report diagnostics through `SymbolAnalysisContext.ReportDiagnostic()`, `SyntaxNodeAnalysisContext.ReportDiagnostic()`, etc. The compiler itself uses `BindingDiagnosticBag`.

The `Program` exposes diagnostics via:
```csharp
program.GetSyntacticDiagnostics(sourceFile);
program.GetSemanticDiagnostics(sourceFile);
program.GetDeclarationDiagnostics(sourceFile);
```

### Serialization

Roslyn supports SARIF output via `dotnet format` and MSBuild structured logging. The `/errorlog:file.sarif` flag on `csc` produces SARIF 2.1.0 output. The `SarifV2ErrorLogger` serializes `Diagnostic` → SARIF `result` objects.

Standard output format: `file(line,col): severity code: message`

### Metadata Beyond Message

- `CustomTags` — `ImmutableArray<string>` for categorization (e.g. `WellKnownDiagnosticTags.Unnecessary`, `WellKnownDiagnosticTags.NotConfigurable`, `WellKnownDiagnosticTags.Compiler`)
- `Properties` bag — freeform `ImmutableDictionary<string, string?>`
- `IsSuppressed` — whether suppressed by `#pragma warning disable` or `[SuppressMessage]`
- `WarningLevel` — integer for `/warn:N` filtering
- `DefaultSeverity` vs `Severity` — the descriptor's severity vs the effective severity after configuration

---

## TypeScript

### Diagnostic Interface

From `src/compiler/types.ts` (microsoft/TypeScript):

```typescript
export interface DiagnosticRelatedInformation {
    category: DiagnosticCategory;
    code: number;
    file: SourceFile | undefined;
    start: number | undefined;      // 0-based byte offset
    length: number | undefined;
    messageText: string | DiagnosticMessageChain;
}

export interface Diagnostic extends DiagnosticRelatedInformation {
    reportsUnnecessary?: {};
    reportsDeprecated?: {};
    source?: string;
    relatedInformation?: DiagnosticRelatedInformation[];
    /** @internal */ skippedOn?: keyof CompilerOptions;
    /** @internal */ canonicalHead?: CanonicalDiagnostic;
}

export interface DiagnosticWithLocation extends Diagnostic {
    file: SourceFile;
    start: number;
    length: number;
}
```

Source: `microsoft/TypeScript`, `src/compiler/types.ts`

### DiagnosticMessage (Rule Definition)

```typescript
export interface DiagnosticMessage {
    key: string;
    category: DiagnosticCategory;
    code: number;
    message: string;                // Format string with {0}, {1}...
    reportsUnnecessary?: {};
    reportsDeprecated?: {};
    /** @internal */ elidedInCompatabilityPyramid?: boolean;
}
```

This is the "descriptor" — the rule definition. Actual messages are in `src/compiler/diagnosticMessages.json` and code-generated into `diagnosticInformationMap.generated.ts`. Each diagnostic gets a stable numeric code (e.g. 2304 for "Cannot find name '{0}'").

### DiagnosticMessageChain

For nested/chained errors (e.g. type mismatch with elaboration):

```typescript
export interface DiagnosticMessageChain {
    messageText: string;
    category: DiagnosticCategory;
    code: number;
    next?: DiagnosticMessageChain[];
}
```

This creates a tree of messages — a primary message with sub-explanations. This is how TypeScript shows "Type 'A' is not assignable to type 'B'" with "Types of property 'x' are incompatible" underneath.

### Position/Location Representation

TypeScript uses **byte offsets** from the start of the file:
- `start: number` — 0-based byte offset
- `length: number` — length in bytes

`TextRange` (the base for all AST nodes) is:
```typescript
export interface TextRange {
    pos: number;
    end: number;
}
```

Line/character conversion is done via `getLineAndCharacterOfPosition()` using a `lineMap` (array of line-start offsets stored on `SourceFile`).

There is no multi-span concept at the diagnostic level — each diagnostic has exactly one primary span (`start` + `length`) or no span.

### Secondary/Related Locations

`Diagnostic.relatedInformation` is `DiagnosticRelatedInformation[]` — each entry has its own file, position, and message. This directly maps to LSP's `DiagnosticRelatedInformation`.

### Severity Model

```typescript
export enum DiagnosticCategory {
    Warning,
    Error,
    Suggestion,
    Message
}
```

Four levels. `Suggestion` is used for "did you mean" style hints. `Message` is used for informational output (not shown as problems).

### Diagnostic Codes

Numeric codes: TS codes range from 1000+ (syntax) through 2000+ (semantics) to 6000+ (project/config). Each code is globally unique. The code is a plain `number` field.

### Fix/Suggestion Model

TypeScript's language service provides `CodeFixProvider`s (internal). Quick fixes are separate from diagnostics. The `CodeAction` returned by `getCodeFixesAtPosition()` contains:
```typescript
interface CodeFixAction extends CodeAction {
    fixName: string;
    fixId?: {};
    fixAllDescription?: string;
}
interface CodeAction {
    description: string;
    changes: FileTextChanges[];
    commands?: CodeActionCommand[];
}
interface FileTextChanges {
    fileName: string;
    textChanges: TextChange[];
}
interface TextChange {
    span: TextSpan;         // { start: number; length: number }
    newText: string;
}
```

### Diagnostic Collection

`SourceFile` stores diagnostics in internal arrays:
```typescript
/** @internal */ parseDiagnostics: DiagnosticWithLocation[];
/** @internal */ bindDiagnostics: DiagnosticWithLocation[];
/** @internal */ bindSuggestionDiagnostics?: DiagnosticWithLocation[];
```

The `Program` gathers them:
```typescript
program.getSyntacticDiagnostics(sourceFile);
program.getSemanticDiagnostics(sourceFile);
program.getDeclarationDiagnostics(sourceFile);
program.getGlobalDiagnostics();
program.getConfigFileParsingDiagnostics();
```

Internally, `DiagnosticCollection` provides `add()`, `lookup()` (for deduplication), and `getDiagnostics()`.

### Serialization

`tsc --pretty false` produces: `file(line,col): error TScode: message`

No native SARIF output. The `--pretty` flag controls ANSI terminal formatting with caret-based source snippets.

### Metadata Beyond Message

- `source?: string` — identifies the source of a diagnostic (e.g. `"ts"` for compiler, plugin name for plugins)
- `reportsUnnecessary?: {}` — marker for "unnecessary" code (triggers fade in editor)
- `reportsDeprecated?: {}` — marker for deprecated usage (triggers strikethrough)
- `canonicalHead` (internal) — for deduplication when two diagnostics for the same problem have different messages

---

## Rust (rustc)

### Diagnostic Types

Rust's diagnostic infrastructure is in `compiler/rustc_errors/`. The core types:

**`DiagInner`** (the actual diagnostic data):
```rust
pub struct DiagInner {
    pub level: Level,
    pub lint_id: Option<LintExpectationId>,
    pub messages: Vec<(DiagMessage, Style)>,
    pub code: Option<ErrCode>,
    pub span: MultiSpan,
    pub children: Vec<Subdiag>,
    pub suggestions: Suggestions,
    pub args: DiagArgMap,
    pub sort_span: Span,
    pub is_lint: Option<IsLint>,
    pub long_ty_path: Option<PathBuf>,
    pub emitted_at: DiagLocation,
}
```

**`Diag<'a, G>`** — the builder wrapper with emission guarantee:
```rust
pub struct Diag<'a, G: EmissionGuarantee = ErrorGuaranteed> {
    pub dcx: DiagCtxtHandle<'a>,
    diag: Option<Box<DiagInner>>,
    _marker: PhantomData<G>,
}
```

Source: `rust-lang/rust`, `compiler/rustc_errors/src/diagnostic.rs`

### Subdiagnostics

```rust
pub struct Subdiag {
    pub level: Level,
    pub messages: Vec<(DiagMessage, Style)>,
    pub span: MultiSpan,
}
```

Children (notes, help messages) are stored as `Vec<Subdiag>` on the parent diagnostic.

### Position/Location Representation (MultiSpan)

Rust uses `MultiSpan` which can hold multiple spans:

```rust
pub struct MultiSpan {
    primary_spans: Vec<Span>,
    span_labels: Vec<SpanLabel>,
}

pub struct SpanLabel {
    pub span: Span,
    pub is_primary: bool,
    pub label: Option<DiagMessage>,
}
```

A `Span` in rustc encodes start byte offset, end byte offset, and `SyntaxContext` (for macro expansion tracking) in a compact 8-byte representation. The `SourceMap` converts spans to file/line/column information.

This is a **multi-span** system — a single diagnostic can highlight multiple regions of code with individual labels. Labels are messages attached directly to spans.

### Severity Model (Level)

```rust
pub enum Level {
    Bug,            // ICE (internal compiler error)
    Fatal,          // Immediate abort
    Error,          // Compilation error
    DelayedBug,     // Delayed ICE
    ForceWarning,   // Force-warn lint
    Warning,        // Warning
    Note,           // Additional context
    OnceNote,       // Note emitted only once
    Help,           // Help message
    OnceHelp,       // Help emitted only once
    FailureNote,    // Context on failure
    Allow,          // Allowed lint (suppressed)
    Expect,         // Expected lint (#[expect])
}
```

13 levels — far more granular than most systems. The `Allow`/`Expect`/`ForceWarning` levels are specific to the lint system.

### Diagnostic Codes

`ErrCode` is a typed wrapper around an error code number. Codes are defined via the `E!` macro in `compiler/rustc_error_codes/`. Format: `E0001` through `E0XXX`. Each code has an optional long-form explanation accessible via `rustc --explain E0308`.

### Suggestions and Applicability

Rust has the most sophisticated suggestion model of any compiler:

```rust
pub struct CodeSuggestion {
    pub substitutions: Vec<Substitution>,
    pub msg: DiagMessage,
    pub style: SuggestionStyle,
    pub applicability: Applicability,
}

pub struct Substitution {
    pub parts: Vec<SubstitutionPart>,
}

pub struct SubstitutionPart {
    pub span: Span,
    pub snippet: String,
}
```

**Applicability** is critical — it tells tools whether the fix can be auto-applied:
```rust
pub enum Applicability {
    MachineApplicable,      // Can be auto-applied safely
    MaybeIncorrect,         // Might not be correct
    HasPlaceholders,        // Contains `_` or `/* ... */` placeholders
    Unspecified,            // Unknown applicability
}
```

**SuggestionStyle** controls display:
```rust
pub enum SuggestionStyle {
    HideCodeAlways,         // Don't show code
    HideCodeInline,         // Hide only when inline
    CompletelyHidden,       // Hidden (for tool-only fixes)
    ShowCode,               // Show suggested code
    ShowAlways,             // Always show code
}
```

A single suggestion can have **multiple substitution alternatives** (e.g. "try one of these") — the `substitutions` Vec. Each substitution can have **multiple parts** (multipart edits).

### Diagnostic Collection (DiagCtxt)

`DiagCtxt` is the central diagnostic context, protected by a `Lock` (similar to `Mutex`):

```rust
pub struct DiagCtxt {
    inner: Lock<DiagCtxtInner>,
}

struct DiagCtxtInner {
    err_guars: Vec<ErrorGuaranteed>,
    lint_err_guars: Vec<ErrorGuaranteed>,
    delayed_bugs: Vec<(DelayedDiagInner, ErrorGuaranteed)>,
    deduplicated_err_count: usize,
    deduplicated_warn_count: usize,
    emitter: Box<DynEmitter>,
    taught_diagnostics: FxHashSet<ErrCode>,
    emitted_diagnostic_codes: FxIndexSet<ErrCode>,
    emitted_diagnostics: FxHashSet<Hash128>,           // For deduplication
    stashed_diagnostics: FxIndexMap<StashKey, ...>,    // Stash for improvement
    future_breakage_diagnostics: Vec<DiagInner>,
    fulfilled_expectations: FxIndexSet<LintExpectationId>,
    // ...
}
```

Key design: `ErrorGuaranteed` is a zero-sized type that proves an error was emitted. It's used as a return type to make it impossible to construct certain data structures without first emitting an error — this is the "tainted" or "poison" pattern.

Diagnostics can be **stashed** (set aside for later improvement by downstream compilation phases) and **stolen** (retrieved and modified). `StashKey` identifies what kind of diagnostic was stashed.

### Serialization (JSON Error Format)

`rustc --error-format=json` produces newline-delimited JSON:

```json
{
    "type": "diagnostic",
    "message": "cannot find value `foo` in this scope",
    "code": { "code": "E0425", "explanation": "..." },
    "level": "error",
    "spans": [
        {
            "file_name": "src/main.rs",
            "byte_start": 100,
            "byte_end": 103,
            "line_start": 5,
            "line_end": 5,
            "column_start": 10,
            "column_end": 13,
            "is_primary": true,
            "text": [{ "text": "    let x = foo;", "highlight_start": 13, "highlight_end": 16 }],
            "label": "not found in this scope",
            "suggested_replacement": null,
            "suggestion_applicability": null,
            "expansion": null
        }
    ],
    "children": [
        {
            "message": "consider importing this function",
            "code": null,
            "level": "help",
            "spans": [{ ... "suggested_replacement": "use crate::foo;\n", "suggestion_applicability": "MaybeIncorrect" }],
            "children": [],
            "rendered": null
        }
    ],
    "rendered": "error[E0425]: cannot find value `foo` in this scope\n ..."
}
```

The `rendered` field contains the fully-formatted human-readable output, so tools can show either structured or pre-rendered output.

### Metadata Beyond Message

- `is_lint: Option<IsLint>` — lint name and future-breakage flag
- `args: DiagArgMap` — typed arguments for Fluent message formatting
- `emitted_at: DiagLocation` — where in the compiler source the diagnostic was created (for debugging with `-Ztrack-diagnostics`)
- `sort_span` — used for ordering diagnostics in output
- `long_ty_path` — file path where a very long type was written (with `--verbose`)
- `lint_id: Option<LintExpectationId>` — links to `#[expect]` attributes

---

## Go

### go/types Error

The basic error type in `go/types`:

```go
type Error struct {
    Fset *token.FileSet
    Pos  token.Pos       // error position
    Msg  string          // error message
    Soft bool            // if set, error is "soft" (e.g. unused variable)
}
```

Source: `golang/go`, `src/go/types/errors.go`

`token.Pos` is an integer offset into a global file set. The `FileSet` maps positions to file/line/column.

### go/analysis Diagnostic

The `go/analysis` framework (used by `go vet`, `staticcheck`, `gopls`) has:

```go
type Diagnostic struct {
    Pos      token.Pos
    End      token.Pos       // optional end position
    Category string          // optional category (e.g. "buildtag")
    Message  string
    URL      string          // optional URL for more info

    SuggestedFixes []SuggestedFix
    Related        []RelatedInformation
}

type SuggestedFix struct {
    Message   string
    TextEdits []TextEdit
}

type TextEdit struct {
    Pos     token.Pos
    End     token.Pos
    NewText []byte
}

type RelatedInformation struct {
    Pos     token.Pos
    End     token.Pos
    Message string
}
```

Source: `golang/tools`, `go/analysis/diagnostic.go`

### Position Representation

`token.Pos` is an opaque integer. `token.FileSet.Position(pos)` returns:
```go
type Position struct {
    Filename string
    Offset   int    // byte offset, starting at 0
    Line     int    // line number, starting at 1
    Column   int    // column number, starting at 1 (byte count)
}
```

### Severity Model

Go has no severity enum in `go/types`. All type errors are errors. The `Soft` flag on `types.Error` indicates "soft" errors (e.g. unused variables) that don't prevent further type-checking but still fail compilation.

In `go/analysis`, there's no severity on `Diagnostic` — all diagnostics from analyzers are presented at the same level. The reporting tool (`gopls`, `go vet`) determines presentation.

### Fix Model

`SuggestedFix` contains a message and a list of `TextEdit`s, each with start pos, end pos, and new text. Multiple fixes can be offered. Each fix can involve multiple edits (multipart). There is no applicability/confidence annotation.

### Diagnostic Collection

The `go/types` `Config.Error` callback is called for each error:
```go
type Config struct {
    Error func(err error)  // called for each error; nil means panic
    // ...
}
```

The `go/analysis` framework collects diagnostics via:
```go
pass.Reportf(pos, format, args...)       // simple
pass.Report(analysis.Diagnostic{...})    // structured
```

### Serialization

`go vet -json` produces JSON output. `gopls` communicates via LSP. There is no SARIF output from standard Go tools.

---

## Kotlin

### K1 Frontend (Old)

The old frontend used `DiagnosticFactory` pattern:

```kotlin
// Simplified representation
interface Diagnostic {
    val factory: DiagnosticFactory<*>
    val severity: Severity
    val psiElement: PsiElement       // IntelliJ PSI node
    val textRanges: List<TextRange>
}

enum class Severity {
    ERROR, WARNING, INFO
}
```

Diagnostics are defined as `DiagnosticFactory0`, `DiagnosticFactory1<A>`, `DiagnosticFactory2<A, B>` etc., where type parameters are the diagnostic arguments. Each factory has an ID-like `Name` string.

### K2 Frontend (FIR)

K2 uses `KtDiagnostic`:

```kotlin
sealed class KtDiagnostic {
    abstract val element: KtSourceElement
    abstract val severity: Severity
    abstract val factory: KtDiagnosticFactory<*>

    // Concrete subclasses: KtSimpleDiagnostic, KtDiagnosticWithParameters1<A>, etc.
}
```

K2 diagnostics are rendered by `KtDiagnosticRenderer` which maps each factory to a message format string. The diagnostic carries typed parameters rather than pre-formatted message text.

### Position

In K1: `PsiElement` reference + `TextRange` (IntelliJ platform's start/end offset pair).
In K2: `KtSourceElement` wraps a `KtLighterASTNode` or `KtPsiSourceElement` with source file and text range.

### Severity

Three levels: `ERROR`, `WARNING`, `INFO`. Same in K1 and K2.

### Fix Model

Quick fixes are provided by `KotlinQuickFixFactory` (K1) or `KotlinQuickFixRegistrar` (K2). Fixes register against specific diagnostic factories. The fix model is IntelliJ `IntentionAction`-based — separate from the diagnostic itself.

---

## Swift

### DiagnosticEngine and Diagnostic

Swift's diagnostic system (in `swift/lib/AST/DiagnosticEngine.cpp` and `include/swift/AST/`):

```cpp
// Simplified from Swift source
struct Diagnostic {
    DiagID ID;
    SourceLoc Loc;
    SmallVector<DiagnosticArgument, 3> Args;
    SmallVector<FixIt, 2> FixIts;
    SmallVector<SourceRange, 2> Ranges;       // Highlighted ranges
    SmallVector<Diagnostic, 1> ChildNotes;    // Child note diagnostics
};

enum class DiagnosticKind : uint8_t {
    Error, Warning, Remark, Note
};
```

### Fix-It Model

```cpp
struct FixIt {
    CharSourceRange Range;
    std::string Text;
};
```

Fix-Its are attached directly to diagnostics (not in a separate system). They contain a source range and replacement text.

### Grouped Diagnostics

Swift supports "diagnostic groups" — a primary diagnostic (error/warning) with attached note diagnostics as children. The `ChildNotes` vector creates a tree structure.

### Position

`SourceLoc` is an opaque pointer into a `SourceManager` buffer. `SourceRange` is a start `SourceLoc` + end `SourceLoc`. `CharSourceRange` adds byte-length semantics.

### Diagnostic Definition

Diagnostics are defined in `.def` files using macros:
```cpp
ERROR(cannot_convert_argument, none,
      "cannot convert value of type %0 to expected argument type %1", (Type, Type))
WARNING(unused_result, none,
        "result of call to %0 is unused", (DeclName))
```

This generates the enum `DiagID`, message strings, and severity simultaneously. The `.def` file approach is the "rule definition" — instances are created at emission sites.

### Serialization

Swift produces diagnostics in its text format by default. The `-serialize-diagnostics` flag produces a binary serialized format. There is also `-diagnostic-style=swift` (default) vs `-diagnostic-style=llvm`.

---

## CEL (Common Expression Language)

### cel.Issues

CEL (github.com/google/cel-go) reports errors through the `cel.Issues` type:

```go
// cel-go/cel/program.go and cel/env.go
type Issues struct {
    errs *cel.Errors
}

// Errors wraps a list of errors
type Errors struct {
    errors []common.Error
}

// common.Error is the base error
type Error struct {
    Location common.Location
    Message  string
}

type Location interface {
    Line() int      // 1-based
    Column() int    // 0-based
}
```

Source: `google/cel-go`, `common/error.go`

### Check vs Eval Errors

CEL has two phases:
1. **Check phase** (`env.Check(ast)`) — returns `*cel.Issues` containing type-check errors. These have location information.
2. **Eval phase** (`program.Eval(activation)`) — returns `ref.Val` and may return a `types.Err`. Eval errors do NOT have source position information by default — they arise at runtime.

### Severity

No severity model. All CEL issues are errors — either the expression is valid or it isn't. There is no warning concept.

### Fix Model

No fix/suggestion model. CEL is an expression language with no IDE integration of its own.

### Diagnostic Codes

No diagnostic codes. Errors are identified purely by message text.

### Serialization

CEL errors are Go `error` values. The `Issues.String()` method produces human-readable format: `ERROR: <source>:line:col: message`. No structured output format.

---

## Rego (OPA)

### ast.Errors

OPA's Rego parser and compiler use `ast.Errors`:

```go
// open-policy-agent/opa, ast/errors.go
type Errors []*Error

type Error struct {
    Code     string        // error code, e.g. "rego_parse_error"
    Message  string
    Location *Location
    Details  interface{}   // optional structured details
}

type Location struct {
    File   string   // filename
    Row    int      // 1-based line
    Col    int      // 1-based column
    Offset int      // byte offset
    Text   []byte   // source text of the token/node
}
```

Source: `open-policy-agent/opa`, `ast/errors.go`

### Error Codes

OPA uses string error codes:
- `"rego_parse_error"` — syntax errors
- `"rego_compile_error"` — semantic errors
- `"rego_type_error"` — type errors
- `"rego_recursion_error"` — recursive definitions
- `"rego_unsafe_var_error"` — unsafe variable references

### Severity

No severity model. All Rego errors are errors. No warning concept.

### Fix Model

No fix/suggestion model.

### Serialization

OPA JSON output (`opa check --format json`) produces:

```json
{
    "errors": [
        {
            "code": "rego_parse_error",
            "message": "unexpected token",
            "location": {
                "file": "policy.rego",
                "row": 5,
                "col": 10
            }
        }
    ]
}
```

The `Details` field can carry structured data (e.g. for type errors, the expected vs actual types).

---

## CUE

### errors.Error Interface

CUE (cuelang.org/go) uses an `errors.Error` interface:

```go
// cue/errors/errors.go
type Error interface {
    error
    Position() token.Pos
    InputPositions() []token.Pos
    Path() []string            // CUE value path (e.g. ["foo", "bar", "0"])
    Msg() (format string, args []interface{})
    Sanitize() Error
}
```

Source: `cue-lang/cue`, `cue/errors/errors.go`

### Constraint Conflict Reporting

CUE's unique challenge: constraint conflicts can arise from multiple paths. A field `foo` might be constrained by both `#Schema.foo` and an inline override. CUE reports these with `InputPositions()` — a list of all source positions that contributed to the conflict.

```
foo: conflicting values 1 and 2:
    ./schema.cue:3:5
    ./data.cue:10:8
```

Each position is a source location that imposed a constraint.

### Path

`Path() []string` gives the CUE value path to the error — e.g. `["servers", "web", "port"]`. This is unique to CUE's structural/path-oriented data model.

### Severity

No severity model. CUE is a constraint language — either constraints are satisfied or they're not.

### Serialization (`cue vet`)

`cue vet` output format:
```
field not allowed: extra
    ./data.cue:5:3
    ./schema.cue:2:1
```

`cue vet --format json` (or via the Go API) produces structured error lists.

---

## Jsonnet

### Error Reporting Model

Jsonnet (google/jsonnet, or go-jsonnet) has two error types:

```go
// google/go-jsonnet
type RuntimeError struct {
    Msg        string
    StackTrace []TraceFrame
}

type StaticError struct {
    Loc LocationRange
    Msg string
}

type LocationRange struct {
    FileName string
    Begin    Location
    End      Location
}

type Location struct {
    Line   int    // 1-based
    Column int    // 1-based
}
```

### Static vs Runtime

**StaticError** — parse/static analysis errors. Carry a `LocationRange` (file + begin/end positions).

**RuntimeError** — evaluation errors. Include a stack trace (`[]TraceFrame`) because Jsonnet is lazy — the error might manifest far from where the problematic value was defined. Each `TraceFrame` has a location and a description.

### Lazy Language Implications

Because Jsonnet is lazy, many errors that would be static in eager languages are runtime errors. A field reference `obj.foo` doesn't fail until `obj.foo` is actually *used*. This means runtime errors carry stack traces rather than single positions.

### Severity

No severity model. Errors are either static (parse/check) or runtime.

### Fix Model

No fix/suggestion model.

### Serialization

Jsonnet CLI output is plain text:
```
STATIC ERROR: filename:line:col-col: message
RUNTIME ERROR: message
    filename:line:col    thunk <name>
    filename:line:col    object <name>
```

---

## Pkl

### PklError

Pkl (apple/pkl) reports errors through structured exceptions:

```kotlin
// Simplified from Pkl source
sealed class PklError {
    abstract val message: String
    abstract val hint: String?
}

class PklBugError : PklError { ... }

class EvalError : PklError {
    val sourceLocation: SourceLocation?
    // ...
}
```

### Constraint Violations vs Type Errors

Pkl distinguishes:
1. **Type errors** — when a value doesn't match its declared type (e.g. `name: String = 42`)
2. **Constraint violations** — when a value matches the type but violates a constraint (e.g. `port: UInt16(this > 0) = 0`)

Constraint violations are reported with the path to the violating value:
```
– Expected value to be a "UInt16(this > 0)", but got 0
  at package://example.com/Config.pkl#/port
```

### Path-Based Reporting

Pkl errors include *value paths* (similar to CUE), like `/servers/web/port`. This is because Pkl is a configuration language where hierarchical paths are natural identifiers.

### Severity

No severity model — errors are fatal.

---

## Dhall

### TypeError Structure

Dhall (dhall-lang/dhall-haskell) has detailed, structured type errors:

```haskell
data TypeError s a = TypeError
    { context     :: Context (Expr s a)    -- typing context at error point
    , current     :: Expr s a              -- expression that failed
    , typeMessage :: TypeMessage s a        -- the specific error
    }

data TypeMessage s a
    = UnboundVariable Text
    | InvalidInputType (Expr s a)
    | InvalidOutputType (Expr s a)
    | NotAFunction (Expr s a) (Expr s a)
    | TypeMismatch (Expr s a) (Expr s a) (Expr s a) (Expr s a)
    | AnnotMismatch (Expr s a) (Expr s a) (Expr s a)
    -- ... ~40 constructors
    | CantAccess Text (Expr s a) (Expr s a)
    | CombineTypesRequiresRecordType (Expr s a) (Expr s a)
    -- etc.
```

Source: `dhall-lang/dhall-haskell`, `dhall/src/Dhall/TypeCheck.hs`

### Error Message Detail

Dhall produces some of the most detailed error messages of any language. Each `TypeMessage` constructor generates a multi-section explanation:

```
Error: Wrong type of function argument

↳ ./config.dhall

Expected type: Natural
Actual type:   Text

1│ let f = λ(x : Natural) → x + 1
2│ in f "hello"
```

The error includes:
- The type of error
- The source location with source snippet
- Expected vs actual types
- The full typing context (available via `--explain`)

### Import Error Reporting

Dhall has first-class imports (`./other.dhall`, `https://example.com/package.dhall`). Import errors are reported with the full import chain:

```
Error: Missing file
↳ ./config.dhall
  ↳ ./database.dhall
    ↳ ./credentials.dhall (file not found)
```

### Severity

No severity model. Dhall type errors are always fatal.

---

## Starlark

### Error Types

Starlark (google/starlark-go) has:

```go
// starlark-go/syntax/syntax.go
type Error struct {
    Pos Position
    Msg string
}

type Position struct {
    file *string    // filename (shared for efficiency)
    Line int32      // 1-based
    Col  int32      // 1-based, in bytes
}

// starlark-go/starlark/eval.go
type EvalError struct {
    Msg       string
    CallStack CallStack    // call stack at time of error
    cause     error        // underlying error if any
}

type CallStack []CallFrame

type CallFrame struct {
    Name string      // function name
    Pos  syntax.Position
}
```

Source: `google/starlark-go`, `syntax/syntax.go` and `starlark/eval.go`

### Static vs Eval Errors

- `syntax.Error` — scanner/parser errors with single position
- `resolve.Error` — name resolution errors (list of `syntax.Error`)
- `starlark.EvalError` — runtime errors with call stack

### Severity

No severity model. All errors are fatal to the evaluation.

---

## Dafny

### Verification Error Reporting

Dafny (dafny-lang/dafny) reports verification failures from its backing prover (Boogie → Z3):

```csharp
// Simplified from Dafny source
class DafnyDiagnostic {
    public ErrorLevel Level { get; }
    public IToken Token { get; }
    public MessageSource Source { get; }
    public string ErrorId { get; }       // e.g. "Error" or verification error code
    public string Message { get; }
    public IToken[] RelatedInformation { get; }
}

enum ErrorLevel {
    Error, Warning, Info
}

enum MessageSource {
    Parser, Resolver, Translator, Rewriter, Verifier, Compiler, Clone, RefinementTransformer, Other
}
```

### Assertion Failure Messages

When a postcondition, invariant, or assert fails verification:
```
file.dfy(15,4): Error: a]postcondition might not hold on this return path
file.dfy(10,20): Related location: this is the postcondition that might not hold
```

Verification errors include:
- Primary location (the statement that can't be verified)
- Related locations (the specification that was violated)
- **Counterexample display** — when Z3 produces a model, Dafny can show concrete values that violate the specification

### Resource Counting

Dafny reports resource usage from Z3:
```
Dafny program verifier finished with 5 verified, 0 errors
  Resource count: 123456
```

The `--resource-limit` flag caps verification resources. Resource exhaustion is reported as a specific error.

### Serialization

`dafny verify --format json` or `--isolate-assertions` for per-assertion results.

---

## Boogie

### Verification Condition Failure

Boogie (boogie-org/boogie) is the intermediate verification language used by Dafny:

```csharp
// Boogie error model (simplified)
class VerificationResult {
    public VCOutcome Outcome { get; }     // Correct, Errors, Inconclusive, TimedOut, OutOfMemory, OutOfResource
    public List<Counterexample> Errors { get; }
}

class Counterexample {
    public BlockSeq Trace { get; }         // Execution trace to the error
    public Model Model { get; }            // Z3 model (counterexample values)
    public List<string> RequestedVariables { get; }
}

enum VCOutcome {
    Correct, Errors, Inconclusive, TimedOut, OutOfMemory, OutOfResource, SolverException
}
```

### Counterexample Models

Boogie's Z3 integration produces `Model` objects — mappings from variable names to concrete values that demonstrate the verification failure. This lets tools show "here's an example where your assertion doesn't hold: x=5, y=-1".

---

## Why3

### Proof Obligation Display

Why3 (why3.lri.fr) presents unproven goals as tree-structured proof obligations:

```
Theory T / Goal G
  H1: x > 0
  H2: y > x
  ────────────
  Goal: x + y > 0
```

Each proof obligation has:
- A set of hypotheses (the context)
- A goal (what needs to be proved)
- A status: Valid, Invalid, Unknown, Timeout, OutOfMemory, Failure
- The prover that was used and its answer

### Multi-Prover Results

Why3 can dispatch the same goal to multiple provers (Z3, CVC5, Alt-Ergo). Results are reported per-prover:
```
Goal G: Valid (Alt-Ergo 2.4.2, 0.05s)
Goal G: Valid (Z3 4.12.2, 0.12s)
Goal G: Unknown (CVC5 1.0.5, timeout 5s)
```

---

## CBMC

### Bounded Model Checking Results

CBMC (diffblue/cbmc) reports:

```xml
<result property="main.assertion.1" status="FAILURE">
  <trace>
    <assignment ... />
    <function_call ... />
    <location file="main.c" line="10" />
    <failure property="main.assertion.1" reason="assertion a > 0" />
  </trace>
</result>
```

### Trace Generation

CBMC produces **execution traces** — step-by-step paths through the program that lead to the property violation. Each step includes:
- Variable assignments
- Function calls/returns
- Branch decisions
- The final failure point

Output formats: text, XML, JSON.

---

## SARIF 2.1.0

### Overview

Static Analysis Results Interchange Format (SARIF) is an OASIS standard (TC SARIF). Version 2.1.0 is the current stable release. Specification: https://docs.oasis-open.org/sarif/sarif/v2.1.0/sarif-v2.1.0.html

### Top-Level Structure

```json
{
    "$schema": "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json",
    "version": "2.1.0",
    "runs": [
        {
            "tool": { ... },
            "results": [ ... ],
            "artifacts": [ ... ],
            "invocations": [ ... ]
        }
    ]
}
```

### tool and toolComponent

```json
{
    "tool": {
        "driver": {
            "name": "ESLint",
            "version": "8.0.0",
            "informationUri": "https://eslint.org",
            "rules": [
                {
                    "id": "no-unused-vars",
                    "name": "NoUnusedVars",
                    "shortDescription": { "text": "Disallow unused variables" },
                    "fullDescription": { "text": "..." },
                    "helpUri": "https://eslint.org/docs/rules/no-unused-vars",
                    "defaultConfiguration": {
                        "level": "warning"
                    },
                    "properties": { ... }
                }
            ]
        },
        "extensions": [ ... ]    // plugin toolComponents
    }
}
```

`reportingDescriptor` (the rule/descriptor):
- `id` — stable identifier
- `name` — human-readable name
- `shortDescription`, `fullDescription` — `multiformatMessageString` (text + optional markdown)
- `helpUri` — URL for documentation
- `help` — `multiformatMessageString` for inline help text
- `defaultConfiguration` — default level, rank, parameters
- `relationships` — links to other descriptors (e.g. "this rule implements CWE-79")
- `properties` — property bag

### result (Diagnostic Instance)

```json
{
    "ruleId": "no-unused-vars",
    "ruleIndex": 0,
    "level": "warning",
    "message": {
        "text": "'x' is defined but never used.",
        "id": "default",
        "arguments": ["x"]
    },
    "locations": [
        {
            "physicalLocation": {
                "artifactLocation": {
                    "uri": "src/main.js",
                    "uriBaseId": "%SRCROOT%"
                },
                "region": {
                    "startLine": 5,
                    "startColumn": 7,
                    "endLine": 5,
                    "endColumn": 8,
                    "charOffset": 100,
                    "charLength": 1,
                    "snippet": { "text": "let x = 1;" },
                    "sourceLanguage": "javascript"
                },
                "contextRegion": {
                    "startLine": 3,
                    "endLine": 7,
                    "snippet": { "text": "function foo() {\n  let x = 1;\n  return 42;\n}" }
                }
            },
            "logicalLocations": [
                {
                    "name": "foo",
                    "kind": "function",
                    "fullyQualifiedName": "module::foo"
                }
            ]
        }
    ],
    "relatedLocations": [
        {
            "id": 1,
            "message": { "text": "Variable 'x' was declared here" },
            "physicalLocation": { ... }
        }
    ],
    "codeFlows": [ ... ],
    "fixes": [ ... ],
    "fingerprints": { ... },
    "partialFingerprints": { ... },
    "properties": { ... }
}
```

### location, physicalLocation, logicalLocation

**physicalLocation**:
- `artifactLocation` — file URI with optional base
- `region` — the specific text region:
  - `startLine`, `startColumn` (1-based)
  - `endLine`, `endColumn` (1-based, exclusive)
  - `charOffset`, `charLength` (0-based byte offsets)
  - `snippet` — source text
  - `sourceLanguage` — language of the source
- `contextRegion` — surrounding context (for display)

**logicalLocation**:
- `name` — short name (e.g. function name)
- `fullyQualifiedName` — e.g. `"MyNamespace.MyClass.MyMethod"`
- `kind` — `"function"`, `"member"`, `"module"`, `"namespace"`, `"type"`, `"returnType"`, `"parameter"`, `"variable"`, etc.
- `parentIndex` — index of parent logical location
- `decoratedName` — mangled/decorated name

### relatedLocations

Array of `location` objects, each with an `id` (integer for cross-referencing within the result) and a `message`. Used for "declared here", "also used here" style annotations.

### codeFlows and threadFlows

For path-sensitive results (data flow, taint analysis):

```json
{
    "codeFlows": [
        {
            "message": { "text": "Tainted data flows from source to sink" },
            "threadFlows": [
                {
                    "locations": [
                        {
                            "location": { ... },
                            "kinds": ["source"],
                            "nestingLevel": 0,
                            "state": { "taintState": "tainted" },
                            "importance": "essential"
                        },
                        {
                            "location": { ... },
                            "kinds": ["sink"],
                            "nestingLevel": 0,
                            "importance": "essential"
                        }
                    ]
                }
            ]
        }
    ]
}
```

`threadFlowLocation`:
- `location` — the physical/logical location
- `kinds` — `["source"]`, `["sink"]`, `["sanitizer"]`, `["pass"]`, etc.
- `nestingLevel` — call depth
- `executionOrder` — ordering hint
- `importance` — `"important"`, `"essential"`, `"unimportant"`
- `state` — key-value pairs describing state at this point

### fixes

```json
{
    "fixes": [
        {
            "description": { "text": "Remove unused variable 'x'" },
            "artifactChanges": [
                {
                    "artifactLocation": { "uri": "src/main.js" },
                    "replacements": [
                        {
                            "deletedRegion": {
                                "startLine": 5,
                                "startColumn": 3,
                                "endLine": 5,
                                "endColumn": 15
                            },
                            "insertedContent": { "text": "" }
                        }
                    ]
                }
            ]
        }
    ]
}
```

### Level (Severity)

```
"error"   — serious problem
"warning" — potential problem
"note"    — informational
"none"    — used for non-failure results (e.g. code metrics)
```

### Fingerprints

```json
{
    "fingerprints": {
        "0": "hash-of-normalized-result"
    },
    "partialFingerprints": {
        "primaryLocationLineHash": "abc123",
        "contextRegionHash": "def456"
    }
}
```

Fingerprints enable result matching across runs (for baseline comparison and new-issue detection). `fingerprints` are opaque stable identifiers. `partialFingerprints` are building blocks for matching heuristics.

### Rule-Instance Separation

SARIF explicitly separates rules (in `tool.driver.rules[]` as `reportingDescriptor` objects) from results (instances). Results reference rules via `ruleId` and `ruleIndex`. This is the same pattern as Roslyn's `DiagnosticDescriptor` / `Diagnostic`.

---

## LSP 3.17 Diagnostics

### Diagnostic Interface

From the Language Server Protocol specification (3.17):

```typescript
interface Diagnostic {
    range: Range;
    severity?: DiagnosticSeverity;
    code?: integer | string;
    codeDescription?: CodeDescription;
    source?: string;
    message: string;
    tags?: DiagnosticTag[];
    relatedInformation?: DiagnosticRelatedInformation[];
    data?: LSPAny;
}

interface Range {
    start: Position;
    end: Position;
}

interface Position {
    line: uinteger;        // 0-based
    character: uinteger;   // 0-based, in UTF-16 code units (or negotiated encoding)
}
```

Source: LSP Specification 3.17, https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/

### DiagnosticSeverity

```typescript
enum DiagnosticSeverity {
    Error = 1,
    Warning = 2,
    Information = 3,
    Hint = 4
}
```

### DiagnosticTag

```typescript
enum DiagnosticTag {
    Unnecessary = 1,    // Unused or unnecessary code (faded in editor)
    Deprecated = 2      // Deprecated code (strikethrough in editor)
}
```

### DiagnosticRelatedInformation

```typescript
interface DiagnosticRelatedInformation {
    location: Location;
    message: string;
}

interface Location {
    uri: DocumentUri;
    range: Range;
}
```

### CodeDescription

```typescript
interface CodeDescription {
    href: URI;    // URL to documentation for this diagnostic code
}
```

### CodeAction (Fix Model)

```typescript
interface CodeAction {
    title: string;
    kind?: CodeActionKind;        // e.g. "quickfix", "refactor", "source"
    diagnostics?: Diagnostic[];   // diagnostics this action resolves
    isPreferred?: boolean;        // is this the preferred fix?
    disabled?: { reason: string };
    edit?: WorkspaceEdit;
    command?: Command;
    data?: LSPAny;                // opaque data for resolve
}

// Predefined CodeActionKind values:
// "quickfix"
// "refactor"
// "refactor.extract"
// "refactor.inline"
// "refactor.rewrite"
// "source"
// "source.organizeImports"
// "source.fixAll"
```

### The `data` Field

LSP 3.17 added a `data` field to `Diagnostic`. This is opaque JSON that a server can set on a diagnostic and receive back when the client requests code actions. This enables lazy computation — the server doesn't need to compute fixes until the user asks for them. The same pattern applies to `CodeAction.data` for lazy resolution of edits.

### Publishing Diagnostics

Diagnostics flow server → client via `textDocument/publishDiagnostics`:
```typescript
interface PublishDiagnosticsParams {
    uri: DocumentUri;
    version?: integer;
    diagnostics: Diagnostic[];
}
```

Or via the **pull model** (3.17+):
```typescript
// Client pulls: textDocument/diagnostic
interface DocumentDiagnosticParams {
    textDocument: TextDocumentIdentifier;
    identifier?: string;
    previousResultId?: string;
}

// Response types:
interface FullDocumentDiagnosticReport {
    kind: "full";
    resultId?: string;
    items: Diagnostic[];
}

interface UnchangedDocumentDiagnosticReport {
    kind: "unchanged";
    resultId: string;
}
```

The pull model enables incremental diagnostics with result IDs for caching.

### Position Encoding

LSP 3.17 added position encoding negotiation. The `general.positionEncodings` capability lets server and client agree on:
- `"utf-32"` — character offset in Unicode code points
- `"utf-16"` — character offset in UTF-16 code units (default, for historical reasons)
- `"utf-8"` — byte offset in UTF-8

---

## Language Server Index Format (LSIF)

### Overview

LSIF (pronounced "else if") pre-computes language intelligence data (references, definitions, hover, diagnostics) as a graph stored in NDJSON. Designed for code navigation on repositories without running a live language server.

Source: https://microsoft.github.io/language-server-protocol/specifications/lsif/0.6.0/specification/

### Diagnostic Representation in LSIF

LSIF stores diagnostics as pre-computed results attached to documents:

```json
{"id": 1, "type": "vertex", "label": "document", "uri": "file:///src/main.ts", "languageId": "typescript"}
{"id": 2, "type": "vertex", "label": "diagnosticResult", "result": [
    {
        "severity": 1,
        "code": "2304",
        "source": "ts",
        "message": "Cannot find name 'foo'.",
        "range": {"start": {"line": 4, "character": 10}, "end": {"line": 4, "character": 13}},
        "relatedInformation": [...]
    }
]}
{"id": 3, "type": "edge", "label": "textDocument/diagnostic", "outV": 1, "inV": 2}
```

The diagnostic structure mirrors LSP's `Diagnostic` interface exactly. LSIF adds:
- Graph edges linking documents to their diagnostic results
- Pre-computation: diagnostics are generated at index time, not query time
- The graph format allows tools to look up diagnostics without compilation

### Versioning

Diagnostics in LSIF are snapshot-based — they represent the state at index time. There is no incremental update model. A new index must be generated when code changes.

---

## Cross-Cutting Observations (Structural Facts Only)

### Location Representation Approaches

| System | Primary | Secondary | Multi-span |
|--------|---------|-----------|------------|
| Roslyn | `Location` (SyntaxTree + TextSpan) | `AdditionalLocations` list | No |
| TypeScript | byte offset + length | `relatedInformation[]` | No |
| Rust | `MultiSpan` (multiple labeled spans) | Child `Subdiag`s with spans | Yes |
| Go | `token.Pos` (offset) | `Related []RelatedInformation` | No |
| Swift | `SourceLoc` + `SourceRange` list | Child notes | No |
| CEL | line + column | None | No |
| Rego | row + col + offset | None | No |
| CUE | `token.Pos` + `InputPositions[]` | Multiple input positions | Yes (via InputPositions) |
| SARIF | `physicalLocation.region` | `relatedLocations[]` + `codeFlows` | Via related locations |
| LSP | `Range` (start/end Position) | `relatedInformation[]` | No |

### Severity Level Counts

| System | Levels |
|--------|--------|
| Roslyn | 4 (Hidden, Info, Warning, Error) |
| TypeScript | 4 (Warning, Error, Suggestion, Message) |
| Rust | 13 (Bug, Fatal, Error, DelayedBug, ForceWarning, Warning, Note, OnceNote, Help, OnceHelp, FailureNote, Allow, Expect) |
| Go | 1 (Error) + `Soft` flag |
| Swift | 4 (Error, Warning, Remark, Note) |
| LSP | 4 (Error, Warning, Information, Hint) |
| SARIF | 4 (error, warning, note, none) |
| CEL, Rego, CUE, Jsonnet, Pkl, Dhall, Starlark | 1 (Error only) |

### Fix/Suggestion Applicability

| System | Has fixes | Applicability levels | Multipart edits |
|--------|-----------|---------------------|-----------------|
| Roslyn | Yes (separate CodeFixProvider) | No | Yes |
| TypeScript | Yes (separate CodeFixProvider) | No | Yes |
| Rust | Yes (attached to diagnostic) | 4 (MachineApplicable, MaybeIncorrect, HasPlaceholders, Unspecified) | Yes |
| Go | Yes (SuggestedFix) | No | Yes |
| Swift | Yes (FixIt, attached) | No | Single edit per FixIt |
| SARIF | Yes (fixes array) | No | Yes |
| LSP | Yes (CodeAction) | `isPreferred` boolean | Yes (WorkspaceEdit) |
| All DSL-scale tools | No | N/A | N/A |

### Rule vs Instance Separation

| System | Explicit separation | Rule type | Instance type |
|--------|-------------------|-----------|---------------|
| Roslyn | Yes | `DiagnosticDescriptor` | `Diagnostic` |
| TypeScript | Yes | `DiagnosticMessage` | `Diagnostic` |
| Rust | Partial (via ErrCode + #[derive(Diagnostic)]) | `ErrCode` + generated descriptors | `DiagInner` |
| Go | No | N/A | `Error` / `Diagnostic` |
| SARIF | Yes | `reportingDescriptor` | `result` |
| LSP | No (code is just a string/number) | N/A | `Diagnostic` |

---

## References

### Source Repositories
- Roslyn: https://github.com/dotnet/roslyn — `src/Compilers/Core/Portable/Diagnostic/`
- TypeScript: https://github.com/microsoft/TypeScript — `src/compiler/types.ts`
- Rust: https://github.com/rust-lang/rust — `compiler/rustc_errors/src/`
- Go tools: https://github.com/golang/tools — `go/analysis/`
- Kotlin: https://github.com/JetBrains/kotlin — `compiler/fir/`
- Swift: https://github.com/swiftlang/swift — `include/swift/AST/DiagnosticEngine.h`
- CEL: https://github.com/google/cel-go — `common/error.go`
- OPA: https://github.com/open-policy-agent/opa — `ast/errors.go`
- CUE: https://github.com/cue-lang/cue — `cue/errors/errors.go`
- Jsonnet: https://github.com/google/go-jsonnet
- Pkl: https://github.com/apple/pkl
- Dhall: https://github.com/dhall-lang/dhall-haskell — `dhall/src/Dhall/TypeCheck.hs`
- Starlark: https://github.com/google/starlark-go — `syntax/syntax.go`, `starlark/eval.go`
- Dafny: https://github.com/dafny-lang/dafny
- Boogie: https://github.com/boogie-org/boogie
- Why3: https://why3.lri.fr
- CBMC: https://github.com/diffblue/cbmc

### Specifications
- SARIF 2.1.0: https://docs.oasis-open.org/sarif/sarif/v2.1.0/sarif-v2.1.0.html
- LSP 3.17: https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/
- LSIF 0.6.0: https://microsoft.github.io/language-server-protocol/specifications/lsif/0.6.0/specification/
