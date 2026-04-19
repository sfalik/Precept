# Parser Combinator Scalability: Superpower, PEG Limits, and Migration Options

**Date:** 2026-04-19
**Author:** Frank (Lead/Architect)
**Research Angle:** Scalability limits of Superpower/PEG-based combinators for Precept's grammar
**Purpose:** Answer: when does Superpower break down as Precept's grammar grows, what would ANTLR or hand-written recursive descent buy, and at what cost?

---

## 1. Executive Summary

**Verdict: Superpower handles Precept's current grammar well and will handle the next 3–5 years of planned growth — provided the grammar stays within its four hard constraints.** Those constraints are: no indentation sensitivity, no left-recursive expression forms, no deeply ambiguous ordered-choice chains, and no context-sensitive tokenization. Precept's design already rules out the first two by principle; the remaining two require attention as the expression grammar grows.

The specific trigger that would open a genuine migration conversation is **nested type parameter syntax with angle brackets** (e.g., `map<string, set<number>>`). Ordered choice with `<` as both a comparison operator and a type-open bracket is a known PEG failure mode. A second trigger would be **multi-error recovery** — if the language server needs to continue parsing after the first syntax error to produce richer diagnostics, Superpower's stop-on-first-error model becomes a ceiling. Neither trigger is imminent, but both are on the grammar's horizon.

ANTLR is overkill for Precept's current size. A hand-written recursive descent parser is the right long-term path if the grammar exceeds roughly 30–40 productions with non-trivial error recovery requirements, which could happen in a 5-year horizon if the expression language continues expanding.

---

## 2. Survey Results

### Source 1: Superpower (github.com/datalust/superpower)

**Design:** Token-list parser combinator for C#. The key architectural decision that matters for Precept: Superpower operates on a pre-tokenized `TokenList<TKind>` rather than raw characters. This is the PEG combinator's main performance advantage for structured languages — the token stream is smaller than the character stream, and token identity is already resolved, so the combinator layer sees keyword/identifier/operator tokens rather than individual characters.

