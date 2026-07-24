# DSL / Compiler / Runtime Bugs

Bugs discovered during sample authoring or development. Capture here when
something blocks idiomatic expression; fix as separate engineering work — do
**not** author hidden workarounds into samples or other consumers. If a
workaround is unavoidable, include a comment citing the BUG ID.

The proof engine `ConstraintContext` narrowing gap fixed earlier in the
V2-Radical spike is the precedent for this discipline: without it, the
workaround would have stayed buried in sample 29 and the bug would never have
surfaced for proper fixing.

## Bug template

```markdown
### BUG-NNN: <one-line summary>

- **Discovered**: YYYY-MM-DD during <context>
- **Affected**: <files / samples / features>
- **Symptom**: <what was observed — diagnostic code, error message, behavior>
- **Root cause** (if known): <source file / function / brief description>
- **Workaround used** (if any): <how it's currently sidestepped — and which files cite this BUG ID in a comment>
- **Fix complexity**: trivial / small / large / design-required
- **Priority**: blocks shipping / quality bar / nice-to-have
- **Repro**: <minimal `.precept` snippet or test case that triggers it>
```

## Active

### BUG-051: A `mincount` is consumed as proof a collection is non-empty, with no establishment obligation anywhere (SOUNDNESS hole)

- **Discovered**: 2026-07-23, probe pass before drafting the collection-non-empty validity argument. **Confirmed via `precept_compile` at HEAD.**
- **Symptom**:

  ```precept
  precept MincountNeverEstablished

  field Items as list of decimal mincount 1 maxcount 5
  field Picked as decimal default 0.0

  event Look()

  on Look
      -> set Picked = Items.first
  ```

  → `success: true`, `List must be non-empty` **Proved** by `DeclarationAttribute`. The obligation list contains **no establishment obligation for `Items`**. The collection has no default and no write site anywhere in the file, so it can only ever be empty — and `.first` on it is proved safe.
- **Same class as BUG-048**, on a different declaration: a declared fact is consumed as a premise while nothing anywhere establishes it. Together they are two instances of the machinery the constraint-establishment design was meant to build, which is currently unlocked.
- **Fix complexity**: blocked on that machinery. Until it exists, a `mincount` cannot soundly discharge a non-empty precondition.
- **Status**: Active — soundness hole.

### BUG-050: The declaration-attribute discharge never checks whether the field was reassigned, so a declared fact survives the write that breaks it (SOUNDNESS hole)

- **Discovered**: 2026-07-23, across two independent probe passes — the non-negative-operand and collection-non-empty argument drafts found it on different obligations. **Confirmed via `precept_compile` at HEAD.**
- **Root cause, and it is one missing check**: `TryDeclarationAttributeProof` (`ProofEngine.Strategies.cs:85-228`) never consults `ReassignedBefore`, while `TryGuardInPathProof` does (`:771`). So a fact taken from a declaration survives a write in the same chain that falsifies it, where the identical program written with a guard is correctly rejected.
- **Symptom A — sign**: `field A as number nonnegative` with `set A = approximate(0.0 - 4.0)` followed by `set R = sqrt(A)` in the same chain compiles with zero diagnostics, `Proved` by `DeclarationAttribute`. The mirror program using a guard instead of a declaration is rejected.
- **Symptom B — collection**: `field Items as list of decimal mincount 1` with `set Items = Other` (where `Other` carries no `mincount`) followed by `Items.first` reports `List must be non-empty` **Proved**. Full-value replacement emits no count obligation, and the discharge consults neither `ReassignedBefore` nor `CountInvalidatedBefore`.
- **Why it matters beyond these two**: this is the declaration-sourced half of the sequential-proof-flow guarantee the spec claims is implemented — *"When a field is reassigned, prior proof facts about that field are invalidated before the new assignment's facts are stored"* (`precept-language-spec.md` § 0.6, Proof philosophy 7). It holds for guard-sourced facts and not for declaration-sourced ones, so the guarantee is half-built and the docs do not say which half.
- **Scope / class**: SOUNDNESS, over-accept, reaching every obligation that can discharge from a declaration — divisor, non-negative, non-empty, and the qualifier kinds.
- **Fix complexity**: small in mechanism — consult `ReassignedBefore` on the declaration path as the guard path already does — but it will surface wherever a declared fact is currently doing work it has not earned, so the corpus impact wants measuring before it lands.
- **Status**: Active — soundness hole.

### BUG-049: Unit normalization silently rounds every conversion to 24 decimal places, contradicting two canonical exactness guarantees

- **Discovered**: 2026-07-23, by a canon search checking whether the quantity/price bound question was already ruled. It was not — but the rounding turns out to contradict locked spec, so this is a bug rather than an open ruling.
- **Symptom**: `field H as quantity in 'yg' nonzero default '1 yg'` is **rejected** — `PRE0079`, default violates `nonzero` — because `1 yg` normalizes to `1e-27` base units, which the normalizer rounds to zero. A genuinely non-zero authored magnitude is refused against its own declaration. `zg` passes; `yg` does not. The boundary is an artifact, not a decision.
- **Root cause**: `Numeric/TypedConstantNormalizer.cs:88` — `private static decimal NormalizeResult(decimal value) => decimal.Round(value, 24, MidpointRounding.ToEven);` — applied unconditionally to every conversion, offset or not. Replicated into the interval path as `TrimIntervalPrecision`.
- **It contradicts two canonical statements**:
  - `business-domain-types.md:426` — *"The extraction is exact; bounds never round."* Every `quantity` bound routes through the normalizer, so bounds do round. The sentence is false as written.
  - `docs/compiler/proof-engine.md:110` — the exactness guarantee holds for units *"representable in the design's `UcumExactFactor` + `decimal` model"*. A 24-place floor is narrower than that model, whose scale runs to 28, and it discards values the model can represent. `yg` is a supported linear unit and `1e-27` is representable in `decimal`.
- **It does NOT contradict the exact-rational-scale decision** (D12, `business-domain-types.md:1786`). The factor genuinely stays a `UcumExactFactor` through composition and collapses to `decimal` only at application, which is what that decision licenses. The defect is the extra truncation *after* that collapse.
- **Provenance — it was never ruled.** The constant entered in commit `5797337c`, titled *"test: Slice 37 — affine matrix tests passing"*, whose only other change is a test edit, and was replicated by `c2dcf685`, *"fix: trim affine interval precision noise"*. Neither touched a doc. The number 24 appears in no canonical doc, design doc, or research file as a precision policy.
- **Why it was added, which matters for the fix**: the affine path (`(value + offset) * scale`, for `Cel` and `[degF]`) generates trailing artifacts that broke round-trip tests. So deleting it outright is not free. The honest fix is scoped to the affine round-trip rather than a global truncation of every linear conversion — but narrowing it changes a shipped proof guarantee's wording, so the scoping is a design call.
- **Scope / class**: over-accept in the prover's view — any magnitude below `1e-24` base units becomes exactly zero to the proof engine — and a false rejection in the observed `yg` case. It also bypasses the sanctioned channel for marking an approximate conversion, which is catalog-declared per unit and surfaced (`catalog-system.md:1203`), so the approximation is invisible where the design intends it to be visible.
- **Status**: Active — contradicts locked spec.

### BUG-048: A constraint modifier on a COMPUTED field is enforced by nothing, and is then consumed as a premise (SOUNDNESS hole)

- **Discovered**: 2026-07-23, round-2 attack on the divisor validity argument. **Behaviourally confirmed via `precept_compile` at HEAD.**
- **Symptom**: the whole file, and it compiles.

  ```precept
  precept ComputedNonzero

  field A as decimal default 1
  field B as decimal default 1
  field D as decimal nonzero <- A - B
  field E as decimal <- 100 / D
  ```

  → `success: true`, no errors, `Divisor must be non-zero` **Proved** by `DeclarationAttribute`. `A` and `B` have no write sites, so `D` is `0` in the only configuration that exists, and `E` divides by it.
- **Root cause**: the obligation list contains **no** establishing obligation for `D`. A stored field declared `nonzero` with a default always produces `Default value of 'D' must satisfy 'nonzero'` — verified repeatedly today — but a computed field produces nothing. So the modifier is declared, never checked, and then read back as a premise by the divisor proof.
- **Why the usual justification does not reach it**: every argument that consumes a declared modifier grounds it in governance at ingress — `precept-language-spec.md:268`, *"Every declared constraint is enforced on every value entering the entity from outside the definition — event arguments, construction inputs, direct field edits — at the moment it enters"*. A computed field's value never enters from outside; it is derived. Ingress governance has no purchase on it, and nothing else checks it.
- **Wider than the divisor case**: this is a modifier on a computed field being unenforced in general. Any proof consuming any modifier on any `<-` field rests on nothing. The divisor is simply the shape that makes it visible.
- **Also collapses a scope boundary**: a `decimal nonzero <- M1 / M2` over two `money` fields gives a covered-lane divisor whose arithmetic happened entirely on an uncovered lane, so restricting a rule to the primitive numeric lanes does not restrict what can reach it.
- **Scope / class**: SOUNDNESS, over-accept, on the headline fault.
- **Fix complexity**: medium — a computed field's declared constraints need an establishment obligation over its expression, which is the constraint-establishment machinery currently unlocked and being redesigned. Until then no proof may consume a modifier on a computed field.
- **Status**: Active — soundness hole.

### BUG-047: A `period` divisor slips past the single-basis rule canon already settled, and its non-zero check reads only the leading component (SOUNDNESS hole, mostly drift against settled canon)

- **Discovered**: 2026-07-23, adversarial attack on the divisor validity argument, on a lane the draft never mentioned. **Behaviourally confirmed via `precept_compile` at HEAD — zero diagnostics.**
- **Symptom**:

  ```precept
  precept PeriodDivisor

  field Amount as money in 'USD' default '100 USD' editable
  field Gap as period nonzero default '1 days + -24 hours' editable
  field Rate as price in 'USD' optional

  event Compute()

  on Compute
      -> set Rate = Amount / Gap
  ```

  → `success: true`, **no diagnostics at all**, `Divisor must be non-zero` **Proved** by `DeclarationAttribute`, and `Default value of 'Gap' must satisfy 'nonzero'` also `Proved`. `'1 days + -24 hours'` has an elapsed length of exactly zero.
- **Re-framed 2026-07-23 after a canon search. Most of this is drift against settled decisions, not an open design question.** The first version treated the whole thing as needing a ruling. Four of the five sub-questions are already answered:
  - **`nonzero` on a `period` is canon, and its structural meaning is the ruled meaning.** `temporal-type-system.md:842` — *"`nonnegative` (value ≥ zero period), `nonzero` (value ≠ zero period) — same semantics as on numeric types, **compared against `Period.Zero`**."* Not an accident of the catalog.
  - **`period` being a divisor at all is settled YES, and removing it was explicitly rejected by the owner.** `research/language/division-by-temporal-span-prior-art.md`, Resolution 2026-05-30 — a single-basis period divisor is *"a **deliberate, owner-authorized extension** of the prior-art type boundary, not an open question"*, with the duration-only alternative *"rejected by the owner"*. Reopening that is a Tier-3 conversation.
  - **A period divisor must be single-basis, and canon already says so** — D15, `business-domain-types.md:1837`: a compound period *"does **not**… it is a compile error. … `CompoundPeriodDenominator` enforces this."* **The repro's divisor is compound** (`days + hours`), so under settled canon it should already be rejected. It is not, because the guard keys off the *declared* `in` basis (`TypeChecker.Expressions.cs:1460` tests `rightTemporalUnit.Components.Length > 1`), and an unqualified `field Gap as period nonzero` carries no `in`, so `rightTemporalUnit` is null and neither PRE0073 nor PRE0074 can fire. **That is the primary defect: an implementation gap against a settled decision.**
  - **`positive` on a `period` contradicts a locked decision.** `temporal-type-system.md:842` lists only `nonnegative` and `nonzero`; Decision #14 at `:1478` locks *"No ordering on `period` — `==` and `!=` only"*. `positive` rode in as a side effect of `ZeroBoundNumericTypes` being the shared applicability array. Mechanical correction, no design needed.
- **The mechanical cause of the clean compile**: `ProjectPeriodSign` returns the sign of the **first non-zero component** — for `'1 days + -24 hours'` it reads `Days` and never looks at the `-24 hours`. The source already documents the defect in its own summary (`TypedExpressionMagnitude.cs:22-28`): *"Mixed-SIGN periods … **misclassify** … NOT suitable for min/max bound comparison. **Period bounds need a separate design.**"*
- **What is genuinely open, and it is one thing**: what non-zero predicate a `period` divisor must satisfy. `Operations.cs:499` and `:573` attach a plain numeric `NotEquals, 0m` — a *magnitude* predicate on a type canon says has no magnitude — and no canonical doc states what non-zero-ness means for a period divisor. Note the catalog's own asymmetry: `PriceTimesPeriod` carries a temporal-dimension chain requirement, while the two divide operations carry no basis requirement at all. A related deferral exists at `temporal-type-system.md:1711` (interval enforcement for duration/period, *"deferred"*).
- **Reach**: `MoneyDividePeriod` and `QuantityDividePeriod`.
- **Scope / class**: SOUNDNESS, over-accept, on the headline fault. Distinct from BUG-045 — that is finite-representation underflow through arithmetic; this fires on a bare field reference.
- **Fix complexity**: the compound-basis half is a bounded fix against settled canon (make the check see value-compound and basis-open periods, not just declared-`in` ones), and the `positive` half is a one-line applicability correction. Only the non-zero predicate needs a ruling.
- **Status**: Active — soundness hole.

### BUG-046: A divisor whose declared interval excludes zero is not proved non-zero — the file is rejected saying it "can be zero" (false rejection)

- **Discovered**: 2026-07-23, fact-checking the divisor validity argument. **Behaviourally confirmed via `precept_compile` at HEAD.**
- **Symptom**:

  ```precept
  precept BoundedDivisor

  field A as decimal min 1 max 100 default 5.0
  field Num as decimal default 100.0
  field Result as decimal default 0.0

  event Compute()

  on Compute
      -> set Result = Num / A
  ```

  → **rejected**, `PRE0083` — *"Division is unsafe: 'A' can be zero in event handler 'Compute'"* — with the divisor obligation `Unresolved`. Every value `A` can hold is at least 1, and the compiler proved both bound obligations on the same field in the same run. The message asserts something the declaration forbids.
- **Scope / class**: false rejection, so the safe direction, but the diagnostic states a falsehood about the author's own declaration, which is worse than a bare refusal. `max -1` on the negative side behaves the same way.
- **Relationship to BUG-045**: these are mirror images on the same obligation. The engine composes non-zero-ness through multiplication where it must not, and fails to read it off a declared interval where it could. Both point at the same missing piece — the divisor check reasons over sign and hole predicates rather than over the magnitude interval it already computes elsewhere.
- **Fix complexity**: small-to-medium — consult the divisor's declared interval and discharge when the interval excludes zero. The interval is already computed for containment obligations on the same field.
- **Status**: Active — false rejection with a misleading message.

### BUG-045: A divisor built by multiplying two `nonzero` operands is proved non-zero, but the product can underflow to exactly zero — on the exact lane as well as the approximate one (SOUNDNESS hole)

- **Discovered**: 2026-07-23, probing the boundaries of the divisor precondition before drafting its validity argument. **Behaviourally confirmed via `precept_compile` at HEAD, on both lanes.**
- **Symptom**: the divisor obligation reports `Proved` by `CompositionalConstraint` for a divisor that is a product of two `nonzero` fields.

  ```precept
  precept UnderflowDecimal

  field A as decimal nonzero default 0.0000000000000000000000000001
  field B as decimal nonzero default 0.0000000000000000000000000001
  field Num as decimal default 100.0
  field Result as decimal default 0.0

  event Compute()

  on Compute
      -> set Result = Num / (A * B)
  ```

  → `success: true`, `Divisor must be non-zero` **Proved**. Both defaults are `1e-28`, accepted as satisfying `nonzero`. Their product is `1e-56`, below `decimal`'s smallest representable magnitude, so it rounds to **exactly zero** and the division faults.

  The `number` lane needs a different witness, and the first version of this entry got it wrong (corrected 2026-07-23 by fact-check). A `1e-200` *default* is rejected — the literal is decimal-folded to zero before it reaches the IEEE lane (`PRE0079`) — and the `1e-28` defaults above do not underflow as doubles. Arguments carry the case cleanly instead, compiling with **zero diagnostics**:

  ```precept
  precept UnderflowNumberArgs

  field Result as number default 0.0

  event Compute(A as number nonzero, B as number nonzero, Num as number)

  on Compute
      -> set Result = Compute.Num / (Compute.A * Compute.B)
  ```

  → `success: true`, no diagnostics at all, `Divisor must be non-zero` **Proved** by `CompositionalConstraint`. Governance admits `1e-200` for a `nonzero` argument, and `1e-200 * 1e-200` underflows to `0.0`.

