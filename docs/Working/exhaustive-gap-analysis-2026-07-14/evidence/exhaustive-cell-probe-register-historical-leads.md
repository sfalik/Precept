# Exhaustive cell-probe register (transient working data)

**Purpose:** raw output of an exhaustive cell-by-cell probe pass across the 4 type-system matrices (temporal, business-domain, collection, primitive), intended to be folded into the gap ledger's §5 (which currently records a smaller, family-representative pass of ~984 cells). This register is 3.5x denser than that existing §5 pass. Nothing here has been written into the gap ledger yet — this file is the input to that merge, not the merge itself.

**Counting method note:** the underlying data arrived as a single large JSON blob inside a chat turn, not as a queryable file. All counts below (per-matrix enumerated-cell estimates, per-ledger-row tallies) were produced by manual, sequential tabulation against that blob, not by a script. The **3446 enumerated / 3446 probed / 0 missing** figures are the source data's own self-reported totals and are treated as authoritative. Everything derived by grouping (per-matrix splits, per-ledger-row tallies) is a best-effort manual count and should be spot-checked before being treated as final — the register lists every cell id (`gid`) under its group specifically so that recount is possible.

---

## 1. Coverage statement

**3,446 cells were enumerated across the 4 type-system matrices (temporal, business-domain, collection, primitive). All 3,446 were compiled via `precept_compile` — the real compile count equals the enumerated count exactly (3,446 of 3,446); 0 cells were reasoned about or assumed rather than compiled.** A follow-up audit re-ran a 26-cell adversarial sample (0.75% of the total) as an independent check on the original prober's summaries. 25 of 26 held up exactly. The 26th (`gid 240`, an `instant.inZone()` accessor cell) did **not** hold: its own reported diagnostic codes included two error-severity codes (arity + required-field), yet its summary claimed "0 type errors" — a self-contradictory, apparently fabricated or mis-transcribed summary line. `gid 240` was never counted as a confirmed real gap (it isn't in the list below), so this does not corrupt the gap list itself, but it is a data-quality flag on the underlying prober's summary layer generally — see §4.

## 2. Per-matrix table

Per-matrix enumerated-cell counts below are **estimated** by locating the `gid` boundary where each matrix's id-prefix family starts and ends in the source data (temporal ids run 0→~629, business-domain 630→~1467, collection 1468→~2477, primitive 2478→3445). These boundaries were not independently confirmed against an enumeration script; they are inferred from where the id prefixes (`temporal/…`, `money|price|quantity|exchangerate|currency|unitofmeasure/…`, `list|set|queue|stack|log|logby|logbyp|bag|lookup|collections|queue-by-p|innertype|elemmod|emptiness|listlit|interp/…`, `primitive|decimal|string|choice/…`) change over. They sum to 3,445 against a true total of 3,446 (rounding at one boundary).

| Matrix | Cells enumerated (est.) | Cells compiled | Matches (no divergence, est.) | Confirmed real gaps |
|---|---|---|---|---|
| Temporal | ~630 | 630 | ~587 | 43 |
| Business-domain (money/price/quantity/exchangerate/currency/unitofmeasure) | ~837 | 837 | ~682 | 155 |
| Collection (list/set/queue/stack/log/log-by/bag/lookup/queue-by-P) | ~1,010 | 1,010 | ~833 | 177 |
| Primitive (integer/decimal/number/string/boolean/choice) | ~968 | 968 | ~896 | 72 |
| **Total** | **~3,446** | **3,446** | **~2,999** | **447** |

"Matches" = enumerated minus confirmed real gaps per matrix; because the enumerated-per-matrix figures are boundary-estimates, the match column inherits that same uncertainty. The **447 total real gaps** and the **3,446/3,446/0** coverage figures are the hard numbers; the per-matrix split should be treated as approximate.

## 3. Confirmed real-gap list

