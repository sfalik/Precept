# Mirror: Ada ACATS / ACVC conformity assessment

Snapshotted 2026-06-01.

## Wikipedia (Secondary) — exhaustiveness, history

Source: https://en.wikipedia.org/wiki/Ada_Conformity_Assessment_Test_Suite, accessed 2026-06-01. Grade: Secondary (encyclopedia summarizing primary ISO/ada-auth material).

> "the ACATS is a set of test programs intended to check broadly for correct implementation; it is not possible to exhaustively test for conformity."

> ACVC tests "developed by the American company SofTech, beginning around 1980"; "The year 1985 saw the issuing of the first Ada validation certificates"; ACVC "came to an end with the closure of the Ada Joint Program Office in 1998."

ACATS: 1821 tests, 255,838 lines of code, 30 MB (per ada-auth, summarized).

## Test classes + grading (ACATS User's Guide, summarized via search of ada-auth.org PDF)

Source: ACATS 4.2 User's Guide (http://www.ada-auth.org/acats-files/4.2/docs/ACATS-UG.PDF — direct fetch timed out 2026-06-01; content below sourced from web-search snippet of that PDF + ada-auth.org). Grade: the class-semantics claim is Primary-origin (ACATS UG) but retrieved Tertiary (search snippet, direct fetch failed — see Threats to Validity).

> "Class A, C, D, and E tests are executable, while Class B tests are expected to produce compilation errors. Class L tests are expected to produce compilation or link errors."

> Class B: "An implementation passes a class B test if each indicated error in the test is detected and reported, and no other errors are reported. The test fails if one or more of the indicated errors are not reported, or if an error is reported that cannot be associated with one of the indicated errors." Potentially illegal constructs are flagged with `-- ERROR:`.

> Class L: "An implementation passes a class L test if [it] does not successfully complete the bind phase ... It fails if the test successfully binds and/or begins execution."

## Coverage documents / traceability (ISO/IEC 18009)

Source: ada-auth.org + ISO/IEC 18009 framing, accessed 2026-06-01. Grade: Secondary.

> "The ACATS Coverage Documents record the mapping of test objectives and tests to Ada rules, making it possible to determine if individual rules are adequately tested."

> "ISO/IEC-18009, Ada: Conformity of a Language Processor ... provides a framework for testing language processors, providing a stable and reproducible basis for testing."