- **The two lanes fail by different mechanisms**, which the first version of this entry also got backwards. `decimal` has no exponent range — it is a 96-bit integer with a scale of 0 to 28 — so the loss is rounding at maximum scale (`1e-14 * 1e-15` is exactly `1e-29`, one digit past the scale, and rounds to zero). `double` genuinely underflows its exponent range. The conclusion is unchanged and is what matters: exactness of `decimal` arithmetic does not rescue the composition, because the failure is not a rounding-versus-exact question at all.
- **Why this is worse than the recorded open question**: the matrix's 2026-07-21 approximate-lane ruling already names this shape — *"Two nonzero finite `number` operands can multiply to exactly `0.0` … `nonzero` on every operand does not give a nonzero product. Recorded here as open, unrouted, and owed a decision."* Two corrections. First, it is not an open question about what the definition should say; the shipped engine **actively proves** the divisor safe today. Second, it is **not confined to the `number` lane** — `decimal` underflows to zero as well, so the exact lane is affected, and the exactness of decimal arithmetic (which several validity arguments lean on) does not save it.
- **Scope / class**: SOUNDNESS, over-accept, on the headline fault the language names first — division by zero.
- **Related**: this is the boundary that any divisor validity argument has to be written around. The defensible rule licenses a divisor whose non-zero-ness is established **directly** — a modifier on the divisor's own field or argument, a guard on the divisor expression, a non-zero literal — and refuses one derived through arithmetic over `nonzero` operands. Multiplication does not preserve non-zero-ness in a finite representation, on either lane.
- **Fix complexity**: medium — the compositional path must stop concluding non-zero from the operands' non-zero-ness for multiplication and division, or must additionally establish that the product cannot underflow, which needs magnitude bounds rather than a sign or hole predicate.
- **Status**: Active — soundness hole.

### BUG-044: The `Dimension` requirement accepts `PeriodDimension.Any` as satisfying any required dimension — the unsound direction (SOUNDNESS hole)

- **Discovered**: 2026-07-23, adversarial review of the qualifier-compatibility validity argument. Source-verified; behavioural repro not yet constructed.
- **Symptom**: `ProofEngine.Strategies.cs:99` — `return dimension == PeriodDimension.Any || dimension == dimReq.RequiredDimension;`. An operand whose temporal dimension is `Any` satisfies *every* dimension requirement.
- **Why that is the wrong direction**: `Any` means the operand could carry any temporal dimension, so it establishes nothing about which one it carries. The sibling path takes the opposite and correct view — `QualifiersAreCompatible` (`ProofEngine.Qualifiers.cs:126-133`) explicitly refuses `Any` on the `TemporalDimension` axis. Two paths, same question, opposite answers, and the accepting one is the one that can license a mismatch.
- **Scope / class**: SOUNDNESS, over-accept. Reach is limited to the `Dimension` requirement kind, which is now in the type-requirement family rather than the fault family.
- **Fix complexity**: small — refuse `Any` and let the obligation stay unresolved, matching the qualifier path.
- **Status**: Active — source-derived, repro owed.

### BUG-043: A large UCUM exponent silently wraps the dimension vector, and a large negative one crashes the compiler

- **Discovered**: 2026-07-23, adversarial review of the dimensional-product validity argument.
- **Symptom, two halves.** *Wraparound*: `DimensionVector.Multiply` is unchecked `int` addition (`Ucum/DimensionVector.cs:16-23`) and `UcumParser` parses exponents with `int.TryParse` and no range bound (`UcumParser.cs:89`). A unit built from exponents summing past `int.MaxValue` wraps, so a product whose true dimension is `L^4294967298` is accepted as area. *Crash*: `quantity in 'm^-2147483648'` throws `ArgumentOutOfRangeException` out of `UcumExactFactor.Pow` — the compiler falls over rather than emitting a diagnostic.
- **Scope / class**: the wraparound is a soundness hole reachable only through absurd unit strings; the crash is a robustness defect on malformed input, which matters more because the language server runs the same parser on every keystroke.
- **Fix complexity**: small — bound the exponent at parse time and emit a diagnostic; use checked arithmetic in the vector operations.
- **Status**: Active.

### BUG-042: A log-by uniqueness guard is not framed against writes on the route, and `contains` on a log-by tests values rather than keys (SOUNDNESS hole)

- **Discovered**: 2026-07-23, adversarial review of the key-presence validity argument. **Both halves behaviourally confirmed via `precept_compile` at HEAD.** Distinct from BUG-038, which is about the guard naming a *different* key; here the key is identical throughout.
- **Symptom A — no frame-and-kill on the collection.** Two appends of the same key in one operation, each under its own membership guard, both discharge:

  ```precept
  precept GuardedHookAppend

  field Ledger as log of string by integer
  field Fixed as integer default 7

  event Record(Payload as string)

  state Active initial
  state Done terminal

  from Active when not (Ledger contains Fixed)
      -> append Ledger "exit" by Fixed

  from Active on Record when not (Ledger contains Fixed)
      -> append Ledger Record.Payload by Fixed
      -> transition Done
  ```

  → `success: true`; **both** `KeyPresence` obligations `Proved` by `GuardInPath`. The phase-3 exit action appends key 7; the phase-5 row action appends it again. The same shape reproduces with two appends in a single chain.
- **Symptom B — the guard may not be testing keys at all.** On a log-by, `contains` is overloaded: value membership for the element type, key membership for the key type, disambiguated by operand type with the element branch tried first (`TypeChecker.Expressions.cs:874-877`, `:833-834`), and the choice is not recorded — both collapse to one `OperationKind.CollectionContains`. So for `log of integer by integer`, `when not (Ledger contains Record.Key)` asks whether the key value appears among the *payloads*. Compiles clean, obligation `Proved`, duplicate key admitted. Widening applies too: `log of decimal by integer` with an `integer` key also lands on the element branch.
- **Scope / class**: SOUNDNESS, over-accept, on the collection-uniqueness guarantee.
- **Fix complexity**: medium for A (apply the frame-and-kill the numeric strategies carry, with the collection in the dependency set); medium-to-large for B, which needs the key/value axis recorded in the typed tree so a proof can tell which membership a guard established.
- **Status**: Active — soundness hole, two independent halves.

### BUG-041: A quantity product checks the dimension and discards the unit scale — a silently wrong magnitude (SOUNDNESS hole)

- **Discovered**: 2026-07-23, adversarial review of the dimensional-product validity argument. **Behaviourally confirmed via `precept_compile` at HEAD.**
- **Symptom**:

  ```precept
  precept ScaleGap

  field Speed as quantity in 'km/h' default '10 km/h' positive
  field Elapsed as quantity in 's' default '2 s' positive
  field Distance as quantity in 'km' <- Speed * Elapsed

  state Open initial terminal
  ```

  → `success: true`, `DimensionalProduct` **Proved**. `10 km/h × 2 s` is about `0.0056 km`; the compiler admits the product into a field declared `quantity in 'km'` and labels it `km`. Wrong by 3600×. The same shape on the mass axis (`5 kg/km × 3 mm` into `quantity in 'kg'`) is wrong by 10⁶.
- **Root cause**: `TryResolveDimensionVector` reads `parsed.Unit.Vector` (`ProofEngine.Qualifiers.cs:92`) and discards the `Scale` the parsed unit carries (`Ucum/UcumParsedUnit.cs:7`). The result's unit label is then produced by the compound-cancellation policy, whose matcher compares dimension *names* and returns the numerator code verbatim, unconverted (`TypeChecker.Expressions.TypedConstants.cs:23`, `:30`).
- **Scope / class**: SOUNDNESS — not a fault in the runtime-trap sense, but a clean compile producing a numerically wrong answer, which is the guarantee Precept sells.
- **Fix complexity**: medium — compose the scale alongside the dimension and either apply the conversion or refuse the product when the scales do not cancel.
- **Status**: Active — soundness hole.

### BUG-040: Quantities with different units on the same dimension are proved compatible for addition — including counting units canon says must be rejected (SOUNDNESS hole)

- **Discovered**: 2026-07-23, adversarial review of the qualifier-compatibility validity argument. **Behaviourally confirmed via `precept_compile` at HEAD.**
- **Symptom**: both additions below report `QualifierCompatibility` **Proved** and the file compiles with no error.

  ```precept
  precept UnitMix

  field Kilos as quantity in 'kg' default '0 kg' nonnegative
  field Grams as quantity in 'g' default '0 g' nonnegative
  field MassTotal as quantity in 'kg' default '0 kg'

  field Boxes as quantity in 'box' default '0 box' nonnegative
  field Eaches as quantity in 'each' default '0 each' nonnegative
  field CountTotal as quantity in 'box' default '0 box'

  event Recalc()

  on Recalc
      -> set MassTotal = Kilos + Grams
      -> set CountTotal = Boxes + Eaches
  ```

  `kg + g` is a silent 1000× magnitude error. `box + each` is worse than wrong — canon requires it to be rejected: `business-domain-types.md:397` — *"Cross-counting-unit operations — including addition (`qty_each + qty_box`) … fail with PRE0137 `CrossCountingUnitOperation`."* No `PRE0137` is emitted.
- **Root cause**: `QualifiersAreCompatible` falls back from token equality to **dimension-name** equality on the `Unit` and `Dimension` axes (`ProofEngine.Qualifiers.cs:141-160`). Every counting unit resolves to the shared `count` dimension (`UnitDimensionHelper.cs:63-64`, with `each`/`box`/`case` in that set), and `kg`/`g` both resolve to `mass`.
- **Related**: the same fallback makes `quantity of 'mass'` compatible with `quantity in 'kg'`, though `of` pins only a dimension family and admits any unit in it.
- **Scope / class**: SOUNDNESS, over-accept, on the qualifier axis for every non-currency comparison.
- **Fix complexity**: medium — the fallback needs to be removed or restricted to cases where a conversion is applied; the currency axis is unaffected and works correctly.
- **Status**: Active — soundness hole.

### BUG-039: An interpolated value escapes its string's declared `maxlength` — the length proof widens a numeric hole to its integer digit width (SOUNDNESS hole)

- **Discovered**: 2026-07-23, adversarial review of the string-length validity argument. **Behaviourally confirmed via `precept_compile` at HEAD.**
- **Symptom**: compiles with **zero diagnostics**, both length obligations `Proved`.

  ```precept
  precept LengthHole

  field Score as number min 0 max 9 default 0 editable
  field Ratio as decimal min 0 max 9 maxplaces 4 default 0 editable
  field A as string maxlength 1 default "x"
  field B as string maxlength 1 default "x"

  event Fire()

  on Fire
      -> set A = "{Score}"
      -> set B = "{Ratio}"
  ```

  `Score` is the approximate lane bounded `[0, 9]`; a governance-admissible `0.30000000000000004` renders to 19 characters against a cap of 1 the compiler proved. `Ratio` at `2.5001` renders to 6 against the same cap.
- **Root cause**: `DecimalDigitWidth` (`ProofEngine.Lengths.cs:191-194`) refuses only when an interval's **endpoints** are non-integral — `if (decimal.Truncate(min) != min || decimal.Truncate(max) != max) return null;`. An interval with integral endpoints admits non-integral members, so `[0, 9]` is treated as one digit wide.
- **Wider than the fractional case**: the rendered width of a business-domain hole is taken over the unit-*normalized* magnitude while the render carries the declared unit, and no allowance is made for a unit or currency suffix, a sign, or a decimal point. `quantity in 'g' min '1000 g' max '9000 g'` interpolated into `maxlength 1` also proves.
- **Underlying gap — corrected 2026-07-23 after a proper canon search.** The first version of this entry said canon specifies no rendering at all beyond *"Any scalar type is coercible to string"* (`precept-language-spec.md:1553`). That was wrong: `docs/compiler/literal-system.md:531-550` carries a live **String Coercion Table** covering thirteen scalar types, and `:520` settles culture — *"String coercion is deterministic and invariant-culture. `{Amount}` always produces `"1234.56"`, never `"1.234,56"`."* Temporal rendering is specified, culture is specified, and `decimal`/`number` are specified in kind. The accurate residue is narrower and is what this bug rests on:
  - **Precision is not pinned.** "Invariant numeric string" does not say how many decimal places. For `number` nothing says whether it is shortest-round-trippable (which is what produces the 19-character `0.30000000000000004`) or fixed. For `decimal`, .NET preserves scale, and canon never says whether the render uses the value's scale, a normalized scale, or the declared `maxplaces` — which is declarable on all four magnitude types and would pin the width if the render honoured it.
  - **The seven business-domain types and `choice` are absent from the table entirely.** There is an adjacent serialization contract (`business-domain-types.md:530` — `"100 USD"`) and a `FormatString` delegate in the runtime API, but nothing links either to interpolation, so whether `{Amount}` on a `money` renders `100 USD` or `100` is strongly implied and nowhere stated.
  - **Which unit a `quantity` hole renders in is unstated.** The evaluator normalizes to UCUM base units on intake; de-normalization is specified for JSON and CLR egress but interpolation is not on that list.
  - **The `duration`/`period` format is self-contradictory across three live docs** — "NodaTime round-trip format" vs `"72:00:00"` vs `"PT72H"`. Eight characters versus five is a real width difference.
- **Scope / class**: SOUNDNESS, over-accept, on a `[StaticallyPreventable]` fault code.
- **Fix complexity**: medium — a numeric hole must widen to unbounded unless its type, its value bounds *and* a declared precision bound jointly pin the width in the operand's own declared unit; and the rendering contract needs stating in canon before any of that is sound.
- **Status**: Active — soundness hole.

### BUG-038: A log-by uniqueness obligation is discharged by a membership guard on the WRONG key — the key operand is never compared (SOUNDNESS hole)

- **Discovered**: 2026-07-23, while writing the key-presence validity argument. **Behaviourally confirmed via `precept_compile` at HEAD**; root cause line-verified in source.
- **Symptom**: a guard testing whether the log contains one key discharges the uniqueness obligation for an append using a *different* key.

  ```precept
  precept KeyMismatch

  field AuditLog as log of string by instant
  field OtherLog as log of string by instant

  event Record(Stamp as instant, Other as instant, Text as string)

  on Record when not (AuditLog contains Record.Other)
      -> append AuditLog Record.Text by Record.Stamp
  ```

  → `success: true`, `KeyPresence` **Proved**, strategy `GuardInPath`. The guard says nothing about `Record.Stamp`, so a duplicate ordering key can be appended to a collection whose uniqueness the compiler just claimed to prove.
- **Root cause**, both halves line-verified: `WalkForContains` (`ProofEngine.Strategies.cs:1295-1327`) matches on collection identity alone — `bin.Left is TypedFieldRef fr && fr.FieldName == fieldName` — and never looks at the `contains` right-hand operand. And there is nothing to compare it against: `KeyPresenceProofRequirement` (`ProofRequirement.cs:243-247`) carries `Subject`, `RequireAbsence` and `Description` only, with **no key payload at all**. The engine establishes the fact "the guard has a membership check on collection F", which is strictly weaker than the fact the obligation needs, "the guard has a membership check on collection F for key K".
- **Scope / class**: **SOUNDNESS** — the over-accept direction. Narrow in reach (log-by collections with an `append … by` under a membership guard) but unambiguous where it applies.
- **Related**: the certificate-format design already flagged this as a candidate fail-open routed to a systematic sweep, with the end-state contract stated there — the discharge step "carries the key comparison, or the verdict is not `Proven`". This bug is that flag, now with a compiled witness and its own entry.
- **Fix complexity**: medium. The requirement needs a key payload, and the guard walk needs to compare the `contains` right operand against it under the same normalization the other guard strategies use. The definitional side is written — the matrix's key-presence argument states the rule at the correct strength and records that the shipped engine does not meet it.
- **Priority**: soundness. Narrower blast radius than BUG-035, same class.
- **Repro**: as above.
- **Status**: Active — soundness hole.

### BUG-037: An index-bounds obligation is discharged only from a guard, so it rejects a provably-safe access and then suggests a repair the author cannot write at that site (false rejection + unusable diagnostic)

