## Core Context

- Owns the squad's durable record: `.squad/decisions.md`, `.squad/decisions-archive.md`, `.squad/orchestration-log/`, `.squad/log/`, agent histories, and PR-body stewardship.
- Historical summary: closed the major language-research, vision/spec, PRECEPT0018, and parser-gap bookkeeping batches while treating inbox merge, archive, and checkbox maintenance as first-class deliverables.
- Summarization rule: when detailed batch history outgrows the working-memory threshold, collapse old detail into Core Context and keep only live batch state in `## Recent Updates`.
- Verification rule: if a requested inbox pass is already reflected in canonical records, log the verification/no-op rather than rewriting decisions.

## Learnings

- Measure decision-ledger size, inbox count, and archive eligibility before every merge pass; log pre/post health explicitly.
- Deduplicate overlapping inbox notes into one canonical decision entry and name every merged source.
- When the inbox is empty, an archive move is ineligible, or no PR exists for the branch, record the no-op/skip explicitly.
- Only check PR boxes for work that is clearly complete; uncertainty stays unchecked.
- Oversized histories must be summarized back into durable context instead of carrying every batch forward verbatim.

## Recent Updates

### 2026-05-07T03:00:00Z — Wave 3 Round 2 sweep recorded

- Pre-check measured `decisions.md` at 75117 bytes with 1 inbox file, so the hard-gate 7-day archive pass ran first and found 0 active entries older than 2026-04-30T03:00:00Z.
- Merged `frank-wave3-round2.md` into the active ledger, preserved the six discovered follow-up gaps in the canonical record, deleted the processed inbox file, wrote the Frank orchestration/session logs, and refreshed Frank/Scribe history.
- Health report: `decisions.md` 75117B -> 77092B; inbox processed = 1; history files summarized = 0.

### 2026-05-07T02:20:00Z — Wave 3 Round 1 sweep recorded

- Pre-check measured `decisions.md` at 73849 bytes with 1 inbox file, so the hard-gate 7-day archive pass ran first and found 0 active entries older than 2026-04-30T02:20:00Z.
- Merged `frank-wave3-round1.md` into the active ledger, deleted the processed inbox file, wrote the Frank orchestration/session logs, and refreshed Frank/Scribe history.
- Health report: `decisions.md` 73849B -> 75117B; inbox processed = 1; history files summarized = 0.

### 2026-05-07T01:26:52Z — Wave 2 closeout recorded

- Pre-check measured `decisions.md` at 72581 bytes with 1 inbox file, so the hard-gate 7-day archive pass ran first and found 0 active entries older than 2026-04-30T01:26:52Z.
- Merged `frank-wave2-complete.md` into the active ledger, deleted the processed inbox file, wrote the Frank orchestration/session logs, refreshed Frank/Scribe history, and summarized Frank back under the 15 KB gate.
- Health report: `decisions.md` 73849B; inbox processed = 1; history files summarized = 1.

### 2026-05-07T01:22:50Z — CC#26 decision ledger merged

- Pre-check measured `decisions.md` at 668130 bytes with 1 inbox file, so the hard-gate 7-day archive pass ran first.
- Archived 6 dated decision entries into `decisions-archive.md`, merged `frank-cc26-locked.md` into the active ledger, deleted the processed inbox file, and wrote the Frank orchestration/session logs.
- Health report: `decisions.md` 668130B -> 605305B; inbox processed = 1; history files summarized = 0.

### 2026-05-05T15:32:50Z — Value-types reconciliation batch recorded

- Pre-check measured `decisions.md` at 62325 bytes with 1 inbox file, so the hard-gate 7-day archive pass ran before merge and found 0 eligible entries older than 2026-04-28T15:32:50Z.
- Merged Frank's authoritative-doc reconciliation inbox note into the canonical ledger, deleted the processed inbox file, wrote the Frank orchestration/session logs, and refreshed Frank/Scribe history.
- Health report: `decisions.md` 62325B -> 63360B; history files summarized = 0.

### 2026-05-05T15:20:17Z — Value-types integration batch recorded

- Pre-check: decisions.md measured 825968 bytes with 2 inbox files, so the hard-gate 7-day archive pass ran before merge.
- Archived 0 dated decision entries, merged 2 inbox files into the canonical ledger, deleted the processed inbox files, and wrote the Frank orchestration/session logs.
- Health report: decisions.md 825968B → 845747B; history files summarized = 0.

