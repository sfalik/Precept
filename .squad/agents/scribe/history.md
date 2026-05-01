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

