## Core Context

- Owns the squad's durable record: `.squad\decisions.md`, `.squad\decisions-archive.md`, `.squad\orchestration-log\`, `.squad\log\`, agent histories, and PR-body stewardship.
- The standing Scribe loop is fixed: measure health first, run archive gates before merges, consolidate overlapping inbox notes into canonical decisions, propagate affected history updates, summarize oversized histories immediately, and stage only the exact `.squad\` paths touched in-session.
- Inbox cleanup is not complete until the deletions are persisted in git alongside the updated ledger.

## Recent Updates

### 2026-05-17T12:46:26Z — Constructor semantics tooling batch recorded

- Pre-check measured `.squad\decisions.md` at 1015376 bytes, `.squad\decisions-archive.md` at 1738938 bytes, and the inbox at 14 files.
- Archived 0 decision entries older than the 30-day cutoff before merging 14 unique inbox records into the primary ledger; 0 duplicate records were suppressed during the pass.
- Wrote 7 orchestration logs, recorded `.squad\log\2026-05-17T12-46-26Z-constructor-semantics-tooling.md`, refreshed George/Kramer/Newman history, and summarized George/Kramer back under the 15 KB gate.
- Health report: decisions 1015376 B -> 1043862 B; archive 1738938 B -> 1738938 B; inbox 14 -> 0; largest history after update 15035 B.

### 2026-05-16T03:08:40Z — Frank-26 batch recorded

- The archive gate was explicit even when it moved no entries, and the no-op batch still closed with logs only.
- Durable reminder: current-batch health belongs in the paired session log, not as sprawling chronology in history prose.

### 2026-05-15T20:40:13Z — Price qualifier enforcement architecture batch recorded

- Merged the inbox into canonical decisions, wrote orchestration/session logs, propagated cross-agent context, and summarized Scribe history to stay under the hard gate.
- Durable rule: exact-path staging matters as much as the text merge; broad `.squad\` staging is not allowed on this branch.
