# Catalog-Driven Parser: George's Cross-Review of Frank's Scope

**By:** George (Runtime Dev)
**Date:** 2026-04-27
**Reviewing:** `frank-catalog-parser-scope.md`
**Against:** `george-catalog-parser-estimate.md`

---

## The Short Version

Frank's scope is tighter than I assumed in two meaningful ways: Layer D drops from XL to L because he eliminates ConstructSlot metadata additions entirely, and his dispatch refactor for Layer B is exactly as clean as he claims. Layer A+B+E still land inside 1 week. The divergence is Layer D — and also Layer C, where Frank explicitly rejected DisambiguationToken as unnecessary. Both of those call for a position update.

---

## 1. Does Frank's Scope Change My Estimates?

### Layer A — Unchanged

My estimate: **S / ~2 days.** Frank's design: identical to mine — derived frozen dictionaries from `Types.All`, `Modifiers.All`, `Actions.All` at `ParseSession` construction. No surprises, no new fields. The `SetType` exception and dual-`initial` edge cases are the same in both analyses. **2 days holds.**

### Layer B — As Light as He Claims

Frank's `ByLeadingToken` index is 6 lines:

```csharp
All.GroupBy(c => c.LeadingToken)
   .ToFrozenDictionary(g => g.Key, g => g.ToImmutableArray())
```

Plus `LeadingTokens` is 3 more lines. Both are trivially derived. I had estimated 1h for the index itself. Confirmed.

The dispatch restructuring — switch → dictionary lookup for 1:1 cases + 4 unchanged disambiguation methods — Frank labels "Medium" in his estimation table. I would call it "Light-Medium." The `ParseAll()` loop restructure is mechanical. The 4 disambiguation methods are untouched. No hidden costs. My estimate of ~5h / 0.5-1 day for all of Layer B still holds. Frank's "Medium" label is slightly generous, but it's not wrong — the dispatch wiring needs care to preserve error paths and sync recovery correctly. I'll call it 5-8h total.

### Layer D — Major Revision Required

This is where Frank's design materially changes my estimate. Let me be precise about what changed.

**What I assumed in my estimate:**

My XL/4-5-week estimate was built on adding four new fields to `ConstructSlot`:
- `IntroductionToken: TokenKind?`
- `SeparatorToken: TokenKind?`
- `StopTokens: TokenKind[]?`
- `IsRepeated: bool`

That plus updating all 11 construct entries in `Constructs.GetMeta()` (12h), designing the slot metadata system (8h), and then writing the generic iterator to consume it.

**What Frank actually proposes:**

No new fields on `ConstructSlot`. No new fields on `ConstructMeta`. The generic iterator calls slot parsers via a `FrozenDictionary<ConstructSlotKind, Func<SyntaxNode?>>` registered in `ParseSession` initialization. Each slot parser is self-contained and owns its own boundary-token logic. The `BuildNode` exhaustive switch maps the slot array to named record fields after parsing completes.

This eliminates approximately **24 hours** of metadata design work from my estimate:
- Design slot metadata: ~8h → gone
- `ConstructSlot` record additions: ~4h → gone
- `Constructs.GetMeta()` annotation for 11 constructs: ~12h → gone

**What remains under Frank's design:**

| Task | My estimate | Frank's design | Revised |
|------|-------------|----------------|---------|
| Generic `ParseConstruct()` iterator | 40h (embedded in slot-metadata rewrite) | ~8h (40-line loop, dictionary dispatch, required-node check) | 8h |
| `_slotParsers` dictionary init (15 entries) | 40h (includes ParseSlot dispatch design) | 2h (trivial registration) | 2h |
| `BuildNode` factory (11 construct cases) | 20h | 12-16h (pure field-mapping, but 11 cases × heterogeneous slot types = real work) | 14h |
| 15 slot parser methods (self-contained) | 30h (rewrite from per-construct) | These were always going to be written. Frank's requirement is stricter: each must have an explicit boundary-token contract. | 35h (+5h for contract discipline) |
| Regression tests (11 constructs × parse+error cases) | 40h | Same. This doesn't change. | 40h |