- **Discovered**: 2026-07-23, while authoring the index-bounds discharge rule for the matrix. **Behaviourally confirmed via `precept_compile` at HEAD**, both the failing and the passing case.
- **Symptom, two halves.** *False rejection*: an index access whose safety is decided by the declared bounds alone is rejected. `field Items as list of decimal mincount 10 maxcount 20` + `field Cursor as integer default 0 nonnegative max 5` + `field Selected as decimal <- Items.at(Cursor)` compiles to `IndexBounds` **Unresolved** and `PRE0100`, even though `Cursor <= 5 < 10 <= Items.count` follows from the two declarations, and even though the same file's own "List must be non-empty" obligation reports `Proved` from that `mincount`. *Unusable repair*: `PRE0100` says "add `when Cursor >= 0 and Cursor < Items.count`", but the site is a computed-field expression, which has no guard position. The same is true at a rule condition and at a state entry/exit action. Three of the five occasions where the obligation is minted cannot host the repair the message names.
- **Root cause**: `TryIndexBoundsProof` (`ProofEngine.Strategies.cs:390`) sources both bounds from guard facts only. Its own decomposition is already correct — the comment describes establishing both bounds per disjunctive branch — but neither half can be sourced from a declared modifier, so a declared index bound and a declared `mincount` contribute nothing.
- **Confirming control**: the identical access inside a guarded handler — `on Pick when Cursor >= 0 and Cursor < Items.count -> set Selected = Items.at(Cursor)` — reports `IndexBounds` **Proved**, strategy `GuardInPath`. So the mechanism works; only its premise sources are too narrow.
- **Scope / class**: false rejection (the safe direction, so not a soundness hole) plus a diagnostic that names an impossible repair. The second half is the more damaging in practice — an author at a computed field is told to do something the grammar does not allow.
- **Fix complexity**: medium. The definitional half is written — the matrix's index-bounds decomposition rule licenses each half from declared bounds, with the count lower bound read from the sequenced count interval the engine already tracks. The implementation extends the strategy's premise sources; the diagnostic needs per-occasion repair text.
- **Priority**: quality bar for the false rejection; the misleading repair is worth fixing whenever the message is next touched.
- **Repro**: as above.
- **Status**: Active — false rejection, definitional rule now written.

### BUG-036: A dynamic (interpolated) TEMPORAL qualifier (`period of '{Dim}'`, `period in '{Unit}'`) builds no qualifier meta and is dropped silently — no enforcement, no diagnostic (coverage/soundness-adjacent)