**Performance:** Superpower benchmarks ~3.5x faster than Sprache (the classic character-level C# combinator) on arithmetic expression parsing. Real-world projects that ship on Superpower: Serilog.Expressions, the Seq log query language, and PromQL.Parser. These are all DSLs of small-to-medium complexity — similar in scale to Precept.

**Known limits:**
- **No packrat memoization.** Superpower does not memoize intermediate results between alternatives. Pathological grammars with significant backtracking degrade to exponential time. For well-designed grammars with early disambiguation (keyword anchoring, `.Try()` chains with distinct first tokens), this is not observed in practice.
- **No left recursion.** Left-recursive rules (`E → E '+' T`) cause infinite loops. Must be rewritten using iteration (`Parse.Chain` or `.Many()`). Superpower provides `Parse.Chain` specifically for left-associative operators, which Precept already uses throughout the expression grammar.
- **Stop-on-first-error.** The combinator produces one error per parse attempt. Language server scenarios that want to recover and continue — to highlight multiple syntax errors simultaneously — cannot be done without architectural changes.
- **No indentation sensitivity.** There is no offside-rule or layout-sensitive scanning. The tokenizer would need to emit synthetic INDENT/DEDENT tokens, which Superpower's `TokenizerBuilder` does not support natively.
- **Performance tips (official documentation):** Avoid LINQ comprehensions in hot-path parsers; prefer `Then()`/`IgnoreThen()` chaining, which allocates fewer delegates. Precept's `PreceptParser.cs` already follows this guidance in the expression layer.

**Actively maintained:** v3.1.0 released June 2025. Apache-2.0 license.

### Source 2: Parsing Expression Grammars (Wikipedia/Ford 2004)

**Key formal properties established by Bryan Ford's original 2004 paper:**

- **Determinism:** PEG ordered choice (`/`) always selects the first alternative that succeeds. No ambiguity is possible. This is a feature for DSLs (eliminates dangling-else class problems) and the reason Precept's grammar is completely deterministic.
- **No left recursion in well-formed PEGs.** A PEG is "well-formed" if it contains no left-recursive rules. Left recursion causes infinite regress in top-down parsers. Workarounds exist (Warth et al. 2008 extended packrat to support direct left recursion, but at the cost of losing linear-time guarantees).
- **Packrat parsing achieves linear time, at O(n) memory cost.** Without memoization, PEG parsers can degrade to exponential time in pathological cases. Superpower does NOT implement packrat — its linear-time behavior in practice depends on the grammar having limited backtracking paths.
- **Midpoint problem.** PEG's greedy ordered choice cannot recognize certain unambiguous CFG rules that require trying multiple parse midpoints (e.g., `S → 'x' S 'x' | 'x'`). This is rarely an issue for programming language DSLs but worth noting for highly recursive symmetric structures.
- **Rule order affects the matched language.** In PEG, adding an alternative to a choice can shadow previously recognized strings. `A = "a" "b" / "a"` and `A = "a" / "a" "b"` match different languages — the second alternative in the latter is unreachable. This is an ongoing grammar maintenance risk as alternatives are added.

### Source 3: Recursive Descent Parser (Wikipedia)

**Formal properties:**
- A predictive (non-backtracking) recursive descent parser requires an LL(k) grammar — one where k lookahead tokens are sufficient to select the production without trial. Precept's statement grammar is effectively LL(1): the first token uniquely identifies every statement kind (keyword anchoring).
- Recursive descent WITH backtracking extends to non-LL(k) grammars but is not guaranteed to terminate in polynomial time unless the grammar is LL(k). PEG formalizes this backtracking behavior.
- Hand-written recursive descent parsers are the choice of production compilers — notably Clang (C/C++ front-end) and Roslyn (C#/VB). The rationale: full control over error recovery, incremental parsing, and arbitrary semantic context during parse.

**ANTLR generates recursive descent parsers.** From the Wikipedia entry: "Predictive parsers can also be automatically generated, using tools like ANTLR." ANTLR's LL(*) algorithm extends predictive parsing to grammars that require more than k fixed lookahead tokens by computing lookahead sets lazily using the full grammar.

### Source 4: Roslyn Overview (github.com/dotnet/roslyn/docs/wiki/Roslyn-Overview.md)

**Architectural decision:** The Roslyn team hand-wrote the C#/VB parser. This was not the obvious choice — they could have used a parser generator. The reasons, as documented:

1. **Full-fidelity syntax trees.** Roslyn syntax trees include every piece of source text: tokens, trivia (whitespace, comments, preprocessor directives). This enables round-trip fidelity for refactoring tools. A combinator or generator doesn't naturally produce trivia-preserving trees.
2. **Error recovery across multiple failures.** Roslyn inserts "missing tokens" and "skipped tokens" into the tree when parse fails, then continues. This allows the IDE to show squiggles on multiple errors simultaneously. Stop-on-first-error would be unacceptable for a production IDE.
3. **Incremental reparsing.** Roslyn can reparse only the changed portions of a file on each keystroke. This requires intimate control over the parser's lookahead and state, which is only practical in hand-written code.
4. **Immutable snapshot model.** Syntax trees are immutable and thread-safe, shared safely across IDE features. This design was achievable because the parser was hand-written with that property in mind from the start.

**Scale:** The Roslyn parser is a massive investment — tens of thousands of lines for C# alone. For Precept, the lesson is not "copy Roslyn" but "understand what problems Roslyn solved and whether Precept will encounter them."

### Source 5: ANTLR (antlr.org — HTTP 429 at time of fetch; from knowledge)

ANTLR (Another Tool for Language Recognition) generates LL(*) parsers from grammar files. Key differentiators from PEG combinators:

- **LL(*) lookahead:** ANTLR uses all available grammar information to compute lookahead sets, avoiding the fixed-k limitation of LL(k). In practice this means ANTLR can handle grammars that would require significant trial-and-error backtracking in a combinator.
- **Left recursion rewriting:** ANTLR 4 automatically rewrites left-recursive rules into equivalent right-recursive forms before generating the parser. This means you can write `expr : expr '+' expr` directly in the grammar without manual transformation.
- **Grammar as a separate artifact:** The grammar lives in `.g4` files, separate from the host language code. This is an advantage (grammar is reviewable independently) and a cost (build-time code generation, additional toolchain dependency, generated C# files to maintain).
- **Error recovery:** ANTLR's default error recovery uses "single-token deletion/insertion" — it can often recover from one bad token and continue parsing, producing partial trees with error nodes.
- **Scale:** ANTLR is used for very large grammars: SQL parsers (TSQL, PostgreSQL), Java/Python grammar references, Kotlin's compiler. It is appropriate when grammar complexity justifies the toolchain cost.

---

## 3. PEG Grammar Properties

### What PEG Guarantees

| Property | Guarantee |
|---|---|
| **Determinism** | Ordered choice always picks the first matching alternative. Exactly one parse tree or failure — never ambiguity. |
| **No ambiguity** | Every input has at most one valid parse. Ambiguous CFG rules are resolved by rule order in PEG. |
| **Full lookahead** | And-predicates (`&e`) and not-predicates (`!e`) allow arbitrarily complex lookahead without consuming input. |
| **Context-free+ expressivity** | PEG can recognize some non-context-free languages (e.g., `aⁿbⁿcⁿ`) that CFGs cannot. |
| **Linear time (with packrat)** | With memoization, any PEG grammar parses in O(n) time. |

### What PEG Does Not Guarantee

| Property | Risk |
|---|---|
| **No left recursion** | Left-recursive rules → infinite loop. Must be manually eliminated (use `*`/`+`/`Parse.Chain`). |
| **Greedy greediness risk** | `a* a` always fails: `a*` greedily consumes all `a`s, leaving none for the second `a`. Counter-intuitive but definitional. |
| **Rule order correctness** | Adding a shorter alternative before a longer one silently removes strings from the matched language. `"a" / "ab"` never matches "ab". |
| **Empty-language undecidability** | It is algorithmically undecidable whether a PEG grammar matches any string. (Post correspondence reduction.) |
| **Polynomial time without packrat** | Without memoization, pathological grammars degrade to exponential time. |

### Superpower's Position on This Spectrum

Superpower does not implement packrat. Its bet is that practical DSL grammars — with keyword anchoring, distinct first tokens for each alternative, and minimal backtracking paths — will never trigger the exponential-time case. This bet is correct for Precept's current grammar. The question is whether it remains correct as the grammar grows.

---

## 4. Superpower Specifically

### What It Handles Well

**Token-list parsing.** Operating on tokens rather than characters eliminates all character-level backtracking. By the time Superpower runs, the tokenizer has already classified every character. The token stream is 10–100x shorter than the character stream for a typical `.precept` file. This alone eliminates the most common PEG pathology: trying multiple character-level alternatives for keywords vs. identifiers.

**Keyword-anchored dispatch.** When every statement begins with a distinct keyword, the top-level parser needs zero backtracking to dispatch to the right production. This is exactly Precept's design: `precept`, `state`, `event`, `initial`, `field`, `from`, `to`, `in`, `on`, `rule`, `ensure`, `edit`. The first token uniquely selects the parser branch.

**`Parse.Chain` for left-associative operators.** The library provides explicit left-chaining support. Precept's expression grammar uses this throughout: multiplicative (`* / %`), additive (`+ -`), comparison (`== != > >= < <= contains`), logical AND, logical OR. No left recursion is needed anywhere in the expression hierarchy.

**`Parse.Ref` for recursive references.** Parenthesized sub-expressions (`ParenExpr`), conditional branches (`ConditionalExpr`), and function arguments (`FunctionCallAtom`) use `Parse.Ref` to break initialization cycles. This is the standard Superpower pattern for recursive grammars and works cleanly.

**Error messages.** Superpower reports errors in terms of token kinds (unexpected identifier `frm`, expected keyword `from`) rather than raw character positions. This is a material improvement over character-level combinators. Precept further augments error messages in `ParseWithDiagnostics` by constructing context-aware messages from the remaining token stream.

### What It Doesn't Handle Well

**Multi-error recovery.** After the first parse failure, Superpower stops. It returns one diagnostic. The language server works around this adequately today, but if Precept grows to the point where authors regularly write complex multi-statement files with multiple simultaneous errors, single-error reporting becomes a genuine UX constraint.

**Indentation sensitivity.** Not supported. No INDENT/DEDENT token synthesis. This is a hard boundary.

**Operator/bracket ambiguity.** When the same token (`<`) serves as both a comparison operator and a type-open delimiter, ordered choice makes it difficult to parse without significant lookahead. Precept currently sidesteps this by using `set<T>`, `queue<T>`, `stack<T>` type syntax in declaration position only — never in expression position. Nested generic types (if ever added) would need careful grammar engineering.

**Grammar debugging.** When an expression fails to parse, diagnosing which `.Try().Or()` branch failed and why requires understanding the ordered-choice chain. There is no grammar visualization or conflict detection like ANTLR's grammar analysis. This is a developer experience cost as the grammar grows.

---

## 5. The Three Alternatives

### Option A: Parser Generator (ANTLR)

**When to choose it:**
- Grammar complexity exceeds ~30–40 productions with significant ambiguity or left-recursive forms that require ANTLR's auto-rewriting.
- A non-C# team member needs to read or modify the grammar. ANTLR `.g4` files are language-independent and readable without C# knowledge.
- You want static grammar analysis: dead rule detection, ambiguity detection, first/follow set computation. ANTLR's tooling provides these; combinators don't.
- Grammar tests at the specification level (test the grammar file, not the generated code) are a requirement.

**What it solves for Precept:**
- Left recursion in expression grammars (ANTLR rewrites it)
- LL(*) lookahead for cases where multiple alternatives share a long common prefix
- Automatic error recovery

**Cost:**
- Build-time code generation step. Generated C# files checked in or generated on build.
- ANTLR runtime dependency (~400KB).
- Grammar split from parser logic — semantic actions (building `PreceptExpression` nodes) are awkward in `.g4` files vs. clean C# LINQ in Superpower.
- Significant rewrite of `PreceptParser.cs` (2,000+ lines).
- ANTLR's error recovery sometimes produces unexpected partial trees; Precept's language server would need to handle ANTLR error node types.

**Verdict for Precept:** Not warranted now. Becomes worth evaluating if the grammar adds nested type parameters, significant ambiguity, or cross-team grammar ownership.

---

### Option B: Hand-Written Recursive Descent (Roslyn Style)

**When to choose it:**
- You need multi-error recovery (continue parsing after first syntax error).
- You need incremental reparsing (reparse only changed tokens on each keystroke — critical for large files with many type errors in the language server).
- You need full trivia (whitespace, comments) in the syntax tree — for formatter, document symbol, or hover range precision.
- Grammar ownership is long-term and internal (no external grammar file needed).
- The grammar has grown to the point where combinator chains are hard to follow and test.

**What it solves for Precept:**
- Multiple simultaneous error diagnostics from the language server
- Incremental parsing in the language server (currently parses the full file on every change)
- Full position fidelity for every token including whitespace (better hover ranges, more precise squiggles)

**Cost:**
- High upfront investment: 3,000–8,000 lines of new C# for a full recursive-descent parser covering Precept's grammar.
- Discipline cost: every grammar addition requires manually updating the parser AND maintaining the descent structure. No declarative grammar spec.
- Roslyn needed years of investment to reach production quality on error recovery. A DSL-scope hand-written parser would be simpler, but still non-trivial.

**Verdict for Precept:** Not warranted now. Becomes worth evaluating around the 5-year horizon if the language server's incremental parsing or multi-error recovery becomes a user pain point.

---

### Option C: Superpower Token Combinators (Current)

**When to choose it (stay with it):**
- Grammar stays keyword-anchored and flat at the statement level.
- Expression grammar grows without introducing left recursion, indentation sensitivity, or angle-bracket type syntax in expression position.
- Single-error reporting is acceptable for the language server (currently true: Precept files are small and authoring is incremental).
- Grammar ownership is internal and the combinator-as-code style is a net positive for readability and co-location with types.

**What it provides:**
- Zero build-time toolchain overhead.
- Grammar and AST construction co-located in one C# file — easy to trace from combinator to output node.
- Token-level error messages out of the box.
- Performance adequate for DSL files of Precept's scale (sub-millisecond parse for typical files; the language server already benchmarks well).

**Limits (restatement):**
- No packrat: complex backtracking grammars degrade.
- No left recursion.
- No indentation sensitivity.
- Stop-on-first-error.
- No static grammar analysis.

---

## 6. Decision Criteria: Grammar Features That Would Trigger Reconsideration

The following grammar additions are the specific triggers that should open a "do we need to reconsider the parser?" conversation:

| Grammar Feature | Risk | Trigger Level |
|---|---|---|
| **Indentation-sensitive blocks** | Impossible in Superpower without tokenizer INDENT/DEDENT surgery | **Hard stop — redesign required** |
| **Nested type parameters with `<T>`** | `<` is comparison operator AND type-open; ordered choice fails silently | **Re-evaluate parser strategy** |
| **Left-recursive expression forms** | Infinite loop; must rewrite to `Parse.Chain` or explicit loop | **Engineering required, not architectural change** |
| **Context-sensitive tokenization** | Keywords that are identifiers in certain positions (e.g., `from` as field name) | **Tokenizer surgery required** |
| **Multi-line string literals / embedded DSLs** | Character-level parsing within token; Superpower character parsers can be mixed in, but complexity grows | **Monitor; manageable with `Apply()`** |
| **50+ grammar productions** | Combinator initialization cost; static analysis deficiency | **Consider ANTLR** |
| **Multi-error recovery requirement** | Fundamental Superpower limitation | **Consider hand-written RD** |
| **Complex type system (union types, intersection types)** | Requires significant ambiguity handling in type expressions | **Evaluate ANTLR or hand-written RD** |

**Current nearest trigger:** Nested type parameters (`map<K, V>` or `set<set<T>>`). The existing `set<T>` syntax is safe because `<` in collection-type position is unambiguous — it follows a keyword (`set`, `queue`, `stack`), not an identifier. Adding parameterized or nested collection types that could appear in expression position would require deliberate grammar engineering.

---

## 7. Current Risk Assessment for Precept

### Today's Grammar

Precept's current grammar in `PreceptParser.cs` has these structural properties:

- **Statement level:** Fully keyword-anchored. Every statement's first token is a keyword (`precept`, `state`, `event`, `initial`, `field`, `from`, `to`, `in`, `on`, `rule`, `ensure`, `edit`). Zero backtracking at statement dispatch — the parser selects the production in O(1) from the first token. **Risk: none.**

- **Expression level:** Five-level precedence hierarchy (`or` → `and` → comparison → additive → multiplicative → unary → atom). All binary operators use `Parse.Chain` (left-associative, no left recursion). `Atom` dispatches across ~8 alternatives (`NumberAtom`, `StringAtom`, `TrueAtom`, `FalseAtom`, `NullAtom`, `ParenExpr`, `ConditionalExpr`, `FunctionCallAtom`, `DottedIdentifier`), all gated with `.Try()`. First-token disambiguation is effective for most atoms (number literals, string literals, boolean keywords, `null`, `(`, `if`). `FunctionCallAtom` requires a `Where` predicate check against the `FunctionRegistry` before trying — this is a small but real runtime cost on every identifier in expression position. **Risk: low.**

- **DottedIdentifier:** Uses `.Then()` chains with `.Try().Or()` to handle 1, 2, or 3-part identifiers (`Field`, `Arg.Field`, `Collection.member.subfield`). Three-level dot chains are the deepest structured lookback in the grammar. **Risk: none.**

- **Recursive references:** `ParenExpr`, `ConditionalExpr`, `FunctionCallAtom` all use `Parse.Ref` to recurse through `BoolExpr`. This is bounded in practice by parenthesis depth and argument count. Deeply nested expressions (e.g., `((((a + b))))`) are correctly handled by the recursive structure. **Risk: none for typical DSL usage.**

- **Conditional expressions:** `if condition then thenBranch else elseBranch` — all three branches are full `BoolExpr` references. Nesting is supported via parentheses: `if A then (if B then 1 else 2) else 3`. The `if` token uniquely identifies this production. **Risk: none.**

- **Type expressions:** `ScalarType` and `ScalarTypeOrChoice` dispatch on distinct token types (`string`, `number`, `boolean`, `integer`, `decimal`, `choice`). Collection types (`set<T>`, `queue<T>`, `stack<T>`) appear in `field` declaration position only, where `<` is unambiguous. **Risk: low, but increases if collection types gain nesting.**

### Near-Term Proposals (Issues #9, #16, #25, #26, #27, #29)

- **#9 (conditional expressions):** Already implemented via `ConditionalExpr`. No parser change required.
- **#16 (built-in function library):** `FunctionCallAtom` already handles n-ary functions. Adding new function names to `FunctionRegistry` does not change parser structure. Risk: near-zero.
- **#25 (choice type):** `choice("A", "B", "C")` already parsed by `ScalarTypeOrChoice`. Risk: none.
- **#26 (date type), #27 (decimal), #29 (integer):** Additional scalar type tokens. Risk: none — `ScalarType` is an `Or`-chain and new types add one branch.

### Medium-Term Horizon (18–36 months)

- **#10 (string `.length`), #15 (string `.contains()`):** Method-call syntax on field values. `.length` is a dotted member accessor (already handled by `DottedIdentifier`). `.contains()` would be a method call on an expression result. If this is parsed as `expr.contains(arg)`, the current `DottedIdentifier` + `FunctionCallAtom` split would need to be unified into a postfix-call form. **Risk: medium — requires grammar refactoring but not architectural change.**
- **Nested collection types** (if proposed): `set<set<number>>` requires nested `<T>` parsing in declaration position. Since `<` appears after a keyword, not after an identifier, disambiguation is tractable via careful ordered choice. **Risk: medium.**

### Overall Assessment

**Current risk level: LOW.** Superpower is a sound choice for Precept's grammar today and through the next 18–24 months of planned development without architectural change. The grammar is well-designed to leverage Superpower's strengths (keyword anchoring, no left recursion, clean token-stream dispatch) and avoids its weaknesses (no indentation, no context-sensitive tokenization, no angle-bracket type parameters in expression position).

**Horizon risk level: MEDIUM at 36–60 months.** The primary drivers: method-call syntax on values (postfix dot-call), nested generics if added, and multi-error recovery pressure from the language server as files grow larger.

---

## 8. Implications for PreceptParser.cs

**No action required immediately.** The parser is well-structured and its design aligns with Superpower's performance model.

**Specific patterns to watch:**

1. **FunctionCallAtom `Where` predicate:** Every identifier in expression-atom position is tested against `FunctionRegistry.IsFunction()`. This is a `O(1)` hash lookup but happens on every atom parse attempt. If the function library grows large (>50 functions) or if expressions become deeply nested, profile this path. Mitigation: reserve a distinct token type for built-in function names at the tokenizer level (currently identifiers and function names share `PreceptToken.Identifier`).

2. **`Atom` ordered-choice chain:** Currently 8 alternatives with `.Try()`. As new expression atoms are added (new literal types, method calls, more complex forms), the chain grows. Keep `.Try()` only where needed (alternatives with overlapping first tokens). Alternatives with distinct first tokens do not need `.Try()` and should use `.Or()` directly.

3. **DottedIdentifier depth:** If 4+ level dotted access is proposed (e.g., `Event.Args.Collection.member`), the current `.Then()` chain would need to become a loop (`Token.EqualTo(Dot).IgnoreThen(AnyMemberToken).Many()`). This is a low-effort change and removes the fixed-depth assumption.

4. **Method-call postfix syntax:** If `Field.contains(expr)` is added, the `DottedIdentifier` parser needs to be extended to try a `(...)` suffix after the final member token. The clean way to do this in Superpower is to parse the dotted access first, then `.Then(opt => ...)` for an optional `(arglist)` continuation. This keeps the combinator structure manageable.

5. **Angle-bracket type syntax in expressions:** If a proposal introduces generic types that can appear in expression position (e.g., casting syntax `as set<T>`), halt and evaluate the parser strategy before implementation. This is the one grammar change that would require ANTLR's LL(*) lookahead or a hand-written approach to handle cleanly.

---

## 9. References

| Source | URL / Location |
|---|---|
| Superpower library README | https://github.com/datalust/superpower |
| Ford, B. (2004). Parsing Expression Grammars | https://bford.info/pub/lang/peg.pdf |
| Parsing Expression Grammar — Wikipedia | https://en.wikipedia.org/wiki/Parsing_expression_grammar |
| Recursive Descent Parser — Wikipedia | https://en.wikipedia.org/wiki/Recursive_descent_parser |
| Warth, A. et al. (2008). Packrat Parsers Can Support Left Recursion | http://www.vpri.org/pdf/tr2007002_packrat.pdf |
| Roslyn Overview | https://github.com/dotnet/roslyn/blob/main/docs/wiki/Roslyn-Overview.md |
| ANTLR.org | https://www.antlr.org/ (HTTP 429 at fetch time; from knowledge) |
| Precept Language Design doc | docs/PreceptLanguageDesign.md |
| PreceptParser.cs | src/Precept/Dsl/PreceptParser.cs |