**Revised Layer D total: ~99h / 2.5-3 weeks.** Down from 4-5 weeks. Frank's design is genuinely cleaner.

**Does this change my recommendation?** Yes, partially. My original verdict was "NOT RECOMMENDED" because 4-5 weeks of risk for a closed 11-construct grammar felt like over-engineering. At 2.5-3 weeks, the calculation shifts, especially because the parser is currently a stub — this is a clean-slate build, not a refactor. Writing fresh code in the generic-iterator pattern costs maybe 30% more than writing it in the per-construct pattern. The architecture you get is considerably better. For a clean-slate build, I'd soften "NOT RECOMMENDED" to **"conditional yes — if we're writing the parser now, write it this way."**

**The concern that doesn't go away:**

`ActionChain` and `Outcome` are not "parse one thing and stop" slots. An action chain is a loop: `-> action -> action → outcome`. The `->` token doubles as a separator and a terminator (it terminates when the next token is `transition`/`no`/`reject`). `Outcome` has three structural forms: `-> transition <state>`, `-> no transition`, `-> reject <msg>`. These require internal control flow, not just a "stop at token X" contract.

Frank's response to this risk is: "Slot parsers are self-contained — each returns a `SyntaxNode?`. The generic iterator handles optionality and missing-node insertion. Slot-specific boundary tokens are part of the slot parser contract (the slot parser knows when to stop)."

He's right that `ParseActionChain()` can own its own loop in the generic model exactly as it would in the per-construct model. The `ActionChain` slot parser just gets called once via the dictionary dispatch and internally manages its own loop. Same code, different call path. The boundary concern is real but it's also present in any parser implementation — Frank's model doesn't make it harder. I accept this.

The `BuildNode` factory risk is real though. Assembling typed records from `SyntaxNode?[]` — with heterogeneous slot types — requires runtime type assertions. `FieldDeclarationNode` expects `IdentifierListNode?` at slot 0, `TypeExpressionNode?` at slot 1, etc. A misalignment produces a runtime cast failure, not a compile-time error. Frank acknowledges this is "trivially mechanical" but I'd call it "mechanically fragile." A wrong slot ordering in `Constructs.GetMeta()` vs. the factory would fail at runtime. I want a test that validates the slot count and types against each factory case before we ship this.

### Layer E — Still Trivial

My estimate: 3h / bundled with A. Frank's design: identical. `Constructs.LeadingTokens` is a 3-line derived property. Zero changes. **Confirmed.**

---

## 2. Layer D: Factory Pattern — Does It Reduce XL/4-5-Week?

**Yes. Not to trivial, but from XL to L.**

The AST construction problem does NOT disappear. The `BuildNode` factory is 11 cases of heterogeneous type-mapping. That's real work and real fragility risk. But it's pure field-mapping with no parsing logic — Frank's framing there is accurate. The implementation risk is slot-ordering mistakes, not algorithmic complexity.

The bigger reduction comes from eliminating the ConstructSlot metadata design work entirely. Frank's approach keeps the slot metadata as-is (name, kind, isRequired) and puts the boundary contracts in the slot parser implementations, not in the catalog. That's correct — slot-level grammar mechanics belong in the parser, not in a metadata field.

Net: **XL/4-5-week → L/2.5-3-week.** The factory pattern meaningfully reduces the estimate. It's no longer the riskiest work in the codebase by a wide margin. It's still the single biggest item, but it's tractable.

---

## 3. Layer B: Hidden Costs?

No hidden costs. I looked for them.

The `ByLeadingToken` dispatch refactor is:
1. Add 2 derived properties to `Constructs` (~6 lines)
2. Replace `ParseAll()` switch with dictionary lookup + `if (array.Length > 1) → disambiguation method`
3. The 4 disambiguation methods (`ParseInScoped`, `ParseToScoped`, `ParseFromScoped`, `ParseOnScoped`) are unchanged

The only risk I see is error recovery integration. The parser doc says sync points are the set of valid leading tokens. If we build `_syncTokens` from `Constructs.LeadingTokens` (Layer E) AND we build the dispatch table from `ByLeadingToken` (Layer B), those two sets must stay consistent. In Frank's design they're both derived from the same catalog property — they can't drift. That's actually a correctness argument FOR Layer B+E together.

