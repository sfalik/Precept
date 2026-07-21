agentId=aaa5a17b5c1cf0b44 ts=

All verified. Final state:

## Corpus measurement harness — delivered

**New files**
- `/home/sfalik/source/repos/Precept/tools/Precept.MatrixTools/CorpusMeasurement.cs` — classifier + report model + JSON/markdown rendering
- `/home/sfalik/source/repos/Precept/tools/Precept.MatrixTools/Cli.cs` — new `measure-corpus <corpus-dir> [<out.json> [<out.md>]]` subcommand (edit)
- `/home/sfalik/source/repos/Precept/test/Precept.MatrixTools.Tests/CorpusMeasurementTests.cs` — 23 tests, written before the implementation
- `/home/sfalik/source/repos/Precept/test/Precept.MatrixTools.Tests/Fixtures/corpus/` — 16 fixture `.precept` files, one per classification bucket (5 licensed derivations, establishment fold ×2, respell-needed ×2, no-licensed-respelling, multi-write, relational, conditional, quantified, editable+entry-hook, enumerator skips, ensure, and the two ruled-but-unimplemented normal-form gaps)

**Test counts (literal):** `Passed: 152, Failed: 0, Skipped: 0` for `test/Precept.MatrixTools.Tests/`; the new suite alone: `Passed: 23, Failed: 0`. `dotnet build tools/Precept.MatrixTools/Precept.MatrixTools.csproj` → `0 Error(s)`.

**Pre-existing blocker (not mine, not fixed):** a parallel session left `test/Precept.MatrixTools.Tests/ProseGeneratorTests.cs` (untracked) referencing a `CellProseGenerator` class that exists nowhere — the test project does not compile at all with it included. I did not touch that file; my runs excluded it via `-p:DefaultItemExcludes="bin/**;obj/**;**/ProseGeneratorTests.cs"`. Plain `dotnet test` will fail until that session lands its class.

**Design decisions (conservative by construction)**
- A derivation is applied only where its stated decision procedure applies; when unsure the harness refuses to license (over-counting the respell band is safe; a wrong "licensed" is not).
- Establishment is licensed **only** by the literal fold — the only establishment discharge the matrix states today; non-ground establishment WPs get a named reason, not a guess.
- The two owner-ruled-but-unimplemented normal-form rules (division folding, negated-ordering flip) surface as **named skips** whenever they could flip a verdict — never a generic "not supported"; every out-of-scope row carries one of 17 named reason constants.
- Guard bound-conjunct facts feed interval closure on any term (the band ruling: "wherever they are spelled"); declared-bound interval use is restricted to primitive-numeric args; the pre-state-hypothesis derivation is implemented exactly as the narrow written-field-minus-nonnegative shape the matrix states.
- Frame pairs (plan doesn't touch the rule) are counted but not minted, matching symmetric attachment.

**Real-corpus headline (samples/, 78 files):** rule-bearing 78 by the broad definition (any rule/desugaring-modifier/ensure); **59 with explicit `rule` statements — matching the matrix's "59 rule-bearing files" exactly**. 1 file with error diagnostics (`Test2.precept`, the known unproven-max shape). 2,509 classified rows + 5,919 frame pairs:
- **licensed as written: 89** — literal fold for establishment 75, arg-bound interval sum 11, guard bound-conjunct interval 1, whole-guard match 1, hypothesis decrease 1
- **respell needed: 10** · **no licensed respelling: 0**
- **out of scope: 2,410** — multi-write plan (pending write-plan ruling) 569, ensures not enumerated 567, enumerator skips 530 (490 accessor-projected length/count, 40 satisfaction-less e.g. `maxplaces`), activation-conditioned 315, relational 208, non-primitive-numeric field 155, editable sites 34, computed-mention 22, edit declarations 9, entry hook 1

**Five most interesting rows**
1. `event-registration.precept` · `TicketsIssued:nonnegative` @ `from PendingPayment on RecordPayment` — **licensed, guard bound-conjunct interval**: guard `SeatsReserved > 0` closes WP `0 <= SeatsReserved` (hand-verified against source; the band-ruling derivation firing on a pre-state field term in the wild).
2. `event-registration.precept` · `TicketsIssued:max` @ same row — **respell needed**, WP `SeatsReserved <= 100`: SeatsReserved's own `max 100` modifier would close it via a pre-state-hypothesis interval, but no stated derivation licenses that — concrete power-widening candidate for the band inventory.
3. `trafficlight.precept` · `CycleCount:nonnegative` @ `from Green on Advance` — **respell needed**, WP `0 <= 1 + CycleCount`: increment of a nonnegative field; sound by hypothesis, but the stated hypothesis derivation covers only the upper-bound decrease shape — the lower-bound mirror is unstated.
4. `Test2.precept` @ `on Subtract` — **licensed, pre-state hypothesis with nonnegative decrease** — the matrix's Base B live in the corpus.
5. `restaurant-waitlist.precept` · `EstimatedWaitMinutes:nonnegative` @ `from Seating on MarkSeated` — **respell needed**, WP `0 <= 10 * PartyQueue.count`: trivially sound (count is nonnegative) but no derivation knows accessor sign facts — a second distinct widening candidate.

**Reports:** `/tmp/claude-1000/-home-sfalik-source-repos-Precept/eb473e68-b1f1-4b97-9b68-85f3ae5d44b1/scratchpad/corpus-measurement.json` and `.md` (same dir).

**Flags for the owner:** (a) the matrix's family-level "respellable: yes" is consistent with this run — all 10 unlicensed rows have recordable respellings, 0 rows have none — but rows 2/3/5 above are recurring band shapes worth the power-widening pipeline; (b) 569 multi-write rows confirm the queued write-plan ruling is the single biggest coverage gate; (c) no canonical doc references MatrixTools, and the matrix/normal-form working docs are under active owner revision, so I made no doc edits — the new subcommand is the ratification-protocol layer-4 tooling and the working docs may want a pointer when next swept.