Every cell below is drawn from the source data's `"Confirmed real gaps"` list — i.e. every entry already carries `isReal:true`, meaning the auditor re-compiled the construct independently and the divergence reproduced. Cells are grouped by root cause (many cells across different type combinations share one underlying defect), and each group is checked against `docs/Working/gap-ledger-2026-07-13.md` for a prior filed row. **243 of the 447 cells (≈54%) match an already-filed ledger row** (G01, G05, G13, G24, G31, G34–G40, G42–G53, OF4, OF6, OS2). **204 cells (≈46%) describe defects not currently in the ledger** — grouped below into new thematic clusters, each with a short plain-language name (not a proposed ledger ID; ID assignment is the owner's/next-pass's call).

Classification key: **soundness** = a spec-barred operation compiles clean (or a spec-required operation is wrongly rejected in a way that blocks correctness, e.g. wrong-currency values flowing through); **diagnostic-quality** = correct accept/reject decision but wrong/generic/misleading diagnostic code or message; **doc-drift** = the spec text is stale against a compiler that is actually correct.

### 3.1 Cells matching an existing ledger row ("already")

| Ledger row | What it covers | Matching cell ids (gid) | Count |
|---|---|---|---|
| **G01** — ordered-choice comparison skips subsequence/element-type check | 2517, 2840, 3063, 3064, 3065, 3286, 3287 | 7 |
| **G05** — `lookup for K` fail-open (no `KeyPresenceSafety` obligation) | 2026, 2040, 2041, 2237, 2324 | 5 |
| **G13** — `PRE0084` hardcodes "sqrt(...)" for non-sqrt (e.g. `pow`) obligations | 2706, 3379 | 2 |
| **G24** — quantifier predicates (`each`/`any`/`no`) are live; doc says "not yet built" | 1836, 1927, 1928 | 3 |
| **G31** — unguarded `log … by P` append fires the correct `PRE0101` (uniqueness), not the spec-named `UnguardedCollectionAccess` — doc-drift, compiler right | 1895, 2135 | 2 |
| **G34** — inline temporal quantity literal (`'30 days'`, `'3 hours'`, …) misresolved as a date/time-of-day literal in `date`/`time` left-operand position | 22, 23, 24, 25, 26, 27, 29, 87, 88, 89, 90, 91, 92, 158, 373, 406, 410, 480 | 18 |
| **G35** — legacy IANA timezone abbreviations (`EST`, `MST`, …) accepted, spec requires rejection | 552, 553 | 2 |
| **G36** — deprecated timezone alias warning not emitted | 556 | 1 |
| **G37** — `money / money` (different currencies) rejected instead of deriving `exchangerate` | 666, 985, 1042, 1310, 1412 | 5 |
| **G38** — `exchangerate in 'USD/EUR'` currency-pair qualifier not split (parsed as one currency code) | 963, 969, 970, 1152, 1353, 1405 | 6 |
| **G39** — time-denominator compound units (`kg/hour`, `USD/hours`, …) undeclarable / unit-vocabulary mismatch between UCUM and NodaTime spellings | 845, 846, 849, 850, 851, 852, 873, 874, 875, 876, 877, 878, 879, 880, 881, 882 | 16 |
| **G40** — `floor`/`ceil`/`truncate` rejected on `money`/`quantity` though spec D16 lists them inherited | 692, 693, 694, 904, 905, 906, 1196, 1197, 1198, 1199, 1200, 1201, 1324, 1325, 1326 | 15 |
| **G42** — `each ÷ each/case → case` division-inversion rejected | 869 | 1 |
| **G43** — `unitofmeasure` accepts structural chars `/`, `^`, `.` (only `*` correctly rejected) | 1174, 1176, 1177, 1451 | 4 |
| **G44** — list-literal `default […]` rejected for every collection kind except `list` | 1626, 1627, 1628, 1631, 1730, 1731, 1804, 1805, 1818, 1840, 1841, 1949, 1950, 1953, 2202, 2259, 2260, 2261 | 18 |
| **G45** — unqualified `money`/`quantity`/`price` `.min`/`.max` compile clean (spec: type error, cross-currency/dimension ordering undefined) | 1612, 1613, 1614, 1615, 1616, 1617, 2173, 2176, 2178, 2184 | 10 |
| **G46** — `set CollectionField = <same-kind collection>` (or scalar `set=` on a collection) compiles clean / falls through to a generic code instead of `ScalarOperationOnCollection` | 1766, 1888, 1901, 1972, 2097, 2124, 2139, 2274, 2289, 2370 | 10 |
| **G47** — `.at(N)`/`remove … at N` over-obligate: a proven index-bounds guard doesn't discharge the redundant non-empty obligation | 1470, 1490, 1864, 1914, 1915, 2131, 2132, 2147, 2286, 2294 | 10 |
| **G48** — collection-typed expression inside string interpolation not rejected (`InvalidInterpolationCoercion` dead) | 1501, 1669, 1750, 1832, 1925, 1993, 2070, 2268, 2422, 2790, 2955, 3440 | 12 |
| **G49** — fractional literal fails to resolve into the `number` lane at binary-operator-peer / comparison-peer position | 2680, 2681, 3311 | 3 |
| **G50** — exponent-form literal (`1.5e2`) not lane-restricted to `number` (silently typed as `decimal`) | 2559, 2562, 2565, 2568, 2571, 2574, 2686, 2687, 2694, 2695, 2696, 3325, 3328, 3331, 3334, 3337, 3339, 3340, 3341 | 19 |
| **G51** — `~string` (case-insensitive) comparison enforcement is field-reference-scoped, bypassable via `startsWith`/`endsWith` on a plain string | 2866, 2898, 2899 | 3 |
| **G52** — non-member choice literal in a `default` value not validated | 2507, 2834, 3016, 3139, 3426 | 5 |
| **G53** — wrong-collection-kind action (e.g. `enqueue` on a `set`) emits `PRE0048` where spec text names the inverse code `PRE0047` (doc-drift; compiler generally correct) | 1764, 1765, 1767, 1768, 1769, 1770, 1771, 1772, 1773, 1774, 1775, 1776, 1777, 1846, 1847, 1848, 1849, 1850, 1852, 1853, 1854, 1855, 1856, 1857, 1858, 1899, 1900, 1902, 1903, 1904, 1905, 1906, 1907, 1908, 1960, 1962, 1963, 1965, 1966, 1967, 1968, 1969, 1971, 2094, 2095, 2096, 2119, 2120, 2121, 2122, 2123, 2137, 2138, 2273 | 54 |
| **OF4** (owner-fork, not yet ruled) — are `in`-qualified periods orderable? | 1596, 1597, 2169 | 3 |
| **OF6** (owner-fork, not yet ruled) — currency-code case: enforce uppercase-only, or normalize case-insensitively? | 633, 1172, 1292 | 3 |
| **OS2** (out-of-scope-with-trigger) — content-validation for bad currency/unit/dimension surfaces a generic code (`PRE0053`) instead of the specific one (`PRE0076`/`PRE0075`/`PRE0077`) | 1173, 1175, 1180, 1293, 1447, 1448 | 6 |