Frank's "Medium" label in his estimation table specifically covers "restructure `ParseAll()` loop" not just the index. I think he's right to call it Medium for the loop restructure — not because the logic is complex, but because error recovery, sync tokens, and the dispatch path all intersect in that loop. You can't mess this up. 8h for the full Layer B including careful integration testing is right.

---

## 4. Layer C: Is DisambiguationToken Actually Useful for LS/MCP?

Frank rejected it. His § 3.5 says "No Changes to ConstructMeta Shape." His Layer C verdict says catalog-driven disambiguation is rejected because it requires inventing a parser-in-the-catalog.

But my proposal for `DisambiguationToken` was narrower than Frank's rejected Layer C. I was NOT proposing to make the parser table-driven on it. I was proposing: add `DisambiguationToken: TokenKind[]?` to `ConstructMeta` as read-only metadata for tooling consumers. Parser still hand-writes its 4 disambiguation methods.

**Is the field actually useful for LS/MCP, or dead metadata nobody reads?**

Here's the concrete LS scenario:
- User has typed `from ReviewPending ` (cursor at end)
- LS wants to provide completions for what comes next
- Without `DisambiguationToken`: LS needs internal knowledge that after `from <state>` you can have `ensure`, `->`, or `on`. That knowledge is either hardcoded in the LS or derived by inspecting `StateEnsure.LeadingToken == From`, `StateAction.LeadingToken == From`, `TransitionRow.LeadingToken == From` — which gives you the construct candidates but not the follow token that disambiguates.
- With `DisambiguationToken`: LS queries `ByLeadingAndDisambiguationToken` grouped by leading=`From`, reads `DisambiguationToken` values, and produces completions: `ensure` (StateEnsure), `->` (StateAction), `on` (TransitionRow).

This is live metadata with a real consumer. LS completions using this field is not hypothetical — it's exactly how LS completions should work for scoped construct disambiguation.

For MCP: `precept_language` output could expose per-construct disambiguation tokens. An AI agent writing a `from`-scoped construct would know from the tool output that `ensure` goes to StateEnsure, `->` goes to StateAction, `on` goes to TransitionRow — without needing to internalize parser mechanics.

**It is NOT dead metadata.** It's moderate-value live metadata with 2 real consumers (LS completions, MCP vocabulary). The 2-day cost for catalog completeness + LS benefit is justified.

**My position stands:** Add `DisambiguationToken: TokenKind[]?` to `ConstructMeta`. Wire it in LS completions and MCP language output. Do NOT wire it into parser disambiguation methods — Frank is correct that the parser's hand-written 4 methods are cleaner and more debuggable than a table-driven replacement.

The divergence from Frank's scope is intentional. Frank's scope is parsimonious — no new ConstructMeta fields for parser purposes. My recommendation adds one field for tooling purposes. That's a different justification and I stand by it.

**One concrete problem I flagged in my original estimate that Frank doesn't address:** `AccessMode` disambiguation from `In` uses THREE possible disambiguation tokens (`Write`, `Read`, `Omit`), not one. A `TokenKind?` field is wrong. `TokenKind[]?` is required. Frank's scope avoids this by not adding the field at all. If we add it, we need the array form.

---

## 5. Updated Estimate

### What Frank's scope changes:

| Layer | My estimate | Frank's scope changes | Revised |
|-------|-------------|----------------------|---------|
| A | S / 2 days | No change | S / 2 days |
| B | XS / 0.5-1 day | Confirmed — his design is as clean as claimed | XS / ~1 day |
| C | S-M / 2-3 days | Frank rejects it; I still recommend it for tooling | S-M / 2 days (catalog + LS/MCP wiring only) |
| D | XL / 4-5 weeks | Factory pattern removes ~24h of metadata design; cleaner design overall | L / 2.5-3 weeks |
| E | XS / bundled with A | No change | bundled |

### Recommended bundles (Shane's decision surface):

