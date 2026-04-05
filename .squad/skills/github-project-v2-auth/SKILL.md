# Skill: GitHub Project V2 Auth Check

## When to use

Use this before creating, listing, or updating a GitHub Projects v2 board with `gh` or GraphQL.

## Procedure

1. Confirm repo owner context from `git remote -v`.
2. Check `gh auth status` and note current scopes.
3. If the task is Projects v2, verify the token includes:
   - `project`
4. Treat `gh project list --owner <owner>` as the real capability check. In practice, a token that reports `project` may already satisfy read access even if `read:project` is not shown separately in `gh auth status`.
5. If scope status is unclear, verify with GraphQL or the `gh project` commands directly:
   - list/query project fields will fail when read access is missing
   - `createProjectV2` will fail when write access is missing
6. If scopes are missing, treat project-board creation as blocked until auth is refreshed with project access.
7. If scopes are present, create the project, set description/readme immediately (`gh project edit <number> --description ...`), and then verify visibility with `gh project list --owner <owner>` or `gh project view`.
8. Do not assume classic repo projects are a fallback; verify the repo-project endpoint separately before planning around it.

## Why

`repo` scope alone does not authorize GitHub Projects v2 operations. A token with `project` scope is sufficient for `gh project list/create/edit` in this environment, even when `gh auth status` does not show `read:project` separately.

## Example

- `gh project list --owner sfalik` → fails without `read:project`
- `gh api graphql` with `createProjectV2` → fails without `project`
- `gh api repos/sfalik/Precept/projects` → can return `404`, so classic projects may not exist as a usable backup
- `gh project create --owner sfalik --title "Precept Preview Panel Redesign"` → returns `https://github.com/users/sfalik/projects/1` when project access is available
- `gh project edit 1 --owner sfalik --description "Track scope, design decisions, and delivery for the Precept preview panel redesign."` → sets the short description immediately after creation