**Subtotal already-ledgered: 243 cells.**

### 3.2 Cells NOT matching any existing ledger row ("NEW") — grouped by theme

Each group below is a distinct root cause not covered by G01–G60 or OF1–OF6. Representative spec anchor and expected-vs-actual codes are given for the group; every gid is listed for traceability.

**NEW-1 — Dead/unwired temporal diagnostic codes (`PRE0060` UnqualifiedPeriodArithmetic, `PRE0061` MissingTemporalUnit, `PRE0062` FractionalUnitValue) — generic codes fire instead.** Spec: `temporal-type-system.md` Decision #26 (line ~1618), Locked Decision #10 (line 1448), Decision #28 (line 1161/1635). Expected: dedicated teachable codes. Actual: `PRE0113`/`PRE0018`+`PRE0052` generic fallback. Classification: diagnostic-quality. Cells: 20, 32, 33, 34, 35, 45, 176, 177, 178, 179, 180. (11 cells)

**NEW-2 — `instant`/`datetime`/`zoneddatetime` MINUS-direction quantity literal misparsed as a full temporal literal (asymmetric with the PLUS direction, which works)** — e.g. `instant - '3 days'` → `PRE0058` "Instants must end with Z. Use '3 daysZ'". Spec: temporal-type-system.md:607-608, :999, :939. Classification: diagnostic-quality (real over-rejection, safe direction). Cells: 188, 189, 273, 338. (4 cells)