### 2026-05-03T23:00:32Z — ReadJson / WriteJson batch recorded
- Pre-check measured decisions.md at 68928 bytes with 1 inbox file; the hard-gate archive pass ran on the 7-day window threshold and found 0 entries old enough to move.
- Merged `frank-readwrite-json-api` into a new CC#25 ledger entry, deleted the processed inbox file, wrote the Frank orchestration log plus the readjson-writejson session log, and confirmed No open PR found for branch 'Precept-V2-Radical'; PR stewardship was a no-op.
- Health report: decisions.md 68928B -> 70011B; inbox processed = 1; history files summarized = 0.
### 2026-05-03T22:22:27Z — CC#25 corpus archival pass recorded
- Pre-check measured `decisions.md` at 61,990 bytes with 19 inbox files; the hard-gate 7-day archive pass ran and found 0 entries older than the cutoff, so no archive move was performed.
- Merged the 19 CC#25 inbox files into 7 canonical ledger entries, wrote the Frank orchestration log and fire-data-flow session log, summarized Frank back under the 15 KB gate, and cleared the inbox.

### 2026-05-01T20:10:18Z — HandlesCatalogMember closeout recorded
- Ran the hard-gate 7-day archive pass because `decisions.md` was 562768 bytes before merge, then merged George-7's rename inbox note, wrote orchestration/session logs, and cleared the processed inbox file.
- Health report: `decisions.md` 562768B -> 573372B; inbox processed 1; history summarization remained at 0 files.

### 2026-05-01T06:40:00Z - Annotation-bridge inbox verification recorded
- Verified `.squad/decisions.md` already contains the canonical annotation-bridge entry sourced from `frank-class-marker` and `george-annotation-bridge-plan`, so the inbox merge itself was a no-op.
- Wrote verification orchestration/session logs, confirmed there is no open PR on `spike/Precept-V2`, and summarized oversized Frank/Scribe histories back under threshold.

### 2026-05-01T06:21:31Z - Annotation-bridge design batch recorded
- Ran the hard-gate 7-day archive pass because `decisions.md` exceeded 51200B before merge.
- Merged 6 inbox files into 2 canonical decision entries, wrote orchestration/session logs for frank-6 / george-2 / george-3, updated Frank and George histories, and cleared the processed inbox files.
- Health report: `decisions.md` 53785B -> 38562B; no `history.md` crossed the 15 KB summarization threshold after propagation.

### 2026-05-01T06:04:34Z - Parser coverage assertion exploration recorded
- Logged Frank's parser-coverage assertion pass, ran the 7-day decision archive gate, merged 1 inbox record, and cleared the processed inbox file.
- Health report: `decisions.md` 518013B -> 535361B and `decisions-archive.md` absorbed 1 active entry; no history summarization was needed in that pass.

### 2026-04-29T05:00:52Z - Collection iteration/rules research recorded
- Wrote orchestration logs for Frank's paired collection research batch, merged 2 inbox records into canonical decisions, and refreshed durable squad state.
- That pass also summarized Frank's oversized history, establishing the current summarization pattern.

### 2026-05-04T12:31:31Z — Cross-cutting driver / audit-status closeout recorded
- Pre-check measured `decisions.md` at 801946 bytes with 4 inbox files; the hard-gate 7-day archive pass ran before merge because the ledger exceeded the threshold.
- Checked for dated ledger entries older than 2026-04-27 and found none eligible to move, then merged 4 Frank inbox notes into the canonical decision record with deduplication, deleted the processed inbox files, and wrote orchestration logs for frank-78 / frank-79 plus the batch session log.
- Health report: `decisions.md` 801946B -> 808541B; inbox processed = 4; history files summarized = 1 (`frank/history.md`).

### 2026-05-04T15:32:34Z — Runtime API mini-spec inbox closeout recorded
- Pre-check measured `decisions.md` at 32088 bytes with 2 inbox files; the hard-gate 30-day archive pass ran and checked `decisions.md` for active entries older than 2026-04-04T15:32:34Z and found none to move.
- Merged Frank's two runtime API inbox notes into one canonical decision entry, deleted the processed inbox files, wrote the paired orchestration logs plus the session log, and refreshed Frank/Scribe history with no summarization required.

### 2026-05-04T16:20:24Z — ToJson / FromJson naming batch recorded
- Pre-check measured `decisions.md` at 808541 bytes with 7 inbox files, so the hard-gate 7-day archive pass ran before merge.
- Archived 0 dated records into `decisions-archive.md`, merged the 7 Frank inbox notes into 4 canonical persistence decisions, wrote the Frank orchestration log plus the session log, and refreshed Frank/Scribe history with no summarization required.

