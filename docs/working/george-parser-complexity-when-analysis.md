# Parser Complexity Re-evaluation: `when`-to-End Move

**By:** George (Runtime Dev)
**Date:** 2026-04-28
**Status:** Analysis — findings for Shane and Frank
**References:**
- `src/Precept/Pipeline/Parser.cs` — parser stub (actual code)
- `src/Precept/Language/Constructs.cs` — live catalog (actual code)
- `src/Precept/Language/Tokens.cs` — token catalog and lexer tables
- `src/Precept/Language/TokenKind.cs` — token kind enum
- `docs/working/catalog-parser-design-v4.md` — disambiguator design
- `docs/working/catalog-parser-design-v5-lang-simplify.md` — original when-move analysis
- `.squad/decisions/inbox/copilot-directive-when-position.md` — locked decision record
- `.squad/decisions/inbox/frank-vocab-B3.md` — Frank's `->` verdict

---

## Ground Truth: The Parser Is a Stub

Before anything else, a fact that changes the framing of every claim below:

**`Parser.cs` (line 87):**
```csharp
public static SyntaxTree Parse(TokenStream tokens) => throw new NotImplementedException();
```

That is the entire parser body. The file contains ~80 lines of architectural design comments followed by this one-line stub. There is no disambiguation logic, no slot loop, no pre-parsed injection, no stash variable. Nothing has been implemented.

This means the complexity reduction from the when-move is not about code that was deleted. It is about design constraints that will govern the parser when it is written. The catalog is where the decision is recorded.

---

## Question 1: What Is the Current Parse Sequence for `in S VERB F [when Guard]`?

The Parser.cs comment block describes the intended dispatch table (lines 38–66). For the `in`-scoped path:

```
In → ParseInScoped() → disambiguates:
       ConstructKind.StateEnsure  (In <state> ensure ...)
       ConstructKind.AccessMode   (In <state> write ...)
```

The generic disambiguator flow (derived from v4 design, which the Parser.cs comment endorses):

1. Consume `in` token
2. Parse anchor target (state name identifier)
3. *(Pre-verb form only)* If `Current == When`: invoke expression parser, stash guard, set `guardConsumed = true`
4. Match `Current` against each candidate's `DisambiguationTokens`
5. Route to matched construct
6. Inject anchor (pre-parsed) at slot 0; if `guardConsumed`, inject stashed guard at first `GuardClause` slot index

This 6-step flow exists only in design documents (v4 § 6, Parser.cs comments). It has never been compiled.

**The current slot sequence in Constructs.cs for AccessMode (actual code):**
```csharp
ConstructKind.AccessMode => new(
    kind,
    "access mode",
    "...",
    "in Draft write Amount",
    [],
    [SlotOptStateTarget, SlotAccessModeKeyword, SlotFieldTarget],
    TokenKind.Write),
```

Three slots: `OptStateTarget`, `AccessModeKeyword`, `FieldTarget`. **No GuardClause slot.** This is the catalog as it exists today. The locked post-field guard form requires a `GuardClause` slot to be added after `FieldTarget` — that hasn't been added yet (a pending catalog update before parser implementation begins).

---

## Question 2: What Does the When-Post-Field Change Actually Deliver?

### The pre-verb `when` problem, precisely stated

With `in UnderReview when FraudFlag write AdjusterName` (old, spec-legal form), the `in`-scoped disambiguator faces this situation after consuming `in` and parsing the state target:

- The current token is `when`
- `when` is not in AccessMode's disambiguation tokens (`{Write, Read, Omit}`)
- The disambiguator cannot route until it knows which construct this is
- But it cannot know which construct this is until it sees the verb — which is buried behind the guard expression

**Resolution**: the disambiguator MUST invoke the expression parser before routing. It stashes the result. After routing, it injects the stash at a slot index that it determined by scanning the matched construct's slot array.

This creates **cross-method state**: the disambiguator allocates a `SyntaxNode? stashedGuard` variable, the expression parser runs in the disambiguator's context rather than the construct's own slot loop, and the slot injector must know the guard's target index (which could drift if the catalog slot sequence changes — hence the `SlotOrderingDriftTests` proposed in v4).

### What the post-field change removes

With `in UnderReview write AdjusterName when FraudFlag` (locked form), after consuming `in` and parsing the state target, the current token is `write`. The disambiguator matches it immediately. Steps 3 and 6 simply disappear:

**Reduced flow:**
1. Consume `in`
2. Parse anchor target
3. Match `Current` against `DisambiguationTokens` (`{Write, Read, Omit}`)
4. Route to AccessMode
5. *(guard is parsed by AccessMode's own `ParseGuardClause` slot parser, after `FieldTarget`)*

**Eliminated from the disambiguator:**
- `SyntaxNode? stashedGuard = null;` — the stash variable
- `bool guardConsumed = false;` — the tracking flag
- The `if (Current == When)` branch that invokes the expression parser
- The injection step that writes the stash to `slots[firstGuardIndex]`
- The `SlotOrderingDriftTest.PreParsedInjection_GuardSlotPositionMatchesExpectation` test cases for AccessMode
- All coordination code between the disambiguator and the AccessMode construct's builder

**What the catalog proves about this:**
The current `AccessMode` slot sequence — `[OptStateTarget, AccessModeKeyword, FieldTarget]` — has no `GuardClause`. Under the pre-verb form, guards were consumed by the disambiguator and injected externally, so the slot sequence didn't need to represent them. Under the post-field form, the GuardClause slot will be added to the sequence itself: `[OptStateTarget, AccessModeKeyword, FieldTarget, GuardClause]`. The guard becomes self-contained inside the construct's own slot loop. The disambiguator never sees it.

### Quantification

| Metric | Pre-verb `when` (old) | Post-field `when` (locked) |
|--------|----------------------|---------------------------|
| Disambiguator steps | 6 | 4 |
| Stash variables in disambiguator | 1 (`SyntaxNode? stashedGuard`) | 0 |
| Tracking flags in disambiguator | 1 (`bool guardConsumed`) | 0 |
| Cross-method injection paths | 1 (anchor + guard) | 1 (anchor only; guard stays in slot loop) |
| `SlotOrderingDriftTests` guard cases | 2–3 (per construct that supports pre-verb guard) | 0 for access mode |
| Expression parser invocations in disambiguator | Conditional (0 or 1) | 0 always |

The claim in v5 is accurate. The 6→4 reduction is real. The savings are not hypothetical.

### One honest caveat

The expression parser runs either way — once in the disambiguator (old form) or once in the slot parser (new form). The total expression-parsing work is identical. What's eliminated is the **coordination overhead**: stash allocation, flag tracking, cross-method injection, and slot-index drift tests. These are low-character-count but high-correctness-burden constructs. They are the kind of code that produces subtle bugs when constructs are refactored.

---

## Question 3: What Would the `->` Shape Cost or Save?

Shane's proposal: `in S -> F ADJECTIVE [when Guard]` vs. current `in S VERB F [when Guard]`.

### Is `->` already a token?

**Yes.** `TokenKind.Arrow` exists in `TokenKind.cs`. Its text is `"->"`. It is registered in `Tokens.cs` as a structural token, picked up by `TwoCharOperators` (which derives from all catalog entries where `Text.Length == 2` and category includes `Operator` or `Structural`). The lexer's `TryScanOperator()` method matches it via `TwoCharOperators` before falling through to single-char operators. `->` is fully lexed today.

**Current roles of Arrow in the language:**
1. `field Name as Type -> Expr` — computed field introduction (in `FieldDeclaration`, after modifier list)
2. `from State on Event -> action -> outcome` — action chain / outcome separator (in `TransitionRow`, `StateAction`, `EventHandler`)
3. Disambiguation token: `to/from State -> ...` → routes to `StateAction`

### Is `->` unambiguous as an `in`-scope disambiguation token?

**Yes.** Looking at the current `in`-scoped disambiguation entries:
- `Ensure` → StateEnsure
- `Write/Read/Omit` (or `Editable/Fixed/Omit` per B2/B3) → AccessMode

`Arrow` is not currently a valid token after `in StateTarget`. `in State ->` is currently a parse error. So using `Arrow` as the `in`-scope disambiguation token for a new access mode shape would create no ambiguity.

### Does `F ADJECTIVE` create new lookahead?

**No.** The slot boundary between FieldTarget and ADJECTIVE is unambiguous at the lexer level. Field targets are identifiers; adjectives (`editable`, `fixed`, `omit`) are keywords. The lexer produces distinct token kinds for identifiers vs. keywords (`TokenKind.Identifier` vs. `TokenKind.Editable`, etc.). The `ParseFieldTarget` slot parser reads tokens until it sees a non-identifier (i.e., a keyword or structural token). It stops at the adjective keyword. The adjective slot parser then consumes the keyword. Zero lookahead beyond the current token.

Compare to current `VERB F`: the verb (`editable`, `fixed`, `omit`) is consumed first, then the field target identifier. The adjective-after-field form swaps the order but creates no new parsing challenge because the type system of tokens (keyword vs. identifier) gives the parser enough information at each step.

### What is the actual parse delta?

**Current VERB F shape (B2/B3 vocabulary):**
- Disambiguation token: `editable`, `fixed`, or `omit` (3 tokens)
- The consumed token is ALSO the slot content — handled by `LeadingTokenSlot: AccessModeKeyword`
- After disambiguation, `AccessModeKeyword` slot is already filled. Parser runs `FieldTarget` slot, then `GuardClause` slot
- Slot sequence: `[OptStateTarget, AccessModeKeyword, FieldTarget, GuardClause]`

**Shane's `-> F ADJECTIVE` shape:**
- Disambiguation token: `Arrow` (1 token)
- `Arrow` has no slot content value — it's a structural separator, not a keyword with semantic meaning
- After disambiguation, parser runs `FieldTarget` slot, then `AccessModeKeyword` (adjective) slot, then `GuardClause` slot
- Slot sequence: `[OptStateTarget, FieldTarget, AccessModeKeyword, GuardClause]`

**The concrete difference:**
- VERB F: 1 disambiguation token (keyword), `LeadingTokenSlot` injects it into slot 1, 2 more slot parsers run (FieldTarget, GuardClause)
- `-> F ADJ`: 1 disambiguation token (arrow, no content), 3 slot parsers run (FieldTarget, AccessModeKeyword, GuardClause)

The `->` shape requires one additional explicit slot parser step (AccessModeKeyword). The VERB F shape gets AccessModeKeyword "for free" via the `LeadingTokenSlot` injection mechanism. Net result: the `->` shape is slightly more work at the slot-loop level, not less.

**At the disambiguation level**, `->` is marginally simpler (one token in the set vs. three). But this is noise — a linear scan over 3 tokens vs. 1 is immeasurable in a parser that runs at most a few thousand declarations total.

### Frank's recommendation on `->` (B3): correct from the parser angle

Frank's B3 verdict rejects `->` on semantic coherence grounds: `->` carries "visual pipeline" semantics throughout the language; a second use as "access mode field introducer" is a meaning collision. This is accurate, and it's also accurate from the parser angle. Disambiguation tables are most readable when each token has one role across contexts. Introducing a second role for `Arrow` — one that is structurally unrelated to its existing pipeline meaning — increases the cognitive load of the parser code without delivering any parsing benefit.

---

## Verdict

### 1. Does the when-post-field change deliver measurable complexity reduction?

**Yes, with the right unit of measurement.** The reduction is not in lines of compiled code (the parser is a stub) — it's in the design constraints encoded by the catalog. The `AccessMode` slot sequence has no `GuardClause` today. When the parser is written:
- The disambiguator has no stash variable, no tracking flag, no conditional expression-parser invocation, and no cross-method injection for access mode guards
- That's 4 eliminated code constructs in the most correctness-sensitive part of the parser
- The corresponding `SlotOrderingDriftTests` guard-injection cases are also eliminated

The v5 6→4 step claim is accurate. The savings are real. They will materialize when the parser is implemented.

### 2. Does Shane's `->` proposal make things simpler, the same, or harder?

**The same to marginally harder.** At disambiguation: slightly simpler (one token vs. three). At slot parsing: one additional explicit step (AccessModeKeyword must be parsed explicitly vs. injected via `LeadingTokenSlot`). Net: no complexity gain. Frank's semantic coherence objection is valid, and the parser-level analysis reinforces it — there's no parser benefit to trade against the semantic cost.

### 3. Does `F ADJECTIVE` (adjective after field) add new lookahead?

**No.** Keywords and identifiers are lexically distinct. The slot boundary is unambiguous without any lookahead.

---

## Pending Catalog Gap

One thing surfaced in this analysis that isn't in the current catalog:

The `AccessMode` construct needs a `SlotGuardClause` added at the end of its slot sequence to reflect the locked post-field guard position:

```csharp
// Current (incomplete — missing GuardClause):
[SlotOptStateTarget, SlotAccessModeKeyword, SlotFieldTarget]

// Correct (with locked post-field guard):
[SlotOptStateTarget, SlotAccessModeKeyword, SlotFieldTarget, SlotGuardClause]
```

This should be applied before parser implementation begins for this construct. It's a one-line catalog update.