- **Discovered**: 2026-07-21, while re-attacking the type-structural context validity argument. **Source-derived (code reading), symptom repro pending** — an attempt to reproduce hit `choice`-declaration syntax errors, so the clean witness is not yet confirmed via `precept_compile`; the code path below is the evidence.
- **Root cause** (reported from source): the interpolated-qualifier meta builder `MapInterpolatedQualifier` (`TypeChecker.cs:389-398`) has arms for the Currency and Unit axes but **none for `TemporalDimension` / `TemporalUnit`**. A `period of '{Dim}'` / `period in '{Unit}'` parses (`Parser.Types.cs:346-354` accepts a `TypedConstantStart` for any catalog slot; period's `QS_TemporalUnitOrDimension` carries both slots, `Types.cs:34-38`), the switch returns `null`, and the null is dropped with no diagnostic (`TypeChecker.cs:313-320`).
- **Symptom (predicted, unconfirmed)**: a declared dynamic temporal qualifier neither mints a compatibility/dimension obligation nor raises a diagnostic — the qualifier silently vanishes, so a temporal-dimension mismatch over such fields would not be caught. Same *family* as BUG-035 (dynamic qualifier mishandled) but a different failure mode: BUG-035 *over-proves* a dynamic currency/unit fact; this one *never creates* the temporal fact at all.
- **Scope / class**: coverage gap with a soundness edge — an obligation that should exist is never minted, silently. Confirm the direction (silent-accept vs. type-error) once a clean repro lands.
- **Fix complexity**: small — add the `TemporalDimension` / `TemporalUnit` arms to `MapInterpolatedQualifier`, and emit a diagnostic (not a silent null) for any interpolated-qualifier axis the builder does not handle, so a future missing arm fails loudly.
- **Priority**: quality bar (pending confirmation it is a live silent-accept rather than caught elsewhere).
- **Repro**: pending correct `choice`/dynamic-temporal syntax.
- **Status**: Active — source-derived, repro pending.

### BUG-035: Dynamic (interpolated) qualifier compatibility is proved by symbolic equality with no frame check on the source field — a false `Proved` when the source field is written mid-handler (SOUNDNESS hole)

- **Discovered**: 2026-07-21, while red-teaming the type-structural context validity argument; **symptom behaviorally confirmed via `precept_compile`** (compile-clean, obligation `Proved`), root cause reported from source reading, not line-verified here.
- **Scope**: qualifiers whose value is interpolated from a runtime field — `money in '{Curr}'`, `quantity in '{Unit}'` (the Tier-2 "Dynamic — requires narrowing" class, `business-domain-types.md` § interpolated qualifiers). Two operands both carrying `SourceFieldName = "Curr"` are accepted as currency-compatible by *symbolic* equality ("both are `{Curr}`"), independent of `Curr`'s value.
- **Wider than the bare-identifier form (confirmed via `precept_compile`)**: the hole is not limited to holes that resolve to a clean source-field name. `ExtractSourceFieldName` (`TypeChecker.cs:406-434`) returns `null` — with no diagnostic — for any hole that is not a bare identifier or root member access (grouped `'{(Curr)}'`, unary, multi-hole, text-bearing), while the qualifier meta is still built as a dynamic brace-template (`TypeChecker.cs:388-397`). `money in '{(Curr)}'` in the repro above compiles clean, `QualifierCompatibility` **Proved**, currency-carrying write on the route — the same mixed-currency addition through a second dynamic spelling. So both the compatibility proof (this bug) and any fix that keys on `SourceFieldName` being populated must treat *every* dynamic brace-template as a value-fact, not only the ones whose source field extracts.
- **Narrowing / resolved discharge paths miss the source-field kill entirely (PLAUSIBLE, source-derived while writing the transport validity argument)**: even where staleness *is* checked, it checks the wrong field. `NarrowedValueFromGuard` keys its `ReassignedBefore` check to the **subject operand's field only** (`ProofEngine.QualifierNarrowing.cs:136/:142/:158`), so a reassignment of an *interpolation source* field (the `SourceFieldName`, distinct from the subject) escapes the kill. Worse, the resolved/declared discharge paths — `TryAssignmentQualifierProof` (`:46-49`) and `TryQualifierAxisNarrowingProof` (`:104-116`) — take `ExtractComparableValue(declared)` with **no `ReassignedBefore` consultation for either operand**, and interpolated qualifiers resolve down this declared path, so their source-field reassignment is checked by nothing. Reachable to a stale `Proved` only when the source field is itself open/narrowed (else the interpolated qualifier is transitively immutable) — hence PLAUSIBLE, pending a witness program. The definition-side fix is the transport rule's `deps(φ) = mention-set ∪ provdeps(φ)` closure (provdeps closes over the qualifier's provenance source fields); the compiler under-implements that closure, symmetric to the BUG-033 write-surface residue.
- **Symptom / confirmation**: writing the source field between an operand's provenance and the compatibility site is not framed out, so the stale "same `{Curr}`" fact still discharges:

  ```precept
  precept CurrencyLedger2
  field Curr as currency default 'USD'
  field Balance as money in '{Curr}' default '0 {Curr}' nonnegative
  event Rebase(NewCurr as currency, Deposit as money in '{Curr}')
  on Rebase
      -> set Curr = Rebase.NewCurr
      -> set Balance = Balance + Rebase.Deposit
  ```

  → `success: true`, `QualifierCompatibility` **Proved** (strategy `QualifierCompatibility`). `Deposit` entered under the old `Curr`; `set Curr = NewCurr` relabels `Balance` to the new currency; `Balance + Deposit` then adds values whose currencies have been decoupled, yet is proved compatible. Same failure shape as the guard-fact transport bugs, on the qualifier axis.
- **Scope / class**: **SOUNDNESS** — the over-accept / false-clean direction. A silently mixed-currency addition.
- **Root cause** (reported, not line-verified here): the symbolic/interpolated-equality path in `ProofEngine.Qualifiers.cs` (`QualifiersAreCompatible` / `ChainQualifiersMatch`, reported ~:105-179) accepts two operands sharing a `SourceFieldName` without checking that the source field is not written on the route between each operand's provenance and the compatibility site — the qualifier axis has no analogue of the `ReassignedBefore` / frame-and-kill the numeric guard strategies use.
- **Related**: this is the interpolated-qualifier case of the 2026-07-21 structural-fact citation ruling — an interpolated qualifier is a *value-fact* (an operation can invalidate it), not a type-structural context fact; the definition treats it under the full establish/preserve citation duty, which the shipped symbolic-equality proof does not honor. The transport rule's frame-and-kill is the mechanism the fix should reuse on the qualifier axis.
- **Workaround used**: none.
- **Fix complexity**: small-to-medium — extend the qualifier compatibility proof with the same source-field frame/kill the numeric strategies carry.
- **Priority**: live false-clean on the fault guarantee (qualifier axis).
- **Repro**: as above.
- **Status**: Active — soundness hole.

### BUG-033: An action's `into` binding target is not treated as a write site — a guard fact survives a write to the field it constrains, giving a false `Proved` (SOUNDNESS hole)

- **Discovered**: 2026-07-21, while establishing whether the set of in-operation writers is closed; **behaviorally confirmed via `precept_compile`**, not grep-only.
- **Scope**: the binding actions that write a second field — `dequeue <coll> into <field>`, `pop <coll> into <field>`, `dequeueBy … into <field>`. The action's primary target is registered as a write; the `into` target is not, by either consumer.
- **Affected**: `GraphAnalyzer.FieldWriteSites.cs:118` and `ProofEngine.cs:532-534` both key on `action.FieldName` only. The binding name is carried on the typed action (`SemanticIndex.cs:335-345`, `TypedBindingAction(… string? Binding …)`) and is never read by either.
- **Symptom / confirmation**: a guard establishes a fact about a field, the `into` write replaces that field's value, and the stale fact still discharges a downstream obligation:

  ```precept
  from Idle on Step when D != 0 and Q.count > 0
      -> dequeue Q into D
      -> set R = 100.0 / D
      -> no transition
  ```

  → `success: true`, divisor obligation `Proved` with strategy `GuardInPath`. The control — identical but written as `-> dequeue Q` followed by `-> set D = 0` — correctly rejects with `PRE0083`. The only difference is the `into`.
- **Second symptom, same root cause**: the same program emits a spurious `PRE0158` "Field 'D' has no write site", because the write-site collector cannot see the `into` write either. The blind spot shows from both directions at once.
- **Scope / class**: **SOUNDNESS** — the over-accept / false-clean direction. An accepted division by zero.
- **Root cause**: the write-site enumeration is derived from the action's primary field only, so an action with two write targets registers one.
- **Related**: the same investigation found no statement anywhere that the set of in-operation writers is closed, and no test that would fail if a sixteenth action were added — `ActionKind` carries no exhaustiveness enforcement in either consumer, and `test/Precept.Tests/Language/ActionCatalogTests.cs:81-116` hand-lists the write-semantics sets. This bug is what an unenforced enumeration produces.
- **Workaround used**: none.
- **Fix complexity**: small — read the binding target in both consumers. The broader enumeration-closure question is separate and larger.
- **Priority**: live false-clean on the fault guarantee.
- **Repro**: as above.
- **Status**: Active — soundness hole.

### BUG-031: Faulting arithmetic in any `when` guard-condition collects NO proof obligation — a false `Proved` (SOUNDNESS hole)

- **Discovered**: 2026-06-07, surfaced as Open Question O-8 by the soundness-and-coverage design Workflow; **behaviorally confirmed via `precept_compile`** (not grep-only).
- **Scope** (confirmed): the hole is the **guard-condition expression introduced by `when`**, in BOTH constructs that carry one — **transition guards** (`from … on … when <cond>`) and **rule guards** (`rule <body> when <cond>`). It is NOT transition-specific. (Ensure guards and `if/then/else` conditions are the same `TypedExpression?` condition shape and should be assumed in-family pending a probe — see the family map note.) The *body* expression of a rule/ensure IS walked correctly; only the `when`-introduced condition is skipped. The `when` keyword is therefore the locus, not an incidental syntax choice — a guard must be boolean, so a fault like division only reaches a guard embedded in a comparison (`X / Y > 0`).
- **Affected**: `CollectObligations` (`ProofEngine.cs:208-269`) passes no `.Guard`/condition expression to `WalkExpression`; guard conditions are consulted only on the *discharge* side for narrowing (`ProofEngine.Intervals.cs:523-529`), never walked for obligation *emission*. Guard/condition fields are `TypedExpression?` (`SemanticIndex.cs:526,571,582,592,600,612`) that can hold faulting arithmetic.
- **Symptom / confirmation**: identical unconstrained `X / Y` (Y arg-assigned, not proven non-zero):
  - in a **rule body** (`rule X / Y > 0`) → `PRE0083` "Division is unsafe: 'Y' can be zero", obligation `Unresolved` (correctly rejected).
  - in a **transition guard** (`from Open on Go when X / Y > 0`) → `success: true`, `proofObligations: []`, **no PRE0083**.
  - in a **rule guard** (`rule Z > 0 when X / Y > 0`) → `success: true`, `proofObligations: []`, **no PRE0083**.
  - In every guard case the division is silently accepted; at runtime that guard divides by zero.
- **Scope / class**: **SOUNDNESS** — the over-accept / false-clean direction. This is the *construct-position entry-point* level of the §1.5 obligation-stamping-completeness family — upstream of the walk-recursion and discharge-arm levels.
- **Root cause**: `CollectObligations` does not enroll guard-condition expressions as obligation-walk entry points.
- **Open fork (O-8 — needs owner/grammar direction)**: either **(a)** the grammar permits faulting arithmetic in guards → the non-walking is a live hole and `CollectObligations` must walk every `.Guard` (the conservative, prevention-preserving default — an unprovable guard op is a reject, not a skip); or **(b)** guards are *intended* to be structurally restricted to non-faulting predicate shapes → that restriction must be **stated in the spec and enforced by a type-checker invariant**, not left implicit in the proof engine's omission. The proof engine cannot settle this unilaterally (it turns on a grammar/intent question: can guards fault?).
- **Workaround used**: none.
- **Fix complexity**: small (walk guards) once the O-8 fork is decided; the decision itself is design/owner-gated.
- **Priority**: blocks the soundness guarantee — a live false-clean. Anchor validating case for the soundness checker's *construct-position entry-point* coverage axis.
- **Repro**: `field X as integer` + `field Y as integer` arg-assigned in an initial event; `from Open on Go when X / Y > 0 -> transition Closed` → compiles clean (no PRE0083, zero proof obligations).
- **Status**: Active — soundness hole; disposition (walk vs restrict) gated on owner direction (O-8).

### BUG-032: Faulting arithmetic nested in a member-call argument (`coll.at(A / B)`) collects no divisor obligation — a false `Proved` (SOUNDNESS hole)

- **Discovered**: 2026-06-07, surfaced by the soundness-and-coverage design Workflow's round-3 completeness-critic; **behaviorally confirmed via `precept_compile`**.
- **Affected**: the obligation-emission walk does not recurse into every child `TypedExpression` position of every walked construct — specifically the `TypedMemberAccess` arm (`ProofEngine.cs:~319-322`) walks `ma.Object` but not `ma.Arguments` (`TypedMemberAccess.Arguments`, `SemanticIndex.cs:113`).
- **Symptom / confirmation**: in one rule, `Items.at(A / B) >= 0 and A / B >= 0` — the SAME `A / B` twice:
  - **bare** `A / B` → `PRE0083` "Division is unsafe: 'B' can be zero" + a `Numeric` "Divisor must be non-zero" obligation.
  - **inside the `.at(...)` argument** → only the accessor's own `IndexBounds` obligation (`PRE0100`); **no divisor obligation, no PRE0083**. (Verified: exactly one divisor obligation emitted, at the bare position.)
- **Scope / class**: **SOUNDNESS** — false-clean. The *child-position recursion-completeness* level of the §1.5 family: the walk reaches the argument for the accessor's own ParamSubject (index-bounds) requirement but does not recurse into the argument subexpression for nested faults (division, overflow, sqrt).
- **Root cause**: the hand-written `WalkExpression` switch recurses into a hand-picked subset of child positions per arm; member-call arguments are omitted. No default arm / no exhaustiveness over child `TypedExpression` members.
- **Workaround used**: none.
- **Fix complexity**: small (recurse into `ma.Arguments`), but the systemic fix is the coverage axis that proves `{declared child TypedExpression fields of subtype X} MINUS {positions arm X recurses into} = ∅` so no position is ever silently skipped again.
- **Priority**: blocks the soundness guarantee — a live false-clean. Anchor validating case for the soundness checker's *child-position recursion-completeness* coverage axis.
- **Repro**: `field Items as list of integer` + `field A, B as integer` + `rule Items.count > 0 and Items.at(A / B) >= 0 because "…"` → the `A / B` divisor is never obligated (only index-bounds).
- **Status**: Active — soundness hole (clear fix; systemic detector is the coverage analyzer).

### BUG-030: Cross-unit qualifier-product cancellation silently drops the UCUM factor — a false `Proved` (SOUNDNESS hole, not completeness)

- **Discovered**: 2026-06-07, surfaced by the soundness-verdict-family enumeration (two independent agents *both* rediscovered it cold) during the soundness-and-coverage architecture's Step-0.
- **Affected**: the `Unit → Dimension` projection used in `quantity × quantity` / `price × quantity` qualifier-product proof (`ProofEngine.Qualifiers.cs` dimension-axis cancellation, ~`:311`/`:357`/`:472`; documented at `docs/compiler/proof-engine.md:1790` "Known gap — cross-unit cancellation").
- **Symptom**: the projection matches operands at *dimension* granularity, so a product of **same-dimension but different-unit** operands (`'USD/kg' × quantity in 'g'`, `'USD/each' × quantity in 'box'`) cancels and is accepted, **silently dropping the UCUM conversion factor**. The result carries the wrong magnitude with no diagnostic.
- **Scope / class**: **SOUNDNESS** — this is the *over-accept / false-`Proved`* direction (a verdict the compiler reports as safe that is not), distinct from the safe under-emit completeness gaps (BUG-028). It is the canonical example the witness-checking architecture exists to catch: a `DimensionalProduct`/`QualifierCompatibility` discharge whose certificate would not survive a unit-normalization re-check.
- **Root cause**: dimension-granularity matching in the unit→dimension projection instead of unit-exact (or explicit convert) matching; the per-unit scale factor is lost at the cancellation step. The doc records the policy as genuinely undecided (reject exact-unit / auto-convert / split by unit-kind).
- **Workaround used**: none — and none should be hidden; this is a real defect to fix.
- **Fix complexity**: design-required — the *fix mechanism* is small (match at unit granularity / carry the conversion factor), but **which policy** (reject vs auto-convert vs split by unit-kind) is an undecided language-surface question (gated by the pre-design consultation; `business-domain-types.md` qualifier/unit decisions are the canonical area).
- **Priority**: blocks the soundness guarantee — a live false-`Proved`. Held additionally as a **validating test case** for the soundness checker (a known false `Proved` the witness checker MUST reject) per the soundness-and-coverage effort; tracked there as falsifier F-5.
- **Repro**: `field unitPrice as price in 'USD/kg'` + `field qty as quantity in 'g'` + a computed/assigned `unitPrice * qty` → compiles clean, but the result silently omits the kg↔g (×1000) factor.
- **Status**: Active — soundness hole. Fix policy deferred to the qualifier-unit consultation; meanwhile it is the anchor validating case for the witness-checker (it must reject this discharge).

### BUG-028: The default-satisfiability fold misses some provable violations (completeness gap — construction-handler coarseness + compound short-circuit; safe under-emit)

- **Discovered**: 2026-06-05, via the adversarial review of the BUG-027 fix (the Slice 2c-ii prerequisite).
- **Affected**: the compile-time default-satisfiability fold — both the new `ScanRulesAgainstDefaults` (global rules, BUG-027) and the pre-existing `CheckInitialStateSatisfiability` (initial-state ensures), plus the shared `ConstantFold`/`FoldValue` evaluator (`ProofEngine.Analysis.cs`).
- **Symptom** (two facets, **both the safe under-emit direction** — a *provable* default-violation is not rejected; never an over-reject):
  - **(a) Construction-handler coarseness**: when a precept has an `initial` construction event, the fold is skipped **entirely** (`if (HasConstructionHandler(semantics)) return`). A rule/ensure over a field the construction event does **not** assign — whose `default` is therefore the real initial value — is not folded, so a default that violates it is missed. The BUG-027 fix deliberately **matched the ensure path's existing behavior** here (parity, not a new regression).
  - **(b) Compound short-circuit**: `ConstantFold` returns `unknown` whenever *either* operand is unknown, so `false and <unknown>` folds to `unknown` rather than `false` — a conjunction rule that is provably false (`false AND anything = false`) is not rejected.
- **Scope**: completeness only. **Never over-rejects** (the safe direction); no invalid instance is admitted that a runtime trap would then catch (governance still enforces at every operation). Affects both the rule and ensure default-folds (shared evaluator + default-env).
- **Root cause**: (a) the coarse whole-precept construction-handler skip rather than per-field suppression of only the fields the construction chain provably assigns; (b) the evaluator returns `unknown` on any unknown operand rather than short-circuiting `false AND _ → false` / `true OR _ → true`.
- **Workaround used**: none needed — under-emit is sound; runtime governance enforces.
- **Fix complexity**: medium — (a) suppress per-field (only construction-assigned fields) and fold rules/ensures over un-assigned fields against their defaults; (b) short-circuit the boolean evaluator. Both improve the **rule and ensure** paths at once (shared machinery). Soundness-critical area → adversarial review (must stay never-over-reject).
- **Priority**: completeness / quality — **not** a soundness hole (safe under-emit). Pre-release.
- **Repro**: (a) `event Create initial` seeds `A` but not `B`; `field B as integer default 0` + `rule B >= 5` → compiles clean (should reject — `B`'s default 0 is its initial value and violates the rule); (b) `field A integer default 5` + `field B integer default 10` + `field C as integer <- A * 2` + `rule A >= B and C >= 0` → `A >= B` is false on defaults but the conjunction folds `unknown` (C unfoldable) → not rejected.
- **Status**: Active — deferred follow-on from BUG-027 (the BUG-027 fix matched the ensure path's pre-existing coarseness; this closes both).

### BUG-029: Satisfiability diagnostics render the raw `TypedExpression` AST in user-facing messages (diagnostic-quality)

- **Discovered**: 2026-06-05, via the adversarial review of the BUG-027 fix.
- **Affected**: `FormatGuardText` (`ProofEngine.Satisfiability.cs:~497`, `expr.ToString() ?? "<guard>"`), used by `UnsatisfiableRule` (PRE0159) and the other satisfiability diagnostics that name a rule/guard condition. The message renders the internal `TypedBinaryOp { ResultType = …, Span = …, ResolvedOp = … }` record graph instead of readable text like `Amount >= Floor`.
- **Symptom**: an author hitting e.g. `UnsatisfiableRule` sees the compiler's AST dump, not their rule — violates spec §0.7 (domain-expert authoring audience) and §0.1 "no opaque proof."
- **Scope**: the pre-existing `FormatGuardText` callers. The **new** `DefaultViolatesRule` (PRE0164) was already fixed in the BUG-027 slice to render readably via `DescribeExpression` (`ProofEngine.cs:986`); this bug is the **shared defect in the siblings** that was out of scope for that slice.
- **Root cause**: `FormatGuardText` uses `expr.ToString()` instead of the readable `DescribeExpression` renderer.
- **Workaround used**: none.
- **Fix complexity**: small — switch the `FormatGuardText` callers to `DescribeExpression` (the same readable renderer `DefaultViolatesRule` now uses, stripping the redundant outer paren pair). Fixes all affected satisfiability messages at once.
- **Priority**: quality / diagnostics UX — not a soundness issue. Pre-release.
- **Repro**: `field X as integer max 5` + `rule X >= 10` → the `UnsatisfiableRule` message embeds the AST dump of the rule condition instead of `X >= 10`.
- **Status**: Active — deferred follow-on from BUG-027.

### BUG-026 (design/policy question): Should a provably-unsatisfiable rule or guard block compilation (Error), or only warn? — current behavior is Warning-only, genuinely unspecified in canon

- **Discovered**: 2026-06-04, during the Slice 2c-i severity investigation (the "read the design" pass). Distinct from the BUG-024 over-prove, which is fixed — this is the *leftover policy question* about the case where a contradiction is NOT consumed by a fault-prone op.
- **Affected**: the severity of the "genuine impossibility" satisfiability diagnostics. A rule/guard the compiler proves unsatisfiable — `UnsatisfiableRule` (PRE0159), `ContradictoryRule` (PRE0155), `UnsatisfiableGuard` (PRE0082) — is emitted at `Severity.Warning` (`src/Precept/Language/Diagnostics.cs`). With only a Warning, `HasErrors=False`, so the precept **builds and produces an engine**.
- **Symptom**: a self-contradictory rule with **no** dependent fault-prone operation — e.g. `field X as integer min 0 max 5` + `rule X >= 10`, with nothing dividing-by / range-checking X — compiles and produces a usable engine, emitting only `UnsatisfiableRule/Warning`. The rule can never hold, yet the definition is accepted. (When the contradiction DOES feed a fault-prone op, BUG-024's fix already rejects it at Error; this question is about the case where it does not.)
- **The tension (why this is an open question, not an obvious defect)**:
  - **For Error**: `philosophy.md:61` lists "constraint contradictions, unsatisfiable guard combinations" among structural problems "the compiler catches … before any entity exists"; and `philosophy.md:49` / `philosophy.md:51` ground Prevention ("Business logic cannot produce an answer that violates a declared rule" and "No operation can produce a result that violates a declared rule — the invalid configuration is not reachable"), with precedent in Error-level diagnostics `UnsatisfiableInitialState` (PRE0115) and `DefaultViolatesRule` (PRE0164). A rule no value can satisfy reads as an invalid definition.
  - **For Warning (status quo)**: spec §0.6 item 6 (impossible *assignment*) explicitly says "compile-time **error**," but item 7 (contradictory *rule*) says the compiler "**reports**" it — deliberately softer, and items 7/8/9 are marked "Specification-only / Phase 5." So the spec may intend a contradictory rule to be a recoverable report, not a hard error.
  - **Git archaeology** (from the BUG-024 investigation): the Warning severity was *defaulted by analogy* to sibling satisfiability codes with **no** warn-vs-error deliberation; the only recorded reconsideration contemplated dropping to *Info*, never raising to Error. So the current Warning is drift, not a settled decision.
- **Scope**: the "genuine impossibility" group (UnsatisfiableRule, ContradictoryRule, UnsatisfiableGuard). The "dead/redundant code" group (VacuousRule, TautologicalGuard, UnreachableState, DeadEndState) is a separate question — those are arguably correctly Warnings.
- **Workaround used**: none — **not a soundness issue** (no over-prove; a contradicted field's fault-prone ops already reject via BUG-024). This is purely about the build-vs-warn disposition of an unsatisfiable rule in isolation.
- **Fix complexity**: design-required — a deliberate severity-policy decision (route to `/design`), reconciling the §0.6 item-6-vs-item-7 verb asymmetry against `philosophy.md:61`/§0.7, and adding the Error-vs-Warning convention `diagnostic-system.md` currently lacks. Blast radius if flipped to Error: ~7 test files + 2 integration `.precept` fixtures reference these codes; any sample carrying an intentional-but-unsatisfiable rule would newly fail to build.
- **Priority**: quality / design decision — not a soundness hole. Pre-release.
- **Repro**: `field X as integer min 0 max 5 default 0` + `rule X >= 10` (no field divides by / range-checks X) → compiles, `HasErrors=False`, only `UnsatisfiableRule/Warning`.
- **Status**: Active — open design/policy question (severity of provable contradictions).

### BUG-025: Relational (field-vs-field) contradictory rules are invisible to the satisfiability scan — no warning, no rule attribution (inspectability gap, NOT a soundness hole)

- **Discovered**: 2026-06-04, during the Slice 2c-i final adversarial review (the NIT) and the relational-contradiction-reject design, which names it as a deferred, non-blocking follow-on.
- **Affected**: the satisfiability scan's contradiction detection. `ExtractGuardConstraints` / `ExtractGuardLeafConstraints` (`ProofEngine.Strategies.cs`) emit a leaf constraint only for *field-op-literal* conditions; a `TypedFieldRef op TypedFieldRef` (relational) condition matches no leaf arm and is dropped before the self-unsat / contradiction judgment. So the PRE0159 (`UnsatisfiableRule`) self-unsat pre-pass and PRE0155 (`ContradictoryRule`) pair sweep are structurally **blind to field-vs-field relations**.
- **Symptom** (sound, but no author-facing diagnostic):
  - A single relational rule that contradicts a field's declared bounds and feeds a fault-prone op (`field X min 0 max 5` + `rule X >= Y` with `Y min 10` + `100 / X`) → the dependent op correctly **rejects** at Error (the contradiction-reject guard, BUG-024), but the contradicting *rule* gets **no Warning attribution** — the author sees "divisor unprovable," not "your rule contradicts the declared bounds." The equivalent *magnitude* rule (`rule X >= 10`) does emit `UnsatisfiableRule`.
  - Two jointly-contradictory relational rules on one field (`rule X >= Y` with `Y` in [10,20], and `rule X <= W` with `W` in [0,5], `X` in [0,100]) with **no** dependent fault-prone op → compiles **silently clean** (no warning), whereas the all-magnitude analogue (`rule X >= 10` + `rule X <= 5`) emits `ContradictoryRule/Warning`.
- **Scope**: any field-vs-field (relational) rule contradiction — single rule vs its field's declared bounds, or a jointly-contradictory relational rule pair. The magnitude / field-op-constant cases are already detected (they warn); this is the relational gap only.
- **Root cause**: the satisfiability scan's leaf-constraint extraction is field-op-constant only; relational conditions are discarded before the self-unsat / contradiction judgment runs. (Named as out-of-scope in `relational-contradiction-reject-2026-06-04.md` § Scope / Open Questions — "surfacing field-op-field contradictions in the satisfiability scan" — because it is not required to close the BUG-024 over-prove.)
- **Workaround used**: none needed — **soundness is intact**: BUG-024's contradiction-reject guard makes any dependent fault-prone op reject regardless of this gap. This is purely a missing author-facing diagnostic.
- **Fix complexity**: medium — extend the satisfiability leaf extraction / self-unsat pre-pass to recognize `TypedFieldRef op TypedFieldRef` conditions and compose them against both fields' declared intervals, emitting a Warning-level attribution that matches the magnitude path — WITHOUT feeding the relational narrowing into the discharge-time scans in a way that re-opens the satisfiability-isolation false-rejection hazard (the empty-intersection judgment is the sound contradiction signal; a non-empty tightening must not warn).
- **Priority**: quality / inspectability — **not** a soundness hole (no over-prove; the dependent op already rejects). Improves author diagnostics. Pre-release.
- **Repro**: the two precepts above (single relational contradiction → dependent op rejects but no rule-attributed warning; jointly-contradictory relational pair with no dependent op → silent clean, vs the magnitude analogue's `ContradictoryRule` warning).
- **Status**: Active — deferred follow-on from Slice 2c-i.

### BUG-024: A rule whose narrowing contradicts the subject's own declared bounds over-proves a dependent fault-prone op (no Error surfaced) → Principle-1 over-prove (relational arm new in Slice 2c-i; magnitude arm pre-existing)

- **Discovered**: 2026-06-04, via the adversarial `precept-reviewer` diff review of the Slice 2c-i build, reproduced against a fresh `Compiler.Compile` (severity + `HasError` probe).
- **Affected**: two arms of one root cause — a *self-unsatisfiable* numeric fact about `X` (its narrowing of `X` is empty against `X`'s own declared bounds) still folds as a discharge fact, and the contradiction is never surfaced as an **Error**:
  - **Relational arm (NEW, introduced by Slice 2c-i)** — the relational discharge folds (`TryRelationalSignForField` / `ResolveNumericSubjectSignSet` in `ProofEngine.Composition.cs`; `RelationalHalfLine` into `BuildNarrowedIntervals` in `ProofEngine.Intervals.cs`) narrow `X` from the *related* field `Y`'s interval without checking the result is consistent with `X`'s own bounds. The empty intersection (`⟦X⟧ ⊓ half-line = ∅`) is treated as "proved safe," and the relational-narrowing design's Decision-4 satisfiability isolation means the relational contradiction never reaches a satisfiability scan, so **no diagnostic at all** is emitted.
  - **Magnitude arm (PRE-EXISTING, surfaced while investigating the above)** — a self-unsatisfiable magnitude rule (`rule X >= 10` with `X max 5`) does emit `UnsatisfiableRule`, **but at Warning severity** (`HasError=False`), and its `ScopedNumericFact` still folds to discharge the dependent op — so the precept **builds and faults at runtime anyway**. The earlier assumption that the magnitude equivalent is "rejected" is false (verified below); it is a sibling over-prove that merely additionally warns.
- **Symptom** (probe-confirmed against a fresh `Compiler.Compile`, severity + `HasError`):
  - `field X as integer min 0 max 5 default 0` + `field Y as integer min 10 max 20` + `field Q as integer <- 100 / X` + `rule X >= Y` → `HasError=False`, **zero diagnostics** (relational arm).
  - same with `rule X >= 10` (no `Y`) → `HasError=False`, **only `UnsatisfiableRule/Warning`** — the precept builds, `100/0` faults at runtime (magnitude arm).
  - `rule X >= Y` is unsatisfiable given `X max 5`, yet `100/X` is proved safe; at runtime `X` holds its valid `default 0` and `100/0` faults.
- **Scope**: general — any rule (relational `X op Y` or magnitude `X op const`) whose narrowing of `X` is empty against `X`'s declared bounds. Both the sign-set divisor discharge and the interval/containment discharge are affected.
- **Root cause**: a contradictory/unsatisfiable narrowing is still consumed by the discharge readers (which treat an empty interval / contradiction-derived sign as "proved safe"), and the contradiction is not raised to an **Error** that blocks the build — relationally it is invisible (Decision-4 isolation), and for magnitude it is a non-blocking Warning. The relational-narrowing design's "strictly additive / never over-prove either direction" claim is false for the contradictory-narrowing case. An empty `⟦X⟧ ⊓ narrowing` is a *genuine* contradiction (reject-correct per Principle 10), distinct from the nonempty-but-tighter case Decision 4 guarded against.
- **Workaround used**: Slice 2c-i held back from landing until the handling is designed.
- **Fix complexity**: design-required — routed to `/design` (`docs/Working/relational-contradiction-discharge-guard-2026-06-04.md`, status `Semantics-Stated`). Open scope call: whether the magnitude-arm fix folds into this slice or is tracked separately; and the diagnostic code/severity for the surfaced contradiction.
- **Priority**: soundness — a live Principle-1/11 over-prove (false "safe" → runtime fault on a built precept). **Blocks Slice 2c-i landing.** Pre-release.
- **Repro**: the three precepts above (relational contradiction → zero diagnostics; magnitude contradiction → `UnsatisfiableRule/Warning`, still builds; both fault at runtime on `100/0`).
- **Status**: ✅ **Fixed 2026-06-04 (Slice 2c-i)** — the relational/magnitude discharge folds gained an empty-intersection contradiction guard (a shared `RelationContradictsSubjectBounds` helper + a self-unsat block in `CollectBlockedConstraints` + a containment-reader backstop): a contradicted narrowing contributes no discharge fact, so the dependent fault-prone op rejects at its existing `Severity.Error`. Both arms converge. Residual *inspectability* follow-ons split out as [[BUG-025]] (relational contradictions get no warning) and [[BUG-026]] (bare-contradiction severity policy). (Move to § Fixed in a cleanup pass.)

### BUG-023: Guarded magnitude/sign rule leaks as an unconditional numeric fact → Principle-1 over-prove

- **Discovered**: 2026-06-03, via the unbiased `precept-reviewer` pass + a `Compiler.Compile` probe, during the relational-narrowing-core (Slice 2c-i) design review (the CONCERN-1 shared blind spot two independent designs both missed).
- **Affected**: `ProofEngine.Composition.cs:144-151` — the rule→fact loop calls `TryGetNumericConstraintFact(semantics.Rules[i].Condition, …)` and **never reads `semantics.Rules[i].Guard`**, whereas the `ensure` loop beside it at `:169` DOES filter guarded ensures (`if (ensure.Guard is not null) return false`, "Guarded ensures are conditional — they must NOT become unconditional numeric facts"). `ScopedNumericFact` (`ProofEngine.cs:88-93`) has no guard field, and `FactAppliesToContext` (`Composition.cs:474-497`) returns `true` unconditionally for an anchor-free rule-derived fact (`:496`) — so the guard is dropped at fact construction and is irrecoverable downstream. The guarded rule becomes a global, context-blind fact about its subject.
- **Symptom** (probe-confirmed against a fresh `Compiler.Compile`, not the MCP): `field X as integer` + `field Flag as boolean` + `rule X >= 5 when Flag because "…"` + a bare-subject divisor `field Q as integer <- 100 / X` → compiles **clean** (no `DivisionByZero`). Controls fixing the diagnosis:
  - the UNGUARDED rule `rule X >= 5` (no `when`) → also clean — the fact legitimately reaches and discharges (confirms the fact-reach path works and the divisor reader consumes it).
  - NO rule at all → **emits `DivisionByZero`** ("Division is unsafe: 'X' can be zero").
  So the guard is being dropped: on the `Flag == false` path `X` is unconstrained (can be 0), yet the divisor is proven safe because the `when Flag` rule was folded as if unconditional.
- **Scope**: general — any guarded `rule subject OP static-numeric` routed through `TryGetNumericConstraintFact` (the whole `min`/`max`/`nonzero`/sign fact family), not just the divisor case. Confirmed leaking with `rule X > 0 when Flag` and `rule X != 0 when Flag` as well.
- **Root cause / fix shape**: mirror the ensure-path filter — skip (or guard-scope) `Rules[i].Guard is not null` in the `:144-151` loop, exactly as `TryGetNumericEnsureFact` does at `:169`. The structural sibling of the same gap is the NEW relational arm added by Slice 2c-i (its design already specifies the guarded-rule DROP); this magnitude arm is the **pre-existing** instance of the identical asymmetry. The shared-loop guard filter naturally covers both arms — applied once to the rule loop, it filters the magnitude fact and the relational fact together.
- **Workaround used**: none.
- **Fix complexity**: small — a guard-null filter on the rule arm + a regression test mirroring the probe (guarded rule → divisor still rejects) + a corpus check.
- **Priority**: soundness — a live Principle-1/11 over-prove: a guarded rule used as an unconditional fact yields a false "safe," so a compiled-clean precept can take a runtime fault that prevention is supposed to make impossible. Pre-release.
- **Repro**: the three precepts above (`rule X >= 5 when Flag` + `100 / X` leaks; unguarded `rule X >= 5` discharges legitimately; no rule emits `DivisionByZero`).
- **Fix folded into Slice 2c-i** (`relational-narrowing-core-design-2026-06-03.md`, Locked): the fix is the single `Rules[i].Guard is null` filter on the shared `Composition.cs:144-151` rule→fact loop, which closes this magnitude arm AND the slice's new relational arm together. BUG-023 is **in scope for that slice, not a separate slice** — it closes when 2c-i lands. The design carries the BUG-023 repro as a MUST-PASS acceptance (matrix row #13) and the corpus soundness-tax (§0.7) as an expected, correct tightening.
- **Status**: ✅ **Fixed 2026-06-04 (Slice 2c-i)** — the `Rules[i].Guard is null` filter on the shared `Composition.cs` rule→fact loop now drops guarded magnitude *and* relational facts; a guarded `rule X >= 5 when Flag` no longer discharges a global `100 / X` divisor. (Move to § Fixed in a cleanup pass.)

### BUG-017: Proof engine silently skips the result-bound check when the result interval is unbounded (Principle-10/11 soundness hole)

- **Discovered**: 2026-06-01 during the dynamic-modifier-bounds design investigation (probing what the proof engine actually proves relationally).
- **Affected**: `ProofEngine` interval-containment discharge for `NumericOverflow` on the **computed-field** result vs the field's declared `min`/`max` (`CollectComputedFieldBoundObligations`, `Analysis.cs:453–454` — `if (interval.IsUnbounded) continue;`). **Scope corrected 2026-06-02** (Phase-2 grounding): the **set-action** path is **sound** — it creates the obligation whenever the target field has bounds (`Actions.cs:285`) and an unbounded RHS interval discharges to `false` → reject. Earlier "computed + set-action" framing was wrong; only the computed-field collector gates on `IsUnbounded`.
- **Symptom**: a computed field that declares a bound but is computed from an *unbounded* operand compiles clean even though the result provably can exceed the bound. The check fires correctly when the operand is bounded-and-exceeds, but silently skips when the operand is unbounded — the unsound direction. Probe-confirmed against a fresh build (not the MCP):
  - `field A as integer min 0 max 20 default 0` + `field C as integer min 0 max 100 <- A * 10` → **NumericOverflow** (200 > 100). ✓ correct.
  - `field A as integer min 0 max 1000 default 0` + same `C` → **NumericOverflow** (10000 ≫ 100). ✓ correct.
  - `field A as integer min 0 default 0` (no `max` — unbounded above) + same `C` → **no diagnostic**. ✗ `A * 10` is unbounded, exceeds `C`'s `max 100` (and the type's representable range), yet compiles clean.
- **Root cause**: the IntervalContainment obligation is not emitted when `IntervalOf(result)` is unbounded — the obligation collectors skip unbounded intervals and the discharge returns "not proved, but not collected." For a **prevention** engine the direction is backwards: an unprovable result must **reject** (emit), not **skip**. "Conservative" here imported a detection-tool mindset (skip-to-avoid-false-positives) into a contract whose guarantee is the opposite — Principle 10 (*"the compiler must either prove safety **or emit a diagnostic** requiring the author to supply constraints that make safety provable"*) and Principle 11 (*"if a precept compiles without diagnostics, it does not fault at runtime"*). An unbounded operand = unprovable = must emit, asking the author to bound the operand.
- **Already mischaracterized once**: the Slice 3b design (`phase8-value-level-obligation-ownership-2026-06-01.md`) noted this exact "IntervalTransfer-unbounded → silent" behavior and called it *"acceptable-by-design conservative,"* deferring it as a coverage hole. That characterization was wrong — it is a soundness hole, not an acceptable deferral. The design's deferred-residual note should be corrected when this is fixed.
- **Workaround used**: none.
- **Fix complexity**: design-required. Flipping "unbounded → skip" to "unbounded → emit" must (a) decide the diagnostic + audience-targeted message ("cannot prove the result of `<expr>` stays within `[min .. max]`; constrain its operands"), (b) fix the computed-field collector (the set-action path is already sound — do not touch it; verify no other `IntervalTransfer`-unbounded collector gates the same way), and (c) measure corpus impact — existing samples with unbounded computed results would newly require constraints (same falsifier shape as Slice 3b D4; if >~3 samples need new constraints, the narrowing may be too weak rather than the inputs genuinely unprovable).
- **Sibling breaches (same shape — obligation creation gated on provability, found in the same Phase-2 grounding)**: [[BUG-018]] (count containment never enforced) and [[BUG-019]] (length containment skipped on non-literal RHS). All three should be fixed under one principle: *unprovable bound ⇒ emit, never skip*.
- **Priority**: quality bar / soundness — a Principle-11 violation, but on a specific shape (unbounded operands) and pre-release.
- **Repro**: the three integer `A`/`C` cases above; the middle one (`A` unbounded, no `NumericOverflow`) is the bug.
- **Status**: ✅ **Fixed 2026-06-02 (`9820b8c1`, Slice 2a)** — removed the `IsUnbounded` skip; the computed-field obligation is now created and emits when unprovable. Paired with the operand-interval flag-fold (`FlagLowerBound` in `ExtractFieldInterval`) so provably-safe computed fields still discharge. (Move to § Fixed in a cleanup pass.)

### BUG-020: A field-reference modifier bound (`min Floor`) is silently accepted, never bound, never enforced — and an undeclared reference is not caught

- **Discovered**: 2026-06-02 (precept-author probe of the relational-rules-and-bounds design's worked example; verified via `precept_compile`, MCP reconnected).
- **Affected**: the modifier-value slot for `min`/`max` (and likely the other value modifiers). The parser accepts an identifier as a bound value; the type checker/binder neither resolves nor enforces it. `TryGetComparableModifierValue` (`TypeChecker.Validation.Modifiers.cs:462-480`) returns `null` for an `IdentifierExpression`, and the null is dropped silently (`TypeChecker.cs:559-577`).
- **Symptom** (all `precept_compile success: true`, zero diagnostics):
  - `field Amount as number min Floor default 0` — clean (bound accepted but inert).
  - `field Amount as number min Nonexistent default 0` (`Nonexistent` matches no field) — clean; **no undeclared-name error** (`PRE0029` not raised). The identifier is swallowed.
  - `field Floor as number default 10` + `field Amount as number min Floor default 5` — clean, despite `5 < 10` plainly violating `Amount >= Floor`. (Contrast the literal `min 5 default 0`, which correctly emits `OutOfRange`/`PRE0079`.)
- **Root cause**: a field-reference bound is a relational constraint (§2.4 — `min Floor` ≡ `rule Amount >= Floor`), but it is neither desugared, bound, nor proof-participated; it is parsed and discarded. Two distinct gaps: (1) *no enforcement* (the soundness gap the relational-rules-and-bounds design closes), and (2) *no undeclared-name diagnostic* — even a typo'd field name in a bound is silently ignored, which is a binder defect independent of the feature.
- **Workaround used**: none.
- **Fix complexity**: gap (1) is the relational-rules-and-bounds design (`relational-rules-and-bounds-design-2026-06-02.md`). Gap (2) (undeclared-name in a bound expression) is a smaller binder fix that should land regardless — a bound referencing a non-existent field must error today, not silently pass.
- **Priority**: soundness (a declared bound that does nothing is a silent governance hole) + correctness (undeclared reference uncaught).
- **Repro**: the three cases above.
- **Status (2026-06-02)**: undeclared-name half **fixed** (`e2b7c9c1` — `NameBinder` name-resolves `min`/`max` field-reference values → `UndeclaredField`). Enforcement half (the resolved bound actually participating in proof) is the merged relational-narrowing slice.

### BUG-021: Set-action assignment does not enforce a field's lower bound (`min`) when the field has no `max` — value provably below `min` compiles clean (Principle-10/11 soundness hole)

- **Discovered**: 2026-06-02 during Slice-1 step-0 probing (resolving the set-action-subtraction obligation-coverage question; verified via `precept_compile`, MCP reconnected).
- **Affected**: `Actions.cs` interval-containment obligation generation for `set`. The obligation appears to be created on the **upper** (max / representable-range) direction but not the **lower** (min) direction when the target has a `min` but no `max`.
- **Symptom** (both `precept_compile`):
  - `field X as integer min 0 max 100` + `set X = A - B` (`A,B nonnegative`, so `A-B` unbounded) → **`PRE0078`**, `IntervalContainment` obligation `Unresolved` `[−∞..+∞]`. ✓ correct (upper direction).
  - `field X as integer nonnegative` (min 0, no max) + `set X = A - B` with `A,B ∈ [0,10]` (so `A-B ∈ [−10,10]`, provably can be `−10 < 0`) → **clean, no `IntervalContainment` obligation at all**. ✗ the lower-bound violation is silently accepted.
- **Root cause**: same family as [[BUG-017]] — the containment obligation is not *created* for the lower-bound direction on a min-only field; the asymmetry with the (sound) upper direction is the tell. This reconciles the precept-author "set-action subtraction into a nonnegative field emits no obligation" finding with the Phase-2 "set-action path is sound" finding: both are right, on different bound directions.
- **Workaround used**: none.
- **Fix complexity**: small, same shape as the slice's other breaches — generate the interval-containment obligation for the `min` direction on `set` regardless of whether a `max` is declared; unprovable ⇒ emit (`OutOfRange`/`PRE0079`).
- **Priority**: soundness — a declared `min` silently unenforced on assignment; pre-release.
- **Repro**: SubProbe1 (emits) vs SubProbe3 (clean) above.
- **Reframing owed (2026-07-21, verified via `precept_compile`)**: the axis stated above — "`min` present, `max` absent" — does not match observed behaviour and should be re-derived before this is fixed. Two probes differing in one word:
  - `field Reading as integer default 0 min 0` + `set Reading = 0 - 5` → **rejects**, `PRE0078`, `IntervalContainment` `Unresolved`, computed interval `[-5 .. -5]`, `declaredMin 0`. A `min` with no `max` *is* enforced here.
  - `field Reading as integer default 0 nonnegative` + `set Reading = 0 - 5` → **compiles clean**, one obligation (the default-value check), **none for the write**.

  So the discriminating axis appears to be the *spelling* — bound modifier (`min`/`max`) versus qualifier modifier (`nonzero`/`positive`/`nonnegative`) — not the presence of a companion bound. The original probes used a non-literal RHS (`A - B`) and a `nonnegative` field, which confounds the two axes. Whether these are one root cause or two is unresolved. This matters beyond containment: the proof engine consumes qualifier modifiers to discharge fault obligations (strategy `DeclarationAttribute`), so the spelling the engine trusts to prove a division safe is the one nothing enforces on write. See [[BUG-033]] for the same shape at a different write path.

### BUG-034: `PRE0078` reports a declared-bound violation as a representable-range overflow — the message tells the author something untrue (diagnostic-quality)

- **Discovered**: 2026-07-21, incidentally, in two unrelated probes.
- **Symptom**: `field Reading as integer default 0 min 0` with `set Reading = 0 - 5` emits:

  > `PRE0078: Numeric computation exceeded the representable range on field 'Reading'`

  The value is −5 and the declared minimum is 0. Nothing approached the representable range of `integer`; a declared bound was violated. The obligation record alongside it is correct and specific — `IntervalContainment`, computed interval `[-5 .. -5]`, `declaredMin 0` — so the information needed for a true message is present and is being discarded at rendering.
- **Scope**: the same code renders both genuine representable-range overflow and declared-bound violation. Only the message is wrong; the accept/reject verdict is right.
- **Why it matters more than an ordinary wording nit**: the primary author is a domain expert (`philosophy.md § Who authors a precept`). "Exceeded the representable range" points them at the type, when the fix is their own declared bound or the expression feeding it. It is a message that sends the reader to the wrong place.
- **Fix complexity**: small — the two conditions are already distinguishable at the point of emission; they need distinct messages, and probably distinct codes.
- **Priority**: diagnostic quality, not soundness.
- **Repro**: as above.
- **Status**: Active.

### BUG-022: Consumer-side `switch (catalog *Kind)` dispatch is not enforced exhaustive — a missing arm is a silent gap (catalog-discipline hole)

- **Discovered**: 2026-06-03 during the `notempty` axis-overlap work — `BuildElementValueBounds` had a `switch (modifier.Kind)` with no `notempty` arm, so a routed `notempty` silently bound no element value. Caught by adversarial review, not an analyzer.
- **Affected**: pipeline code that dispatches per-member on a catalog `*Kind` enum via a `switch`, in a class **not** decorated `[HandlesCatalogExhaustively(typeof(T))]`. `Precept0019` only enforces member coverage for *already-enrolled* classes; nothing forces a switch's class to enroll. So an unenrolled consumer switch can silently miss a member. Known instances (verified by grep 2026-06-03): `TypeChecker.Validation.Modifiers.cs:350` (`switch (modifier.Kind)` — min/max-presence → bound-qualifier validation) and `:538` (`maxplaces` value validation); candidates also at `NameBinder.cs:63` (`construct.Meta.Kind`) and `TypeChecker.Expressions.AssignmentQualifiers.cs:189` (`resolution.Kind`). (`BuildElementValueBounds` itself was the worst case and is **already fixed** — genericized off `ProofSatisfactions` in `c3209ad2`.)
- **Symptom**: adding a new catalog member (e.g. a new modifier) compiles clean even though a consumer switch silently doesn't handle it — a behavior gap with no diagnostic. This is the `*Kind`-enum-dispatch anti-pattern CLAUDE.md forbids ("behavior belongs in catalog metadata"), unenforced at build time.
- **Root cause**: `Precept0019PipelineCoverageExhaustiveness` is opt-in (it analyzes class symbols + their `[HandlesCatalogExhaustively]` attributes; it never inspects switch operations). There is no rule that a `switch` on a catalog `*Kind` enum *requires* the attribute.
- **Workaround used**: none. (The `notempty`-retarget arm-shape guard test `ModifierCatalogCapabilityTests.EveryElementRoutableModifier_MatchesAKnownElementBoundShape` covers the *element-bound binding* shape specifically, but not the general switch class.)
- **Fix complexity**: small-to-medium analyzer change + remediation. Add a rule to `Precept0019` (register on switch operations): a `switch` whose matched type is a catalog `*Kind` enum, in a class lacking `[HandlesCatalogExhaustively(typeof(ThatEnum))]`, is an error — forcing enroll-or-genericize. **Exemptions** (scope decision, owner-aligned 2026-06-03): the catalog `GetMeta(XKind)` definition switches (the metadata's home, already `*CrossRef`-checked) and lexer/parser `switch (token.Kind)` token-dispatch. Then enroll-or-genericize each flagged consumer switch. Needs a small `/design` for the exemption model before building (see the conversation 2026-06-03 for the agreed shape).
- **Priority**: quality bar / catalog-discipline — not a current soundness hole (the known switches are validation/classification, not silent value-drops like `BuildElementValueBounds` was), but the *class* of gap is the kind that already bit once. Pre-release.
- **Repro**: add a new `ModifierKind` member with no arm in `TypeChecker.Validation.Modifiers.cs:350`/`:538` → compiles clean, no diagnostic.

## Fixed

### BUG-027: Global rules are not folded against default field values — a default-violating rule compiles clean (Principle-11 gap; prerequisite for BUG-020 enforcement)

- **Discovered**: 2026-06-05, via the adversarial review of the Slice 2c-ii field-reference-bound-enforcement design; verified against a fresh `Compiler.Compile`.
- **Affected**: the compile-time check that default field values satisfy declared rules. `CheckInitialStateSatisfiability` (`ProofEngine.Analysis.cs`) folded only initial-state **ensures** against defaults; global **rules** (`semantics.Rules`) were walked for sub-expression obligations but their *truth* was never evaluated against the default environment. Spec §0.1 **Principle 11**: *"Rules and initial-state ensures are checked against default field values at compile time. A definition where default values violate a declared rule is rejected."*
- **Symptom** (probe-confirmed): `field Amount as number default 5` + `field Floor as number default 10` + `rule Amount >= Floor` → compiled **clean**, despite the default configuration (5 < 10) violating the rule.
- **Status**: ✅ **Fixed 2026-06-05** — added `ScanRulesAgainstDefaults` to the proof-engine satisfiability scan (`ProofEngine.Satisfiability.cs`), wired alongside `ScanRules`. It folds each **unguarded** global rule's condition against the field default environment using the **same** `ConstantFold`/`FoldValue` evaluator the initial-state ensure path uses, and emits the new `DefaultViolatesRule` (PRE0164, Proof/**Error**) **only** on a provably-`false` fold — an unknown/unfoldable default (computed field, non-constant default) folds to `null` and never rejects (soundness over completeness, §0.6 #1); guarded rules are skipped (they hold only under their `when`). The default-environment builder was extracted from `CheckInitialStateSatisfiability` into a shared `BuildDefaultEnvironment` helper so the ensure path and the new rule path share one environment and one evaluator — the ensure fold (PRE0115) is byte-identical. A construction (`initial`) event suppresses the rule-vs-default fold (the defaults are placeholders the construction handler overwrites), mirroring the ensure path's `HasConstructionHandler` early-return; the residual coarseness — a rule over a field the construction event does not assign — matches the ensure path's existing behavior (the safe under-emit direction; per-field precision is a future refinement). The generic repro: `field Amount as number default 5` + `field Floor as number default 10` + `rule Amount >= Floor` → `5 >= 10` folds false → `DefaultViolatesRule`. The fold surfaced genuine Principle-11 latent violations in fixtures, remediated two ways: where the defaults should satisfy the rule, the default was corrected (e.g. `RelationalNarrowingCoreTests` `rule OnHand > Reserved` with both `default 0` → `0 > 0` false → `OnHand default 1`); where the rule should hold only once an optional field has a stated value, the rule was guarded and the field's default dropped (sample `customer-profile.precept` — `PreferredContactMethod` made `optional` with no default, its reachability rules guarded `when PreferredContactMethod is set`, so a freshly created profile satisfies them vacuously). Fold confirmed sound (every reject is a provable `false`) — surfaced to the owner, not silently patched.

### BUG-018: `maxcount`/`mincount` are never enforced — count-containment proof is dead code (Principle-10/11 soundness hole)

- **Discovered**: 2026-06-02 during Phase-2 contract grounding.
- **Affected**: collection growing **and** shrinking mutations and `default [...]` literals vs a field's declared `mincount`/`maxcount`. `CountBoundViolation` (Diag 136, `[StaticallyPreventable]` Fault 15) was **dead code** — no obligation generator constructed a count-containment requirement, and the prover `TryCountContainmentProof` was a `=> null` stub.
- **Symptom**: a mutation that provably exceeded (or underflowed) a declared count bound compiled clean. Probe-confirmed (full pipeline): `field C as set of integer maxcount 1` + two `add C` actions growing it past 1 → no diagnostic; symmetrically a `remove`/`clear` dropping a fully-determined collection below `mincount` emitted nothing.
- **Root cause**: same shape as [[BUG-017]] — the obligation was never *created*, so the bound was never enforced. The first partial fix wired only the grow/maxcount direction (plus `default [...]`), leaving the shrink/mincount direction as a remaining under-reject hole.
- **Workaround used**: none.
- **Status**: ✅ **Fixed** (reworked to the locked **Reading A — obligation / prove-or-reject** discharge; design `count-bound-discharge-semantics`, Locked 2026-06-03). Count-containment obligation is created in **both** directions on every mutation of a `mincount`/`maxcount` field: growing (`add`/`append`/`enqueue`/`push`/`put`/`insert` + by-variants, `Effect == Grows && WriteSemantics == EstablishesValue`), shrinking (`remove`/`removeAt`/`pop`/`dequeue`, `Effect == Shrinks`) and `clear` (`Effect == Empties`) — all derived from `ActionMeta`, no hand-list — plus a `default [...]` literal (statically-exact count). A `set` to a collection is not a literal-count site: a list-literal RHS is type-rejected (list literals are legal only in `default`), so a valid `set` is always a non-literal whose count is unknown and drops the tracked interval. A **single** count interval is tracked **sequentially** in `WalkActions` (reusing the per-effect `ActionEffectClass` classification): seeded **once** on first touch from the governed band `[mincount ?? 0, maxcount ?? ∞]`, narrowed by a `count`-comparison guard and by routed reject-row siblings (integer count domain), then advanced by each mutation's **sound per-kind/per-action delta**. The delta inputs are catalog-derived: dedup-ness from `TypeMeta.DeduplicatesElements` (set/lookup → grow leaves lower bound unchanged, possible duplicate no-op; ordered/multiset → exact `+1`), might-no-op-ness from `ActionMeta.EffectIsConditional` (remove/removeAt → upper unchanged, possible no-op; positional pop/dequeue → definite `−1` on both). `TryCountContainmentProof` discharges on the **prove-or-reject** line, identical to the length sibling: clean **iff** the post-mutation interval is provably in-band (`lo ≥ mincount` AND `hi ≤ maxcount`), otherwise — a provable violation **or** a merely-unprovable case (an unguarded grow whose `hi` is `∞`-seeded) — the obligation is unresolved and `CountBoundViolation` (PRE0136) emits, naming the guard (overflow → `when C.count < N`; underflow → `when C.count > M`, locked OQ2). No deferral to a runtime check (§0.7); the runtime trap is a defense-in-depth backstop. The disputed `CountLowerAfter`/`CountUpperAfter` requirement fields and the emit-on-provable-violation discharge are gone; the obligation carries `[CountLower, CountUpper]` and the prover is two-way. `CountBoundViolation` is off the Gate-1 (no-emission) list — it sits in Gate-2 (emitted, test-referenced in `CountContainmentEmissionTests`, alongside `LengthBoundViolation`). All `CountContainmentEmissionTests` green; corpus stays clean.

### BUG-019: Length containment skipped on non-literal RHS — obligation generated only for literal assignments (Principle-10/11 soundness hole)

- **Discovered**: 2026-06-02 during Phase-2 contract grounding.
- **Affected**: `set`-action string assignment vs a field's declared `maxlength`/`minlength`. The length-containment obligation was generated **only for literal RHS**; a non-literal RHS (concatenation, field reference) was not checked.
- **Symptom**: `field Name as string maxlength 10` + `set Name = First + Last` (whose result can exceed 10) compiled clean. The literal case (`set Name = "<11 chars>"`) correctly emitted `LengthBoundViolation`.
- **Root cause**: same shape as [[BUG-017]] / [[BUG-018]] — obligation creation was gated (here, on the RHS being a literal) rather than emitted-or-proven for every assignment.
- **Workaround used**: none.
- **Status**: ✅ **Fixed 2026-06-02** — literal gate dropped; length-containment obligation generated for any string RHS into a length-bounded field; discharged against a sound string-length interval (`StringLengthIntervalOf`: literal, field/arg-ref, concat, conditional, interpolation with bounded holes, member-access hook, length-stable functions); unprovable ⇒ emit `LengthBoundViolation` (§0.7, no deferral). 20 corpus samples updated to declare the matching `maxlength` on the source args/fields the proof requires (the §0.7 Composition fix). **Both residual gaps now closed:** (1) ✅ collection **string element-type length** is now expressible (`queue of string maxlength 200`) — the modifier binds to the element, write-sites prove the source within the bound, and `.peek`/accessor reads carry the bound into a capped destination; the 3 samples (it-helpdesk-ticket, restaurant-waitlist, utility-outage-report) are green; (2) ✅ **resolved 2026-06-02** — an interpolation of an open-ended quantity into a capped string is genuinely unbounded; ruling = the field is free-form, so `statistical-process-control` `CurrentAlertReason` had its `maxlength` dropped (Precept correctly rejects a cap there).

### BUG-016: collection-mutation forward-propagation — guard facts not effect-adjusted across grow/shrink

- **Status**: ✅ **Fixed 2026-05-29.** A new catalog axis `ActionMeta.Effect` (`ActionEffectClass`: `Grows`/`Shrinks`/`Empties`/`ReplacesValue`/`None`) classifies each action; `ReplacesEntireValue` (BUG-014) is now derived from it (`ReplacesValue | Empties`). `WalkActions` (`ProofEngine.cs`) runs a forward walk over the action chain tracking per-collection non-empty status, stamping each obligation with `CountEstablishedBefore` (grown) / `CountInvalidatedBefore` (shrunk). A grow establishes `count > 0` (new **Strategy 11 `CollectionGrowth`** discharges a following `dequeue`/`pop` non-empty obligation without an author guard — closes the completeness false-positive); a shrink invalidates a pre-shrink `count > 0` guard in `TryGuardInPathProof` (closes the latent soundness hole — `when Q.count > 0 -> dequeue -> dequeue` rejects the second). The two count sets are mutually exclusive (last effect wins), so a grow re-establishes after a shrink. Probe-confirmed both sides before/after. Tests: `test/Precept.Tests/ProofEngine/CollectionCountEffectTests.cs` (6) + `ActionsTests.cs` effect-classification canaries. Spec § 0.6 item 7 marked implemented; proof-engine stage doc § Strategy 3 documents the mechanism. **Bounded incompleteness retained** (sound, not a gap): the model is boolean not a count interval, so `count >= 2 -> dequeue -> dequeue` under-proves the second; membership-across-shrink (`F contains K` after `remove F K2`) is out of scope.
- **Discovered**: 2026-05-28 (BUG-014 follow-on; spec § 0.6 item 7 clause b, "before the new assignment's facts are stored").
- **Symptom (two-sided, both probe-confirmed)**: (1) *completeness* — `enqueue Q V -> dequeue Q` over-rejected (`UnguardedCollectionMutation`) even though the enqueue guarantees `count >= 1`; (2) *soundness* — `when Q.count > 0 -> dequeue Q -> dequeue Q` over-proved the second dequeue via the stale guard (the first may have emptied Q). `clear` was already sound via BUG-014's `ReplacesEntireValue`/`ReassignedBefore` path.

### BUG-015: reassignment-invalidation not applied to index-bounds / key-presence / field-to-field guard strategies (was over-proving)

- **Status**: ✅ **Fixed 2026-05-29.** The `ReassignedBefore` filter (BUG-014) is now consulted by all three remaining guard-narrowing strategies in `ProofEngine.Strategies.cs`: `TryIndexBoundsProof` rejects discharge when the index subject *or* the collection field was reassigned earlier in the chain; `TryKeyPresenceProof` rejects when the collection was reassigned; `TryFlowNarrowingProof` rejects when either subtraction operand was reassigned. Each is a pure early-return (refuse to discharge) — sound by construction (only ever proves less). Probe-confirmed the index-bounds over-prove (`when Idx >= 0 and Idx < L.count and L.count > 0 -> set Idx = 0 -> remove L at Idx`) now flips the index obligation `Proved → Unresolved` while the collection's `count > 0` stays `Proved`. Tests: four index-bounds red/green (accessor + action site) and one key-presence case in `ReassignmentInvalidationTests.cs`, one synthetic field-to-field case in `ProofEngineTests.cs` `Slice6_FlowNarrowing`. Full suite green.
- **Discovered**: 2026-05-28 (BUG-014 follow-on; spec § 0.6 item 7 clause a).
- **Root cause**: BUG-014's first cut added the `ReassignedBefore` filter only to `TryGuardInPathProof` (numeric + presence). The other three strategies share the guard substrate but matched guard constraints without the field-filter, so a reassigned subject's stale fact still discharged their obligations.

### BUG-014: guard facts survive field reassignment — sequential-proof-flow (spec item 7) not implemented for guard narrowing

- **Status**: ✅ **Fixed 2026-05-29.** `ProofObligation.ReassignedBefore` carries the fields written by prior **full-value-replacement** actions in the same chain — a catalog-declared property (`ActionMeta.ReplacesEntireValue`, true for `set`/`clear`), so the proof engine no longer switches on `ActionKind` identity to classify replacement. `WalkActions` (`ProofEngine.cs`) stamps each action's obligations with the prefix-write set; the four guard-narrowing strategies skip guard facts about reassigned subjects (numeric + presence in `TryGuardInPathProof` landed first; index-bounds / key-presence / field-to-field followed — see BUG-015). A `when X != 0` fact no longer discharges `100 / X` after `set X = 0`; `when X is set` no longer survives `clear X`. In-place collection mutations (`append`/`insert`/`remove`) are deliberately excluded from invalidation (they transform rather than replace — `shopping-cart.precept`'s insert-then-remove stays sound); effect-aware forward-propagation for those is BUG-016. Tests: `ReassignmentInvalidationTests.cs`. Full suite green. § 0.6 impl-status drift corrected in the same pass.
- **Discovered**: 2026-05-28 while grounding the D9 qualifier-narrowing design (`docs/Working/d9-qualifier-narrowing-design.md`).
- **Affected**: every guard-narrowing discharge — `TryGuardInPathProof` (numeric + presence), `TryFlowNarrowingProof` (field-to-field), `TryIndexBoundsProof`, `TryKeyPresenceProof`. All pull the row guard from `t.Row.Guard` (`ProofEngine.Strategies.cs`) and apply it to every body obligation regardless of intervening reassignment of the guarded field.
- **Symptom**: a guard fact discharges an obligation *after* the guarded field was reassigned to a guard-violating value. Confirmed unsound divide-by-zero proof:
  ```precept
  precept ReassignProbe
  field X as integer default 1
  field R as integer default 0
  state S initial
  event E
  from S on E when X != 0
      -> set X = 0
      -> set R = 100 / X
      -> no transition
  ```
  `precept_compile` → `success: true`, divisor obligation `Proved` via `GuardInPath`. At runtime `100 / X` divides by zero; the engine declared it structurally safe — a Principle-1 violation.
- **Root cause**: `precept-language-spec.md § 0.6` proof-contract **item 7 "Sequential proof flow"** mandates *"When a field is reassigned, prior proof facts about that field are invalidated before the new assignment's facts are stored."* The engine does not implement this for guard-derived facts — `TransitionRowContext(Row)` carries the whole row, not the obligation's action-chain position, so the discharge cannot tell a guard fact is stale. **Doc-drift**: § 0.6 Implementation status (~line 246) lists "sequential proof flow" among obligations *"implemented and exercised by the proof-engine test suite"* — false.
- **Workaround used**: none (surfaced by a probe; no sample exploits it).
- **Fix complexity**: large — thread action-chain position into the obligation/context (or precompute per-obligation "fields reassigned before me" in `WalkActions`, which already iterates actions in order) and have guard-consuming strategies drop constraints whose subject was reassigned before the obligation's site. Shared guard-substrate change benefiting all strategies.
- **Priority**: blocks shipping (soundness). **Being fixed as the foundational step of the D9 qualifier-narrowing slice** — the new narrowing layer requires the same invalidation, so fixing the substrate fixes both; correct the § 0.6 Implementation-status drift in the same pass.
- **Repro**: the `ReassignProbe` snippet above.

### Post-Phase-5 code-review remediation: interval-algebra soundness + catalog/diagnostic completeness

- **Status**: ✅ **Fixed by post-Phase-5 remediation Slice 1 (2026-05-27).** An extra-high-effort `/code-review` on the Phase 5 spike branch surfaced 15 findings spanning soundness, catalog completeness, and naming. Slice 1 (1a/1b/1c/1d) addressed 11 of the 15 against the locked Phase-5 designs:
  - **Interval-algebra soundness (Slice 1c)**: `BuildSiblingRejectExclusions` forfeits multi-leaf AND-branches (¬(A∧B) = ¬A∨¬B, not ¬A∧¬B); `BuildNarrowedIntervals` cross-branch OR-union back-fills the base interval for fields absent from a branch; `NegateConstraintToInterval` dispatches integer vs. decimal-backed domains — integer uses exact half-step `V ± 1`, decimal closes at `V` (sound superset). The same dispatch + sentinel-safe arithmetic applied to `NarrowByConstraint` in the satisfiability scan.
  - **Catalog completeness (Slice 1b)**: `TryResolveDimensionVector` consults `DimensionCatalog` for bare dimension names (`quantity of 'length'` resolves via the catalog, not UCUM); access-modifier validation now respects `MutuallyExclusiveWith`; `IntegerDivideInteger` and `IntegerDivideNumber` got `IntervalTransfer` functions so narrowed counters propagate through division.
  - **Cleanup (Slice 1a)**: removed dead `NumericInterval.Difference`; collapsed `ProofLedger` ctor to a single 6-arg form.
  - **Diagnostic rename (Slice 1d)**: `AlwaysFalsePeriodComparison` → `DegeneratePeriodComparison` (the code fires for both always-false `==` and always-true `!=`).
- **Discovered**: 2026-05-27 via 9-angle code-review on the Phase 5 spike branch
- **Affected**: any precept whose proof discharge relies on the satisfiability scan or sibling-reject narrowing with multi-field guards, OR-branches, decimal-typed fields; any catalog consumer of bare-dimension qualifiers; any author writing `!=` with disjoint period literals.
- **Tests added**: `test/Precept.Tests/ProofEngine/IntervalAlgebraSoundnessTests.cs` (3), `test/Precept.Tests/Operations/QuantityProductDimensionTests.cs::BareDimensionQualifier_LengthTimesLength_ResolvesViaDimensionCatalog` (1). Suite: 7140/7140 pass.
- **Deferred**: 4 of 15 findings (qualifier-policy wiring for `MoneyDividePrice`, `each * box` dimensionless-product policy, `UnsatisfiableRule` diagnostic split, reachability threading into `FieldNeverSet`) require `/design` passes and are pending in Slices 2–5.

### F-LANG-BIZ-08: Discrete equality narrowing for `choice of` fields

- **Status**: ✅ **Fixed by Phase 5 W-D BIZ-08 (2026-05-27, commit `08fae0ef`).** Extended `NumericConstraintSubsumes` in `ProofEngine.Strategies.cs` to recognize `F == V_lit` equality guards: when the guard pins a field to a singleton value, the singleton is checked directly against the obligation's (comparison, threshold) pair via a new `ValueSatisfiesRequirement` helper. Reuses the existing guard-decomposition pipeline (branch-walking discipline preserved — every OR branch must independently discharge). Minimal sound surface per the locked design: direct `F == literal` only; disjunctive equality and field-to-field equality are deferred as separate extensions. Tests in `test/Precept.Tests/ProofEngine/DiscreteEqualityNarrowingTests.cs` (4 new, including soundness negatives for `Severity == 0 ⇒ 1 / Severity` and field-scope checks).
- **Discovered**: 2026-05-25 during Phase 3 Step 3.3f verification
- **Affected**: any precept rule or guard that uses `choice == "literal"` and expects the proof engine to narrow the choice field to that value in the branch body
- **Original symptom**: `when Priority == "High"` did not narrow `Priority` to `"High"` inside the branch. Strategy 3 (GuardInPath) and Strategy 4 (FlowNarrowing) were numeric-only — no `BuildNarrowedDiscreteValues` analog. The equality operator resolved cleanly, but no narrowing strategy consumed the result.
- **Workaround used**: none needed for shipped samples — choice equality worked as a boolean condition; just didn't enable further proof narrowing.
- **Fix complexity**: ended up small — extending the existing subsumption switch with one equality arm + one value-satisfies helper. The "design-required" estimate proved too pessimistic: the reuse path through `GuardConstraint` made a new mechanism unnecessary.

### BUG-006: Proof engine doesn't combine guard narrowing with field-level `max` for arithmetic interval inference

- **Status**: ✅ **Fixed by Phase 5 W-C (2026-05-27, commit `c16be77b`).** `BuildNarrowedIntervals` in `ProofEngine.Intervals.cs` now composes sibling reject-row guards into the current row's per-field narrowing. New `BuildSiblingRejectExclusions` helper walks every reject row on the same `(state, event)` pair; for each leaf constraint, `NegateConstraintToInterval` produces the negated interval (integer-style half-open: `>= V` ⇒ `<= V-1`; conservative-on-decimal noted in the doc-comment). Required collateral: integer arithmetic ops (`IntegerPlusInteger`, `IntegerMinusInteger`, `IntegerTimesInteger`) now carry `IntervalTransfer` functions so the narrowed interval propagates through `Counter + 1`. Tests in `test/Precept.Tests/ProofEngine/CrossRowIntervalCompositionTests.cs` (3 new, including the verbatim repro). **Fix scope (post-Phase-5 sample-restore audit, 2026-05-28)**: the W-C cross-row narrowing discharges only **literal-comparison sibling rejects** (`Counter >= 5`). It does NOT compose for field-vs-field guards (`Counter >= MaxField`), narrowed-through-division (`(a+b+c)/3`), or presence narrowing (`when Field is not set -> reject`). Six of nine post-Phase-5 BUG-006 cites fell into these unfixable patterns; the runtime rule workaround was retained in those samples as the legitimate enforcement. Future extension of the W-C narrowing to field-vs-field guards is tracked as a separate follow-up.
- **Discovered**: 2026-05-24 during authoring of `samples/library-inter-library-loan.precept`
- **Affected**: any precept doing `set Counter = Counter + 1` in a row body where a sibling row above rejects when `Counter >= MaxField`, and `MaxField` carries a field-level `max N` modifier
- **Original symptom**: `PRE0078 UnprovedOverflow` on `Counter + 1` even though the guard row above structurally rejected the case where `Counter >= MaxField` AND `MaxField` was bounded by `max N`. The proof engine narrowed on each separately but did not combine them transitively.
- **Workaround used**: in `samples/library-inter-library-loan.precept`, dropped the field-level `max 5` on `RenewalCount` and relied on a runtime `rule RenewalCount <= MaxRenewals` invariant. Cited inline with a `# BUG-006:` comment.
- **Post-fix cleanup completed** (2026-05-28, commit `332f76ac`): `max N` restored on counters where the literal-comparison sibling-reject narrowing now discharges (`saas-trial-to-paid` ExtensionCount, `global-meeting-scheduler` ParticipantCount); `# BUG-006` cites stripped from the remaining 6 samples where the runtime rule is the proper enforcement (field-vs-field / division / presence-narrowing patterns the W-C fix doesn't reach).

### BUG-004: Proof engine ignores event ensures for transition-row body narrowing

- **Status**: ✅ **Fixed by Phase 5 W-A (2026-05-27, commit `034a5976`).** Extended the guard-extraction switch in `ProofEngine.Strategies.cs:TryGuardInPathProof` so event ensures on the row's event contribute their predicates to the row body's narrowing context. AND-combined with the row's own explicit guard via the new `CombineAndBranches` helper. Mirrors BUG-001's fix shape. Tests in `test/Precept.Tests/ProofEngine/EventEnsureNarrowingTests.cs` (4 new). Sample-restore pending: `samples/equipment-lease-agreement.precept` can drop the redundant `when MonthlyPayment is set` guard.
- **Discovered**: 2026-05-24 during authoring of `samples/equipment-lease-agreement.precept`
- **Affected**: any precept whose transition row body reads an optional field that an `on Event ensure Field is set` declaration has already proven present.
- **Original symptom**: `PRE0116 UnprovedPresenceRequirement` on `MonthlyPayment` (after `on Quote ensure MonthlyPayment is set`) even though the event ensure structurally rejects the event before the row body runs if the field is absent. Same shape as BUG-001 (rule/ensure `when` guard body narrowing) but for event ensures narrowing transition-row bodies.
- **Workaround used**: equipment-lease-agreement sample added a redundant `when MonthlyPayment is set` guard to the row with a `# BUG-004:` comment.
- **Post-fix cleanup pending**: remove the redundant `when X is set` guard from every row carrying a `# BUG-004` cite. **Scope-check**: sweep `grep -rn "# BUG-004" samples/`.

### BUG-013: `ParserIntegrationTests.TestSample_EventDeclaration_BindsInitialToCreateOnly` references missing `samples/Test.precept`

- **Status**: ✅ **Fixed by Phase 5 W-F (2026-05-27, commit `766637c0`).** `samples/Test.precept` restored with the minimal multi-event-with-`initial` shape the test asserts: `event create initial`, `event start`, `event stop`, `event reset`, three states (Idle/Running/Stopped), four transition rows. Test now passes.
- **Discovered**: 2026-05-25 during Phase 2 Step 2.7 verification of the test suite
- **Affected**: `test/Precept.Tests/Parser/ParserIntegrationTests.cs:179` read `Path.Combine(SamplesRoot, "Test.precept")` and parsed it; `samples/Test.precept` did not exist in the repo, so `ParseFile` threw `FileNotFoundException`.
- **Original symptom**: `System.IO.FileNotFoundException : Could not find file '/home/sfalik/source/repos/Precept/samples/Test.precept'.` at the test's `ParseFile(path)` call.

### F-LANG-COLL-13: `clear` and `notempty` lifted on `lookup of K to V`

- **Status**: ✅ **Fixed by Phase 4 W-J (2026-05-26)** after a `/design` pass surveyed comparator languages (Java `Map.clear`, C# `IDictionary.Clear`, Python `dict.clear`, Rust `HashMap::clear`, Swift `Dictionary.removeAll`, Kotlin `MutableMap.clear`, F# `Dictionary.Clear`, Go `clear(map)` added in 1.21 specifically to avoid forcing iteration). No surveyed language with per-key remove forbids the bulk operation; Precept's exclusion was anomalous. Bundled lift: `notempty` on lookup also lifted (was a parallel synonym restriction without independent rationale once `clear` lifted). Spec docs (`collection-types.md:85`, `:756`, `:902`, `precept-language-spec.md:1624`, `:1632`, `:1662`) updated. `Actions.cs ClearApplicable` adds `TypeKind.Lookup`; `Types.cs` Lookup TypeMeta drops the explicit `NotemptyApplicable: false` (default is `true`). Sample restore: `samples/shopping-cart.precept` ClearCart + Cancel events revert to canonical `clear LineItems / clear ItemQuantities / clear CartPromotions / clear GiftMessages`.
- **Discovered**: 2026-05-26 during Phase 4 W-A's precept-reviewer audit.
- **Affected**: any precept that wanted to empty a lookup field in one statement.
- **Pre-fix symptom**: `clear MyLookup` emitted `PRE0048 ScalarOperationOnCollection`. `notempty MyLookup` emitted `InvalidModifierForType`.

### BUG-012: Ordinal comparison between an ordered-choice field and a choice-literal cannot be proved

- **Status**: ✅ **Fixed by Phase 4 W-G (2026-05-26).** The proof engine's `TryDeclarationAttributeProof` ModifierRequirement arm now lifts a literal-side modifier from the sibling operand when the obligation site is a binary op (`ProofEngine.Strategies.cs`). When `Severity <= 2` emits `ModifierRequirement(PChoice, Ordered)` resolved against the literal `2`, the engine consults the sibling `Severity` operand and discharges from its declared `ordered` choice. The fix is symmetric: `Tier <= "Low"` discharges identically. Field-vs-field comparisons continue to discharge via the original DeclarationAttribute path. Unordered choice fields with literal comparisons still emit `PRE0112` (no order to inherit). Sample restore: `samples/it-helpdesk-ticket.precept` reverts the equality-cascade Priority computation to the canonical ordinal form (`if Severity <= 1 and Urgency <= 1 then "Critical" / else if Severity <= 2 or Urgency <= 2 then "High" / else if Severity <= 4 and Urgency <= 4 then "Medium" / else "Low"`). Tests in `test/Precept.Tests/ProofEngine/OrderedChoiceLiteralTests.cs` (4 new).
- **Discovered**: 2026-05-25 during refactor of `samples/it-helpdesk-ticket.precept` to give Severity, Urgency, and Priority an idiomatic ordered-choice shape.
- **Affected**: any precept that used `<`, `<=`, `>`, or `>=` between an `ordered` `choice of …` field and a bare literal from the same choice set.
- **Pre-fix symptom**: `precept_compile` emitted `PRE0112` UnprovedModifierRequirement on the literal operand. Field-vs-field comparisons of two same-set ordered choice fields proved cleanly via `DeclarationAttribute`.

### BUG-005: `lookup of K to money in '<Currency>'` and other qualified inner types

- **Status**: ✅ **Fully fixed by Phase 4 W-C (2026-05-26, F-LANG-COLL-06).** Qualified inner types in collections (`set of money in 'USD'`, `lookup of K to money in 'USD'`, `bag of quantity of 'mass'`, etc.) parse, type-check, and propagate qualifier metadata through to the proof engine. New `TypedElementType` DU in `SemanticIndex.cs` (variants: `TypedScalarElement`, `TypedChoiceElement` for W-G, `TypedQualifiedElement` with declared-qualifier metadata). Parser extended at `Parser.Types.cs ParseInnerTypeReference` to route inner types through `TryParseQualifiers`. Proof engine extended at `ProofEngine.Qualifiers.cs ResolveQualifierFromExpression` to inherit lookup-access result qualifiers from the lookup's element-type metadata. Sample restore: `event-venue-booking.precept` reverted from event-arg-carried-fee workaround back to canonical `set of string AddOnServices + lookup of string to money in 'USD' AddOnFees` paired pattern. Tests in `test/Precept.Tests/Parser/QualifiedInnerTypeTests.cs` (13 new).
- **Discovered**: 2026-05-24 during authoring of `samples/event-venue-booking.precept`
- **Affected**: any precept declaring a lookup with a qualified-money value type; bisected to specifically the `in '<Currency>'` qualifier on the value side
- **Original symptom** (before Phase 2): `precept_compile` returned "An error occurred invoking 'precept_compile'." with no PRE-code and no diagnostic. Phase 2 added a symmetric currency-qualifier rejection (clean PRE0105); Phase 4 added full support.

### BUG-002: `remove` on a lookup expects the value type instead of the key

- **Status**: ✅ **Fixed by Phase 4 W-B (2026-05-26)** — `TypeChecker.Expressions.Callables.cs` `CollectionValueAction` arm now branches on target type: when target is Lookup and action is Remove, the expected operand type is the lookup's `KeyType` rather than `ElementType`. `remove Items "specific-key"` and `remove Items Drop.Key` now type-check correctly. Sample cleanup landed in the same commit: `bill-of-materials-management.precept` and `shopping-cart.precept` reverted from the `put F K = 0` workaround to direct `remove F K`. Tests in `test/Precept.Tests/TypeChecker/LookupRemoveTests.cs`.
- **Discovered**: 2026-05-24 during authoring of `samples/bill-of-materials-management.precept`
- **Affected**: any precept that wants to delete a key from a `lookup of K to V` field
- **Pre-fix symptom**: `PRE0105 CollectionInnerTypeError — Expected a integer value, but 'Components' holds elements of type string` when the user wrote `remove Components RemoveComponent.PartNumber` where `Components` is `lookup of string to integer`. The type checker required the `remove` argument to match the lookup's value type rather than the key type.

### BUG-011: `timezone` and `time` fields with typed-constant default crash the compiler

- **Status**: ✅ **Fixed by Phase 2 (2026-05-25, commit `38712543`)** — already passing per the Phase 2 Step 2.2 verification (no crash; valid temporal defaults compile clean). Defence-in-depth wrapper added to `tools/Precept.Mcp/Tools/McpToolSafeInvoke.cs` ensures any future regression surfaces as a structured `McpToolInternalError` diagnostic instead of the raw `"An error occurred invoking ..."` MCP response. Scenario test in `test/Precept.Mcp.Tests/CompileTool_BugReproTests.cs`. **Post-fix sample cleanup pending**: restore the `timezone default` / `time default` declarations in `samples/global-meeting-scheduler.precept`.
- **Discovered**: 2026-05-24 during feature-gap-fill batch authoring `samples/global-meeting-scheduler.precept`
- **Affected**: any precept that declares `field X as timezone default '<literal>'` (e.g. `field DefaultTz as timezone default 'America/New_York'`) or `field X as time default '<literal>'` (e.g. `field DefaultStart as time default '09:00'`). Same fault family as BUG-003 (`period`), BUG-008 (`duration`), BUG-010 (`now() + duration`) — typed-constant temporal-literal handling.
- **Original symptom**: `precept_compile` returned "An error occurred invoking 'precept_compile'." with no diagnostic, no PRE-code, no message — identical to BUG-003/008/010. Stripping `default '<literal>'` (leaving the field as `optional`) let the same definition compile clean.
- **Workaround used**: in `samples/global-meeting-scheduler.precept`, `DefaultTimezone` is declared `timezone optional` and the host supplies it at construction via `Create(DefaultTimezone as timezone, ...)`; `DefaultStartTime` is similarly `time optional`. Cited inline with a `# BUG-011:` comment in the file header.
- **Post-fix cleanup**: in `samples/global-meeting-scheduler.precept`, restore `field DefaultTimezone as timezone default 'America/New_York'` and `field DefaultStartTime as time default '09:00'`; drop the `DefaultTimezone` and `DefaultStartTime` arguments from `Create(...)`; remove the `# BUG-011:` header comment.

### BUG-010: `now() + '<duration>'` expression crashes the compiler

- **Status**: ✅ **Fully fixed by Phase 3 (F-LANG-TEMP-01/02, context-aware temporal classification).** Phase 2 fixed the crash (commit `38712543`); Phase 3 fixed the type-inference quirk — `TemporalQuantityParser.Parse` now accepts an optional `TypeKind? expectedType` parameter, so `now() + '365 days'` in a duration arithmetic context correctly classifies the literal as a duration and compiles clean. Defense-in-depth wrapper in `McpToolSafeInvoke` remains for any future regression. Scenario test in `test/Precept.Mcp.Tests/CompileTool_BugReproTests.cs`.
- **Discovered**: 2026-05-24 during sweep batch 6 refactor of `samples/saas-user-provisioning.precept` and `samples/saas-license-management.precept`
- **Affected**: any precept that computes a future `instant` by adding a duration literal to `now()` — e.g. `set ExpirationDate = now() + '365 days'`, `now() + '30 days'`, `now() + '1 hour'`. The shape appears in `now() + '<duration literal>'` whether in a transition row body or an event ensure.
- **Original symptom** (before Phase 2): `precept_compile` returned "An error occurred invoking 'precept_compile'." with no diagnostic, no PRE-code, no message. Now returns structured PRE0058 (type-inference quirk; no crash).
- **Workaround used**: in batch 6, both `samples/saas-user-provisioning.precept` and `samples/saas-license-management.precept` carry the expiration date as an event argument supplied by the procurement/identity host. Cited inline with `# BUG-010:` comment in the file header.
- **Post-fix cleanup** (Phase 4 sample-restore): restore server-side temporal derivation in `samples/saas-user-provisioning.precept` and `samples/saas-license-management.precept`: replace the `LicenseExpirationDate`/`ExpirationDate`/`NewExpirationDate` event-arg path with `set ExpirationDate = now() + '<term>'` row bodies (e.g. `'365 days'` for annual license, `'30 days'` for grace). **Scope-check**: sweep `grep -rn "# BUG-010" samples/` to find every cite site. Remove `# BUG-010` comments.

### BUG-009: `precept_compile` MCP tool has an undocumented payload-size limit

- **Status**: ✅ **Fixed by Phase 2 (2026-05-25, commit `38712543`)** — not reproducible in-process. Synthetic ~20 KB precept compiles cleanly via direct `CompileTool.Compile()` call; if a stdio-framing limit exists in the MCP SDK, the new `McpToolSafeInvoke` wrapper catches it cleanly and returns a structured `McpToolInternalError` diagnostic rather than the raw `"An error occurred invoking ..."` MCP response. Scenario test in `test/Precept.Mcp.Tests/CompileTool_LargePayloadTests.cs`. **Wire-level re-verification needed** once the MCP server is rebuilt and the new wrapper is loaded — current session's MCP server is the pre-Phase-2 build.
- **Discovered**: 2026-05-24 during sweep batch 1 refactor of `samples/medical-prior-auth.precept` and `samples/prior-auth-appeal.precept`
- **Affected**: any precept file larger than ~12-15 KB sent to `precept_compile` via the MCP tool (originally reported).
- **Original symptom**: `precept_compile` returned "An error occurred invoking 'precept_compile'." with no diagnostic, no PRE-code, no message — identical symptom to BUG-003, BUG-005, BUG-008. Files in the 8-10 KB range compiled fine; files at ~14 KB crashed.
- **Workaround used**: in sweep batch 1, the agent validated `samples/medical-prior-auth.precept` (~14 KB) and `samples/prior-auth-appeal.precept` (~14 KB) via chunked compiles.
- **Post-fix cleanup**: no sample changes required — this is a tooling bug, not a DSL workaround. Once the MCP server is rebuilt with the Phase 2 wrapper, agents and `/precept-author` workflows revert to full-file `precept_compile` instead of chunked validation. No in-corpus `# BUG-009` citations exist to remove.

### BUG-008: `duration` field with typed-constant default crashes the compiler

- **Status**: ✅ **Fixed by Phase 2 (2026-05-25, commit `38712543`)** — already passing per the Phase 2 Step 2.2 verification (valid duration defaults like `'14 days'` compile clean). Defense-in-depth wrapper in `McpToolSafeInvoke` covers regression. Scenario test in `test/Precept.Mcp.Tests/CompileTool_BugReproTests.cs`. **Post-fix sample cleanup pending**: restore `duration default` in `samples/patient-care-plan-coordination.precept`.
- **Discovered**: 2026-05-24 during refactor of `samples/patient-care-plan-coordination.precept`
- **Affected**: any precept that declares `field X as duration default '<literal>'` (e.g. `field ReviewFrequencyDays as duration default '14 days'`, `field GracePeriod as duration default '4 hours'`). Same family as BUG-003 (`period` default).
- **Original symptom**: `precept_compile` returned an MCP-level error: `"An error occurred invoking 'precept_compile'."` with no diagnostic, no PRE-code, no message. Stripping `default '<literal>'` and leaving the field as `duration optional` let the same definition compile clean.
- **Workaround used**: in `samples/patient-care-plan-coordination.precept`, dropped the `ReviewFrequencyDays` field entirely. The host can carry review-frequency scheduling outside the precept. Cited inline with `# BUG-008:` comment.
- **Post-fix cleanup**: restore `field ReviewFrequencyDays as duration default '14 days'` (or whatever frequency the domain calls for) in `samples/patient-care-plan-coordination.precept`; if the field had rules / ensures that referenced it, restore those too. **Scope-check**: sweep `grep -rn "# BUG-008" samples/` to find every cite site. Remove `# BUG-008` comments.

### BUG-007: `precept_domains` MCP tool crashes on any scope

- **Status**: ✅ **Fixed by Phase 2 (2026-05-25, commit `38712543`)** — verified working for all 5 scopes (`currencies`, `units`, `dimensions`, `prefixes`, `temporal`, and no-arg) via direct call to `DomainsTool.Domains(scope)`. The earlier-reported crash appears to have been resolved by intervening work before Phase 2; Phase 2's `McpToolSafeInvoke` wrapper provides defense-in-depth against regression. Scenario test in `test/Precept.Mcp.Tests/DomainsTool_AllScopesTests.cs`. **Wire-level re-verification needed** once the MCP server is rebuilt.
- **Discovered**: 2026-05-24 during Phase 4 stateless authoring
- **Affected**: AI agents that follow the agent body's guidance to use `precept_domains` for currency / unit / dimension lookups; user-facing because the MCP tool is documented and surfaced.
- **Original symptom**: Calling `precept_domains` (with or without a scope argument) returned "An error occurred invoking 'precept_domains'." with no detail or error code. Other MCP tools worked fine.
- **Workaround used**: agents fell back to inline known values or to `precept_types` for type-system metadata. No in-corpus `# BUG-007` citations exist to remove.

### BUG-003: `period` field with typed-constant default crashes the compiler

- **Status**: ✅ **Fixed by Phase 2 (2026-05-25, commit `38712543`)** — already passing per the Phase 2 Step 2.2 verification (valid period defaults like `'1 year'` compile clean; invalid defaults like `'1 bogus'` emit a structured `InvalidTypedConstantContent` diagnostic). Defense-in-depth wrapper in `McpToolSafeInvoke` covers regression. Scenario test in `test/Precept.Mcp.Tests/CompileTool_BugReproTests.cs`. **Post-fix sample cleanup pending**: restore `period default` in `samples/equipment-lease-agreement.precept` and `samples/insurance-renewal-processing.precept`.
- **Discovered**: 2026-05-24 during authoring of `samples/equipment-lease-agreement.precept`
- **Affected**: any precept that declares `field X as period default '<literal>'` (e.g. `field GracePeriod as period default '10 days'`, `field RenewalPeriod as period default '1 year'`).
- **Original symptom**: `precept_compile` returned an MCP-level error: `"An error occurred invoking 'precept_compile'."` with no diagnostic, no message, no structured payload. Stripping the `default '<literal>'` clause (leaving the field as `period optional`) let the same definition compile clean.
- **Workaround used**: equipment-lease-agreement sample declares `GracePeriod` as `period optional` (the host supplies it) and cites BUG-003 in a comment.
- **Post-fix cleanup**: restore `field GracePeriod as period default '<literal>'` in `samples/equipment-lease-agreement.precept`; drop the constructor / event-arg path that supplies it from the host; restore equivalent `period default '<literal>'` form in `samples/insurance-renewal-processing.precept` if it was workarounded too. **Scope-check**: sweep `grep -rn "# BUG-003" samples/` to find every cite site. Remove `# BUG-003` comments.

### BUG-001: Proof engine ignored rule/ensure `when` guards for body narrowing

- **Discovered**: 2026-05-23 during authoring of `samples/patient-referral-management.precept`
- **Affected**: any precept using `rule X when Y is set because "..."` or `ensure X when Y is set because "..."` with optional-field narrowing in the body
- **Symptom**: `UnprovedPresenceRequirement` (PRE0116) on the body's field references even though the `when` guard provably established presence
- **Root cause**: `ProofEngine.Strategies.cs` — Strategy 3 (GuardInPath) and Strategy 4 (FlowNarrowing) enumerated `TransitionRowContext`, `StateHookContext`, `EventHandlerContext` in their guard-extraction switch, but treated `ConstraintContext` (rules + ensures) as the discard arm
- **Workaround used**: sample 29 originally scattered the invariant across an event ensure + per-state transition reject rows. Refactored to a single rule after the fix landed.
- **Fix complexity**: trivial (two switch extensions)
- **Priority**: was blocking sample quality — fixed before shipping any sample with a workaround comment
- **Fixed by**: `ProofEngine.Strategies.cs:275, :522` — added `ConstraintContext c => c.Constraint switch { RuleIdentity ri => semantics.Rules[ri.RuleIndex].Guard, EnsureIdentity ei => semantics.Ensures[ei.EnsureIndex].Guard, _ => null }`; tests in `test/Precept.Tests/ProofEnginePresenceTests.cs`; doc updates in `docs/compiler/proof-engine.md`
