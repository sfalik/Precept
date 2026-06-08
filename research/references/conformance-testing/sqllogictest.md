# Mirror: sqllogictest (SQLite)

Snapshotted 2026-06-01.

## sqlite.org testing.html — differential oracle (Primary)

Source: https://sqlite.org/testing.html, accessed 2026-06-01. Grade: Primary (SQLite project doc).

> "The SQL Logic Test or SLT test harness is used to run huge numbers of SQL statements against both SQLite and several other SQL database engines and verify that they all get the same answers."

## sqlite.org about.wiki — modes (via search snippet; direct fetch 503 on 2026-06-01)

Source: https://www.sqlite.org/sqllogictest/doc/trunk/about.wiki, accessed 2026-06-01 (503 at fetch; content below from web-search result snippet of that page). Grade: Secondary (snippet of primary doc; see Threats to Validity).

> Completion mode: "reads a prototype script and runs the statements and queries against a reference database engine, and the output is a full script that is a copy of the prototype script with results inserted."
> Validation mode: "reads a full script and runs the statements and queries contained therein against a database engine under test. The results received back ... are compared against the results in the full script."
> "SLT currently compares SQLite against PostgreSQL, MySQL, Microsoft SQL Server, and Oracle 10g."
> Scale: "~6 million generated SQL queries"; "7.2 million queries comprising 1.12GB of test data."

## DataFusion README — .slt record format (Secondary, format-faithful)

Source: https://github.com/apache/datafusion/blob/main/datafusion/sqllogictest/README.md, accessed 2026-06-01. Grade: Secondary.

Query record:
```
query <type_string> <sort_mode>
<sql_query>
----
<expected_result>
```
type_string chars: B Boolean, D Datetime, I Integer, P timestamp, R float, T Text, ? other.
sort modes: nosort, rowsort, valuesort. Large results may be replaced by a hash of the rendered text.
