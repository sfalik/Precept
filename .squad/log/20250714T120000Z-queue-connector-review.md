# Session Log — Queue Connector Review

**Timestamp:** 2025-07-14T12:00:00Z
**Topic:** queue-connector-review
**Agents:** Frank (Lead/Architect), Elaine (UX Designer)

Frank and Elaine reviewed the priority queue connector surface from complementary angles. Frank concluded that `by` remains the technically safest action-site connector and that `at` should stay rejected, while Elaine concluded that `with` reads better at enqueue/dequeue capture because it avoids the directional mismatch of `by`; she also emphasized that docs should teach the three-line `peek` / `priority` / `dequeue` pattern before the compound shorthand. Both decision records were merged into `decisions.md` in this session.