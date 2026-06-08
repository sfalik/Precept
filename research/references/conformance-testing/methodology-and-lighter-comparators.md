# Mirror: methodology literature + lighter comparators

Snapshotted 2026-06-01.

## Category-partition method — Ostrand & Balcer (1988)

Source: T. J. Ostrand & M. J. Balcer, "The category-partition method for specifying and generating functional tests," CACM 31(6):676–686, June 1988. DOI 10.1145/62959.62964. Course summary fetched: https://coursys.sfu.ca/2016fa-cmpt-473-d1/pages/CategoryPartition + KAIST PDF copy https://swtv.kaist.ac.kr/courses/.../category-partition.pdf, accessed 2026-06-01. Grade: paper = Primary; SFU/KAIST course pages = Secondary.

> Categories are partitioned into choices representing "different cases that are expected to be handled differently or perhaps may represent error-prone boundary conditions."
> A test frame combines one choice from each category; the FIND example yields 4×3×3×3×3×3×2 = 1,944 potential frames.
> Constraints control explosion: `[if NonEmpty]` properties prevent contradictory pairings; "not all combinations make sense." `[error]` and `[single]` annotations "receive limited testing, reducing the FIND specification from 678 frames to 125."
> All-pairs alternative: tests "each pair of choices ... exactly once," reducing 4^20 combinations to ~3,040 pairs.

## DO-178C requirements-based testing (Secondary — vendor docs)

Sources: Rapita Systems (https://www.rapitasystems.com/do178c-testing), LDRA, Jama, accessed 2026-06-01. Grade: Secondary.

> "in verification, you must demonstrate the trace-ability of your test cases to requirements via requirements-based coverage analysis" and "to code structure through structural coverage analysis."
> Coverage by DAL: DAL A = statement + decision + MC/DC; DAL B = statement + decision; DAL C = statement.
> "all implemented functionality traces back to requirements and all dead code has been eliminated."

## Kotlin spec tests — sentence-level traceability (Primary)

Source: Kotlin/kotlin-spec CONTRIBUTING.md (raw, release branch), accessed 2026-06-01. Grade: Primary.

> A specification test is linked to "one *primary* sentence, which is the main thing the test is checking, and zero or more *secondary* sentences."
> "Sentences are identified by their section path plus their paragraph number plus their sentence number (aka sentence identifier)."
> "If a sentence have one or more tests linked to it (aka a *tested sentence*), it is highlighted in green; otherwise, it is highlighted in gray."

## Go test/ directory — header directives (Primary repo + Secondary issue)

Sources: go.dev/test/gc.go (accessed 2026-06-01, Primary), golang/go issue #19577 (Secondary).

> gc.go first line: `// run`
> "// ERROR annotations ... can only occur at the end of the line" (issue #19577). Directives include `// run`, `// errorcheck`, `// compile`, `// build`. `// errorcheck` files verify the compiler produces expected errors; `// ERROR` annotations mark expected messages per line.

## TypeScript conformance tests + baselines (Primary wiki)

Source: microsoft/TypeScript wiki "Spec conformance testing", accessed 2026-06-01. Grade: Primary (project wiki).

> Spec changes should be observable in the "tests/cases/conformance folder."
> Baseline workflow: when moving tests, "The only change should be the path of the test they reference." Baselines (`.errors.txt`, `.types`, `.js`) capture expected compiler output; new/changed output appears as a baseline diff that is reviewed and accepted.