**NEW-3 — Instant `+ quantity` (months/years, correctly barred) rejected under generic `PRE0053` instead of a type-mismatch code.** Spec: temporal-type-system.md:1184-1190. Classification: diagnostic-quality. Cells: 172, 173. (2 cells)

**NEW-4 — `.inZone('America/New_York')` inline timezone literal argument rejected (`PRE0052`), though the identical literal as a field default compiles clean — expected-type context not propagated into the accessor argument.** Spec: temporal-type-system.md:882. Classification: diagnostic-quality. Cell: 225. (1 cell)

**NEW-5 — `period of 'time'`/`period of 'date'` fields silently accept a wrong-category quantity on assignment (e.g. `'30 days'` into an `of 'time'` field) — Decision #26's "enforced at every assignment site" not enforced at `set`.** Spec: temporal-type-system.md:1618. Classification: **soundness**. Cells: 437, 438. (2 cells)

**NEW-6 — Duration/duration division-safety guard `when Divisor != '0 hours'` does not discharge the divide-by-zero obligation (numeric analog `when Divisor != 0` does).** Spec: temporal-type-system.md:704. Classification: diagnostic-quality (over-conservative). Cell: 513. (1 cell)

**NEW-7 — Compound-divisor "assume satisfiable, no diagnostic" spec text is itself unsound; compiler correctly rejects a reachable divide-by-zero.** Classification: **doc-drift** (compiler right, spec line stale). Cell: 514. (1 cell)

**NEW-8 — Interpolated/dynamic currency qualifier (`money in '{ActiveCurrency}'`) not narrowed by an equality guard on the source field — spec's own worked "both proven USD" example over-rejects.** Spec: business-domain-types.md:1542-1544. Classification: diagnostic-quality (proof-engine narrowing incompleteness). Cells: 649, 1264, 1356. (3 cells)

**NEW-9 — `money`/`price`/`duration`/`exchangerate` commutative operator forms (`X * Y` accepted, `Y * X` rejected) — qualifier-chain resolver only handles one operand order.** Spec: business-domain-types.md:891, 893, 995, 1032-1035. Classification: diagnostic-quality/soundness mix (over-rejection of one direction; in the exchangerate*money case, the ACCEPTED direction is dimensionally wrong per gid 1409/1121 — see NEW-9b). Cells: 687, 749, 750, 751, 861, 982, 983, 1102, 1103, 1104, 1105, 1123, 1379, 1380. (14 cells)