**Option 1: A + B + C (catalog/tooling only) + E** ← My original recommendation
- **~5 days / 1 week**
- 80 vocabulary entries catalog-derived
- `ByLeadingToken`, `DisambiguationToken` for LS/MCP
- Sync-point set auto-derived
- Zero parser architectural change
- Risk: low

**Option 2: A + B + E (Frank's baseline without D or C)**
- **~3 days**
- 80 vocabulary entries catalog-derived + dispatch index
- Sync-point set auto-derived
- No new ConstructMeta fields
- Risk: near-zero
- Leaves DisambiguationToken out — LS completions for scoped constructs will need per-construct knowledge hardcoded

**Option 3: A + B + D + E (Frank's full recommended set)**
- **~3.5-4 weeks**
- Full catalog-driven parsing, generic slot iteration, per-construct AST factories
- The architecturally complete version
- Risk: moderate — `BuildNode` factory fragility, ActionChain boundary contracts, test coverage scope
- Makes sense for a clean-slate parser build. Does not make sense as a refactor.

**Option 4: All 5 layers (A + B + C + D + E)**
- **~4.5 weeks**
- Complete — everything catalog-derived that can be, plus DisambiguationToken for LS/MCP
- Risk: same as Option 3 plus Layer C catalog work

### Does my 1-week estimate hold?

For **Option 1 (A+B+C+E)**: yes, 1 week still holds. Frank's design confirms B is light, and A+E haven't changed. C at 2 days is unchanged.

For **Option 3 (Frank's full set A+B+D+E)**: the estimate moves from XL/6-7 weeks to L/~4 weeks. Frank's factory pattern design is genuinely cleaner. If Shane wants full catalog-driven parsing, it's 4 weeks not 6-7. That's a meaningful reduction.

---

## 6. The Real Divergence: Layer D Recommendation

Frank recommends Layer D. I originally recommended against it.

**My updated position:** At 2.5-3 weeks with a clean-slate parser, Layer D is defensible. The case for it:
1. No rework cost — we're writing this for the first time
2. The 30% overhead vs. per-construct methods buys a genuinely better architecture
3. Adding construct #12 becomes: catalog entry + factory case + slot parsers (if new). That's real.
4. Slot parsers become reusable across constructs naturally.

The case against, which I maintain:
1. `BuildNode` factory with heterogeneous slot types is fragile. Wrong slot ordering = runtime cast failure. Needs disciplined testing.
2. 11-construct grammar is small. "Zero dispatch changes for new 1:1 construct" benefits a grammar change that happens approximately never.
3. The `Slots` metadata is ALREADY serving tooling consumers (LS completions iterate it, MCP exposes it). The parser doesn't need to consume the same metadata to get the tooling value.

**Frank's counter-argument is legitimate:** Since we're building from scratch, the incremental cost is modest compared to the architectural win. I accept this. My "NOT RECOMMENDED" becomes "**viable on clean slate, but validate the factory fragility risk with a dedicated slot-ordering test suite before considering this layer done.**"

If Shane approves Option 3, I want a pre-condition: a test that validates, for each `ConstructKind`, that the `ConstructMeta.Slots` count and slot positions match exactly what `BuildNode` expects. A mismatch between catalog slot ordering and factory field-mapping should fail loudly, not silently produce a wrong AST.

---

## Summary

Frank's scope is well-reasoned and cleaner than I assumed. His Layer D design removes ~24h of metadata work by keeping ConstructSlot unchanged and putting boundary contracts in the slot parser implementations instead. That drops my Layer D estimate from 4-5 weeks to 2.5-3 weeks.

Layer B is exactly as light as he claims. Layer A+E haven't changed.

Layer C: Frank and I diverge. He says no new ConstructMeta fields. I say `DisambiguationToken: TokenKind[]?` is live metadata for LS completions and MCP vocabulary — not dead weight. 2 days, real consumer, justified. I'm keeping my recommendation.

Layer D: my "NOT RECOMMENDED" softens to "viable on clean slate, with a factory slot-ordering test suite as a pre-condition."

The real question for Shane is Option 1 (1 week, zero architectural risk) vs. Option 3 (4 weeks, better long-term architecture for a parser we're building from scratch anyway). That's a prioritization call, not a correctness call.
