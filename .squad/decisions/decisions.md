## Decision: Protect `main` with PR-only flow

**Date:** 2026-04-05  
**Filed by:** Steinbrenner  
**Status:** Applied

### Decision
Protect `main` so all changes must go through a pull request. Use a solo-friendly configuration:

- require pull requests before merge
- require `0` approving reviews
- enforce for admins
- keep force pushes disabled
- keep branch deletion disabled

### Why
Shane is the only active user on the repository. We need to stop direct pushes to `main` without creating a self-locking workflow that would require a second reviewer who does not exist.

### Operational impact
Work now lands by branch + pull request only. Admin bypass is also blocked, so `main` stays protected consistently.