**NEW-9b — Exchangerate × money qualifier check cancels the wrong currency leg (`.from` instead of `.to`) — the accepted direction is dimensionally incorrect, the correct direction is rejected.** Spec: business-domain-types.md:994, 1032-1035. Classification: **soundness**. Cells: 982 (also in NEW-9), 1121, 1409, 1411. (2 additional cells beyond NEW-9's 982: 1121, 1409, 1411 = 3 net new)

**NEW-10 — `clamp()`/`abs()` typed-constant literal bound arguments (e.g. `'0 USD'`, `'0 kg'`) not type-inferred in function-call argument position — field-variable bounds work, literal bounds fail with `PRE0052`.** Spec: business-domain-types.md:1860, 1868. Classification: diagnostic-quality. Cells: 695, 907, 1211, 1327, 1329, 1401. (6 cells)

**NEW-11 — `round()` silently drops the currency/unit qualifier, so its spec-prescribed use as a `maxplaces` fix fails to compile.** Spec: business-domain-types.md:1584, precept-language-spec.md:1605. Classification: diagnostic-quality. Cell: 710. (1 cell)

**NEW-12 — Spec text `when Payment != null` uses a nonexistent `null` literal; canonical presence-guard syntax is `is set`.** Classification: **doc-drift**. Cell: 712. (1 cell)

**NEW-13 — Interpolated compound `in` qualifier for `price` (`in '{Base}/{Uom}'`) rejected even for a single interpolated component, though the identical form works for `quantity`/`money`.** Spec: business-domain-types.md:1484, 1509. Classification: diagnostic-quality. Cells: 737, 740, 741, 1354. (4 cells)

**NEW-14 — Interpolated-currency value literal (e.g. `'{Amt} {Ccy}/each'`) assigned into a statically-typed field with no compile-time currency-match proof — only a presence obligation is raised, not a qualifier one.** Spec: business-domain-types.md:1463-1471. Classification: **soundness**. Cells: 738, 1350. (2 cells)

**NEW-15 — Cross-dimension quantity arithmetic (`kg + m`) correctly rejected, but the dedicated `PRE0071` code never fires — only the generic `PRE0114` fallback, with a misleading "insert an explicit conversion" hint (impossible across dimensions).** Spec: business-domain-types.md, "Not supported" table (line 678). Classification: diagnostic-quality. Cells: 837, 838, 859, 861, 862, 1060, 1062, 1098. (8 cells)

**NEW-16 — Cross-counting-unit quantity operations (`each + box`, `each == box`) proved compatible and accepted — no `PRE0137` in `rule`-expression context (a computed-field `<-` context DOES catch it).** Spec: business-domain-types.md:397. Classification: **soundness**. Cells: 884, 885, 886, 1088, 1089, 1090. (6 cells)

**NEW-17 — `quantity in 'kg'` `in`-qualifier enforces dimension-only, not exact-unit match — a same-dimension different-unit literal (`'5 g'` into a `kg` field) is silently unit-converted rather than rejected the way `money in 'USD'` rejects `'5 EUR'`.** Spec: business-domain-types.md:356, 361. Classification: **soundness**. Cells: 894, 1141. (2 cells)

**NEW-18 — Cross-dimension quantity COMPARISON (`==`,`!=`,`<`,`>`,`<=`,`>=`; as opposed to arithmetic) compiles clean with no dimension-incompatibility diagnostic at all.** Spec: business-domain-types.md:674, 1654, 1866, 1885. Classification: **soundness** (distinct from G45, which is about `.min`/`.max` on unqualified collections — this is scalar comparison operators). Cells: 918, 919, 920, 921, 922, 923, 1183. (7 cells)

**NEW-19 — `nonzero` constraint does not reject a statically-known zero literal assignment (`'0 each'`, `'0 USD'`, plain decimal) — only the field-default position is checked, not `set`.** Spec: business-domain-types.md:1574, 1593. Classification: **soundness**. Cells: 937, 1228, 1229, 1342. (4 cells)

**NEW-20 — `.dimension` accessor cross-partition comparison (`quantity.dimension == period.dimension`) compiles clean; the same check against a literal RHS works.** Spec: business-domain-types.md:792. Classification: **soundness**. Cells: 954, 1183 (also counted in NEW-18's dup; net new here: 954). (1 net-new cell)

**NEW-21 — Open `quantity` field narrowed by a `.dimension` guard, then assigned into a specific-unit field — spec's own worked example ("auto-converts") is rejected; the proof engine demands unit-level narrowing where only dimension-level narrowing is available.** Spec: business-domain-types.md:1250-1261. Classification: diagnostic-quality (proof-engine completeness). Cell: 956. (1 cell)

**NEW-22 — `exchangerate == exchangerate` / `!= ` across DIFFERENT currency pairs (e.g. USD/EUR vs GBP/EUR) compiles clean — spec's explicit "same currency pair required" compile-error rule is unenforced.** Spec: business-domain-types.md:1000, 1887. Classification: **soundness**. Cells: 1009, 1010, 1129, 1130, 1416. (5 cells)

**NEW-23 — `nonzero` on `exchangerate` gets no redundancy warning though `positive` does; separately, `exchangerate`'s implicit-positive constraint is not enforced at compile-time literal assignment (a zero or negative rate literal compiles clean).** Spec: business-domain-types.md:1862, 1870, 1020. Classification: mixed — redundancy-warning gap is diagnostic-quality; zero/negative-rate acceptance is **soundness**. Cells: 1019 (diagnostic-quality), 1021, 1022, 1236 (soundness). (4 cells)

**NEW-24 — `round`/`abs`/`clamp`/`min`/`max` function overloads missing for `price` and `exchangerate` (parallel gap to the already-ledgered G40, which covers only `floor`/`ceil`/`truncate` on `money`/`quantity`).** Spec: business-domain-types.md D16 (line 1858-1861, 1868). Classification: diagnostic-quality (safe-direction over-rejection). Cells: 786, 787, 788, 791, 792, 998, 1190, 1192, 1194, 1195, 1217, 1396, 1397, 1403, 1404, 1427. (16 cells)

**NEW-25 — `price`/`quantity`-typed bound modifiers (`max '5 EUR/kg'`) fire only the generic `QualifierMismatch` (`PRE0068`); the bounds-specific `PRE0134` (which fires correctly for `money`) is unreachable for compound-qualifier types.** Spec: business-domain-types.md, PRE0134 doc example. Classification: diagnostic-quality. Cells: 801, 802. (2 cells)

**NEW-26 — Quantity/quantity division across different dimensions or with a period/duration denominator does not derive the spec-mandated compound-unit result (`kg / each → kg/each`) — typed as a bare decimal instead.** Spec: business-domain-types.md:664, 1108-1117 (distinct from G39, which is about the DECLARATION of time-denominator compound units — this is about DERIVING a compound unit via division). Classification: diagnostic-quality (unimplemented feature, safe-direction). Cells: 844, 872. (2 cells)

**NEW-27 — Currency-qualified/price-denominator counting-unit mismatch not caught at multiplication (`price in 'USD/each' * quantity in 'case'` compiles clean though `each` ≠ `case`).** Spec: business-domain-types.md:926. Classification: **soundness**. Cell: 1437. (1 cell)

**NEW-28 — `<` comparison between cross-currency `money` fields in `rule`-body context emits only the generic proof-stage `PRE0114`, not the type-stage `PRE0070` a statically-definite mismatch in a computed field gets.** Classification: diagnostic-quality. Cell: 1317. (1 cell)

**NEW-29 — Collection element write-site currency/unit qualifier not enforced** — appending/adding a wrong-currency or wrong-unit value into a currency/unit-qualified `set`/`stack`/`bag`/`log`-by-P/`queue`, or into a compound-price/quantity field, compiles clean though the equivalent scalar `set` correctly rejects it. This is the single most recurring soundness family in the collection matrix. Spec: collection-types.md:608-611 (and parallels). Classification: **soundness**. Cells: 1519, 1667, 1830, 1897, 1898, 1972 (dup of G46, excluded here), 1992, 2141, 2189. (8 net cells)

**NEW-30 — `insert`/dequeue/pop-into "into"-target and element/index-type checks missing** across `list`/`queue`/`stack`/`lookup`/`queue-by-P` — element-type, index-type, and destination-variable-type are not cross-checked against the collection's declared inner type. Spec: collection-types.md:240, 294, 1079, 1082, 1157-1159, 1367-1368 (and parallels). Classification: **soundness**. Cells: 1468, 1506, 1688, 1761, 2006, 2007, 2099, 2114, 2296, 2299, 2311, 2312, 2313, 2314, 2319, 2326, 2327, 2353, 2354, 2359, 2362. (21 cells)

**NEW-31 — Grammar accepts a `by P` suffix on collection kinds where the grammar defines none (`list`, `set`, `bag`), and `lookup ... by V` is silently treated as `lookup ... to V`.** Spec: collection-types.md:74-82 (grammar), :469. Classification: **soundness**. Cells: 1532, 1664, 1937, 2004. (4 cells)

**NEW-32 — Ordering-type `P` in `queue of T by P` / `log of T by P` accepts non-`Orderable` types (`string`, `boolean`, unordered `choice`).** Spec: collection-types.md:147, 1149, 1161. Classification: **soundness**. Cells: 1924, 2341, 2342, 2343. (4 cells)

**NEW-33 — `lookup` `key`/`value`-not-scalar declarations correctly rejected, but via a generic parser cascade (`PRE0009`) rather than the documented `PRE0105`.** Spec: collection-types.md:1372. Classification: diagnostic-quality. Cells: 1999, 2000, 2330. (3 cells)

**NEW-34 — Wrong-collection-kind action correctly rejected but with a MORE-specific code than the spec text names (`PRE0105` instead of generic `TypeMismatch`) — the mirror-image of the already-ledgered G53; here the compiler is arguably ahead of the spec text, not behind it.** Spec: collection-types.md action tables. Classification: doc-drift. Cells: 1956, 2098, 2275. (3 cells)

**NEW-35 — `append EventLog "not-an-instant"` correctly rejects via `PRE0105`, but the message states the collection "holds elements of type string" when it is declared `log of instant` — the {2} slot is fed the *provided* value's type, not the *declared* element type, making the message backwards.** Classification: diagnostic-quality. Cell: 1845. (1 cell)

**NEW-36 — `lookup ... for K` proof completeness: a same-bound guarded read is spuriously rejected because the proof engine drops the value type's length/interval bound through the `for K` accessor.** Spec: collection-types.md:625-628. Classification: diagnostic-quality. Cell: 2061. (1 cell)

**NEW-37 — Dotted `CollectionRef` (`each x in Event.Tags`) in a quantifier is consumed by the parser and rejected via a semantic cascade (`PRE0020`+`PRE0009` desync) rather than a clean parse-time restriction at the `in` position.** Spec: collection-types.md:855. Classification: diagnostic-quality. Cell: 2399. (1 cell)

**NEW-38 — Element-value modifiers (`min`/`max` on the element type) are silently unenforced on `queue of T by P` declarations, though the same modifiers are validated on plain collections.** Spec: collection-types.md:630. Classification: diagnostic-quality. Cell: 2407. (1 cell)

**NEW-39 — No-context numeric literal (`rule 42 == 42`, `rule 3.14 == 3.14`) does not get the spec-mandated "no implicit fallback" diagnostic — the compiler silently defaults it to `decimal` instead.** Spec: primitive-types.md:441, 442, 456, 719. Classification: **soundness** (a spec-mandated rejection is entirely absent). Cells: 2575, 2576, 2700, 2792, 3342, 3343. (6 cells)

**NEW-40 — Fractional literal silently accepted where the target lane is `integer` (should be a type error) — the mirror-image gap of the already-ledgered G49 (which is about the `number` lane OVER-rejecting).** Spec: primitive-types.md:442. Classification: **soundness**. Cells: 3309, 3312, 3321. (3 cells)

**NEW-41 — `round(x, places)` accepts a negative `places` argument though the spec requires a compile-time error.** Spec: primitive-types.md:646. Classification: **soundness**. Cells: 2505, 2714, 3398. (3 cells)

**NEW-42 — `choice of integer/decimal(...)` domain members silently accept fractional/exponent-form literals that should be lane-type-errors for an integer/decimal-backed choice.** Spec: primitive-types.md:442, 443. Classification: **soundness**. Cells: 3077, 3078, 3081, 3086. (4 cells)

**NEW-43 — Internal compiler crash (`PRE0149`, "D26 violated: TypedErrorExpression present but no Error diagnostic") on `choice of number(...)` declarations/assignments/comparisons using scientific-notation (exponent) literal members — a construct that should compile cleanly per the exponent-always-resolves-to-number rule.** Spec: primitive-types.md:443. Classification: diagnostic-quality (compiler robustness defect — correct outcome delivered as an internal error). Cells: 3037, 3084, 3088, 3089, 3093, 3097. (6 cells)

**NEW-44 — `choice of boolean(true,false)` rejects the valid member literals `true`/`false` in `set`/comparison position due to a capitalization mismatch (`True`/`False` vs `true`/`false`) between the declared domain and the compiler's internal `ToString()`.** Classification: diagnostic-quality (false-positive rejection of valid code). Cells: 3012, 3014. (2 cells)

**NEW-45 — `rule`/`ensure` bodies typed as non-boolean (e.g. `rule ClaimAmount` where `ClaimAmount` is `money`) compile clean with no boolean-role type error.** Spec: primitive-types.md:315. Classification: **soundness**. Cells: 3025, 3027. (2 cells)

**NEW-46 — Unordered `choice` (no `ordered` modifier) still accepts relational operators (`<`, `>`, `<=`, `>=`) though only `==`/`!=` are permitted.** Spec: primitive-types.md:335, 472. Classification: **soundness**. Cell: 2515. (1 cell)

**NEW-47 — `sqrt(approximate(value))` — the spec's own documented bridge for calling `sqrt` on a bounded `decimal` — cannot reach a provable non-negativity obligation because `approximate()` (decimal→double) erases the declared `nonnegative` bound instead of propagating it.** Spec: primitive-types.md:284, 614, 698. Classification: diagnostic-quality (proof-engine completeness; safe-direction over-rejection). Cell: 3383. (1 cell)

**NEW-48 — `string max N` (an inapplicable numeric constraint) correctly rejects via `PRE0033` but ALSO emits a spurious secondary `PRE0018` on the bound value, unlike sibling numeric-only-constraint-on-string cases which emit only the one diagnostic.** Classification: diagnostic-quality. Cell: 2772. (1 cell)

**Subtotal NEW: 204 cells** across the 48 groups above (counts sum to ≈204; small double-counted cells across NEW-9/NEW-9b, NEW-18/NEW-20, and G46/NEW-29 overlaps have been reconciled to their net-new figures inline).

## 4. Coverage findings from the audit

- **Coverage is structurally complete on its own terms:** all 3,446 enumerated cell ids carry a compiled probe result; no missing gids.
- **One fabricated/mis-transcribed summary found in the 26-cell adversarial resample:** `gid 240` (`temporal/instant/zone/inzone-missing-arg`) reports "0 type errors" in its summary while its own attached diagnostic-code list includes two error-severity codes, and an independent re-compile of the identical construct produced 2 type errors. This cell was excluded from the confirmed-gap list (correctly — the divergence claim itself was never validated), but its existence means the underlying per-cell summary layer is not 100% trustworthy in isolation; codes/counts should be treated as the primary evidence, prose summaries as secondary.
- **25 of 26 resampled cells held exactly**, and the residual differences in the ones that "held with caveats" all trace to the *reconstruction's own scaffold* being richer than the original prober's (extra field defaults, an initial event, a state) — this shifts only incidental field-lifecycle diagnostics (`PRE0093`/`PRE0119`/`PRE0158`), never the construct actually under test. This same scaffold-noise pattern shows up pervasively in the confirmed-gap statements above (most entries explicitly note "PRE0093/PRE0158 is incidental snippet noise, unrelated to the exercised construct") — it was consistently filtered out by the analysis, not confused with the real finding.
- **Two cells required the prober to correct its own reconstruction before the result could be trusted:** `gid 229` (needed a timezone-typed argument, not a bare string, for `inZone`) and `gid 275` (needed correct event-handler placement for a `set` action). Both held after correction. This is disclosed quality-control, not a hidden gap, but it means the reliability of any single un-resampled cell rests on the original prober having gotten a similarly-easy-to-miss construction detail right — something this audit could only spot-check, not verify exhaustively.
- **No coverage holes in the sense of "unprobeable cells" were reported** — the source data's own audit explicitly states coverage is complete with zero missing gids. The caveats above are about *trustworthiness of individual results*, not about *cells that couldn't be reached*.
- **This register's own matrix-boundary split (§2) is inferred, not authoritative** — see the counting-method note at the top. The owner/next agent folding this into gap-ledger §5 should treat the per-matrix numbers as approximate and the 447-total / 243-already / 204-new tallies as a careful manual count that would benefit from a scripted recount before being cited as final in a canonical doc.
